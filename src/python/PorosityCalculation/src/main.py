import argparse
import os.path
from models import Propellant, RegionCalculationResult
from json_reader import load_propellants, load_region_result
from calculators import calculate_porosity, calculate_region_density
from json_writer import write_porosity_result

def get_output_path(region_file: str) -> str:
    """Generates output path for porosity results"""
    directory = os.path.dirname(region_file)
    return os.path.join(directory, "porosity.json")

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--propellants-file', required=True)
    parser.add_argument('--propellant-name', required=True)
    parser.add_argument('--region-file', required=True)
    
    args = parser.parse_args()
    
    # Data loading
    propellants = load_propellants(args.propellants_file)
    region_result = load_region_result(args.region_file)
    
    # Find propellant
    propellant = next((p for p in propellants if p.name == args.propellant_name), None)
    if not propellant:
        raise ValueError(f"Propellant '{args.propellant_name}' not found")
    
    # Calculation
    result = calculate_porosity(
        calculate_region_density(propellant),
        propellant,
        region_result
    )
    
    # Save results
    output_path = get_output_path(args.region_file)
    write_porosity_result(result, output_path)
    
    # Output summary
    print(f"Results saved to: {output_path}")
    print(f"Region density: {result.region_density:.1f} kg/m³")
    print(f"Porosity: {result.porosity:.4f}")

if __name__ == '__main__':
    main()
