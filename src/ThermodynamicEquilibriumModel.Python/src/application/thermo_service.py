"""Module for thermodynamic calculations.

This module defines the ThermoService class that computes thermodynamic properties
(such as enthalpy (H), entropy (S), Gibbs free energy (G), and specific heat (Cp)) at a given temperature.
The calculation is based on input substance data (ChemicalFormula), a list of combustion reaction products
(ReactionProduct), a matrix of thermodynamic coefficients (M), and a reference pressure (P0).

Note:
    This is a simplified/dummy implementation. In a real application, the Gibbs function and
    related thermodynamic equations would be implemented in detail.
"""

import numpy as np
from typing import List, Tuple, Dict

from models.chemical_formula import ChemicalFormula
from models.reaction_product import ReactionProduct
from infrastructure.optimizer import optimize_gibbs

class ThermoService:
    def __init__(self, M: np.ndarray, P0: float):
        self.M = M
        self.P0 = P0 / 101325  # Convert Pa to atm

    def _i(self, num: int, T: float) -> float:
        t_k = T * 1e-3
        return 4.184 * (
            self.M[num, 1] +
            self.M[num, 2] * t_k +
            self.M[num, 3] * t_k**2 +
            self.M[num, 4] * t_k**3 +
            self.M[num, 5] * t_k**4 +
            self.M[num, 6] * t_k**5 +
            self.M[num, 7] * t_k**6 +
            self.M[num, 8] * t_k**7
        )

    def _s(self, num: int, T: float) -> float:
        t_k = T * 1e-3
        return 4.184 * (
            self.M[num, 0] +
            1e-3 * self.M[num, 2] * np.log(t_k) +
            1e-3 * (
                2 * self.M[num, 3] * t_k +
                1.5 * self.M[num, 4] * t_k**2 +
                (4/3) * self.M[num, 5] * t_k**3 +
                1.25 * self.M[num, 6] * t_k**4 +
                1.2 * self.M[num, 7] * t_k**5 +
                (7/6) * self.M[num, 8] * t_k**6
            )
        )

    def _c(self, num: int, T: float) -> float:
        t_k = T * 1e-3
        return 4.184 * 1e-3 * (
            self.M[num, 2] +
            2 * self.M[num, 3] * t_k +
            3 * self.M[num, 4] * t_k**2 +
            4 * self.M[num, 5] * t_k**3 +
            5 * self.M[num, 6] * t_k**4 +
            6 * self.M[num, 7] * t_k**5 +
            7 * self.M[num, 8] * t_k**6
        )

    def calculate(
        self,
        T: float,
        formula: ChemicalFormula,
        gas_products: List[ReactionProduct],
        condensed_products: List[ReactionProduct]
    ) -> Dict:
        
        # Filter products by temperature
        gas_products = [p for p in gas_products if p.temp_range[0] <= T <= p.temp_range[1]]
        condensed_products = [p for p in condensed_products if p.temp_range[0] <= T <= p.temp_range[1]]
        
        # Build constraint matrix
        elements = formula.elements
        n_gas = len(gas_products)
        n_cond = len(condensed_products)
        n_elem = len(elements)
        
        # Create A_eq matrix
        A_eq = np.zeros((n_gas + n_cond + n_elem, n_gas + n_cond + n_elem))
        
        # Gas products constraints
        for i, prod in enumerate(gas_products):
            for elem, coeff in prod.formula.composition.items():
                j = elements.index(elem)
                A_eq[i, j] = coeff
                
        # Condensed products constraints
        for i, prod in enumerate(condensed_products, start=n_gas):
            for elem, coeff in prod.formula.composition.items():
                j = elements.index(elem)
                A_eq[i, j] = coeff
                
        # Element constraints
        for i in range(n_elem):
            A_eq[n_gas + n_cond + i, n_gas + n_cond + i] = 1.0
            
        A_eq = A_eq.T
        b_eq = np.array(formula.values)
        
        # Initial guess
        x0 = np.ones(n_gas + n_cond + n_elem)
        
        # Define Gibbs function
        def gibbs(x: np.ndarray) -> float:
            G = 0.0
            ns = np.sum(x[:n_gas]) + np.sum(x[n_gas + n_cond:])
            
            for i in range(len(x)):
                G += x[i] * (self._i(i, T) - T * self._s(i, T))
                if i < n_gas or i >= n_gas + n_cond:
                    if ns > 0 and x[i] > 0:
                        G += x[i] * 8.314 * T * np.log(x[i] / ns * self.P0)
            return G
        
        # Run optimization
        x_opt, Gt = optimize_gibbs(
            objective=gibbs,
            x0=x0,
            A_eq=A_eq,
            b_eq=b_eq,
            bounds=[(0, None)] * len(x0),
            options={'maxiter': 1000, 'ftol': 1e-6}
        )
        
        # Post-processing
        ns = np.sum(x_opt[:n_gas]) + np.sum(x_opt[n_gas + n_cond:])
        
        Ht = sum(x * self._i(i, T) for i, x in enumerate(x_opt))
        St = sum(x * self._s(i, T) for i, x in enumerate(x_opt))
        Cp = sum(x * self._c(i, T) for i, x in enumerate(x_opt)) / ns if ns != 0 else 0
        
        # Adjust entropy for gaseous components
        for i in range(len(x_opt)):
            if i < n_gas or i >= n_gas + n_cond:
                if ns > 0 and x_opt[i] > 0:
                    St -= x_opt[i] * 8.314 * np.log(x_opt[i] / ns * self.P0)
        
        return {
            'RPm': x_opt[:n_gas] / ns,
            'RPc': x_opt[n_gas:n_gas + n_cond] / ns,
            'RPa': x_opt[n_gas + n_cond:] / ns,
            'Ht': Ht,
            'St': St,
            'Gt': Gt,
            'Cp': Cp
        }
