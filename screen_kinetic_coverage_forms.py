"""Pick the functional form of the kinetic skeleton-coverage closure before writing any C#.

Structure fixed by the previous screening (test_skeleton_coverage_kinetics.py):

    f_s = 1 / (1 + K),   K = wdot_C / (rho_C * phi_C * phi_s * r)

with a two-channel burnout: one channel carried by the binder alone (pressure-independent) and one
carried by the fine AP that sits inside the pocket.  What is still open is how the two channels scale
with the condensed inventory the equilibrium table already computes, so the candidates here differ only
in the normaliser N(phi_C, phi_s) and in whether the burn rate survives:

    K = [a0 + a1 * w_fine * p^m] / N

Fitting set: **Bas_2, Bas_3, Bas_4 only** - one binder, the same 0.5 % activated carbon, differing
only in AP dispersity, so every input is published.  Bas_0 and Bas_1 are held out: Bas_1 carries an
unquantified ferrocene compound and Bas_0 the unmodified binder, so a form that "explains" them is
explaining something it has no data for.

Target: f_s = Z_m^a(p) / Z_pocket, i.e. Babuk's measurement (see analyze_babuk_agglomeration_data.py).
"""

import json
import math
import os

REPO = os.path.dirname(os.path.abspath(__file__))
RUN_M = "/root/runs/6h-Classic-Tm2300-kc400-constrON-fsEquilibrium-20260803-0118/skeleton_layer.json"
FAMILY = ("Bas_2", "Bas_3", "Bas_4")
HELD_OUT = ("Bas_0", "Bas_1")


def load(path):
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def polynomial(coefficients, pressure_mpa):
    return sum(c * pressure_mpa ** i for i, c in enumerate(coefficients))


def build_rows():
    propellants = {p["name"]: p for p in load(os.path.join(REPO, "data", "propellants.01234.json"))}
    equilibrium = load(os.path.join(REPO, "data", "skeleton_carbon_equilibrium.json"))
    temperatures = equilibrium["surfaceTemperaturesKelvins"]
    table = {p["name"]: p for p in equilibrium["propellants"]}
    run_m = {p["name"]: p for p in load(RUN_M)}

    rows = []
    for name, propellant in propellants.items():
        perchlorate = propellant["components"]["AmmoniumPerchlorate"]
        fine = perchlorate["mass_fraction"] * (1.0 - perchlorate["large_particles_fraction"])
        aluminium_fraction = table[name]["aluminiumVolumeFraction"]
        frames = {round(f["pressure"]): f for f in table[name]["frames"]}

        for frame_m in run_m[name]["pressure_frames"]:
            pressure = frame_m["pressure"]
            pressure_mpa = pressure / 1e6
            surface = frame_m["surface_temperature_pocket"]
            coverages = frames[round(pressure)]["coverages"]

            phi_s = coverages[-1]
            for i in range(len(temperatures) - 1):
                lo, hi = temperatures[i], temperatures[i + 1]
                if lo <= surface <= hi:
                    phi_s = coverages[i] + (surface - lo) / (hi - lo) * (coverages[i + 1] - coverages[i])
                    break
            else:
                if surface < temperatures[0]:
                    phi_s = coverages[0]

            measured = polynomial(propellant["pocket_surface_fraction_coefficients"], pressure_mpa) \
                / propellant["pocket_mass_fraction"]
            rows.append({
                "name": name,
                "p": pressure_mpa,
                "w_fine": fine,
                "phi_s": phi_s,
                "phi_c": phi_s - aluminium_fraction,
                "r": frame_m["experimental_burn_rate"] * 1e-3,
                "f_s": measured,
                "K": (1.0 - measured) / measured,
            })
    return rows


NORMALISERS = {
    "1                     ": lambda row: 1.0,
    "phi_s                 ": lambda row: row["phi_s"],
    "phi_C                 ": lambda row: row["phi_c"],
    "phi_C * phi_s         ": lambda row: row["phi_c"] * row["phi_s"],
    "phi_C^0.5             ": lambda row: math.sqrt(row["phi_c"]),
    "phi_C * phi_s * r/r0  ": lambda row: row["phi_c"] * row["phi_s"] * row["r"] / 0.02,
    "phi_s * r/r0          ": lambda row: row["phi_s"] * row["r"] / 0.02,
}


def fit(rows, normaliser, exponent):
    """Least squares in (a0, a1) for K * N = a0 + a1 * w_fine * p^m."""
    s11 = s12 = s22 = t1 = t2 = 0.0
    for row in rows:
        basis = (1.0, row["w_fine"] * row["p"] ** exponent)
        target = row["K"] * normaliser(row)
        s11 += basis[0] ** 2
        s12 += basis[0] * basis[1]
        s22 += basis[1] ** 2
        t1 += basis[0] * target
        t2 += basis[1] * target
    determinant = s11 * s22 - s12 * s12
    a0 = (t1 * s22 - t2 * s12) / determinant
    a1 = (s11 * t2 - s12 * t1) / determinant
    return a0, a1


def score(rows, normaliser, exponent, a0, a1):
    """Scored on f_s itself, not on K - K blows up where coverage is small and would flatter the fit."""
    worst = 0.0
    total = 0.0
    for row in rows:
        predicted_k = (a0 + a1 * row["w_fine"] * row["p"] ** exponent) / normaliser(row)
        if predicted_k <= 0:
            return float("inf"), float("inf")
        predicted = 1.0 / (1.0 + predicted_k)
        relative = (predicted - row["f_s"]) / row["f_s"]
        worst = max(worst, abs(relative))
        total += relative ** 2
    return math.sqrt(total / len(rows)), worst


def main():
    rows = build_rows()
    family = [row for row in rows if row["name"] in FAMILY]

    print("=" * 100)
    print("FORM SCREENING:  K = [a0 + a1 * w_fine * p^m] / N     fitted on Bas_2/3/4 (30 points)")
    print("=" * 100)
    print(f"{'normaliser N':22}{'m':>6}{'a0':>10}{'a1':>10}{'RMS rel':>10}{'worst':>9}   held-out Bas_0 / Bas_1 worst")
    results = []
    for label, normaliser in NORMALISERS.items():
        best = None
        for step in range(20, 261):
            exponent = step / 200.0
            a0, a1 = fit(family, normaliser, exponent)
            rms, worst = score(family, normaliser, exponent, a0, a1)
            if best is None or rms < best[0]:
                best = (rms, worst, exponent, a0, a1)
        rms, worst, exponent, a0, a1 = best
        held = []
        for name in HELD_OUT:
            subset = [row for row in rows if row["name"] == name]
            _, held_worst = score(subset, normaliser, exponent, a0, a1)
            held.append(held_worst)
        results.append((rms, label, exponent, a0, a1, worst, held))
        print(f"{label:22}{exponent:6.2f}{a0:10.4f}{a1:10.4f}{rms * 100:9.1f}%{worst * 100:8.1f}%"
              f"   {held[0] * 100:6.0f}% / {held[1] * 100:.0f}%")

    results.sort()
    rms, label, exponent, a0, a1, worst, held = results[0]
    normaliser = NORMALISERS[label]
    print(f"\nBest: N = {label.strip()},  m = {exponent:.2f},  a0 = {a0:.4f},  a1 = {a1:.4f}")
    print("=" * 100)
    print("PER-POINT, BEST FORM")
    print("=" * 100)
    print(f"{'':7}{'p, MPa':>8}{'f_s meas':>10}{'f_s calc':>10}{'rel':>8}{'phi_C':>8}{'phi_s':>8}")
    for name in FAMILY + HELD_OUT:
        for row in [r for r in rows if r["name"] == name]:
            predicted_k = (a0 + a1 * row["w_fine"] * row["p"] ** exponent) / normaliser(row)
            predicted = 1.0 / (1.0 + predicted_k)
            flag = "" if name in FAMILY else "  held out"
            print(f"{name:7}{row['p']:8.2f}{row['f_s']:10.4f}{predicted:10.4f}"
                  f"{100 * (predicted - row['f_s']) / row['f_s']:+7.1f}%{row['phi_c']:8.4f}{row['phi_s']:8.4f}{flag}")

    print()
    print("Reference: the equilibrium (Delesse-carbon) closure alone, same points, no free constants")
    print(f"{'':7}{'RMS rel':>10}{'worst':>9}")
    for name in FAMILY + HELD_OUT:
        subset = [row for row in rows if row["name"] == name]
        total = max_rel = 0.0
        for row in subset:
            relative = (row["phi_s"] - row["f_s"]) / row["f_s"]
            total += relative ** 2
            max_rel = max(max_rel, abs(relative))
        print(f"{name:7}{100 * math.sqrt(total / len(subset)):9.1f}%{100 * max_rel:8.1f}%")


if __name__ == "__main__":
    main()
