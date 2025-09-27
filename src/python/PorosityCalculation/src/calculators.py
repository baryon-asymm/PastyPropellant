from dataclasses import dataclass

from molar_masses import ELEMENT_MOLAR_MASSES
from models import RegionCalculationResult, Propellant

CARBON_DENSITY = 2267 # kg/m^3
CARBON_RETENTION = 0.1 # 10% of carbon is retained in the region
ALUMINUM_TEMPERATURE_FACTOR = 0.6 # 40% density reduction at 2300 K

@dataclass(frozen=True)
class PorosityCalculationResult:
    """Holds porosity calculation results"""
    region_density: float
    porosity: float
    region_input: RegionCalculationResult

def calculate_region_density(propellant: Propellant) -> float:
    """Calculates region density using component volume fractions"""
    total_volume = sum(
        comp.mass_fraction / comp.density
        for comp in propellant.components.values()
    )
    return 1 / total_volume if total_volume else 0.0

def calculate_porosity(
    region_density: float,
    propellant: Propellant,
    region_result: RegionCalculationResult
) -> PorosityCalculationResult:
    """Calculates porosity based on chemical composition"""
    # Element mass calculations using molar masses
    mass_c = CARBON_RETENTION * region_result.composition.get('C', 0) * ELEMENT_MOLAR_MASSES['C']
    mass_al = region_result.composition.get('Al', 0) * ELEMENT_MOLAR_MASSES['Al']
    
    # Volume calculations
    volume_c = mass_c / CARBON_DENSITY
    volume_al = mass_al / propellant.components['Aluminum'].density
    
    # Total volume from propellant components
    total_volume = sum(
        comp.mass_fraction / 
        comp.density
        for comp in propellant.components.values()
    )
    
    porosity = 1 - (volume_c + volume_al) / total_volume
    return PorosityCalculationResult(region_density, porosity, region_result)
