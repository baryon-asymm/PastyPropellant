from dataclasses import dataclass
from typing import Optional, Dict, List

@dataclass(frozen=True)
class PropellantComponent:
    """
    Represents a component of a propellant with its mass fraction and optional properties.

    Attributes:
        mass_fraction (float): The mass fraction of the component in the propellant (0 to 1).
        density (float): The density of the component in kg/m^3.
        large_particles_fraction (Optional[float]): The fraction of the component that consists of large particles.
            Defaults to None if not applicable.
        agglomeration_coefficients (Optional[List[float]]): Coefficients used to calculate agglomeration effects.
            Defaults to None if not applicable.

    Example:
        >>> PropellantComponent(
        ...     mass_fraction=0.3,
        ...     density=1000,
        ...     large_particles_fraction=0.1,
        ...     agglomeration_coefficients=[0.05, 0.02, 0.01]
        ... )
        PropellantComponent(mass_fraction=0.3, large_particles_fraction=0.1, agglomeration_coefficients=[0.05, 0.02, 0.01])
    """
    mass_fraction: float
    density: float
    large_particles_fraction: Optional[float]
    agglomeration_coefficients: Optional[List[float]]


@dataclass(frozen=True)
class Propellant:
    """
    Represents a propellant composed of multiple components.

    Attributes:
        name (str): The name of the propellant (e.g., "SolidRocketFuel").
        density (float): The density of the propellant in kg/m^3.
        components (Dict[str, PropellantComponent]): A dictionary where keys are component names 
            (e.g., "CombustibleBinder") and values are `PropellantComponent` objects.

    Example:
        >>> Propellant(
        ...     name="SolidRocketFuel",
        ...     density=1500,
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
    density: float
    components: Dict[str, PropellantComponent]

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
