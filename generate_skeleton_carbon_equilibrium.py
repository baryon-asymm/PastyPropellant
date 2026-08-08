"""Generate `data/skeleton_carbon_equilibrium.json` — the equilibrium closure for f_s.

WHAT THIS REPLACES
  `f_s`, the share of the pocket surface carrying a skeleton layer, used to come from a
  per-propellant polynomial fitted to agglomeration measurements over 1..6.5 MPa (the
  `pocket_surface_fraction_coefficients` of `data/propellants*.json`).  Twenty-two fitted
  coefficients, none of which transfers to an unmeasured composition, and four of which
  go negative or turn upward outside the fitted window.

WHAT REPLACES IT
  The skeleton is the aluminium of the pocket held together by the carbonaceous residue of
  binder pyrolysis.  f_s is an AREA fraction, and by Delesse's theorem the area fraction on
  a random section equals the volume fraction, so f_s is the volume fraction of the pocket
  that is still solid at the surface:

      f_s(p, T_s) = phi_Al + phi_C(s)(T_pore, p) ,     T_pore = (T_s + T_m) / 2

  phi_Al is fixed by the recipe.  phi_C(s) is what equilibrium leaves condensed of the
  binder's carbon: at the temperature and pressure inside the pocket it decides how much
  carbon stays solid and how much leaves as methane.

  WHICH TEMPERATURE THE SKELETON ACTUALLY SPANS.  T_m is the metal melting temperature,
  the model's `model.metalMeltingTemperatureKelvins`.  The skeleton layer is, by this
  model's own definition, the region between the burning surface and the point where the
  metal melts - above that there is no skeleton left to speak of.  So the carbon inside it
  lives between T_s and T_m, and the representative value is the mean of those two.

  An earlier version used (T_flame,skeleton + T_s)/2 instead.  That is the temperature of
  the GAS FLAME sitting on the skeleton: it says where the heat comes from, not what
  temperature the condensed skeleton reaches, and it is the wrong quantity.  It also made
  the closure depend on a precomputed field of the input file rather than on a declared
  model constant that the resolved run record already carries.  Three artefacts of that
  mistake are gone with it: Bas_3 sat at 1611..1761 K, past the carbon maximum, and so
  came out flat-to-rising with pressure where every other composition fell; Bas_4 sat at
  an implausibly cold 970 K; and the pore temperature differed between compositions for
  no reason other than a gas-phase field.  Under the corrected rule every composition
  shares one pore temperature at a given T_s, and all five gain condensed carbon.

  THERE IS NO SCALE FACTOR, and none is allowed - Delesse fixes the mapping.  An earlier
  version normalised by the carbon inventory instead (n_C(s)/n_C,total).  That reads as a
  char yield rather than an area fraction, it overshot the measured agglomeration by
  0.7..4.3x, and it threw away the aluminium, whose volume fraction is the one recipe
  difference between these compositions (0.170 / 0.218 / 0.258, through the fine-AP
  dilution).  With the aluminium in, the overshoot band is 0.6..2.1x and the compositions
  come out in the measured order at both ends.

  C(s) + 2H2 -> CH4 is the only carbon sink that runs with a DECREASE in mole number
  (dn = -1), so it is the only one pressure drives forward; Boudouard (C + CO2) and steam
  gasification (C + H2O) both go with dn = +1 and are suppressed by pressure.  That is why
  f_s falls with pressure at all, and it is a thermodynamic fact, not a fitted trend.

  The pocket gas is 19..71 mol % H2 over the whole window (the aluminium locks the oxygen
  into Al2O3 and leaves the hydrogen free), so the reagent is in excess and equilibrium,
  not supply, sets the answer.

INPUTS - THERE ARE NO OTHERS, AND NONE IS FITTED
  recipe          data/propellants.01234.json, data/propellant_components.json
                  -> the pocket matrix is binder + aluminium + FINE AP.  Coarse AP and
                     octogen are the pocket WALLS, not pocket material, so they are
                     excluded; the fine/coarse split is the file's own
                     `large_particles_fraction`.
  thermodynamics  externals/.../AerospacePropellantThermodynamics, Gibbs minimisation at
                  fixed T and fixed p.
  T_m             the run's `model.metalMeltingTemperatureKelvins`.  It is RECORDED in the
                  output so a table can never be silently paired with a run configured for
                  a different melting temperature - the table and the run must agree.

OUTPUT
  One table per (propellant, pressure) over the surface-temperature search bracket
  600..900 K, which is what the solver bisects on.  The table is a pure function of recipe
  and thermodynamics - it does not depend on the optimisation vector - so it is computed
  once here rather than inside the fitness function, where a Gibbs minimisation per
  bisection step would be ruinous.

USAGE
  python generate_skeleton_carbon_equilibrium.py [propellants file] [output file] [T_m, K]

  PORE_RULE=mean | offset:<K>        which temperature the carbon is held at (default mean)
  NORMALISATION=delesse | yield      which quantity `coverages` holds  (default delesse)

  Both are written into the output as `poreTemperatureRule` / `coverageNormalisation`, because
  the solver reads only the coverages and so cannot tell one table from another. See the block
  above their definitions for why either would be changed.
"""
from __future__ import annotations

import json
import os
import sys
import time

ROOT = os.path.dirname(os.path.abspath(__file__))
SUB = os.path.join(ROOT, "externals/src/python/AerospacePropellantThermodynamics")
sys.path.insert(0, os.path.join(SUB, "src"))

from json_reader import load_combustion_products          # noqa: E402
from models import PropellantComposition                  # noqa: E402
from optimization import TemperatureOptimizer             # noqa: E402
from utils import filter_and_construct_matrices           # noqa: E402
from molar_masses import ELEMENT_MOLAR_MASSES as MM       # noqa: E402

# The solver's own bracket (SurfaceTemperatureSearchBounds).  Sampled at both ends and in
# between; the equilibrium carbon curve is smooth over this range, so five points carry it.
SURFACE_TEMPERATURES_K = [600.0, 675.0, 750.0, 825.0, 900.0]

# Density of the carbonaceous residue, kg/m3. A MATERIAL PROPERTY, not a knob: pyrolytic
# char and soot sit between about 1000 and 2000, with dense graphite at 2260. It is the one
# number here that is not recipe or thermodynamics, and it barely matters - halving it from
# 2000 to 1000 moves the predicted/measured band from 0.57..2.03 to 0.59..2.13.
CARBON_RESIDUE_DENSITY = 1500.0

CARBON_MOLAR_MASS = 12.011e-3   # kg/mol

# Metal melting temperature, K - the upper end of the range the skeleton spans.  Must match
# the run's `model.metalMeltingTemperatureKelvins`; it is written into the output so the two
# can be checked against each other instead of being assumed to agree.
METAL_MELTING_TEMPERATURE_K = 2300.0

# WHICH TEMPERATURE, AND WHICH NORMALISATION - both selectable, both recorded in the output.
#
# Neither is a free parameter to tune: they are two model statements that have each been in
# force at different points, and a table must say which one built it or its numbers cannot be
# read.  The C# side consumes only `surfaceTemperaturesKelvins` and `coverages`, so nothing
# downstream can detect the difference - which is exactly why it is written down here.
#
#   PORE_RULE = "mean"        T_pore = (T_s + T_m)/2 - the skeleton spans surface to melting
#                             front, so its carbon lives at the mean of the two.  Default.
#               "offset:X"    T_pore = T_s + X.  A fixed rise across the layer instead of one
#                             pinned to T_m.  It decouples the pore temperature from the metal
#                             melting temperature entirely, which matters because at T_m = 2300 K
#                             the mean rule puts T_pore at 1450..1600 K, where the equilibrium
#                             carbon curve is SATURATED at 0.94..0.98 of the inventory and has
#                             no discrimination left.  An offset of 200 K puts it at 800..1100 K,
#                             on the rising flank where the curve actually varies.
#
#   NORMALISATION = "delesse" f_s = phi_Al + phi_C(s).  Area fraction via Delesse's theorem.
#                   "yield"   f_s = n_C(s)/n_C,total.  The char yield, which is what run M used
#                             (its table carries no `aluminiumVolumeFraction` field).  It drops
#                             the aluminium, so it is NOT an area fraction; kept because run M's
#                             scheme is a reference point that has to stay reproducible.
PORE_RULE = os.environ.get("PORE_RULE", "mean")
NORMALISATION = os.environ.get("NORMALISATION", "delesse")


def pore_temperature_of(surface_temperature: float) -> float:
    if PORE_RULE == "mean":
        return 0.5 * (surface_temperature + METAL_MELTING_TEMPERATURE_K)
    if PORE_RULE.startswith("offset:"):
        return surface_temperature + float(PORE_RULE.split(":", 1)[1])
    raise SystemExit(f"unknown PORE_RULE {PORE_RULE!r}: expected 'mean' or 'offset:<kelvins>'")


def pore_rule_description() -> str:
    return "T_pore = (T_s + T_m)/2" if PORE_RULE == "mean" \
        else f"T_pore = T_s + {float(PORE_RULE.split(':', 1)[1]):.0f}"

PRODUCTS = load_combustion_products(os.path.join(SUB, "data/combustion_products.json"))
COMPONENTS = {name: body
              for entry in json.load(open(os.path.join(ROOT, "data/propellant_components.json")))
              for name, body in entry.items()}


def _per_kg(name: str):
    """Elemental composition in mol/kg and enthalpy in J/kg for one propellant component."""
    composition = COMPONENTS[name]["composition"]
    mass = sum(count * MM[element] for element, count in composition.items())
    return {e: n / mass for e, n in composition.items()}, COMPONENTS[name]["enthalpy"]


def pocket_matrix(propellant) -> PropellantComposition:
    """The material that actually pyrolyses inside a pocket: binder + aluminium + fine AP."""
    components = propellant["components"]
    ap = components["AmmoniumPerchlorate"]
    parts = {
        "CombustibleBinder": components["CombustibleBinder"]["mass_fraction"],
        "Aluminum": components["Aluminum"]["mass_fraction"],
        "AmmoniumPerchlorate": ap["mass_fraction"] * (1.0 - ap["large_particles_fraction"]),
    }
    total = sum(parts.values())
    elements: dict[str, float] = {}
    enthalpy = 0.0
    for name, mass in parts.items():
        composition, component_enthalpy = _per_kg(name)
        weight = mass / total
        enthalpy += weight * component_enthalpy
        for element, moles in composition.items():
            elements[element] = elements.get(element, 0.0) + weight * moles
    return PropellantComposition(enthalpy=enthalpy, composition=elements)


def pocket_volumetry(propellant) -> tuple[float, float]:
    """(specific volume of the pocket matrix in m3/kg, aluminium volume fraction of it).

    The denominator is the volume of the material that WAS there, not what is left: f_s is
    the fraction of a section through the pocket that carries skeleton, and the section is
    taken through the original matrix.
    """
    components = propellant["components"]
    ap = components["AmmoniumPerchlorate"]
    parts = {
        "CombustibleBinder": (components["CombustibleBinder"]["mass_fraction"],
                              components["CombustibleBinder"]["density"]),
        "Aluminum": (components["Aluminum"]["mass_fraction"], components["Aluminum"]["density"]),
        "AmmoniumPerchlorate": (ap["mass_fraction"] * (1.0 - ap["large_particles_fraction"]),
                                ap["density"]),
    }
    total_mass = sum(mass for mass, _ in parts.values())
    specific_volume = sum(mass / total_mass / density for mass, density in parts.values())
    aluminium_mass, aluminium_density = parts["Aluminum"]
    aluminium_fraction = (aluminium_mass / total_mass / aluminium_density) / specific_volume
    return specific_volume, aluminium_fraction


def condensed_carbon(matrix: PropellantComposition, temperature: float, pressure: float) -> float:
    """Moles of condensed carbon per kg of matrix at equilibrium, fixed T and fixed p."""
    optimizer = TemperatureOptimizer(pressure=pressure, min_temperature=temperature,
                                     max_temperature=temperature, propellant=matrix,
                                     products=PRODUCTS)
    context = optimizer.optimize_context_at_temperature(temperature)
    filtered, _ = filter_and_construct_matrices(PRODUCTS, matrix, temperature)
    return sum(amount for amount, substance in zip(context.substance_amounts, filtered)
               if substance.phase == "condensed" and substance.formula == "C")


def main() -> None:
    global METAL_MELTING_TEMPERATURE_K
    source = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "data/propellants.01234.json")
    target = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "data/skeleton_carbon_equilibrium.json")
    if len(sys.argv) > 3:
        METAL_MELTING_TEMPERATURE_K = float(sys.argv[3])
    print(f"{pore_rule_description()}  "
          f"=> {pore_temperature_of(SURFACE_TEMPERATURES_K[0]):.0f}"
          f"..{pore_temperature_of(SURFACE_TEMPERATURES_K[-1]):.0f} K"
          f"   |   normalisation: {NORMALISATION}"
          f"   |   T_m = {METAL_MELTING_TEMPERATURE_K:.0f} K\n", flush=True)

    propellants = json.load(open(source))
    started = time.time()
    tables = []

    for propellant in propellants:
        matrix = pocket_matrix(propellant)
        carbon_available = matrix.composition["C"]
        specific_volume, aluminium_fraction = pocket_volumetry(propellant)
        frames = []
        for frame in propellant["pressure_frames"]:
            flame = frame["pocket_gas_phase"]["skeleton_gas_phase"]["T_kinetic_flame"]
            coverages = []
            for surface_temperature in SURFACE_TEMPERATURES_K:
                pore_temperature = pore_temperature_of(surface_temperature)
                stable = condensed_carbon(matrix, pore_temperature, frame["pressure"])
                carbon_fraction = (stable * CARBON_MOLAR_MASS / CARBON_RESIDUE_DENSITY) \
                    / specific_volume
                coverages.append(aluminium_fraction + carbon_fraction if NORMALISATION == "delesse"
                                 else stable / carbon_available)
                print(f"{propellant['name']:7} p={frame['pressure']/1e6:5.2f} MPa "
                      f"Ts={surface_temperature:5.0f} K  T_pore={pore_temperature:7.1f} K  "
                      f"C(s)={stable:7.3f} mol/kg  phi_C={carbon_fraction:.4f}  "
                      f"f_s={coverages[-1]:.4f}", flush=True)
            frames.append({
                "pressure": frame["pressure"],
                "skeletonFlameTemperature": flame,
                "coverages": coverages,
            })
        tables.append({
            "name": propellant["name"],
            "carbonAvailableMolesPerKilogram": carbon_available,
            "aluminiumVolumeFraction": aluminium_fraction,
            "matrixSpecificVolumeCubicMetresPerKilogram": specific_volume,
            "frames": frames,
        })

    json.dump({
        "surfaceTemperaturesKelvins": SURFACE_TEMPERATURES_K,
        "carbonResidueDensityKilogramsPerCubicMetre": CARBON_RESIDUE_DENSITY,
        "metalMeltingTemperatureKelvins": METAL_MELTING_TEMPERATURE_K,
        # Not read by the solver — it consumes coverages and nothing else. Recorded because a
        # table is unreadable without them: the same five numbers mean different things under
        # a different pore rule or a different normalisation.
        "poreTemperatureRule": pore_rule_description(),
        "coverageNormalisation": NORMALISATION,
        "poreTemperaturesKelvins": [pore_temperature_of(t) for t in SURFACE_TEMPERATURES_K],
        "propellants": tables,
    }, open(target, "w"), indent=1)
    print(f"\nwrote {target} in {time.time() - started:.0f} s")


if __name__ == "__main__":
    main()
