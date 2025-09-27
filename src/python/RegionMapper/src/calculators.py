from dataclasses import dataclass
from typing import Dict

from models import Component
from region_mappers import RegionData
from utils import (
    calculate_elemental_composition,
    calculate_enthalpy,
    normalize_elemental_composition
)

@dataclass(frozen=True)
class RegionCalculationResult:
    """
    Data Transfer Object (DTO) for storing the result of calculations for a region.

    Attributes:
        pressure (float): Pressure in Pascals.
        enthalpy (float): Overall enthalpy of the region in joule per kg.
        composition (Dict[str, float]): Normalized elemental composition of the region,
            where keys are element symbols (e.g., "H", "C") and values are their normalized contributions.
    """
    pressure: float
    enthalpy: float
    composition: Dict[str, float]

class RegionCalculator:
    """
    Calculates the overall chemical formula, enthalpy, and other properties for a given region.

    Methods:
        calculate(region_data: RegionData, component_data: Dict[str, Component], pressure: float) -> RegionCalculationResult:
            Calculates the chemical formula, enthalpy, and composition for the region.
    """

    @staticmethod
    def calculate(
        region_data: RegionData, 
        component_data: Dict[str, Component], 
        pressure: float
    ) -> RegionCalculationResult:
        """
        Calculate the overall chemical formula, enthalpy, and composition for the region.

        Args:
            region_data (RegionData): A dataclass containing components and their normalized mass fractions.
            component_data (Dict[str, Component]): A dictionary where keys are component names and values are 
                `Component` objects containing elemental composition and other properties.
            pressure (float): Pressure in Pascals.

        Returns:
            RegionCalculationResult: A dataclass containing the calculated pressure, enthalpy, composition.
        """
        # Calculate the overall elemental composition
        raw_elemental_composition = calculate_elemental_composition(region_data.components, component_data)
        normalized_elemental_composition = normalize_elemental_composition(raw_elemental_composition)

        # Calculate the overall enthalpy
        enthalpy = calculate_enthalpy(region_data.components, component_data)

        # Return the results as a DTO
        return RegionCalculationResult(
            pressure=pressure,
            enthalpy=enthalpy,
            composition=normalized_elemental_composition
        )
