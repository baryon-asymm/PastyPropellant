# What surface temperature would reproduce the f_s polynomial?

**Question.** The equilibrium closure gives `f_s = φ_Al + φ_C(s)(T_pore, p)` with
`T_pore = (T_s + T_m)/2` and `T_m = 2300 K`. `φ_Al` is recipe and fixed, so the surface
temperature is the closure's *only* free quantity. Run it backwards: for every composition and
pressure, what `T_s` would make the closure land exactly on the shipped 22-coefficient
polynomial?

**Answer.** Of the 50 (composition, pressure) points, **one** — Bas_2 at 4.67 MPa, `T_s = 758 K`
— has a solution inside the solver's own 600–900 K bracket. Five more have a solution at a real
but out-of-bracket temperature (16, 211, 238, 430, 1763 K). Five need a *negative* Kelvin
temperature (−276 to −795 K). The remaining **39 have no solution at any temperature at all**,
for reasons that are conservation laws, not numerics.

**How much of this depends on the temperature rule.** `T_pore = (T_s + T_m)/2` pins the pore
above 1150 K even at `T_s = 0`, so the closure's pore is always hot and always near the carbon
plateau. That rule is load-bearing for the 12 points that do have a root. It is *not* load-bearing
for the other 38: 20 are above the carbon inventory, 13 ask for `f_s < φ_Al`, and 5 are above the
peak of `φ_C(T)` over 700–2300 K. Those verdicts survive any mapping from `T_s` to a pore
temperature, and any choice of `T_m`.

## How it was computed

`φ_C(s)` was tabulated by Gibbs minimisation over `T_pore` = 700…2300 K at all ten pressures for
all five compositions — 1200 equilibria in 24 shards, `scan_carbon_shard.py`, sharded because one
solve takes 6–17 s and the library threads across the whole machine (one process is ~8× slower
than 24 single-threaded ones). The grid was refined on the steep cold flank after the first pass
put a root at −510 K where a direct solve puts it at −502 K. `invert_skeleton_surface_fraction.py`
then reads the shards plus `data/skeleton_carbon_equilibrium.json` (which already covers
`T_pore` 1450–1600 K) and solves `φ_C(T_pore, p) = f_s,poly(p) − φ_Al` on each branch.

One point in 1200 — Bas_4, 1.61 MPa, 700 K — returned 37.5 mol/kg of condensed carbon against an
inventory of 11.9. That is a non-converged Gibbs solve, and it is dropped by a mass-balance
filter rather than by eye; without the filter it would have become the "maximum" of its curve
and decided a verdict on its own.

`φ_C(T)` has a maximum near `T_pore` ≈ 1750–1900 K: methanation (exothermic, Δn = −1) takes the
carbon on the cold side, Boudouard and steam gasification on the hot side. So a target below the
maximum is met twice, and both roots are reported. The reachable window is `T_s ∈ [0, T_m]`,
i.e. `T_pore ∈ [1150, 2300]` — a negative Kelvin temperature is impossible, and a surface hotter
than the melting metal has no skeleton standing on it.

## Why 39 points have no solution

The polynomial demands, over the same 1–6.5 MPa window, **both more solid than the pocket
contains and less solid than its aluminium alone occupies**:

| verdict | points | meaning |
|---|---|---|
| ABOVE INVENTORY | 20 | target > φ_C,max, the entire carbon the binder brought in |
| NEGATIVE TARGET | 13 | target < 0, i.e. the polynomial asks for `f_s < φ_Al` |
| ABOVE EQUILIBRIUM | 5 | carbon exists but equilibrium never holds that much condensed |
| BELOW TABULATED | 1 | needs a colder branch than `T_s = −900 K` |
| UNPHYSICAL | 5 | root exists only at `T_s < 0` |
| OUTSIDE BRACKET | 5 | real temperature, outside 600–900 K |
| IN BRACKET | 1 | Bas_2 at 4.67 MPa |

Ceilings and floors, all from the recipe:

| | φ_Al | φ_C,max | f_s reachable at *any* T | polynomial asks for |
|---|---|---|---|---|
| Bas_0 | 0.2184 | 0.1131 | 0.218…0.331 | 0.259 → **0.126** |
| Bas_1 | 0.2184 | 0.1131 | 0.218…0.331 | **0.378** → 0.163 |
| Bas_2 | 0.2184 | 0.1131 | 0.218…0.331 | **0.359** → 0.272 |
| Bas_3 | 0.1700 | 0.0880 | 0.170…0.258 | **0.313** → 0.151 |
| Bas_4 | 0.2578 | 0.1335 | 0.258…0.391 | **0.529** → **0.522** |

Bas_4 asks for 2.03× the pocket's whole carbon inventory at every pressure. Bas_0 and Bas_1
drop below `φ_Al` from 3.44 MPa upward. Under Delesse that means the *aluminium* has to leave
the surface — which the equilibrium closure has no mechanism for, since only carbon can
gasify. The polynomial is a fit to agglomeration, so it embeds aluminium departure; the closure
does not.

Bas_3 is a separate case: its pocket carries *all* the AP (`large_particles_fraction = 0`), so
equilibrium leaves only φ_C ≈ 0.021–0.028 condensed — 24–31 % of its inventory, against
95–98 % for the others. Its ceiling is therefore not the inventory but the equilibrium itself.

## Verification

Four independent checks, since the headline is a negative result:

* **The root is real, not an interpolation artefact.** Solving directly at `T_pore = 899 K` for
  Bas_0 at 1 MPa gives `f_s = 0.2585` against the polynomial's 0.2586.
* **The two pipelines agree exactly.** Recomputing `φ_C` at `T_pore = 1525 K` — a point the
  shipped table already holds — reproduces it to five decimals (0.00 % deviation) for Bas_0,
  Bas_3 and Bas_4. The Gibbs solve is deterministic and the scan is not drifting from the table
  the run itself used.
* **The polynomial is evaluated as the C# evaluates it**: `Σ cᵢ pⁱ` with p in MPa, divided by
  `pocket_mass_fraction` (`PropellantExtensions.GetPocketSurfaceFraction`). 0.2586 at 1 MPa.
* **`φ_Al` and the volume basis check by hand**: `v_matrix = 6.732×10⁻⁴ m³/kg`, `φ_Al = 0.2184`,
  both matching the table.

`φ_C,max` is a conservation ceiling, not the range of the function: over the reachable window
`φ_C` for Bas_0 at 1 MPa only spans 0.0733…0.1108. A target of 0.0402 is below the window's
floor, not between zero and the ceiling — which is why it lands on the cold branch.

## The leverage argument

Even where a root exists, the temperature is not a usable knob. Across the entire 600–900 K
search bracket the closure moves `f_s` by 0.0023–0.0097. The gap to the polynomial is
0.003–0.190 — a median of **17× the whole bracket's swing**, up to 61× for Bas_4 at 1 MPa.

And the required pressure dependence has the wrong sign and the wrong magnitude. For Bas_2, the
only composition with several roots, the required `T_s` runs 758 → 430 → 211 → 16 K over
4.67 → 6.50 MPa: **−405 K/MPa**. In the best run under this closure the solver actually
converged to 623 → 670 K over 1 → 6.5 MPa: **+8.5 K/MPa**. Opposite sign, 48× the magnitude.

Converged `T_s` in that run (objective 0.0228), for scale: Bas_0 817–842 K, Bas_1 618–641,
Bas_2 623–670, Bas_3 643–686, Bas_4 600–641 — all inside the bracket, all rising slowly with
pressure.

## What this settles

The polynomial and the equilibrium closure are not two calibrations of one model separated by a
temperature error. They disagree about *what is on the surface*, and no choice of `T_s` — or of
the bracket, which was the practical worry — reconciles them. The discrepancy is material, not
thermal, which is the same conclusion the agglomeration-side inverse problem reached from the
other direction, and it points at the same missing inputs: the ferrocene-containing catalyst and
the activated carbon, neither of which appears in `data/propellant_components.json`.

Reproduce with:

```bash
for i in $(seq 0 23); do OMP_NUM_THREADS=1 python3 scan_carbon_shard.py $i 24 shard_$i.jsonl & done; wait
for i in $(seq 0 23); do OMP_NUM_THREADS=1 python3 scan_carbon_shard.py $i 24 shard_b$i.jsonl \
    "780,830,880,930,980,1050,1200,1250,1350,1400,2000,2150,2250" & done; wait
python3 invert_skeleton_surface_fraction.py
python3 plot_required_surface_temperature.py    # -> required_surface_temperature.png
```
