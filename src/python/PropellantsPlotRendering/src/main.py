import json
import sys

import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

from typing import List

PARAMETER_LABELS = {
    'lambda_gas': 'Thermal Conductivity (λ), W/(m·K)',
    'average_molar_mass': 'Average Molar Mass, kg/mol',
    'c_volume': 'Constant Volume Heat Capacity (cₚ), J/(kg·K)',
    'temperatures': 'Temperature of the Flames, K',
    'agglomeration_fraction': 'Aluminum Agglomeration Fraction',
    'skeleton_surface_fraction': 'Skeleton Surface Fraction (Agglomeration/Pocket Mass)'
}

def read_json(file_path):
    with open(file_path, 'r') as file:
        return json.load(file)

def _calculate_agglomeration_fraction(coefficients: List[float], pressure: float) -> float:
    normalized_pressure = pressure / 1e6
    fraction = sum(coeff * (normalized_pressure)**i for i, coeff in enumerate(coefficients))
    return max(0, min(100, fraction))

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

    num_fuels = len(data)
    rows = (num_fuels + 1) // 2
    fig, axs = plt.subplots(rows, 2, figsize=(16, 12))
    fig.suptitle(PARAMETER_LABELS[parameter_name], fontsize=16)
    axs = axs.flatten()

    for i, fuel in enumerate(data):
        ax = axs[i]
        name = fuel['name']
        pressures = []

        if parameter_name == 'temperatures':
            temp_inter = []
            temp_skeleton = []
            temp_out_skeleton = []
            temp_diffusion = []

            for frame in fuel['pressure_frames']:
                pressures.append(frame['pressure'])
                temp_inter.append(frame['inter_pocket_gas_phase']['T_kinetic_flame'])
                temp_skeleton.append(frame['pocket_gas_phase']['skeleton_gas_phase']['T_kinetic_flame'])
                temp_out_skeleton.append(frame['pocket_gas_phase']['out_skeleton_gas_phase']['T_kinetic_flame'])
                temp_diffusion.append(frame['pocket_gas_phase']['T_diffusion_flame'])
            
            ax.plot(pressures, temp_inter, label='Inter-Pocket Gas Phase', marker='o')
            ax.plot(pressures, temp_skeleton, label='Skeleton Gas Phase', marker='^')
            ax.plot(pressures, temp_out_skeleton, label='Outer Skeleton Phase', marker='*')
            ax.plot(pressures, temp_diffusion, label='Diffusion Flame (Pocket)', marker='s')

        else:
            values_inter = []
            values_pocket = []
            values_skeleton = []
            values_out_skeleton = []

            for frame in fuel['pressure_frames']:
                pressures.append(frame['pressure'])
                
                inter_val = frame['inter_pocket_gas_phase'].get(parameter_name)
                pocket_val = frame['pocket_gas_phase'].get(parameter_name)
                skeleton_val = frame['pocket_gas_phase']['skeleton_gas_phase'].get(parameter_name)
                out_skeleton_val = frame['pocket_gas_phase']['out_skeleton_gas_phase'].get(parameter_name)
                
                if inter_val is not None:
                    values_inter.append(inter_val)
                if pocket_val is not None:
                    values_pocket.append(pocket_val)
                if skeleton_val is not None:
                    values_skeleton.append(skeleton_val)
                if out_skeleton_val is not None:
                    values_out_skeleton.append(out_skeleton_val)

            if values_inter:
                ax.plot(pressures, values_inter, label='Inter-Pocket Gas Phase', marker='o')
            if values_pocket:
                ax.plot(pressures, values_pocket, label='Pocket (Diffusion) Gas Phase', marker='s')
            if values_skeleton:
                ax.plot(pressures, values_skeleton, label='Skeleton Gas Phase', marker='^')
            if values_out_skeleton:
                ax.plot(pressures, values_out_skeleton, label='Outer Skeleton Phase', marker='*')

        ax.grid(which='major', linestyle='-', linewidth=1.0, color='#666666')
        ax.grid(which='minor', linestyle=':', linewidth=0.8, color='#666666')
        ax.minorticks_on()
        ax.xaxis.set_minor_locator(ticker.AutoMinorLocator(5))
        ax.yaxis.set_minor_locator(ticker.AutoMinorLocator(5))
        
        ax.set_title(f'{name}', fontsize=14)
        ax.set_xlabel('Pressure, Pa', fontsize=12)
        ax.set_ylabel(PARAMETER_LABELS[parameter_name], fontsize=12)
        ax.legend(fontsize=10)

    for j in range(num_fuels, len(axs)):
        axs[j].axis('off')
    
    plt.tight_layout(rect=[0, 0.03, 1, 0.95])
    plt.savefig(output_filename, dpi=300, bbox_inches='tight')
    plt.close()
    print(f"Saved plot to: {output_filename}")

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
