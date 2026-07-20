"""Value tests for RegionMapper's stoichiometry helpers (utils.py).

`calculate_elemental_composition` turns mass fractions into per-kilogram element
amounts; it is the step where a wrong molar mass or a missing division silently
rescales the whole thermodynamic input. All expectations are computed by hand from
the molar masses in molar_masses.py.
"""

import math

import pytest

# Molar masses (kg/mol) as shipped in molar_masses.py.
M_H = 0.00100784
M_C = 0.012011
M_N = 0.014007
M_O = 0.015999
M_AL = 0.0269815385
M_CL = 0.03545


# --- compute_molar_mass ------------------------------------------------------------


def test_compute_molar_mass_of_water(region_mapper):
    mass = region_mapper.utils.compute_molar_mass({"H": 2, "O": 1})

    assert mass == pytest.approx(2 * M_H + M_O, rel=1e-12)
    # kg/mol, not g/mol: water is 0.018 kg/mol.
    assert mass == pytest.approx(0.01801468, abs=1e-8)


def test_compute_molar_mass_of_ammonium_perchlorate(region_mapper):
    """NH4ClO4 as shipped in propellant_components.json: H4 O4 N1 Cl1."""
    mass = region_mapper.utils.compute_molar_mass({"H": 4, "O": 4, "N": 1, "Cl": 1})

    assert mass == pytest.approx(4 * M_H + 4 * M_O + M_N + M_CL, rel=1e-12)
    assert mass == pytest.approx(0.11749, abs=1e-4)


def test_compute_molar_mass_accepts_fractional_counts(region_mapper):
    """The shipped binder composition uses non-integer counts (C 23.85, H 71.18...)."""
    mass = region_mapper.utils.compute_molar_mass({"C": 23.85, "H": 71.18})

    assert mass == pytest.approx(23.85 * M_C + 71.18 * M_H, rel=1e-12)


def test_compute_molar_mass_of_empty_composition_is_zero(region_mapper):
    assert region_mapper.utils.compute_molar_mass({}) == 0.0


def test_compute_molar_mass_rejects_unknown_element(region_mapper):
    with pytest.raises(KeyError, match="Element 'Xx' not found"):
        region_mapper.utils.compute_molar_mass({"Xx": 1})


# --- calculate_elemental_composition ------------------------------------------------


def test_elemental_composition_of_a_single_pure_component(region_mapper):
    """One kilogram of pure Aluminum is 1/M_Al moles of Al -- about 37.06 mol/kg."""
    components = {
        "Aluminum": region_mapper.models.Component(
            name="Aluminum", composition={"Al": 1}, enthalpy=0.0
        )
    }

    result = region_mapper.utils.calculate_elemental_composition({"Aluminum": 1.0}, components)

    assert result == {"Al": pytest.approx(1.0 / M_AL, rel=1e-12)}
    assert result["Al"] == pytest.approx(37.0623, abs=1e-4)


def test_elemental_composition_scales_linearly_with_mass_fraction(region_mapper):
    components = {
        "Aluminum": region_mapper.models.Component(
            name="Aluminum", composition={"Al": 1}, enthalpy=0.0
        )
    }
    utils = region_mapper.utils

    full = utils.calculate_elemental_composition({"Aluminum": 1.0}, components)
    half = utils.calculate_elemental_composition({"Aluminum": 0.5}, components)

    assert half["Al"] == pytest.approx(full["Al"] / 2.0, rel=1e-12)


def test_elemental_composition_sums_shared_elements_across_components(region_mapper):
    """Water and methane both contribute H; the contributions must add, not overwrite."""
    models = region_mapper.models
    components = {
        "Water": models.Component(name="Water", composition={"H": 2, "O": 1}, enthalpy=0.0),
        "Methane": models.Component(name="Methane", composition={"C": 1, "H": 4}, enthalpy=0.0),
    }

    result = region_mapper.utils.calculate_elemental_composition(
        {"Water": 0.5, "Methane": 0.5}, components
    )

    m_water = 2 * M_H + M_O
    m_methane = M_C + 4 * M_H
    assert result["H"] == pytest.approx((2 / m_water) * 0.5 + (4 / m_methane) * 0.5, rel=1e-12)
    assert result["O"] == pytest.approx((1 / m_water) * 0.5, rel=1e-12)
    assert result["C"] == pytest.approx((1 / m_methane) * 0.5, rel=1e-12)


def test_elemental_composition_is_mass_conserving(region_mapper):
    """Multiplying each element amount back by its molar mass recovers the mass.

    This is the invariant that makes the (count / molar_mass) * mass_fraction
    expression correct: sum over elements of n_i * M_i must equal the total mass
    fraction of the region.
    """
    models = region_mapper.models
    components = {
        "Water": models.Component(name="Water", composition={"H": 2, "O": 1}, enthalpy=0.0),
        "Alumina": models.Component(name="Alumina", composition={"Al": 2, "O": 3}, enthalpy=0.0),
    }
    molar = {"H": M_H, "O": M_O, "Al": M_AL}

    result = region_mapper.utils.calculate_elemental_composition(
        {"Water": 0.3, "Alumina": 0.7}, components
    )

    recovered = sum(amount * molar[element] for element, amount in result.items())
    assert recovered == pytest.approx(1.0, rel=1e-12)


def test_elemental_composition_raises_for_a_component_with_no_definition(region_mapper):
    with pytest.raises(KeyError):
        region_mapper.utils.calculate_elemental_composition({"Unobtainium": 1.0}, {})


# --- normalize_elemental_composition ------------------------------------------------


def test_normalisation_defaults_to_the_identity(region_mapper):
    """The default molar mass is 1.0, so RegionCalculator's call is a no-op.

    Pinned because the name suggests the values are rescaled to sum to 1, and they
    are not: `RegionCalculator.calculate` calls this without a molar mass, so the
    'normalized' composition it reports is numerically the raw one.
    """
    elements = {"H": 1.5, "O": 2.5}

    assert region_mapper.utils.normalize_elemental_composition(elements) == elements


def test_normalisation_multiplies_by_the_supplied_molar_mass(region_mapper):
    result = region_mapper.utils.normalize_elemental_composition({"H": 1.5, "O": 2.5}, 0.02)

    assert result == {"H": pytest.approx(0.03), "O": pytest.approx(0.05)}


# --- calculate_enthalpy --------------------------------------------------------------


def test_enthalpy_is_the_mass_weighted_sum(region_mapper):
    models = region_mapper.models
    components = {
        "CombustibleBinder": models.Component(
            name="CombustibleBinder", composition={"C": 1}, enthalpy=-2543930.0
        ),
        "Octogen": models.Component(name="Octogen", composition={"C": 4}, enthalpy=313000.0),
    }

    enthalpy = region_mapper.utils.calculate_enthalpy(
        {"CombustibleBinder": 0.25, "Octogen": 0.75}, components
    )

    assert enthalpy == pytest.approx(0.25 * -2543930.0 + 0.75 * 313000.0, rel=1e-12)
    assert enthalpy == pytest.approx(-401232.5, rel=1e-12)


def test_enthalpy_of_an_empty_region_is_zero(region_mapper):
    assert region_mapper.utils.calculate_enthalpy({}, {}) == 0.0


def test_enthalpy_sign_is_preserved(region_mapper):
    """Formation enthalpies are negative for the binder and AP; no abs() anywhere."""
    models = region_mapper.models
    components = {
        "AmmoniumPerchlorate": models.Component(
            name="AmmoniumPerchlorate", composition={"N": 1}, enthalpy=-2512000.0
        )
    }

    enthalpy = region_mapper.utils.calculate_enthalpy({"AmmoniumPerchlorate": 1.0}, components)

    assert enthalpy == pytest.approx(-2512000.0)
    assert math.copysign(1, enthalpy) == -1


# --- RegionCalculator (the thin orchestrator over the above) -------------------------


def test_region_calculator_passes_pressure_through_untouched(region_mapper):
    """No unit conversion happens here: the pressure in equals the pressure out."""
    models = region_mapper.models
    components = {
        "Aluminum": models.Component(name="Aluminum", composition={"Al": 1}, enthalpy=0.0)
    }
    region_data = region_mapper.region_mappers.RegionData({"Aluminum": 1.0})

    result = region_mapper.calculators.RegionCalculator.calculate(
        region_data, components, 6.5e6
    )

    assert result.pressure == 6.5e6
    assert result.enthalpy == pytest.approx(0.0)
    assert result.composition["Al"] == pytest.approx(1.0 / M_AL, rel=1e-12)


def test_region_calculator_matches_the_shipped_binder_definition(
    region_mapper, shipped_components_path
):
    """End-to-end over the real component file, still fully deterministic."""
    components = region_mapper.json_reader.read_components(str(shipped_components_path))
    region_data = region_mapper.region_mappers.RegionData({"CombustibleBinder": 1.0})

    result = region_mapper.calculators.RegionCalculator.calculate(
        region_data, components, 1.0e6
    )

    binder = components["CombustibleBinder"]
    molar_mass = region_mapper.utils.compute_molar_mass(binder.composition)
    assert result.enthalpy == pytest.approx(-2543930.0)
    assert result.composition["C"] == pytest.approx(23.85 / molar_mass, rel=1e-12)
    assert result.composition["Cl"] == pytest.approx(4.06 / molar_mass, rel=1e-12)
