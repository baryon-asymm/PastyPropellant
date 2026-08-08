"""Read the bridge scan and answer the question: can a bridge's skeleton fraction be zero, and when?

Prints two things and draws one figure.

  CONSISTENCY  The adiabatic flame temperature of each mixture against what the input file declares.
               If the bridge really burns as the whole propellant, its T_ad must land near the
               declared inter-pocket kinetic flame of 3707 K, and the pocket matrix near the declared
               out-skeleton flame of 2365 K.  Neither number was used to build the mixtures, so this
               is a test, not a fit.

  THE ANSWER   f_s(T) under both normalisations the campaign has used, over 400..1200 K - the range
               that contains every inter-pocket surface temperature run M converged to (628..900 K).

USAGE
  python report_bridge_cold_branch.py [output png]
"""
from __future__ import annotations

import json
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

REPO = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, REPO)

from bridge_cold_branch import (  # noqa: E402
    DECLARED_INTER_POCKET_FLAME_K,
    DECLARED_OUT_SKELETON_FLAME_K,
    mixtures,
)

CARBON_MOLAR_MASS = 12.011e-3
CARBON_RESIDUE_DENSITY = 1500.0
SURFACE_WINDOW_K = (628.0, 900.0)      # run M, inter-pocket, over all five and all ten pressures
COLOURS = {"Bas_0": "#1f77b4", "Bas_1": "#d62728", "Bas_2": "#2ca02c",
           "Bas_3": "#9467bd", "Bas_4": "#8c564b"}
MARKERS = {"Bas_0": "o", "Bas_1": "s", "Bas_2": "^", "Bas_3": "D", "Bas_4": "*"}


def volumetry(propellant: dict, mixture: dict[str, float]) -> tuple[float, float]:
    """(specific volume m3/kg, aluminium volume fraction) of a mixture given as component masses."""
    densities = {name: body["density"] for name, body in propellant["components"].items()}
    total = sum(mixture.values())
    specific_volume = sum(mass / total / densities[name] for name, mass in mixture.items())
    aluminium = (mixture["Aluminum"] / total / densities["Aluminum"]) / specific_volume
    return specific_volume, aluminium


def main() -> None:
    target = sys.argv[1] if len(sys.argv) > 1 \
        else os.path.join(REPO, "docs/results/bridge_cold_branch.png")

    rows = [json.loads(line) for line in open(os.path.join(REPO, "bridge_cold_branch.jsonl"))]
    propellants = {p["name"]: p for p in
                   json.load(open(os.path.join(REPO, "data/propellants.01234.json")))}
    geometry = {(name, label): volumetry(propellant, mixture)
                for name, propellant in propellants.items()
                for label, mixture in mixtures(propellant).items()}

    print("=" * 104)
    print("CONSISTENCY — adiabatic flame temperature against what the input file already declares")
    print("=" * 104)
    print(f"{'':8}{'mixture':>15}{'p, MPa':>9}{'T_ad, K':>10}{'declared':>10}{'difference':>12}")
    for row in sorted((r for r in rows if r["kind"] == "flame"),
                      key=lambda r: (r["name"], r["mixture"], r["pressure"])):
        declared = DECLARED_INTER_POCKET_FLAME_K if row["mixture"] == "bridge" \
            else DECLARED_OUT_SKELETON_FLAME_K
        print(f"{row['name']:8}{row['mixture']:>15}{row['pressure'] / 1e6:9.1f}"
              f"{row['value']:10.0f}{declared:10.0f}{row['value'] - declared:+12.0f}")

    print()
    print("=" * 104)
    print("THE ANSWER — skeleton fraction of a bridge over the temperatures a bridge actually has")
    print(f"  the inter-pocket surface in run M sits at {SURFACE_WINDOW_K[0]:.0f}..{SURFACE_WINDOW_K[1]:.0f} K")
    print("  char yield f_s = n_C(s)/n_C,total  |  Delesse f_s = phi_Al + phi_C(s)")
    print("=" * 104)

    # Mass-balance filter. The minimiser occasionally returns more condensed carbon than the mixture
    # contains — three points here, all at 650 K and 1 MPa, all reporting 11.08 mol/kg against an
    # inventory of 9.51. That is a failed solve, not a datum, and left in it becomes a spurious spike.
    # The same failure appeared once in the pocket scan; the filter lives in the code, not in my head.
    dropped = [r for r in rows
               if r["kind"] == "carbon" and r["value"] > r["carbonTotal"] * 1.000001]
    for row in dropped:
        print(f"  DROPPED (mass balance): {row['name']} {row['mixture']} "
              f"{row['pressure'] / 1e6:.1f} MPa {row['temperature']:.0f} K — "
              f"{row['value']:.3f} mol/kg against an inventory of {row['carbonTotal']:.3f}")
    carbon = {(r["name"], r["mixture"], r["pressure"], r["temperature"]): (r["value"], r["carbonTotal"])
              for r in rows if r["kind"] == "carbon" and r not in dropped}
    temperatures = sorted({r["temperature"] for r in rows if r["kind"] == "carbon"})

    for mixture_label in ("bridge", "pocket_matrix"):
        print(f"\n  {mixture_label}")
        print(f"{'':8}{'p, MPa':>8}" + "".join(f"{t:>8.0f}" for t in temperatures))
        for name in sorted(COLOURS):
            specific_volume, aluminium = geometry[(name, mixture_label)]
            for pressure in sorted({r["pressure"] for r in rows}):
                yields, delesse = [], []
                for temperature in temperatures:
                    entry = carbon.get((name, mixture_label, pressure, temperature))
                    if entry is None:
                        yields.append(None)
                        delesse.append(None)
                        continue
                    moles, total = entry
                    yields.append(moles / total)
                    delesse.append(aluminium + moles * CARBON_MOLAR_MASS
                                   / CARBON_RESIDUE_DENSITY / specific_volume)
                cell = lambda v: f"{'  --  ':>8}" if v is None else f"{v:8.3f}"  # noqa: E731
                print(f"{name:8}{pressure / 1e6:8.1f}" + "".join(cell(v) for v in yields)
                      + "   yield")
                print(f"{'':8}{'':8}" + "".join(cell(v) for v in delesse)
                      + f"   Delesse (phi_Al = {aluminium:.3f})")

    figure, axes = plt.subplots(1, 2, figsize=(13.5, 6.2), sharey=True)
    for axis, pressure in zip(axes, sorted({r["pressure"] for r in rows})):
        axis.axvspan(*SURFACE_WINDOW_K, color="0.85", zorder=0)
        for name in sorted(COLOURS):
            for mixture_label, style, fill in (("bridge", "-", True), ("pocket_matrix", ":", False)):
                values = [None if (name, mixture_label, pressure, t) not in carbon else
                          carbon[(name, mixture_label, pressure, t)][0]
                          / carbon[(name, mixture_label, pressure, t)][1] for t in temperatures]
                axis.plot(temperatures, values, style, marker=MARKERS[name],
                          markersize=7 if fill else 5, linewidth=2.0 if fill else 1.2,
                          color=COLOURS[name], markerfacecolor=COLOURS[name] if fill else "white",
                          label=name if fill and pressure == max(r["pressure"] for r in rows) else None, zorder=3)
        axis.set_title(f"{pressure / 1e6:.1f} MPa")
        axis.set_xlabel("Temperature, K")
        axis.grid(alpha=0.3)

    axes[0].set_ylabel("$f_s = n_{C(s)}/n_{C,total}$")
    axes[0].text(SURFACE_WINDOW_K[0] + 10, 0.02,
                 "inter-pocket surface\ntemperatures, run M", fontsize=8.5, color="0.35")
    # Not a rendering accident, and worth saying on the figure: read as "burns as the whole
    # propellant", every bridge is the same mixture, because the five recipes differ only in how the
    # AP is split between fine and coarse and that split cancels in the total.
    axes[0].annotate("all five solid curves coincide:\nthe five recipes have identical totals,\n"
                     "only the AP fine/coarse split differs",
                     xy=(500, 0.434), xytext=(560, 0.62), fontsize=8.5, color="0.25",
                     arrowprops=dict(arrowstyle="->", color="0.45", linewidth=0.9))
    figure.suptitle("Condensed carbon left in a bridge, over the temperatures a bridge actually has\n"
                    "solid: bridge mixture (burns as the whole propellant)   "
                    "dotted: pocket matrix, for contrast", fontsize=12)
    axes[-1].legend(loc="center left", bbox_to_anchor=(1.02, 0.5), fontsize=9, title="composition")
    figure.tight_layout(rect=(0, 0, 1, 0.92))
    figure.savefig(target, dpi=150)
    print(f"\nwritten: {target}")


if __name__ == "__main__":
    main()
