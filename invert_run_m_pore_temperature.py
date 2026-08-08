"""What pore temperature would have made RUN M's closure reproduce the measured agglomeration curve?

Run M used a closure this repository no longer ships:

    f_s = n_C(s)(T_pore, p) / n_C,total          (char yield, aluminium not involved)
    T_pore = (T_flame,skeleton + T_s) / 2        (the pre-correction rule)

so the question "which T_pore closes the gap" is a different inverse problem from the one settled on
2026-08-04, which asked it of the Delesse closure under T_pore = (T_s + T_m)/2.

No new Gibbs solves are run here.  The 1200-point scan in shard_*.jsonl already carries
`condensedCarbonMolesPerKilogram` on a 24-point pore-temperature grid for every composition and
pressure, and that is the raw quantity both normalisations are built from - the shards were produced
for the Delesse inversion but they are normalisation-agnostic.

Target: f_s = Z_m^a(p) / Z_pocket, i.e. Babuk's measured agglomeration curve (see
analyze_babuk_agglomeration_data.py for the proof that the shipped polynomial IS that measurement).

n_C(s)(T) rises on the cold side (methanation C + 2H2 -> CH4 is suppressed as it warms) and falls on
the hot side (Boudouard and steam gasification take over), so a target below the maximum has TWO roots
and both are reported.
"""

import glob
import json
import os

REPO = os.path.dirname(os.path.abspath(__file__))
RUN_M = "/root/runs/6h-Classic-Tm2300-kc400-constrON-fsEquilibrium-20260803-0118"
BRACKET = (600.0, 900.0)


def load_scan():
    curves: dict[tuple[str, int], list[tuple[float, float]]] = {}
    for path in glob.glob(os.path.join(REPO, "shard_*.jsonl")):
        with open(path, encoding="utf-8") as handle:
            for line in handle:
                row = json.loads(line)
                key = (row["name"], round(row["pressure"]))
                curves.setdefault(key, []).append(
                    (row["poreTemperature"], row["condensedCarbonMolesPerKilogram"]))
    for key in curves:
        curves[key].sort()
    return curves


def interpolate(curve, temperature):
    if temperature <= curve[0][0]:
        return curve[0][1]
    for i in range(1, len(curve)):
        if temperature <= curve[i][0]:
            (t0, v0), (t1, v1) = curve[i - 1], curve[i]
            return v0 + (temperature - t0) / (t1 - t0) * (v1 - v0)
    return curve[-1][1]


def roots(curve, total, target):
    """Every crossing of n_C(T)/n_total = target on the sampled grid, by linear interpolation."""
    found = []
    for i in range(1, len(curve)):
        (t0, v0), (t1, v1) = curve[i - 1], curve[i]
        f0, f1 = v0 / total, v1 / total
        if (f0 - target) * (f1 - target) <= 0 and f0 != f1:
            found.append(t0 + (target - f0) / (f1 - f0) * (t1 - t0))
    return found


def polynomial(coefficients, pressure_mpa):
    return sum(c * pressure_mpa ** i for i, c in enumerate(coefficients))


def main():
    curves = load_scan()
    table = json.load(open(os.path.join(RUN_M, "skeleton_carbon_equilibrium.json"), encoding="utf-8"))
    totals = {p["name"]: p["carbonAvailableMolesPerKilogram"] for p in table["propellants"]}
    flames = {(p["name"], round(f["pressure"])): f["skeletonFlameTemperature"]
              for p in table["propellants"] for f in p["frames"]}
    grid = table["surfaceTemperaturesKelvins"]
    shipped = {(p["name"], round(f["pressure"])): f["coverages"]
               for p in table["propellants"] for f in p["frames"]}

    propellants = {p["name"]: p for p in
                   json.load(open(os.path.join(RUN_M, "propellants.01234.json"), encoding="utf-8"))}
    layer = {p["name"]: p for p in
             json.load(open(os.path.join(RUN_M, "skeleton_layer.json"), encoding="utf-8"))}

    print("=" * 100)
    print("VALIDATION: the shards must reproduce run M's own shipped table at run M's own T_pore")
    print("=" * 100)
    print(f"{'':7}{'p, MPa':>8}{'T_s':>7}{'T_pore':>9}{'table':>9}{'shards':>9}{'diff':>9}")
    worst = 0.0
    for name in sorted(totals):
        for pressure_key, coverages in ((k[1], v) for k, v in shipped.items() if k[0] == name):
            flame = flames[(name, pressure_key)]
            for surface, expected in zip(grid, coverages):
                pore = 0.5 * (surface + flame)
                got = interpolate(curves[(name, pressure_key)], pore) / totals[name]
                worst = max(worst, abs(got - expected))
            if pressure_key in (1000000, 6500000):
                surface, expected = grid[2], coverages[2]
                pore = 0.5 * (surface + flame)
                got = interpolate(curves[(name, pressure_key)], pore) / totals[name]
                print(f"{name:7}{pressure_key / 1e6:8.2f}{surface:7.0f}{pore:9.1f}"
                      f"{expected:9.4f}{got:9.4f}{got - expected:+9.4f}")
    print(f"\n  worst over all 250 table entries: {worst:.4f}")
    print("  (the scan grid has a 1400->1750 K gap, so entries landing inside it interpolate across it)")

    print()
    print("=" * 100)
    print("THE INVERSE PROBLEM: which T_pore makes run M's closure hit the measured curve?")
    print("=" * 100)
    print(f"{'':7}{'p, MPa':>7}{'target':>8}{'M had':>8}{'T_pore M':>10}"
          f"{'cold root':>11}{'hot root':>10}{'  needed T_s (cold branch)'}")

    summary = {"cold": 0, "hot": 0, "none": 0, "in_bracket": 0}
    for name in sorted(totals):
        propellant = propellants[name]
        for frame in layer[name]["pressure_frames"]:
            pressure_key = round(frame["pressure"])
            pressure_mpa = frame["pressure"] / 1e6
            surface = frame["surface_temperature_pocket"]
            flame = flames[(name, pressure_key)]
            pore_used = 0.5 * (surface + flame)
            curve = curves[(name, pressure_key)]

            had = interpolate(curve, pore_used) / totals[name]
            target = polynomial(propellant["pocket_surface_fraction_coefficients"], pressure_mpa) \
                / propellant["pocket_mass_fraction"]

            found = roots(curve, totals[name], target)
            peak = max(v for _, v in curve) / totals[name]
            cold = min(found) if found else None
            hot = max(found) if len(found) > 1 else None

            if not found:
                summary["none"] += 1
                verdict = f"  no root at any T_pore (max {peak:.3f} < target)"
            else:
                summary["cold" if cold is not None else "hot"] += 1
                needed_surface = 2.0 * cold - flame
                inside = BRACKET[0] <= needed_surface <= BRACKET[1]
                summary["in_bracket"] += 1 if inside else 0
                verdict = f"  T_s = {needed_surface:7.0f} K  {'IN BRACKET' if inside else 'outside'}"

            print(f"{name:7}{pressure_mpa:7.2f}{target:8.4f}{had:8.4f}{pore_used:10.0f}"
                  f"{'-' if cold is None else f'{cold:.0f}':>11}"
                  f"{'-' if hot is None else f'{hot:.0f}':>10}"
                  f"{verdict}")

    print()
    print(f"  roots found: {summary['cold'] + summary['hot']} of 50, of which "
          f"{summary['in_bracket']} need a surface temperature inside the 600-900 K bracket; "
          f"{summary['none']} have no root at any pore temperature in 700-2300 K.")


if __name__ == "__main__":
    main()
