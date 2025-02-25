"""Tests for the JSON reader module using pytest.

These tests verify that JSON files for combustion products and input substance data
are correctly parsed into domain objects.
"""

import sys
import os
# Ensure the 'src' directory is in the PYTHONPATH.
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'src'))

import json
import pytest

from models.chemical_formula import ChemicalFormula
from infrastructure.json_reader import load_combustion_products, load_input_substance


def test_load_combustion_products(tmp_path):
    """Test loading combustion products from a JSON file."""
    data = [
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
    ]
    file_path = tmp_path / "combustion_products.json"
    file_path.write_text(json.dumps(data))
    
    products = load_combustion_products(str(file_path))
    assert len(products) == 1
    product = products[0]
    assert product.formula == "O"
    assert product.phase == "gas"
    assert product.is_condensed is False
    assert product.temperature_range.min == 1000
    assert product.temperature_range.max == 5000
    assert len(product.coefficients) == 9


def test_load_input_substance(tmp_path):
    """Test loading input substance data from a JSON file."""
    data = {
        "enthalpy": 1.23e-10,
        "formula": {
            "C": 45.67,
            "H": 67.98
        }
    }
    file_path = tmp_path / "input_substance.json"
    file_path.write_text(json.dumps(data))
    
    enthalpy, formula = load_input_substance(str(file_path))
    assert abs(enthalpy - 1.23e-10) < 1e-20
    assert isinstance(formula, ChemicalFormula)
    assert "C" in formula.composition
    assert "H" in formula.composition
    assert abs(formula.composition["C"] - 45.67) < 1e-6
    assert abs(formula.composition["H"] - 67.98) < 1e-6
