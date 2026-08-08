"""Plot the inverse problem: where the f_s polynomial sits relative to what equilibrium can do.

One panel per pressure, one curve per composition - the compositions are the series, so the
panel count does not depend on how many propellants the file happens to hold.

Each panel shows, against the surface temperature:
  * solid   - f_s from the equilibrium closure, phi_Al + phi_C(s)((T_s + T_m)/2, p)
  * dashed  - what the polynomial asks for at that pressure, a horizontal line since the
              polynomial knows nothing about temperature
  * shading - the solver's own surface-temperature bracket (600..900 K), and everything
              below 0 K, which no propellant can reach

Where a dashed line does not cross its own solid curve inside the reachable region, the
polynomial cannot be reproduced by any surface temperature - which is the point of the plot.

USAGE
  python plot_required_surface_temperature.py [output png]
"""
from __future__ import annotations

import glob
import json
import os
import sys

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

ROOT = os.path.dirname(os.path.abspath(__file__))
METAL_MELTING_TEMPERATURE_K = 2300.0
PANEL_PRESSURES_MEGAPASCALS = [1.0, 6.5]
SEARCH_BRACKET_K = (600.0, 900.0)

# The best run under this closure (objective 0.0228).  Only used to mark where the solver
# actually settled, so the required temperatures can be read against a real one.
RUN_SKELETON_LAYER = "/root/runs/6h-Classic-Tm2300-kc400-constrON-fsEquilibrium-20260803-0118/skeleton_layer.json"


def polynomial_target(propellant: dict, megapascals: float) -> float:
    return sum(c * megapascals ** i
               for i, c in enumerate(propellant["pocket_surface_fraction_coefficients"])) \
        / propellant["pocket_mass_fraction"]


def coverage_curves() -> tuple[dict[tuple[str, int], dict[float, float]], dict[str, float]]:
    """f_s as {T_s: f_s} per (propellant, rounded pressure), plus phi_Al per propellant."""
    curves: dict[tuple[str, int], dict[float, float]] = {}
    aluminium: dict[str, float] = {}

    table = json.load(open(os.path.join(ROOT, "data/skeleton_carbon_equilibrium.json")))
    for entry in table["propellants"]:
        aluminium[entry["name"]] = entry["aluminiumVolumeFraction"]
        for frame in entry["frames"]:
            key = (entry["name"], round(frame["pressure"]))
            curves.setdefault(key, {}).update(
                dict(zip(table["surfaceTemperaturesKelvins"], frame["coverages"])))

    for path in sorted(glob.glob(os.path.join(ROOT, "shard_*.jsonl"))):
        for line in open(path):
            record = json.loads(line)
            key = (record["name"], round(record["pressure"]))
            surface = 2.0 * record["poreTemperature"] - METAL_MELTING_TEMPERATURE_K
            curves.setdefault(key, {})[surface] = \
                aluminium[record["name"]] + record["carbonFraction"]

    return curves, aluminium


def converged_surface_temperatures() -> dict[tuple[str, int], float]:
    """Where the solver actually put T_s in the best equilibrium-closure run, if it is around.

    Not an input to the inverse problem - it is the control.  The required temperature has to
    be read against the one the model converged to, or "unphysical" has no scale.
    """
    if not os.path.exists(RUN_SKELETON_LAYER):
        return {}
    return {(entry["name"], round(frame["pressure"])): frame["surface_temperature_pocket"]
            for entry in json.load(open(RUN_SKELETON_LAYER))
            for frame in entry["pressure_frames"]}


def interpolate(curve: dict[float, float], surface: float) -> float:
    temperatures = sorted(curve)
    for low, high in zip(temperatures, temperatures[1:]):
        if low <= surface <= high:
            return curve[low] + (curve[high] - curve[low]) * (surface - low) / (high - low)
    return curve[min(temperatures, key=lambda t: abs(t - surface))]


def main() -> None:
    target = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "required_surface_temperature.png")
    propellants = json.load(open(os.path.join(ROOT, "data/propellants.01234.json")))
    curves, _ = coverage_curves()
    pressures = [frame["pressure"] for frame in propellants[0]["pressure_frames"]]

    figure, axes = plt.subplots(1, len(PANEL_PRESSURES_MEGAPASCALS), figsize=(13, 5.5), sharey=True)
    colours = plt.rcParams["axes.prop_cycle"].by_key()["color"]

    # Bas_0, Bas_1 and Bas_2 hold the same recipe, so their equilibrium curves are the same
    # curve and would hide one another at equal width.  Drawing them thick-to-thin makes the
    # coincidence visible instead of accidental - and it is the whole point of the figure:
    # one solid curve underneath three widely separated dashed lines.
    widths = {"Bas_0": 4.5, "Bas_1": 2.8, "Bas_2": 1.4}
    converged = converged_surface_temperatures()

    for axis, wanted in zip(axes, PANEL_PRESSURES_MEGAPASCALS):
        pressure = min(pressures, key=lambda p: abs(p / 1e6 - wanted))
        for index, propellant in enumerate(propellants):
            name = propellant["name"]
            colour = colours[index % len(colours)]
            curve = curves.get((name, round(pressure)))
            if curve:
                temperatures = sorted(curve)
                axis.plot(temperatures, [curve[t] for t in temperatures], color=colour,
                          linewidth=widths.get(name, 1.8), marker="o", markersize=3, label=name)
                actual = converged.get((name, round(pressure)))
                if actual is not None:
                    axis.plot([actual], [interpolate(curve, actual)], color=colour, marker="*",
                              markersize=16, markeredgecolor="black", markeredgewidth=0.6,
                              linestyle="none", zorder=5)
            axis.axhline(polynomial_target(propellant, pressure / 1e6),
                         color=colour, linestyle="--", linewidth=1.2)

        axis.axvspan(-1000, 0, color="0.85", zorder=0)
        axis.axvspan(*SEARCH_BRACKET_K, color="tab:blue", alpha=0.10, zorder=0)
        axis.set_xlim(-1000, 2400)
        axis.set_xlabel("Surface temperature $T_s$, K")
        axis.set_title(f"p = {pressure / 1e6:.2f} MPa")
        axis.grid(alpha=0.3)

    axes[0].set_ylabel("Skeleton surface fraction $f_s$")
    axes[0].legend(title="solid: equilibrium   dashed: polynomial\nstar: where the solver converged",
                   fontsize=9, loc="lower right")
    axes[0].text(0.03, 0.97, "Bas_0, Bas_1 and Bas_2 share one recipe,\nso their solid curves coincide exactly",
                 transform=axes[0].transAxes, va="top", fontsize=8.5,
                 bbox=dict(boxstyle="round", facecolor="white", alpha=0.8, edgecolor="0.7"))
    figure.suptitle("What surface temperature would reproduce the $f_s$ polynomial?  "
                    "$T_{pore}=(T_s+2300)/2$; grey: $T_s<0$; blue: solver bracket 600–900 K")
    figure.tight_layout()
    figure.savefig(target, dpi=150)
    print(f"wrote {target}")


if __name__ == "__main__":
    main()
