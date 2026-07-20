"""Value tests for RegionMapper's four region mappers.

These mappers decide which mass ends up in which combustion region, so an error here
silently rescales every downstream elemental composition and enthalpy. Every
assertion below is on a hand-computed number, not on "it returned something".
"""

import pytest

# Mass fractions of the shipped Bas_* propellants (all five share these).
BINDER = 0.21
AP = 0.30
AL = 0.2073
HMX = 0.2827
AP_LARGE = 0.65

# Bas_0's Aluminum agglomeration polynomial, ascending order, argument in MPa.
BAS_0_COEFFS = [
    0.1895454545454542,
    -0.005582750582750498,
    -0.003205128205128215,
    0.00025641025641025706,
]


@pytest.fixture
def shipped_like(make_region_propellant):
    """A propellant with the shipped Bas_0 numbers."""
    return make_region_propellant(
        name="Bas_0",
        CombustibleBinder=(BINDER, None, None),
        AmmoniumPerchlorate=(AP, AP_LARGE, None),
        Aluminum=(AL, None, BAS_0_COEFFS),
        Octogen=(HMX, None, None),
    )


# --- InterPocketRegionMapper -------------------------------------------------------


def test_inter_pocket_returns_the_original_mass_fractions(region_mapper, shipped_like):
    result = region_mapper.region_mappers.InterPocketRegionMapper().calculate(shipped_like)

    assert result.components == {
        "CombustibleBinder": BINDER,
        "AmmoniumPerchlorate": AP,
        "Aluminum": AL,
        "Octogen": HMX,
    }
    assert sum(result.components.values()) == pytest.approx(1.0, abs=1e-12)


def test_inter_pocket_does_not_renormalise(region_mapper, make_region_propellant):
    """ACTUAL CURRENT BEHAVIOUR, not necessarily the intended one.

    The class docstring says the returned fractions are "normalized ... such that
    their sum equals 1.0", but the implementation copies the input fractions
    verbatim. For the shipped propellants the input already sums to 1, so the two
    readings coincide; for an input that does not, the output does not sum to 1
    either. Pinned here so the discrepancy is visible rather than assumed away.
    """
    propellant = make_region_propellant(
        CombustibleBinder=(0.2, None, None),
        AmmoniumPerchlorate=(0.2, 0.5, None),
        Aluminum=(0.2, None, [0.1]),
        Octogen=(0.2, None, None),
    )

    result = region_mapper.region_mappers.InterPocketRegionMapper().calculate(propellant)

    assert sum(result.components.values()) == pytest.approx(0.8)


@pytest.mark.parametrize(
    "missing",
    ["CombustibleBinder", "AmmoniumPerchlorate", "Aluminum", "Octogen"],
)
def test_inter_pocket_rejects_a_missing_component(
    region_mapper, make_region_propellant, missing
):
    components = {
        "CombustibleBinder": (BINDER, None, None),
        "AmmoniumPerchlorate": (AP, AP_LARGE, None),
        "Aluminum": (AL, None, BAS_0_COEFFS),
        "Octogen": (HMX, None, None),
    }
    del components[missing]
    propellant = make_region_propellant(**components)

    with pytest.raises(ValueError, match=f"Missing component '{missing}'"):
        region_mapper.region_mappers.InterPocketRegionMapper().calculate(propellant)


@pytest.mark.parametrize(
    "mass_fraction, message",
    [
        (-0.1, "Invalid mass fraction"),
        (1.5, "Invalid mass fraction"),
        (0.0, "Missing or zero mass fraction"),
        (None, "Missing 'mass_fraction'"),
    ],
)
def test_validation_rejects_out_of_range_mass_fractions(
    region_mapper, make_region_propellant, mass_fraction, message
):
    propellant = make_region_propellant(
        CombustibleBinder=(mass_fraction, None, None),
        AmmoniumPerchlorate=(AP, AP_LARGE, None),
        Aluminum=(AL, None, BAS_0_COEFFS),
        Octogen=(HMX, None, None),
    )

    with pytest.raises(ValueError, match=message):
        region_mapper.region_mappers.InterPocketRegionMapper().calculate(propellant)


# --- PocketRegionWithoutSkeletonMapper ---------------------------------------------


def test_pocket_without_skeleton_excludes_large_ap_particles(region_mapper, shipped_like):
    result = region_mapper.region_mappers.PocketRegionWithoutSkeletonMapper().calculate(
        shipped_like
    )

    small_ap = AP * (1 - AP_LARGE)  # 0.105
    total = BINDER + small_ap + AL  # 0.5223
    assert small_ap == pytest.approx(0.105)

    assert set(result.components) == {"CombustibleBinder", "AmmoniumPerchlorate", "Aluminum"}
    assert result.components["CombustibleBinder"] == pytest.approx(BINDER / total)
    assert result.components["AmmoniumPerchlorate"] == pytest.approx(small_ap / total)
    assert result.components["Aluminum"] == pytest.approx(AL / total)
    # Renormalised to the region, so it does sum to 1 here.
    assert sum(result.components.values()) == pytest.approx(1.0)
    # Pin the actual numbers, not just the ratios.
    assert result.components["CombustibleBinder"] == pytest.approx(0.4020677, abs=1e-7)
    assert result.components["AmmoniumPerchlorate"] == pytest.approx(0.2010339, abs=1e-7)
    assert result.components["Aluminum"] == pytest.approx(0.3968984, abs=1e-7)


def test_pocket_without_skeleton_keeps_all_aluminium(region_mapper, make_region_propellant):
    """Octogen is dropped from this region but Aluminum is kept whole.

    The pocket-without-skeleton region is defined as the homogeneous mixture minus
    the large AP particles; agglomeration does not enter here (it only enters the
    diffusion region), so the full Aluminum mass fraction participates.
    """
    propellant = make_region_propellant(
        CombustibleBinder=(0.5, None, None),
        AmmoniumPerchlorate=(0.2, 0.0, None),
        Aluminum=(0.2, None, [0.5]),
        Octogen=(0.1, None, None),
    )

    result = region_mapper.region_mappers.PocketRegionWithoutSkeletonMapper().calculate(
        propellant
    )

    assert "Octogen" not in result.components
    assert result.components["Aluminum"] == pytest.approx(0.2 / 0.9)


def test_pocket_without_skeleton_requires_large_particles_fraction(
    region_mapper, make_region_propellant
):
    propellant = make_region_propellant(
        CombustibleBinder=(BINDER, None, None),
        AmmoniumPerchlorate=(AP, None, None),  # no large_particles_fraction
        Aluminum=(AL, None, BAS_0_COEFFS),
        Octogen=(HMX, None, None),
    )

    with pytest.raises(ValueError, match="Missing 'large_particles_fraction'"):
        region_mapper.region_mappers.PocketRegionWithoutSkeletonMapper().calculate(propellant)


@pytest.mark.parametrize("bad", [-0.01, 1.01])
def test_pocket_without_skeleton_rejects_out_of_range_large_fraction(
    region_mapper, make_region_propellant, bad
):
    propellant = make_region_propellant(
        CombustibleBinder=(BINDER, None, None),
        AmmoniumPerchlorate=(AP, bad, None),
        Aluminum=(AL, None, BAS_0_COEFFS),
        Octogen=(HMX, None, None),
    )

    with pytest.raises(ValueError, match="Invalid 'large_particles_fraction'"):
        region_mapper.region_mappers.PocketRegionWithoutSkeletonMapper().calculate(propellant)


# --- PocketRegionWithSkeletonMapper ------------------------------------------------


def test_pocket_with_skeleton_is_binder_and_small_ap_only(region_mapper, shipped_like):
    result = region_mapper.region_mappers.PocketRegionWithSkeletonMapper().calculate(
        shipped_like
    )

    small_ap = AP * (1 - AP_LARGE)  # 0.105
    total = BINDER + small_ap  # 0.315

    assert set(result.components) == {"CombustibleBinder", "AmmoniumPerchlorate"}
    assert result.components["CombustibleBinder"] == pytest.approx(BINDER / total)
    assert result.components["AmmoniumPerchlorate"] == pytest.approx(small_ap / total)
    # Binder:small-AP is exactly 2:1 for the shipped numbers.
    assert result.components["CombustibleBinder"] == pytest.approx(2.0 / 3.0)
    assert result.components["AmmoniumPerchlorate"] == pytest.approx(1.0 / 3.0)


def test_pocket_with_skeleton_with_all_ap_large_is_pure_binder(
    region_mapper, make_region_propellant
):
    """Bas_4 ships large_particles_fraction == 1, so no AP reaches the skeleton."""
    propellant = make_region_propellant(
        name="Bas_4",
        CombustibleBinder=(BINDER, None, None),
        AmmoniumPerchlorate=(AP, 1.0, None),
        Aluminum=(AL, None, BAS_0_COEFFS),
        Octogen=(HMX, None, None),
    )

    result = region_mapper.region_mappers.PocketRegionWithSkeletonMapper().calculate(
        propellant
    )

    assert result.components["CombustibleBinder"] == pytest.approx(1.0)
    assert result.components["AmmoniumPerchlorate"] == pytest.approx(0.0)


def test_pocket_with_skeleton_with_no_large_ap_uses_all_ap(
    region_mapper, make_region_propellant
):
    """Bas_3 ships large_particles_fraction == 0, so all AP is 'small'."""
    propellant = make_region_propellant(
        name="Bas_3",
        CombustibleBinder=(BINDER, None, None),
        AmmoniumPerchlorate=(AP, 0.0, None),
        Aluminum=(AL, None, BAS_0_COEFFS),
        Octogen=(HMX, None, None),
    )

    result = region_mapper.region_mappers.PocketRegionWithSkeletonMapper().calculate(
        propellant
    )

    assert result.components["CombustibleBinder"] == pytest.approx(BINDER / (BINDER + AP))
    assert result.components["AmmoniumPerchlorate"] == pytest.approx(AP / (BINDER + AP))


# --- DiffusionRegionMapper and the agglomeration polynomial ------------------------


def test_agglomeration_polynomial_argument_is_megapascals(region_mapper):
    """The polynomial is evaluated in MPa even though the argument is in Pa.

    This is the single easiest thing in the tree to get wrong (recorded in the
    tech-debt register), so pin it with a coefficient set whose value IS the
    argument: [0, 1] evaluates to x, so a 6.5 MPa input must give 6.5, not 6.5e6.
    """
    calculate = region_mapper.region_mappers.DiffusionRegionMapper()._calculate_agglomeration_fraction

    assert calculate([0.0, 1.0], 6.5e6) == pytest.approx(6.5)
    assert calculate([0.0, 1.0], 1.0e6) == pytest.approx(1.0)
    # Quadratic term confirms the scaling is applied before the power, not after.
    assert calculate([0.0, 0.0, 1.0], 3.0e6) == pytest.approx(9.0)


def test_agglomeration_polynomial_is_ascending_order(region_mapper):
    calculate = region_mapper.region_mappers.DiffusionRegionMapper()._calculate_agglomeration_fraction

    # 5 + 3x + 2x^2 at x = 2 MPa -> 5 + 6 + 8 = 19
    assert calculate([5.0, 3.0, 2.0], 2.0e6) == pytest.approx(19.0)


def test_agglomeration_polynomial_pins_shipped_bas_0_values(region_mapper):
    calculate = region_mapper.region_mappers.DiffusionRegionMapper()._calculate_agglomeration_fraction

    assert calculate(BAS_0_COEFFS, 1.0e6) == pytest.approx(0.18101398601398574, rel=1e-12)
    assert calculate(BAS_0_COEFFS, 6.5e6) == pytest.approx(0.08825757575757576, rel=1e-12)
    # It is a mass FRACTION in [0, 1] over the fitted range, not a percentage.
    assert 0.0 < calculate(BAS_0_COEFFS, 3.0e6) < 1.0


def test_agglomeration_polynomial_is_unclamped_when_extrapolated(region_mapper):
    """ACTUAL CURRENT BEHAVIOUR. RegionMapper deliberately does not clamp.

    Bas_3's fit goes negative above roughly 10.2 MPa. Extrapolated there the
    polynomial returns a negative fraction, which makes `1 - fraction > 1` and
    yields more non-agglomerated Aluminum than there is Aluminum. This is
    documented in the source docstring as a caller responsibility, and is pinned
    here so a future clamp is a conscious change rather than an accident.
    """
    calculate = region_mapper.region_mappers.DiffusionRegionMapper()._calculate_agglomeration_fraction
    bas_3 = [
        0.18985236987285337,
        -0.010617974640070237,
        -0.0008857808796132974,
        1.0360009859915167e-05,
    ]

    assert calculate(bas_3, 6.5e6) > 0.0
    assert calculate(bas_3, 12.0e6) < 0.0


def test_diffusion_region_excludes_agglomerated_aluminium(region_mapper, shipped_like):
    result = region_mapper.region_mappers.DiffusionRegionMapper().calculate(
        shipped_like, 1.0e6
    )

    agglomerated = 0.18101398601398574  # Bas_0 at 1 MPa
    al_free = AL * (1 - agglomerated)
    total = BINDER + AP + HMX + al_free

    assert set(result.components) == {
        "CombustibleBinder",
        "AmmoniumPerchlorate",
        "Octogen",
        "Aluminum",
    }
    assert result.components["Aluminum"] == pytest.approx(al_free / total)
    assert result.components["CombustibleBinder"] == pytest.approx(BINDER / total)
    assert result.components["AmmoniumPerchlorate"] == pytest.approx(AP / total)
    assert result.components["Octogen"] == pytest.approx(HMX / total)
    assert sum(result.components.values()) == pytest.approx(1.0)


def test_diffusion_region_aluminium_share_falls_as_pressure_rises(
    region_mapper, make_region_propellant
):
    """A rising agglomeration fraction must leave less free Aluminum, not more.

    Uses a monotonically increasing synthetic fit so the direction of the
    `1 - fraction` term is asserted independently of any shipped coefficients.
    """
    propellant = make_region_propellant(
        CombustibleBinder=(BINDER, None, None),
        AmmoniumPerchlorate=(AP, AP_LARGE, None),
        Aluminum=(AL, None, [0.0, 0.1]),  # fraction = 0.1 * p[MPa]
        Octogen=(HMX, None, None),
    )
    mapper = region_mapper.region_mappers.DiffusionRegionMapper()

    low = mapper.calculate(propellant, 1.0e6).components["Aluminum"]
    high = mapper.calculate(propellant, 5.0e6).components["Aluminum"]

    assert low > high
    # fraction(1 MPa) = 0.1, fraction(5 MPa) = 0.5
    assert low == pytest.approx((AL * 0.9) / (BINDER + AP + HMX + AL * 0.9))
    assert high == pytest.approx((AL * 0.5) / (BINDER + AP + HMX + AL * 0.5))


@pytest.mark.parametrize("coeffs", [None, []])
def test_diffusion_region_requires_agglomeration_coefficients(
    region_mapper, make_region_propellant, coeffs
):
    propellant = make_region_propellant(
        CombustibleBinder=(BINDER, None, None),
        AmmoniumPerchlorate=(AP, AP_LARGE, None),
        Aluminum=(AL, None, coeffs),
        Octogen=(HMX, None, None),
    )

    with pytest.raises(ValueError, match="Missing 'agglomeration_coefficients'"):
        region_mapper.region_mappers.DiffusionRegionMapper().calculate(propellant, 1.0e6)


def test_region_data_is_immutable(region_mapper, shipped_like):
    result = region_mapper.region_mappers.InterPocketRegionMapper().calculate(shipped_like)

    with pytest.raises(Exception):
        result.components = {}
