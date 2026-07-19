# Differential Evolution — Tuning To-Do

> ## ⚠️ Status: MOSTLY LANDED — retained as a design record, not as a work list
>
> **Reviewed 2026-07-19** against the current `GetLowerBound` / `GetUpperBound` /
> `BuildGroupPenaltyEvaluators` / `RunGroupOptimizationAsync` in
> [Program.cs](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs). Resolves **DOC-6** in
> [tech-debt.md](tech-debt.md).
>
> **Priorities 1–6 are done.** Only **Priority 7** (reproducibility & instrumentation) is still open.
>
> | Priority | Verdict |
> |---|---|
> | 1.1/1.2 Search bounds | **Adopted**, 12 of 15 rows verbatim; 3 rows superseded (`[0]`, `[15]`, `[16]`) |
> | 1.3 Remove degenerate slots | **Obsolete** — the three slots were *opened up*, not removed |
> | 2 Population size | **Adopted verbatim** (`12·D = 384`) |
> | 3 Penalty rates | **Adopted verbatim** (`0.01`) |
> | 4 `F` / `CR` | **Adopted** (`F=0.5`, `CR=0.9`), but largely moot under jDE |
> | 5 Termination strategy | **Adopted verbatim** (relative stagnation 5 000 × 1e-6 OR 9 h) |
> | 6 Library / algorithm upgrade | **Done, and overtaken** — the premise below is obsolete |
> | 7 Reproducibility & instrumentation | **STILL OPEN — both items** |
>
> **Why this document is kept rather than retired.** Two reasons. First, Priority 7 is genuinely
> unfinished. Second, and more important, §1.1 row `[0]` is the written record of *why*
> `ADecompose`'s lower bound moved off `0` — the "LB=0 is a degenerate corner ($A=0 \Rightarrow
> \dot m_d = 0$)" reasoning. That turned out to matter for the solver-parity defect tracked as
> **COR-2**: a genome at the degenerate corner is exactly the input that separates the `ByDoubles`
> and `ByUnits` paths' `BurnRateIsFound` behaviour. Deleting this file would delete the rationale.
>
> **How to read the rest of the file.** The original text is preserved unedited so the reasoning
> stays intact. Each priority now opens with a **`> STATUS:`** block giving the current value and the
> verdict. Where a "Current state" snippet below shows the *old* values, that snippet is history —
> trust the STATUS block and `Program.cs`, not the snippet.
>
> **Line references are stale.** `Program.cs` has grown substantially since this was written; the
> `Program.cs:NN` anchors throughout no longer point at the cited code. Search by symbol name.

Recommendations for improving the DE configuration used in `RunGroupOptimizationAsync` ([Program.cs](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs)).
Items are listed in **execution order** — earlier items dominate the effect of later ones, so do not skip ahead.

For background on every formula, parameter, and PDF report field referenced below, see [docs/math-model.md](math-model.md). For the library's surface area (only one mutation/selection strategy, only uniform initial sampling) see `DotNetDifferentialEvolution 1.1.1`.
*(Obsolete as of the 4.0.0 upgrade — see the Priority 6 STATUS block.)*

---

## Priority 1 — Search bounds (critical, blocking)

> **STATUS: ADOPTED** (landed in `b57bdd5` "tune(de): apply physical search bounds (Priority 1.2) +
> open slot [17]", amended by `b6ed508`). 12 of the 15 non-degenerate rows match the recommendation
> **verbatim**. Three diverge — see the per-row Status column in §1.1 and the annotated summary table
> at the foot of the file. The `double.MaxValue` slot and the negative-`Nu` half-space, the two
> pathologies this priority existed to kill, are both gone.

**File:** `GetLowerBound()` / `GetUpperBound()` in [Program.cs](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs).

Current state *(historical — this is the pre-fix snapshot, kept to show what was wrong)*:

```csharp
double[] GetLowerBound()
{
    return [0, 0, 0, 5e4, 0, 5e4, 0, 5e4, -3.0, -3.0, -3.0, 1e-15, 1e-15, -1e12, 1e-15, 1.0, 2.0, 0.0];
}

double[] GetUpperBound()
{
    return [double.MaxValue, 4422718, 1e15, 2e5, 1e15, 2e5, 1e15, 2e5,
            3.0, 3.0, 3.0, 1.0, 1.0, 1e12, 1e1, 1.0, 2.0, 0.0];
}
```

Beyond the obvious `double.MaxValue` slot, almost every entry needs revision. The 18-slot layout (see [docs/math-model.md §3.5](math-model.md#35-the-18-element-optimization-vector-single-composition)) maps as follows; problem severity flagged in the "Issue" column:

### 1.1 Per-slot audit

> **STATUS — per-slot verdict against current `Program.cs`:**
>
> | idx | Field | Recommended | Actual now | Verdict |
> |---:|---|---|---|---|
> | 0 | `ADecompose` | `1` / `1e9` | `1.0` / **`1e13`** | **Superseded.** LB adopted. UB raised 4 decades in `b6ed508`: Bas_0 sat *pinned at* the `1e9` ceiling, which is precisely the §1.4 check-2 signal that a bound is still wrong. An in-code comment records this. The §1.4 diagnostic worked. |
> | 1 | `EDecompose` | `5e4` / `3e5` | `5e4` / `3e5` | **Adopted verbatim.** |
> | 2,4,6 | `AKineticFlame*` | `1e5` / `1e13` | `1e5` / `1e13` | **Adopted verbatim.** |
> | 3,5,7 | `EKineticFlame*` | keep, or `5e4` / `2.5e5` | `5e4` / `2.5e5` | **Adopted** — the optional-widening variant was taken. |
> | 8,9,10 | `Nu*` | `0.0` / `2.5` | `0.0` / `2.5` | **Adopted verbatim.** Negative reaction order is locked out. |
> | 11 | `AMetalBurningConstant` | `1e-10` / `1e-3` | `1e-10` / `1e-3` | **Adopted verbatim.** |
> | 12 | `BMetalBurningConstant` | `1e-12` / `1e-5` | `1e-12` / `1e-5` | **Adopted verbatim.** |
> | 13 | `DeltaH` | `-1e7` / `1e7` | `-1e7` / `1e7` | **Adopted verbatim.** |
> | 14 | `KDiffusionHeight` | `1e-3` / `1e1` | `1e-3` / `1e1` | **Adopted verbatim.** |
> | 15 | `APowOrder` | keep `1`/`1`, or remove slot | **`0.0` / `3.0`** | **Superseded.** Neither branch taken: the slot was *freed*. It is now a fitted parameter, no longer a dimensional constant. |
> | 16 | `BPowOrder` | keep `2`/`2`, or remove slot | **`0.0` / `3.0`** | **Superseded.** Same as `[15]`. |
> | 17 | `KCoefficientRadiationTemperature` | decide: `const 0`, or open to `[0,1]` | `0.0` / `1.0` | **Adopted** — the "open the bound" branch was chosen explicitly (`b57bdd5`). DE now explores the radiative-temperature closure. |
>
> ⚠️ **Slots `[15]` and `[16]` are the one place where the code contradicts the analysis below.**
> The text argues they are dimensionally *forced* ($p_A=1$, $p_B=2$ by unit consistency). They are
> now free in `[0, 3]`, so a converged run can report $p_A \neq 1$ / $p_B \neq 2$ — dimensionally
> inconsistent on the argument given here. Whether that is a deliberate empirical relaxation or
> drift is **not recorded anywhere in the code**; the in-code comments still call them
> "degenerate dimensional const". If you are reporting fitted $p_A$ / $p_B$ values, resolve this
> first.

| idx | Field | Unit | LB now | UB now | Issue | Recommend |
|---:|---|---|---|---|---|---|
| **0** | `ADecompose` | kg/(m²·s) | `0` | `double.MaxValue` | **Critical** — uniform sampling lands at ≈1e308; `aDecompose * Math.Exp(-E/(RT))` overflows; `BurnRateIsFound = false` for nearly the whole initial population. LB=0 is also a degenerate corner ($A=0 \Rightarrow \dot m_d = 0$). | LB=`1`, UB=`1e9`. RDX/HMX/PCP binder pre-exponentials sit in $10^{0}..10^{9}$ kg/(m²·s). |
| **1** | `EDecompose` | J/mol | `0` | `4_422_718` | **Critical** — UB ≈ 4.4 MJ/mol is **>10× higher than any plausible activation energy**. Typical for energetic binders: 80–250 kJ/mol. With current bounds 95% of random samples have $E_d > 5\cdot 10^5$ J/mol; at $T_s \approx 700$ K, $\exp(-E_d/(RT_s)) \approx \exp(-86) \approx 10^{-38}$ — dead zone, no decomposition, sentinel cascade. LB=0 collapses Arrhenius to a temperature-independent constant. | LB=`5e4`, UB=`3e5`. |
| **2,4,6** | `AKineticFlame{InterPocket,PocketOutSkeleton,PocketSkeleton}` | 1/s | `0` | `1e15` | UB too high by ≥ 2 decades; physical gas-phase pre-exponentials live in $10^{6}..10^{13}$ 1/s. LB=0 yields infinite kinetic-flame height (see [BaseKineticPropellantSolver.cs:216-237](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Solvers/BaseKineticPropellantSolver.cs#L216-L237)), collapsing $q_k \to 0$. | LB=`1e5`, UB=`1e13`. |
| **3,5,7** | `EKineticFlame{InterPocket,PocketOutSkeleton,PocketSkeleton}` | J/mol | `5e4` | `2e5` | **OK as-is.** 50–200 kJ/mol is the standard band for gas-phase oxidation kinetics in solid-propellant combustion. Optional widening to UB=`2.5e5` for headroom. | (keep) or LB=`5e4`, UB=`2.5e5`. |
| **8,9,10** | `Nu{InterPocket,PocketOutSkeleton,PocketSkeleton}` | — | `-3.0` | `3.0` | **Critical** — negative reaction order is unphysical: $h_k \propto \dot m_d / (A_k \rho^\nu \exp(-E/RT))$, so $\nu<0$ makes flame height *grow* with density, $q_k$ *shrinks* with pressure, $T_s$ *drops* with pressure — inverted pressure dependence of burn rate. Optimizer can sit in this half-space and "fit" the data with garbage physics. | LB=`0`, UB=`2.5`. Allow up to 2.5 for unusual mechanisms; lock out negative. |
| **11** | `AMetalBurningConstant` | m²/s | `1e-15` | `1.0` | Range spans **15 decades** beyond physical relevance. With $v_b \sim 10^{-2}$ m/s and $p_A=1$: $\delta_s = A_m / v_b \approx 100\,A_m$. Realistic $\delta_s \in [10^{-6}, 10^{-3}]$ m ⇒ $A_m \in [10^{-8}, 10^{-5}]$. The current `1.0` upper bound implies $\delta_s \sim 100$ m. | LB=`1e-10`, UB=`1e-3`. |
| **12** | `BMetalBurningConstant` | m³/s² | `1e-15` | `1.0` | Same shape of problem. With $v_b \sim 10^{-2}$ and $p_B=2$: $d_p = B_m / v_b^2 \approx 10^4\,B_m$. Realistic $d_p \in [10^{-6}, 10^{-4}]$ m (1–100 μm) ⇒ $B_m \in [10^{-10}, 10^{-8}]$. | LB=`1e-12`, UB=`1e-5`. |
| **13** | `DeltaH` | J/kg | `-1e12` | `1e12` | **Critical** — 1 TJ/kg exceeds any chemical energy density (TNT ≈ 4.2 MJ/kg, nitromethane ≈ 11 MJ/kg). Typical sublimation/decomposition delta for energetic materials: $10^{5}..10^{7}$ J/kg. Magnitude `1e12` makes $q_{\text{sub}} = \dot m_d (c_p \Delta T + \Delta H)$ blow up, the surface-T bisection misses bracketing, sentinel `-1 K` returns. | LB=`-1e7`, UB=`1e7`. |
| **14** | `KDiffusionHeight` | — | `1e-15` | `1e1` | LB at $10^{-15}$ collapses $h_{\text{diff}} \to 0$, so $q_{\text{diff}} \to \infty$, breaking the surface-T balance. UB at 10 is reasonable. After rescaling: $h_{\text{diff}} = K_h \cdot \dot m_d \cdot d_{ox}^2 \cdot c_{p,V} / \lambda_g$; with the typical input magnitudes, useful $K_h \in 10^{-3}..10^{0}$. | LB=`1e-3`, UB=`1e1`. |
| **15** | `APowOrder` | — | `1.0` | `1.0` | **Degenerate slot (LB = UB).** This is a dimensional-consistency constant, not a free parameter: $[\delta_s] = [m^2/s] / [m/s]^{p_A} = [m]$ ⇒ $p_A = 1$. The slot is sampled but never moves. See §1.3 below. | (no bound change; restructure — see §1.3) |
| **16** | `BPowOrder` | — | `2.0` | `2.0` | **Degenerate slot.** Same logic: $[d_p] = [m^3/s^2] / [m/s]^{p_B} = [m]$ ⇒ $p_B = 2$. | (no bound change; restructure — see §1.3) |
| **17** | `KCoefficientRadiationTemperature` | — | `0.0` | `0.0` | **Degenerate slot (intentional lock).** With LB=UB=0, $\bar T_r = T_f^{(S)}$ — the surface temperature drops out of the radiative-temperature closure (see [docs/math-model.md §7.1](math-model.md#71-average-radiative-temperature-closure)). Either keep as `const 0` in code, or open the bound (e.g. `[0, 1]`) and document the rationale. | (decide — see §1.3) |

### 1.2 Proposed replacement

```csharp
double[] GetLowerBound()
{
    return [
        1.0,    // [0]  ADecompose                       kg/(m²·s)
        5e4,    // [1]  EDecompose                       J/mol
        1e5,    // [2]  AKineticFlameInterPocket         1/s
        5e4,    // [3]  EKineticFlameInterPocket         J/mol
        1e5,    // [4]  AKineticFlamePocketOutSkeleton   1/s
        5e4,    // [5]  EKineticFlamePocketOutSkeleton   J/mol
        1e5,    // [6]  AKineticFlamePocketSkeleton      1/s
        5e4,    // [7]  EKineticFlamePocketSkeleton      J/mol
        0.0,    // [8]  NuInterPocket                    —
        0.0,    // [9]  NuPocketOutSkeleton              —
        0.0,    // [10] NuPocketSkeleton                 —
        1e-10,  // [11] AMetalBurningConstant            m²/s
        1e-12,  // [12] BMetalBurningConstant            m³/s²
        -1e7,   // [13] DeltaH                           J/kg
        1e-3,   // [14] KDiffusionHeight                 —
        1.0,    // [15] APowOrder                        — (degenerate, see §1.3)
        2.0,    // [16] BPowOrder                        — (degenerate)
        0.0     // [17] KCoefficientRadiationTemperature — (degenerate, locked)
    ];
}

double[] GetUpperBound()
{
    return [
        1e9,    // [0]
        3e5,    // [1]
        1e13,   // [2]
        2.5e5,  // [3]
        1e13,   // [4]
        2.5e5,  // [5]
        1e13,   // [6]
        2.5e5,  // [7]
        2.5,    // [8]
        2.5,    // [9]
        2.5,    // [10]
        1e-3,   // [11]
        1e-5,   // [12]
        1e7,    // [13]
        1e1,    // [14]
        1.0,    // [15]
        2.0,    // [16]
        0.0     // [17]
    ];
}
```

This still preserves the 18-slot layout used by `CombustionSolverParamsByDoubles.FromVector` ([CombustionSolverParamsByUnits.cs:93-117](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/CombustionSolverParamsByUnits.cs#L93-L117)) and the 32-slot group layout. Restructuring beyond bound changes is §1.3.

### 1.3 (Structural, optional) Remove the three degenerate slots

> **STATUS: OBSOLETE — do not implement.** This section is predicated on `[15]`, `[16]`, `[17]`
> having LB = UB. **None of them do any more**: `[17]` was opened to `[0, 1]` and `[15]`/`[16]` to
> `[0, 3]`. All three are live DE dimensions carrying real search work, so there is no degenerate
> slot left to remove and no ~15% dimension saving to claim. D remains **18** (single) / **32**
> (group), and the 32-slot layout documented in `CLAUDE.md` and `math-model.md` is authoritative.
> The change set below would now be actively wrong. Kept only as the record of a path not taken.

Slots `[15]`, `[16]`, `[17]` all have LB=UB. They occupy 3 of 18 DE dimensions (17%) and 3 of the 11 shared block slots in the 32-vector — DE wastes mutation and crossover work on them. Removing them shrinks D from 18 to 15 (single mode) and from 32 to 29 (group mode), which gives an automatic ~15% reduction in NP × generations cost without changing the model.

Concrete change set if you decide to do this:

- Drop the three fields from `CombustionSolverParamsByUnits` and `CombustionSolverParamsByDoubles`; hard-code `APowOrder = 1`, `BPowOrder = 2`, `KCoefficientRadiationTemperature = 0` as `const` in the solver (or `static readonly` on the struct).
- Update `FromVector(ReadOnlySpan<double> vector)` to length 15.
- Update `GroupCombustionSolverParamsByDoubles.FromVector` and `ToCompositionVector` ([GroupCombustionSolverParams.cs:69-164](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Computation/Models/KnownParams/GroupCombustionSolverParams.cs#L69-L164)) to length 29 and the new shared/specific slot count (8 + 3·7 = 29).
- Update `GetLowerBound`/`GetUpperBound`/`GetGroupLowerBound`/`GetGroupUpperBound` in [Program.cs:53-107](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L53-L107).
- Re-run the unit tests under `ParametricCombustionModel.Core.Tests` and the PlotRenderer tests; the DE optimizer should report `Parameter vector size: 29 (8 shared + 7×3 specific)`.
- Update [docs/math-model.md §3.5–§3.6](math-model.md#35-the-18-element-optimization-vector-single-composition) to reflect the new layout.

Treat this as an opt-in follow-up: do it once Priorities 1.1–1.2 + 2–5 are in and benchmarked. The bound-only fix already removes the worst pathology without touching contracts.

If `[17]` should *not* be a constant (i.e. you want DE to explore the radiative-temperature closure), open its bound to `[0, 1]` and skip the structural step for that slot. Decide before §1.3, since the structural change makes the slot inaccessible.

### 1.4 Verification

> **STATUS: performed; check 2 caught a real problem.** The "values pinned to a bound usually mean
> the bound is still wrong" heuristic is what surfaced `ADecompose`'s `1e9` ceiling on Bas_0, leading
> to the `1e13` change in `b6ed508`. Re-run these three checks after any future bound edit.

Three checks after the bound edit:

1. **No more sentinel cascade.** Run a 3-minute campaign (timeout knob in [Program.cs:154](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs#L154)). The first `Generation: N, Best individual: …` line should report a **finite** best within the first few seconds, not `double.MaxValue`.
2. **Magnitudes match physics.** In the resulting PDF, "Optimization Parameters" section (`CombustionSolverParamsReport`) — each best-fit value should fall comfortably inside the new `[LB; UB]` rather than at a corner. Values pinned to a bound usually mean the bound is still wrong.
3. **No regressed fitness.** Compare final `FitnessFunctionValue` to a baseline from a prior campaign on the **same propellant set**. The new bounds are a strict subset of useful physics, so the optimum should not get worse; if it does, one of the bounds (most likely `[1]` `EDecompose` or `[8,9,10]` `Nu*`) was clipped too tight.

### 1.5 Side effects

After this change, every previously stored optimum (saved 32-vector) becomes incomparable with future runs because they were obtained from a different feasible domain. Tag the change in commit message and re-run baseline campaigns.

---

## Priority 2 — Population size

> **STATUS: ADOPTED VERBATIM.** `populationSize = dimensions * 12` → `32 · 12 = 384`, with an in-code
> comment marking it the jDE production population. The "NP must divide processor count" note was
> honoured too: a `for (; processorsCount >= 14; processorsCount--)` loop walks the worker count down
> until it divides the population evenly. The suggested follow-up step to `16·D = 512` was **not**
> taken and no longer appears necessary — 384 reaches obj 0.157 / penalty 0.
>
> *New since this was written:* the L-SHADE branch uses a different sizing (`18·D = 576` with linear
> population reduction and a 5 000 000-evaluation budget). That branch is not the active one.

**File:** `RunGroupOptimizationAsync` in [Program.cs](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs).

Current: `populationSize = groupLowerBound.Length * 8` = `32 · 8 = 256`. Storn–Price classic guideline for the canonical `DE/rand/1/bin` (which is exactly what `DotNetDifferentialEvolution 1.1.1` implements) is `NP ∈ [10·D, 30·D]`; the combustion fitness is multimodal and noisy (three nested binary searches per evaluation + penalty plateaus), which pushes the recommendation toward the upper half of that band.

**Action:** raise to `12·D = 384` as a first step, evaluate, then consider `16·D = 512` if global-search robustness still matters more than wall-clock.

```csharp
var populationSize = groupLowerBound.Length * 12;  // 32 * 12 = 384
```

The CPU cost per generation scales linearly with NP; double the population doubles the per-generation cost. The compensating win is fewer wasted generations early on and a lower chance of premature convergence on a deceptive local minimum.

### 2.1 Verification

Run the same input twice with `NP = 256` and `NP = 384`, same termination strategy. Compare:

- Final aggregated fitness (Section "Optimization Results" in the PDF — `FitnessFunctionEvaluatorReport`, key `FitnessFunctionValue`).
- Total constraint penalty (`TotalEvaluatedPenalty`).
- Generation count at which the best fitness first dropped below, say, `1e2`.

If the larger population reaches a comparable or better fitness faster (in generations, not wall-clock), the increase is paying off.

---

## Priority 3 — Penalty rates

> **STATUS: ADOPTED VERBATIM.** `const double penaltyRate = 0.01;` in `BuildGroupPenaltyEvaluators()`,
> applied uniformly to every evaluator — the lower `0.001` option was not needed. The suggested
> "alternative (more invasive)" per-evaluator rates were **not** adopted and remain unnecessary:
> the production run reports penalty 0.
>
> *Two drifts from the text below.* (a) The evaluator list is now **five**, not six —
> `RadiativeThermalConductivity` is no longer wired up at all, and `LargeOxidizerParticleSize` is no
> longer dormant but **active** (threshold `1.0`). (b) The evaluators are now single-sourced through
> `BuildGroupPenaltyEvaluators()` so the optimisation run and the forward-eval path report identical
> penalties for the same point — that de-duplication is not described here.

**File:** `BuildGroupPenaltyEvaluators()` in [Program.cs](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs).

Current: every one of the six penalty evaluators (`PocketHeatFluxRatioCompetition`, `InterPocketFasterBurn`, `KineticFlameHeatFlux`, `PoreDiameter`, plus the dormant `RadiativeThermalConductivity` and `LargeOxidizerParticleSize`) is instantiated with `penaltyRate = 1.0`.

Augmented fitness is $\tilde F = F + \sum_k P_k$ ([PenaltyFitnessFunctionEvaluator.cs:23-37](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/FitnessFunctionEvaluators/PenaltyFitnessFunctionEvaluator.cs#L23-L37)). Order-of-magnitude expectations:

| Quantity | Typical range during optimization |
|---|---|
| Fitness $F$ (mean relative burn-rate RMS, see [docs/math-model.md §13](math-model.md#13-fitness-function)) | `1e-2 .. 1e-1` near optimum; `1e0 .. 1e2` early |
| Per-cell $P_k$ at violation | `1e1 .. 1e4` (each penalty is `rate × ratio`, and the ratio can be huge — e.g. `q_kinetic / q_max ≈ 1e3` on a bad genome) |
| Sum across `N_propellants × N_pressures × 4 active evaluators` | another `10×–100×` multiplier |

Net: in the early phase, `Σ P_k` dominates $F$ by 3–4 decades, so DE effectively optimizes "minimize constraint violation, ignore fit". The result tends to converge onto the feasible-region boundary where fit is mediocre.

**Action:** drop `penaltyRate` to `0.01` (or even `0.001`) so violations stay measurable but do not overwhelm the fit term until the genome is already near-feasible.

```csharp
const double penaltyRate = 0.01;
```

Alternative (more invasive): make rate per-evaluator and tune individually — e.g. `PocketHeatFluxRatioCompetition` needs less than `KineticFlameHeatFlux` because its ratio is bounded by the data, while the kinetic-flux ratio can blow up arbitrarily. Defer this until the uniform-rate change is benchmarked.

### 3.1 Verification

In the PDF "Functional Constraints" section (`ConstraintPenaltyEvaluatorReport`) the per-evaluator `PenaltyRate` is printed alongside the threshold. After the change, every active evaluator should show `0.0100`. In the "Optimization Results" block, `Total penalty from constraint violations` should drop sharply on convergence; if it stays large (≥ `F`), the rate is still too high.

---

## Priority 4 — Mutation force `F` and crossover probability `CR`

> **STATUS: ADOPTED — but largely moot under the current strategy.** The code now reads
> `.WithMutationForce(0.5)` (§4.1 adopted) and `.WithCrossoverProbability(0.9)` (§4.2 kept, as
> argued). However the whole priority was framed around a library that *couldn't* adapt F/CR. It can
> now: an in-code comment states F/CR "are only consumed by the Classic and jDE strategies; the
> adaptive variants self-tune them", and the active strategy is **jDE**, which self-adapts F and CR
> per individual from these as seeds. So `0.5` is now a starting point, not the value that governs
> the run. §4.3's "revert to 0.7 if late-phase improvement stops" test is no longer meaningful.

**File:** the `settingsBuilder` chain in [Program.cs](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/Program.cs).

Current *(historical)*: `F = 0.7`, `CR = 0.9`.

### 4.1 `F`: try lowering to 0.5

`DE/rand/1/bin` is sensitive to F. Classical safe range is `F ∈ [0.4, 0.9]`. For multimodal/noisy landscapes (your case — penalty plateaus + nested root-finding) `F ≈ 0.5` is the most-cited "robust default". `F = 0.7` is biased toward exploration: good in the first few thousand generations, but causes overshooting near a basin in the late phase.

The library `DotNetDifferentialEvolution 1.1.1` does not implement adaptive control (no jDE/SHADE/JADE), so a single F must serve the entire run. `0.5` is a better single-value compromise than `0.7`.

```csharp
.WithMutationForce(0.5)
```

### 4.2 `CR`: keep at 0.9

The parameters in the model are strongly coupled: every Arrhenius pair `(A_k, E_k, ν)` is one chemistry, `(A_m, B_m, p_A, p_B)` is the skeleton geometry, `(ΔH, K_h)` is the diffusion-flame closure. CR = 0.9 means "inherit the donor's whole pattern almost intact" — this preserves consistent chemistries through crossover. With low CR (e.g. 0.1) you'd routinely mix `A` from one chemistry with `E` from another, producing physically inconsistent genomes.

**Do not change CR.** Document in the commit message that 0.9 was retained deliberately.

### 4.3 Verification

After lowering F to 0.5, two diagnostics:

1. **Late-phase improvement**. After ~10⁴ generations, observe whether the best fitness still drops occasionally or has plateaued for ≥5000 generations. With F=0.7 the late-phase trajectory tends to be jittery (overshooting); with F=0.5 it should be a smooth monotone decrease.
2. **Final fit quality**. The mean relative burn-rate error (the leaf of $F$, see [docs/math-model.md §13](math-model.md#13-fitness-function)) should be ≤ comparable to the F=0.7 run, **and** the penalty term should be smaller (lower F means smaller perturbations means fewer infeasible donors).

If late-phase improvement stops and the best fitness is worse than with F=0.7, revert to 0.7. F is the one knob where intuition is unreliable; trust the data.

---

## Priority 5 — Termination strategy

> **STATUS: ADOPTED VERBATIM — both sub-items.**
> §5.1: `CustomStagnationStreakTerminationStrategy` now takes a `relativeStagnationThreshold`
> parameter (the comparator was made relative, exactly as specified).
> §5.2: [`OrTerminationStrategy.cs`](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/OrTerminationStrategy.cs)
> was written and is in use. The active configuration is
> `Or(CustomStagnationStreak(maxStagnationStreak: 5_000, relativeStagnationThreshold: 1e-6), Timeout(9h))`
> — the recommended 5 000 / 1e-6 relative / 9 h, unchanged.
>
> *New since:* the L-SHADE branch swaps the stagnation half for
> `LimitEvaluationNumberTerminationStrategy(5_000_000)`, because LPSR's population-reduction schedule
> is defined by the evaluation budget. Both branches keep the 9 h safety timeout.

**Files:** [CustomStagnationStreakTerminationStrategy.cs](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/CustomStagnationStreakTerminationStrategy.cs); [OrTerminationStrategy.cs](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/OrTerminationStrategy.cs); [TimeoutTerminationStrategy.cs](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/TimeoutTerminationStrategy.cs).

Current active: `TimeoutTerminationStrategy(TimeSpan.FromHours(9))` — wall-clock only.
Current dormant (commented): `CustomStagnationStreakTerminationStrategy(299_999, 1e-8)` — absolute stagnation.

Problems:

- **Timeout alone**: budget knob, not a convergence test. On easy targets, wastes hours past convergence; on hard targets, cuts off before reaching even a passable optimum.
- **Stagnation 300k × 1e-8 absolute**: streak window of 300k generations is enormous (likely never triggers within 9 h); 1e-8 absolute is too tight when $F$ is at the `1e-2` scale — the natural late-phase fluctuations are ~`1e-6` *relative*, which corresponds to `1e-8` *absolute* of a `1e-2` value, but only barely. Across many runs the threshold will trigger inconsistently.

### 5.1 Make stagnation relative

In [CustomStagnationStreakTerminationStrategy.cs:31](../src/dotnet/Apps/src/PastyPropellant.ConsoleApp/CustomStagnationStreakTerminationStrategy.cs#L31) the comparison is absolute:

```csharp
if (Math.Abs(population.IndividualCursor.FitnessFunctionValue - LastBestFitnessFunctionValue) > StagnationThreshold)
```

Add a **relative** variant or replace the comparator with `> StagnationThreshold * Math.Abs(LastBestFitnessFunctionValue) + ε`. With relative threshold `1e-6` and the typical late-phase fitness `1e-2`, the absolute equivalent is `1e-8` — same as today, but it auto-adapts as $F$ shrinks.

### 5.2 Combine: stagnation OR timeout

Write a tiny `OrTerminationStrategy : ITerminationStrategy` that returns `s1.ShouldTerminate(p) || s2.ShouldTerminate(p)` so the run can finish on convergence or hit a hard wall-clock ceiling — whichever comes first.

```csharp
.WithTerminationStrategy(
    new OrTerminationStrategy(
        new CustomStagnationStreakTerminationStrategy(
            maxStagnationStreak: 5_000,
            stagnationThreshold: 1e-6),  // now interpreted relative
        new TimeoutTerminationStrategy(TimeSpan.FromHours(9))
    )
)
```

Streak window 5 000 (not 300 000) reflects that, with NP=384, a single generation is one full evaluation across the population — converging to within `1e-6` relative for 5k consecutive generations is already overkill.

### 5.3 Verification

Run with both strategies wired up and observe which one fires. Log message at the moment of termination should record whether it was stagnation or timeout. Re-tune the streak window upward if termination happens too early (final $F$ noticeably worse than a 9 h run), or downward if it routinely runs the full 9 h.

---

## Priority 6 — Library / algorithm upgrade (deferred, scoped separately)

> **STATUS: DONE, AND OVERTAKEN BY EVENTS. The premise below is obsolete — read the STATUS block, not
> the section.**
>
> The dependency is now **`DotNetDifferentialEvolution 4.0.0`**, which ships a selectable
> `DifferentialEvolutionStrategy` enum covering **Classic, jDE, JADE, SHADE and L-SHADE** — i.e. every
> variant this section lists as "none of these are in the dependency". Option 1 (swap/upgrade the
> library) was taken; option 2 (hand-implement `IMutationStrategy`) was not needed.
>
> The repo also went *beyond* this section, adding a Nelder–Mead layer that was never proposed here:
> `DotNetNelderMead` + `DotNetNelderMead.DifferentialEvolution`, surfaced as
> `NelderMeadRefinementSettings` with an in-loop memetic refiner and a final sequential polish.
>
> **Empirical outcome (recorded in `Program.cs` comments — this supersedes the section's expectation
> that modern variants beat canonical DE by 2–4×):**
> - **jDE wins** and is the active strategy: obj **0.157**, penalty **0**.
> - **SHADE / L-SHADE trap** in a penalised degenerate corner (**0.92**).
> - **JADE converges prematurely** to a worse optimum (**0.42**).
> - NM: **final-polish only** (0.15721). In-loop memetic is **off** — tested 2026-06-04, it reached
>   0.15742 / penalty 0, a hair worse, by trimming diversity for no gain.
>
> So the current-to-*p*best family, which this section implicitly favours, is the family that
> *failed* on this problem. Do not re-open the upgrade on the strength of benchmark literature alone.
>
> **Still open from this section:** option 3, **Latin Hypercube Sampling** for the initial population
> (custom `IPopulationSamplingMaker`). No evidence of it in the tree; still uniform sampling.

`DotNetDifferentialEvolution 1.1.1` provides exactly one mutation strategy (canonical `DE/rand/1/bin`), one selection strategy (greedy), and one initial sampling method (`UniformRandomSamplingMaker`). Modern DE variants are documented to outperform this by 2–4× evaluations-to-convergence on standard benchmarks:

- **jDE** — self-adaptive F and CR per individual.
- **SHADE / L-SHADE** — historical-archive-based adaptive F/CR with linearly decreasing population.
- **JADE** — adaptive F/CR with current-to-pbest mutation.

None of these are in the dependency. Adopting any of them means one of:

1. **Swap the library** for one that ships them (e.g. `Accord.NET` has DE, but quality varies; a CMA-ES implementation might be a better fit since CMA-ES handles strongly correlated parameters natively, which is exactly our `(A, E)` Arrhenius case).
2. **Implement custom `IMutationStrategy`** in this repo (the interface is exposed: [DotNetDifferentialEvolution.MutationStrategies.Interfaces.IMutationStrategy](https://www.nuget.org/packages/DotNetDifferentialEvolution)). Self-adapted F/CR per individual is a ~50-line change; SHADE archive bookkeeping is ~200 lines.
3. **Latin Hypercube Sampling** for the initial population (custom `IPopulationSamplingMaker`). Cheaper than algorithm replacement, mostly fixes the cold-start issue.

This is its own design task, not a tuning knob. Open it as a separate ticket once Priorities 1–5 are landed and measured.

---

## Priority 7 — Reproducibility and instrumentation (low-effort, high value)

> ## 🔴 STATUS: **STILL OPEN — both items. This is the only unfinished part of this document.**
>
> **§7.1 fixed seed — NOT DONE.** No seed, `RandomProvider` or `BaseRandomProvider` usage appears
> anywhere in `src/dotnet`. Every campaign still starts from an unseeded initial population, so no
> two runs are comparable and no knob change can be attributed cleanly. This has become *more*
> valuable, not less: the strategy comparison recorded in Priority 6 (jDE 0.157 vs JADE 0.42 vs
> SHADE 0.92) was measured **without** a fixed seed, and the DE variants now differ in their internal
> random streams as well as their update rules.
>
> **§7.2 per-generation CSV — NOT DONE.** An `IPopulationUpdatedHandler` *is* now wired up
> (`PopulationUpdateHandler` in `DifferentialEvolutionScenarioSettings.cs`), but it is not the handler
> described here: it **throttles to one sample every 3 seconds** and publishes a human-readable
> `InfoLogEvent` log line rather than appending `(generation, best_fitness, total_penalty)` to a file.
> Time-throttled sampling makes the trace non-reproducible across machines — the same run logs a
> different set of generations depending on host speed. The plug point is ready; the CSV handler is
> not written.
>
> Both items are cheap and remain worth doing before the next tuning campaign.

Two small things to make the whole tuning exercise tractable:

### 7.1 Fix the random seed during tuning

The library exposes `BaseRandomProvider`/`RandomProvider`; pass a deterministic seed during benchmark runs so changes to F/CR/NP are evaluated against identical initial populations and identical mutation streams. Without a fixed seed every "did this knob help?" comparison is contaminated by run-to-run variance, which for DE on 32-D problems can swamp a 10–20% improvement.

### 7.2 Log per-generation best fitness to CSV

`IPopulationUpdatedHandler` is already pluggable via `WithPopulationUpdateHandler(...)` in [GroupDifferentialEvolutionOptimizer.cs:112-113](../src/dotnet/ParametricCombustionModel/src/ParametricCombustionModel.Optimization/Optimizers/GroupDifferentialEvolutionOptimizer.cs#L112-L113). Write a handler that appends `(generation, best_fitness, total_penalty)` to a CSV per campaign. Compare campaigns by plotting these traces, not by reading PDF endpoints.

Currently the only generation-level signal is the throwaway log line `Generation: N, Best individual: F` (visible in `/tmp/pasty_run.log` from the diagnostic run).

---

## Summary table (changes after Priorities 1–5)

> **All rows below are RESOLVED.** "Was" = the pre-fix state this document was written against;
> "Now" = verified in `Program.cs` on 2026-07-19. The original "Target" column is preserved so
> divergences stay visible.

### Bounds (Priority 1.1)

| idx | Field | Was LB / UB | Target LB / UB | **Now** | Status |
|---:|---|---|---|---|---|
| 0 | `ADecompose` | `0` / `double.MaxValue` | `1` / `1e9` | `1.0` / **`1e13`** | ⚠ superseded (UB raised — Bas_0 pinned at `1e9`) |
| 1 | `EDecompose` | `0` / `4_422_718` | `5e4` / `3e5` | `5e4` / `3e5` | ✅ adopted |
| 2, 4, 6 | `AKineticFlame*` | `0` / `1e15` | `1e5` / `1e13` | `1e5` / `1e13` | ✅ adopted |
| 3, 5, 7 | `EKineticFlame*` | `5e4` / `2e5` | `5e4` / `2.5e5` (or keep) | `5e4` / `2.5e5` | ✅ adopted (widened variant) |
| 8, 9, 10 | `Nu*` | `-3.0` / `3.0` | `0.0` / `2.5` | `0.0` / `2.5` | ✅ adopted |
| 11 | `AMetalBurningConstant` | `1e-15` / `1.0` | `1e-10` / `1e-3` | `1e-10` / `1e-3` | ✅ adopted |
| 12 | `BMetalBurningConstant` | `1e-15` / `1.0` | `1e-12` / `1e-5` | `1e-12` / `1e-5` | ✅ adopted |
| 13 | `DeltaH` | `-1e12` / `1e12` | `-1e7` / `1e7` | `-1e7` / `1e7` | ✅ adopted |
| 14 | `KDiffusionHeight` | `1e-15` / `1e1` | `1e-3` / `1e1` | `1e-3` / `1e1` | ✅ adopted |
| 15 | `APowOrder` | `1.0` / `1.0` (fixed) | (keep — or remove slot, §1.3) | **`0.0` / `3.0`** | ⚠ superseded — freed, now a fitted parameter |
| 16 | `BPowOrder` | `2.0` / `2.0` (fixed) | (keep — or remove slot, §1.3) | **`0.0` / `3.0`** | ⚠ superseded — freed, now a fitted parameter |
| 17 | `KCoefficientRadiationTemperature` | `0.0` / `0.0` (fixed) | (decide — see §1.3) | `0.0` / `1.0` | ✅ decided — bound opened |

### Other knobs (Priorities 2–5)

| Knob | Was | Target | **Now** | Status |
|---|---|---|---|---|
| `populationSize` | `8·D = 256` | `12·D = 384` | `dimensions * 12` = **384** | ✅ adopted; divisibility loop present |
| `penaltyRate` | `1.0` (six evaluators) | `0.01` | **`0.01`** (five evaluators) | ✅ adopted; evaluator list changed — see Priority 3 STATUS |
| `MutationForce` | `0.7` | `0.5` | **`0.5`** | ✅ adopted (seed value only — jDE self-adapts) |
| `CrossoverProbability` | `0.9` | `0.9` (keep) | **`0.9`** | ✅ kept as argued |
| `TerminationStrategy` | `Timeout(9h)` | `Or(Stagnation(5k, rel 1e-6), Timeout(9h))` | **exactly that** | ✅ adopted verbatim |
| *Strategy* (not in original scope) | `DE/rand/1/bin` only | — | **jDE**, selectable enum | ✅ Priority 6 landed |
| *Nelder–Mead refinement* (not proposed) | — | — | **final polish on, memetic off** | ✅ added beyond scope |

### Structural (Priority 1.3 — optional)

> **OBSOLETE — this table is void.** It assumes `[15]`/`[16]`/`[17]` are degenerate (LB = UB). All
> three now have open bounds and are live DE dimensions, so none can be collapsed to a `const` and
> the dimension reductions shown here are unattainable. D stays **18 / 32**. Retained only as the
> record of an option that was considered and rejected in favour of freeing the slots instead.

| Slot | Action | Effect |
|---|---|---|
| `[15]` APowOrder | ~~Move to `const = 1` in solver, drop from vector~~ | ~~D: 18 → 17 (single), 32 → 31 (group)~~ |
| `[16]` BPowOrder | ~~Move to `const = 2`~~ | ~~D: 17 → 16, 31 → 30~~ |
| `[17]` KCoefficientRadiationTemperature | Either move to `const = 0` or **open bound `[0, 1]`** ← taken | — |

## How to measure progress through the priorities

After each priority lands:

1. Reduce timeout to 3 minutes for the smoke run (same pattern as earlier diagnostic runs).
2. Build Release, run from `artifacts/bin/PastyPropellant.ConsoleApp/Release/net10.0/`.
3. Capture `(generation_count, final_F, final_penalty)` from `/tmp/pasty_run.log` or via the new CSV handler from §7.2.
4. Compare against the previous baseline. The order is chosen so each step's effect is measurable in isolation; do not combine multiple changes into one commit.

For the final cross-check, regenerate `propellants.group_optimization.report.en.pdf` and confirm the "Differential Evolution Algorithm" section reflects the new settings, and that "Functional Constraints" shows the new penalty rates.
