import json

from typing import Dict, List
from models import Component, Propellant, PropellantComponent

def read_components(file_path: str) -> Dict[str, Component]:
    """
    Reads the components.json file and returns a dictionary of Component objects.
    """
    with open(file_path, 'r', encoding='utf-8') as file:
        data = json.load(file)

    components = {}
    for item in data:
        # Extract the first key-value pair in the dictionary
        component_name, component_data = next(iter(item.items()))
        components[component_name] = Component(
            name=component_name,
            composition=component_data.get("composition", {}),
            enthalpy=component_data.get("enthalpy", 0.0)
        )
    return components

def read_propellants(file_path: str) -> List[Propellant]:
    """
    Reads the propellant.json file and returns a list of Propellant objects.
    """
    with open(file_path, 'r', encoding='utf-8') as file:
        data = json.load(file)

    propellants = []
    for item in data:
        propellant_name = item.get("name")
        propellant_data = item.get("components", {})
        if not isinstance(propellant_data, dict):
            raise ValueError(f"Expected a dictionary for propellant data, but got {type(propellant_data)}")

        components = {
            component_name: parse_propellant_component(component_data)
            for component_name, component_data in propellant_data.items()
        }
        propellants.append(Propellant(name=propellant_name, components=components))
    return propellants

def parse_propellant_component(data: dict) -> PropellantComponent:
    """
    Parses a single PropellantComponent from a dictionary.
    """
    return PropellantComponent(
        mass_fraction=data.get("mass_fraction", 0.0),
        large_particles_fraction=data.get("large_particles_fraction"),
        agglomeration_coefficients=data.get("agglomeration_coefficients", [])
    )
