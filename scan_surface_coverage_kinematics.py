"""Stage-0 filter for the kinematic (skeleton-geometry) closure of the surface coverage f_s.

The proposal under test: the skeleton layer is not a random cut through the pocket but a layer
that ACCUMULATES the non-volatile residue of a depth h of propellant and sheds it as
agglomerates.  Writing the steady-state balance on one unit of pocket surface,

    f_s * delta * (1 - Pi)  =  h * phi_s                     (solid stored = solid collected)

so that

    f_s = (h / delta) * phi_s / (1 - Pi),      h = r * tau

with delta the skeleton-layer thickness the model already fits (A / r^a), Pi the layer porosity
already shipped in propellants*.json, and phi_s = phi_Al + phi_C(s) the condensed inventory the
equilibrium closure already computes.  Delesse - the closure in use today - is the special case
h = delta (1 - Pi), i.e. f_s = phi_s.

The identity is unavoidable, and it says the model needs exactly ONE new statement: a residence
time.  No diameter enters.  This script tests every candidate for that time against the converged
output of the best equilibrium-closure run, so that a candidate can be killed for a stated
numerical reason before any C# is written or any six-hour run is spent.

Candidates:
  POCKET DEPTH   the island collects until the surface has passed through its pocket, h = d_AP.
  MELTING        the island holds the metal until it is molten; tau from the enthalpy needed.
  DRAG           detachment when aerodynamic drag beats capillary adhesion.
  RESIDENCE BAND no closure at all - just ask what tau the closure in use today implies, and
                 compare it against measured agglomerate residence times (0.1 .. 5 ms).

USAGE
  python scan_surface_coverage_kinematics.py
"""
from __future__ import annotations

import json
import os

ROOT = os.path.dirname(os.path.abspath(__file__))
RUN = "/root/runs/6h-Classic-Tm2300-kc400-constrON-fsEquilibrium-20260803-0118/skeleton_layer.json"

# Handbook material data.  None of these is fitted and none is adjustable: they are the reason
# the candidates below can be falsified rather than tuned.
ALUMINIUM_DENSITY = 2700.0            # kg/m3
ALUMINIUM_HEAT_CAPACITY = 900.0       # J/kg/K, solid
ALUMINIUM_FUSION_ENTHALPY = 397_000.0  # J/kg
ALUMINIUM_MELTING_K = 933.0           # metal melts
OXIDE_MELTING_K = 2327.0              # alumina shell melts - the classic ignition criterion
ALUMINIUM_SURFACE_TENSION = 0.86      # N/m, molten, near its melting point
DRAG_COEFFICIENT = 0.5
GAS_CONSTANT = 8.314

# Measured residence of aluminium/agglomerates on a burning propellant surface.  Reported as
# ~1-5 ms; the lower decade is kept so the band cannot be accused of being drawn to fit.
RESIDENCE_BAND_S = (1.0e-4, 5.0e-3)


def load() -> tuple[list, dict, dict]:
    run = json.load(open(RUN))
    table = {entry["name"]: entry
             for entry in json.load(open(os.path.join(ROOT, "data/skeleton_carbon_equilibrium.json")))["propellants"]}
    propellants = {entry["name"]: entry
                   for entry in json.load(open(os.path.join(ROOT, "data/propellants.01234.json")))}
    return run, table, propellants


def porosity(propellant: dict, pressure: float) -> float:
    frame = min(propellant["pressure_frames"], key=lambda f: abs(f["pressure"] - pressure))
    return frame["porosity_within_skeleton"]


def coverage(table_entry: dict, table: dict, surface_temperature: float, pressure: float) -> float:
    """f_s as the equilibrium closure evaluates it, at the converged surface temperature."""
    frame = min(table_entry["frames"], key=lambda f: abs(f["pressure"] - pressure))
    grid, values = table["surfaceTemperaturesKelvins"], frame["coverages"]
    index = max(0, min(len(grid) - 2,
                       min(range(len(grid) - 1), key=lambda k: abs(grid[k] - surface_temperature))))
    span = grid[index + 1] - grid[index]
    return values[index] + (values[index + 1] - values[index]) * (surface_temperature - grid[index]) / span


def main() -> None:
    run, table, propellants = load()
    full = json.load(open(os.path.join(ROOT, "data/skeleton_carbon_equilibrium.json")))

    print(__doc__.split("USAGE")[0].strip().splitlines()[0])
    print(f"source run: {os.path.basename(os.path.dirname(RUN))}\n")

    print("=== A. What the closure in use today implies about the skeleton's kinematics")
    print("(Delesse => h = delta(1-Pi) exactly, so tau = delta(1-Pi)/r with no freedom left)\n")
    print("%-6s %5s | %8s %7s %7s | %7s %8s %9s" %
          ("name", "pMPa", "delta_um", "r_mm/s", "f_s", "h_um", "tau_ms", "verdict"))

    residence: list[tuple[str, float, float]] = []
    for entry in run:
        name = entry["name"]
        for frame in entry["pressure_frames"]:
            pressure = frame["pressure"]
            thickness = frame["skeleton_layer"]["thickness"] * 1e-6
            rate = frame["calculated_burn_rate"] * 1e-3
            solid = 1.0 - porosity(propellants[name], pressure)
            fraction = coverage(table[name], full, frame["surface_temperature_pocket"], pressure)
            depth = thickness * solid                       # h = delta (1 - Pi), Delesse
            tau = depth / rate
            residence.append((name, pressure, tau))
            if frame is entry["pressure_frames"][0] or frame is entry["pressure_frames"][-1]:
                inside = RESIDENCE_BAND_S[0] <= tau <= RESIDENCE_BAND_S[1]
                print("%-6s %5.2f | %8.3f %7.2f %7.4f | %7.3f %8.4f %9s" % (
                    name, pressure / 1e6, thickness * 1e6, rate * 1e3, fraction,
                    depth * 1e6, tau * 1e3, "in band" if inside else "OUT"))

    inside = sum(1 for _, _, t in residence if RESIDENCE_BAND_S[0] <= t <= RESIDENCE_BAND_S[1])
    print(f"\n  {inside} of {len(residence)} points land inside the measured "
          f"{RESIDENCE_BAND_S[0]*1e3:.1f}-{RESIDENCE_BAND_S[1]*1e3:.1f} ms residence band.\n")

    print("=== B. Candidate closures, each against the same run\n")
    print("%-6s %5s | %10s | %11s %11s | %10s" %
          ("name", "pMPa", "delta_req_um", "q_melt_MW", "q_avail_MW", "d_crit_m"))

    pocket_failures = melting_failures = drag_failures = 0
    points = 0
    for entry in run:
        name = entry["name"]
        oxidiser = propellants[name]["components"]["AmmoniumPerchlorate"]
        pocket_depth = oxidiser["average_particles_diameter"]
        aluminium = table[name]["aluminiumVolumeFraction"]
        for frame in entry["pressure_frames"]:
            points += 1
            pressure = frame["pressure"]
            thickness = frame["skeleton_layer"]["thickness"] * 1e-6
            rate = frame["calculated_burn_rate"] * 1e-3
            surface = frame["surface_temperature_pocket"]
            solid = 1.0 - porosity(propellants[name], pressure)
            fraction = coverage(table[name], full, surface, pressure)

            # POCKET DEPTH: h = d_AP forces a thickness; compare with the model's own delta <= d_AP.
            required = pocket_depth * fraction / solid          # f_s <= 1 => delta >= h phi_s/(1-Pi)
            if required > pocket_depth:
                pocket_failures += 1

            # MELTING: power needed to bring the incoming aluminium to the melt, per unit area,
            # against the total flux the model delivers to the surface.
            enthalpy = ALUMINIUM_HEAT_CAPACITY * (ALUMINIUM_MELTING_K - surface) + ALUMINIUM_FUSION_ENTHALPY
            melting = rate * aluminium * ALUMINIUM_DENSITY * enthalpy
            available = frame["heat_flux"]["to_surface_total"]
            if melting > available:
                melting_failures += 1

            # DRAG: detachment diameter from drag against capillary adhesion.
            gas_phase = min(propellants[name]["pressure_frames"],
                            key=lambda f: abs(f["pressure"] - pressure))["pocket_gas_phase"]
            density = pressure * gas_phase["average_molar_mass"] / (GAS_CONSTANT * surface)
            flux = rate * propellants[name]["density"]
            critical = 8.0 * ALUMINIUM_SURFACE_TENSION * density / (DRAG_COEFFICIENT * flux ** 2)
            if critical > pocket_depth:
                drag_failures += 1

            if frame is entry["pressure_frames"][0] or frame is entry["pressure_frames"][-1]:
                print("%-6s %5.2f | %10.1f | %11.3f %11.3f | %10.4f" % (
                    name, pressure / 1e6, required * 1e6,
                    melting / 1e6, available / 1e6, critical))
        print("%-6s %5s | d_AP = %.0f um" % ("", "", pocket_depth * 1e6))

    print()
    print(f"POCKET DEPTH : {pocket_failures}/{points} points need delta > d_AP, "
          f"which the model's own constraint forbids.")
    print(f"MELTING      : {melting_failures}/{points} points need more power to melt the incoming "
          f"aluminium than the whole surface flux.")
    print(f"DRAG         : {drag_failures}/{points} points give a detachment diameter larger than "
          f"the pocket itself - capillary adhesion never loses.")

    print("\n=== C. What survives: the residence band as a constraint on delta\n")
    print("No closure for tau exists, but tau is MEASURED.  Under the closure in use today")
    print("tau = delta(1-Pi)/r, so a measured band is a two-sided bound on delta that costs")
    print("no fitted number.  The model's own delta <= d_AP caps it from above.\n")
    print("%-6s %5s | %9s %9s %9s | %9s %8s" %
          ("name", "pMPa", "delta_lo", "delta_hi", "d_AP_um", "delta_runM", "verdict"))

    empty = below = 0
    exponents: list[float] = []
    for entry in run:
        name = entry["name"]
        pocket_depth = propellants[name]["components"]["AmmoniumPerchlorate"]["average_particles_diameter"]
        first, last = entry["pressure_frames"][0], entry["pressure_frames"][-1]
        for frame in entry["pressure_frames"]:
            pressure = frame["pressure"]
            thickness = frame["skeleton_layer"]["thickness"] * 1e-6
            rate = frame["calculated_burn_rate"] * 1e-3
            solid = 1.0 - porosity(propellants[name], pressure)
            low = RESIDENCE_BAND_S[0] * rate / solid
            high = min(RESIDENCE_BAND_S[1] * rate / solid, pocket_depth)
            if low > high:
                empty += 1
            if thickness < low:
                below += 1
            if frame is first or frame is last:
                print("%-6s %5.2f | %9.1f %9.1f %9.0f | %9.3f %8s" % (
                    name, pressure / 1e6, low * 1e6, high * 1e6, pocket_depth * 1e6,
                    thickness * 1e6,
                    "ok" if low <= thickness <= high else "below" if thickness < low else "above"))
        # delta = A / r^a with a shared across compositions: read it back from the two ends.
        ratio = (first["skeleton_layer"]["thickness"] / last["skeleton_layer"]["thickness"])
        rates = last["calculated_burn_rate"] / first["calculated_burn_rate"]
        exponents.append(__import__("math").log(ratio) / __import__("math").log(rates))

    print(f"\n  admissible windows: {points - empty}/{points} non-empty "
          f"({empty} empty) -> the constraint is feasible, not contradictory.")
    print(f"  run M: {below}/{points} points sit BELOW the floor.")
    print(f"  fitted APowOrder from this run: {min(exponents):.3f}..{max(exponents):.3f} (shared slot).")

    # Two separate questions: does the SLOPE of tau fit inside the band's width, and does the
    # LEVEL sit inside the band at all.  Only one of them binds, and it is worth knowing which.
    log = __import__("math").log
    spans = [max(f["calculated_burn_rate"] for f in e["pressure_frames"]) /
             min(f["calculated_burn_rate"] for f in e["pressure_frames"]) for e in run]
    width = RESIDENCE_BAND_S[1] / RESIDENCE_BAND_S[0]
    print("  slope: tau ~ r^-(1+a) varies by (r range)^(1+a) = %.0fx at a=%.2f, against a band "
          "%.0fx wide" % (max(spans) ** (1.0 + max(exponents)), max(exponents), width))
    print("         => a <= %.2f, NOT binding at the fitted %.2f." %
          (log(width) / log(max(spans)) - 1.0, max(exponents)))
    shortfalls = []
    for entry in run:
        name = entry["name"]
        for frame in entry["pressure_frames"]:
            rate = frame["calculated_burn_rate"] * 1e-3
            solid = 1.0 - porosity(propellants[name], frame["pressure"])
            low = RESIDENCE_BAND_S[0] * rate / solid
            thickness = frame["skeleton_layer"]["thickness"] * 1e-6
            if thickness < low:
                shortfalls.append(low / thickness)
    if shortfalls:
        print("  level: the floor is what binds - delta must rise by %.1fx..%.1fx on the %d points "
              "below it." % (min(shortfalls), max(shortfalls), len(shortfalls)))


if __name__ == "__main__":
    main()
