"""The temperature an inter-pocket bridge needs before it holds no condensed carbon.

One curve per composition — the compositions are the series, so nothing here depends on how many
propellants the file happens to carry.

    solid, filled      the bridge as the recipe makes it: matrix (binder + aluminium + fine AP) plus
                       entrained wall material
    dotted, open       the same bridge with the aluminium taken out, which is the solver's own
                       inter-pocket assumption ("no metal") read literally.  The gap between the two
                       curves is the aluminium's share of the difficulty, and it is the whole story.
    ▲ at the ceiling   condensed carbon survives 3200 K: no temperature does it at that entrainment
    grey band          the bridge pore temperature the shipped rule would give, (T_s + T_m)/2 over the
                       inter-pocket surface temperatures run M actually converged to (628..900 K)
    dashed line        T_m = 2300 K — above it the metal is molten and there is no skeleton to speak
                       of, so any requirement lying above this line is answered by the melting alone

USAGE
  python plot_bridge_zero_carbon.py [output png]
"""
from __future__ import annotations

import json
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

REPO = os.path.dirname(os.path.abspath(__file__))
CEILING_KELVINS = 3200.0
METAL_MELTING_K = 2300.0
INTER_POCKET_SURFACE_K = (628.0, 900.0)     # run M, min and max over all five and all ten pressures
COLOURS = {"Bas_0": "#1f77b4", "Bas_1": "#d62728", "Bas_2": "#2ca02c",
           "Bas_3": "#9467bd", "Bas_4": "#8c564b"}
MARKERS = {"Bas_0": "o", "Bas_1": "s", "Bas_2": "^", "Bas_3": "D", "Bas_4": "*"}


def load() -> list[dict]:
    rows = []
    for name in ("bridge_zero_carbon.x0.jsonl", "bridge_zero_carbon.jsonl"):
        path = os.path.join(REPO, name)
        if os.path.exists(path):
            rows += [json.loads(line) for line in open(path) if line.strip()]
    return rows


def main() -> None:
    target = sys.argv[1] if len(sys.argv) > 1 \
        else os.path.join(REPO, "docs/results/bridge_zero_carbon_temperature.png")

    rows = load()
    pressures = sorted({row["pressure"] for row in rows})
    figure, axes = plt.subplots(1, len(pressures), figsize=(13.5, 6.4), sharey=True)
    axes = [axes] if len(pressures) == 1 else list(axes)

    for axis, pressure in zip(axes, pressures):
        axis.axhspan(0.5 * (INTER_POCKET_SURFACE_K[0] + METAL_MELTING_K),
                     0.5 * (INTER_POCKET_SURFACE_K[1] + METAL_MELTING_K),
                     color="0.85", zorder=0)
        axis.axhline(METAL_MELTING_K, color="0.45", linestyle="--", linewidth=1.2, zorder=1)

        for name in sorted(COLOURS):
            for variant, style, fill in (("with_aluminium", "-", True),
                                         ("no_aluminium", ":", False)):
                points = sorted((row["entrainment"], row["zeroTemperature"])
                                for row in rows
                                if row["name"] == name and row["variant"] == variant
                                and row["pressure"] == pressure)
                if not points:
                    continue
                xs = [x for x, _ in points]
                ys = [CEILING_KELVINS if t is None else t for _, t in points]
                axis.plot(xs, ys, style, marker=MARKERS[name], markersize=7 if fill else 6,
                          linewidth=2.0 if fill else 1.3, color=COLOURS[name],
                          markerfacecolor=COLOURS[name] if fill else "white",
                          label=name if variant == "with_aluminium" and pressure == pressures[0]
                          else None, zorder=3)
                for x, temperature in points:
                    if temperature is None:
                        axis.plot([x], [CEILING_KELVINS], marker="^", markersize=11,
                                  color=COLOURS[name], zorder=5)

        axis.set_xlabel("Wall material entrained per unit matrix, $x$ (kg/kg)")
        axis.set_title(f"{pressure / 1e6:.1f} MPa")
        axis.grid(alpha=0.3)

    axes[0].set_ylabel("Temperature at which the bridge holds no condensed carbon, K")
    axes[0].text(0.03, METAL_MELTING_K + 40, "$T_m$ = 2300 K — metal molten above this",
                 fontsize=8.5, color="0.35")
    axes[0].text(0.03, 0.5 * (INTER_POCKET_SURFACE_K[1] + METAL_MELTING_K) - 60,
                 "bridge pore temperature\nfrom the shipped rule", fontsize=8.5,
                 va="top", color="0.35")
    figure.suptitle("What an inter-pocket bridge would need before its skeleton fraction can be zero\n"
                    "solid: bridge as mixed (with aluminium)   dotted: aluminium removed   "
                    "▲: carbon survives 3200 K", fontsize=12)
    axes[-1].legend(loc="center left", bbox_to_anchor=(1.02, 0.5), fontsize=9, title="composition")

    figure.tight_layout(rect=(0, 0, 1, 0.93))
    figure.savefig(target, dpi=150)
    print(f"written: {target}")

    print()
    print(f"{'':8}{'x':>6}{'variant':>16}" + "".join(f"{p / 1e6:>10.1f} MPa" for p in pressures))
    for name in sorted(COLOURS):
        for variant in ("with_aluminium", "no_aluminium"):
            for entrainment in sorted({row["entrainment"] for row in rows}):
                cells = []
                for pressure in pressures:
                    match = [row for row in rows if row["name"] == name and row["variant"] == variant
                             and row["pressure"] == pressure and row["entrainment"] == entrainment]
                    if not match:
                        cells.append(f"{'-':>14}")
                    elif match[0]["zeroTemperature"] is None:
                        cells.append(f"{'>3200 K':>14}")
                    else:
                        cells.append(f"{match[0]['zeroTemperature']:>11.0f} K")
                print(f"{name:8}{entrainment:6.2f}{variant:>16}" + "".join(cells))


if __name__ == "__main__":
    main()
