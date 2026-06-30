# Pressure-decaying bimodal-packing heat feedback and the low-pressure under-prediction of the
# bimodal composition (Step 11, a_pack)

**Scope.** This note documents why — *after* the AP self-deflagration premixed flame
(`ap_monopropellant_premixed_flame.md`, Steps 7–8) and the confirmed-null slope-break trial (Step 10) —
the BDP-style competing-flames model in this repository still leaves **two opposite-extreme residual
pockets** inside the Bas_2/3/4 group, and specifies one physically grounded, implementable fix for the
low-pressure pocket: a **pressure decay of the bimodal-packing heat-feedback enhancement**. It adds
exactly one new **shared** tunable coefficient `a_pack` (no per-composition parameters), reduces exactly
to the pressure-independent factor (the Step-8 model) at the limit, is **identically zero for the
monomodal compositions** (Bas_3 f_c=0, Bas_4 f_c=1), and does not touch the surface-temperature
bracketing root.

---

## 1. The residual after the AP-flame family (Steps 7–10)

The AP self-deflagration flame (Step 7, fixed n_AP=0.77) and its float (Step 8, n_AP fitted) brought the
aggregated objective to **0.011732** (penalty 0) — the campaign best. Two further AP-flame
reformulations were then trialled and **both failed to improve on Step 8**:

- **Step 9 (parallel rate).** Re-casting the AP term as a parallel monopropellant burn rate added on
  top of the harmonic pocket/inter-pocket combine *regressed* to 0.012563. The fitted rate scale landed
  interior (not railed) — the additive AP lever is **exhausted** as a magnitude knob.
- **Step 10 (fitted slope-break p_break).** Replacing the fixed 2 MPa deflagration-limit gate reference
  with a fitted `p_break ∈ [2,5] MPa` is a **confirmed null**: across a 3-run multi-restart the best
  escaped run reached 0.011864 (≈ Step 8) with **p_break = 2.38 MPa ≈ the 2 MPa floor** — the optimiser
  found no use for the degree of freedom, and Bas_3's high-pressure curvature was unchanged
  (segment-ν 4.06→6.5 MPa = 0.379 vs target 0.642; 6.5 MPa error −10.3%). The unused dimension also
  raised the jDE trap rate (2 of 3 cold runs collapsed to ~0.30).

The Step-10 best run gives a clean, fully-characterised residual map (all five fuels, three anchor
pressures; the entire error is in the Bas_2/3/4 group, with Bas_1 = 0.0022 and Bas_0 = 0.0007 perfect):

| composition | 1.0 MPa | 4.06 MPa | 6.5 MPa | character |
|-------------|--------:|---------:|--------:|-----------|
| **Bas_2** (bimodal, f_c=0.65) | **−9.4 %** | +3.0 % | −3.1 % | low-pressure magnitude deficit (+ a *slope* error) |
| **Bas_3** (pure fine, f_c=0)  | +0.3 %    | +1.5 % | **−10.3 %** | high-pressure curvature deficit |
| **Bas_4** (pure coarse, f_c=1)| +2.2 %    | +1.0 % | −2.0 % | near-perfect |

So the residual is **two pockets at opposite pressure extremes**: Bas_2 under-predicted at low
pressure, Bas_3 under-predicted at high pressure. This note addresses the **Bas_2 low-pressure** pocket;
the Bas_3 high-pressure pocket is discussed in §5.

---

## 2. Why the obvious shared levers cannot move these pockets

Two shared parameters sit **railed** at their bounds in *both* the Step-8 and Step-10 best vectors:

- **n_p = 4** (diffusion-standoff pressure exponent, cap 4), and
- **K_pack ≈ 8** (bimodal-packing factor, cap 10).

A railed parameter is the optimiser signalling a *starved* lever. But the railed-ness alone does not
tell us whether the lever has **leverage** on the failed points. The per-point flame-structure
decomposition in the report settles it.

**The diffusion flame is negligible at both pockets — n_p is a red herring.**
At Bas_3, 6.5 MPa the diffusion-flame heat flux is **216 kW·m⁻² out of a 23.2 MW·m⁻² total surface
flux — 0.9 %**. At Bas_2, 1 MPa it is **34 kW·m⁻² (0.2 %)**. The diffusion-standoff exponent n_p only
shapes this sub-1 % term, so raising its cap cannot move either pocket. (n_p is railed because it shapes
the pure-mode slopes elsewhere, not these points.)

**Both pockets are *skeleton-pathway* effects.** The pocket model evaluates two conditional pathways and
combines them through the volume-weighted **harmonic** (series) mean, which is dominated by the *slower*
pathway. The dominant slow pathway is the "Secondary Conditional Fuel" (skeleton kinetic flame +
condensed surface balance):

- *Bas_3 @ 6.5 MPa:* the Secondary pathway **saturates** — surface temperature is frozen
  (734→749.6→752.6 K over 1→4.06→6.5 MPa), its linear rate flattens (11.5→19.9→22.0 mm·s⁻¹) and its
  total surface flux is flat (2.09→2.35→2.32 MW·m⁻²); the skeleton kinetic flame is surface-attached
  (1.9 µm). The fast First-Conditional pathway (118 mm·s⁻¹) cannot rescue the harmonic mean.
- *Bas_2 @ 1 MPa:* the Secondary pathway is **too cool/slow** — its skeleton flame sits at only
  ~1158 K (vs Bas_3's 1679 K) and the rate is 8.2 mm·s⁻¹, dragging the harmonic mean below the
  experimental 16.94 mm·s⁻¹.

---

## 3. Physical mechanism: the leading-edge flame loses importance with pressure

The lever that *does* act on the bimodal compositions is the **bimodal-packing heat-feedback factor**
(`GetBimodalPackingFactor`, added Step 4):

```
q_surface  ←  q_surface · [ 1 + K_pack · f_c · (1 − f_c) ]
```

The bimodality measure `f_c·(1−f_c)` is **identically zero** for monomodal AP (Bas_3 f_c=0, Bas_4
f_c=1) and maximal for a balanced blend, so the factor enhances only the **bimodal** compositions
(Bas_2, and Bas_0/Bas_1 which are also f_c=0.65). Physically it represents the denser bimodal packing
producing more intense, closer near-surface flames (Miller 1982; Kubota, *Propellants and Explosives*).

The defect is that this factor is **pressure-independent**, so it cannot fix a pressure *slope* error —
and Bas_2 has exactly that: −9.4 % at 1 MPa but +3.0 % at 4.06 MPa. The physics says the enhancement
should **decay with pressure**. The bimodal enhancement is a **leading-edge flame (LEF)** effect: the
heterogeneous near-surface diffusion structure (the leading edges of the oxidizer/fuel flamelets over
the coarse particles) dominates the heat feedback only while the flames **stand off** the surface — at
low pressure. As pressure rises the flames collapse toward the surface, the standoff (∝ p⁻¹…p⁻²)
shrinks, and the near-surface LEF region loses its *relative* importance; the burn rate trends toward a
**plateau / regime change**. This is the standard explanation in the AP literature for plateau and
slope-break behaviour:

> *"A plateau at elevated pressures … related to the diminishing relative importance of the near-surface
> leading-edge region of the oxidizer/fuel diffusion flame in the gas-phase combustion zone."*

and the coarse/fine ratio and particle size set **where** in pressure this transition occurs (Combustion
and Flame 1987, 2015; Combustion, Explosion, and Shock Waves 2007). The leading-edge flame is the
Beckstead–Derr–Price multi-flame element whose standoff carries the pressure dependence (BDP 1970).

---

## 4. The fix (Step 11): pressure-decaying packing factor

Multiply the bimodal-packing enhancement by a decay in pressure:

```
factor = 1 + K_pack · f_c · (1 − f_c) · (p_ref / p)^a_pack ,   p_ref = 1 MPa
```

with one new **shared** fitted coefficient **`a_pack` = KBimodalPackingPressureExponent ≥ 0**.

**Properties (all load-bearing):**

1. **Targets the slope, not just the magnitude.** A factor large at low p and small at high p
   simultaneously **lifts** Bas_2 at 1 MPa and **relaxes** the +3.0 % over-prediction at 4.06 MPa — a
   correction the pressure-independent factor cannot make. This is the unique reason to prefer it over
   simply raising the K_pack cap (which lifts all pressures equally and would worsen the 4 MPa point).
2. **Cannot regress.** `a_pack = 0` ⇒ `(p_ref/p)^0 = 1` ⇒ exactly the pressure-independent factor (the
   Step-8 model). The Step-11 search space strictly contains Step-8, so the optimum cannot be worse.
3. **Isolated to the bimodal fuels.** `f_c·(1−f_c) = 0` for Bas_3 (f_c=0) and Bas_4 (f_c=1), so the new
   term is **identically zero** for the monomodal compositions — Bas_4's near-perfect fit and Bas_3's
   (separate) high-pressure problem are untouched. Bas_0/Bas_1 (f_c=0.65) receive the term but retain
   their own composition-specific parameters to re-absorb it (and a_pack=0 remains available to them).
4. **Bracket-safe.** The factor only scales an already-positive surface heat flux by a positive number;
   it does not change the sign structure of the surface energy balance, so the T_s bisection bracket is
   preserved (no `−1 K` sentinel / fitness blow-up).
5. **Physical range.** The LEF importance follows the diffusion standoff, which scales as p⁻¹…p⁻², so
   `a_pack ∈ [0, 2]` is the physically defensible bound.

**Implementation.** Repurposes the Step-10 tail slot (group vector[39] / single vector[25]); the AP-flame
gate reference is fixed back to the 2 MPa deflagration limit (Step-8). Vector stays 40 (group) / 26
(single). Both the ByUnits and ByDoubles paths call the single shared `GetBimodalPackingFactor` helper
(now taking pressure and a_pack), preserving exact parity. See `PocketPropellantSolver.cs`
(`GetBimodalPackingFactor`), `CombustionSolverParamsByUnits.cs`, `GroupCombustionSolverParams.cs`,
`Program.cs` (bound [25] = [0,2]), `GroupCombustionSolverParamsReport.cs`.

**Success diagnostic.** Bas_2 1 MPa error −9.4 % → ~0 **and** 4.06 MPa +3.0 % → ~0 (the whole Bas_2
slope corrected); a_pack lands interior in (0,2); group fitness 0.0327 → <0.0271 so the aggregate
objective crosses **< 0.01** with penalty 0; Bas_3/Bas_4/Bas_1/Bas_0 unchanged.

---

## 5. The remaining Bas_3 high-pressure pocket (out of scope here)

The Bas_3 6.5 MPa −10.3 % deficit is **not** addressable by this lever: Bas_3 is monomodal fine
(f_c=0), so the bimodal term is identically zero for it. It is the Secondary-pathway **saturation**
described in §2 — physically the AP self-deflagration slope-break that the literature places in our
pressure range for fine AP (finer AP ⇒ lower break pressure, post-break exponent > 1; AIAA J. Propulsion
& Power, very-high-pressure AP/HTPB burning rates). The AP-flame family (Steps 7–10) is the correct
physics for it but routes the heat through the *saturated* quasi-steady T_s balance and so cannot bite;
fixing it properly would require **re-routing** the AP flame outside the energy balance — a structural
change deferred pending the Step-11 outcome. If a_pack clears the aggregate < 0.01 on its own, the Bas_3
pocket may be left as an honest, well-characterised physical residual.

---

## 6. Result (Step 11, 2026-06-25)

The run **confirmed the diagnosis and cleared the target.** Aggregated objective **0.008300** (penalty 0) —
the first sub-1 % aggregate of the campaign (best-before was Step-8 0.011732). Per-composition: group
Bas_2/3/4 **0.021785** (down from 0.032617, below the 0.0271 threshold needed for aggregate < 0.01),
Bas_1 0.001996, Bas_0 0.001121.

- **a_pack = 0.2188 — interior** in [0,2] (a gentle decay, factor ∝ p^−0.22), not railed: the lever is a
  genuine optimum, not a boundary artefact. For Bas_2 the packing factor runs **3.28× at 1 MPa → 2.51× at
  6.5 MPa**, exactly the leading-edge-flame collapse of §3.
- **Bas_2 low-pressure slope corrected as designed** (exp/calc): 1 MPa −9.4 % → **−3.4 %** (undershoot more
  than halved), 4.06 MPa +3.0 % → **+1.5 %** — the whole slope, not just the magnitude.
- **Monomodal compositions untouched:** Bas_4 (coarse, f_c=1, term = 0) stays within ±1.8 %; Bas_1/Bas_0
  remain sub-1 % across the range. No `−1 K` sentinels (surface-heat-balance residuals ~10⁻²–10⁻⁴ W/m²).
- **K_pack railed at 10** and the dead diffusion-standoff exponent **n_p railed at 4** (multiplying a
  near-zero C_rxn, so degenerate) — expected; a_pack now supplies the pressure shape K_pack alone could not.

**The §5 Bas_3 6.5 MPa pocket persisted, as predicted.** The residual error migrated to the top of the
range: at 6.5 MPa, Bas_2 −6.3 % and Bas_3 −5.7 % (the fine-fraction compositions are marginally too flat
at the very top), while Bas_4 stays −1.8 %. The group RMS over all ten pressures nonetheless clears the
target, so this is left as the honest, well-characterised structural residual anticipated above — recorded
as known limitation #15 in `docs/math-model.md §17`. Best report archived at
`artifacts/bin/.../campaign_reports/STEP11_a_pack_BEST_obj0.0083_pen0_AGG_BELOW_0.01.pdf`.

---

## References

- **Beckstead, M. W., Derr, R. L., & Price, C. F. (1970).** "A model of composite solid-propellant
  combustion based on multiple flames." *AIAA Journal* 8(12), 2200–2207. doi:10.2514/3.6087.
  *(BDP multi-flame; the leading-edge flame element and its pressure-dependent standoff.)*
- **Ramakrishna, P. A., et al. / plateau-burning AP formulations (2007).** *Combustion, Explosion, and
  Shock Waves* 43(4), 436–. doi:10.1007/s10573-007-0059-5. *(Plateau trends vs AP particle size /
  coarse fraction; diminishing near-surface diffusion-flame importance at elevated pressure.)*
- **Particle size & plateau burning (1987).** *Combustion and Flame.* doi:10.1016/0010-2180(87)90099-X.
  *(Relationship between plateau behaviour and AP particle size in HTPB–AP propellants.)*
- **Coarse-to-fine ratio & microscale flame structure (2015).** *Combustion and Flame.*
  doi:10.1016/j.combustflame.2015.10.017. *(Bimodal flame structure; coarse particles and the fill
  region dominate the near-surface heat feedback.)*
- **Miller, R. R. (1982); Kubota, N., *Propellants and Explosives*.** *(Bimodal packing density and
  near-surface heat feedback — the original basis for the K_pack enhancement, Step 4.)*
- **Very-high-pressure burning rates of AP/HTPB composite propellants.** *Journal of Propulsion and
  Power.* doi:10.2514/1.B38173. *(Slope-break above the deflagration regime; finer AP lowers the break
  pressure — context for the separate Bas_3 high-pressure pocket, §5.)*
