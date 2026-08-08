"""Run Q's skeleton surface fraction against the polynomial closure it was built to replace.

WHAT THE REFERENCE CURVE IS.  The comparison is against the POLYNOMIAL closure — what
`SkeletonSurfaceFractionMode.Polynomial` feeds the solver, i.e. `PropellantExtensions
.GetPocketSurfaceFraction`, which is `sum(c_i * (p/1e6)^i) / PocketMassFraction` over the 22 shipped
`pocket_surface_fraction_coefficients`.  Those coefficients are byte-identical to Babuk's measured
agglomeration curve (proof in `analyze_babuk_agglomeration_data.py`), so this single curve is
simultaneously the historical closure and the measurement — there is no second reference to draw.
Run M is deliberately NOT plotted: it is the same closure family as run Q under a different pore rule,
and comparing two variants of the thing being tested says nothing about whether either replaces the
polynomial.


WHY THIS EXISTS.  Run Q's own `skeleton_surface_fraction_plot.png` shows the INPUT polynomial, not the
closure the run was configured with — the plot helper renders `agglomeration_coefficients /
pocket_mass_fraction` whatever `model.skeletonSurfaceFraction.mode` says (report defect #11).  Run Q used
`EquilibriumCarbon` with the pore rule `T_pore = T_s + 200`, so the curve it solved with is not the curve
that was drawn.

WHAT RUN Q's f_s IS.  Unlike the polynomial and the kinetic closures, this one is not a function of
pressure alone: `f_s = n_C(s)(T_pore, p) / n_C,total` with `T_pore = T_s + 200`, and `T_s` is an OUTPUT of
the surface-temperature bisection.  So the value plotted is the run's own table interpolated at the run's
own converged surface temperatures, read from `skeleton_layer.json` — not recomputed and not re-fitted.

THE FOUR PANELS
  (a) f_s vs pressure      run Q solid, polynomial dotted.
  (b) run Q / polynomial   the same information as a residual.  The sign of the miss is not the same for
                           all five, which is what makes it unfixable by any rigid shift of the family.
  (c) THE MECHANISM        f_s along the equilibrium curve itself at 1 MPa, with a filled marker where run Q
                           landed and a hollow star where it had to land to match the polynomial.  Bas_0,
                           Bas_1 and Bas_2 share one curve — identical pocket matrices — so their markers
                           sit on the same line and the spread between them is pure T_s.
  (d) T_s vs pressure      what the solver converged to, against what was required.  This is the panel that
                           carries the result: the arrows in (c) point in incompatible directions, and (d)
                           shows the solver did not move at all.

USAGE
  python3 plot_run_q_surface_fraction.py [output png]
"""
from __future__ import annotations

import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

REPO = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, REPO)

from invert_pore_temperature import (  # noqa: E402
    build_curves,
    drop_non_converged,
    load,
    polynomial,
    roots,
)

RUN_Q = "/root/runs/3h-Classic-Tm2300-kc400-constrON-fsEquilibrium-TporeTs200-20260807-1132"
PORE_OFFSET_K = 200.0
COLOURS = {"Bas_0": "#1f77b4", "Bas_1": "#d62728", "Bas_2": "#2ca02c",
           "Bas_3": "#9467bd", "Bas_4": "#8c564b"}
MARKERS = {"Bas_0": "o", "Bas_1": "s", "Bas_2": "^", "Bas_3": "D", "Bas_4": "*"}


def surfaces(run: str) -> dict[tuple[str, int], float]:
    return {(p["name"], round(f["pressure"])): f["surface_temperature_pocket"]
            for p in load(os.path.join(run, "skeleton_layer.json")) for f in p["pressure_frames"]}


def table_interpolator(run: str):
    """f_s(name, pressure, T_s) from a run's own equilibrium table, by linear interpolation in T_s."""
    table = load(os.path.join(run, "skeleton_carbon_equilibrium.json"))
    grid = table["surfaceTemperaturesKelvins"]
    coverages = {(p["name"], round(f["pressure"])): f["coverages"]
                 for p in table["propellants"] for f in p["frames"]}

    def at(name: str, pressure_key: int, surface: float) -> float:
        values = coverages[(name, pressure_key)]
        index = max(0, min(len(grid) - 2, next((i for i in range(len(grid) - 1)
                                                if grid[i + 1] >= surface), len(grid) - 2)))
        span = grid[index + 1] - grid[index]
        return values[index] + (values[index + 1] - values[index]) * (surface - grid[index]) / span

    return at


def main() -> None:
    target = sys.argv[1] if len(sys.argv) > 1 \
        else os.path.join(REPO, "docs/results/run_q_surface_fraction.png")

    propellants = {p["name"]: p for p in load(os.path.join(REPO, "data/propellants.01234.json"))}
    totals = {p["name"]: p["carbonAvailableMolesPerKilogram"]
              for p in load(os.path.join(REPO, "data/skeleton_carbon_equilibrium.json"))["propellants"]}
    assembled, _ = drop_non_converged(build_curves(), totals)

    q_surface, q_at = surfaces(RUN_Q), table_interpolator(RUN_Q)

    figure, ((upper_left, upper_right), (lower_left, lower_right)) = \
        plt.subplots(2, 2, figsize=(14.5, 10.0))
    upper_right.axhline(1.0, color="0.45", linestyle="--", linewidth=1.2, zorder=1)

    series: dict[str, dict[str, list[float]]] = {}
    for name in sorted(propellants):
        propellant = propellants[name]
        pressures, run_q, reference = [], [], []
        required_surface, required_pore = [], []
        for frame in propellant["pressure_frames"]:
            pressure_key = round(frame["pressure"])
            megapascals = frame["pressure"] / 1e6
            pressures.append(megapascals)
            # Exactly what GetPocketSurfaceFraction returns — the polynomial closure, not a rescaling
            # of it, so the two curves on the panel are the two closures as the solver sees them.
            reference.append(polynomial(propellant["pocket_surface_fraction_coefficients"],
                                        megapascals) / propellant["pocket_mass_fraction"])
            run_q.append(q_at(name, pressure_key, q_surface[(name, pressure_key)]))

            # Where the pore had to be for the measurement to come out, on the assembled 33-point
            # curve rather than on the run's own 5-point table — the roots are read, not fitted.
            found = [t for t in roots(assembled[(name, pressure_key)], reference[-1] * totals[name])
                     if 700.0 <= t <= 1300.0]
            required_pore.append(found[0] if found else None)
            required_surface.append(found[0] - PORE_OFFSET_K if found else None)

        series[name] = {"pressures": pressures, "runQ": run_q, "reference": reference,
                        "requiredSurface": required_surface, "requiredPore": required_pore,
                        "surface": [q_surface[(name, round(f["pressure"]))]
                                    for f in propellant["pressure_frames"]]}

        colour, marker = COLOURS[name], MARKERS[name]
        upper_left.plot(pressures, run_q, "-", marker=marker, markersize=7, linewidth=2.2,
                        color=colour, label=name, zorder=4)
        upper_left.plot(pressures, reference, ":", marker=marker, markersize=6, linewidth=1.8,
                        color=colour, markerfacecolor="white", zorder=3)
        upper_right.plot(pressures, [q / r for q, r in zip(run_q, reference)], "-", marker=marker,
                         markersize=6, linewidth=2.0, color=colour, zorder=3)

        lower_right.plot(pressures, series[name]["surface"], "-", marker=marker, markersize=6,
                         linewidth=2.0, color=colour, label=name, zorder=4)
        lower_right.plot(pressures, required_surface, ":", marker=marker, markersize=6,
                         linewidth=1.6, color=colour, markerfacecolor="white", zorder=3)

    # (c) The equilibrium curve itself at 1 MPa, with where the run sat and where it had to sit.
    for name in sorted(propellants):
        curve = [(t, v / totals[name]) for t, v in assembled[(name, 1000000)] if 700 <= t <= 1300]
        lower_left.plot([t for t, _ in curve], [v for _, v in curve], "-", linewidth=1.8,
                        color=COLOURS[name], alpha=0.85, label=name, zorder=2)
        landed = series[name]["surface"][0] + PORE_OFFSET_K
        lower_left.plot([landed], [series[name]["runQ"][0]], MARKERS[name], markersize=11,
                        color=COLOURS[name], zorder=5)
        needed = series[name]["requiredPore"][0]
        if needed is not None:
            lower_left.plot([needed], [series[name]["reference"][0]], MARKERS[name], markersize=13,
                            color=COLOURS[name], markerfacecolor="white", markeredgewidth=1.8, zorder=5)
            lower_left.annotate("", xy=(needed, series[name]["reference"][0]),
                                xytext=(landed, series[name]["runQ"][0]),
                                arrowprops=dict(arrowstyle="->", color=COLOURS[name],
                                                linewidth=1.4, alpha=0.75), zorder=4)

    upper_left.set_xlabel("Pressure, MPa")
    upper_left.set_ylabel("$f_s$, share of pocket surface carrying skeleton")
    upper_left.set_title("(a) solid: run Q, equilibrium carbon at $T_{pore}=T_s+200$   "
                         "dotted: polynomial closure (= measured $Z_m^a/Z_p$)", fontsize=10.5)
    upper_left.grid(alpha=0.3)
    upper_left.legend(fontsize=8.5, ncol=2, loc="upper right")

    upper_right.set_xlabel("Pressure, MPa")
    upper_right.set_ylabel("run Q / polynomial")
    upper_right.set_title("(b) residual — Bas_0 high by 2–3×, the other four low by 1.1–2×; "
                          "the sign differs", fontsize=10.5)
    upper_right.grid(alpha=0.3)

    lower_left.set_xlabel("Pore temperature $T_{pore}$, K")
    lower_left.set_ylabel("$f_s = n_{C(s)}/n_{C,total}$")
    lower_left.set_title("(c) the equilibrium curve at 1 MPa — filled: where run Q landed, "
                         "hollow: where it had to land", fontsize=10.5)
    lower_left.grid(alpha=0.3)
    lower_left.legend(fontsize=8.5, ncol=2, loc="upper left")
    # The whole result of the run, stated on the panel that shows it.
    lower_left.annotate("Bas_0, Bas_1, Bas_2 share one curve\n(identical pocket matrices) — the spread\n"
                        "between their markers is $T_s$ alone.\nThe arrows point in incompatible\n"
                        "directions: Bas_0 is the hottest surface\nand needs the coldest pore.",
                        xy=(0.97, 0.03), xycoords="axes fraction", ha="right", va="bottom",
                        fontsize=8.5, color="0.2",
                        bbox=dict(boxstyle="round", facecolor="0.95", edgecolor="0.75"))

    lower_right.set_xlabel("Pressure, MPa")
    lower_right.set_ylabel("Pocket surface temperature $T_s$, K")
    lower_right.set_title("(d) solid: what run Q converged to   "
                          "dotted: what the polynomial's $f_s$ required", fontsize=10.5)
    lower_right.grid(alpha=0.3)
    lower_right.axhspan(600, 900, color="0.9", zorder=0)
    lower_right.text(1.05, 878, "solver bracket 600–900 K", fontsize=8, color="0.4")
    # No legend here: the colour code is identified in (a) and (c), and a legend in this panel can
    # only sit on top of the curves that carry the result.

    figure.suptitle("Run Q skeleton surface fraction against the 22-coefficient polynomial it replaces, "
                    "and why the gap could not close", fontsize=13)
    figure.tight_layout(rect=(0, 0, 1, 0.955))
    figure.savefig(target, dpi=150)
    write_table(series, os.path.splitext(target)[0] + ".md")
    print(f"\nwritten: {target}")


def write_table(series, path) -> None:
    names = sorted(series)
    pressures = series[names[0]]["pressures"]
    lines = [
        "# Skeleton surface fraction in run Q, against the polynomial closure it replaces",
        "",
        "`run Q` — `f_s = n_C(s)(T_pore, p) / n_C,total`, `T_pore = T_s + 200 K`, **zero fitted "
        "constants**. `T_s` is the run's own converged pocket surface temperature, read from "
        "`skeleton_layer.json` — so this is the f_s the solver actually used, not a recomputation.",
        "",
        "`polynomial` — `PropellantExtensions.GetPocketSurfaceFraction`, i.e. "
        "`sum(c_i (p/1e6)^i) / pocket_mass_fraction` over the **22 shipped coefficients**. Those "
        "coefficients are byte-identical to Babuk's measured `Z_m^a(p)/Z_p`, so this column is at once "
        "the historical closure and the measurement.",
        "",
        "| p, MPa | " + " | ".join(f"{n} run Q | {n} polynomial | ratio" for n in names) + " |",
        "|---:|" + "---:|" * (3 * len(names)),
    ]
    for index, pressure in enumerate(pressures):
        cells = []
        for name in names:
            computed, reference = series[name]["runQ"][index], series[name]["reference"][index]
            cells.append(f"{computed:.4f} | {reference:.4f} | {computed / reference:.3f}")
        lines.append(f"| {pressure:.2f} | " + " | ".join(cells) + " |")

    lines += ["", "| composition | T_s converged, K | T_s required, K | mean ratio | RMS relative error |",
              "|---|---:|---:|---:|---:|"]
    for name in names:
        entry = series[name]
        ratios = [c / r for c, r in zip(entry["runQ"], entry["reference"])]
        rms = (sum((r - 1.0) ** 2 for r in ratios) / len(ratios)) ** 0.5
        required = [t for t in entry["requiredSurface"] if t is not None]
        span = f"{min(required):.0f}–{max(required):.0f}" if required else "—"
        lines.append(f"| {name} | {min(entry['surface']):.0f}–{max(entry['surface']):.0f} | {span} | "
                     f"{sum(ratios) / len(ratios):.3f} | {100 * rms:.1f} % |")

    with open(path, "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines) + "\n")
    print("\n".join(lines))
    print(f"\nwritten: {path}")


if __name__ == "__main__":
    main()
