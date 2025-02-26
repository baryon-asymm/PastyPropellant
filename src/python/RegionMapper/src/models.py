from dataclasses import dataclass
from typing import Optional, Dict, List

@dataclass(frozen=True)
class Component:
    """
    Represents a chemical component with its composition and enthalpy.

    Attributes:
        name (str): The name of the component (e.g., "CombustibleBinder").
        composition (Dict[str, float]): A dictionary where keys are element symbols (e.g., "H", "C") 
            and values are their respective counts in the component.
        enthalpy (float): The enthalpy of the component in joules per kilogram.

    Example:
        >>> Component(
        ...     name="CombustibleBinder",
        ...     composition={"C": 1, "H": 4},
        ...     enthalpy=-1000
        ... )
        Component(name='CombustibleBinder', composition={'C': 1, 'H': 4}, enthalpy=-1000)
    """
    name: str
    composition: Dict[str, float]
    enthalpy: float


@dataclass(frozen=True)
class PropellantComponent:
    """
    Represents a component of a propellant with its mass fraction and optional properties.

    Attributes:
        mass_fraction (float): The mass fraction of the component in the propellant (0 to 1).
        large_particles_fraction (Optional[float]): The fraction of the component that consists of large particles.
            Defaults to None if not applicable.
        agglomeration_coefficients (Optional[List[float]]): Coefficients used to calculate agglomeration effects.
            Defaults to None if not applicable.

    Example:
        >>> PropellantComponent(
        ...     mass_fraction=0.3,
        ...     large_particles_fraction=0.1,
        ...     agglomeration_coefficients=[0.05, 0.02, 0.01]
        ... )
        PropellantComponent(mass_fraction=0.3, large_particles_fraction=0.1, agglomeration_coefficients=[0.05, 0.02, 0.01])
    """
    mass_fraction: float
    large_particles_fraction: Optional[float]
    agglomeration_coefficients: Optional[List[float]]


@dataclass(frozen=True)
class Propellant:
    """
    Represents a propellant composed of multiple components.

    Attributes:
        name (str): The name of the propellant (e.g., "SolidRocketFuel").
        components (Dict[str, PropellantComponent]): A dictionary where keys are component names 
            (e.g., "CombustibleBinder") and values are `PropellantComponent` objects.

    Example:
        >>> Propellant(
        ...     name="SolidRocketFuel",
        ...     components={
        ...         "CombustibleBinder": PropellantComponent(
        ...             mass_fraction=0.25,
        ...             large_particles_fraction=None,
        ...             agglomeration_coefficients=None
        ...         ),
        ...         "AmmoniumPerchlorate": PropellantComponent(
        ...             mass_fraction=0.5,
        ...             large_particles_fraction=0.1,
        ...             agglomeration_coefficients=[0.05, 0.02, 0.01]
        ...         )
        ...     }
        ... )
        Propellant(name='SolidRocketFuel', components={'CombustibleBinder': PropellantComponent(...), ...})
    """
    name: str
    components: Dict[str, PropellantComponent]
