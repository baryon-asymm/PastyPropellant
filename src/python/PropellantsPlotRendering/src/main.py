"""
Render per-characteristic thermodynamic plots for a set of propellants.

Reads the INPUT propellants JSON (the same file the .NET host feeds the solver) and
writes one PNG per characteristic, with every fuel drawn as a curve inside each panel.

Usage:
    python main.py <path/to/propellants.json> [--output-dir DIR]

`--output-dir` defaults to the current working directory, which is the historical
behaviour the .NET `PythonPlotsRenderer` relies on: it passes only the JSON path and
then reads the PNGs back from `Directory.GetCurrentDirectory()`.
"""

import argparse
import json
import os
import sys

import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

from typing import List

PLOT_PARAMETERS = [
    'lambda_gas',
    'average_molar_mass',
    'c_volume',
    'temperatures',
    'agglomeration_fraction',
    'skeleton_surface_fraction',
]

PARAMETER_LABELS = {
    'lambda_gas': 'Thermal Conductivity (λ), W/(m·K)',
    'average_molar_mass': 'Average Molar Mass, kg/mol',
    'c_volume': 'Constant Volume Heat Capacity (cᵥ), J/(kg·K)',
    'temperatures': 'Temperature of the Flames, K',
    'agglomeration_fraction': 'Aluminum Agglomeration Fraction',
    'skeleton_surface_fraction': 'Skeleton Surface Fraction (Agglomeration/Pocket Mass)'
}

# For the per-phase thermodynamic fields (lambda_gas / average_molar_mass / c_volume)
# each characteristic is shown as one sub-plot PER GAS PHASE, and every fuel is a curve
# inside that sub-plot. The phase's value is read at path(frame)[parameter_name]. The
# panel count is fixed by this list, so the number of fuels only changes the number of
# lines per panel (the layout no longer needs to know the fuel count in advance).
GAS_PHASE_PATHS = [
    ('Inter-Pocket Gas Phase',       lambda frame: frame['inter_pocket_gas_phase']),
    ('Pocket (Diffusion) Gas Phase', lambda frame: frame['pocket_gas_phase']),
    ('Skeleton Gas Phase',           lambda frame: frame['pocket_gas_phase']['skeleton_gas_phase']),
    ('Outer Skeleton Phase',         lambda frame: frame['pocket_gas_phase']['out_skeleton_gas_phase']),
]

# Flame temperatures live at different keys per phase (kinetic-flame temps plus the
# pocket diffusion-flame temp), so they get their own (phase title, accessor) list.
TEMPERATURE_PHASES = [
    ('Inter-Pocket Gas Phase',   lambda frame: frame['inter_pocket_gas_phase'].get('T_kinetic_flame')),
    ('Skeleton Gas Phase',       lambda frame: frame['pocket_gas_phase']['skeleton_gas_phase'].get('T_kinetic_flame')),
    ('Outer Skeleton Phase',     lambda frame: frame['pocket_gas_phase']['out_skeleton_gas_phase'].get('T_kinetic_flame')),
    ('Diffusion Flame (Pocket)', lambda frame: frame['pocket_gas_phase'].get('T_diffusion_flame')),
]


def read_json(file_path):
    with open(file_path, 'r') as file:
        return json.load(file)

def _calculate_agglomeration_fraction(coefficients: List[float], pressure: float) -> float:
    """
    Calculate agglomeration fraction using polynomial coefficients.

    Same polynomial as RegionMapper's `_calculate_agglomeration_fraction`
    (evaluated in MPa). The quantity is a mass FRACTION in [0, 1], not a
    percentage: the shipped propellants fit to 0.086..0.296 over the 1..6.5 MPa
    fitted range.

    The `min(100, ...)` upper bound is therefore on the wrong scale and is inert
    — nothing approaches 100 — and it is kept only so this plot's numbers stay
    bit-identical to what has been published. The `max(0, ...)` lower bound is
    the meaningful one: it keeps the plotted curve off a negative axis if the
    fit is ever extrapolated past the pressure range it was fitted on.

    Over the fitted range this returns exactly the same values as RegionMapper's
    unclamped version for every shipped propellant, because neither bound binds.
    """
    normalized_pressure = pressure / 1e6
    fraction = sum(coeff * (normalized_pressure)**i for i, coeff in enumerate(coefficients))
    return max(0, min(100, fraction))


def _draw_confidence_intervals(ax, fuel, color, divisor=1.0):
    """Overlay the measured Z_a^m points and their error bars on an agglomeration panel.

    The intervals are optional: a fuel that carries none is simply drawn as the bare
    polynomial, which is what every propellants file did before the measurements were
    digitised from Babuk's figure.

    Two unit conversions are load-bearing and easy to get wrong:

    * `x_value` is a pressure in MPa -- the same convention as the burn-rate
      `confidence_intervals` -- while this plot's x axis is in Pa.
    * `size_of_confidence_interval` is the FULL height of the bar (matching the .NET
      `ConfidenceIntervalDrawer`, which draws `y ± size/2`), so matplotlib's `yerr`,
      which is a half-height, gets half of it.

    `divisor` converts Z_a^m into the skeleton surface fraction f_s = Z_a^m / Z_p; it
    scales the interval as well as the point, since f_s is a plain rescaling.
    """
    try:
        intervals = fuel['components']['Aluminum'].get('agglomeration_confidence_intervals')
    except KeyError:
        return

    if not intervals or not divisor:
        return

    pressures = [ci['x_value'] * 1e6 for ci in intervals]
    values = [ci['y_value'] / divisor for ci in intervals]
    half_heights = [ci['size_of_confidence_interval'] / 2 / divisor for ci in intervals]

    ax.errorbar(pressures, values,
                yerr=half_heights,
                fmt='o',
                markersize=5,
                markerfacecolor='none',
                color=color,
                ecolor=color,
                elinewidth=1.5,
                capsize=5,
                capthick=1.5,
                linestyle='none',
                zorder=5)


def _gas_phase_phases(parameter_name):
    """(phase title, frame -> value) pairs for a per-phase thermodynamic field."""
    def value_fn(path):
        return lambda frame: path(frame).get(parameter_name)
    return [(label, value_fn(path)) for label, path in GAS_PHASE_PATHS]


def _plot_phase_grid(data, phases, suptitle, ylabel, output_filename):
    """Render one sub-plot per phase (a fixed set), each overlaying every fuel as a
    curve. The number of fuels only changes the number of lines per panel, so the
    layout is independent of how many fuels a run has."""
    num_panels = len(phases)
    rows = (num_panels + 1) // 2
    fig, axs = plt.subplots(rows, 2, figsize=(16, 6 * rows), squeeze=False)
    fig.suptitle(suptitle, fontsize=16)
    axs = axs.flatten()

    for i, (phase_label, value_fn) in enumerate(phases):
        ax = axs[i]
        for fuel in data:
            pressures = []
            values = []
            for frame in fuel['pressure_frames']:
                value = value_fn(frame)
                if value is not None:
                    pressures.append(frame['pressure'])
                    values.append(value)
            if values:
                ax.plot(pressures, values, label=fuel['name'], marker='o',
                        markersize=6, linewidth=2)

        ax.grid(which='major', linestyle='-', linewidth=1.0, color='#666666')
        ax.grid(which='minor', linestyle=':', linewidth=0.8, color='#666666')
        ax.minorticks_on()
        ax.xaxis.set_minor_locator(ticker.AutoMinorLocator(5))
        ax.yaxis.set_minor_locator(ticker.AutoMinorLocator(5))
        ax.set_title(phase_label, fontsize=14)
        ax.set_xlabel('Pressure, Pa', fontsize=12)
        ax.set_ylabel(ylabel, fontsize=12)
        ax.legend(fontsize=10)

    for j in range(num_panels, len(axs)):
        axs[j].axis('off')

    plt.tight_layout(rect=[0, 0.03, 1, 0.95])
    plt.savefig(output_filename, dpi=300, bbox_inches='tight')
    plt.close()
    print(f"Saved plot to: {output_filename}")


def plot_parameter(data, parameter_name, output_filename):
    if parameter_name in ['agglomeration_fraction', 'skeleton_surface_fraction']:
        fig, ax = plt.subplots(figsize=(16, 12))
        fig.suptitle(PARAMETER_LABELS[parameter_name], fontsize=16)
        ax.set_xlabel('Pressure, Pa', fontsize=12)
        ax.set_ylabel(PARAMETER_LABELS[parameter_name], fontsize=12)

        ax.grid(which='major', linestyle='-', linewidth=1.0, color='#666666')
        ax.grid(which='minor', linestyle=':', linewidth=0.8, color='#666666')
        ax.minorticks_on()
        ax.xaxis.set_minor_locator(ticker.AutoMinorLocator(5))
        ax.yaxis.set_minor_locator(ticker.AutoMinorLocator(5))

        for fuel in data:
            name = fuel['name']
            pressures = []
            values = []

            try:
                aluminum_coeffs = fuel['components']['Aluminum']['agglomeration_coefficients']
                if parameter_name == 'skeleton_surface_fraction':
                    pocket_mass = fuel['pocket_mass_fraction']
            except KeyError:
                print(f"Warning: '{name}' missing required data for {parameter_name}. Skipping.")
                continue

            for frame in fuel['pressure_frames']:
                pressure = frame['pressure']
                pressures.append(pressure)

                agglomeration = _calculate_agglomeration_fraction(aluminum_coeffs, pressure)

                if parameter_name == 'agglomeration_fraction':
                    value = agglomeration
                else:  # skeleton_surface_fraction
                    if pocket_mass == 0:
                        print(f"Warning: '{name}' has zero pocket_mass_fraction. Skipping.")
                        continue
                    value = agglomeration / pocket_mass

                values.append(value)

            line, = ax.plot(pressures, values,
                            label=name,
                            marker='D',
                            markersize=6,
                            linewidth=2)

            _draw_confidence_intervals(
                ax,
                fuel,
                line.get_color(),
                divisor=pocket_mass if parameter_name == 'skeleton_surface_fraction' else 1.0)

        ax.legend(fontsize=10)
        plt.tight_layout(rect=[0, 0.03, 1, 0.95])
        plt.savefig(output_filename, dpi=300, bbox_inches='tight')
        plt.close()
        print(f"Saved plot to: {output_filename}")
        return

    # Per-phase thermodynamic fields: one sub-plot per gas phase, fuels as curves.
    if parameter_name == 'temperatures':
        phases = TEMPERATURE_PHASES
    else:
        phases = _gas_phase_phases(parameter_name)

    _plot_phase_grid(data, phases, PARAMETER_LABELS[parameter_name],
                     PARAMETER_LABELS[parameter_name], output_filename)

def parse_args():
    """Parse command-line arguments.

    The propellants JSON stays a bare positional argument so the existing
    single-argument invocation (`python main.py propellants.json`) keeps working
    unchanged -- that is the contract the .NET PythonPlotsRenderer depends on.
    """
    parser = argparse.ArgumentParser(
        description="Render thermodynamic plots for a set of propellants."
    )
    parser.add_argument(
        "propellants",
        help="Path to the propellant JSON file (e.g., propellants.json)."
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="Directory to write the PNG plots into. Defaults to the current "
             "working directory, matching the historical behaviour."
    )
    return parser.parse_args()


def resolve_output_path(output_dir, filename):
    """Return the path to write `filename` to, creating `output_dir` if needed.

    With no --output-dir the bare filename is returned verbatim, so the files land
    in the process working directory exactly as they always have.
    """
    if not output_dir:
        return filename

    if not os.path.exists(output_dir):
        os.makedirs(output_dir)
        print(f"Created directory: {output_dir}")

    return os.path.join(output_dir, filename)


if __name__ == "__main__":
    args = parse_args()

    file_path = args.propellants
    try:
        data = read_json(file_path)
    except FileNotFoundError:
        print(f"Error: File '{file_path}' not found")
        sys.exit(1)
    except json.JSONDecodeError:
        print(f"Error: Invalid JSON format in '{file_path}'")
        sys.exit(1)

    for param in PLOT_PARAMETERS:
        output_path = resolve_output_path(args.output_dir, f"{param}_plot.png")
        plot_parameter(data, param, output_path)
