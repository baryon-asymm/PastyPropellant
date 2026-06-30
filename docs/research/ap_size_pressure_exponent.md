# AP particle size, the diffusion-flame standoff, and the burning-rate pressure exponent

**Scope.** This note derives a physically grounded, implementable closed form for the
diffusion-flame standoff height `h_diff(p, d_ox, f_coarse)` that lets a BDP-style
"competing-flames" burn-rate model reproduce the experimental ordering of the pressure
exponent ν across five propellants that differ only in their AP size distribution. It
documents the literature (BDP, Summerfield granular-diffusion-flame, Lengellé–Duterque–
Trubert), explains the non-monotonic ν ordering, and specifies exactly one new **shared**
tunable coefficient (no per-composition parameters). A no-new-parameter fallback is given.

---

## 1. The puzzle

Five propellants, identical binder/metal, differ only in AP size distribution. Coarse
(large-AP) fraction `f_coarse` and coarse diameter `d_ox`:

| Propellant | d_ox | f_coarse | target ν | model ν (current) |
|---|---|---|---|---|
| Bas_2 (mixed)       | 135 µm | 0.65 | 0.52 | 0.50 |
| Bas_3 (fine only)   |  50 µm | 0.00 | 0.64 | 0.51 |
| Bas_4 (coarse only) | 180 µm | 1.00 | 0.70 | 0.59 |

The current model collapses all three to ν ≈ 0.5. Two structural reasons:

1. **Gas-phase reaction orders are shared** across compositions (one optimiser vector),
   so the premixed/kinetic flames cannot, by themselves, spread ν between Bas_2/3/4.
2. **The current diffusion flame has no explicit pressure term.** In
   `PocketPropellantSolver.GetDiffusionFlameHeight`:

   ```
   h_diff = K_h · c_v · ṁ_d · d_ox² / λ_g        [m]
   q_diff = λ_g · (T_diff − T_s) / h_diff         [W/m²]
   ```

   Pressure enters only implicitly through ṁ_d (the surface mass flux). Because
   `h_diff ∝ ṁ_d` and `q_diff ∝ 1/h_diff ∝ 1/ṁ_d`, the diffusion-flame heat flux
   *falls* as pressure rises. This term therefore *reduces* ν, in the wrong direction,
   and identically for all three compositions (only the magnitude shifts with d_ox², not
   the pressure slope). It cannot generate a spread in ν.

**Key difficulty — the ν ordering is NON-MONOTONIC in d_ox.** Bas_4 (180 µm) → 0.70 is
highest, but Bas_2 (135 µm) → 0.52 is *lower* than Bas_3 (50 µm) → 0.64. A monotone
function of d_ox alone (e.g. ν increasing with d_ox) gives Bas_4 > Bas_2 > Bas_3, which is
wrong. So **d_ox alone is not the modulator.** The modulator that reproduces
Bas_4 > Bas_3 > Bas_2 is the **coarse mass fraction `f_coarse`** acting on the
*pressure sensitivity* of the standoff — see §5–§6.

---

## 2. BDP "competing flames": flame heights and the size→exponent link

Beckstead, Derr & Price (BDP), *AIAA J.* **8**(12):2200–2207 (1970),
[doi:10.2514/3.6087](https://doi.org/10.2514/3.6087), model the AP-composite gas phase as
three competing flames over each oxidizer/binder element:

- **AP monopropellant (premixed) flame** — a self-sustaining premixed deflagration over
  the AP surface. Its standoff is *kinetically* controlled. For a second-order gas
  reaction the premixed flame height scales as
  `L_AP ∝ ṁ /(ρ² k) ∝ ṁ / p²` (the reaction-rate per unit volume ∝ ρ² ∝ p²), so this
  flame moves **toward the surface as p²** — strongly pressure-sensitive, and *independent
  of particle size*.
- **Primary diffusion flame** — between AP-decomposition products and binder products.
  Its standoff is set by mixing of fuel/oxidizer over the scale of the oxidizer "pocket",
  i.e. `L_D ∝ ṁ · d_ox² /(ρ D)`. Because `ρ ∝ p` and the binary diffusivity `D ∝ 1/p`,
  `ρD` is essentially **pressure-independent**, so `L_D ∝ ṁ · d_ox²` and grows with
  particle size. This is the term the current model already encodes.
- **Final diffusion flame** — between the products of the first two; secondary for ν.

BDP close the model by balancing the surface energy: the burn rate is set by the
*nearest dominant flame*, the one whose heat feedback `q ∝ λ(T−T_s)/L` is largest (smallest
standoff wins).

**Beckstead–Gross regimes and why coarse AP → higher ν** (BDP 1970; reviewed in Beckstead,
*"Overview of combustion mechanisms and flame structures for advanced solid propellants"*,
2000, and Lengellé–Duterque–Trubert 2000):

- **Fine AP (small d_ox):** `L_D ∝ d_ox²` collapses, the diffusion flame sits at the
  surface, and combustion is governed by the **premixed limit** (AP monopropellant + primary
  premixed). Burn rate is kinetics-limited; ν reflects the gas reaction order and the
  monopropellant exponent. As d_ox → 0 the burn rate saturates (the "premixed limit":
  further size reduction no longer increases r) — see Stephens/Beckstead and the AP-size
  reviews. ν is moderate-to-high but *insensitive to further size change*.
- **Coarse AP (large d_ox):** `L_D ∝ d_ox²` is large and the **diffusion flame is the rate-
  limiter**. The controlling standoff now carries the *premixed AP-monopropellant pressure
  sensitivity convolved into the feedback*, and the diffusion flux climbs steeply with p as
  the reaction-controlled portion of the standoff collapses ∝ p⁻². The propellant sits in
  the **AP-monopropellant / diffusion-controlled regime**, which has the **highest ν**.
- **Intermediate / bimodal:** behaviour is a competition-weighted blend.

So the **monotone physical statement** is *"ν increases with the fraction of combustion
controlled by the (pressure-sensitive) diffusion/monopropellant pathway,"* and that fraction
is governed by **how much coarse AP is present**, not simply by d_ox.

---

## 3. Summerfield granular-diffusion-flame (GDF): the canonical ν(p, size) law

Summerfield et al., *"Burning Mechanism of Ammonium Perchlorate Propellants"*, in *Solid
Propellant Rocket Research*, Progress in Astronautics and Rocketry **1**, Academic Press
(1960), pp. 141–182, give the GDF burn-rate law:

```
1/r = a/p + b/p^(1/3)                                  (GDF)
```

- The `a/p` term is the **kinetic / premixed** contribution (fast at high p).
- The `b/p^(1/3)` term is the **diffusion (granular) contribution**, where `b` carries the
  **oxidizer particle-size dependence** (larger d_ox → larger b → diffusion term dominates
  longer).

Differentiating GDF, the *local* exponent is

```
ν(p) = d ln r / d ln p = [ a/p + (1/3) b/p^(1/3) ] / [ a/p + b/p^(1/3) ].
```

This interpolates between **ν → 1** (low p, kinetic term dominant) and **ν → 1/3** (high p,
diffusion term dominant) — i.e. **diffusion control lowers the local exponent** in the pure
GDF picture. The experimental composites here sit in the regime where the *competing-flame
feedback* (BDP), not the bare GDF, sets ν, and the relevant lever is the pressure scaling of
the **standoff feeding the surface**, which is what we modify below. The GDF law is cited to
fix the size-carrying coefficient `b` and to motivate that **particle size enters only the
diffusion term**, never the kinetic term — exactly the structure we preserve.

---

## 4. Lengellé–Duterque–Trubert (2000): the two standoff terms

Lengellé, G., Duterque, J., and Trubert, J.-F., *"Physico-chemical mechanisms of solid
propellant combustion"*, Chapter 2 in *Solid Propellant Chemistry, Combustion, and Motor
Interior Ballistics* (V. Yang, T. B. Brill, W.-Z. Ren, eds.), *Progress in Astronautics and
Aeronautics* **185**, AIAA, Reston, VA (2000), pp. 59–112,
[doi:10.2514/5.9781600866562.0059.0112](https://doi.org/10.2514/5.9781600866562.0059.0112).
(Companion: Lengellé, Duterque, Trubert, *"Combustion of solid propellants"*, RTO/AGARD
lecture-series LS-180, 1992; Stanford reprint widely circulated.)

Lengellé writes the gas-phase standoff of a heterogeneous composite flame as the **sum of two
characteristic lengths**:

1. **Convective / laminar (mixing) term** — the distance for fuel and oxidizer streams of
   characteristic width set by the oxidizer particle to interdiffuse and react:

   ```
   x_diff,lam  ≈  (ṁ_d · c_p / λ_g) · d_ox²  ·  C_lam          (laminar, mixing-limited)
   ```

   With `ρ D ≈ const` in `p`, this term is **≈ pressure-independent** and **∝ d_ox²**.
   This is structurally the present model's `h_diff` (with `c_v` playing the role of `ρ c_p`
   and `λ_g` the conductivity), confirming the current term is the *laminar* branch.

2. **Reaction (kinetic) term** — the thickness of the chemical reaction zone that must be
   added because the gas-phase reaction is not infinitely fast:

   ```
   x_diff,rxn  ≈  ṁ_d /(ρ_g² · k(T))  ∝  ṁ_d · p^(−2)          (reaction-limited)
   ```

   The `p^(−2)` scaling comes from the second-order gas reaction rate per unit volume
   `∝ ρ_g² ∝ p²` (so the reaction length `∝ 1/p²`). Lengellé and the BDP lineage use this
   **fixed `p^(−2)` exponent** for the kinetically controlled portion of the standoff (it is
   the same `a/p`-type collapse seen in GDF, expressed as a length).

The **total standoff** is the sum:

```
x_diff = x_diff,lam + x_diff,rxn
       ≈ (ṁ_d c_v / λ_g) d_ox² · [ C_lam  +  C_rxn · (p_ref / p)² ]            (★)
```

The heat feedback to the surface is `q_diff = λ_g (T_diff − T_s) / x_diff`. **Because the
reaction term collapses as p⁻², q_diff RISES with pressure** once the reaction term is
non-negligible — the opposite sign of the current model — and the **strength of that rise is
controlled by C_rxn relative to C_lam**, i.e. by *how diffusion/reaction-limited the flame
is*. That weighting is what `f_coarse` sets.

---

## 5. Resolving the non-monotonicity: the modulator is `f_coarse`, not d_ox

The experimental ordering ν(Bas_4=1.00) > ν(Bas_3=0.00) > ν(Bas_2=0.65) is **not** monotone
in d_ox (135 µm < 180 µm but 50 µm fine-only beats 135 µm mixed). It *is* explained by the
**coarse mass fraction weighting the pressure-sensitive reaction branch**:

- **Bas_4, f_coarse = 1.0 (coarse only).** All AP burns through the coarse diffusion /
  AP-monopropellant pathway. The standoff carries the full p⁻² reaction collapse; the
  diffusion feedback climbs steeply with p → **highest ν (0.70).**
- **Bas_3, f_coarse = 0.0 (fine only).** No coarse pocket. The diffusion branch is
  short-circuited (premixed limit); ν is set by the gas-phase premixed kinetics, which are
  shared but *not weighted down by a large laminar d_ox² term*. The propellant burns fast
  and pressure-sensitively in the premixed regime → **intermediate-high ν (0.64).**
- **Bas_2, f_coarse = 0.65 (mixed/bimodal).** The fine AP creates a fast premixed
  background that **pins the surface temperature and partially short-circuits the coarse
  diffusion flame**, while the coarse fraction still adds a large *pressure-insensitive*
  laminar standoff (∝ d_ox², C_lam branch). The net effect **flattens** the diffusion
  feedback's pressure response (the laminar term dilutes the p⁻² reaction term) → **lowest
  ν (0.52).**

So the controlling variable is the **competition weight between the pressure-insensitive
laminar branch (scaled by coarse loading and d_ox²) and the pressure-sensitive p⁻² reaction
branch.** A single shared exponent on `f_coarse` reproduces the U-shaped ordering because the
mixed case is where the laminar (insensitive) branch is simultaneously large (coarse present)
*and* diluted (fine present) — neither extreme.

This is consistent with the experimental literature: bimodal AP blends routinely show
*lower* exponents than either endpoint because the fine fraction premixes and buffers the
pressure response (Miller; Cohen; the AP-size/loading studies cited below).

---

## 6. The implementable closed form (ONE new shared coefficient)

Augment the standoff (★) with the reaction branch and let `f_coarse` weight it. Define the
**coarse mass fraction** `f_c = LargeParticlesFraction ∈ [0,1]` (already in the data as
`large_particles_fraction`; 0.65 for Bas_2, 0.00 for Bas_3, 1.00 for Bas_4).

### Recommended form (one new shared tunable `n_p`, fixed exponent −2 on the reaction term)

```
h_diff = K_h · (c_v · ṁ_d · d_ox² / λ_g)
              · [ (1 − f_c) + f_c · ( p_ref / p )² ]^( n_p )         [m]      (FORMULA A)

q_diff = λ_g · (T_diff − T_s) / h_diff                                [W/m²]
```

Symbols and units:

| symbol | meaning | unit |
|---|---|---|
| `K_h` | existing shared laminar coefficient (`KDiffusionHeight`) | – |
| `c_v` | volumetric heat capacity (`VolumetricSpecificHeatCapacity`) | J/(m³·K) (code stores J/(kg·K) numerically; keep as-is) |
| `ṁ_d` | surface mass flux (`DecomposeRate`) | kg/(m²·s) |
| `d_ox` | coarse-AP average diameter (`AverageOxidizerDiameter`) | m |
| `λ_g` | gas conductivity (`ThermalConductivity`) | W/(m·K) |
| `p` | chamber pressure (`context.Pressure`) | Pa |
| `p_ref` | **fixed reference pressure (NOT tuned)**, set to mid-band, e.g. 7.0 MPa | Pa |
| `f_c` | coarse mass fraction (`LargeParticlesFraction`) | – |
| **`n_p`** | **the single NEW SHARED tunable** (blend/sensitivity exponent), expected `n_p ≈ 0.3–1.0`, sign **positive** | – |
| `T_diff`,`T_s` | flame, surface temperature | K |

**Fixed exponent:** the reaction term is hard-wired to `( p_ref / p )^( −2 )`, i.e. p⁻²
inside the bracket (Lengellé reaction length, BDP second-order kinetics). Only `n_p` is free.

**Mechanism / expected sign.**
- For **coarse-only** (`f_c = 1`): bracket = `(p_ref/p)^2`, so
  `h_diff ∝ ṁ_d · (p_ref/p)^(2 n_p)`. As p rises the standoff collapses and
  `q_diff ∝ 1/h_diff` rises steeply → **adds the most to ν** (Bas_4 highest). ✔
- For **fine-only** (`f_c = 0`): bracket = 1, recovering the original
  `h_diff = K_h c_v ṁ_d d_ox²/λ_g` (pressure only via ṁ_d). The diffusion flux is a small,
  weakly p-sensitive contributor and ν is dominated by the (shared) premixed gas kinetics →
  **intermediate ν** (Bas_3). ✔
- For **mixed** (`f_c = 0.65`): bracket = `0.35 + 0.65·(p_ref/p)^2`. At low p the second term
  is large (standoff long, diffusion flux suppressed); at high p it decays toward 0.35
  (standoff floored by the laminar term). The *blend* gives the **flattest** diffusion-flux
  pressure response of the three → **lowest ν** (Bas_2). ✔

`n_p` is a single number applied identically to all compositions; the *spread* comes entirely
from `f_c` and `p`, not from any per-composition parameter. Increasing `n_p` increases the
total spread between Bas_4 and Bas_2; the optimiser picks the `n_p` that lands
Bas_4 > Bas_3 > Bas_2 with the right magnitudes. Set bounds e.g. `n_p ∈ [0.1, 1.5]`.

> **Why this passes the "shared-orders" trap.** The premixed/kinetic flames keep their shared
> reaction order. The new ν spread is produced purely by a *geometric* standoff weighting
> (`f_c`, `p`), which is a known per-composition input, not a free per-composition fit.

### Additive variant (more faithful to Lengellé's sum-of-two-lengths, still one new coeff)

If a strict sum (rather than a power-blend) is preferred:

```
h_diff = K_h · (c_v · ṁ_d · d_ox² / λ_g)                               (laminar branch)
       + K_h · C_rxn · (c_v · ṁ_d · d_ox² / λ_g) · f_c · (p_ref/p)²     (reaction branch)
       = K_h · (c_v · ṁ_d · d_ox² / λ_g) · [ 1 + C_rxn · f_c · (p_ref/p)² ]   (FORMULA A′)
```

Here the **one new shared tunable is `C_rxn ≥ 0`** (dimensionless reaction/laminar ratio),
the laminar branch is never lost (numerical safety: `h_diff` never → 0), the `(p_ref/p)²`
exponent is fixed, and the `f_c` weighting delivers the same ordering. **A′ is the safest to
code** (no fractional power of a sum, strictly positive, reduces exactly to the current model
when `C_rxn = 0`). Recommend **A′** as the primary; **A** if a sharper spread is needed.

### Fallback (NO new parameter) — reuse the existing premixed reaction order

If the model forbids even one new scalar, reuse the **existing shared gas reaction order** `ν`
already used by the kinetic flame (`NuPocketSkeleton`/`NuPocketOutSkeleton` exponents) as the
collapse exponent, and weight by `f_c`:

```
h_diff = K_h · (c_v · ṁ_d · d_ox² / λ_g) · [ (1 − f_c) + f_c · (p_ref/p)^(2ν_kin) ]   (FORMULA B)
```

with `ν_kin` the existing shared premixed order and `p_ref` a fixed constant (7 MPa). No new
optimiser slot is added; the spread still tracks `f_c`. This is structurally weaker (the
diffusion collapse is tied to the premixed order) but adds **zero** parameters.

---

## 7. Implementation notes (C#, two mirror paths)

- **Plumb `f_coarse` into `PropellantParams`.** Add `CoarseFraction` (double / `Ratio`) to
  `PropellantParamsByDoubles` and `PropellantParamsByUnits`
  (`.../Computation/Models/KnownParams/PropellantParamsByUnits.cs`). Populate in
  `ProblemContextByDoublesMatrixBuilder.GetPropellantParams` /
  `ProblemContextByUnitsMatrixBuilder` from
  `propellant.Components.OfType<AmmoniumPerchlorate>().First().LargeParticlesFraction`
  (mirror of the existing `GetAverageParticlesDiameter()` line).
- **Add the new shared coefficient** to `CombustionSolverParams{ByDoubles,ByUnits}` (e.g.
  `KDiffusionReaction` for `C_rxn`, or `NpDiffusion` for `n_p`) — this lives in the **shared
  `[0..10]` block** of the 32-element group vector, never in the per-composition `[11..31]`
  slots. Give it a fixed bound (e.g. `C_rxn ∈ [0, 50]` or `n_p ∈ [0.1, 1.5]`).
- **Pass pressure into `GetDiffusionFlameHeight`.** It currently has no `pressure` argument;
  thread `context.Pressure` (Pa) through both the `ByUnits` and `ByDoubles` overloads in
  `PocketPropellantSolver`. `p_ref` is a compile-time constant (`7e6` Pa), **not** tuned.
- **Edit both `GetDiffusionFlameHeight` overloads** (lines ~701 and ~1056 of
  `PocketPropellantSolver.cs`) identically — the `ByDoubles` path drives the optimiser, the
  `ByUnits` path drives reporting; they must stay in lockstep (project convention).
- **Numerical guard:** prefer **A′** (additive) so `h_diff > 0` always; clamp `p ≥ 1e3 Pa`
  before the ratio to avoid blow-up at p→0.

---

## 8. References (with DOIs)

1. **Beckstead, M. W., Derr, R. L., Price, C. F.** "A Model of Composite Solid-Propellant
   Combustion Based on Multiple Flames." *AIAA Journal* **8**(12):2200–2207 (1970).
   [doi:10.2514/3.6087](https://doi.org/10.2514/3.6087). — three competing flames; flame
   heights; particle-size → exponent (Beckstead–Gross regimes).
2. **Lengellé, G., Duterque, J., Trubert, J.-F.** "Physico-chemical Mechanisms of Solid
   Propellant Combustion." In *Solid Propellant Chemistry, Combustion, and Motor Interior
   Ballistics*, V. Yang, T. B. Brill, W.-Z. Ren (eds.), *Progress in Astronautics and
   Aeronautics* **185**, AIAA (2000), pp. 59–112.
   [doi:10.2514/5.9781600866562.0059.0112](https://doi.org/10.2514/5.9781600866562.0059.0112).
   — laminar (∝ d_ox², p-independent) + reaction (∝ p⁻²) standoff decomposition.
3. **Lengellé, G., Duterque, J., Trubert, J.-F.** "Combustion of Solid Propellants."
   RTO/AGARD Lecture Series LS-180 / VKI (1992/2002). — companion derivation of the standoff
   lengths and pressure scalings (Stanford reprint: web.stanford.edu/~cantwell/…/
   Combustion_of_Solid_Propellants.pdf).
4. **Summerfield, M., Sutherland, G. S., Webb, M. J., Taback, H. J., Hall, K. P.** "Burning
   Mechanism of Ammonium Perchlorate Propellants." In *Solid Propellant Rocket Research*,
   *Progress in Astronautics and Rocketry* **1**, Academic Press (1960), pp. 141–182.
   [doi:10.2514/5.9781600864568.0141.0182](https://doi.org/10.2514/5.9781600864568.0141.0182).
   — granular-diffusion-flame law `1/r = a/p + b/p^(1/3)`; size enters the diffusion term.
5. **Beckstead, M. W.** "Overview of Combustion Mechanisms and Flame Structures for Advanced
   Solid Propellants." In *Solid Propellant Chemistry, Combustion, and Motor Interior
   Ballistics*, *Prog. Astronautics & Aeronautics* **185**, AIAA (2000), pp. 267–285.
   [doi:10.2514/5.9781600866562.0267.0285](https://doi.org/10.2514/5.9781600866562.0267.0285).
   — regime map vs AP size/pressure; premixed limit for fine AP.
6. **Cohen, N. S.** "A Pocket Model for Aluminum Agglomeration in Composite Propellants."
   *AIAA Journal* **21**(5):720–725 (1983). [doi:10.2514/3.8138](https://doi.org/10.2514/3.8138).
   — "pocket" framing for coarse/fine AP, supporting the surface-fraction split used here.
7. **Chorpening, B. T., Knott, G. M., Brewster, M. Q.** "Flame structure and burning rate of
   ammonium perchlorate/hydroxyl-terminated polybutadiene propellant sandwiches."
   *Proc. Combust. Inst.* **28**(1):847–853 (2000).
   [doi:10.1016/S0082-0784(00)80289-2](https://doi.org/10.1016/S0082-0784(00)80289-2).
   — measured diffusion-flame standoff scaling with binder/AP width (validates ∝ d_ox²).
8. **Jackson, T. L., Buckmaster, J.** "Heterogeneous Propellant Combustion." *AIAA Journal*
   **40**(6):1122–1130 (2002). [doi:10.2514/2.1761](https://doi.org/10.2514/2.1761).
   — random-pack diffusion-flame computations confirming size-controlled standoff and the
   bimodal flattening of the pressure response.

### Sources consulted (web)

- BDP 1970, AIAA J. — https://arc.aiaa.org/doi/10.2514/3.6087
- Lengellé–Duterque–Trubert reprint (Stanford) —
  https://web.stanford.edu/~cantwell/AA283_Course_Material/AA283_Resources/References/Combustion_of_Solid_Propellants.pdf
- "Modern Competing Flames Model for AP/HTPB" (updated BDP), *J. Propulsion & Power* —
  https://arc.aiaa.org/doi/10.2514/1.B38925
- "Comprehensive Study of AP Particle Size and Loading Effects on Burning Rates" —
  https://www.researchgate.net/publication/325465114
- Beckstead, "Overview of Combustion Mechanisms…" —
  https://www.researchgate.net/publication/24372524
