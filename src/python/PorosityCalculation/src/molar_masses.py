"""Module providing molar masses of chemical elements.

This module defines a dictionary, `ELEMENT_MOLAR_MASSES`, which contains the molar masses
of all known chemical elements. The molar masses are expressed in kilograms per mole (kg/mol)
and are derived from standard atomic weight data.

The molar masses provided in this module can be used for thermodynamic and stoichiometric
calculations, such as computing the mass of a compound from its chemical formula or determining
the normalization condition for propellant compositions.

Attributes:
    ELEMENT_MOLAR_MASSES (dict[str, float]): A dictionary mapping element symbols (e.g., 'H', 'O')
        to their molar masses in kg/mol. For example:
        {
            "H": 0.00100784,
            "O": 0.015999,
            "Fe": 0.055845,
            ...
        }

Usage Example:
    >>> from molar_masses import ELEMENT_MOLAR_MASSES
    >>> print(ELEMENT_MOLAR_MASSES["H"])  # Molar mass of hydrogen
    0.00100784
    >>> print(ELEMENT_MOLAR_MASSES["O"])  # Molar mass of oxygen
    0.015999

Note:
    - The molar masses are provided in kilograms per mole (kg/mol) for consistency with SI units.
    - This module is intended for use in scientific computations involving chemical elements,
      such as combustion processes, thermodynamic equilibrium calculations, and material science.
"""

ELEMENT_MOLAR_MASSES = {
    "H": 0.00100784,
    "He": 0.004002602,
    "Li": 0.00694,
    "Be": 0.0090121831,
    "B": 0.01081,
    "C": 0.012011,
    "N": 0.014007,
    "O": 0.015999,
    "F": 0.018998403163,
    "Ne": 0.0201797,
    "Na": 0.02298976928,
    "Mg": 0.024305,
    "Al": 0.0269815385,
    "Si": 0.028085,
    "P": 0.030973761998,
    "S": 0.03206,
    "Cl": 0.03545,
    "Ar": 0.039948,
    "K": 0.0390983,
    "Ca": 0.040078,
    "Sc": 0.044955908,
    "Ti": 0.047867,
    "V": 0.0509415,
    "Cr": 0.0519961,
    "Mn": 0.054938044,
    "Fe": 0.055845,
    "Co": 0.058933194,
    "Ni": 0.0586934,
    "Cu": 0.063546,
    "Zn": 0.06538,
    "Ga": 0.069723,
    "Ge": 0.07263,
    "As": 0.074921595,
    "Se": 0.078971,
    "Br": 0.079904,
    "Kr": 0.083798,
    "Rb": 0.0854678,
    "Sr": 0.08762,
    "Y": 0.08890584,
    "Zr": 0.091224,
    "Nb": 0.09290637,
    "Mo": 0.09595,
    "Tc": 0.097,
    "Ru": 0.10107,
    "Rh": 0.1029055,
    "Pd": 0.10642,
    "Ag": 0.1078682,
    "Cd": 0.112414,
    "In": 0.114818,
    "Sn": 0.11871,
    "Sb": 0.12176,
    "Te": 0.1276,
    "I": 0.12690447,
    "Xe": 0.131293,
    "Cs": 0.13290545196,
    "Ba": 0.137327,
    "La": 0.13890547,
    "Ce": 0.140116,
    "Pr": 0.14090766,
    "Nd": 0.144242,
    "Pm": 0.145,
    "Sm": 0.15036,
    "Eu": 0.151964,
    "Gd": 0.15725,
    "Tb": 0.15892535,
    "Dy": 0.1625,
    "Ho": 0.16493033,
    "Er": 0.167259,
    "Tm": 0.16893422,
    "Yb": 0.173045,
    "Lu": 0.1749668,
    "Hf": 0.17849,
    "Ta": 0.18094788,
    "W": 0.18384,
    "Re": 0.186207,
    "Os": 0.19023,
    "Ir": 0.192217,
    "Pt": 0.195084,
    "Au": 0.196966569,
    "Hg": 0.200592,
    "Tl": 0.20438,
    "Pb": 0.2072,
    "Bi": 0.2089804,
    "Th": 0.2320377,
    "Pa": 0.23103588,
    "U": 0.23802891,
    "Np": 0.237,
    "Pu": 0.244,
    "Am": 0.243,
    "Cm": 0.247,
    "Bk": 0.247,
    "Cf": 0.251,
    "Es": 0.252,
    "Fm": 0.257,
    "Md": 0.258,
    "No": 0.259,
    "Lr": 0.262,
    "Rf": 0.267,
    "Db": 0.268,
    "Sg": 0.271,
    "Bh": 0.272,
    "Hs": 0.27,
    "Mt": 0.276,
    "Ds": 0.281,
    "Rg": 0.28,
    "Cn": 0.285,
    "Nh": 0.286,
    "Fl": 0.289,
    "Mc": 0.289,
    "Lv": 0.293,
    "Ts": 0.294,
    "Og": 0.294
}
