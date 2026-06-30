"""
Skeleton-layer plots for the competing-flames pocket model.

Renders per-fuel, per-pressure SOLVER OUTPUTS (heat-flux decomposition, flame
heights, skeleton-layer geometry, exit temperatures) from the machine-readable
sidecar `skeleton_layer.json` that the .NET console host writes next to the
report. Style mirrors the thermodynamic plots in main.py: a 2-column grid of
per-fuel sub-plots with major/minor gridlines.

Unlike main.py (which reads the INPUT propellants JSON and therefore shows the
same equilibrium thermodynamics for every run), this script consumes the FITTED
solution, so the curves reflect the optimised parameter vector.

Usage:
    python skeleton_layer_plots.py <path/to/skeleton_layer.json>
"""

import json
import sys

import matplotlib.pyplot as plt
import matplotlib.ticker as ticker


# (key in frame['heat_flux'], legend label, marker)
HEAT_FLUX_SERIES = [
    ('skeleton', 'Skeleton kinetic', 'o'),
    ('out_skeleton', 'Out-skeleton kinetic', '*'),
    ('diffusion', 'Diffusion flame', 's'),
    ('metal_burning', 'Metal burning', 'D'),
    ('to_surface_total', 'Total to surface', '^'),
    ('sublimation', 'Sublimation (sink)', 'v'),
]

FLAME_HEIGHT_SERIES = [
    ('skeleton_kinetic', 'Skeleton kinetic flame', 'o'),
    ('out_skeleton_kinetic', 'Out-skeleton kinetic flame', '*'),
    ('diffusion', 'Diffusion flame', 's'),
]

SKELETON_LAYER_SERIES = [
    ('thickness', 'Skeleton layer thickness', 'o'),
    ('pore_diameter', 'Pore diameter', 's'),
]

TEMPERATURE_SERIES = [
    ('surface_temperature_pocket', 'Surface T (pocket)', 'o'),
    ('surface_temperature_inter_pocket', 'Surface T (inter-pocket)', 's'),
    ('average_metal_burning_temperature', 'Metal burning T (avg)', '^'),
]


def read_json(file_path):
    with open(file_path, 'r') as file:
        return json.load(file)


def _style_axes(ax, ylog=False):
    ax.grid(which='major', linestyle='-', linewidth=1.0, color='#666666')
    ax.grid(which='minor', linestyle=':', linewidth=0.8, color='#666666')
    ax.minorticks_on()
    ax.xaxis.set_minor_locator(ticker.AutoMinorLocator(5))
    if not ylog:  # AutoMinorLocator is invalid on a logarithmic axis
        ax.yaxis.set_minor_locator(ticker.AutoMinorLocator(5))


def _pressures_mpa(fuel):
    return [frame['pressure'] / 1e6 for frame in fuel['pressure_frames']]


def _make_grid(data, suptitle):
    num_fuels = len(data)
    rows = (num_fuels + 1) // 2
    fig, axs = plt.subplots(rows, 2, figsize=(16, 6 * rows))
    fig.suptitle(suptitle, fontsize=16)
    axs = axs.flatten()  # 2 columns => subplots always returns an array
    return fig, axs, num_fuels


def _finish_grid(fig, axs, num_fuels, output_filename):
    for j in range(num_fuels, len(axs)):
        axs[j].axis('off')
    plt.tight_layout(rect=[0, 0.03, 1, 0.95])
    plt.savefig(output_filename, dpi=300, bbox_inches='tight')
    plt.close()
    print(f"Saved plot to: {output_filename}")


def plot_nested(data, group_key, series, suptitle, ylabel, output_filename,
                ylog=False):
    """Plot a dict-valued sub-block (heat_flux / flame_heights / skeleton_layer)."""
    fig, axs, num_fuels = _make_grid(data, suptitle)

    for i, fuel in enumerate(data):
        ax = axs[i]
        pressures = _pressures_mpa(fuel)
        for key, label, marker in series:
            values = []
            for frame in fuel['pressure_frames']:
                block = frame.get(group_key, {})
                values.append(block.get(key))
            if any(v is not None for v in values):
                ax.plot(pressures, values, label=label, marker=marker,
                        markersize=6, linewidth=2)

        if ylog:
            ax.set_yscale('log')
        _style_axes(ax, ylog=ylog)
        ax.set_title(fuel['name'], fontsize=14)
        ax.set_xlabel('Pressure, MPa', fontsize=12)
        ax.set_ylabel(ylabel, fontsize=12)
        ax.legend(fontsize=10)

    _finish_grid(fig, axs, num_fuels, output_filename)


def plot_flat(data, series, suptitle, ylabel, output_filename):
    """Plot frame-level scalar fields (the exit temperatures)."""
    fig, axs, num_fuels = _make_grid(data, suptitle)

    for i, fuel in enumerate(data):
        ax = axs[i]
        pressures = _pressures_mpa(fuel)
        for key, label, marker in series:
            values = [frame.get(key) for frame in fuel['pressure_frames']]
            if any(v is not None for v in values):
                ax.plot(pressures, values, label=label, marker=marker,
                        markersize=6, linewidth=2)

        _style_axes(ax)
        ax.set_title(fuel['name'], fontsize=14)
        ax.set_xlabel('Pressure, MPa', fontsize=12)
        ax.set_ylabel(ylabel, fontsize=12)
        ax.legend(fontsize=10)

    _finish_grid(fig, axs, num_fuels, output_filename)


def render_all(data):
    plot_nested(
        data, 'heat_flux', HEAT_FLUX_SERIES,
        'Pocket Heat-Flux Decomposition', 'Heat flux, W/m²',
        'skeleton_heat_flux_plot.png', ylog=True)

    plot_nested(
        data, 'flame_heights', FLAME_HEIGHT_SERIES,
        'Pocket Flame Heights', 'Flame height, µm',
        'skeleton_flame_heights_plot.png', ylog=True)

    plot_nested(
        data, 'skeleton_layer', SKELETON_LAYER_SERIES,
        'Skeleton-Layer Geometry', 'Length, µm',
        'skeleton_layer_geometry_plot.png')

    plot_flat(
        data, TEMPERATURE_SERIES,
        'Pocket Exit Temperatures', 'Temperature, K',
        'skeleton_temperatures_plot.png')


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python skeleton_layer_plots.py <path/to/skeleton_layer.json>")
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

    render_all(data)
