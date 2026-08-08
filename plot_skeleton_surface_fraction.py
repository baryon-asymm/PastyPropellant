"""Plot the skeleton surface fraction f_s a run ACTUALLY used, against the polynomial.

WHY THIS EXISTS
  The PDF report's `agglomeration_fraction_plot.png` and `skeleton_surface_fraction`
  panels are drawn by `src/python/PropellantsPlotRendering` straight from the INPUT
  propellants file, i.e. from the fitted polynomial. Under
  `model.skeletonSurfaceFraction.mode = EquilibriumCarbon` that is no longer what the
  solver used: f_s is then computed from equilibrium at the pore temperature, which
  follows the surface temperature the solver converged to. So the report plots one
  quantity while the run minimised another. This script draws the one the run used.

PANELS - one per series, every fuel a curve inside it
  (a) f_s from equilibrium, evaluated at each fuel's own converged surface temperature
  (b) f_s from the shipped polynomial (what the report draws)
  (c) their ratio, with unity marked
  (d) the converged surface temperature, which is what drives (a), with the solver's
      600..900 K search bracket marked - a fuel sitting on a bound is not a solution
      the closure chose, it is one the bracket imposed

USAGE
  python plot_skeleton_surface_fraction.py <run-directory> [--output-dir DIR]

  The run directory is the one the console host wrote: it must hold the PDF report and
  the propellants file. The equilibrium table is read from `data/` unless the run
  directory carries its own copy, which it normally does.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

ROOT = os.path.dirname(os.path.abspath(__file__))
SEARCH_BRACKET_KELVINS = (600.0, 900.0)   # SurfaceTemperatureSearchBounds


def report_text(run_directory: str) -> str:
    """Plain text of the run's PDF report."""
    pdfs = [f for f in os.listdir(run_directory) if f.endswith(".pdf")]
    if not pdfs:
        raise SystemExit(f"No PDF report in {run_directory}.")
    if shutil.which("pdftotext") is None:
        raise SystemExit("pdftotext is required to read the report; install poppler-utils.")

    with tempfile.TemporaryDirectory() as tmp:
        out = os.path.join(tmp, "report.txt")
        subprocess.run(["pdftotext", "-layout", os.path.join(run_directory, pdfs[0]), out],
                       check=True)
        return open(out).read()


def converged_surface_temperatures(text: str) -> dict[str, dict[float, float]]:
    """{fuel: {pressure_MPa: surface temperature}} from the per-pressure detail blocks.

    The POCKET surface temperature is the one that matters: f_s describes the pocket
    surface, and the report calls that region the "Secondary Conditional Fuel".
    """
    result: dict[str, dict[float, float]] = {}
    parts = re.split(r'\nPropellant Composition "(\w+)"\n', text)
    for i in range(1, len(parts), 2):
        name, body = parts[i], parts[i + 1]
        frames: dict[float, float] = {}
        for chunk in re.split(r"\nAt pressure ", body)[1:]:
            header = re.match(r"([\d.]+) MPa", chunk)
            if not header or "Secondary Conditional Fuel" not in chunk:
                continue
            pocket = chunk.split("Secondary Conditional Fuel")[1]
            temperature = re.search(r"Surface temperature ([\d.]+) K", pocket)
            if temperature:
                frames[float(header.group(1))] = float(temperature.group(1))
        if frames:
            result[name] = frames
    return result


def polynomial_coverage(propellant: dict, pressure_megapascals: float) -> float:
    """The shipped closure: sum(c_i p^i) / omega_pocket, p in MPa."""
    coefficients = propellant["pocket_surface_fraction_coefficients"]
    return sum(c * pressure_megapascals ** i for i, c in enumerate(coefficients)) \
        / propellant["pocket_mass_fraction"]


class EquilibriumTable:
    """Coverage curves from `skeleton_carbon_equilibrium.json`, interpolated in T_s."""

    def __init__(self, path: str):
        table = json.load(open(path))
        self.temperatures = table["surfaceTemperaturesKelvins"]
        self.curves = {
            entry["name"]: {round(frame["pressure"]): frame["coverages"]
                            for frame in entry["frames"]}
            for entry in table["propellants"]
        }
        self.source = path

    def at(self, name: str, pressure_pascals: float, surface_temperature: float) -> float:
        # The report's detail blocks round the pressure to two decimals ("At pressure 1.61
        # MPa" for the 1.611 MPa frame), so match the nearest frame rather than the exact
        # key. The frames are 0.6 MPa apart, far wider than that rounding.
        frames = self.curves[name]
        key = min(frames, key=lambda stored: abs(stored - pressure_pascals))
        if abs(key - pressure_pascals) > 5e4:
            raise SystemExit(
                f"{name}: the coverage table has no frame near {pressure_pascals / 1e6:.3f} MPa; "
                "regenerate it from the propellants file this run used.")
        coverages = frames[key]
        temperatures = self.temperatures
        if surface_temperature <= temperatures[0]:
            return coverages[0]
        for i in range(1, len(temperatures)):
            if surface_temperature <= temperatures[i]:
                weight = ((surface_temperature - temperatures[i - 1])
                          / (temperatures[i] - temperatures[i - 1]))
                return coverages[i - 1] + weight * (coverages[i] - coverages[i - 1])
        return coverages[-1]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("run_directory")
    parser.add_argument("--output-dir", default=os.getcwd())
    parser.add_argument("--propellants", default=None,
                        help="Defaults to the propellants file inside the run directory.")
    parser.add_argument("--table", default=None,
                        help="Defaults to the run directory's copy, then data/.")
    arguments = parser.parse_args()

    run = arguments.run_directory
    propellants_path = arguments.propellants or os.path.join(run, "propellants.01234.json")
    table_path = arguments.table
    if table_path is None:
        local = os.path.join(run, "skeleton_carbon_equilibrium.json")
        table_path = local if os.path.exists(local) \
            else os.path.join(ROOT, "data/skeleton_carbon_equilibrium.json")

    propellants = {p["name"]: p for p in json.load(open(propellants_path))}
    table = EquilibriumTable(table_path)
    text = report_text(run)
    surface_temperatures = converged_surface_temperatures(text)

    objective = re.search(r"Objective \(aggregated fitness\): ([\d.E+-]+)", text)
    penalty = re.search(r"Penalty \(total aggregated\): ([\d.E+-]+)", text)

    names = [n for n in propellants if n in surface_temperatures]
    if not names:
        raise SystemExit("The report carries no per-pressure detail for any propellant.")

    series: dict[str, dict[str, list]] = {}
    for name in names:
        pressures = sorted(surface_temperatures[name])
        equilibrium, polynomial, temperatures = [], [], []
        for pressure in pressures:
            surface_temperature = surface_temperatures[name][pressure]
            temperatures.append(surface_temperature)
            equilibrium.append(table.at(name, pressure * 1e6, surface_temperature))
            polynomial.append(polynomial_coverage(propellants[name], pressure))
        series[name] = {
            "p": pressures,
            "equilibrium": equilibrium,
            "polynomial": polynomial,
            "ratio": [e / q for e, q in zip(equilibrium, polynomial)],
            "surface": temperatures,
        }

    panels = [
        ("equilibrium", "(a) f_s used by the run — equilibrium carbon at the converged T_s",
         "Skeleton surface fraction f_s", None),
        ("polynomial", "(b) f_s from the shipped polynomial — what the report plots",
         "Skeleton surface fraction f_s", None),
        ("ratio", "(c) ratio — equilibrium / polynomial", "f_s(equilibrium) / f_s(polynomial)", 1.0),
        ("surface", "(d) converged pocket surface temperature — the driver of (a)",
         "Surface temperature T_s, K", None),
    ]

    figure, axes = plt.subplots(2, 2, figsize=(16, 12), squeeze=False)
    heading = "Skeleton surface fraction: computed vs the polynomial it replaced"
    if objective and penalty:
        heading += f"\nrun objective {objective.group(1)}, penalty {penalty.group(1)}"
    figure.suptitle(heading, fontsize=16)

    for axis, (key, title, ylabel, reference) in zip(axes.flatten(), panels):
        for name in names:
            axis.plot(series[name]["p"], series[name][key], marker="o", label=name)
        if reference is not None:
            axis.axhline(reference, color="black", linewidth=1, linestyle="--")
        if key == "surface":
            for bound in SEARCH_BRACKET_KELVINS:
                axis.axhline(bound, color="red", linewidth=1, linestyle=":")
            axis.text(0.99, 0.02, "red: solver search bracket", transform=axis.transAxes,
                      ha="right", va="bottom", fontsize=9, color="red")
        axis.set_title(title, fontsize=12)
        axis.set_xlabel("Pressure, MPa")
        axis.set_ylabel(ylabel)
        axis.grid(True, alpha=0.3)
        axis.legend()

    figure.tight_layout(rect=(0, 0, 1, 0.95))
    os.makedirs(arguments.output_dir, exist_ok=True)
    target = os.path.join(arguments.output_dir, "skeleton_surface_fraction_computed_plot.png")
    figure.savefig(target, dpi=140)
    print(f"wrote {target}")

    print(f"\n{'fuel':8}{'p, MPa':>8}{'T_s, K':>9}{'f_s equil':>11}{'f_s poly':>10}{'ratio':>8}")
    for name in names:
        s = series[name]
        for i in (0, len(s["p"]) - 1):
            print(f"{name:8}{s['p'][i]:8.2f}{s['surface'][i]:9.1f}"
                  f"{s['equilibrium'][i]:11.4f}{s['polynomial'][i]:10.4f}{s['ratio'][i]:8.2f}")


if __name__ == "__main__":
    main()
