"""Runs M and Q side by side against the 22-coefficient polynomial they were built to replace.

FOR PRESENTING THE CAMPAIGN, not for diagnosing one run — `plot_run_q_surface_fraction.py` does that.

The two runs share everything except the rule fixing the temperature the pocket equilibrium is
evaluated at:

    run M   T_pore = (T_s + T_flame,skeleton)/2   -> 921..1633 K, composition-dependent through the flame
    run Q   T_pore = T_s + 200                    -> 800..1054 K, composition-dependent through T_s only

Both use `f_s = n_C(s)(T_pore, p) / n_C,total` with ZERO fitted constants, against the polynomial's 22.
The reference curve is `PropellantExtensions.GetPocketSurfaceFraction`, which is
`sum(c_i (p/1e6)^i) / pocket_mass_fraction` and is byte-identical to the measured `Z_m^a(p)/Z_p`.

PANELS
  (a) f_s vs pressure         polynomial dotted, run M dashed, run Q solid.
  (b) ratio to the polynomial both runs, so the sign of each miss is visible.
  (c) both runs on ONE equilibrium curve at 1 MPa — the panel that carries the result. Where a run sat
      is set entirely by its pore rule; where it HAD to sit is set by the polynomial. Bas_0, Bas_1 and
      Bas_2 share one curve because their pocket matrices are identical.
  (d) the burn-rate objective, with the number of fitted constants each closure carries.

USAGE
  python3 plot_runs_m_q_vs_polynomial.py [output png]
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
    RUN_M,
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

# Aggregate objective and fitted-constant count, read from each run's own run.log at write time.
OBJECTIVES = [("run I\npolynomial", 0.0391309, 22, "#8c8c8c"),
              ("run M\nequilibrium", 0.0227826, 0, "#2c7fb8"),
              ("run P\nkinetic", 0.0272623, 3, "#c7a020"),
              ("run Q\nequilibrium", 0.0374050, 0, "#d95f0e")]


def surfaces(run: str) -> dict[tuple[str, int], float]:
    return {(p["name"], round(f["pressure"])): f["surface_temperature_pocket"]
            for p in load(os.path.join(run, "skeleton_layer.json")) for f in p["pressure_frames"]}


def coverage_reader(run: str):
    """(f_s at a surface temperature, pore temperature that produced it) from a run's own table."""
    table = load(os.path.join(run, "skeleton_carbon_equilibrium.json"))
    grid = table["surfaceTemperaturesKelvins"]
    frames = {(p["name"], round(f["pressure"])): f for p in table["propellants"] for f in p["frames"]}
    flame_rule = "poreTemperatureRule" not in table      # run M predates the recorded rule

    def read(name: str, pressure_key: int, surface: float) -> tuple[float, float]:
        frame = frames[(name, pressure_key)]
        values = frame["coverages"]
        index = max(0, min(len(grid) - 2, next((i for i in range(len(grid) - 1)
                                                if grid[i + 1] >= surface), len(grid) - 2)))
        span = grid[index + 1] - grid[index]
        coverage = values[index] + (values[index + 1] - values[index]) * (surface - grid[index]) / span
        pore = 0.5 * (surface + frame["skeletonFlameTemperature"]) if flame_rule \
            else surface + PORE_OFFSET_K
        return coverage, pore

    return read


def main() -> None:
    target = sys.argv[1] if len(sys.argv) > 1 \
        else os.path.join(REPO, "docs/results/runs_M_Q_vs_polynomial.png")

    propellants = {p["name"]: p for p in load(os.path.join(REPO, "data/propellants.01234.json"))}
    totals = {p["name"]: p["carbonAvailableMolesPerKilogram"]
              for p in load(os.path.join(REPO, "data/skeleton_carbon_equilibrium.json"))["propellants"]}
    assembled, _ = drop_non_converged(build_curves(), totals)

    runs = {"M": (surfaces(RUN_M), coverage_reader(RUN_M)),
            "Q": (surfaces(RUN_Q), coverage_reader(RUN_Q))}

    figure, ((upper_left, upper_right), (lower_left, lower_right)) = \
        plt.subplots(2, 2, figsize=(15.0, 10.2))
    upper_right.axhline(1.0, color="0.35", linestyle="--", linewidth=1.4, zorder=1)

    summary: dict[str, dict] = {}
    for name in sorted(propellants):
        propellant = propellants[name]
        pressures, reference = [], []
        values = {"M": [], "Q": []}
        pores = {"M": [], "Q": []}
        for frame in propellant["pressure_frames"]:
            pressure_key = round(frame["pressure"])
            pressures.append(frame["pressure"] / 1e6)
            reference.append(polynomial(propellant["pocket_surface_fraction_coefficients"],
                                        pressures[-1]) / propellant["pocket_mass_fraction"])
            for tag, (surface, read) in runs.items():
                coverage, pore = read(name, pressure_key, surface[(name, pressure_key)])
                values[tag].append(coverage)
                pores[tag].append(pore)

        needed = [t for t in roots(assembled[(name, 1000000)], reference[0] * totals[name])
                  if 700.0 <= t <= 1400.0]
        summary[name] = {"pressures": pressures, "reference": reference, "values": values,
                         "pores": pores, "needed": needed[0] if needed else None}

        colour, marker = COLOURS[name], MARKERS[name]
        upper_left.plot(pressures, reference, ":", marker=marker, markersize=6, linewidth=1.8,
                        color=colour, markerfacecolor="white", zorder=3)
        upper_left.plot(pressures, values["M"], "--", linewidth=1.7, color=colour, alpha=0.75, zorder=2)
        upper_left.plot(pressures, values["Q"], "-", marker=marker, markersize=6, linewidth=2.2,
                        color=colour, label=name, zorder=4)
        upper_right.plot(pressures, [v / r for v, r in zip(values["M"], reference)], "--",
                         linewidth=1.7, color=colour, alpha=0.75, zorder=2)
        upper_right.plot(pressures, [v / r for v, r in zip(values["Q"], reference)], "-",
                         marker=marker, markersize=6, linewidth=2.2, color=colour, zorder=3)

    # (c) One equilibrium curve per composition; both runs are points on it.
    for name in sorted(propellants):
        entry = summary[name]
        curve = [(t, v / totals[name]) for t, v in assembled[(name, 1000000)] if 700 <= t <= 1750]
        lower_left.plot([t for t, _ in curve], [v for _, v in curve], "-", linewidth=1.8,
                        color=COLOURS[name], alpha=0.85, label=name, zorder=2)
        lower_left.plot([entry["pores"]["M"][0]], [entry["values"]["M"][0]], MARKERS[name],
                        markersize=11, color=COLOURS[name], alpha=0.55, zorder=4)
        lower_left.plot([entry["pores"]["Q"][0]], [entry["values"]["Q"][0]], MARKERS[name],
                        markersize=11, color=COLOURS[name], zorder=5)
        if entry["needed"] is not None:
            lower_left.plot([entry["needed"]], [entry["reference"][0]], MARKERS[name], markersize=14,
                            color=COLOURS[name], markerfacecolor="white", markeredgewidth=2.0, zorder=6)

    labels = [row[0] for row in OBJECTIVES]
    lower_right.bar(labels, [row[1] for row in OBJECTIVES], color=[row[3] for row in OBJECTIVES],
                    width=0.62, zorder=3)
    for index, (_, objective, fitted, _colour) in enumerate(OBJECTIVES):
        lower_right.text(index, objective + 0.0012, f"{objective:.4f}", ha="center", fontsize=10,
                         fontweight="bold")
        lower_right.text(index, 0.0016, f"{fitted} fitted", ha="center", fontsize=9, color="white",
                         fontweight="bold")
    lower_right.axhline(OBJECTIVES[0][1], color="0.35", linestyle="--", linewidth=1.3, zorder=2)
    lower_right.text(3.42, OBJECTIVES[0][1] + 0.0009, "polynomial", fontsize=8.5, color="0.35",
                     ha="right")

    upper_left.set_xlabel("Pressure, MPa")
    upper_left.set_ylabel("$f_s$, share of pocket surface carrying skeleton")
    upper_left.set_title("(a) dotted: polynomial (22 fitted)   dashed: run M   solid: run Q",
                         fontsize=10.5)
    upper_left.grid(alpha=0.3)
    upper_left.legend(fontsize=8.5, ncol=2, loc="upper right")

    upper_right.set_xlabel("Pressure, MPa")
    upper_right.set_ylabel("closure / polynomial")
    upper_right.set_title("(b) run M (dashed) overshoots everywhere; run Q (solid) splits the sign",
                          fontsize=10.5)
    upper_right.grid(alpha=0.3)

    lower_left.set_xlabel("Pore temperature $T_{pore}$, K")
    lower_left.set_ylabel("$f_s = n_{C(s)}/n_{C,total}$")
    lower_left.set_title("(c) one equilibrium curve per composition, 1 MPa — faded: run M, "
                         "filled: run Q, hollow: the polynomial's target", fontsize=10.5)
    lower_left.grid(alpha=0.3)
    lower_left.legend(fontsize=8.5, ncol=2, loc="lower right")
    lower_left.annotate("Bas_0, Bas_1 and Bas_2 sit on ONE curve —\ntheir pocket matrices are identical. "
                        "The polynomial\nstill wants three different levels from them,\nand the only "
                        "lever either closure has is $T_s$.",
                        xy=(0.035, 0.965), xycoords="axes fraction", va="top", fontsize=8.5,
                        color="0.2", bbox=dict(boxstyle="round", facecolor="0.95", edgecolor="0.75"))

    lower_right.set_ylabel("Aggregate objective (lower is better)")
    lower_right.set_title("(d) burn-rate fit — the zero-fit closure still holds the record",
                          fontsize=10.5)
    lower_right.grid(alpha=0.3, axis="y")
    lower_right.set_ylim(0, 0.047)

    figure.suptitle("Replacing the 22-coefficient skeleton-coverage polynomial: runs M and Q",
                    fontsize=13.5)
    figure.tight_layout(rect=(0, 0, 1, 0.955))
    figure.savefig(target, dpi=150)

    print(f"{'comp':7}{'RMS run M':>11}{'RMS run Q':>11}   ratio at 1 MPa (M / Q / polynomial)")
    for name in sorted(propellants):
        entry = summary[name]
        rms = {}
        for tag in ("M", "Q"):
            errors = [v / r - 1.0 for v, r in zip(entry["values"][tag], entry["reference"])]
            rms[tag] = 100 * (sum(e * e for e in errors) / len(errors)) ** 0.5
        print(f"{name:7}{rms['M']:10.1f}%{rms['Q']:10.1f}%   "
              f"{entry['values']['M'][0]:.3f} / {entry['values']['Q'][0]:.3f} / "
              f"{entry['reference'][0]:.3f}")
    print(f"\nwritten: {target}")


if __name__ == "__main__":
    main()
