"""Module for optimizing the Gibbs function.

This module provides a function to perform constrained minimization using
SciPy's SLSQP optimizer.
"""

import numpy as np
from scipy.optimize import minimize

def optimize_gibbs(objective, x0, A_eq, b_eq, bounds, options):
    constraints = [{'type': 'eq', 'fun': lambda x: A_eq.dot(x) - b_eq}]
    
    result = minimize(
        objective,
        x0,
        method='SLSQP',
        bounds=bounds,
        constraints=constraints,
        options=options
    )
    
    if not result.success:
        raise RuntimeError(f"Optimization failed: {result.message}")
    
    return result.x, result.fun
