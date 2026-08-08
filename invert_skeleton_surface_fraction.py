"""What surface temperature would make the equilibrium closure reproduce the f_s polynomial?

Reads four things and needs nothing else:
  * data/propellants.01234.json            - the polynomial, i.e. the target
  * data/skeleton_carbon_equilibrium.json  - phi_C over T_pore 1450..1600 K (the run's table)
  * shard_*.jsonl                          - phi_C over T_pore 700..2300 K, the sharded scan

and reports, per (propellant, pressure), the surface temperature the closure would need.

The closure is f_s = phi_Al + phi_C(s)(T_pore, p) with T_pore = (T_s + T_m)/2, T_m = 2300 K.
phi_Al is recipe and fixed, so inverting means solving phi_C(T_pore, p) = f_s,poly(p) - phi_Al
and reading back T_s = 2 T_pore - T_m.

phi_C(T) rises to a maximum near T_pore = 1750 K and falls on both sides - methanation
(exothermic, dn = -1) takes the carbon on the cold side, Boudouard and steam gasification on
the hot side - so a target below that maximum is met TWICE.  Both roots are reported.  The
physically meaningful range is T_s in [0, T_m]: a negative Kelvin temperature is impossible,
and a surface hotter than the melting metal has no skeleton standing on it.

Verdicts:
  IN BRACKET        root inside the solver's own 600..900 K search bracket - the only outcome
                    that would mean the polynomial and equilibrium agree on a real run.
  OUTSIDE BRACKET   root is a real temperature but outside 600..900 K.
  UNPHYSICAL        root exists only at T_s < 0 or T_s > T_m.
  ABOVE EQUILIBRIUM target exceeds the peak of phi_C(T): carbon is present but equilibrium
                    never holds that much of it condensed, at any temperature.
  ABOVE INVENTORY   target exceeds phi_C,max, the whole carbon inventory of the pocket.
                    Conservation; no calculation can change it.
  BELOW TABULATED   target is below phi_C at the coldest computed point (T_s = -900 K).
  NEGATIVE TARGET   the polynomial asks for f_s < phi_Al - less solid than the aluminium alone.

USAGE
  python invert_skeleton_surface_fraction.py
"""
from __future__ import annotations

import glob
import json
import os

ROOT = os.path.dirname(os.path.abspath(__file__))

METAL_MELTING_TEMPERATURE_K = 2300.0
SEARCH_BRACKET_K = (600.0, 900.0)
CARBON_MOLAR_MASS = 12.011e-3


def polynomial_target(propellant: dict, pressure_pascals: float) -> float:
    """The shipped closure: sum(c_i p^i) / omega_pocket, p in MPa."""
    megapascals = pressure_pascals / 1e6
    return sum(c * megapascals ** i
               for i, c in enumerate(propellant["pocket_surface_fraction_coefficients"])) \
        / propellant["pocket_mass_fraction"]


def carbon_curves(table: dict, ceiling: dict[str, float]) -> dict[tuple[str, int], list[tuple[float, float]]]:
    """phi_C as (T_pore, phi_C) pairs per (propellant, rounded pressure), sorted by T_pore.

    Points that violate the carbon balance are dropped.  The Gibbs minimisation is a
    constrained optimisation and it does occasionally fail to converge - one point in 550 of
    this scan came back with 37.5 mol/kg of condensed carbon against an inventory of 11.9 -
    and a single such point would otherwise become the "maximum" of a curve and silently
    decide a verdict.  phi_C > phi_C,max is impossible by conservation, so it is a sound
    filter and not a taste-based one.
    """
    curves: dict[tuple[str, int], dict[float, float]] = {}
    metal_melting = table["metalMeltingTemperatureKelvins"]

    for entry in table["propellants"]:
        aluminium = entry["aluminiumVolumeFraction"]
        for frame in entry["frames"]:
            key = (entry["name"], round(frame["pressure"]))
            point = curves.setdefault(key, {})
            for surface, coverage in zip(table["surfaceTemperaturesKelvins"], frame["coverages"]):
                point[0.5 * (surface + metal_melting)] = coverage - aluminium

    rejected = 0
    for path in sorted(glob.glob(os.path.join(ROOT, "shard_*.jsonl"))):
        for line in open(path):
            record = json.loads(line)
            if record["carbonFraction"] > ceiling[record["name"]] * (1.0 + 1e-9):
                rejected += 1
                print(f"  ! dropped {record['name']} p={record['pressure']/1e6:.2f} MPa "
                      f"T_pore={record['poreTemperature']:.0f} K: phi_C={record['carbonFraction']:.4f} "
                      f"exceeds the inventory {ceiling[record['name']]:.4f} - Gibbs solve did not converge")
                continue
            curves.setdefault((record["name"], round(record["pressure"])), {})[
                record["poreTemperature"]] = record["carbonFraction"]
    if rejected:
        print()

    return {key: sorted(point.items()) for key, point in curves.items()}


def roots(curve: list[tuple[float, float]], target: float) -> list[float]:
    """Every T_pore where the piecewise-linear curve crosses the target.

    A sample that sits exactly on the target belongs to both of its segments and would be
    reported twice, so identical roots are collapsed.  The interpolation is linear between
    computed points; on the steep cold flank that is worth a few K, which is why the grid is
    refined there rather than the roots being polished afterwards - polishing would make this
    script depend on the thermodynamics library it deliberately does not import.
    """
    found: list[float] = []
    for (t0, v0), (t1, v1) in zip(curve, curve[1:]):
        if (v0 - target) * (v1 - target) <= 0 and v0 != v1:
            crossing = t0 + (t1 - t0) * (target - v0) / (v1 - v0)
            if not found or abs(crossing - found[-1]) > 1e-6:
                found.append(crossing)
    return found


def verdict_for(surface: float) -> str:
    if surface < 0.0 or surface > METAL_MELTING_TEMPERATURE_K:
        return "UNPHYSICAL"
    return "IN BRACKET" if SEARCH_BRACKET_K[0] <= surface <= SEARCH_BRACKET_K[1] \
        else "OUTSIDE BRACKET"


def main() -> None:
    propellants = {p["name"]: p for p in
                   json.load(open(os.path.join(ROOT, "data/propellants.01234.json")))}
    table = json.load(open(os.path.join(ROOT, "data/skeleton_carbon_equilibrium.json")))
    ceiling = {entry["name"]:
               entry["carbonAvailableMolesPerKilogram"] * CARBON_MOLAR_MASS /
               table["carbonResidueDensityKilogramsPerCubicMetre"] /
               entry["matrixSpecificVolumeCubicMetresPerKilogram"]
               for entry in table["propellants"]}
    curves = carbon_curves(table, ceiling)

    print(f"closure: f_s = phi_Al + phi_C(s)(T_pore, p),  T_pore = (T_s + "
          f"{METAL_MELTING_TEMPERATURE_K:.0f})/2")
    print(f"physical range: T_s in [0, {METAL_MELTING_TEMPERATURE_K:.0f}] K "
          f"=> T_pore in [{METAL_MELTING_TEMPERATURE_K/2:.0f}, {METAL_MELTING_TEMPERATURE_K:.0f}] K; "
          f"solver bracket {SEARCH_BRACKET_K[0]:.0f}..{SEARCH_BRACKET_K[1]:.0f} K\n")

    tally: dict[str, int] = {}
    for entry in table["propellants"]:
        name = entry["name"]
        aluminium = entry["aluminiumVolumeFraction"]
        maximum = ceiling[name]
        print(f"=== {name}   phi_Al={aluminium:.4f}   phi_C,max={maximum:.4f}   "
              f"f_s at any T in [{aluminium:.4f}, {aluminium + maximum:.4f}]")
        print("%6s | %7s %9s | %8s %8s | %9s %9s | %s" %
              ("p,MPa", "f_s req", "phi_C req", "phi_C max", "T at max",
               "T_s cold", "T_s hot", "verdict"))

        for frame in entry["frames"]:
            pressure = frame["pressure"]
            curve = curves[(name, round(pressure))]
            target = polynomial_target(propellants[name], pressure) - aluminium
            peak_temperature, peak = max(curve, key=lambda point: point[1])
            coldest = curve[0]

            surfaces = [2.0 * t - METAL_MELTING_TEMPERATURE_K for t in roots(curve, target)]
            cold = [s for s in surfaces if s <= 2.0 * peak_temperature - METAL_MELTING_TEMPERATURE_K]
            hot = [s for s in surfaces if s > 2.0 * peak_temperature - METAL_MELTING_TEMPERATURE_K]

            if surfaces:
                # A root that exists is the answer; the bound-based verdicts below only ever
                # explain why there is none, so they must not pre-empt one.
                best = [verdict_for(s) for s in surfaces]
                verdict = "IN BRACKET" if "IN BRACKET" in best \
                    else "OUTSIDE BRACKET" if "OUTSIDE BRACKET" in best else "UNPHYSICAL"
            elif target < 0:
                verdict = "NEGATIVE TARGET"
            elif target > maximum:
                verdict = "ABOVE INVENTORY"
            elif target > peak:
                verdict = "ABOVE EQUILIBRIUM"
            elif target < coldest[1]:
                verdict = "BELOW TABULATED"
            else:
                verdict = "NO CROSSING"

            tally[verdict] = tally.get(verdict, 0) + 1
            print("%6.2f | %7.4f %9.4f | %8.4f %8.0f | %9s %9s | %s" % (
                pressure / 1e6, target + aluminium, target, peak, peak_temperature,
                f"{cold[0]:.0f}" if cold else "-",
                f"{hot[0]:.0f}" if hot else "-",
                verdict))
        print()

    print("verdicts:", ", ".join(f"{name} {count}" for name, count in sorted(tally.items())))


if __name__ == "__main__":
    main()
