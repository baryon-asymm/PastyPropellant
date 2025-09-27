from typing import Dict

from molar_masses import ELEMENT_MOLAR_MASSES
from models import Component

def compute_molar_mass(elements: Dict[str, float]) -> float:
    """
    Calculates the molar mass of a compound from its elemental composition.

    Args:
        elements (Dict[str, float]): A dictionary where keys are element symbols (e.g., "H", "C") 
            and values are their respective counts in the compound.

    Returns:
        float: The molar mass of the compound in kg/mol.

    Raises:
        KeyError: If any element is not found in the `ELEMENT_MOLAR_MASSES` database.
    """
    molar_mass = 0.0
    for element, count in elements.items():
        if element not in ELEMENT_MOLAR_MASSES:
            raise KeyError(f"Element '{element}' not found in molar mass database.")
        molar_mass += ELEMENT_MOLAR_MASSES[element] * count
    return molar_mass

def calculate_elemental_composition(
    region_data: Dict[str, float], 
    component_data: Dict[str, Component]
) -> Dict[str, float]:
    """
    Calculate the overall elemental composition of a region based on mass fractions and component compositions.

    Args:
        region_data (Dict[str, float]): A dictionary where keys are component names (e.g., "CombustibleBinder") 
            and values are their mass fractions in the region.
        component_data (Dict[str, Component]): A dictionary where keys are component names and values are 
            `Component` objects containing elemental composition and other properties.

    Returns:
        Dict[str, float]: A dictionary where keys are element symbols (e.g., "H", "C") and values are their 
            relative contributions to the overall composition.
    """
    total_elemental_composition = {}
    for component_name, mass_fraction in region_data.items():
        component = component_data[component_name]
        molar_mass = compute_molar_mass(component.composition)
        for element, count in component.composition.items():
            relative_content = (count / molar_mass) * mass_fraction
            total_elemental_composition[element] = total_elemental_composition.get(element, 0.0) + relative_content
    return total_elemental_composition

def normalize_elemental_composition(elements: Dict[str, float], propellant_molar_mass: float = 1.0) -> Dict[str, float]:
    """
    Normalize the elemental composition by multiplying by the molar mass of the propellant.

    Args:
        elements (Dict[str, float]): A dictionary where keys are element symbols (e.g., "H", "C") and values 
            are their relative contributions to the composition.
        fuel_molar_mass (float): The molar mass of the propellant in kg/mol. Defaults to 1.0.

    Returns:
        Dict[str, float]: A dictionary where keys are element symbols and values are their normalized contributions.
    """
    return {element: content * propellant_molar_mass for element, content in elements.items()}

def calculate_enthalpy(
    region_data: Dict[str, float], 
    component_data: Dict[str, Component]
) -> float:
    """
    Calculate the overall enthalpy based on mass fractions and component enthalpies.

    Args:
        region_data (Dict[str, float]): A dictionary where keys are component names (e.g., "CombustibleBinder") 
            and values are their mass fractions in the region.
        component_data (Dict[str, Component]): A dictionary where keys are component names and values are 
            `Component` objects containing enthalpy and other properties.

    Returns:
        float: The overall enthalpy of the region in joule per kg.
    """
    total_enthalpy = 0.0
    for component_name, mass_fraction in region_data.items():
        enthalpy = component_data[component_name].enthalpy
        total_enthalpy += enthalpy * mass_fraction
    return total_enthalpy
