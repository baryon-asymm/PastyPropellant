import json
from typing import List

from models import Propellant, PropellantComponent, RegionCalculationResult

def load_propellants(file_path: str) -> List[Propellant]:
    with open(file_path, 'r') as f:
        data = json.load(f)
    propellants = []
    for item in data:
        components = {}
        for comp_name, comp_data in item['components'].items():
            components[comp_name] = PropellantComponent(
                mass_fraction=comp_data['mass_fraction'],
                density=comp_data['density'],
                large_particles_fraction=comp_data.get('large_particles_fraction'),
                agglomeration_coefficients=comp_data.get('agglomeration_coefficients')
            )
        propellant = Propellant(
            name=item['name'],
            density=item['density'],
            components=components
        )
        propellants.append(propellant)
    return propellants

def load_region_result(file_path: str) -> RegionCalculationResult:
    with open(file_path, 'r') as f:
        data = json.load(f)
    return RegionCalculationResult(
        pressure=data['pressure'],
        enthalpy=data['enthalpy'],
        composition=data['composition']
    )