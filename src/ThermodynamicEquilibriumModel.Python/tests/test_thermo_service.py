"""Tests for the ThermoService in the application layer using pytest.

This file tests the calculate() method of the ThermoService class.
"""

import sys
import os
# Add the 'src' directory to PYTHONPATH so that domain and application modules can be imported.
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'src'))

import numpy as np
import pytest

from models.chemical_formula import ChemicalFormula
from models.reaction_product import ReactionProduct
from models.temperature_range import TemperatureRange
from application.thermo_service import ThermoService


@pytest.fixture
def dummy_data():
    """Fixture that provides dummy data for thermodynamic calculations."""
    # Dummy thermodynamic coefficient matrix for 3 products.
    M = np.array([
        [40.0, -2000.0, 70.0],
        [50.0, -2500.0, 80.0],
        [60.0, -3000.0, 90.0]
    ])
    P0 = 101325
    thermo_service = ThermoService(M=M, P0=P0)
    # Dummy chemical formula with 2 elements.
    chemical_formula = ChemicalFormula(composition={"C": 12.0, "H": 24.0})
    # Dummy reaction products.
    temp_range = TemperatureRange(min=1000, max=5000)
    products = [
        ReactionProduct(formula="CO", coefficients=[40.0, -2000.0, 70.0], phase="gas", temperature_range=temp_range, is_condensed=False),
        ReactionProduct(formula="CO2", coefficients=[50.0, -2500.0, 80.0], phase="gas", temperature_range=temp_range, is_condensed=False),
        ReactionProduct(formula="H2O", coefficients=[60.0, -3000.0, 90.0], phase="gas", temperature_range=temp_range, is_condensed=False),
    ]
    return thermo_service, chemical_formula, products


def test_calculate(dummy_data):
    """Test that ThermoService.calculate() returns the expected output structure."""
    thermo_service, chemical_formula, products = dummy_data
    T = 1500.0  # Temperature in Kelvin.
    result = thermo_service.calculate(T, chemical_formula, products)
    
    # Expecting a tuple of 5 values.
    assert len(result) == 5

    x_opt, H, S, G, Cp = result
    assert isinstance(x_opt, np.ndarray)
    assert isinstance(H, float)
    assert isinstance(S, float)
    assert isinstance(G, float)
    assert isinstance(Cp, float)
    # Verify that all optimized decision variables are non-negative.
    assert np.all(x_opt >= 0)
