import json
import sys

import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

from typing import List

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

    Note: This implementation clamps the result to [0, 100] (percentage range).
    This differs from RegionMapper's implementation which returns raw values.
    """
    normalized_pressure = pressure / 1e6
    fraction = sum(coeff * (normalized_pressure)**i for i, coeff in enumerate(coefficients))
    return max(0, min(100, fraction))


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

            ax.plot(pressures, values,
                    label=name,
                    marker='D',
                    markersize=6,
                    linewidth=2)

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

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python script.py <path/to/propellants.json>")
        sys.exit(1)

    file_path = sys.argv[1]
    try:
        data = read_json(file_path)
    except FileNotFoundError:
        print(f"Error: File '{file_path}' not found")
        sys.exit(1)
    except json.JSONDecodeError:
        print(f"Error: Invalid JSON format in '{file_path}'")
        sys.exit(1)

    parameters = [
        'lambda_gas',
        'average_molar_mass',
        'c_volume',
        'temperatures',
        'agglomeration_fraction',
        'skeleton_surface_fraction'
    ]

    for param in parameters:
        output_name = f"{param}_plot.png"
        plot_parameter(data, param, output_name)
