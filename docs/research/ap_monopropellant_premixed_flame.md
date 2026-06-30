# Fine-AP self-deflagration (monopropellant premixed flame) and the residual high-pressure
# under-prediction of the pure-fine composition

**Scope.** This note documents why — *after* the WSB condensed-phase closure
(`wsb_condensed_phase_closure.md`, Step 6) — the BDP-style competing-flames model in this repository
still **under-predicts the pure-fine-AP composition (Bas_3) at high pressure**, and specifies one
physically grounded, implementable fix: an **ammonium-perchlorate self-deflagration (monopropellant
premixed) flame** added to the pocket surface energy balance, weighted by the fine-AP surface fraction
and gated by the AP low-pressure deflagration limit. It adds exactly one new **shared** tunable
coefficient (no per-composition parameters), reduces exactly to the Step-6 model at the limit, and is
structurally safe for the surface-temperature bracketing root.

---

## 1. The residual after WSB (Step 6)

The WSB closure dropped the aggregated objective 0.02315 → **0.01709** (penalty 0). It lifted the
coarse Bas_4 to near-perfect across the whole range and the bimodal Bas_2 to within a few percent, and
the diffusion-standoff pressure term it displaced collapsed (C_rxn 50 → ~0). But the residual did not
vanish — it **relocated** almost entirely onto the **pure-fine composition Bas_3** at high pressure:

| segment ν (model) | 1→4.06 MPa | 4.06→6.5 MPa | experiment | 6.5 MPa error |
|---|---|---|---|---|
| Bas_2 (mixed)       | 0.592 | 0.353 | 0.52 | −4.5 % |
| **Bas_3 (fine only)** | 0.612 | **0.187** | 0.64 | **−18.5 %** |
| Bas_4 (coarse only) | 0.678 | 0.701 | 0.70 | −0.0 % |

The shared, pressure-only WSB completeness θ(p) lifts all three group fuels by the same pressure law and
**cannot isolate Bas_3**: raising K_wsb to lift Bas_3 would over-predict Bas_2 and Bas_4. The only handle
that distinguishes Bas_3 from its group-mates is **morphology** — Bas_3 is pure fine AP
(coarse fraction f_c = 0, d ≈ 50 µm), whereas Bas_4 is pure coarse (f_c = 1, d = 180 µm) and Bas_2 is
bimodal (f_c = 0.65).

## 2. Mechanism: fine AP burns as a monopropellant, not as a diffusion flame

In the BDP / Beckstead–Derr–Price picture the controlling flame structure is **size-dependent**:

- **Coarse AP** burns predominantly through the **AP/binder diffusion flame** (fuel and oxidiser
  initially separated; the rate is mixing-limited). This is the regime the model already captures well
  for Bas_4.
- **Fine AP** burns predominantly through the **AP self-deflagration (monopropellant) flame**: a
  *premixed*, homogeneous gas-phase flame of AP's own decomposition products sitting close to the
  surface. As the AP particle size shrinks, the diffusion flames merge and the surface increasingly
  behaves like burning AP monopropellant (Beckstead, Derr & Price 1970; Price 1984; Cohen 1980).

Two empirical facts about AP monopropellant deflagration are load-bearing here:

1. **It self-deflagrates only above a low-pressure deflagration limit** p_dl ≈ 2 MPa (~20 atm); below
   that, pure AP will not sustain a self-deflagration flame (Boggs 1970; Price, Boggs & Derr).
2. **Its burning rate follows r_AP ∝ p^n with n ≈ 0.77** between ~2 and ~10 MPa (Guirao & Williams 1971;
   Boggs 1970), and — being a premixed flame — it **does not saturate** with pressure the way a
   surface-attached diffusion flame does.

That non-saturating, pressure-driven premixed heat feedback is exactly the term the current closure
lacks for the fine limit. The model routes fine AP through the same diffusion + kinetic ensemble whose
heat flux saturates once the flame becomes surface-attached at high pressure (§2 of
`wsb_condensed_phase_closure.md`), so the calculated Bas_3 rate flattens (segment ν → 0.19) while the
real fine-AP rate keeps climbing (ν ≈ 0.64).

## 3. Implemented form (one shared parameter, reduces to the Step-6 baseline)

A parallel near-surface heat flux is added to the pocket total heat flux (alongside, *not inside*, the
bimodal-packing factor), weighted by the fine-AP surface fraction and gated by the AP deflagration limit:

```
q_AP = K_AP · (1 − f_c) · max(0, T_AP − T_s) · max(0, (p / p_dl)^n_AP − 1)
```

with the two physical anchors **fixed** (not fitted):

| symbol | value | source |
|---|---|---|
| p_dl  (AP low-pressure deflagration limit) | 2 MPa  | Boggs 1970; Price, Boggs & Derr |
| T_AP  (AP monopropellant flame temperature) | 1400 K | Boggs 1970; Price 1984 |
| f_c   (coarse-AP surface fraction)          | per-composition input (0.65 / 0 / 1 for Bas_2 / Bas_3 / Bas_4) |

and two new **shared** coefficients:

- **K_AP ≥ 0** — a lumped premixed-flame conductance λ_g/δ_AP [W·m⁻²·K⁻¹] (group vector index 37,
  single-composition index 23), bounds **[0, 5000]**. **K_AP = 0 ⇒ q_AP ≡ 0 ⇒ the Step-6 model exactly**
  (cannot regress the already-fit Bas_4 / Bas_2 / Bas_1 / Bas_0).
- **n_AP ∈ [0.77, 2.5]** — the AP self-deflagration pressure exponent in the gate (group vector index 38,
  single-composition index 24). The **floor 0.77** is the Guirao & Williams (1971) average over 20–100 atm and
  **reproduces the original fixed-exponent form** (Step 7); larger n_AP steepens the gate. See §3a below for why
  it was unfrozen.

Why each anchor matters:

- **(1 − f_c)** — only fine AP self-deflagrates; coarse AP is diffusion-controlled. So **coarse Bas_4
  (f_c = 1) receives exactly zero** (it stays near-perfect) and **pure-fine Bas_3 (f_c = 0) receives the
  full term** — precisely the morphological selectivity the residual demands. Bas_2 / Bas_0 / Bas_1
  (f_c = 0.65) receive the fine fraction 0.35, re-absorbed by their own fitted blocks as with K_pack.
- **The deflagration-limit gate** `max(0, (p/p_dl)^n_AP − 1)` vanishes at and below p_dl = 2 MPa, so the
  **already-good low-pressure (≤ 2 MPa) burn rate of Bas_3 is untouched**; the term switches on only in
  the saturated 4–6.5 MPa tail, which is where the residual lives. It then **rises monotonically with
  pressure (premixed, non-saturating)**, de-saturating the tail.
- **T_AP − T_s > 0** always on this branch (T_s ≤ 900 K < 1400 K), so q_AP > 0 and the
  surface-temperature binary-search root is preserved (no `−1 K` sentinel / fitness blow-up). q_AP
  decreases monotonically as T_s rises, so adding it leaves the energy balance monotone and well-bracketed.

## 3a. Step-7 result and why n_AP was unfrozen (Step 8)

The fixed-exponent form above (Step 7, K_AP only) dropped the aggregated objective **0.01709 → 0.01443**
(penalty 0). It did exactly what it was designed to do at the anchor points (calc vs experiment):

| | 6.5 MPa error | seg-ν 4.06→6.5 (calc / exp) |
|---|---|---|
| **Bas_3** (pure-fine, f_c = 0) | −18.5 % → **−11.0 %** | **0.187 → 0.340** / 0.642 |
| **Bas_4** (coarse, f_c = 1)     | −0.0 % → −1.6 %       | 0.657 / 0.702 (untouched, weight 0) |

So Bas_3 de-saturated and Bas_4 stayed near-perfect, **but two things blocked the < 0.01 target**:

1. **Bas_3 only rose half-way** (ν 0.34 vs 0.64). K_AP fitted to **4253** (85 % of its [0, 5000] range,
   pushing hard) — yet the **fixed** exponent n_AP = 0.77 makes the gate `(p/p_dl)^0.77 − 1` too gentle to
   concentrate the lift at 6.5 MPa without simultaneously lifting 4.06 MPa (already +2.6 % there). The
   binding constraint is the **gate shape**, not K_AP magnitude.
2. **A new low-pressure regression on the f_c = 0.65 family** (Bas_2 at 1 MPa: −6.6 % → −11.3 %). Because
   K_AP lifts the family's high pressure at weight 0.35, the optimizer **lowered the shared K_wsb
   (0.200 → 0.166)** to avoid over-predicting their high-pressure end — and that K_wsb drop pulled their
   **low-pressure** magnitude down (the deflagration gate is off below 2 MPa, so q_AP cannot compensate there).

Both pockets share one root: **the gate is too shallow.** Letting **n_AP float (floor 0.77)** steepens the
gate, concentrating Bas_3's lift in the 6.5 MPa tail without over-lifting 4.06 MPa — which (a) fully
de-saturates Bas_3 toward ν ≈ 0.64, and (b) **relieves the K_wsb suppression** so K_wsb can rise back and
restore the family's low-pressure magnitude. One parameter targets both pockets. Physically, the AP
self-deflagration burn-rate slope is **regime-dependent**, not a single power law across the full range
(Boggs 1970 reports a slope break / plateau near ~65 atm; Price 1984), so a fitted effective exponent in the
2–6.5 MPa band — bounded below by the 20–100 atm average 0.77 — is well grounded.

## 4. Code map

| Element | Location |
|---|---|
| Parallel-rate helper `r_AP` (Step 9) | `PocketPropellantSolver.GetApPremixedFlameBurnRate` (shared static, returns m·s⁻¹; both paths) |
| Added to final pocket rate (ByUnits) | `PocketPropellantSolver.Visit` (after `GetBurnRate`, before the volume-weighted combine) |
| Added to final pocket rate (ByDoubles) | `PocketPropellantSolver.Visit` (double path; identical numbers — §17.8 parity) |
| Regression combine | `MixedPropellantSolver.GetBurnRate` (volume-weighted; receives the AP-augmented pocket rate) |
| Parameter `a_AP` (rate scale, m·s⁻¹) | `CombustionSolverParams{ByDoubles,ByUnits}.KApPremixedFactor` (vector[23]) — repurposed from flux conductance |
| Parameter `n_AP` | `CombustionSolverParams{ByDoubles,ByUnits}.KApPressureExponent` (vector[24]) |
| Group plumbing | `GroupCombustionSolverParams.*` (a_AP vector[37]→index 23, n_AP vector[38]→index 24) |
| Bounds | `Program.GetLowerBound/GetUpperBound` [23] = [0, 0.05] (a_AP, m·s⁻¹), [24] = [0.77, 2.5]; group [37], [38] |

## 5. Success criterion

Model ν over 4.06→6.5 MPa for Bas_3 rises from 0.340 (Step 7) toward ~0.64 (high-pressure saturation fully
removed); the −11.0 % point at 6.5 MPa closes; the shared K_wsb recovers (≳ 0.20) so the Bas_2 1-MPa point
closes back toward ~0; group RMS falls below ~0.024 and the aggregated objective below 0.01, with penalty 0
and no `BurnRateIsFound = false` (no `−1 K`). Bas_4 must stay near-perfect (its (1 − f_c) weight is zero, so it
is structurally untouched). n_AP = 0.77 reproduces the Step-7 model exactly, so the lever cannot regress. If
n_AP rails at 2.5 and Bas_3 is still flat, the reserve lever is to let T_AP float, or to revisit whether q_AP
should be additive vs. directly gating the diffusion saturation.

## 6. Step-8 result and the flux→rate reformulation (Step 9)

Floating n_AP (Step 8) **confirmed the mechanism**: n_AP fitted to an **interior** 1.754 (not railed in
[0.77, 2.5]), the shared K_wsb **recovered** to 0.221, and the aggregated objective fell
**0.01443 → 0.01173** (penalty 0). But it did not reach < 0.01. The two pockets each improved ~1 point and
neither closed (calc vs experiment):

| | Step-7 | Step-8 | target |
|---|---|---|---|
| Bas_3 (pure-fine, f_c = 0) 6.5 MPa error | −11.0 % | **−10.0 %** | ~0 |
| └ seg-ν 4.06→6.5 (calc) | 0.340 | **0.378** | 0.642 |
| Bas_2 (mixed, f_c = 0.65) 1 MPa error | −11.3 % | **−9.4 %** | ~0 |

n_AP landing **interior** means the gate *shape* is already optimal — the additive-flux lever is exhausted.
Two ceilings remain:

1. **Within-group coupling.** One shared K_AP / n_AP serves Bas_3 (weight 1 − f_c = 1.0) and the f_c = 0.65
   family (weight 0.35, already +3 % at 4.06 MPa). K_AP cannot rise enough to lift Bas_3's tail without
   over-predicting Bas_2.
2. **The flux form self-throttles at high pressure.** `q_AP ∝ (T_AP − T_s)` is fed *into* the surface
   energy balance, whose solution is **saturated** at high p — Bas_3's T_s rises only 747 → 817 K over
   1 → 6.5 MPa while the burn rate triples. A heat flux fed into a saturated balance yields a diminishing
   rate response exactly where the steepening is needed, and `(T_AP − T_s)` itself shrinks as T_s rises.

**Step-9 reformulation (flux → parallel rate).** Replace the additive heat *flux* with a parallel
monopropellant burn *rate* added on top of the composite pocket regression:

> r_pocket,total = r_composite(T_s) + (1 − f_c) · a_AP · max(0, (p / p_dl)^n_AP − 1),  p_dl = 2 MPa,

which then flows through the volume-weighted regression combine (`MixedPropellantSolver.GetBurnRate`) to the
reported rate. Because the term is added **after** the surface energy balance (not inside it), it is **not
throttled by the high-pressure saturation** that flattened the flux form — a parallel surface patch of fine
AP self-deflagrating as a monopropellant adds mass flux *directly*. This is the Cohen–Strand "petite
ensemble" / BDP multiple-flame picture of co-existing surface flamelets combined by area/mass, rather than a
single lumped energy balance. The exponent n_AP is now **literally** the AP monopropellant burn-rate pressure
exponent (r_AP ∝ p^n_AP), whose 20–100 atm value 0.77 (Guirao–Williams 1971) is the physical floor; T_AP and
`(T_AP − T_s)` drop out entirely (one fewer fixed anchor). **a_AP = 0 reduces to the Step-6 model** (cannot
regress); f_c = 1 (Bas_4) still gets exactly zero.

Parameter `K_AP` (`KApPremixedFactor`, vector[23] / group[37]) is **repurposed** from a flux conductance
[W·m⁻²·K⁻¹, bounds 0–5000] to a **burn-rate scale a_AP [m·s⁻¹, bounds 0–0.05]**; the vector layout (39 / 25)
and n_AP (vector[24] / group[38], [0.77, 2.5]) are unchanged. Success: Bas_3 seg-ν 4.06→6.5 rises
0.378 → ~0.64, its 6.5 MPa point closes, group RMS < ~0.024, aggregated < 0.01, penalty 0, Bas_4 untouched.
The lever is added to the **pocket** regression (where the AP pockets are), bypassing only the saturating
energy balance; if the volume-weighted combine throttles it, the reserve is to add the parallel term at the
mixed-rate level instead.

## 7. Step-9 result and the slope-break onset (Step 10)

The parallel-rate reformulation **regressed**: aggregated **0.01173 (Step-8) → 0.01256 (Step-9)** at full
convergence (731k generations), penalty 0. Critically, **a_AP fitted to an interior 0.000219 m·s⁻¹ (0.4 % of
its [0, 0.05] range, not railed)** while n_AP railed toward 2.5 — the optimizer did **not** want more AP lift.
So the ceiling was **never magnitude** (neither the energy-balance saturation nor the volume-weighted
throttling): it is the **gate shape plus within-group coupling**. Bas_3's high-pressure tail stayed flat
(seg-ν 4.06→6.5 = 0.372 vs Step-8's 0.378; 6.5 MPa error −10.5 %).

The refined diagnosis is a **curvature** mismatch, not a magnitude deficit. Bas_3's experimental curve is a
clean ν ≈ 0.64 power law; the model already reproduces the **1→4 MPa** slope correctly (ν_calc ≈ 0.65) and
fails **only** on **4→6.5 MPa** (ν_calc ≈ 0.37). The AP gate onsets at the deflagration limit p_dl = 2 MPa, so
any monotonic gate term `(p/p_dl)^n_AP − 1` necessarily adds slope across the **whole** 2→6.5 band — including
the already-correct 2–4 MPa region. Steepening n_AP concentrates the lift toward 6.5 MPa but cannot remove the
contribution at 4 MPa, where Bas_3 is already +1.6 % and the f_c = 0.65 family is already +3 %. No additive
p-gated term (flux or rate) can add slope **only** in 4→6.5 from a 2 MPa onset.

**Step-10 (slope-break onset).** Revert to the Step-8 **flux** form (the best result, 0.01173) and replace the
**fixed** gate reference p_dl = 2 MPa with a **fitted shared break pressure p_break**, bounds **[2, 5] MPa**:

> q_AP = K_AP · (1 − f_c) · max(0, T_AP − T_s) · max(0, (p / p_break)^n_AP − 1),  p_break ∈ [2, 5] MPa.

The gate is identically zero at and below p_break, so a fitted p_break ≈ 4 MPa concentrates the entire AP
steepening in the **4→6.5 MPa** segment — exactly the failed segment — while leaving the already-correct
1→4 MPa region untouched. This is a genuinely **new shape degree of freedom** (onset *location*) that n_AP
(onset *steepness*) cannot supply, because in Steps 7–9 the onset was pinned at 2 MPa. It also helps the
f_c = 0.65 family at 6.5 MPa (Bas_2 is −3.2 % there) without disturbing 4.06 MPa, where the gate is now ≈ 0.
**Floor p_break = 2 MPa reproduces Step-8 exactly**, so Step-10's search space *contains* Step-8 and cannot
regress. Physical basis: ammonium-perchlorate self-deflagration is **two-regime** — a low-pressure
deflagration limit near 20 atm (2 MPa) and a distinct higher-pressure regime with a slope break / instability
plateau near ~65 atm / 6.5 MPa (Boggs 1970; Price 1984); the effective transition pressure of the fine-AP
fraction inside the composite is fitted between those two physical anchors. New shared parameter
`KApBreakPressure` (Pa) is appended at the tail: group vector[39] / single vector[25]; layout grows 39 → 40 /
25 → 26; K_AP and n_AP are restored to their Step-8 roles (flux conductance [0, 5000]; exponent [0.77, 2.5]).
Success: Bas_3 seg-ν 4.06→6.5 rises 0.378 → ~0.64, its −10 % point at 6.5 MPa closes, p_break lands ≈ 3.5–5 MPa
(if it stays at 2 MPa the lever added nothing), aggregated < 0.01, penalty 0, Bas_4/Bas_1/Bas_0 untouched.

## References

- M. W. Beckstead, R. L. Derr, C. F. Price, *A model of composite solid-propellant combustion based on
  multiple flames*, AIAA J. **8**(12):2200–2207, 1970. doi:10.2514/3.6087.
- N. S. Cohen, L. D. Strand, *An improved model for the combustion of AP composite propellants*, AIAA J.
  **20**(12):1739–1746, 1982. doi:10.2514/3.8014. (petite-ensemble / parallel-rate combination of
  co-existing surface flamelets — the basis for adding a parallel monopropellant rate rather than a flux)
- N. S. Cohen, *A pocket model for aluminum agglomeration in composite propellants*, AIAA J.
  **21**(5):720–725, 1983. doi:10.2514/3.8139. (size-class / pocket framework)
- T. L. Boggs, *Deflagration rate, surface structure, and subsurface profile of self-deflagrating single
  crystals of ammonium perchlorate*, AIAA J. **8**(5):867–873, 1970. doi:10.2514/3.5778.
- J. A. Guirao, F. A. Williams, *A model for ammonium perchlorate deflagration between 20 and 100 atm*,
  AIAA J. **9**(7):1345–1356, 1971. doi:10.2514/3.6360.
- C. F. Price, *Combustion of ammonium perchlorate–based composite propellants: experimental survey of
  the mechanisms*, in *Fundamentals of Solid-Propellant Combustion* (Prog. Astronaut. Aeronaut. 90),
  AIAA, 1984.
- M. J. Ward, S. F. Son, M. Q. Brewster, *Steady deflagration of HMX with simple kinetics: a gas phase
  chain reaction model*, Combust. Flame **114**(4):556–568, 1998. doi:10.1016/S0010-2180(97)00332-5.
