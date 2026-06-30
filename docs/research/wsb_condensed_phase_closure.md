# WSB condensed-phase closure and the high-pressure burn-rate saturation

**Scope.** This note documents why the BDP-style competing-flames model in this repository
**saturates** the burning rate above ~4 MPa for the AP-containing group compositions, and
specifies one physically grounded, implementable fix: a Ward–Son–Brewster (WSB) condensed-phase
reaction term in the surface energy balance, gated by a second-order (p²) Damköhler completeness.
It adds exactly one new **shared** tunable coefficient (no per-composition parameters), reduces
exactly to the existing model at the limit, and is structurally safe for the surface-temperature
bracketing root.

---

## 1. The puzzle

After the diffusion-standoff campaign (Steps 1–5: `ap_size_pressure_exponent.md`), the aggregated
objective sits at 0.0231 with the entire residual in the Bas_2/3/4 group (0.065). A curvature
analysis (segment slopes 1→4.06 MPa and 4.06→6.5 MPa) shows the residual is **not** a slope-spread
but a **high-pressure saturation** common to all three group compositions:

| ν (model) | 1→4.06 MPa | 4.06→6.5 MPa | experiment (clean power law) |
|---|---|---|---|
| Bas_2 (mixed)       | 0.585 | **0.290** | 0.52 / 0.52 |
| Bas_3 (fine only)   | 0.634 | **0.225** | 0.64 / 0.64 |
| Bas_4 (coarse only) | 0.633 | 0.580 | 0.70 / 0.70 |

Below 4 MPa the model tracks the data; above 4 MPa the calculated rate flattens. The worst single
point is Bas_3 (pure fine) at 6.5 MPa: −19 %.

## 2. Mechanism: surface-attached flames saturate the heat feedback

The kinetic-flame heat flux in `BaseKineticPropellantSolver` is

```
q_k = λ_g · (T_f − T_s) / h_flame ,   h_flame = ṁ / ( A_k · ρ_g^ν · exp(−E_k/RT̄) )
```

with `ρ_g ∝ p`. As pressure rises the flame height collapses (for Bas_3 the skeleton kinetic flame
height falls to **1.85 µm at 6.5 MPa**). Once a flame is essentially **surface-attached**
(`h_flame → 0`), ~100 % of its heat already reaches the surface, so `dq/dp → 0`: further pressure
increase cannot raise the delivered heat. With the burning rate set by the surface energy balance

```
q_gas(T_s, p) = ṁ(T_s) · [ c_s·(T_s − T_0) + ΔH ] ,   ṁ = A·exp(−E/RT_s)         (current model)
```

a saturating `q_gas` produces a saturating `T_s`, hence a saturating `ṁ`. This is **correct physics
for an inert condensed phase**, but real AP propellants keep accelerating because the **condensed /
near-surface reaction layer** takes over the energy supply at high pressure — precisely the term
the current closure omits (the condensed phase here is a bare zeroth-order Arrhenius pyrolysis, no
reaction heat).

## 3. WSB closure

Ward, Son & Brewster (1998) model the condensed phase as an exothermic reacting layer and the gas
phase as a single second-order (bimolecular) reaction. Two results matter here:

1. The condensed-phase exothermic reaction releases heat **Q_c** *within* the condensed phase,
   reducing the net surface heat the gas must supply. The surface energy balance becomes
   `q_gas = ṁ·[c_s(T_s − T_0) + ΔH − Q_c·θ]`, i.e. a reacting condensed phase **pre-supplies** part
   of the sensible enthalpy.
2. The pressure sensitivity enters through a **second-order Damköhler number** `Da ∝ p²/ṁ²`
   (ratio of reaction rate to through-flow); the burn-rate eigenvalue asymptotes to ν ≈ 0.8 with
   dν/dp < 0. The completeness of the condensed-phase reaction rises toward unity as `Da → ∞`.

These are corroborated by Zenin's (1995) embedded-thermocouple measurements of AP/HMX condensed-phase
heat release and surface temperatures, and by the Brewster–Son quasi-steady framework.

## 4. Implemented form (one shared parameter, reduces to baseline)

The completeness of the condensed-phase reaction is written as a saturating function of the
second-order Damköhler number, anchored on pressure:

```
θ(p) = Da / (1 + Da) ,    Da = K_wsb · (p / p_ref)² ,    p_ref = 1 MPa  (fixed, low-pressure anchor)
```

and folded into the **sensible** part of the surface heat demand (always positive, so the bracketing
root is preserved):

```
q_sink = ṁ · [ c_s·(T_s − T_0)·(1 − θ) + ΔH ]
```

- **K_wsb ≥ 0** is the single new **shared** coefficient (group vector index 36, single-composition
  index 22), bounds **[0, 5]**. **K_wsb = 0 ⇒ θ ≡ 0 ⇒ the original surface energy balance exactly**
  (cannot regress the already-fit Bas_0/Bas_1).
- As `p` rises, `Da ∝ p²` grows and `θ → 1`, lowering the sensible sink so the energy balance crosses
  at a **higher T_s** ⇒ higher ṁ ⇒ the burning rate keeps climbing past flame surface-attachment.
  This **de-saturates the 4–6.5 MPa tail**, where the residual lives.
- `θ < 1` always, so `c_s(T_s − T_0)·(1 − θ) > 0`: the sink stays positive, the surface-temperature
  binary-search root is preserved (no `−1 K` sentinel / fitness blow-up).

**Design note — pressure anchor vs. ṁ anchor.** WSB's *natural* Damköhler is `Da ∝ p²/ṁ²`, which
gives *higher* completeness to *slower* (lower-ṁ) propellants — i.e. it would preferentially lift the
coarse Bas_4, whose deficit is the **smallest**. We therefore anchor the Damköhler on pressure,
`Da = K_wsb·(p/p_ref)²`, leaving each composition's own ṁ-response to set the magnitude. This puts the
headroom where the high-pressure saturation is worst (fine Bas_3) while keeping a single shared
coefficient and the second-order (p²) WSB pressure scaling. `p_ref = 1 MPa` is the data's low-pressure
anchor — no arbitrary mid-range pivot — and the `Da/(1+Da)` sigmoid keeps the activation concentrated
at high pressure for small K_wsb.

## 5. Code map

| Element | Location |
|---|---|
| Completeness helper `θ(p)` | `PocketPropellantSolver.GetWsbCondensedCompleteness` (shared static; both paths) |
| Energy-balance use (ByUnits) | `PocketPropellantSolver.GetSurfaceHeatFluxesError` (`(1 − θ)` on the sensible term) |
| Energy-balance use (ByDoubles) | `PocketPropellantSolver.GetSurfaceHeatFluxesError` (double path; identical numbers — §17.8 parity) |
| Parameter `K_wsb` | `CombustionSolverParams{ByDoubles,ByUnits}.KCondensedReactionFactor` (vector[22]) |
| Group plumbing | `GroupCombustionSolverParams.*` (vector[36]; `ToCompositionVector` → index 22) |
| Bounds | `Program.GetLowerBound/GetUpperBound` [22] = [0, 5]; group [36] |

## 6. Success criterion

Model ν over 4.06→6.5 MPa for Bas_3 rises from 0.225 toward ~0.64 (saturation removed); group RMS
falls below ~0.025 and aggregated objective below 0.01, with penalty 0 and no `BurnRateIsFound = false`
(no `−1 K`). If K_wsb rails at 5 and the tail is still flat, the reserve lever is to let the gas-phase
reaction order (pocket out-skeleton) carry the AP-monopropellant exponent for the fine-dominated
compositions.

## References

- M. W. Beckstead, R. L. Derr, C. F. Price, *A model of composite solid-propellant combustion based
  on multiple flames*, AIAA J. **8**(12):2200–2207, 1970. doi:10.2514/3.6087.
- M. Q. Brewster, S. F. Son, *Quasi-steady combustion modeling of homogeneous solid propellants*,
  Combust. Flame, 1995.
- M. J. Ward, S. F. Son, M. Q. Brewster, *Steady deflagration of HMX with simple kinetics: a gas
  phase chain reaction model*, Combust. Flame **114**(4):556–568, 1998. doi:10.1016/S0010-2180(97)00332-5.
- A. Zenin, *HMX and RDX: combustion mechanism and influence on modern double-base propellant
  combustion*, J. Propul. Power **11**(4):752–758, 1995. doi:10.2514/3.23900.
- G. Lengellé, J. Duterque, J. F. Trubert, *Combustion of solid propellants*, RTO/AGARD, 2000.
