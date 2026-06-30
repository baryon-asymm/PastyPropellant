# Campaign reports — competing-flames model (group Bas_2/3/4)

One pinned PDF per **accepted** step of the campaign that drove the aggregated
objective from **0.0697 → 0.0083** (mean RMS relative burn-rate error vs Vieille
`A·p^ν`, 5 fuels × 10 pressures 1.0–6.5 MPa, penalty 0). Each report is the best
run of that step (jDE + Nelder–Mead polish, population = 12 × vector dim, ~4 h);
the `*.burntable.txt` sidecar holds the per-(fuel, pressure) burn-rate table for
that run. Full physical rationale and references are in
[`docs/research/`](../../research/) and [`docs/math-model.md`](../../math-model.md).

| Step | Report | agg | New shared parameter | math-model § |
|---|---|---|---|---|
| 1 | `diffmod_C_rxn_pop396_obj0.05828_pen0.pdf` | 0.0583 | `C_rxn` — diffusion-standoff pressure factor | §9.1 |
| 2 | `ensemble_m_pop408_obj0.05610_pen0.pdf` | 0.0561 | `m` — two-mode petite-ensemble size exponent | §9.2 |
| 4 | `packing_Kpack_pop420_obj0.02366_pen0.pdf` | 0.0237 | `K_pack` — bimodal-packing heat-feedback enhancement | §11.1.1 |
| 5 | `npexp_n_p_pop432_obj0.02315_pen0.pdf` | 0.02315 | `n_p` — regime-dependent standoff pressure exponent (dead lever, railed) | §9.1 |
| 6 | `wsb_Kwsb_pop444_obj0.01709_pen0.pdf` | 0.01709 | `K_wsb` — WSB condensed-phase reaction closure | §11.3 |
| 7 | `apflame_KAP_pop456_obj0.01443_pen0.pdf` | 0.01443 | `K_AP` — AP self-deflagration premixed flame | §11.5 |
| 8 | `apflame2_nAP_pop468_obj0.01173_pen0.pdf` | 0.01173 | `n_AP` — AP gate pressure exponent | §11.5 |
| 11 | `STEP11_a_pack_BEST_obj0.0083_pen0_AGG_BELOW_0.01.pdf` | **0.00830** | `a_pack` — bimodal-packing pressure decay (LEF collapse) | §11.1.1 |

The Step-11 report is also pinned as the canonical
[`../propellants.group_optimization.report.en.pdf`](../propellants.group_optimization.report.en.pdf).

**Per-context at the final (Step-11) best:** group Bas_2/3/4 = 0.0218, Bas_1 = 0.0020,
Bas_0 = 0.0011 → aggregate 0.00830. `a_pack = 0.219` (interior); Bas_2 low-pressure
slope corrected (1 MPa −9.4 % → −3.4 %); residual now at 6.5 MPa for the f_c<1
compositions (−5…−6 %), recorded as `docs/math-model.md` §17 limitation #15.

## Not included here (intentionally)

Reverted or null steps live only in the gitignored run area and are **not** part of
the model: Step 3 (surface-area ensemble weighting, agg 0.0630), Step 9 (parallel-rate
AP reformulation, agg 0.01256), Step 10 (fitted Boggs `p_break` slope-break, confirmed
null) — the AP-additive family is exhausted at agg ≈ 0.012. Optimizer-selection runs
(jDE/JADE) and degenerate-trap restarts are likewise excluded.

> **Thermal-bound note.** These results are on the `surface-900-metal-1300` experiment
> branch (T_s bound 900 K, T_metal 1300 K), which deviates from the Babuk & Burachek
> 2025 paper baseline (T_s 600–750 K, T_metal ~2300 K).
