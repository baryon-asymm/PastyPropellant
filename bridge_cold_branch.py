"""The bridge, on its own terms: its own mixture, and the temperature it actually has.

CORRECTS bridge_zero_carbon_scan.py, which asked the wrong question in two ways at once.

  WRONG COMPOSITION.  That script treated the entrained wall oxidiser as an unknown and swept it.  It
  is not unknown: the input file already declares what the inter-pocket region burns like.  Every
  composition and every pressure carries `inter_pocket_gas_phase.T_kinetic_flame` = 3707 K, against
  1591 K for the pocket's skeleton flame and 2365 K for its out-skeleton flame.  3707 K is the
  full-propellant flame temperature, and that is the statement: in a bridge the matrix film is thin
  enough that it burns together with the coarse particles bounding it, i.e. as the whole propellant.
  So the bridge mixture is the RECIPE - binder + aluminium + all the AP + octogen - with nothing swept
  and nothing invented.  Its entrainment on the old script's axis is x = (coarse AP + octogen)/matrix
  = 0.916 (Bas_0/1/2), 0.395 (Bas_3), 1.398 (Bas_4), which the flame temperature check confirms.

  WRONG BRANCH.  That script bisected for the temperature ABOVE which carbon is gasified, 1400..3200 K.
  A bridge never sees those temperatures in the condensed phase.  The pocket's T_pore = (T_s + T_m)/2
  is high only because the skeleton is a LAYER spanning surface to melting front; a bridge has no
  skeleton and therefore no melting front, so its condensed material sits at the burning surface,
  which run M converged to 628..900 K.  That is on the COLD branch, where carbon disappears into
  methane rather than into CO - C(s) + 2H2 -> CH4 runs backwards as it warms, so the carbon-free
  window there is a temperature CEILING, not a floor.

WHAT IS COMPUTED
  T_ad      the adiabatic flame temperature of the bridge mixture, to be checked against the declared
            3707 K.  Also for the pocket matrix, to be checked against 2365 K.  Neither is fitted:
            they are the input file's own numbers and this is a consistency test of the reading above.
  n_C(s)(T) condensed carbon over 400..1200 K at 1.0 and 6.5 MPa, which brackets the surface
            temperatures run M actually converged to, so the answer can be read off rather than
            extrapolated.

USAGE
  python bridge_cold_branch.py [workers] [output jsonl]
"""
from __future__ import annotations

import json
import multiprocessing
import os
import sys
import time

ROOT = os.path.dirname(os.path.abspath(__file__))
SUB = os.path.join(ROOT, "externals/src/python/AerospacePropellantThermodynamics")
sys.path.insert(0, os.path.join(SUB, "src"))
sys.path.insert(0, ROOT)

from bridge_zero_carbon import elements, per_kilogram  # noqa: E402

TEMPERATURES_K = [400.0, 500.0, 600.0, 650.0, 700.0, 750.0, 800.0,
                  850.0, 900.0, 1000.0, 1100.0, 1200.0]
PRESSURES = [1.0e6, 6.5e6]

# The grid above brackets the SURFACE temperature. It does not reach the pore temperature the skeleton
# layer actually occupies, T_pore = (T_s + T_m)/2, which is 964..1100 K at T_m = 1300 K but
# 1464..1600 K at the 2300 K the published work used. BRIDGE_TEMPERATURES extends it rather than
# letting the T_m = 2300 K case be read off by extrapolation.
if os.environ.get("BRIDGE_TEMPERATURES"):
    TEMPERATURES_K = [float(value) for value in os.environ["BRIDGE_TEMPERATURES"].split(",")]

# What the input file declares, identical for every composition and every pressure.  These are the
# targets of the consistency check, not inputs to it.
DECLARED_INTER_POCKET_FLAME_K = 3707.0
DECLARED_OUT_SKELETON_FLAME_K = 2365.0
DECLARED_SKELETON_FLAME_K = 1591.0

_PRODUCTS = None


def products():
    global _PRODUCTS
    if _PRODUCTS is None:
        from json_reader import load_combustion_products
        _PRODUCTS = load_combustion_products(os.path.join(SUB, "data/combustion_products.json"))
    return _PRODUCTS


def mixtures(propellant: dict) -> dict[str, dict[str, float]]:
    """Component masses of the two mixtures being compared, unnormalised."""
    components = propellant["components"]
    ap = components["AmmoniumPerchlorate"]
    fine = ap["mass_fraction"] * (1.0 - ap["large_particles_fraction"])
    return {
        # The bridge burns as the whole propellant - that is what a 3707 K flame means.
        "bridge": {
            "CombustibleBinder": components["CombustibleBinder"]["mass_fraction"],
            "Aluminum": components["Aluminum"]["mass_fraction"],
            "AmmoniumPerchlorate": ap["mass_fraction"],
            "Octogen": components["Octogen"]["mass_fraction"],
        },
        # The pocket matrix, for contrast: coarse AP and octogen are walls, not pocket material.
        "pocket_matrix": {
            "CombustibleBinder": components["CombustibleBinder"]["mass_fraction"],
            "Aluminum": components["Aluminum"]["mass_fraction"],
            "AmmoniumPerchlorate": fine,
        },
    }


def solve(case: dict) -> dict:
    from models import PropellantComposition
    from optimization import TemperatureOptimizer
    from utils import filter_and_construct_matrices

    started = time.time()
    matrix = PropellantComposition(enthalpy=case["enthalpy"], composition=case["composition"])

    if case["kind"] == "flame":
        optimizer = TemperatureOptimizer(pressure=case["pressure"], min_temperature=1500.0,
                                         max_temperature=5000.0, propellant=matrix,
                                         products=products())
        return case | {"value": optimizer.optimize(), "seconds": time.time() - started}

    temperature = case["temperature"]
    optimizer = TemperatureOptimizer(pressure=case["pressure"], min_temperature=temperature,
                                     max_temperature=temperature, propellant=matrix,
                                     products=products())
    context = optimizer.optimize_context_at_temperature(temperature)
    filtered, _ = filter_and_construct_matrices(products(), matrix, temperature)
    carbon = sum(amount for amount, substance in zip(context.substance_amounts, filtered)
                 if substance.phase == "condensed" and substance.formula == "C")
    return case | {"value": carbon, "seconds": time.time() - started}


def build_cases() -> list[dict]:
    cases = []
    for propellant in json.load(open(os.path.join(ROOT, "data/propellants.01234.json"))):
        for label, mixture in mixtures(propellant).items():
            composition, enthalpy = elements(mixture)
            common = {"name": propellant["name"], "mixture": label,
                      "composition": composition, "enthalpy": enthalpy,
                      "carbonTotal": composition["C"]}
            for pressure in PRESSURES:
                cases.append(common | {"kind": "flame", "pressure": pressure})
                for temperature in TEMPERATURES_K:
                    cases.append(common | {"kind": "carbon", "pressure": pressure,
                                           "temperature": temperature})
    # The adiabatic solve is a root-find over a whole Gibbs minimisation and costs minutes, while a
    # fixed-temperature one costs seconds. Kept selectable so the cheap answer is never queued behind
    # the expensive consistency check.
    kinds = os.environ.get("BRIDGE_KINDS", "carbon,flame").split(",")
    # Bas_0/1/2 share a recipe and the bridge mixture is the same for all five, so the unique
    # adiabatic solves are few; the filter exists so the expensive ones are never recomputed.
    names = os.environ.get("BRIDGE_NAMES", "").split(",") if os.environ.get("BRIDGE_NAMES") else None
    return [case for case in cases
            if case["kind"] in kinds and (names is None or case["name"] in names)]


def main() -> None:
    workers = int(sys.argv[1]) if len(sys.argv) > 1 else 4
    target = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "bridge_cold_branch.jsonl")

    cases = build_cases()
    print(f"{len(cases)} cases on {workers} workers -> {target}", flush=True)
    print(f"declared flames: inter-pocket {DECLARED_INTER_POCKET_FLAME_K:.0f} K, "
          f"out-skeleton {DECLARED_OUT_SKELETON_FLAME_K:.0f} K, "
          f"skeleton {DECLARED_SKELETON_FLAME_K:.0f} K\n", flush=True)

    started = time.time()
    done = 0
    with open(target, "w") as handle, multiprocessing.Pool(workers) as pool:
        for row in pool.imap_unordered(solve, cases, chunksize=1):
            done += 1
            handle.write(json.dumps({k: v for k, v in row.items()
                                     if k not in ("composition", "enthalpy")}) + "\n")
            handle.flush()
            label = "T_ad" if row["kind"] == "flame" else f"T={row['temperature']:.0f} K"
            units = "K" if row["kind"] == "flame" else "mol/kg"
            print(f"[{done:3}/{len(cases)}] {row['name']:7} {row['mixture']:14} "
                  f"p={row['pressure'] / 1e6:.1f} {label:>10}  {row['value']:9.4f} {units}  "
                  f"({row['seconds']:.0f} s; elapsed {(time.time() - started) / 60:.1f} min)",
                  flush=True)


if __name__ == "__main__":
    main()
