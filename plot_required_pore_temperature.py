"""The pore temperature the measured coverage demands, against the ones the rules deliver.

One panel, one curve per composition - the compositions are the series, so nothing about the plot
depends on how many propellants the file happens to hold.

    solid + filled markers   T_pore that makes run M's closure, f_s = n_C(s)/n_C,total, reproduce
                             Babuk's measured agglomeration curve
    open markers             the two points that have NO root: the target is above the ceiling of the
                             whole curve, so what is plotted is the ceiling - the temperature of the
                             maximum, and the closest the closure can come at any temperature
    dashed                   T_pore run M actually used, (T_s + T_flame,skeleton)/2
    shaded band              T_pore the shipped rule produces, (T_s + T_m)/2 over the 600..900 K
                             surface-temperature bracket at T_m = 2300 K

The gap between the solid curves and everything else is the result: the measurement wants a pore
several hundred kelvin colder than either rule delivers.

USAGE
  python plot_required_pore_temperature.py [output png]
"""
from __future__ import annotations

import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from invert_pore_temperature import (  # noqa: E402
    METAL_MELTING_K,
    RUN_M,
    build_curves,
    drop_non_converged,
    load,
    polynomial,
    roots,
)

REPO = os.path.dirname(os.path.abspath(__file__))
BRACKET = (600.0, 900.0)
COLOURS = {"Bas_0": "#1f77b4", "Bas_1": "#d62728", "Bas_2": "#2ca02c",
           "Bas_3": "#9467bd", "Bas_4": "#8c564b"}
MARKERS = {"Bas_0": "o", "Bas_1": "s", "Bas_2": "^", "Bas_3": "D", "Bas_4": "*"}


def main() -> None:
    target_path = sys.argv[1] if len(sys.argv) > 1 \
        else os.path.join(REPO, "docs/results/required_pore_temperature.png")

    shipped = load(os.path.join(REPO, "data", "skeleton_carbon_equilibrium.json"))
    totals = {p["name"]: p["carbonAvailableMolesPerKilogram"] for p in shipped["propellants"]}
    flames = {(p["name"], round(f["pressure"])): f["skeletonFlameTemperature"]
              for p in load(os.path.join(RUN_M, "skeleton_carbon_equilibrium.json"))["propellants"]
              for f in p["frames"]}
    surfaces = {(p["name"], round(f["pressure"])): f["surface_temperature_pocket"]
                for p in load(os.path.join(RUN_M, "skeleton_layer.json"))
                for f in p["pressure_frames"]}
    propellants = {p["name"]: p for p in load(os.path.join(REPO, "data", "propellants.01234.json"))}

    curves, _ = drop_non_converged(build_curves(), totals)

    figure, axis = plt.subplots(figsize=(12.5, 7.0))

    # What the shipped rule can produce: (T_s + 2300)/2 over the whole surface-temperature bracket.
    axis.axhspan(0.5 * (BRACKET[0] + METAL_MELTING_K), 0.5 * (BRACKET[1] + METAL_MELTING_K),
                 color="0.85", zorder=0)
    axis.text(7.3, 0.5 * (BRACKET[1] + METAL_MELTING_K) - 18,
              "shipped rule $(T_s+T_m)/2$, $T_m$ = 2300 K,\nover the whole 600–900 K bracket",
              ha="right", va="top", fontsize=8.5, color="0.35")

    ceilings = []
    for name in sorted(propellants):
        propellant = propellants[name]
        total = totals[name]
        colour, marker = COLOURS[name], MARKERS[name]

        pressures, cold_branch, hot_branch, used = [], [], [], []
        for frame in propellant["pressure_frames"]:
            pressure = frame["pressure"]
            key = round(pressure)
            megapascals = pressure / 1e6
            target = polynomial(propellant["pocket_surface_fraction_coefficients"], megapascals) \
                / propellant["pocket_mass_fraction"]

            curve = curves[(name, key)]
            found = roots(curve, target * total)
            used.append((megapascals, 0.5 * (surfaces[(name, key)] + flames[(name, key)])))
            pressures.append(megapascals)

            if found:
                # None breaks the line rather than bridging it: where there is no root there is no
                # requirement, and a line drawn across the hole would invent one.
                cold_branch.append(found[0])
                hot_branch.append(found[-1] if len(found) > 1 else None)
            else:
                cold_branch.append(None)
                hot_branch.append(None)
                # No root: the target is above the ceiling of the whole curve. Plot the ceiling -
                # the temperature of the maximum - which is the closest this closure can come.
                peak_moles, peak_temperature = max((v, t) for t, v in curve)
                ceilings.append((megapascals, peak_temperature, name, peak_moles / total, target))

        axis.plot(pressures, cold_branch, marker=marker, markersize=7, linewidth=2.0,
                  color=colour, label=name, zorder=3)
        if any(t is not None for t in hot_branch):
            axis.plot(pressures, hot_branch, marker=marker, markersize=5, linewidth=1.2,
                      linestyle=":", color=colour, alpha=0.9, zorder=3,
                      markerfacecolor="white")
        axis.plot(*zip(*used), linestyle="--", linewidth=1.4, color=colour, alpha=0.75, zorder=2)

    for megapascals, temperature, name, peak, target in ceilings:
        axis.plot([megapascals], [temperature], marker="x", markersize=11,
                  markeredgewidth=2.4, color=COLOURS[name], zorder=5)
        axis.annotate(f"no root — ceiling {peak:.3f} < {target:.3f}",
                      xy=(megapascals, temperature), xytext=(megapascals + 0.15, temperature - 210),
                      fontsize=9, color=COLOURS[name],
                      arrowprops=dict(arrowstyle="-", color=COLOURS[name], linewidth=0.9))

    axis.set_xlabel("Pressure, MPa")
    axis.set_ylabel("Pore temperature $T_{pore}$, K")
    axis.set_title("What pore temperature would reproduce the measured coverage?\n"
                   "run M's closure $f_s = n_{C(s)}/n_{C,total}$, target $Z_m^a(p)/Z_{pocket}$ "
                   "(Babuk, measured)", fontsize=12)
    axis.grid(alpha=0.3)
    axis.set_xlim(0.6, 7.4)
    axis.legend(title="solid: required (cold branch)\n"
                      "dotted: second root, where one exists\n"
                      "dashed: what run M actually used\n"
                      "×: no root at any temperature",
                loc="center left", bbox_to_anchor=(1.01, 0.5),
                fontsize=9, title_fontsize=9)

    figure.tight_layout()
    figure.savefig(target_path, dpi=150)
    print(f"written: {target_path}")
    for megapascals, temperature, name, peak, target in ceilings:
        print(f"  ceiling point: {name} at {megapascals:.2f} MPa — max f_s {peak:.4f} at "
              f"{temperature:.0f} K, target {target:.4f}")


if __name__ == "__main__":
    main()
