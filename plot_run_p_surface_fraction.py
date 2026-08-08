"""The skeleton surface fraction run P actually used, against the measurement and against run M.

WHY THIS EXISTS.  The run directory already contains `skeleton_surface_fraction_plot.png`, and for this
run it is misleading: the plot helper renders `agglomeration_coefficients / pocket_mass_fraction` - the
INPUT polynomial - whatever `model.skeletonSurfaceFraction.mode` is set to.  Run P was configured with
`KineticCoverage`, so the curve the solver used is not the curve that was drawn.  Two checks settle it:
the helper shows Bas_4 at 0.528 falling slightly, while the kinetic closure gives Bas_4 a constant
1/(1+a0) = 0.5009 because its pocket holds no fine oxidiser at all; and it shows Bas_3 at 0.313 against
the closure's 0.333 at 1 MPa.

WHAT IS PLOTTED, all three from the same definition f_s = share of pocket surface carrying skeleton:
    run P      f_s = 1 / (1 + a0 + a1 * w_fine * (p/p_ref)^m), constants read from the run's own
               resolved configuration record rather than retyped
    measured   f_s = Z_m^a(p) / Z_pocket, i.e. the shipped polynomial - which IS Babuk's measured
               agglomeration curve (see analyze_babuk_agglomeration_data.py for the proof)
    run M      the equilibrium closure at run M's own converged surface temperatures, for context on
               what the previous best run was using

USAGE
  python plot_run_p_surface_fraction.py [output png]
"""
from __future__ import annotations

import json
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

REPO = os.path.dirname(os.path.abspath(__file__))
RUN_P = os.path.join(REPO, "docs/results/campaign")
RUN_M = "/root/runs/6h-Classic-Tm2300-kc400-constrON-fsEquilibrium-20260803-0118"
COLOURS = {"Bas_0": "#1f77b4", "Bas_1": "#d62728", "Bas_2": "#2ca02c",
           "Bas_3": "#9467bd", "Bas_4": "#8c564b"}
MARKERS = {"Bas_0": "o", "Bas_1": "s", "Bas_2": "^", "Bas_3": "D", "Bas_4": "*"}


def load(path):
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def polynomial(coefficients, pressure_mpa):
    return sum(c * pressure_mpa ** i for i, c in enumerate(coefficients))


def write_table(series, path) -> None:
    """The same numbers as the figure, as a table — the figure shows the shape, the table the values."""
    names = sorted(series)
    pressures = series[names[0]]["pressures"]
    lines = [
        "# Skeleton surface fraction: run P against the measurement",
        "",
        "`run P` — the kinetic-coverage closure the run was configured with, "
        "`f_s = 1/(1 + a0 + a1*w_fine*(p/p_ref)^m)`, constants read from the run's own resolved record "
        "(a0 = 0.9966, a1 = 3.3653, m = 0.71, p_ref = 1 MPa).",
        "",
        "`measured` — `Z_m^a(p)/Z_pocket`, Babuk's measured agglomeration curve, which is what the "
        "shipped polynomial reproduces to <= 0.023 over 20 published points.",
        "",
        "Bas_2, Bas_3 and Bas_4 are the calibration set. Bas_0 and Bas_1 carry a different binder and "
        "the closure is **not** calibrated for them; their columns are a prediction, not a fit.",
        "",
        "| p, MPa | " + " | ".join(f"{n} run P | {n} measured | ratio" for n in names) + " |",
        "|---:|" + "---:|" * (3 * len(names)),
    ]
    for index, pressure in enumerate(pressures):
        cells = []
        for name in names:
            computed = series[name]["runP"][index]
            measured = series[name]["measured"][index]
            cells.append(f"{computed:.4f} | {measured:.4f} | {computed / measured:.3f}")
        lines.append(f"| {pressure:.2f} | " + " | ".join(cells) + " |")

    lines += ["", "| composition | w_fine | mean ratio | worst ratio | RMS relative error |",
              "|---|---:|---:|---:|---:|"]
    for name in names:
        ratios = [c / m for c, m in zip(series[name]["runP"], series[name]["measured"])]
        errors = [r - 1.0 for r in ratios]
        rms = (sum(e * e for e in errors) / len(errors)) ** 0.5
        worst = max(ratios, key=lambda r: abs(r - 1.0))
        lines.append(f"| {name} | {series[name]['fine']:.3f} | {sum(ratios) / len(ratios):.3f} | "
                     f"{worst:.3f} | {100 * rms:.1f} % |")

    with open(path, "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines) + "\n")
    print("\n".join(lines))
    print(f"\nwritten: {path}")


def main() -> None:
    target = sys.argv[1] if len(sys.argv) > 1 \
        else os.path.join(REPO, "docs/results/run_p_surface_fraction.png")

    resolved = load(os.path.join(RUN_P, "runP-fsKinetic-20260806.run_configuration.resolved.json"))
    kinetic = resolved["configuration"]["model"]["skeletonSurfaceFraction"]["kinetic"]
    binder = kinetic["binderChannel"]
    fine_channel = kinetic["fineOxidiserChannel"]
    order = kinetic["pressureOrder"]
    reference = kinetic["referencePressurePascals"]

    propellants = {p["name"]: p for p in load(os.path.join(REPO, "data/propellants.01234.json"))}

    # Run M's equilibrium coverage, evaluated at the surface temperatures run M itself converged to.
    table = load(os.path.join(RUN_M, "skeleton_carbon_equilibrium.json"))
    grid = table["surfaceTemperaturesKelvins"]
    coverages = {(p["name"], round(f["pressure"])): f["coverages"]
                 for p in table["propellants"] for f in p["frames"]}
    surfaces = {(p["name"], round(f["pressure"])): f["surface_temperature_pocket"]
                for p in load(os.path.join(RUN_M, "skeleton_layer.json"))
                for f in p["pressure_frames"]}

    def run_m_coverage(name, pressure_key):
        values = coverages[(name, pressure_key)]
        surface = surfaces[(name, pressure_key)]
        index = max(0, min(len(grid) - 2, next((i for i in range(len(grid) - 1)
                                                if grid[i + 1] >= surface), len(grid) - 2)))
        span = grid[index + 1] - grid[index]
        return values[index] + (values[index + 1] - values[index]) * (surface - grid[index]) / span

    figure, (upper, lower) = plt.subplots(2, 1, figsize=(11.5, 9.2), sharex=True,
                                          gridspec_kw={"height_ratios": [2.0, 1.0]})
    lower.axhline(1.0, color="0.45", linestyle="--", linewidth=1.2, zorder=1)

    series: dict[str, dict[str, list[float]]] = {}
    for name in sorted(propellants):
        propellant = propellants[name]
        ap = propellant["components"]["AmmoniumPerchlorate"]
        fine = ap["mass_fraction"] * (1.0 - ap["large_particles_fraction"])
        pocket_mass = propellant["pocket_mass_fraction"]

        pressures, computed, measured, previous = [], [], [], []
        for frame in propellant["pressure_frames"]:
            pressure = frame["pressure"]
            megapascals = pressure / 1e6
            pressures.append(megapascals)
            computed.append(1.0 / (1.0 + binder + fine_channel * fine
                                   * (pressure / reference) ** order))
            measured.append(polynomial(propellant["pocket_surface_fraction_coefficients"],
                                       megapascals) / pocket_mass)
            previous.append(run_m_coverage(name, round(pressure)))
        series[name] = {"pressures": pressures, "runP": computed, "measured": measured,
                        "runM": previous, "fine": fine}

        colour, marker = COLOURS[name], MARKERS[name]
        upper.plot(pressures, computed, "-", marker=marker, markersize=7, linewidth=2.2,
                   color=colour, label=name, zorder=4)
        upper.plot(pressures, measured, ":", marker=marker, markersize=5, linewidth=1.4,
                   color=colour, markerfacecolor="white", zorder=3)
        upper.plot(pressures, previous, "--", linewidth=1.2, color=colour, alpha=0.55, zorder=2)
        lower.plot(pressures, [c / m for c, m in zip(computed, measured)], "-", marker=marker,
                   markersize=6, linewidth=2.0, color=colour, zorder=3)

    upper.set_ylabel("$f_s$, share of pocket surface carrying skeleton")
    upper.set_title("Skeleton surface fraction: what run P used, what was measured, what run M used\n"
                    "solid: run P, kinetic coverage   dotted: measured $Z_m^a/Z_p$   "
                    "dashed: run M, equilibrium carbon", fontsize=12)
    upper.grid(alpha=0.3)
    upper.legend(loc="center left", bbox_to_anchor=(1.01, 0.5), fontsize=9, title="composition")

    lower.set_xlabel("Pressure, MPa")
    lower.set_ylabel("run P / measured")
    lower.grid(alpha=0.3)
    lower.text(1.05, 1.03, "exact agreement", fontsize=8.5, color="0.35")
    # Not overlapping curves but one curve: the closure sees the recipe only through w_fine, and
    # Bas_0, Bas_1 and Bas_2 share it exactly. Their measured curves do not coincide, which is the
    # limitation the report block spells out and the lower panel makes visible.
    upper.annotate("Bas_0, Bas_1, Bas_2 coincide —\nthe closure sees only $w_{fine}$,\n"
                   "and theirs is identical (0.105)",
                   xy=(2.22, 0.382), xytext=(2.7, 0.60), fontsize=8.5, color="0.25",
                   arrowprops=dict(arrowstyle="->", color="0.45", linewidth=0.9))

    figure.tight_layout()
    figure.savefig(target, dpi=150)

    write_table(series, os.path.splitext(target)[0] + ".md")
    print(f"\nwritten: {target}")
    print(f"constants read from the resolved record: a0 = {binder}, a1 = {fine_channel}, "
          f"m = {order}, p_ref = {reference / 1e6:.0f} MPa")


if __name__ == "__main__":
    main()
