"""
This module contains classes responsible for mapping mass fractions of components 
in different regions of a propellant. Each class corresponds to a specific region 
and calculates the normalized mass fractions of components based on the input propellant 
and pressure (if applicable). The classes do not store any data internally; all arguments 
are passed directly to the methods.

Regions:
    1. Inter-pocket region: Homogeneous mixture of all components.
    2. Pocket region without skeleton: Remaining homogeneous mixture excluding large particles.
    3. Pocket region with skeleton: Binder and small fractions of ammonium perchlorate.
    4. Diffusion region: Includes all components except agglomerated aluminium.

Classes:
    - RegionData: DTO for storing the result of preprocessing calculations.
    - InterPocketRegionMapper: Maps mass fractions for the inter-pocket region.
    - PocketRegionWithoutSkeletonMapper: Maps mass fractions for the pocket region without skeleton.
    - PocketRegionWithSkeletonMapper: Maps mass fractions for the pocket region with skeleton.
    - DiffusionRegionMapper: Maps mass fractions for the diffusion region.

Usage:
    Instantiate the appropriate mapper class and call its `calculate()` method, passing 
    the required arguments (e.g., Propellant object and pressure for DiffusionRegionMapper) 
    to obtain the mass fractions for the region. If a component is missing or invalid, 
    an exception will be raised.
"""

from dataclasses import dataclass
from typing import Dict, List

from models import Propellant, PropellantComponent

@dataclass(frozen=True)
class RegionData:
    """
    Data Transfer Object (DTO) for storing the result of preprocessing calculations.

    Attributes:
        components (Dict[str, float]): A dictionary where keys are component names 
            (e.g., "CombustibleBinder", "AmmoniumPerchlorate") and values are their 
            normalized mass fractions in the region. The mass fractions are normalized 
            such that their sum equals 1.0.
    """
    components: Dict[str, float]

class BaseMapper:
    """
    Base class for all mappers. Provides utility methods for error handling and validation.
    """

    @staticmethod
    def _validate_component(component: PropellantComponent, name: str):
        """
        Validate that a component exists and has valid properties.

        Args:
            component (PropellantComponent): The component to validate.
            name (str): The name of the component.

        Raises:
            ValueError: If the component is invalid or missing.
        """
        if component.mass_fraction < 0 or component.mass_fraction > 1:
            raise ValueError(f"Invalid mass fraction for component '{name}': {component.mass_fraction}")
        if component.mass_fraction == 0:
            raise ValueError(f"Missing or zero mass fraction for component '{name}'")

class InterPocketRegionMapper(BaseMapper):
    """
    Maps mass fractions for the inter-pocket region (homogeneous mixture of all components).
    Mass fractions are numerically equal to the original propellant's mass fractions.

    Methods:
        calculate(propellant: Propellant) -> RegionData:
            Calculates the mass fractions for the inter-pocket region.
    """

    def calculate(self, propellant: Propellant) -> RegionData:
        """
        Calculate the mass fractions for the inter-pocket region.

        Args:
            propellant (Propellant): The propellant object containing all components.

        Returns:
            RegionData: A dataclass containing components and their normalized mass fractions.

        Raises:
            ValueError: If any required component is missing or invalid.
        """
        # Extract and validate components
        binder = propellant.components.get("CombustibleBinder")
        ap = propellant.components.get("AmmoniumPerchlorate")
        al = propellant.components.get("Aluminium")
        hmx = propellant.components.get("Octogen")

        self._validate_component(binder, "CombustibleBinder")
        self._validate_component(ap, "AmmoniumPerchlorate")
        self._validate_component(al, "Aluminium")
        self._validate_component(hmx, "Octogen")

        # Mass fractions are numerically equal to the original propellant's mass fractions
        components = {
            "CombustibleBinder": binder.mass_fraction,
            "AmmoniumPerchlorate": ap.mass_fraction,
            "Aluminium": al.mass_fraction,
            "Octogen": hmx.mass_fraction
        }

        return RegionData(components)

class PocketRegionWithoutSkeletonMapper(BaseMapper):
    """
    Maps mass fractions for the pocket region without skeleton (remaining homogeneous mixture).

    Methods:
        calculate(propellant: Propellant) -> RegionData:
            Calculates the mass fractions for the pocket region without skeleton.
    """

    def calculate(self, propellant: Propellant) -> RegionData:
        """
        Calculate the mass fractions for the pocket region without skeleton.

        Args:
            propellant (Propellant): The propellant object containing all components.

        Returns:
            RegionData: A dataclass containing components and their normalized mass fractions.

        Raises:
            ValueError: If any required component is missing or invalid.
        """
        # Extract and validate components
        binder = propellant.components.get("CombustibleBinder")
        ap = propellant.components.get("AmmoniumPerchlorate")
        al = propellant.components.get("Aluminium")

        self._validate_component(binder, "CombustibleBinder")
        self._validate_component(ap, "AmmoniumPerchlorate")
        self._validate_component(al, "Aluminium")

        # Assume total propellant mass is 1 kg
        propellant_mass = 1.0

        # Convert mass fractions to masses
        binder_mass = binder.mass_fraction * propellant_mass
        ap_mass = ap.mass_fraction * (1 - ap.large_particles_fraction) * propellant_mass
        al_mass = al.mass_fraction * propellant_mass

        # Calculate homogeneous mixture mass (exclude large particles AP and HMX)
        homogeneous_mixture_mass = (
            binder_mass +
            ap_mass +
            al_mass
        )

        # Validate mixture mass
        if homogeneous_mixture_mass <= 0:
            raise ValueError("Homogeneous mixture mass must be greater than zero.")

        # Calculate mass fractions relative to homogeneous mixture mass
        components = {
            "CombustibleBinder": binder_mass / homogeneous_mixture_mass,
            "AmmoniumPerchlorate": ap_mass / homogeneous_mixture_mass,
            "Aluminium": al_mass / homogeneous_mixture_mass
        }

        return RegionData(components)

class PocketRegionWithSkeletonMapper(BaseMapper):
    """
    Maps mass fractions for the pocket region with skeleton (binder and small fractions AP only).

    Methods:
        calculate(propellant: Propellant) -> RegionData:
            Calculates the mass fractions for the pocket region with skeleton.
    """

    def calculate(self, propellant: Propellant) -> RegionData:
        """
        Calculate the mass fractions for the pocket region with skeleton.

        Args:
            propellant (Propellant): The propellant object containing all components.

        Returns:
            RegionData: A dataclass containing components and their normalized mass fractions.

        Raises:
            ValueError: If any required component is missing or invalid.
        """
        # Extract and validate components
        binder = propellant.components.get("CombustibleBinder")
        ap = propellant.components.get("AmmoniumPerchlorate")

        self._validate_component(binder, "CombustibleBinder")
        self._validate_component(ap, "AmmoniumPerchlorate")

        # Assume total propellant mass is 1 kg
        propellant_mass = 1.0

        # Convert mass fractions to masses
        binder_mass = binder.mass_fraction * propellant_mass
        ap_mass = ap.mass_fraction * (1 - ap.large_particles_fraction) * propellant_mass

        # Calculate skeleton region mass (only binder and small fractions AP)
        skeleton_mass = binder_mass + ap_mass

        # Validate skeleton mass
        if skeleton_mass <= 0:
            raise ValueError("Skeleton region mass must be greater than zero.")

        # Calculate mass fractions relative to skeleton region mass
        components = {
            "CombustibleBinder": binder_mass / skeleton_mass,
            "AmmoniumPerchlorate": ap_mass / skeleton_mass
        }

        return RegionData(components)

class DiffusionRegionMapper(BaseMapper):
    """
    Maps mass fractions for the diffusion region (includes all components except agglomerated aluminium).

    Methods:
        calculate(propellant: Propellant, pressure: float) -> RegionData:
            Calculates the mass fractions for the diffusion region.
    """

    def calculate(self, propellant: Propellant, pressure: float) -> RegionData:
        """
        Calculate the mass fractions for the diffusion region.

        Args:
            propellant (Propellant): The propellant object containing all components.
            pressure (float): Pressure in Pascals.

        Returns:
            RegionData: A dataclass containing components and their normalized mass fractions.

        Raises:
            ValueError: If any required component is missing or invalid.
        """
        # Extract and validate components
        binder = propellant.components.get("CombustibleBinder")
        ap = propellant.components.get("AmmoniumPerchlorate")
        al = propellant.components.get("Aluminium")
        hmx = propellant.components.get("Octogen")

        self._validate_component(binder, "CombustibleBinder")
        self._validate_component(ap, "AmmoniumPerchlorate")
        self._validate_component(al, "Aluminium")
        self._validate_component(hmx, "Octogen")

        # Assume total propellant mass is 1 kg
        propellant_mass = 1.0

        # Convert mass fractions to masses
        binder_mass = binder.mass_fraction * propellant_mass
        ap_mass = ap.mass_fraction * propellant_mass
        hmx_mass = hmx.mass_fraction * propellant_mass

        # Calculate non-agglomerated aluminium mass
        al_non_agglomerated_mass = self._calculate_non_agglomerated_aluminium(al, pressure)

        # Total mass of the diffusion region
        diffusion_mass = binder_mass + ap_mass + hmx_mass + al_non_agglomerated_mass

        # Validate diffusion mass
        if diffusion_mass <= 0:
            raise ValueError("Diffusion region mass must be greater than zero.")

        # Calculate mass fractions relative to diffusion mass
        components = {
            "CombustibleBinder": binder_mass / diffusion_mass,
            "AmmoniumPerchlorate": ap_mass / diffusion_mass,
            "Octogen": hmx_mass / diffusion_mass,
            "Aluminium": al_non_agglomerated_mass / diffusion_mass
        }

        return RegionData(components)

    def _calculate_non_agglomerated_aluminium(self, al: PropellantComponent, pressure: float) -> float:
        """
        Calculate the mass of non-agglomerated aluminium.

        Args:
            al (PropellantComponent): Aluminium component.
            pressure (float): Pressure in Pascals.

        Returns:
            float: Mass of non-agglomerated aluminium.
        """
        agglomeration_fraction = self._calculate_agglomeration_fraction(al.agglomeration_coefficients, pressure)
        # print("Agglomeration fraction", agglomeration_fraction)
        return al.mass_fraction * (1 - agglomeration_fraction)

    def _calculate_agglomeration_fraction(self, coefficients: List[float], pressure: float) -> float:
        """
        Calculate agglomeration fraction using polynomial coefficients.

        Args:
            coefficients (List[float]): Polynomial coefficients.
            pressure (float): Pressure in Pascals.

        Returns:
            float: Agglomeration fraction.
        """
        return sum(coeff * (pressure / 1e6) ** i for i, coeff in enumerate(coefficients))
