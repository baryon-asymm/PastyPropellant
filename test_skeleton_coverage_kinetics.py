"""Screen candidate forms for a *kinetic* skeleton-coverage closure against Babuk's measurement.

Hypothesis under test: the pocket surface carries a skeleton on the fraction of its area where
accumulation of condensed residue outruns burnout of the carbonaceous binder residue that holds it:

    f_s = 1 / (1 + K),    K = k_destroy / k_grow

    k_grow    = r * phi_s / (delta * (1 - Pi))          [layer built in one pass]
    k_destroy = wdot_C / (rho_C * phi_C * delta * (1 - Pi))

so delta and the porosity cancel and

    K = wdot_C / (rho_C * phi_C * phi_s * r).

The only open question is wdot_C, the carbon burnout rate per unit skeleton area.  Candidates:

    (i)   wdot_C ~ p   * exp(-E/RT_s)   heterogeneous C + oxidiser, first order in pressure
    (ii)  wdot_C ~ r   * exp(-E/RT_s)   burnout limited by the gas flux leaving the surface
    (iii) wdot_C ~ p*X_ox * exp(-E/RT_s) as (i) but weighted by the pocket's own oxidiser loading

For each candidate, K_measured (from Babuk's Z_m^a / Z_p) is inverted for the Arrhenius group and
regressed on 1/T_s.  A candidate is only interesting if the activation energies agree ACROSS the five
compositions - otherwise E is just absorbing whatever the form gets wrong, i.e. a fit in disguise.

Inputs: measured Z_m^a (propellants.01234.json = Babuk's curves, proven in
analyze_babuk_agglomeration_data.py), phi_C / phi_s from data/skeleton_carbon_equilibrium.json,
and r, T_s from run M.  No number here is tuned.
"""

import json
import math
import os

REPO = os.path.dirname(os.path.abspath(__file__))
RUN_M = "/root/runs/6h-Classic-Tm2300-kc400-constrON-fsEquilibrium-20260803-0118/skeleton_layer.json"
GAS_CONSTANT = 8.314462618


def load(path):
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def polynomial(coefficients, pressure_mpa):
    return sum(c * pressure_mpa ** i for i, c in enumerate(coefficients))


def oxidiser_loading(propellant):
    """Mole-of-available-oxygen weighting of the pocket gas: the binder is itself a perchlorate,
    and the fine AP fraction sits inside the pocket while the coarse fraction walls it in."""
    components = propellant["components"]
    binder = components["CombustibleBinder"]["mass_fraction"]
    aluminium = components["Aluminum"]["mass_fraction"]
    perchlorate = components["AmmoniumPerchlorate"]
    fine = perchlorate["mass_fraction"] * (1.0 - perchlorate["large_particles_fraction"])
    # AP is 4 O per 117.49 g/mol; XPEPA-22E is a polyethylenepolyamine perchlorate - take its
    # perchlorate content as one ClO4 per repeat unit, i.e. the same 4 O per 99.45 g/mol of ClO4.
    oxygen = fine * 4.0 / 117.49 + binder * 4.0 / 99.45 * 0.5
    return oxygen / (binder + aluminium + fine)


def regress(xs, ys):
    n = len(xs)
    mean_x = sum(xs) / n
    mean_y = sum(ys) / n
    sxx = sum((x - mean_x) ** 2 for x in xs)
    sxy = sum((x - mean_x) * (y - mean_y) for x, y in zip(xs, ys))
    slope = sxy / sxx
    intercept = mean_y - slope * mean_x
    residuals = [y - (intercept + slope * x) for x, y in zip(xs, ys)]
    syy = sum((y - mean_y) ** 2 for y in ys)
    sse = sum(r ** 2 for r in residuals)
    return slope, intercept, 1.0 - sse / syy if syy > 0 else float("nan")


def main():
    propellants = {p["name"]: p for p in load(os.path.join(REPO, "data", "propellants.01234.json"))}
    equilibrium = load(os.path.join(REPO, "data", "skeleton_carbon_equilibrium.json"))
    temperatures = equilibrium["surfaceTemperaturesKelvins"]
    coverage_table = {p["name"]: p for p in equilibrium["propellants"]}
    run_m = {p["name"]: p for p in load(RUN_M)}

    rows = []
    for name in sorted(propellants):
        propellant = propellants[name]
        table = coverage_table[name]
        aluminium_fraction = table["aluminiumVolumeFraction"]
        coefficients = propellant["pocket_surface_fraction_coefficients"]
        pocket_mass_fraction = propellant["pocket_mass_fraction"]
        x_ox = oxidiser_loading(propellant)
        frames = {round(f["pressure"]): f for f in table["frames"]}

        for frame_m in run_m[name]["pressure_frames"]:
            pressure = frame_m["pressure"]
            pressure_mpa = pressure / 1e6
            surface = frame_m["surface_temperature_pocket"]
            rate = frame_m["experimental_burn_rate"] * 1e-3  # mm/s -> m/s

            coverages = frames[round(pressure)]["coverages"]
            phi_s = None
            for i in range(len(temperatures) - 1):
                lo, hi = temperatures[i], temperatures[i + 1]
                if lo <= surface <= hi:
                    weight = (surface - lo) / (hi - lo)
                    phi_s = coverages[i] + weight * (coverages[i + 1] - coverages[i])
                    break
            if phi_s is None:
                phi_s = coverages[0] if surface < temperatures[0] else coverages[-1]
            phi_c = phi_s - aluminium_fraction

            measured = polynomial(coefficients, pressure_mpa) / pocket_mass_fraction
            k_measured = (1.0 - measured) / measured

            rows.append({
                "name": name, "p": pressure_mpa, "T_s": surface, "r": rate,
                "phi_s": phi_s, "phi_c": phi_c, "x_ox": x_ox,
                "f_s": measured, "K": k_measured,
            })

    print("=" * 104)
    print("MEASURED COVERAGE AND THE IMPLIED K = (1-f_s)/f_s")
    print("=" * 104)
    print(f"{'':7}{'p':>6}{'f_s':>8}{'K':>8}{'r, mm/s':>9}{'T_s, K':>8}{'phi_s':>8}{'phi_C':>8}{'X_ox':>8}")
    for row in rows:
        if row["p"] < 1.05 or row["p"] > 6.4:
            print(f"{row['name']:7}{row['p']:6.2f}{row['f_s']:8.4f}{row['K']:8.3f}"
                  f"{row['r'] * 1e3:9.2f}{row['T_s']:8.1f}{row['phi_s']:8.4f}{row['phi_c']:8.4f}{row['x_ox']:8.5f}")

    candidates = {
        "(i)   wdot_C ~ p * exp(-E/RT_s)": lambda row: row["p"] * 1e6,
        "(ii)  wdot_C ~ r * exp(-E/RT_s)": lambda row: row["r"],
        "(iii) wdot_C ~ p*X_ox * exp(-E/RT_s)": lambda row: row["p"] * 1e6 * row["x_ox"],
    }

    for label, driver in candidates.items():
        print()
        print("=" * 104)
        print(f"CANDIDATE {label}")
        print("=" * 104)
        print("   K * rho_C * phi_C * phi_s * r / driver  =  A * exp(-E/RT_s)   ->  regress ln(...) on 1/T_s")
        print(f"{'':7}{'E, kJ/mol':>11}{'ln A':>10}{'R^2':>8}{'K span 1->6.5 MPa':>20}")
        energies = []
        for name in sorted(propellants):
            subset = [row for row in rows if row["name"] == name]
            xs, ys = [], []
            for row in subset:
                group = row["K"] * row["phi_c"] * row["phi_s"] * row["r"] / driver(row)
                xs.append(1.0 / row["T_s"])
                ys.append(math.log(group))
            slope, intercept, r2 = regress(xs, ys)
            energy = -slope * GAS_CONSTANT / 1000.0
            energies.append(energy)
            print(f"{name:7}{energy:11.1f}{intercept:10.2f}{r2:8.3f}"
                  f"{subset[0]['K']:10.2f} ->{subset[-1]['K']:8.2f}")
        spread = max(energies) - min(energies)
        print(f"\n   activation energies span {spread:.0f} kJ/mol across the five compositions "
              f"(min {min(energies):.0f}, max {max(energies):.0f})")
        print("   -> a physical closure needs ONE E; a spread of this size means E is absorbing the "
              "form's error.")

    print()
    print("=" * 104)
    print("CONTROL: what pressure exponent does K actually have, and how does it compare with 1 - nu?")
    print("=" * 104)
    print(f"{'':7}{'d lnK/d lnp':>13}{'nu (exp)':>10}{'1 - nu':>9}{'nu + dlnK/dlnp':>16}")
    for name in sorted(propellants):
        subset = [row for row in rows if row["name"] == name]
        xs = [math.log(row["p"]) for row in subset]
        ys = [math.log(row["K"]) for row in subset]
        slope, _, _ = regress(xs, ys)
        nu = propellants[name]["nu"]
        print(f"{name:7}{slope:13.3f}{nu:10.2f}{1.0 - nu:9.2f}{nu + slope:16.3f}")
    print("\n   The last column is the pressure order the burnout rate itself must have "
          "(K ~ wdot_C / r, r ~ p^nu).")


if __name__ == "__main__":
    main()
