# Pasty Propellant — Mathematical Model

This document is the reference description of the combustion model implemented in `ParametricCombustionModel.Computation` together with the optimization layer that fits its 18 free parameters to experimental burn-rate data. It is meant to be read alongside:

- the C# source (every formula carries a `[file.cs:Lxx-Lyy]` link);
- a generated PDF report (e.g. `propellants.group_optimization.report.en.pdf`) — each quantity lists the PDF resource key it is printed under;
- a JSON input file (e.g. `data/propellants.01234.json`) — each input lists its JSON field path.

> **Convention.** Identifiers (variable names, classes, JSON fields) are kept in English exactly as in the code. The .resx report files ship in `ru-RU`, `en-US` and `fr-FR`; this document mirrors the `en-US` labels.

---

## 1. Overview and conventions

The model represents a single point on the propellant burning surface as a steady-state 1-D problem. The surface is at $z=0$, the gas phase occupies $z>0$. Three combustion sub-regions are tracked side-by-side on the surface:

- **Inter-pocket region** (`InterPocketPropellantSolver`): a homogeneous gas phase with a single kinetic flame. No metal, no diffusion flame.
- **Pocket region** (`PocketPropellantSolver`): heterogeneous gas phase containing
  - a **skeleton sub-region** with its own kinetic flame and a burning metal skeleton conducting heat to the surface, and
  - an **out-skeleton sub-region** with its own kinetic flame;
  - plus a single **diffusion flame** above both sub-regions.
- **Mixed propellant** (`MixedPropellantSolver`) blends the two via a volume-fraction harmonic mean of burn rates.

### 1.1 Sign conventions
- Heat fluxes are **positive magnitudes** (`W/m²`); the energy balance is written as `q_total − q_sublimation = 0`.
- Surface temperature root finding returns the sentinel `−1 K` when no root is bracketed.

### 1.2 Two-tier implementation: ByUnits and ByDoubles
Every solver and every parameter struct has two mirror implementations:

| Tier | Suffix | Numbers | Purpose |
|---|---|---|---|
| Type-safe | `ByUnits` | UnitsNet types (`Temperature`, `Pressure`, `MassFlux`, `Frequency`, …) | Final-result evaluation, reports. |
| Performance | `ByDoubles` | raw `double` in SI | Inner DE loop. |

The two paths must produce identical numbers. Any drift between them is a bug. See [§17 Known limitations](#17-known-limitations--open-questions).

---

## 2. Physical constants

| Symbol | Name | Value | Unit |
|---|---|---|---|
| $R$ | Universal gas constant | `8.314_462_618_153_24` | $\mathrm{J/(mol\cdot K)}$ |
| $\sigma$ | Stefan–Boltzmann constant | `5.670_374_419_184_43 \times 10^{-8}` | $\mathrm{W/(m^{2}\cdot K^{4})}$ |

Source: [PhysicalConstants.cs:11-16](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Common/PhysicalConstants.cs#L11-L16).

---

## 3. Inputs

### 3.1 JSON propellant schema

The runtime loads `data/propellants*.json` as an array of `Propellant` records. The schema is defined by the C# `record` types under `ParametricCombustionModel.Core.Models/`. One annotated example (truncated to a single pressure frame):

```json
{
  "name": "Bas_0",
  "a": 1.720582e-06,                  // Vieille's law A coefficient (m/s · Pa^(-Nu))
  "nu": 0.59,                         // Vieille's law pressure exponent
  "components": {
    "CombustibleBinder":   { "mass_fraction": 0.21,   "density": 950  },
    "AmmoniumPerchlorate": { "mass_fraction": 0.30,   "density": 1952,
                              "large_particles_fraction":  0.65,
                              "average_particles_diameter": 0.000135 },   // m
    "Aluminum": { "mass_fraction": 0.2073, "density": 2700,
                  "agglomeration_coefficients": [ 0.1895, -0.0056, -0.0032, 0.00026 ] },
    "Octogen":  { "mass_fraction": 0.2827, "density": 1870 }
  },
  "density": 1659,                    // kg/m³
  "specific_heat_capacity": 1500,     // J/(kg·K)
  "initial_temperature": 298,         // K
  "pocket_surface_fraction_coefficients": [ 0.1895, -0.0056, -0.0032, 0.00026 ],
  "pocket_mass_fraction": 0.7,
  "pressure_frames": [
    {
      "pressure": 1_000_000,                          // Pa
      "porosity_within_skeleton": 0.7477,             // dimensionless φ
      "inter_pocket_gas_phase": {                     // HomogeneousGasPhase
        "lambda_gas": 0.1,                            // W/(m·K)
        "average_molar_mass": 0.033,                  // kg/mol
        "c_volume": 635.6,                            // J/(m³·K) volumetric c_p
        "T_kinetic_flame": 3707.0                     // K
      },
      "pocket_gas_phase": {                           // HeterogeneousGasPhase
        "lambda_gas": 0.1, "average_molar_mass": 0.033, "c_volume": 635.6,
        "T_diffusion_flame": 3666.0,
        "skeleton_gas_phase":     { "lambda_gas": 0.1, "average_molar_mass": 0.033,
                                    "c_volume": 635.6, "T_kinetic_flame": 1591.0 },
        "out_skeleton_gas_phase": { "lambda_gas": 0.1, "average_molar_mass": 0.033,
                                    "c_volume": 635.6, "T_kinetic_flame": 2365.0 }
      }
    }
  ]
}
```

Schemas: [Propellant.cs:81-115](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Core/Models/Propellant.cs#L81-L115), [PressureFrame.cs:6-19](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Core/Models/PressureFrame.cs#L6-L19), [HomogeneousGasPhase.cs:5-18](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Core/Models/GasPhases/HomogeneousGasPhase.cs#L5-L18), [HeterogeneousGasPhase.cs:5-24](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Core/Models/GasPhases/HeterogeneousGasPhase.cs#L5-L24), [AmmoniumPerchlorate.cs:5-18](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Core/Models/PropellantComponents/AmmoniumPerchlorate.cs#L5-L18).

### 3.2 Derived inputs (from the JSON)

These quantities are precomputed once per propellant/pressure pair by `ProblemContextByUnitsMatrixBuilder` and stored on `ProblemContextByUnits`:

| Quantity | Symbol | Source | Code |
|---|---|---|---|
| Pocket / skeleton surface fraction | $f_s$ | polynomial in $p$ (see §12.1) | [PropellantExtensions.cs:135-144](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L135-L144) |
| Average oxidizer (large AP) diameter | $d_{ox}$ | `components.AmmoniumPerchlorate.average_particles_diameter` | [PropellantExtensions.cs:114-121](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L114-L121) |
| Metal melting temperature | $T_{melt}$ | hard-coded **2300 K** | [PropellantExtensions.cs:155-159](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L155-L159) |
| Metal boiling temperature | $T_{boil}(p)$ | 6th-order polynomial in $p\,[\text{MPa}]$ | [PropellantExtensions.cs:173-193](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L173-L193) |
| Inter-pocket volume fraction | $f_{V,inter}$ | mass-fraction sum / density mix | [PropellantExtensions.cs:26-58](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L26-L58) |
| Pocket volume fraction | $f_{V,pocket}$ | same | [PropellantExtensions.cs:72-100](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L72-L100) |

> **Bas_2 anomaly.** `GetInterPocketAreaVolumeFraction` multiplies the result by `1.5` when `propellant.Name == "Bas_2"`. This is an empirical correction baked into the code. See [§17](#17-known-limitations--open-questions).

### 3.3 Experimental burn rate (Vieille's law)

$$v_b^{exp}(p) = A\,p^{\nu}$$

| Symbol | Meaning | Unit | Source |
|---|---|---|---|
| $A$ | Vieille's law coefficient | $\mathrm{m\cdot s^{-1}\cdot Pa^{-\nu}}$ | JSON `Propellant.a` |
| $\nu$ | Vieille's law pressure exponent | dimensionless | JSON `Propellant.nu` |
| $p$ | Chamber pressure | Pa | JSON `PressureFrame.pressure` |

**Code:** [PropellantExtensions.cs:27-32](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Extensions/PropellantExtensions.cs#L27-L32), [PropellantExtensions.cs:48-54](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Extensions/PropellantExtensions.cs#L48-L54).
**In PDF:** `PropellantReport`, key `VieillesLaw` (`U = A · p^v, A = {0}; v = {1}`).
**Used by:** the fitness function (§13) as the experimental target $v_b^{exp}$.

> **Two distinct uses of `nu`.** The Vieille's law exponent (`Propellant.nu`) is unrelated to the gas-phase reaction orders `NuInterPocket`, `NuPocketSkeleton`, `NuPocketOutSkeleton` that appear in §8. Conflating them is a common mistake.

### 3.4 Solver-parameter families

These five structs bundle the inputs that the solver receives every iteration. They are populated once per (propellant × pressure) cell by the context-matrix builders.

| Family | Code | Fields (units) | JSON origin |
|---|---|---|---|
| `CombustionSolverParamsByUnits` | [CombustionSolverParamsByUnits.cs:127-235](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/CombustionSolverParamsByUnits.cs#L127-L235) | the 18 optimization parameters (see §3.5) | DE-optimized |
| `PropellantParamsByUnits` | [PropellantParamsByUnits.cs:59-93](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/PropellantParamsByUnits.cs#L59-L93) | $c_p$ J/(kg·K), $\rho$ kg/m³, $T_0$ K, $d_{ox}$ m, $f_s$ (Ratio) | `density`, `specific_heat_capacity`, `initial_temperature`, oxidizer diameter, surface-fraction polynomial |
| `KineticFlameParamsByUnits` (×3) | [KineticFlameParamsByUnits.cs:57-86](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/KineticFlameParamsByUnits.cs#L57-L86) | $T_{f}$ K, $M$ kg/mol, $\lambda_g$ W/(m·K), $c_{p,V}$ J/(m³·K) | `…_gas_phase.T_kinetic_flame`, `average_molar_mass`, `lambda_gas`, `c_volume` |
| `DiffusionFlameParamsByUnits` | [DiffusionFlameParamsByUnits.cs:57-86](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/DiffusionFlameParamsByUnits.cs#L57-L86) | $T_{f,\mathrm{diff}}$ K, $M$, $\lambda_g$, $c_{p,V}$ | `pocket_gas_phase.T_diffusion_flame`, `lambda_gas`, `average_molar_mass`, `c_volume` |
| `MetalCombustionParamsByUnits` | [MetalCombustionParamsByUnits.cs:40-55](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/MetalCombustionParamsByUnits.cs#L40-L55) | $T_{melt}$, $T_{boil}$ both K | hard-coded / polynomial of $p$ |
| `SkeletonLayerParamsByUnits` | [SkeletonLayerParamsByUnits.cs:18-23](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/SkeletonLayerParamsByUnits.cs#L18-L23) | $\varphi$ porosity, $\lambda_c^{(s)}$ W/(m·K) | `porosity_within_skeleton`; $\lambda_c^{(s)}$ is the condensed-phase conductivity used in §7.2 |

### 3.5 The 18-element optimization vector (single composition)

[CombustionSolverParamsByUnits.cs:93-117](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/CombustionSolverParamsByUnits.cs#L93-L117), [CombustionSolverParamsByUnits.cs:207-232](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/CombustionSolverParamsByUnits.cs#L207-L232).

| idx | Field | Symbol | Unit | Role |
|---:|---|---|---|---|
| 0 | `ADecompose` | $A_d$ | kg/(m²·s) | Arrhenius pre-exponential for condensed-phase decomposition |
| 1 | `EDecompose` | $E_d$ | J/mol | activation energy for decomposition |
| 2 | `AKineticFlameInterPocket` | $A_k^{(IP)}$ | 1/s | Arrhenius pre-exp. for inter-pocket kinetic flame |
| 3 | `EKineticFlameInterPocket` | $E_k^{(IP)}$ | J/mol | activation energy, inter-pocket kinetic flame |
| 4 | `AKineticFlamePocketOutSkeleton` | $A_k^{(OS)}$ | 1/s | Arrhenius pre-exp. for pocket out-skeleton kinetic flame |
| 5 | `EKineticFlamePocketOutSkeleton` | $E_k^{(OS)}$ | J/mol | activation energy, pocket out-skeleton kinetic flame |
| 6 | `AKineticFlamePocketSkeleton` | $A_k^{(S)}$ | 1/s | Arrhenius pre-exp. for pocket skeleton kinetic flame |
| 7 | `EKineticFlamePocketSkeleton` | $E_k^{(S)}$ | J/mol | activation energy, pocket skeleton kinetic flame |
| 8 | `NuInterPocket` | $\nu^{(IP)}$ | — | reaction order, inter-pocket kinetic flame |
| 9 | `NuPocketOutSkeleton` | $\nu^{(OS)}$ | — | reaction order, pocket out-skeleton kinetic flame |
| 10 | `NuPocketSkeleton` | $\nu^{(S)}$ | — | reaction order, pocket skeleton kinetic flame |
| 11 | `AMetalBurningConstant` | $A_m$ | m²/s | skeleton-thickness power-law coefficient |
| 12 | `BMetalBurningConstant` | $B_m$ | m³/s² | pore-diameter power-law coefficient |
| 13 | `DeltaH` | $\Delta H$ | J/kg | specific energy of sublimation minus decomposition (latent heat term) |
| 14 | `KDiffusionHeight` | $K_h$ | — | diffusion-flame-height proportionality |
| 15 | `APowOrder` | $p_A$ | — | exponent in $\delta_s = A_m / v_b^{p_A}$ |
| 16 | `BPowOrder` | $p_B$ | — | exponent in $d_p = B_m / v_b^{p_B}$ |
| 17 | `KCoefficientRadiationTemperature` | $k_{rT}$ | — | convex combination weight in $\bar T_r$ (§7.1) |

Bounds for the DE optimizer are defined inline at [Program.cs:53-61](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L53-L61).

### 3.6 The 32-element group vector (joint optimization)

When several compositions are optimized at once, the DE vector has 32 elements. The 11 first elements are **shared** across all three composition groups; the next three blocks of 7 hold composition-specific values:

```
[ 0..10 ]   shared (11)
[11..17 ]   Bas_2 + Bas_3 + Bas_4 specific (7)   compositionIndex = 0
[18..24 ]   Bas_1 specific (7)                   compositionIndex = 1
[25..31 ]   Bas_0 specific (7)                   compositionIndex = 2
```

**Important: the 32-vector slot order is NOT the 18-vector slot order.** The shared block stores fields in a different sequence; the composition-specific block also reorders the 7 fields. The canonical 32→18 mapping for evaluating one composition is `GroupCombustionSolverParamsByDoubles.ToCompositionVector(idx)`:

[GroupCombustionSolverParams.cs:123-164](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/GroupCombustionSolverParams.cs#L123-L164):

| 32-vector index | 32-vector field | → 18-vector index |
|---:|---|---:|
| 0 | `AKineticFlameInterPocket` | 2 |
| 1 | `EKineticFlameInterPocket` | 3 |
| 2 | `AKineticFlamePocketOutSkeleton` | 4 |
| 3 | `EKineticFlamePocketOutSkeleton` | 5 |
| 4 | `NuInterPocket` | 8 |
| 5 | `NuPocketOutSkeleton` | 9 |
| 6 | `DeltaH` | 13 |
| 7 | `KDiffusionHeight` | 14 |
| 8 | `APowOrder` | 15 |
| 9 | `BPowOrder` | 16 |
| 10 | `KCoefficientRadiationTemperature` | 17 |
| 11 + 7·g | `ADecomposeBas{*}` | 0 |
| 12 + 7·g | `EDecomposeBas{*}` | 1 |
| 13 + 7·g | `AKineticFlamePocketSkeletonBas{*}` | 6 |
| 14 + 7·g | `EKineticFlamePocketSkeletonBas{*}` | 7 |
| 15 + 7·g | `NuPocketSkeletonBas{*}` | 10 |
| 16 + 7·g | `AMetalBurningConstantBas{*}` | 11 |
| 17 + 7·g | `BMetalBurningConstantBas{*}` | 12 |

with $g \in \{0,1,2\}$ for Bas_2-group, Bas_1, Bas_0.

The reverse remapping (18-bounds → 32-bounds), used to construct the DE search box, lives in [Program.cs:65-107](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L65-L107).

Bas_2, Bas_3, Bas_4 share the same 7 composition-specific values — that is the load-bearing reason for the 32-parameter formulation.

---

## 4. Surface temperature: root finding

For each region (`InterPocket`, `Pocket`), the surface temperature $T_s$ is the root of the heat-flux residual:

$$F(T_s) \;\equiv\; q_{\text{total}}(T_s) \;-\; q_{\text{sub}}(T_s) \;=\; 0$$

where $q_{\text{total}}$ collects every incoming heat flux contribution (kinetic flames, metal, diffusion as applicable) and $q_{\text{sub}}$ is the heat absorbed by sublimating/decomposing condensed phase (§11). Search bounds and tolerance:

| Parameter | Value | Source |
|---|---|---|
| $T_s^{\min}$ | 600 K (default) | [ProblemContextByUnits.cs:98](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/ProblemContexts/ProblemContextByUnits.cs#L98) |
| $T_s^{\max}$ | 750 K (default) | [ProblemContextByUnits.cs:105](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/ProblemContexts/ProblemContextByUnits.cs#L105) |
| tolerance | $10^{-8}$ K | [BasePropellantSolver.cs:113](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BasePropellantSolver.cs#L113), [BasePropellantSolver.cs:267](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BasePropellantSolver.cs#L267) |
| failure sentinel | $T_s = -1$ K | [BasePropellantSolver.cs:207-209](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BasePropellantSolver.cs#L207-L209) |

**Algorithm.** Standard bisection. If $F(T_s^{\min}) \cdot F(T_s^{\max}) > 0$ no root is bracketed and the sentinel is returned; the upstream code then sets `BurnRateIsFound = false` and the fitness function for that genome returns `double.MaxValue`.

**Code:** [BasePropellantSolver.cs:193-233](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BasePropellantSolver.cs#L193-L233) (ByUnits), [BasePropellantSolver.cs:347-385](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BasePropellantSolver.cs#L347-L385) (ByDoubles).

**In PDF:** the resulting $T_s$ is printed per region under `ProblemContextReport` (`SurfaceTemperature`); the configured bounds appear in `ParametricConstraintReport` (`MinSurfaceTemperature`, `MaxSurfaceTemperature`).

---

## 5. Condensed-phase decomposition and burn rate

### 5.1 Arrhenius decomposition rate (mass flux)

$$\dot m_d(T_s) \;=\; A_d \,\exp\!\Big(-\frac{E_d}{R\,T_s}\Big)$$

**Code:** [BasePropellantSolver.cs:149-161](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BasePropellantSolver.cs#L149-L161), [BasePropellantSolver.cs:303-315](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BasePropellantSolver.cs#L303-L315).
**Inputs:** $A_d$ (vector[0]), $E_d$ (vector[1]), $T_s$ (§4), $R$ (§2).
**In PDF:** `ProblemContextReport` key `DecomposeRate` ("Mass decomposition rate"); `CombustionSolverParamsReport` keys `ADecompose`, `EDecompose`.

### 5.2 Linear burn rate

$$v_b \;=\; \dot m_d / \rho$$

**Code:** [BasePropellantSolver.cs:133-137](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BasePropellantSolver.cs#L133-L137), [BasePropellantSolver.cs:287-291](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BasePropellantSolver.cs#L287-L291).
**Inputs:** $\dot m_d$ (§5.1); $\rho$ from `PropellantParamsByUnits.Density` (JSON `density`).
**In PDF:** `ProblemContextReport` key `LinearBurnRate`, `PressureTablesReport` rows `LinearBurningRate`, `CalculatedBurningRate`.

---

## 6. Skeleton-layer geometry

These two power laws set the geometric scale of the burning metal skeleton inside the pocket.

### 6.1 Skeleton layer thickness

$$\delta_s \;=\; \frac{A_m}{v_b^{\,p_A}}$$

**Code:** [PocketPropellantSolver.cs:543-551](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L543-L551), [PocketPropellantSolver.cs:904-912](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L904-L912).
**Inputs:** $A_m$ (vector[11]), $p_A$ (vector[15]), $v_b$ (§5.2).
**In PDF:** `ProblemContextReport` key `SkeletonLayerThickness`.

### 6.2 Pore diameter

$$d_p \;=\; \frac{B_m}{v_b^{\,p_B}}$$

**Code:** [PocketPropellantSolver.cs:553-564](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L553-L564), [PocketPropellantSolver.cs:914-922](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L914-L922).
**Inputs:** $B_m$ (vector[12]), $p_B$ (vector[16]), $v_b$ (§5.2).
**In PDF:** `ProblemContextReport` key `PoreDiameter`.

---

## 7. Thermal conductivity of the porous skeleton

### 7.1 Average radiative temperature (closure)

The radiative conductivity (§7.2) is evaluated at a temperature that is a convex combination of the surface temperature and the skeleton kinetic-flame temperature:

$$\bar T_r \;=\; k_{rT}\,T_s \;+\; (1 - k_{rT})\,T_f^{(S)}$$

**Code:** [PocketPropellantSolver.cs:465-466](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L465-L466), [PocketPropellantSolver.cs:837-838](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L837-L838).
**Inputs:** $k_{rT}$ (vector[17]), $T_s$ (§4), $T_f^{(S)}$ = skeleton kinetic-flame final temperature (JSON `pocket_gas_phase.skeleton_gas_phase.T_kinetic_flame`).
**Note.** This is a phenomenological closure, not a derived quantity. With $k_{rT} = 0$ the radiative temperature equals $T_f^{(S)}$.

### 7.2 Radiative conductivity — Rosseland approximation

$$\lambda_r \;=\; \frac{16\,\sigma\,\bar T_r^{3}}{3\,\beta},\qquad \beta \;=\; \frac{3(1-\varphi)}{d_p}$$

$\beta$ is the **Rosseland mean extinction coefficient** for a packed bed of opaque particles (Goldsmith–Larkin; Modest, *Radiative Heat Transfer*; Kuo, *Principles of Combustion*).

**Code:** [PocketPropellantSolver.cs:566-586](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L566-L586), [PocketPropellantSolver.cs:924-944](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L924-L944).
**Inputs:** $\bar T_r$ (§7.1), $d_p$ (§6.2), $\varphi$ (JSON `porosity_within_skeleton`), $\sigma$ (§2).
**In PDF:** `ProblemContextReport` key `RadiativeThermalConductivity`.

> **History.** The 1/3 prefactor was missing from this formula before commit `9ca96ff` (2026-05-27); previously the code computed $16\sigma\bar T_r^3 / \beta$, overestimating $\lambda_r$ by exactly a factor of 3. See [§17](#17-known-limitations--open-questions).

### 7.3 Conductive thermal conductivity — effective-medium closure

$\lambda_c$ is the root of the Bruggeman (symmetric effective-medium) residual

$$\varphi\,\frac{\lambda_g - \lambda_c}{\lambda_g + 2\lambda_c}\;+\;(1-\varphi)\,\frac{\lambda_c^{(s)} - \lambda_c}{\lambda_c^{(s)} + 2\lambda_c}\;=\;0$$

where $\lambda_g$ is the gas-phase thermal conductivity (`DiffusionFlameParamsByUnits.ThermalConductivity`, i.e. JSON `pocket_gas_phase.lambda_gas`) and $\lambda_c^{(s)}$ is the condensed-phase conductivity (`SkeletonLayerParamsByUnits.CondensedThermalConductivity`).

**Algorithm:** bisection on $\lambda_c \in [0, 10^5]\,\mathrm{W/(m\cdot K)}$, tolerance $10^{-6}\,\mathrm{W/(m\cdot K)}$. Failure sentinel: $\lambda_c = 0$.

**Code:** root finder [PocketPropellantSolver.cs:588-641](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L588-L641), residual [PocketPropellantSolver.cs:643-664](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L643-L664). ByDoubles: [PocketPropellantSolver.cs:946-1019](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L946-L1019).
**In PDF:** `ProblemContextReport` key `ConductiveThermalConductivity` (and `ConductiveThermalConductivityBalanceError`).

### 7.4 Effective conductivity

$$\lambda_{\text{eff}} \;=\; \lambda_r \;+\; \lambda_c$$

**Code:** [PocketPropellantSolver.cs:483](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L483), [PocketPropellantSolver.cs:855](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L855).
**Used by:** §10 (metal heat flux).

---

## 8. Kinetic flame system

The same closure applies to all three kinetic flames (`InterPocket`, `PocketSkeleton`, `PocketOutSkeleton`); only the parameter triple $(A_k, E_k, \nu)$ and the gas-phase properties $(T_f, M, \lambda_g)$ change.

### 8.1 Mean flame temperature

$$\bar T_k \;=\; \tfrac12\,(T_f + T_s)$$

**Code:** [BaseKineticPropellantSolver.cs:150-162](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BaseKineticPropellantSolver.cs#L150-L162), [BaseKineticPropellantSolver.cs:308-319](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BaseKineticPropellantSolver.cs#L308-L319).
**Inputs:** $T_f$ from the appropriate `KineticFlameParamsByUnits.FinalTemperature`; $T_s$ from §4.
**In PDF:** `ProblemContextReport` key `AverageKineticFlameTemperature`.

### 8.2 Mean flame density (ideal-gas)

$$\bar\rho_k \;=\; \frac{p \, M}{R\,\bar T_k}$$

**Code:** [BaseKineticPropellantSolver.cs:180-195](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BaseKineticPropellantSolver.cs#L180-L195), [BaseKineticPropellantSolver.cs:337-352](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BaseKineticPropellantSolver.cs#L337-L352).
**Inputs:** $p$ (JSON `pressure`), $M$ (gas-phase `average_molar_mass`), $R$.
**In PDF:** `ProblemContextReport` key `AverageKineticFlameDensity`.

### 8.3 Flame height (Arrhenius)

$$h_k \;=\; \frac{\dot m_d}{A_k \,\bar\rho_k^{\,\nu}\,\exp(-E_k / (R\,\bar T_k))}$$

**Code:** [BaseKineticPropellantSolver.cs:216-237](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BaseKineticPropellantSolver.cs#L216-L237), [BaseKineticPropellantSolver.cs:373-393](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BaseKineticPropellantSolver.cs#L373-L393).
**Inputs:** $\dot m_d$ (§5.1), $\bar\rho_k$ (§8.2), $\bar T_k$ (§8.1), $A_k$ from the appropriate `Frequency` slot of the 18-vector, $E_k$ from the matching `MolarEnergy` slot, $\nu$ from the matching reaction-order slot. The choice of which slot depends on the region; see the dispatcher table below.
**In PDF:** `ProblemContextReport` key `KineticFlameHeight`, `PressureTablesReport` rows `InterPocketKineticFlameHeight`, `SkeletonKineticFlameHeight`, `OutSkeletonKineticFlameHeight`.

### 8.4 Kinetic-flame heat flux (Fourier-like)

$$q_k \;=\; \lambda_g \,\frac{T_f - T_s}{h_k}$$

**Code:** [BaseKineticPropellantSolver.cs:102-134](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BaseKineticPropellantSolver.cs#L102-L134), [BaseKineticPropellantSolver.cs:262-292](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BaseKineticPropellantSolver.cs#L262-L292).
**In PDF:** `ProblemContextReport` key `KineticFlameHeatFlux`; `PressureTablesReport` rows `InterPocketKineticFlameHeatFlux`, `SkeletonKineticFlameHeatFlux`, `OutSkeletonKineticFlameHeatFlux`.

### 8.5 Region dispatcher

Each region implements `ExtractKineticBurnParams` to pick the triple from the 18-vector:

| Solver | $A_k$ slot | $E_k$ slot | $\nu$ slot | Code |
|---|---|---|---|---|
| `InterPocketPropellantSolver` | `AKineticFlameInterPocket` (vector[2]) | `EKineticFlameInterPocket` (vector[3]) | `NuInterPocket` (vector[8]) | [InterPocketPropellantSolver.cs:93-103](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/InterPocketPropellantSolver.cs#L93-L103) |
| `KineticSkeletonHelper` | `AKineticFlamePocketSkeleton` (vector[6]) | `EKineticFlamePocketSkeleton` (vector[7]) | `NuPocketSkeleton` (vector[10]) | [PocketPropellantSolver.cs:127-137](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L127-L137) |
| `KineticOutSkeletonHelper` | `AKineticFlamePocketOutSkeleton` (vector[4]) | `EKineticFlamePocketOutSkeleton` (vector[5]) | `NuPocketOutSkeleton` (vector[9]) | [PocketPropellantSolver.cs:297-307](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L297-L307) |

---

## 9. Diffusion flame

### 9.1 Diffusion flame height

$$h_{\text{diff}} \;=\; K_h \;\dot m_d \,d_{ox}^{\,2} \,\frac{c_{p,V}}{\lambda_g}$$

> **Reading the code carefully.** The C# uses `volumetricSpecificHeatCapacity.JoulesPerKilogramKelvin` even though the field stores a *volumetric* heat capacity in $\mathrm{J/(m^{3}\cdot K)}$. This is an UnitsNet accessor name only — the numerical value is the volumetric one (`c_volume` in JSON). See [§17](#17-known-limitations--open-questions).

**Code:** [PocketPropellantSolver.cs:701-720](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L701-L720), [PocketPropellantSolver.cs:1056-1074](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L1056-L1074).
**Inputs:** $K_h$ (vector[14]), $\dot m_d$ (§5.1), $d_{ox}$ from `PropellantParamsByUnits.AverageOxidizerDiameter` (JSON `components.AmmoniumPerchlorate.average_particles_diameter`), $c_{p,V}$ from `DiffusionFlameParamsByUnits.VolumetricSpecificHeatCapacity` (JSON `pocket_gas_phase.c_volume`), $\lambda_g$ from `DiffusionFlameParamsByUnits.ThermalConductivity` (JSON `pocket_gas_phase.lambda_gas`).
**In PDF:** `PressureTablesReport` row `DiffusionFlameHeight`.

### 9.2 Diffusion flame heat flux

$$q_{\text{diff}} \;=\; \lambda_g \,\frac{T_{f,\text{diff}} - T_s}{h_{\text{diff}}}$$

**Code:** [PocketPropellantSolver.cs:737-751](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L737-L751), [PocketPropellantSolver.cs:1092-1106](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L1092-L1106).
**Inputs:** $T_{f,\text{diff}}$ from `DiffusionFlameParamsByUnits.FinalTemperature` (JSON `pocket_gas_phase.T_diffusion_flame`); the rest as in §9.1.
**In PDF:** `PressureTablesReport` row `DiffusionFlameHeatFlux`.

---

## 10. Metal-combustion heat flux

Fourier conduction through the skeleton layer between the melting front $T_{melt}$ and the surface:

$$q_{\text{metal}} \;=\; \lambda_{\text{eff}}\,\frac{T_{melt} - T_s}{\delta_s}$$

**Code:** [PocketPropellantSolver.cs:666-681](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L666-L681), [PocketPropellantSolver.cs:1021-1035](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L1021-L1035).
**Inputs:** $T_{melt}$ from `MetalCombustionParamsByUnits.MetalMeltingTemperature` (hard-coded 2300 K), $\delta_s$ (§6.1), $\lambda_{\text{eff}}$ (§7.4).
**In PDF:** `PressureTablesReport` row `MetalHeatFlux`.

> **History.** The original implementation had `/ effectiveThermalConductivity` rather than `* effectiveThermalConductivity` — fixed in commit `9ca96ff`.

---

## 11. Surface energy balance

### 11.1 Pocket region (composite)

Components are weighted by the surface fractions of the skeleton ($f_s$) vs out-skeleton ($1 - f_s$); the diffusion flame contributes in full:

$$q_{\text{total}}^{(P)} \;=\; (1-f_s)\,q_k^{(OS)} \;+\; f_s\big(q_{\text{metal}} + q_k^{(S)}\big) \;+\; q_{\text{diff}}$$

**Code:** [PocketPropellantSolver.cs:495-504](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L495-L504), [PocketPropellantSolver.cs:867-876](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L867-L876).

### 11.2 Inter-pocket region (single kinetic flame only)

$$q_{\text{total}}^{(IP)} \;=\; q_k^{(IP)}$$

**Code:** [InterPocketPropellantSolver.cs:55-84](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/InterPocketPropellantSolver.cs#L55-L84), [InterPocketPropellantSolver.cs:148-178](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/InterPocketPropellantSolver.cs#L148-L178).

### 11.3 Sublimation / decomposition heat flux

$$q_{\text{sub}} \;=\; \dot m_d \,\Big(\,c_p\,(T_s - T_0) \;+\; \Delta H\,\Big)$$

**Code:** [PocketPropellantSolver.cs:506-511](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L506-L511), [PocketPropellantSolver.cs:878-883](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L878-L883), [InterPocketPropellantSolver.cs:76-82](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/InterPocketPropellantSolver.cs#L76-L82), [InterPocketPropellantSolver.cs:170-176](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/InterPocketPropellantSolver.cs#L170-L176).
**Inputs:** $c_p$ from `PropellantParamsByUnits.SpecificHeatCapacity` (JSON `specific_heat_capacity`), $T_0$ from `InitialTemperature` (JSON `initial_temperature`), $\Delta H$ (vector[13]).
**In PDF:** `ProblemContextReport` key `SublimationHeatFlux`.

### 11.4 Equilibrium condition (closes §4)

The root of the bisection in §4 is the $T_s$ for which $q_{\text{total}}(T_s) = q_{\text{sub}}(T_s)$ — separately in each region.

---

## 12. Region mixing

### 12.1 Pocket surface fraction (polynomial in pressure)

$f_s$ is the share of the burning surface covered by the skeleton sub-region. It is computed once per (propellant, pressure) cell as

$$f_s(p) \;=\; \frac{1}{\phi_{\text{pm}}} \sum_{i=0}^{N-1} c_i \,\Big(\frac{p}{10^{6}\,\text{Pa}}\Big)^{i}$$

where $c_i$ comes from JSON `pocket_surface_fraction_coefficients` and $\phi_{\text{pm}}$ from `pocket_mass_fraction`.

**Code:** [PropellantExtensions.cs:135-144](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L135-L144).

> **Naming gotcha.** The C# field is named `PropellantParamsByUnits.SkeletonSurfaceFraction`, but it stores the *pocket* surface fraction returned by `GetPocketSurfaceFraction(...)`. See [§17](#17-known-limitations--open-questions).

### 12.2 Mixed burn rate (volume-fraction harmonic mean)

$$v_b^{\text{mix}} \;=\; \frac{1}{\dfrac{f_{V,\text{inter}}}{v_b^{(IP)}} \;+\; \dfrac{f_{V,\text{pocket}}}{v_b^{(P)}}}$$

The mixed result is set only if both `InterPocket` and `Pocket` regions converged.

**Code:** [MixedPropellantSolver.cs:127-142](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/MixedPropellantSolver.cs#L127-L142), [MixedPropellantSolver.cs:157-172](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/MixedPropellantSolver.cs#L157-L172).
**Inputs:** $f_{V,\text{inter}}$ and $f_{V,\text{pocket}}$ from §3.2.

This is what the fitness function (§13) compares against the experimental Vieille's law $v_b^{exp}(p) = A\,p^{\nu}$.

---

## 13. Fitness function

For a single composition with $N_p$ propellants and $N_q$ pressure points:

$$F \;=\; \frac{1}{N_p}\sum_{i=1}^{N_p}\sqrt{\frac{1}{N_q}\sum_{j=1}^{N_q}\left(\frac{v_b^{\text{calc}}(i,j) - v_b^{exp}(i,j)}{v_b^{exp}(i,j)}\right)^{2}}$$

If any `MixedCombustionParams.BurnRateIsFound == false`, the function returns `double.MaxValue` and the genome is discarded.

**Code:** [FitnessFunctionEvaluator.cs:26-90](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/FitnessFunctionEvaluators/FitnessFunctionEvaluator.cs#L26-L90).
**In PDF:** `FitnessFunctionEvaluatorReport`, key `FitnessFunctionValue` ("Objective function value").

### 13.1 Penalty-augmented fitness

$$\tilde F \;=\; F \;+\; \sum_k P_k$$

with each $P_k$ a positive penalty computed by an evaluator (§14), accumulated over all (propellant, pressure) cells, and stored in `context.TotalEvaluatedPenalty`.

**Code:** [PenaltyFitnessFunctionEvaluator.cs:10-94](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/FitnessFunctionEvaluators/PenaltyFitnessFunctionEvaluator.cs#L10-L94).
**In PDF:** `FitnessFunctionEvaluatorReport`, key `TotalEvaluatedPenalty` ("Total penalty from constraint violations"); per-evaluator breakdown via `ConstraintPenaltyEvaluatorReport`.

### 13.2 Group fitness (joint optimization)

The group optimizer averages the three composition fitnesses and adds their pooled penalty:

$$\tilde F_{\text{group}} \;=\; \frac{1}{3}\sum_{g=0}^{2} F_g \;+\; \sum_{g=0}^{2} \sum_{k} P_{k,g}$$

**Code:** [GroupDifferentialEvolutionOptimizer.cs:149-179](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Optimizers/GroupDifferentialEvolutionOptimizer.cs#L149-L179).

---

## 14. Constraint penalties

All six penalty evaluators inherit from `BaseConstraintPenaltyEvaluator` (`PenaltyRate > 0` is enforced in the ctor: [BaseConstraintPenaltyEvaluator.cs:6-26](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/ConstraintPenaltyEvaluators/BaseConstraintPenaltyEvaluator.cs#L6-L26)). A penalty is added per (propellant, pressure) cell and accumulated by the penalty fitness evaluator.

| # | Class | Physical constraint | Penalty when violated | Code |
|---:|---|---|---|---|
| 1 | `InterPocketFasterBurnPenaltyEvaluator` | $v_b^{(IP)} \ge v_b^{(P)}$ — i.e. the first conditional fuel must burn at least as fast as the second | $P_1 = k_P \cdot v_b^{(P)} / v_b^{(IP)}$ if $v_b^{(P)} \ge v_b^{(IP)}$ | [InterPocketFasterBurnPenaltyEvaluator.cs:38-50](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/ConstraintPenaltyEvaluators/InterPocketFasterBurnPenaltyEvaluator.cs#L38-L50) |
| 2 | `PocketHeatFluxRatioCompetitionPenaltyEvaluator` | $\max/\min$ over $\{q_{\text{diff}}, q_{\text{metal}}, q_k^{(S)}, q_k^{(OS)}\} \le R^{\max}$ — flame-competition constraint | $P_2 = k_P \cdot (\max/\min)$ if ratio exceeds threshold | [PocketHeatFluxRatioCompetitionPenaltyEvaluator.cs:21-67](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/ConstraintPenaltyEvaluators/PocketHeatFluxRatioCompetitionPenaltyEvaluator.cs#L21-L67) |
| 3 | `KineticFlameHeatFluxPenaltyEvaluator` | Each of $q_k^{(IP)}$, $q_k^{(S)}$, $q_k^{(OS)}$ must not exceed its prescribed maximum | $P_3 = k_P \sum_{r \in \{IP,S,OS\}} (q_k^{(r)} / q_{\max}^{(r)})$ over violating zones | [KineticFlameHeatFluxPenaltyEvaluator.cs:65-131](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/ConstraintPenaltyEvaluators/KineticFlameHeatFluxPenaltyEvaluator.cs#L65-L131) |
| 4 | `PoreDiameterPenaltyEvaluator` | $\delta_s \ge \alpha\,d_p$ — skeleton layer must be thicker than a multiple of pore diameter | $P_4 = k_P \cdot \alpha\,d_p / \delta_s$ if $\delta_s < \alpha\,d_p$ | [PoreDiameterPenaltyEvaluator.cs:21-45](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/ConstraintPenaltyEvaluators/PoreDiameterPenaltyEvaluator.cs#L21-L45) |
| 5 | `LargeOxidizerParticleSizePenaltyEvaluator` | $\delta_s \le \alpha\,d_{ox}$ — skeleton layer must be thinner than a multiple of the large AP particle diameter | $P_5 = k_P \cdot \delta_s / (\alpha\,d_{ox})$ if violated | [LargeOxidizerParticleSizePenaltyEvaluator.cs:21-45](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/ConstraintPenaltyEvaluators/LargeOxidizerParticleSizePenaltyEvaluator.cs#L21-L45) |
| 6 | `RadiativeThermalConductivityPenaltyEvaluator` | $\lambda_r \ge \lambda_c$ — radiative pathway must dominate conduction | $P_6 = k_P \cdot \lambda_c / \lambda_r$ if $\lambda_r < \lambda_c$ | [RadiativeThermalConductivityPenaltyEvaluator.cs:13-34](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/ConstraintPenaltyEvaluators/RadiativeThermalConductivityPenaltyEvaluator.cs#L13-L34) |

$k_P$ is the per-evaluator `PenaltyRate` (a positive scalar constructor argument). $\alpha$ is the threshold scaling factor; in the current configuration $\alpha = 3$ (pore) and $\alpha = 1$ (large particle), set in [Program.cs:111-132](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L111-L132). Note that `LargeOxidizerParticleSizePenaltyEvaluator` is currently commented out of the active penalty set.

**In PDF:** all six rendered by `ConstraintPenaltyEvaluatorReport` under header `HeaderOfConstraintPenaltyEvaluators` ("Functional Constraints"); each evaluator prints its `PenaltyRate` plus its threshold values (`HeatFluxRatioThreshold`, `MaxInterPocketKineticFlameHeatFlux`, …).

---

## 15. Differential evolution optimizer

The optimization layer (`GroupDifferentialEvolutionOptimizer`) wraps the external library `DotNetDifferentialEvolution` with a fitness function that converts the 32-element genome to three 18-element vectors, runs the solver on each composition, and aggregates the result.

| Setting | Source | Bounds & semantics |
|---|---|---|
| `PopulationSize` | `Builder.WithPopulationSize` | positive int; current value $32 \cdot 8 = 256$ ([Program.cs:137](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L137)) |
| `MutationForce` $F$ | `Builder.WithMutationForce` | $\in [0,2]$; current `0.7` |
| `CrossoverProbability` $C_r$ | `Builder.WithCrossoverProbability` | $\in [0,1]$; current `0.9` |
| `ProcessorsCount` | `Builder.WithProcessorsCount` | positive int; chosen so it divides population size |
| `TerminationStrategy` | `Builder.WithTerminationStrategy` | `TimeoutTerminationStrategy` (9 h default) or `CustomStagnationStreakTerminationStrategy` |
| `LowerBound` / `UpperBound` | `Builder.WithLowerBound/Upper` | length must equal vector dimension (32 for group, 18 for single); validated in `Builder.Build` |

**Settings record:** [DifferentialEvolutionSettings.cs:7-198](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Settings/DifferentialEvolutionSettings.cs#L7-L198).
**Group optimizer:** [GroupDifferentialEvolutionOptimizer.cs:96-179](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Optimizers/GroupDifferentialEvolutionOptimizer.cs#L96-L179).

**Evaluate flow** (one DE function evaluation):
1. Wrap the 32-vector genome with `GroupCombustionSolverParamsByDoubles.FromVector`.
2. For each `groupIdx ∈ {0,1,2}` call `ToCompositionVector(groupIdx)` → 18-vector → `CombustionSolverParamsByDoubles.FromVector` → `_fitnessFunctionEvaluator.Visit(...)`.
3. Sum `FitnessFunctionValue + TotalEvaluatedPenalty` across all three; return $(F_0+F_1+F_2)/3 + \sum_g P_g$.
4. If any group's fitness is `double.MaxValue`, short-circuit to `double.MaxValue`.

**Final result.** After the DE loop terminates, the best 32-vector is wrapped as `GroupCombustionSolverParamsByUnits` and each composition is re-evaluated with UnitsNet types into a `OptimizationProblemByUnits`. These three contexts plus the best 32-vector are returned as `GroupOptimizationResult` ([GroupOptimizationResult.cs:9-83](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Results/GroupOptimizationResult.cs#L9-L83)). The adapter `GroupOptimizationResultAdapter` then merges the three context matrices into a single 5-propellant matrix in the order $\{B_2, B_3, B_4, B_1, B_0\}$ for PDF reporting.

**In PDF:** `DifferentialEvolutionSettingsReport` (header `Differential Evolution Algorithm`) prints population size, mutation force, crossover probability, threads/processors count, and termination strategy.

---

## 16. Output mapping (formula ↔ PDF section ↔ JSON field)

### 16.1 PDF section index

| PDF section (en-US label) | Class | Drives |
|---|---|---|
| Optimization Parameters | `CombustionSolverParamsReport` | the 18 best-fit values with their `[LB; UB]` bounds (one row per slot) |
| Optimization Results | `FitnessFunctionEvaluatorReport` | $F$, $\sum P_k$ (§13) |
| Functional Constraints | `ConstraintPenaltyEvaluatorReport` | active penalties + thresholds (§14) |
| Differential Evolution Algorithm | `DifferentialEvolutionSettingsReport` | DE settings (§15) |
| Parametric Constraints | `ParametricConstraintReport` | $T_s^{\min}$, $T_s^{\max}$ used in §4 |
| Propellant Composition Parameters | `PropellantReport` | per-propellant: density, $c_p$, $T_0$, Vieille's $A,\nu$, four gas-phase blocks (`InterPocket`, `Pocket`, `Skeleton`, `OutSkeleton`) with $\lambda_g$, $M$, $c_{p,V}$, $T_f$ |
| Propellant Composition "X" | `ProblemContextReport` | per-(propellant, pressure) snapshot of every quantity in §4–§11 |
| Per-pressure tables | `PressureTablesReport` | columns (inter-pocket \| pocket) × rows (burn rate, $T_s$, kinetic-flame heat fluxes & heights, diffusion-flame, metal flux) for each selected pressure |
| Performance Report | `PerformanceMeterReport` / `GroupPerformanceMeterReport` | system info + per-frame timing — not part of the math model |

### 16.2 Symbol ↔ code ↔ resource ↔ JSON

| Symbol | Code field | PDF resource key | JSON field |
|---|---|---|---|
| $T_s$ | `*.SurfaceTemperature` | `SurfaceTemperature` | computed (§4) |
| $\dot m_d$ | `*.DecomposeRate` | `DecomposeRate` | computed (§5.1) |
| $v_b$ | `*.BurnRate` | `LinearBurnRate`, `CalculatedBurningRate` | computed (§5.2) |
| $v_b^{exp}$ | `ExperimentalBurnRates[i,j]` | `ExperimentalBurnRate` | $A\,p^{\nu}$ from `Propellant.a`, `Propellant.nu` |
| $T_f$ kinetic flames | `KineticFlameParamsByUnits.FinalTemperature` | `KineticFlameTemperature` | `*_gas_phase.T_kinetic_flame` |
| $T_{f,\text{diff}}$ | `DiffusionFlameParamsByUnits.FinalTemperature` | `DiffusionFlameTemperature` | `pocket_gas_phase.T_diffusion_flame` |
| $\lambda_g$ | `*.ThermalConductivity` | `LamdaGas` | `*_gas_phase.lambda_gas` |
| $M$ | `*.AverageMolarMass` | `AverageMolarMass` | `*_gas_phase.average_molar_mass` |
| $c_{p,V}$ | `*.VolumetricSpecificHeatCapacity` | `SpecificHeatCapacity_Volume` | `*_gas_phase.c_volume` |
| $\varphi$ | `SkeletonLayerParamsByUnits.Porosity` | (in `ProblemContextReport`) | `PressureFrame.porosity_within_skeleton` |
| $\delta_s$ | `PocketCombustionParams.SkeletonLayerThickness` | `SkeletonLayerThickness` | computed (§6.1) |
| $d_p$ | `PocketCombustionParams.PoreDiameter` | `PoreDiameter` | computed (§6.2) |
| $\lambda_r$ | `PocketCombustionParams.RadiativeThermalConductivity` | `RadiativeThermalConductivity` | computed (§7.2) |
| $\lambda_c$ | `PocketCombustionParams.ConductiveThermalConductivity` | `ConductiveThermalConductivity` | computed (§7.3) |
| $\lambda_{\text{eff}}$ | `PocketCombustionParams.EffectiveThermalConductivity` | (derived) | $\lambda_r + \lambda_c$ |
| $q_{\text{metal}}$ | `PocketCombustionParams.MetalBurningHeatFlux` | `MetalHeatFlux` | computed (§10) |
| $q_k^{(\cdot)}$ | `*KineticFlameCombustionParams.KineticFlameHeatFlux` | `*KineticFlameHeatFlux` | computed (§8.4) |
| $h_k^{(\cdot)}$ | `*KineticFlameCombustionParams.KineticFlameHeight` | `*KineticFlameHeight` | computed (§8.3) |
| $q_{\text{diff}}$ | `PocketCombustionParams.DiffusionFlameHeatFlux` | `DiffusionFlameHeatFlux` | computed (§9.2) |
| $h_{\text{diff}}$ | `PocketCombustionParams.DiffusionFlameHeight` | `DiffusionFlameHeight` | computed (§9.1) |
| $q_{\text{sub}}$ | `*CombustionParams.SublimationHeatFlux` | `SublimationHeatFlux` | computed (§11.3) |
| $f_s$ | `PropellantParamsByUnits.SkeletonSurfaceFraction` | (derived) | `pocket_surface_fraction_coefficients` / `pocket_mass_fraction` |
| $T_{melt}$ | `MetalCombustionParamsByUnits.MetalMeltingTemperature` | (derived, in `ProblemContextReport`) | hard-coded 2300 K |

---

## 17. Known limitations & open questions

This section is the audit log for the model. New issues should be appended here as they surface, so future model improvements have a single check-list to work against.

1. **Empirical Bas_2 correction.** `GetInterPocketAreaVolumeFraction` multiplies the inter-pocket volume fraction by `1.5` when `propellant.Name == "Bas_2"` ([PropellantExtensions.cs:53-55](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L53-L55)). The reason is not documented. Any plan to expand the model beyond the current Bas_0..4 family needs to revisit this factor.

2. **`SkeletonSurfaceFraction` actually stores the *pocket* surface fraction** ([ProblemContextBy…MatrixBuilder.cs:135](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Builders/ProblemContextByUnitsMatrixBuilder.cs#L135)). The field name and the `pocket_surface_fraction_coefficients` JSON name reflect the original intent; the rename never reached `PropellantParamsByUnits`. Renaming to `PocketSurfaceFraction` is a cosmetic fix worth doing.

3. **`KCoefficientRadiationTemperature` is a free parameter with a fixed structural role**, not derived from physics. The current bounds are `[0, 0]` (lock-out, [Program.cs:55-60](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L55-L60)), so $\bar T_r = T_f^{(S)}$ in every DE evaluation. If the bounds are relaxed, document the chosen physical justification before rerunning campaigns.

4. **Pore radiative β closure is heuristic.** $\beta = 3(1-\varphi)/d_p$ assumes geometric-optics scattering by opaque spherical particles. For high-porosity skeletons ($\varphi \to 1$) the Rosseland regime breaks down before the formula's singularity is reached.

5. **Conductive conductivity uses the Bruggeman symmetric closure** (§7.3). This is a closure, not a derivation; for very contrasted $\lambda_g$ vs $\lambda_c^{(s)}$ ratios it can deviate from measured values by a factor of a few.

6. **Kinetic-flame `Nu` reaction orders are tuned as free parameters** ($\nu \in \mathbb{R}$, not restricted to $\{1, 1.5, 2\}$). With $\nu \to 0$ the flame height becomes independent of density, which is unphysical; with $\nu \gg 1$ the flame becomes infinitely thin and $q_k$ diverges. Bounds in [Program.cs:53-61](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L53-L61) limit this.

7. **`VolumetricSpecificHeatCapacity` accessor name (`JoulesPerKilogramKelvin`) is misleading.** In `DiffusionFlameParamsByUnits` and `KineticFlameParamsByUnits` the field type is `SpecificEntropy`, but the numeric value stored is volumetric $\mathrm{J/(m^{3}\cdot K)}$ as per JSON `c_volume`. The code retrieves it as `.JoulesPerKilogramKelvin` because UnitsNet does not distinguish those slots. Anyone reading the diffusion-flame-height expression ([PocketPropellantSolver.cs:713-714](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/PocketPropellantSolver.cs#L713-L714)) must keep this in mind.

8. **ByUnits / ByDoubles drift risk.** Every formula in §5–§11 has two mirror implementations. The current code is consistent (verified by reading both paths during the authoring of this doc), but the only safeguard is human discipline — a single integration test that compares ByUnits vs ByDoubles output for a fixed input would catch any future drift before it lands in a release.

9. **Rosseland 1/3 prefactor** — recently fixed (commit `9ca96ff`, 2026-05-27). Optimization results obtained before this commit must be regenerated; the previous code overestimated $\lambda_r$ by a factor of 3, which feeds into $\lambda_{\text{eff}}$ and $q_{\text{metal}}$ and therefore through the surface heat balance.

10. **Metal heat flux sign flip** — also fixed in `9ca96ff`: the previous code divided by $\lambda_{\text{eff}}$ instead of multiplying. Same caveat as point 9.

11. **`MetalMeltingTemperature` is hard-coded to 2300 K** in [PropellantExtensions.cs:155-159](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L155-L159), ignoring any pressure dependence and any composition difference. Plausible for aluminum-skeleton propellants, but should be made explicit if the metal binder changes.

12. **`MetalBoilingTemperature(p)` polynomial coefficients in [PropellantExtensions.cs:177-186](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Extensions/PropellantExtensions.cs#L177-L186) are hard-coded without provenance.** They should be documented (source publication, fit window) or replaced by JSON-driven coefficients.

13. **Surface-temperature bounds [600, 750] K are hard-coded** as default field values in `ProblemContextByUnits` and `ProblemContextByDoubles`. For high-pressure / high-flame-temperature points these bounds may be too narrow, producing the `−1 K` failure sentinel; the failure then collapses the genome's fitness to `MaxValue` and the DE silently rejects it. Worth widening or making JSON-configurable.

14. **`PressureTablesReport`'s `.en-US.resx` file has duplicate / placeholder entries** at the top of the file (`Name1`, `Color1`, `Bitmap1`, etc., visible in `grep` output) — these are Visual-Studio resource-editor scaffolding leftovers and do not appear in the rendered PDF. Cosmetic.

---

## Verification recipes (for future maintainers)

When you change a formula in §5–§12, verify against this doc:

1. **Spot-check the cited line range.** `git blame` the file, then re-open the `[file.cs:Lxx-Lyy]` link in the relevant entry. If the line numbers shifted, update them — the linked range should still contain the same formula.
2. **Cross-check the PDF mapping.** Run `dotnet run --project src/dotnet/Apps/src/PastyPropellant.ConsoleApp` (3-minute termination is enough for a smoke test, see commit `268cbee` for the timeout knob), then open `propellants.group_optimization.report.en.pdf`. Every resource key cited in §16 must still appear in the PDF and label the correct number.
3. **Cross-check the JSON mapping.** Open `data/propellants.01234.json` and confirm that each JSON path cited in §3 and §16 still exists and feeds the expected quantity.
4. **Group-vector round-trip.** Run the optimizer with `compositionIndex ∈ {0,1,2}` and confirm that `ToCompositionVector(idx)` produces a vector whose components match §3.5 in slot and units. The simplest check: with `KCoefficientRadiationTemperature = 0` the radiative temperature should equal `T_f^{(S)}` exactly.
5. **ByUnits vs ByDoubles parity.** Pick any (propellant, pressure, parameter-vector) triple and run both `ProblemContextByUnits` and `ProblemContextByDoubles` evaluators; every shared quantity ($v_b$, $T_s$, all heat fluxes) should match to machine precision.
