"""Value tests for PorosityCalculation's numerical kernel.

`calculate_porosity` converts a region's elemental composition (mol/kg) into
condensed-phase volumes and reports the void fraction left over. The two constants
it leans on (carbon density, carbon retention) and the mol -> kg -> m^3 chain are
exactly the kind of thing that fails silently, so each step is pinned.
"""

import pytest

M_C = 0.012011  # kg/mol
M_AL = 0.0269815385  # kg/mol


# --- Module constants ----------------------------------------------------------------


def test_constants_are_the_documented_values(porosity):
    assert porosity.calculators.CARBON_DENSITY == 2267  # kg/m^3
    assert porosity.calculators.CARBON_RETENTION == 0.1  # 10% of carbon retained


def test_the_rejected_aluminium_temperature_factor_stays_disabled(porosity):
    """Tech-debt NEW-2: calculators.py:8 keeps a commented-out constant on purpose.

    `ALUMINUM_TEMPERATURE_FACTOR = 0.6` records a REJECTED physical assumption (a
    40% density reduction at 2300 K). The comment must stay as documentation, but
    the constant must stay inactive -- if it is ever uncommented, every porosity
    number shifts. This asserts it is not live.
    """
    assert not hasattr(porosity.calculators, "ALUMINUM_TEMPERATURE_FACTOR")


# --- calculate_region_density ---------------------------------------------------------


def test_region_density_is_the_volume_weighted_harmonic_mean(
    porosity, make_porosity_propellant
):
    propellant = make_porosity_propellant(
        CombustibleBinder=(0.5, 1000.0),
        Aluminum=(0.5, 2500.0),
    )

    density = porosity.calculators.calculate_region_density(propellant)

    # 1 / (0.5/1000 + 0.5/2500) = 1 / 0.0007
    assert density == pytest.approx(1428.5714285714287, rel=1e-12)
    # Strictly between the component densities, as a mixture density must be.
    assert 1000.0 < density < 2500.0


def test_region_density_of_a_single_component_is_that_component_density(
    porosity, make_porosity_propellant
):
    propellant = make_porosity_propellant(Aluminum=(1.0, 2700.0))

    assert porosity.calculators.calculate_region_density(propellant) == pytest.approx(2700.0)


def test_region_density_reproduces_the_shipped_bulk_density(
    porosity, shipped_propellants_path
):
    """The shipped Bas_* mass fractions and component densities imply ~1659 kg/m^3,
    which is the `density` field those same records carry -- so the input file is
    internally consistent and the formula matches the one that produced it."""
    propellants = porosity.json_reader.load_propellants(str(shipped_propellants_path))

    for propellant in propellants:
        computed = porosity.calculators.calculate_region_density(propellant)
        assert computed == pytest.approx(propellant.density, rel=2e-4)
        assert computed == pytest.approx(1659.212917, abs=1e-5)


def test_region_density_returns_zero_for_a_zero_volume_mixture(
    porosity, make_porosity_propellant
):
    """ACTUAL CURRENT BEHAVIOUR, and an inconsistency worth knowing about.

    With every mass fraction zero the total volume is zero and this returns 0.0
    rather than raising -- a physically meaningless density that propagates into
    the output JSON. `calculate_porosity` raises ValueError for the very same
    condition. Pinned, not fixed.
    """
    propellant = make_porosity_propellant(
        CombustibleBinder=(0.0, 1000.0),
        Aluminum=(0.0, 2700.0),
    )

    assert porosity.calculators.calculate_region_density(propellant) == 0.0


def test_region_density_of_an_empty_component_set_is_zero(
    porosity, make_porosity_propellant
):
    assert porosity.calculators.calculate_region_density(make_porosity_propellant()) == 0.0


# --- calculate_porosity ---------------------------------------------------------------


@pytest.fixture
def simple_propellant(make_porosity_propellant):
    """Round numbers: total component volume is exactly 7e-4 m^3/kg."""
    return make_porosity_propellant(
        name="Simple",
        CombustibleBinder=(0.5, 1000.0),
        Aluminum=(0.5, 2500.0),
    )


@pytest.fixture
def simple_region(porosity):
    return porosity.models.RegionCalculationResult(
        pressure=1.0e6, enthalpy=-1000.0, composition={"C": 10.0, "Al": 5.0}
    )


def test_porosity_value_is_one_minus_the_condensed_volume_share(
    porosity, simple_propellant, simple_region
):
    result = porosity.calculators.calculate_porosity(1428.57, simple_propellant, simple_region)

    volume_c = (0.1 * 10.0 * M_C) / 2267.0
    volume_al = (5.0 * M_AL) / 2500.0
    total_volume = 0.5 / 1000.0 + 0.5 / 2500.0

    assert result.porosity == pytest.approx(1 - (volume_c + volume_al) / total_volume, rel=1e-12)
    assert result.porosity == pytest.approx(0.9153410450822358, rel=1e-12)


def test_porosity_applies_the_ten_percent_carbon_retention(
    porosity, simple_propellant, simple_region
):
    """Only a tenth of the carbon occupies volume; without the factor the carbon
    contribution -- and therefore the porosity deficit -- would be ten times larger."""
    with_carbon = porosity.calculators.calculate_porosity(
        0.0, simple_propellant, simple_region
    ).porosity
    without_carbon = porosity.calculators.calculate_porosity(
        0.0,
        simple_propellant,
        porosity.models.RegionCalculationResult(
            pressure=1.0e6, enthalpy=-1000.0, composition={"C": 0.0, "Al": 5.0}
        ),
    ).porosity

    carbon_share = without_carbon - with_carbon
    total_volume = 0.5 / 1000.0 + 0.5 / 2500.0
    assert carbon_share == pytest.approx(((0.1 * 10.0 * M_C) / 2267.0) / total_volume, rel=1e-12)
    # A tenth of what the un-retained carbon would occupy.
    assert carbon_share == pytest.approx(0.1 * ((10.0 * M_C) / 2267.0) / total_volume, rel=1e-12)


def test_porosity_uses_the_aluminium_component_density_for_the_metal_volume(
    porosity, make_porosity_propellant
):
    """Doubling the Aluminum density must halve the metal's volume deficit exactly.

    The Aluminum mass fraction is set to zero so the propellant's own total volume
    is held fixed at 1e-3 m^3/kg; the only thing the density change can move is the
    `mass_al / aluminum.density` term. The metal in the region comes from the region
    composition (5 mol/kg of Al), not from the propellant's mass fractions.
    """
    region = porosity.models.RegionCalculationResult(
        pressure=1.0e6, enthalpy=0.0, composition={"C": 0.0, "Al": 5.0}
    )

    def porosity_at(al_density):
        propellant = make_porosity_propellant(
            CombustibleBinder=(1.0, 1000.0), Aluminum=(0.0, al_density)
        )
        return porosity.calculators.calculate_porosity(0.0, propellant, region).porosity

    light = porosity_at(2500.0)
    dense = porosity_at(5000.0)

    total_volume = 1.0 / 1000.0
    assert light == pytest.approx(1 - (5.0 * M_AL / 2500.0) / total_volume, rel=1e-12)
    assert dense == pytest.approx(1 - (5.0 * M_AL / 5000.0) / total_volume, rel=1e-12)
    assert (1 - dense) == pytest.approx((1 - light) / 2.0, rel=1e-12)


def test_porosity_of_a_region_with_no_condensed_species_is_one(
    porosity, simple_propellant
):
    empty = porosity.models.RegionCalculationResult(
        pressure=1.0e6, enthalpy=0.0, composition={}
    )

    result = porosity.calculators.calculate_porosity(0.0, simple_propellant, empty)

    assert result.porosity == pytest.approx(1.0)


def test_porosity_treats_absent_carbon_and_aluminium_keys_as_zero(
    porosity, simple_propellant
):
    """Missing composition keys default to 0 rather than raising."""
    carbon_only = porosity.models.RegionCalculationResult(
        pressure=1.0e6, enthalpy=0.0, composition={"C": 10.0}
    )

    result = porosity.calculators.calculate_porosity(0.0, simple_propellant, carbon_only)

    total_volume = 0.5 / 1000.0 + 0.5 / 2500.0
    assert result.porosity == pytest.approx(
        1 - ((0.1 * 10.0 * M_C) / 2267.0) / total_volume, rel=1e-12
    )


def test_porosity_can_go_negative_when_condensed_volume_exceeds_the_region(
    porosity, simple_propellant
):
    """ACTUAL CURRENT BEHAVIOUR: no clamp to [0, 1].

    An elemental composition claiming more Aluminum than the propellant's volume can
    hold yields a negative porosity rather than an error. Pinned so any future clamp
    is a deliberate change.
    """
    overloaded = porosity.models.RegionCalculationResult(
        pressure=1.0e6, enthalpy=0.0, composition={"C": 0.0, "Al": 500.0}
    )

    result = porosity.calculators.calculate_porosity(0.0, simple_propellant, overloaded)

    assert result.porosity < 0.0


def test_porosity_carries_the_region_density_and_input_through_unchanged(
    porosity, simple_propellant, simple_region
):
    result = porosity.calculators.calculate_porosity(1234.5, simple_propellant, simple_region)

    assert result.region_density == 1234.5
    assert result.region_input is simple_region


def test_porosity_requires_an_aluminium_component(porosity, make_porosity_propellant):
    propellant = make_porosity_propellant(
        name="NoMetal", CombustibleBinder=(1.0, 1000.0)
    )
    region = porosity.models.RegionCalculationResult(
        pressure=1.0e6, enthalpy=0.0, composition={"Al": 1.0}
    )

    with pytest.raises(KeyError) as excinfo:
        porosity.calculators.calculate_porosity(0.0, propellant, region)

    assert "NoMetal" in str(excinfo.value)
    assert "Aluminum" in str(excinfo.value)


@pytest.mark.parametrize("al_density", [0.0, None])
def test_porosity_rejects_a_missing_aluminium_density(
    porosity, make_porosity_propellant, simple_region, al_density
):
    propellant = make_porosity_propellant(
        name="BadMetal",
        CombustibleBinder=(0.5, 1000.0),
        Aluminum=(0.5, al_density),
    )

    with pytest.raises(ValueError, match="missing or zero density"):
        porosity.calculators.calculate_porosity(0.0, propellant, simple_region)


def test_porosity_rejects_a_zero_total_component_volume(
    porosity, make_porosity_propellant, simple_region
):
    propellant = make_porosity_propellant(
        name="Empty",
        CombustibleBinder=(0.0, 1000.0),
        Aluminum=(0.0, 2700.0),
    )

    with pytest.raises(ValueError, match="non-positive total component volume"):
        porosity.calculators.calculate_porosity(0.0, propellant, simple_region)


def test_porosity_over_the_shipped_propellants_is_physical(
    porosity, shipped_propellants_path
):
    """A realistic diffusion-region composition over every shipped propellant must
    land in (0, 1). Composition amounts are in mol/kg, as RegionMapper emits."""
    propellants = porosity.json_reader.load_propellants(str(shipped_propellants_path))
    region = porosity.models.RegionCalculationResult(
        pressure=1.0e6,
        enthalpy=-1.0e6,
        composition={"C": 2.5, "H": 20.0, "O": 15.0, "N": 8.0, "Cl": 2.5, "Al": 6.0},
    )

    for propellant in propellants:
        result = porosity.calculators.calculate_porosity(
            porosity.calculators.calculate_region_density(propellant), propellant, region
        )
        assert 0.0 < result.porosity < 1.0


def test_porosity_result_is_immutable(porosity, simple_propellant, simple_region):
    result = porosity.calculators.calculate_porosity(0.0, simple_propellant, simple_region)

    with pytest.raises(Exception):
        result.porosity = 0.0


# --- main.py helper -------------------------------------------------------------------


def test_output_path_is_a_sibling_of_the_region_file(porosity):
    assert porosity.main.get_output_path("/out/Bas_0/diffusion.json") == "/out/Bas_0/porosity.json"
    assert porosity.main.get_output_path("diffusion.json") == "porosity.json"
