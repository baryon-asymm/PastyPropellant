"""Module for representing a combustion reaction product.

This module defines the ReactionProduct class, which describes a combustion product including its chemical
formula, coefficients for thermodynamic calculations, phase, and applicable temperature range.
"""

from dataclasses import dataclass
from typing import List
from .temperature_range import TemperatureRange

@dataclass(frozen=True)
class ReactionProduct:
    """Represents a combustion reaction product.

    Attributes:
        formula (str): The chemical formula of the product (e.g., "O").
        coefficients (List[float]): A list of coefficients used for thermodynamic calculations.
        phase (str): The phase of the product (e.g., "gas" for gaseous, "condensed" for condensed).
        temperature_range (TemperatureRange): The temperature range within which the product is relevant.
        is_condensed (bool): A flag indicating whether the product is in a condensed phase.
    """
    formula: str
    coefficients: List[float]
    phase: str
    temperature_range: TemperatureRange
    is_condensed: bool
