import json

from calculators import PorosityCalculationResult

def write_porosity_result(result: PorosityCalculationResult, output_path: str):
    """Saves porosity calculation results to JSON file"""
    data = {
        "region_density": result.region_density,
        "porosity": result.porosity,
        "region_input": {
            "pressure": result.region_input.pressure,
            "enthalpy": result.region_input.enthalpy,
            "composition": result.region_input.composition
        }
    }
    
    with open(output_path, 'w') as f:
        json.dump(data, f, indent=4)
