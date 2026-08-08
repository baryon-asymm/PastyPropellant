"""At which temperature does an inter-pocket bridge stop holding condensed carbon?

Companion to `bridge_zero_carbon.py`, which builds the bridge compositions.  This one answers the
temperature question by Gibbs minimisation, because - as that script's header records - the algebraic
short-cut is wrong here.

WHY THERE IS NO ALGEBRAIC CRITERION.  "Aluminium takes 1.5 oxygen each, so carbon can vanish only if
O - 1.5*Al >= C" predicts that Bas_3's pocket matrix keeps its carbon to 2300 K.  The solver says the
opposite: at 2000 K and 1 MPa Bas_3 holds 4e-8 mol/kg, i.e. none.  The reason is in the product list -
1.45 mol/kg of its aluminium leaves as AlCl3 / AlCl2 / AlCl, an OXYGEN-FREE exit, and the oxygen that
frees is exactly what gasifies the carbon (1.5 * 1.45 = 2.18 against a predicted deficit of 2.30).
Carbon likewise has three exits, not one: CO/CO2 on oxygen, CH4/C2H2 on hydrogen, HCN/CN on nitrogen.
Four elements compete for each other and only the minimisation resolves it.  So: no criterion, scan.

WHAT IS SCANNED
  composition  all five
  entrainment  x = kg of wall material (the composition's own coarse AP + octogen, in recipe ratio)
               per kg of matrix.  x = 0 is the pocket matrix, so the pocket answer is the left edge.
  aluminium    variant A keeps it (the bridge is matrix, and matrix carries aluminium);
               variant B removes it, which is the solver's own inter-pocket assumption - no metal -
               taken literally.  The gap between A and B is the aluminium's share of the difficulty.
  pressure     1.0 and 6.5 MPa, the ends of the measured window.

For each case the lowest temperature at which condensed carbon vanishes is found by bisection on
[1400, 3200] K.  Carbon is non-monotone in temperature but vanishes only on the hot branch and stays
vanished, so the predicate is monotone there and bisection is sound.  A case whose carbon survives
3200 K is reported as unreachable rather than extrapolated.

USAGE
  python bridge_zero_carbon_scan.py [workers] [output jsonl]
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

from bridge_zero_carbon import elements, parts  # noqa: E402

ENTRAINMENTS = [float(value) for value in
                os.environ.get("BRIDGE_ENTRAINMENTS", "0.25,0.5,1.0,2.0").split(",")]
PRESSURES = [1.0e6, 6.5e6]
LOW_KELVINS, HIGH_KELVINS = 1400.0, 3200.0
RESOLUTION_KELVINS = 12.5
ZERO_TOLERANCE = 1.0e-4          # mol/kg; converged zeros come back at 1e-7, live carbon at 0.1..12

_PRODUCTS = None


def condensed_carbon(composition: dict[str, float], enthalpy: float,
                     temperature: float, pressure: float) -> float:
    global _PRODUCTS
    from json_reader import load_combustion_products
    from models import PropellantComposition
    from optimization import TemperatureOptimizer
    from utils import filter_and_construct_matrices

    if _PRODUCTS is None:
        _PRODUCTS = load_combustion_products(os.path.join(SUB, "data/combustion_products.json"))

    matrix = PropellantComposition(enthalpy=enthalpy, composition=composition)
    optimizer = TemperatureOptimizer(pressure=pressure, min_temperature=temperature,
                                     max_temperature=temperature, propellant=matrix,
                                     products=_PRODUCTS)
    context = optimizer.optimize_context_at_temperature(temperature)
    filtered, _ = filter_and_construct_matrices(_PRODUCTS, matrix, temperature)
    return sum(amount for amount, substance in zip(context.substance_amounts, filtered)
               if substance.phase == "condensed" and substance.formula == "C")


def solve(case: dict) -> dict:
    started = time.time()
    composition, enthalpy = case["composition"], case["enthalpy"]
    pressure = case["pressure"]
    calls = 0

    top = condensed_carbon(composition, enthalpy, HIGH_KELVINS, pressure)
    calls += 1
    if top > ZERO_TOLERANCE:
        return case | {"zeroTemperature": None, "carbonAtCeiling": top,
                       "calls": calls, "seconds": time.time() - started}

    low, high = LOW_KELVINS, HIGH_KELVINS
    while high - low > RESOLUTION_KELVINS:
        middle = 0.5 * (low + high)
        calls += 1
        if condensed_carbon(composition, enthalpy, middle, pressure) > ZERO_TOLERANCE:
            low = middle
        else:
            high = middle
    return case | {"zeroTemperature": high, "carbonAtCeiling": top,
                   "calls": calls, "seconds": time.time() - started}


def build_cases() -> list[dict]:
    propellants = json.load(open(os.path.join(ROOT, "data/propellants.01234.json")))
    cases = []
    for propellant in propellants:
        for entrainment in ENTRAINMENTS:
            mixture = parts(propellant, entrainment)
            for variant in ("with_aluminium", "no_aluminium"):
                trimmed = dict(mixture)
                if variant == "no_aluminium":
                    trimmed.pop("Aluminum", None)
                composition, enthalpy = elements(trimmed)
                for pressure in PRESSURES:
                    cases.append({
                        "name": propellant["name"],
                        "entrainment": entrainment,
                        "variant": variant,
                        "pressure": pressure,
                        "composition": composition,
                        "enthalpy": enthalpy,
                    })
    return cases


def main() -> None:
    workers = int(sys.argv[1]) if len(sys.argv) > 1 else 4
    target = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "bridge_zero_carbon.jsonl")

    cases = build_cases()
    print(f"{len(cases)} cases on {workers} workers -> {target}", flush=True)
    started = time.time()
    done = 0
    with open(target, "w") as handle, multiprocessing.Pool(workers) as pool:
        for row in pool.imap_unordered(solve, cases):
            done += 1
            handle.write(json.dumps({k: v for k, v in row.items()
                                     if k not in ("composition", "enthalpy")}) + "\n")
            handle.flush()
            temperature = row["zeroTemperature"]
            print(f"[{done:3}/{len(cases)}] {row['name']:7} x={row['entrainment']:.2f} "
                  f"{row['variant']:14} p={row['pressure'] / 1e6:.1f} MPa  "
                  f"T_zero = {'>3200 K' if temperature is None else f'{temperature:.0f} K':>8}  "
                  f"({row['calls']} solves, {row['seconds']:.0f} s; "
                  f"elapsed {(time.time() - started) / 60:.1f} min)", flush=True)


if __name__ == "__main__":
    main()
