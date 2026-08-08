"""Which pore temperature would make the equilibrium closure reproduce the measured coverage?

For every composition and every pressure frame - 50 points - solve

    f_s(T_pore, p) = f_s measured

for T_pore, under BOTH normalisations the campaign has used:

    run M    f_s = n_C(s) / n_C,total                 (char yield; aluminium not involved)
    shipped  f_s = phi_Al + phi_C(s)                   (Delesse; area fraction = volume fraction)

and report, for each root, the surface temperature it would require under each of the two pore rules
that have been in force, plus the melting temperature that the shipped rule would need.

NO NEW GIBBS SOLVES.  The condensed-carbon curve n_C(s)(T_pore, p) is assembled from three sets of
results already on disk, all of them exact outputs of the same minimiser:

  shard_*.jsonl                            24 pore temperatures, 700..2300 K, but with a gap at
                                           1400..1750 K
  data/skeleton_carbon_equilibrium.json    the shipped table, computed at T_pore = (T_s + T_m)/2 for
                                           T_s = 600..900 K, i.e. exactly 1450, 1487.5, 1525, 1562.5
                                           and 1600 K - inside that gap, for all five compositions
  <run M>/skeleton_carbon_equilibrium.json run M's table, computed at T_pore = (T_s + T_flame)/2, which
                                           for Bas_3 is 1611.5..1761.5 K - the rest of the gap, for the
                                           one composition whose pore landed in it

The two tables store coverages rather than moles, so they are converted back:
    run M    n_C = coverage * n_C,total
    shipped  n_C = (coverage - phi_Al) * v_matrix * rho_C / M_C
Both conversions are exact inverses of what the generator wrote.

Target: f_s = Z_m^a(p) / Z_pocket - Babuk's measured agglomeration curve (see
analyze_babuk_agglomeration_data.py for the proof that the shipped polynomial IS that measurement).

n_C(s)(T) is NOT monotone: it rises on the cold side as methanation (C + 2H2 -> CH4, exothermic,
Delta-n < 0) retreats, passes a maximum, then falls as oxidation and gasification take over - to exactly
zero for a composition whose pocket carries enough oxygen.  So a target may have two roots, one, or
none, and "none" is a statement about the ceiling, not about the search.
"""

import glob
import json
import os

REPO = os.path.dirname(os.path.abspath(__file__))
RUN_M = "/root/runs/6h-Classic-Tm2300-kc400-constrON-fsEquilibrium-20260803-0118"
CARBON_MOLAR_MASS = 12.011e-3
CARBON_RESIDUE_DENSITY = 1500.0
BRACKET = (600.0, 900.0)
METAL_MELTING_K = 2300.0


def load(path):
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def polynomial(coefficients, pressure_mpa):
    return sum(c * pressure_mpa ** i for i, c in enumerate(coefficients))


def build_curves():
    """n_C(s) in mol per kg of pocket matrix, keyed by (name, rounded pressure), sorted by T_pore."""
    points: dict[tuple[str, int], dict[float, float]] = {}

    def add(name, pressure, temperature, moles):
        points.setdefault((name, round(pressure)), {}).setdefault(round(temperature, 3), moles)

    for path in glob.glob(os.path.join(REPO, "shard_*.jsonl")):
        with open(path, encoding="utf-8") as handle:
            for line in handle:
                row = json.loads(line)
                add(row["name"], row["pressure"], row["poreTemperature"],
                    row["condensedCarbonMolesPerKilogram"])

    shipped = load(os.path.join(REPO, "data", "skeleton_carbon_equilibrium.json"))
    for propellant in shipped["propellants"]:
        aluminium = propellant["aluminiumVolumeFraction"]
        specific_volume = propellant["matrixSpecificVolumeCubicMetresPerKilogram"]
        for frame in propellant["frames"]:
            for surface, coverage in zip(shipped["surfaceTemperaturesKelvins"], frame["coverages"]):
                pore = 0.5 * (surface + shipped["metalMeltingTemperatureKelvins"])
                carbon_fraction = coverage - aluminium
                moles = carbon_fraction * specific_volume * CARBON_RESIDUE_DENSITY / CARBON_MOLAR_MASS
                add(propellant["name"], frame["pressure"], pore, moles)

    run_m = load(os.path.join(RUN_M, "skeleton_carbon_equilibrium.json"))
    for propellant in run_m["propellants"]:
        total = propellant["carbonAvailableMolesPerKilogram"]
        for frame in propellant["frames"]:
            for surface, coverage in zip(run_m["surfaceTemperaturesKelvins"], frame["coverages"]):
                pore = 0.5 * (surface + frame["skeletonFlameTemperature"])
                add(propellant["name"], frame["pressure"], pore, coverage * total)

    return {key: sorted(value.items()) for key, value in points.items()}


def drop_non_converged(curves, totals):
    """Remove points where the minimiser reported more condensed carbon than the matrix contains.

    One solve in 550 of the original scan did this (Bas_4 at 1.61 MPa and 700 K: 37.5 mol/kg against an
    inventory of 11.9). It is a mass-balance violation, so it is a failed solve and not a datum; left in,
    it becomes a spurious maximum and every verdict at that pressure is drawn from it.
    """
    dropped = []
    cleaned = {}
    for (name, pressure), curve in curves.items():
        keep = [(t, v) for t, v in curve if v <= totals[name] * 1.000001]
        dropped += [(name, pressure, t, v) for t, v in curve if v > totals[name] * 1.000001]
        cleaned[(name, pressure)] = keep
    return cleaned, dropped


def roots(curve, target_moles):
    """Every crossing of n_C(T) = target on the assembled grid, by linear interpolation."""
    found = []
    for i in range(1, len(curve)):
        (t0, v0), (t1, v1) = curve[i - 1], curve[i]
        if (v0 - target_moles) * (v1 - target_moles) <= 0 and v0 != v1:
            found.append(t0 + (target_moles - v0) / (v1 - v0) * (t1 - t0))
    return sorted(set(round(t, 1) for t in found))


def widest_gap(curve, low=900.0, high=2000.0):
    """Largest unsampled interval in the region where the maximum sits, for the reliability note."""
    inside = [t for t, _ in curve if low <= t <= high]
    return max((b - a for a, b in zip(inside, inside[1:])), default=0.0)


def main():
    curves = build_curves()
    shipped = load(os.path.join(REPO, "data", "skeleton_carbon_equilibrium.json"))
    geometry = {p["name"]: (p["aluminiumVolumeFraction"],
                            p["matrixSpecificVolumeCubicMetresPerKilogram"],
                            p["carbonAvailableMolesPerKilogram"])
                for p in shipped["propellants"]}
    flames = {(p["name"], round(f["pressure"])): f["skeletonFlameTemperature"]
              for p in load(os.path.join(RUN_M, "skeleton_carbon_equilibrium.json"))["propellants"]
              for f in p["frames"]}
    propellants = {p["name"]: p for p in load(os.path.join(REPO, "data", "propellants.01234.json"))}
    converged_surface = {
        (p["name"], round(f["pressure"])): f["surface_temperature_pocket"]
        for p in load(os.path.join(RUN_M, "skeleton_layer.json")) for f in p["pressure_frames"]}

    curves, dropped = drop_non_converged(curves, {n: g[2] for n, g in geometry.items()})

    sizes = sorted({len(c) for c in curves.values()})
    gaps = max(widest_gap(c) for c in curves.values())
    print("=" * 112)
    print(f"ASSEMBLED CURVE: {sizes} pore temperatures per point, "
          f"widest remaining gap between 900 and 2000 K: {gaps:.0f} K")
    for name, pressure, temperature, moles in dropped:
        print(f"  DROPPED (mass balance): {name} at {pressure / 1e6:.2f} MPa, {temperature:.0f} K — "
              f"{moles:.1f} mol/kg against an inventory of {geometry[name][2]:.1f}")
    print("=" * 112)

    for label, normaliser in (
        ("RUN M's NORMALISATION   f_s = n_C(s) / n_C,total", "yield"),
        ("SHIPPED NORMALISATION   f_s = phi_Al + phi_C(s)", "delesse"),
    ):
        print()
        print("=" * 112)
        print(label)
        print("=" * 112)
        print(f"{'':7}{'p, MPa':>7}{'target':>8}{'f_s max':>8}{'T at max':>9}"
              f"{'T_pore cold':>12}{'T_pore hot':>11}"
              f"{'T_s needed':>11}{'T_m needed':>11}  verdict")

        stats = {"roots": 0, "none": 0, "in_bracket": 0, "below_floor": 0}
        for name in sorted(propellants):
            propellant = propellants[name]
            aluminium, specific_volume, total = geometry[name]
            for frame in propellant["pressure_frames"]:
                pressure = frame["pressure"]
                pressure_key = round(pressure)
                pressure_mpa = pressure / 1e6
                curve = curves[(name, pressure_key)]

                target = polynomial(propellant["pocket_surface_fraction_coefficients"], pressure_mpa) \
                    / propellant["pocket_mass_fraction"]

                if normaliser == "yield":
                    to_coverage = lambda moles: moles / total  # noqa: E731
                    target_moles = target * total
                else:
                    def to_coverage(moles):
                        return aluminium + moles * CARBON_MOLAR_MASS \
                            / CARBON_RESIDUE_DENSITY / specific_volume

                    target_moles = (target - aluminium) * specific_volume \
                        * CARBON_RESIDUE_DENSITY / CARBON_MOLAR_MASS

                peak_moles, peak_temperature = max((v, t) for t, v in curve)
                peak = to_coverage(peak_moles)
                floor = to_coverage(0.0)

                if target_moles < 0:
                    stats["none"] += 1
                    stats["below_floor"] += 1
                    print(f"{name:7}{pressure_mpa:7.2f}{target:8.4f}{peak:8.4f}{peak_temperature:9.0f}"
                          f"{'-':>12}{'-':>11}{'-':>11}{'-':>11}"
                          f"  NO ROOT: target below the aluminium floor {floor:.4f}")
                    continue

                found = roots(curve, target_moles)
                if not found:
                    stats["none"] += 1
                    print(f"{name:7}{pressure_mpa:7.2f}{target:8.4f}{peak:8.4f}{peak_temperature:9.0f}"
                          f"{'-':>12}{'-':>11}{'-':>11}{'-':>11}"
                          f"  NO ROOT: ceiling {peak:.4f} < target")
                    continue

                stats["roots"] += 1
                cold, hot = found[0], (found[-1] if len(found) > 1 else None)

                # Two ways to express the same root against the rules that have been in force.
                #   T_s needed:  hold T_m at 2300 K and ask what surface temperature reaches the root.
                #   T_m needed:  hold the surface at run M's own converged value and ask what melting
                #                temperature reaches it. This is the honest anchor for T_m, because the
                #                surface temperature is the one the solver actually produced.
                surface_metal_rule = 2.0 * cold - METAL_MELTING_K
                melting_needed = 2.0 * cold - converged_surface[(name, pressure_key)]
                inside = BRACKET[0] <= surface_metal_rule <= BRACKET[1]
                stats["in_bracket"] += 1 if inside else 0

                print(f"{name:7}{pressure_mpa:7.2f}{target:8.4f}{peak:8.4f}{peak_temperature:9.0f}"
                      f"{cold:12.0f}{'-' if hot is None else f'{hot:.0f}':>11}"
                      f"{surface_metal_rule:11.0f}{melting_needed:11.0f}"
                      f"  {'T_s IN BRACKET' if inside else 'T_s outside 600-900 K'}")

        print(f"\n  {stats['roots']} of 50 points have a pore temperature that closes the gap; "
              f"{stats['none']} have none "
              f"({stats['below_floor']} of those because the target is below the aluminium floor).")
        print(f"  Of the {stats['roots']} with a root, {stats['in_bracket']} would need a surface "
              f"temperature inside 600-900 K under T_pore = (T_s + 2300)/2.")
        print("  'T_s needed' holds T_m at 2300 K and asks for the surface temperature; "
              "'T_m needed' holds")
        print("  the surface at run M's own converged value and asks for the melting temperature.")


if __name__ == "__main__":
    main()
