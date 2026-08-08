"""What would have to be true for the skeleton fraction to be ZERO in the inter-pocket bridges?

The solver already assumes it: `InterPocketPropellantSolver` gives the inter-pocket region a single
kinetic flame, no metal and no skeleton, i.e. f_s = 0 there by construction.  This script asks what that
assumption costs thermodynamically, in the same currency the pocket inversion used - temperature.

TWO THINGS HAVE TO VANISH, AND THEY VANISH FOR DIFFERENT REASONS
  metal   Under the Delesse closure f_s = phi_Al + phi_C(s), so f_s = 0 needs phi_Al = 0 as well.  No
          temperature below T_m can do that - it is a statement about MATERIAL, not about heat.  Above
          T_m the metal is molten and there is no skeleton left by the model's own definition, so the
          metal half of the answer is the single number T > T_m.
  carbon  Under either closure the carbon half is a genuine temperature question: at what temperature
          does Gibbs equilibrium leave NO condensed carbon in the bridge material?

WHAT THE BRIDGE IS MADE OF
  A bridge is matrix squeezed between two neighbouring coarse particles, so it is the same matrix as the
  pocket (binder + aluminium + fine AP) with far more wall per unit volume.  The wall is the composition's
  own coarse phase in its recipe ratio - coarse AP plus octogen, which for Bas_3 (no coarse AP) is octogen
  alone and for Bas_4 (no fine AP) is the only AP in the picture.

  How much of that wall oxidiser reaches the bridge is NOT a number this repository has, and it is exactly
  the kind of number this campaign refuses to invent.  So it is not assumed: it is the abscissa.  For each
  entrainment x - kilograms of wall material per kilogram of matrix - the script reports whether zero
  condensed carbon is reachable at all, and at which temperature.  x = 0 is the pocket matrix itself, so
  the pocket result is the left edge of every curve and the two calculations share an origin.

THERE IS NO ALGEBRAIC CRITERION — TESTED AND REJECTED
  The obvious short-cut is: aluminium takes 1.5 oxygen atoms each to Al2O3 and takes them first, carbon
  can then only leave as CO or CO2, so no temperature gasifies all the carbon unless

      O - 1.5*Al >= C

  It is WRONG, and the solver says so.  That inequality gives Bas_3's pocket matrix a deficit of
  2.30 mol/kg and predicts carbon surviving to 2300 K; the Gibbs minimisation returns 4e-8 mol/kg -
  none - from 2000 K at 1 MPa.  The product list shows why: 1.45 mol/kg of the aluminium leaves as
  AlCl3 / AlCl2 / AlCl, an OXYGEN-FREE exit paid for in chlorine, and the oxygen that releases is
  1.5 * 1.45 = 2.18 mol/kg - the missing amount almost exactly.  The chlorine comes from the perchlorate
  binder as much as from the AP.  Carbon has three exits too, not one: CO/CO2 on oxygen, CH4/C2H2 on
  hydrogen, HCN/CN on nitrogen.  Four elements bid for each other and only the minimisation settles it.

  What survives as a genuine bound is much weaker - carbon cannot all leave on oxygen alone if O < C -
  and even that is not decisive, since the nitrogen and hydrogen routes stay open.  So the columns below
  are DIAGNOSTIC, not a verdict: the verdict comes from `bridge_zero_carbon_scan.py`.

USAGE
  python bridge_zero_carbon.py                 # element balance, instant
  python bridge_zero_carbon_scan.py            # the equilibrium answer
"""
from __future__ import annotations

import json
import os
import sys

ROOT = os.path.dirname(os.path.abspath(__file__))
SUB = os.path.join(ROOT, "externals/src/python/AerospacePropellantThermodynamics")
sys.path.insert(0, os.path.join(SUB, "src"))

from molar_masses import ELEMENT_MOLAR_MASSES as MM  # noqa: E402

COMPONENTS = {name: body
              for entry in json.load(open(os.path.join(ROOT, "data/propellant_components.json")))
              for name, body in entry.items()}

# Entrainment grid: kg of wall material per kg of matrix.  0 is the pocket matrix.
ENTRAINMENTS = [0.0, 0.1, 0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 3.0, 5.0]


def per_kilogram(name: str) -> tuple[dict[str, float], float]:
    """Elemental composition in mol/kg and enthalpy in J/kg for one propellant component."""
    composition = COMPONENTS[name]["composition"]
    mass = sum(count * MM[element] for element, count in composition.items())
    return {element: moles / mass for element, moles in composition.items()}, \
        COMPONENTS[name]["enthalpy"]


def parts(propellant: dict, entrainment: float) -> dict[str, float]:
    """Mass of each component in one bridge, per kilogram of matrix plus `entrainment` of wall."""
    components = propellant["components"]
    ap = components["AmmoniumPerchlorate"]
    coarse = ap["mass_fraction"] * ap["large_particles_fraction"]
    octogen = components.get("Octogen", {}).get("mass_fraction", 0.0)
    wall_total = coarse + octogen

    matrix = {
        "CombustibleBinder": components["CombustibleBinder"]["mass_fraction"],
        "Aluminum": components["Aluminum"]["mass_fraction"],
        "AmmoniumPerchlorate": ap["mass_fraction"] * (1.0 - ap["large_particles_fraction"]),
    }
    matrix_total = sum(matrix.values())
    mixture = {name: mass / matrix_total for name, mass in matrix.items()}

    if wall_total > 0.0:
        # The wall arrives in its own recipe ratio - coarse AP and octogen as the composition has them.
        mixture["AmmoniumPerchlorate"] += entrainment * coarse / wall_total
        mixture["Octogen"] = mixture.get("Octogen", 0.0) + entrainment * octogen / wall_total
    return mixture


def elements(mixture: dict[str, float]) -> tuple[dict[str, float], float]:
    """(mol/kg of each element, J/kg enthalpy) for a mixture given as component masses."""
    total = sum(mixture.values())
    composition: dict[str, float] = {}
    enthalpy = 0.0
    for name, mass in mixture.items():
        if mass <= 0.0:
            continue
        per_kg, component_enthalpy = per_kilogram(name)
        weight = mass / total
        enthalpy += weight * component_enthalpy
        for element, moles in per_kg.items():
            composition[element] = composition.get(element, 0.0) + weight * moles
    return composition, enthalpy


def oxygen_margin(composition: dict[str, float]) -> float:
    """O - 1.5*Al - C, mol/kg — the naive criterion, kept only because it is instructive that it fails.

    It assumes aluminium is fully oxidised and carbon leaves only on oxygen. Both assumptions break:
    aluminium escapes as AlCl_x and carbon as HCN / C2H2. Read it as "how hard is the oxygen budget",
    not as a verdict.
    """
    return composition.get("O", 0.0) - 1.5 * composition.get("Al", 0.0) - composition.get("C", 0.0)


def chlorinated_margin(composition: dict[str, float]) -> float:
    """The same budget after the chlorine has taken what aluminium it can, one Cl per Al at least.

    Still not a criterion — HCl competes for the same chlorine — but it brackets the real answer from
    the other side, and the true Bas_3 behaviour falls between this and `oxygen_margin`.
    """
    aluminium = composition.get("Al", 0.0)
    oxidised = max(0.0, aluminium - composition.get("Cl", 0.0))
    return composition.get("O", 0.0) - 1.5 * oxidised - composition.get("C", 0.0)


def threshold_entrainment(propellant: dict) -> float | None:
    """Smallest wall entrainment for which the oxygen margin turns non-negative, by bisection."""
    if oxygen_margin(elements(parts(propellant, 0.0))[0]) >= 0.0:
        return 0.0
    high = 1.0
    for _ in range(40):
        if oxygen_margin(elements(parts(propellant, high))[0]) >= 0.0:
            break
        high *= 2.0
        if high > 1e6:
            return None
    low = high / 2.0
    for _ in range(80):
        middle = 0.5 * (low + high)
        if oxygen_margin(elements(parts(propellant, middle))[0]) >= 0.0:
            high = middle
        else:
            low = middle
    return high


def main() -> None:
    propellants = json.load(open(os.path.join(ROOT, "data/propellants.01234.json")))

    print("=" * 116)
    print("ELEMENT BALANCE OF A BRIDGE — diagnostic only; the verdict is bridge_zero_carbon_scan.py")
    print("  naive  = O - 1.5*Al - C   aluminium fully oxidised, carbon leaving only on oxygen")
    print("  Cl-fed = O - 1.5*max(0, Al - Cl) - C   aluminium chlorinated as far as the chlorine allows")
    print("  The truth lies between: Bas_3 at x = 0 has naive -2.30 yet holds no carbon above 2000 K.")
    print("=" * 116)
    print(f"{'':7}{'x = wall/matrix':>16}{'C':>9}{'O':>9}{'Al':>9}{'Cl':>8}{'O/C':>7}"
          f"{'naive':>9}{'Cl-fed':>9}")

    for propellant in propellants:
        for entrainment in ENTRAINMENTS:
            composition, _ = elements(parts(propellant, entrainment))
            carbon = composition.get("C", 0.0)
            print(f"{propellant['name']:7}{entrainment:16.2f}{carbon:9.3f}"
                  f"{composition.get('O', 0.0):9.3f}{composition.get('Al', 0.0):9.3f}"
                  f"{composition.get('Cl', 0.0):8.3f}"
                  f"{composition.get('O', 0.0) / carbon:7.2f}{oxygen_margin(composition):9.3f}"
                  f"{chlorinated_margin(composition):9.3f}")
        threshold = threshold_entrainment(propellant)
        print(f"{propellant['name']:7}{'naive x* =':>16} "
              f"{'never' if threshold is None else f'{threshold:.3f}'} kg wall per kg matrix "
              f"(an over-estimate — see the header)\n")


if __name__ == "__main__":
    main()
