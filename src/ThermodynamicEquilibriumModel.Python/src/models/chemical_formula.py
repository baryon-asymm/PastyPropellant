"""Module for representing a chemical formula.

This module defines the ChemicalFormula class, which encapsulates the composition of a substance
using a dictionary.
"""

from dataclasses import dataclass
from typing import Dict

@dataclass(frozen=True)
class ChemicalFormula:
    """Represents the chemical formula of a substance.

    Attributes:
        composition (Dict[str, float]): A dictionary where the key is the element symbol (e.g., "C", "H")
            and the value is its mass fraction or quantity.
    """
    composition: Dict[str, float]
