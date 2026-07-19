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
    4. Diffusion region: Includes all components except agglomerated Aluminum.

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
        if component is None:
            raise ValueError(
                f"Missing component '{name}': it is not present in the propellant definition."
            )
        if component.mass_fraction is None:
            raise ValueError(f"Missing 'mass_fraction' for component '{name}'")
        if component.mass_fraction < 0 or component.mass_fraction > 1:
            raise ValueError(f"Invalid mass fraction for component '{name}': {component.mass_fraction}")
        if component.mass_fraction == 0:
            raise ValueError(f"Missing or zero mass fraction for component '{name}'")

    @staticmethod
    def _require_large_particles_fraction(component: PropellantComponent, name: str) -> float:
        """
        Return the component's large-particles fraction, failing loudly if it is absent.

        `large_particles_fraction` is optional on PropellantComponent, but the
        pocket-region mappers need it to split large from small particles. It is a
        measured physical quantity, so a missing value must NOT be defaulted to 0
        (that would silently claim "no large particles" and inflate the small-particle
        mass) — it is an error in the input data.

        Args:
            component (PropellantComponent): The component to read.
            name (str): The name of the component.

        Returns:
            float: The large-particles fraction, in [0, 1].

        Raises:
            ValueError: If the fraction is missing or outside [0, 1].
        """
        fraction = component.large_particles_fraction
        if fraction is None:
            raise ValueError(
                f"Missing 'large_particles_fraction' for component '{name}'. "
                f"It is required to split large from small particles and has no "
                f"safe default; add it to the propellant JSON."
            )
        if fraction < 0 or fraction > 1:
            raise ValueError(
                f"Invalid 'large_particles_fraction' for component '{name}': {fraction} "
                f"(expected a fraction in [0, 1])."
            )
        return fraction

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
        al = propellant.components.get("Aluminum")
        hmx = propellant.components.get("Octogen")

        self._validate_component(binder, "CombustibleBinder")
        self._validate_component(ap, "AmmoniumPerchlorate")
        self._validate_component(al, "Aluminum")
        self._validate_component(hmx, "Octogen")

        # Mass fractions are numerically equal to the original propellant's mass fractions
        components = {
            "CombustibleBinder": binder.mass_fraction,
            "AmmoniumPerchlorate": ap.mass_fraction,
            "Aluminum": al.mass_fraction,
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
        al = propellant.components.get("Aluminum")

        self._validate_component(binder, "CombustibleBinder")
        self._validate_component(ap, "AmmoniumPerchlorate")
        self._validate_component(al, "Aluminum")

        # Assume total propellant mass is 1 kg
        propellant_mass = 1.0

        # Convert mass fractions to masses
        ap_large_particles_fraction = self._require_large_particles_fraction(
            ap, "AmmoniumPerchlorate")

        binder_mass = binder.mass_fraction * propellant_mass
        ap_mass = ap.mass_fraction * (1 - ap_large_particles_fraction) * propellant_mass
        al_mass = al.mass_fraction * propellant_mass

        # Calculate homogeneous mixture mass (exclude large particles AP)
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
            "Aluminum": al_mass / homogeneous_mixture_mass
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
        ap_large_particles_fraction = self._require_large_particles_fraction(
            ap, "AmmoniumPerchlorate")

        binder_mass = binder.mass_fraction * propellant_mass
        ap_mass = ap.mass_fraction * (1 - ap_large_particles_fraction) * propellant_mass

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
    Maps mass fractions for the diffusion region (includes all components plus the
    non-agglomerated portion of Aluminum).

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
        al = propellant.components.get("Aluminum")
        hmx = propellant.components.get("Octogen")

        self._validate_component(binder, "CombustibleBinder")
        self._validate_component(ap, "AmmoniumPerchlorate")
        self._validate_component(al, "Aluminum")
        self._validate_component(hmx, "Octogen")

        # Assume total propellant mass is 1 kg
        propellant_mass = 1.0

        # Convert mass fractions to masses
        binder_mass = binder.mass_fraction * propellant_mass
        ap_mass = ap.mass_fraction * propellant_mass
        hmx_mass = hmx.mass_fraction * propellant_mass

        # Calculate non-agglomerated Aluminum mass
        al_non_agglomerated_mass = self._calculate_non_agglomerated_Aluminum(al, pressure)

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
            "Aluminum": al_non_agglomerated_mass / diffusion_mass
        }

        return RegionData(components)

    def _calculate_non_agglomerated_Aluminum(self, al: PropellantComponent, pressure: float) -> float:
        """
        Calculate the mass of non-agglomerated Aluminum.

        Args:
            al (PropellantComponent): Aluminum component.
            pressure (float): Pressure in Pascals.

        Returns:
            float: Mass of non-agglomerated Aluminum.

        Raises:
            ValueError: If the agglomeration coefficients are missing or empty.
        """
        if not al.agglomeration_coefficients:
            raise ValueError(
                "Missing 'agglomeration_coefficients' for component 'Aluminum'. "
                "They are required to evaluate the pressure-dependent agglomeration "
                "fraction and have no safe default; add them to the propellant JSON."
            )

        agglomeration_fraction = self._calculate_agglomeration_fraction(al.agglomeration_coefficients, pressure)
        return al.mass_fraction * (1 - agglomeration_fraction)

    def _calculate_agglomeration_fraction(self, coefficients: List[float], pressure: float) -> float:
        """
        Calculate agglomeration fraction using polynomial coefficients.

        The polynomial is evaluated in pressure expressed in MPa and returns a
        mass FRACTION in [0, 1] (not a percentage) — it is consumed by
        `_calculate_non_agglomerated_Aluminum` as `1 - fraction`, so the [0, 1]
        scale is load-bearing. For the shipped propellants the fit yields
        0.086..0.296 over the 1..6.5 MPa fitted range.

        No clamping is applied here, deliberately. PropellantsPlotRendering
        applies a `max(0, min(100, ...))` clamp for its plot axis; over the
        fitted 1..6.5 MPa range the two agree exactly for every shipped
        propellant, because the clamp never binds there.

        Caveat: the coefficients are a fit valid only over the fitted pressure
        range. Extrapolated well above it the polynomial can go negative
        (Bas_1 above ~11.5 MPa, Bas_3 above ~10.2 MPa), which would make
        `1 - fraction > 1` and yield more non-agglomerated Aluminum than there
        is Aluminum. `--pressure` is not bounded above, so callers are
        responsible for staying inside the fitted range.

        Args:
            coefficients (List[float]): Polynomial coefficients, ascending order.
            pressure (float): Pressure in Pascals.

        Returns:
            float: Agglomeration mass fraction (nominally [0, 1]).
        """
        return sum(coeff * (pressure / 1e6) ** i for i, coeff in enumerate(coefficients))
