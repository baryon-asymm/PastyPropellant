"""Module for reading and parsing JSON input files.

This module provides functions to load combustion products and the input substance data
from JSON files and convert them into domain objects.
"""

import json
from typing import List, Tuple

from models.reaction_product import ReactionProduct
from models.temperature_range import TemperatureRange
from models.chemical_formula import ChemicalFormula


def load_combustion_products(filepath: str) -> List[ReactionProduct]:
    """Loads combustion products from a JSON file and returns a list of ReactionProduct objects.

    The JSON file must contain an array of objects, each with the following structure:
        {
          "formula": "O",
          "coefficients": [
            45.168916,
            58008.607,
            5353.7423,
            -412.44632,
            246.19247,
            -86.140481,
            17.415382,
            -1.8288189,
            0.077299666
          ],
          "phase": "gas",
          "temperature_range": {
            "min": 1000,
            "max": 5000
          }
        }

    Args:
        filepath (str): The path to the combustion products JSON file.

    Returns:
        List[ReactionProduct]: A list of ReactionProduct domain objects.

    Raises:
        FileNotFoundError: If the specified file does not exist.
        json.JSONDecodeError: If the JSON file is malformed.
        ValueError: If required keys are missing in the JSON data.
    """
    products: List[ReactionProduct] = []
    with open(filepath, "r", encoding="utf-8") as file:
        data = json.load(file)

    for item in data:
        try:
            temp_range = TemperatureRange(
                min=item["temperature_range"]["min"],
                max=item["temperature_range"]["max"]
            )
            is_condensed = (item["phase"] == "condensed")
            product = ReactionProduct(
                formula=item["formula"],
                coefficients=item["coefficients"],
                phase=item["phase"],
                temperature_range=temp_range,
                is_condensed=is_condensed
            )
            products.append(product)
        except KeyError as error:
            raise ValueError(f"Missing key in combustion product data: {error}")
    return products

def load_input_substance(filepath: str) -> Tuple[float, ChemicalFormula]:
    """Loads input substance data from a JSON file and returns its enthalpy and chemical formula.

    The JSON file must contain an object with the following structure:
        {
            "enthalpy": 1.23e-10,
            "formula": {
                "C": 45.67,
                "H": 67.98,
                ...
            }
        }

    Args:
        filepath (str): The path to the input substance JSON file.

    Returns:
        Tuple[float, ChemicalFormula]: A tuple containing the enthalpy (float) and a ChemicalFormula object.

    Raises:
        FileNotFoundError: If the specified file does not exist.
        json.JSONDecodeError: If the JSON file is malformed.
        ValueError: If required keys are missing in the JSON data.
    """
    with open(filepath, "r", encoding="utf-8") as file:
        data = json.load(file)
    try:
        enthalpy = data["enthalpy"]
        formula_data = data["formula"]
        chemical_formula = ChemicalFormula(composition=formula_data)
    except KeyError as error:
        raise ValueError(f"Missing key in input substance data: {error}")
    return enthalpy, chemical_formula
