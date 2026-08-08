"""The required pore temperature, tabulated against the pore temperatures that are actually reachable.

Same inversion as `invert_pore_temperature.py` — solve `f_s(T_pore, p) = f_s measured` on the assembled
Gibbs curve, no new solves — but reported on the axis the campaign now argues in.

WHY A SECOND VIEW.  `invert_pore_temperature.py` judged reachability by pinning `T_m` at 2300 K and
asking for the surface temperature, so nearly every row reads "T_s outside 600-900 K" and the size of
the miss is invisible.  The pore temperature is what the equilibrium actually depends on, and what a
`T_m` choice only reaches indirectly, so the honest comparison is required `T_pore` against the WINDOW
of reachable `T_pore` — `[(600 + T_m)/2, (900 + T_m)/2]`, the surface bracket carried through the
`T_pore = (T_s + T_m)/2` rule.

    T_m = 1300 K (this branch)   reachable T_pore = 950 .. 1100 K
    T_m = 2300 K (the papers)    reachable T_pore = 1450 .. 1600 K

`n_C(s)(T)` is not monotone — it rises while methanation retreats, peaks, then falls to gasification —
so a target can have two roots, one, or none, and "none" is a statement about the ceiling of the curve.

USAGE
  python3 report_required_pore_temperature.py [output markdown]
"""
from __future__ import annotations

import os
import sys

REPO = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, REPO)

from invert_pore_temperature import (  # noqa: E402
    CARBON_MOLAR_MASS,
    CARBON_RESIDUE_DENSITY,
    RUN_M,
    build_curves,
    drop_non_converged,
    load,
    polynomial,
    roots,
)

SURFACE_BRACKET = (600.0, 900.0)
MELTING_CASES = ((1300.0, "T_m = 1300 K"), (2300.0, "T_m = 2300 K"))


def window(melting: float) -> tuple[float, float]:
    return 0.5 * (SURFACE_BRACKET[0] + melting), 0.5 * (SURFACE_BRACKET[1] + melting)


def verdict(found: list[float], low: float, high: float) -> str:
    if not found:
        return "—"
    if any(low <= t <= high for t in found):
        return "**да**"
    nearest = min(found, key=lambda t: min(abs(t - low), abs(t - high)))
    miss = low - nearest if nearest < low else nearest - high
    return f"нет ({miss:+.0f} K)"


def main() -> None:
    target_path = sys.argv[1] if len(sys.argv) > 1 \
        else os.path.join(REPO, "docs/results/required_pore_temperature.md")

    shipped = load(os.path.join(REPO, "data", "skeleton_carbon_equilibrium.json"))
    geometry = {p["name"]: (p["aluminiumVolumeFraction"],
                            p["matrixSpecificVolumeCubicMetresPerKilogram"],
                            p["carbonAvailableMolesPerKilogram"])
                for p in shipped["propellants"]}
    propellants = {p["name"]: p for p in load(os.path.join(REPO, "data", "propellants.01234.json"))}
    converged_surface = {
        (p["name"], round(f["pressure"])): f["surface_temperature_pocket"]
        for p in load(os.path.join(RUN_M, "skeleton_layer.json")) for f in p["pressure_frames"]}

    curves, dropped = drop_non_converged(build_curves(), {n: g[2] for n, g in geometry.items()})

    lines = [
        "# Потребная температура пор против достижимой",
        "",
        "Решение `f_s(T_pore, p) = f_s измеренная` относительно `T_pore` на собранной равновесной кривой "
        "(33–34 температуры на точку, новых расчётов Гиббса нет). Цель — `Z_m^a(p)/Z_p`, измеренная "
        "кривая агломерации Бабука.",
        "",
        "`n_C(s)(T)` немонотонна: растёт, пока отступает метанирование, проходит максимум, затем падает "
        "на газификации — поэтому корней может быть два, один или ни одного. «Ни одного» — это "
        "утверждение о потолке кривой, а не о поиске.",
        "",
        f"Достижимое окно `T_pore = (T_s + T_m)/2` при вилке поверхности "
        f"{SURFACE_BRACKET[0]:.0f}–{SURFACE_BRACKET[1]:.0f} K:",
        "",
    ] + [f"- **{label}** → T_pore = {window(m)[0]:.0f}–{window(m)[1]:.0f} K" for m, label in MELTING_CASES]

    if dropped:
        lines += ["", "Отброшено по балансу массы (сбойные решения минимизатора):"]
        lines += [f"- {n} при {p / 1e6:.2f} МПа, {t:.0f} K — {v:.1f} моль/кг при запасе "
                  f"{geometry[n][2]:.1f}" for n, p, t, v in dropped]

    for label, normaliser in (("f_s = n_C(s) / n_C,total (нормировка по инвентарю углерода)", "yield"),
                              ("f_s = φ_Al + φ_C(s) (Делесс)", "delesse")):
        lines += ["", f"## {label}", "",
                  "| состав | p, МПа | измер. f_s | потолок кривой | T_pore потребная, K | "
                  + " | ".join(f"достижимо при {name}" for _, name in MELTING_CASES)
                  + " | T_m потребная при своей T_s |",
                  "|---|---:|---:|---:|---|" + "---|" * (len(MELTING_CASES) + 1)]

        reachable = {m: 0 for m, _ in MELTING_CASES}
        rooted = 0
        for name in sorted(propellants):
            propellant = propellants[name]
            aluminium, specific_volume, total = geometry[name]
            for frame in propellant["pressure_frames"]:
                pressure_key = round(frame["pressure"])
                pressure_mpa = frame["pressure"] / 1e6
                curve = curves[(name, pressure_key)]

                measured = polynomial(propellant["pocket_surface_fraction_coefficients"],
                                      pressure_mpa) / propellant["pocket_mass_fraction"]
                if normaliser == "yield":
                    to_coverage = lambda moles: moles / total  # noqa: E731
                    target_moles = measured * total
                else:
                    def to_coverage(moles, _a=aluminium, _v=specific_volume):
                        return _a + moles * CARBON_MOLAR_MASS / CARBON_RESIDUE_DENSITY / _v

                    target_moles = (measured - aluminium) * specific_volume \
                        * CARBON_RESIDUE_DENSITY / CARBON_MOLAR_MASS

                peak_moles, _ = max((v, t) for t, v in curve)
                ceiling = to_coverage(peak_moles)
                found = [] if target_moles < 0 else roots(curve, target_moles)
                rooted += bool(found)
                for melting, _ in MELTING_CASES:
                    low, high = window(melting)
                    reachable[melting] += any(low <= t <= high for t in found)

                if not found:
                    why = "ниже алюминиевого пола" if target_moles < 0 \
                        else f"потолок {ceiling:.3f} < цели"
                    required = f"нет корня — {why}"
                    melting_needed = "—"
                else:
                    required = " / ".join(f"{t:.0f}" for t in found)
                    surface = converged_surface[(name, pressure_key)]
                    melting_needed = " / ".join(f"{2 * t - surface:.0f}" for t in found)

                lines.append(
                    f"| {name} | {pressure_mpa:.2f} | {measured:.4f} | {ceiling:.3f} | {required} | "
                    + " | ".join(verdict(found, *window(m)) for m, _ in MELTING_CASES)
                    + f" | {melting_needed} |")

        lines += ["", f"**Итог:** корень существует у {rooted} из 50 точек; попадает в достижимое окно "
                  + ", ".join(f"{reachable[m]} при {name}" for m, name in MELTING_CASES) + "."]

    with open(target_path, "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines) + "\n")
    print("\n".join(lines))
    print(f"\nwritten: {target_path}")


if __name__ == "__main__":
    main()
