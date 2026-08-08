"""Confront the model's skeleton coverage with V. A. Babuk's *published measurements*.

Provenance of every external number used here (nothing in this file is fitted or tuned):

  [A] Бабук В. А., Куклин Д. И., Нарыжный С. Ю., Низяев А. А. "Рецептурные факторы и
      закономерности горения пастообразных топлив" // Proceedings of the 10th International
      Seminar on Flame Structure, 2024, pp. 39-47. DOI 10.53954/9785605098669_39.
      http://kinetics.nsc.ru/10ISFS/Proceedings/Proceedings%20of%20the%2010ISFS_2024.pdf
        - Table 1: what actually distinguishes Bas_0..Bas_4.
        - Fig. 2: Z_m^a(p) - mass fraction of the propellant's aluminium that agglomerates.
        - Fig. 3: eta(p)   - oxide mass fraction inside the agglomerates.
        - Fig. 4: D43(p)   - mass-mean agglomerate diameter, um.  <-- digitised below
        - Notation list: "xi - доля поверхности «кармана», на которой находится каркасный
          слой" - i.e. Babuk's own symbol for what this repo calls the skeleton surface
          fraction f_s, and it is per POCKET surface, exactly as the solver uses it.

  [B] Бабук В. А., Бурачек Э. С. "Модель конкурирующих пламён применительно к пастообразным
      топливам" // Аэрокосмическая техника и технологии. 2025. Т. 3, № 4. С. 598-613.
      DOI 10.52467/2949-401X-2025-3-4-598-613.
        - Table (Результаты моделирования структуры исследуемых ПТ): pocket mass-mean size,
          pocket mass fraction, mean size of the particles that form the pocket.
        - "параметр Zam коррелируется с долей поверхности горящего топлива, на которой имеет
          место формирование КС" - the stated link between the measurement and the coverage.

  [C] Бабук В. А., Низяев А. А. "Моделирование структуры смесевых твердых топлив и проблема
      описания процесса агломерации" // Химическая физика и мезоскопия. 2014. Т. 16, № 1.
      С. 31-42.  - the structure model that produced [B]'s table, and the three agglomeration
      mechanisms: «карманный» (one agglomerate per pocket), «межкарманный» (one per several
      pockets), «докарманный» (several per pocket).

Run:  python3 analyze_babuk_agglomeration_data.py
"""

import json
import math
import os

REPO = os.path.dirname(os.path.abspath(__file__))
PROPELLANTS = os.path.join(REPO, "data", "propellants.01234.json")
EQUILIBRIUM = os.path.join(REPO, "data", "skeleton_carbon_equilibrium.json")
RUN_M = "/root/runs/6h-Classic-Tm2300-kc400-constrON-fsEquilibrium-20260803-0118/skeleton_layer.json"

# --- [B], table. Bas_0 is absent from it; it shares Bas_1's particle recipe (they differ only
# --- by the binder modifier), so the packing model must return the same numbers for it.
STRUCTURE = {
    "Bas_0": {"pocket_size_um": 104.67, "pocket_mass_fraction": 0.702, "wall_particle_um": 204, "assumed": True},
    "Bas_1": {"pocket_size_um": 104.67, "pocket_mass_fraction": 0.702, "wall_particle_um": 204, "assumed": False},
    "Bas_2": {"pocket_size_um": 104.67, "pocket_mass_fraction": 0.702, "wall_particle_um": 204, "assumed": False},
    "Bas_3": {"pocket_size_um": 91.87, "pocket_mass_fraction": 0.568, "wall_particle_um": 189, "assumed": False},
    "Bas_4": {"pocket_size_um": 143.19, "pocket_mass_fraction": 0.561, "wall_particle_um": 212, "assumed": False},
}

# --- [A], Fig. 4, digitised off the published trend lines at the two ends of the range.
# --- Reading accuracy is about +-3 um; the plotted confidence intervals are +-10..20 um,
# --- so the digitisation is well inside the experimental uncertainty.
D43_UM = {  # (at 1.0 MPa, at 6.5 MPa), linear in p as drawn
    "Bas_0": (139.0, 122.0),
    "Bas_1": (136.0, 117.0),
    "Bas_2": (120.0, 112.0),
    "Bas_3": (100.0, 88.0),
    "Bas_4": (166.0, 118.0),
}

# --- [A], Fig. 4, individual measured points (p MPa, D43 um), same digitisation.
D43_POINTS = {
    "Bas_0": [(1.25, 135.5), (2.30, 126.5), (4.20, 128.0), (6.30, 114.5)],
    "Bas_1": [(1.20, 140.5), (2.25, 135.5), (4.40, 124.0), (6.50, 124.5)],
    "Bas_2": [(1.20, 119.0), (2.30, 121.5), (4.30, 109.0), (6.30, 115.5)],
    "Bas_3": [(1.20, 101.5), (2.40, 98.5), (4.10, 94.0), (6.40, 89.0)],
    "Bas_4": [(1.30, 159.0), (2.40, 175.0), (4.10, 139.5), (6.30, 118.0)],
}

# --- [A], Fig. 2, individual measured points (p MPa, Z_m^a). Used only to prove that the
# --- polynomial shipped in propellants.01234.json IS this measurement.
ZMA_POINTS = {
    "Bas_0": [(1.05, 0.180), (2.20, 0.170), (4.10, 0.138), (6.05, 0.105)],
    "Bas_1": [(1.30, 0.238), (2.35, 0.185), (4.40, 0.135), (6.20, 0.117)],
    "Bas_2": [(1.00, 0.252), (2.30, 0.243), (4.30, 0.251), (6.40, 0.178)],
    "Bas_3": [(1.25, 0.175), (2.35, 0.160), (4.15, 0.130), (6.35, 0.088)],
    "Bas_4": [(1.30, 0.293), (2.40, 0.299), (4.15, 0.291), (6.30, 0.292)],
}

# --- [A], Fig. 3: oxide mass fraction inside the agglomerates, ~0.35 at 1 MPa, ~0.55 at 6 MPa,
# --- all five compositions within one another's confidence intervals.
ETA = (0.35, 0.55)  # at 1.0 and 6.0 MPa, linear in p

ALUMINIUM_DENSITY = 2700.0
ALUMINA_DENSITY = 3950.0
ALUMINA_PER_ALUMINIUM = 101.96 / (2 * 26.98)  # kg Al2O3 per kg Al consumed


def interp(pair, pressure_mpa, p_lo=1.0, p_hi=6.5):
    lo, hi = pair
    return lo + (hi - lo) * (pressure_mpa - p_lo) / (p_hi - p_lo)


def polynomial(coefficients, pressure_mpa):
    return sum(c * pressure_mpa ** i for i, c in enumerate(coefficients))


def load_propellants():
    with open(PROPELLANTS, encoding="utf-8") as handle:
        return {p["name"]: p for p in json.load(handle)}


def component(propellant, name):
    return propellant["components"][name]


def pocket_matrix(propellant):
    """Volume fractions inside the pocket: binder + aluminium + fine AP (coarse AP and HMX are walls)."""
    binder = component(propellant, "CombustibleBinder")
    aluminium = component(propellant, "Aluminum")
    perchlorate = component(propellant, "AmmoniumPerchlorate")
    fine = perchlorate["mass_fraction"] * (1.0 - perchlorate["large_particles_fraction"])

    volumes = {
        "binder": binder["mass_fraction"] / binder["density"],
        "aluminium": aluminium["mass_fraction"] / aluminium["density"],
        "fine_ap": fine / perchlorate["density"],
    }
    total = sum(volumes.values())
    mass = binder["mass_fraction"] + aluminium["mass_fraction"] + fine
    return {
        "aluminium_volume_fraction": volumes["aluminium"] / total,
        "aluminium_per_pocket_volume": aluminium["mass_fraction"] / (total / 1.0) * 0 + aluminium["mass_fraction"] / total,
        "density": mass / total,
        "aluminium_mass_fraction": aluminium["mass_fraction"] / mass,
    }


def main():
    propellants = load_propellants()
    with open(EQUILIBRIUM, encoding="utf-8") as handle:
        equilibrium = json.load(handle)
    equilibrium_by_name = {p["name"]: p for p in equilibrium["propellants"]}
    surface_temperatures = equilibrium["surfaceTemperaturesKelvins"]

    with open(RUN_M, encoding="utf-8") as handle:
        run_m = {p["name"]: p for p in json.load(handle)}

    porosity = 0.7477402967110957  # shipped per pressure frame, constant across the set
    solid = 1.0 - porosity

    print("=" * 108)
    print("A. IS THE SHIPPED POLYNOMIAL BABUK'S MEASURED Z_m^a(p)?  [A] Fig. 2 vs propellants.01234.json")
    print("=" * 108)
    print(f"{'':7}{'p, MPa':>8}{'measured':>11}{'polynomial':>12}{'diff':>9}")
    worst = 0.0
    for name in sorted(ZMA_POINTS):
        coefficients = component(propellants[name], "Aluminum")["agglomeration_coefficients"]
        for pressure, measured in ZMA_POINTS[name]:
            fitted = polynomial(coefficients, pressure)
            worst = max(worst, abs(fitted - measured))
            print(f"{name:7}{pressure:8.2f}{measured:11.3f}{fitted:12.4f}{fitted - measured:+9.4f}")
    print(f"\n  worst discrepancy over 20 digitised points: {worst:.4f} "
          f"(the figure's own confidence intervals are +-0.01..0.03)")

    print()
    print("=" * 108)
    print("B. THE MEASURED COVERAGE TARGET  f_s = Z_m^a / Z_pocket   vs the equilibrium-carbon closure")
    print("=" * 108)
    print(f"{'':7}{'p, MPa':>8}{'Z_m^a':>9}{'f_s meas':>10}{'f_s eq':>9}{'ratio':>8}   (f_s eq at converged T_s of run M)")
    for name in sorted(equilibrium_by_name):
        propellant = propellants[name]
        coefficients = propellant["pocket_surface_fraction_coefficients"]
        pocket_mass_fraction = propellant["pocket_mass_fraction"]
        frames_m = {round(f["pressure"]): f for f in run_m[name]["pressure_frames"]}
        for frame in equilibrium_by_name[name]["frames"]:
            pressure = frame["pressure"]
            pressure_mpa = pressure / 1e6
            measured = polynomial(coefficients, pressure_mpa) / pocket_mass_fraction
            frame_m = frames_m.get(round(pressure))
            if frame_m is None:
                continue
            surface = frame_m["surface_temperature_pocket"]
            coverages = frame["coverages"]
            computed = None
            for i in range(len(surface_temperatures) - 1):
                lo, hi = surface_temperatures[i], surface_temperatures[i + 1]
                if lo <= surface <= hi:
                    weight = (surface - lo) / (hi - lo)
                    computed = coverages[i] + weight * (coverages[i + 1] - coverages[i])
                    break
            if computed is None:
                computed = coverages[0] if surface < surface_temperatures[0] else coverages[-1]
            print(f"{name:7}{pressure_mpa:8.2f}{polynomial(coefficients, pressure_mpa):9.4f}"
                  f"{measured:10.4f}{computed:9.4f}{computed / measured:8.2f}")

    print()
    print("=" * 108)
    print("C. GEOMETRY: how many pockets does one MEASURED agglomerate take?  [A] Fig. 4 + [B] table")
    print("=" * 108)
    print(f"{'':7}{'p, MPa':>8}{'D43, um':>9}{'D_pocket':>10}{'eta':>7}{'D_1pocket':>11}{'N pockets':>11}  mechanism [C]")
    for name in sorted(STRUCTURE):
        propellant = propellants[name]
        matrix = pocket_matrix(propellant)
        structure = STRUCTURE[name]
        pocket_size = structure["pocket_size_um"] * 1e-6
        pocket_volume = math.pi * pocket_size ** 3 / 6.0
        aluminium_in_pocket = pocket_volume * matrix["density"] * matrix["aluminium_mass_fraction"]
        coefficients = component(propellant, "Aluminum")["agglomeration_coefficients"]

        for pressure_mpa in (1.0, 6.5):
            zma = polynomial(coefficients, pressure_mpa)
            eta = interp(ETA, pressure_mpa, 1.0, 6.0)
            # Density of an agglomerate that is (1-eta) metal, eta oxide, taken as compact.
            agglomerate_density = 1.0 / ((1.0 - eta) / ALUMINIUM_DENSITY + eta / ALUMINA_DENSITY)
            # kg of the propellant's ORIGINAL aluminium per kg of agglomerate
            aluminium_per_agglomerate = (1.0 - eta) + eta / ALUMINA_PER_ALUMINIUM

            d43 = interp(D43_UM[name], pressure_mpa) * 1e-6
            agglomerate_volume = math.pi * d43 ** 3 / 6.0
            aluminium_in_agglomerate = agglomerate_volume * agglomerate_density * aluminium_per_agglomerate

            # Diameter of the agglomerate a single pocket could feed, same density and oxide load
            single = (6.0 * (aluminium_in_pocket * zma / aluminium_per_agglomerate / agglomerate_density)
                      / math.pi) ** (1.0 / 3.0)
            pockets = aluminium_in_agglomerate / (aluminium_in_pocket * zma)
            mechanism = ("«карманный»" if pockets < 1.5 else
                         "«межкарманный»" if pockets > 1.5 else "?")
            print(f"{name:7}{pressure_mpa:8.2f}{d43 * 1e6:9.1f}{structure['pocket_size_um']:10.2f}{eta:7.2f}"
                  f"{single * 1e6:11.1f}{pockets:11.1f}  {mechanism}")

    print()
    print("=" * 108)
    print("D. THE THICKNESS THE GEOMETRY WANTS vs the thickness the optimiser fitted (run M)")
    print("=" * 108)
    print("   closure:  f_s * (N * A_pocket) * delta * (1-porosity) = agglomerate volume")
    print("   with f_s = Z_m^a/Z_p measured and N from block C, so delta is pure consequence.")
    print(f"{'':7}{'p, MPa':>8}{'delta geom':>12}{'delta run M':>13}{'ratio':>8}{'d_AP, um':>10}{'<= d_AP?':>10}")
    for name in sorted(STRUCTURE):
        propellant = propellants[name]
        matrix = pocket_matrix(propellant)
        structure = STRUCTURE[name]
        pocket_size = structure["pocket_size_um"] * 1e-6
        pocket_area = math.pi * pocket_size ** 2 / 4.0
        pocket_volume = math.pi * pocket_size ** 3 / 6.0
        aluminium_in_pocket = pocket_volume * matrix["density"] * matrix["aluminium_mass_fraction"]
        coefficients = component(propellant, "Aluminum")["agglomeration_coefficients"]
        large = component(propellant, "AmmoniumPerchlorate")["average_particles_diameter"] * 1e6
        frames_m = {round(f["pressure"] / 1e6, 2): f for f in run_m[name]["pressure_frames"]}

        for pressure_mpa in (1.0, 6.5):
            zma = polynomial(coefficients, pressure_mpa)
            coverage = zma / propellant["pocket_mass_fraction"]
            eta = interp(ETA, pressure_mpa, 1.0, 6.0)
            agglomerate_density = 1.0 / ((1.0 - eta) / ALUMINIUM_DENSITY + eta / ALUMINA_DENSITY)
            aluminium_per_agglomerate = (1.0 - eta) + eta / ALUMINA_PER_ALUMINIUM
            d43 = interp(D43_UM[name], pressure_mpa) * 1e-6
            agglomerate_volume = math.pi * d43 ** 3 / 6.0
            aluminium_in_agglomerate = agglomerate_volume * agglomerate_density * aluminium_per_agglomerate
            pockets = aluminium_in_agglomerate / (aluminium_in_pocket * zma)

            delta = agglomerate_volume / (coverage * pockets * pocket_area * solid)
            key = min(frames_m, key=lambda k: abs(k - pressure_mpa))
            fitted = frames_m[key]["skeleton_layer"]["thickness"]
            print(f"{name:7}{pressure_mpa:8.2f}{delta * 1e6:12.1f}{fitted:13.2f}{delta * 1e6 / fitted:8.1f}"
                  f"{large:10.0f}{'yes' if delta * 1e6 <= large else 'NO':>10}")


if __name__ == "__main__":
    main()
