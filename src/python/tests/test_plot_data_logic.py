"""Tests for the pure (non-rendering) logic inside PropellantsPlotRendering.

Nothing here draws anything: only the agglomeration polynomial, the pressure unit
conversion, the nested-JSON accessors and the output-path resolution are exercised.
Figure rendering is deliberately out of scope -- it needs a display-free backend
and asserting on pixels would be a change-detector, not a correctness test.

The most valuable test in this file is the cross-package one: the plot script and
RegionMapper carry two independent copies of the same polynomial, and the plot
copy's docstring claims they agree over the fitted range. That claim is checked
here against the real shipped coefficients.
"""

import pytest

FITTED_PRESSURES_PA = [1.0e6 + i * (5.5e6 / 9) for i in range(10)]  # 1 .. 6.5 MPa


# --- Agglomeration polynomial: units and clamping -------------------------------------


def test_plot_agglomeration_polynomial_argument_is_megapascals(plotting):
    calculate = plotting.main._calculate_agglomeration_fraction

    # coefficients [0, 1] make the result equal the argument in MPa.
    assert calculate([0.0, 1.0], 6.5e6) == pytest.approx(6.5)
    assert calculate([0.0, 0.0, 1.0], 3.0e6) == pytest.approx(9.0)


def test_plot_agglomeration_clamps_negatives_to_zero(plotting):
    """The `max(0, ...)` lower bound is the meaningful half of the clamp."""
    assert plotting.main._calculate_agglomeration_fraction([-1.0], 1.0e6) == 0.0
    assert plotting.main._calculate_agglomeration_fraction([1.0, -1.0], 5.0e6) == 0.0


def test_plot_agglomeration_upper_clamp_is_on_the_percentage_scale(plotting):
    """ACTUAL CURRENT BEHAVIOUR, deliberately kept (see the source docstring).

    The quantity is a mass FRACTION in [0, 1], so a `min(100, ...)` upper bound is
    on the wrong scale and is inert for any real fit. It is retained only so the
    published plots stay bit-identical. Pinned at 100, not 1, to record that.
    """
    assert plotting.main._calculate_agglomeration_fraction([150.0], 1.0e6) == 100.0
    # A fraction of 2.0 -- physically impossible -- passes through unclamped.
    assert plotting.main._calculate_agglomeration_fraction([2.0], 1.0e6) == pytest.approx(2.0)


def test_plot_and_region_mapper_polynomials_agree_over_the_fitted_range(
    plotting, region_mapper, shipped_propellants
):
    """The two independent copies must give identical numbers over 1..6.5 MPa.

    This is the contract the plot script's docstring asserts; it holds only because
    neither clamp binds inside the fitted range. If one copy is edited without the
    other, this fails.
    """
    plot_fn = plotting.main._calculate_agglomeration_fraction
    mapper_fn = region_mapper.region_mappers.DiffusionRegionMapper()._calculate_agglomeration_fraction

    for name, propellant in shipped_propellants.items():
        coefficients = propellant["components"]["Aluminum"]["agglomeration_coefficients"]
        for pressure in FITTED_PRESSURES_PA:
            plotted = plot_fn(coefficients, pressure)
            mapped = mapper_fn(coefficients, pressure)
            assert plotted == mapped, f"{name} at {pressure / 1e6:.3f} MPa"
            # And it really is a fraction in (0, 1) there, for every shipped fit.
            assert 0.0 < plotted < 1.0, f"{name} at {pressure / 1e6:.3f} MPa"


def test_plot_and_region_mapper_polynomials_diverge_outside_the_fitted_range(
    plotting, region_mapper, shipped_propellants
):
    """ACTUAL CURRENT BEHAVIOUR: extrapolation is where the clamp starts to matter.

    Bas_3's fit is negative at 12 MPa. RegionMapper returns the negative value
    (yielding `1 - fraction > 1` downstream); the plot script clamps it to 0. The
    two therefore disagree outside the range the coefficients were fitted on.
    """
    coefficients = shipped_propellants["Bas_3"]["components"]["Aluminum"][
        "agglomeration_coefficients"
    ]
    plot_fn = plotting.main._calculate_agglomeration_fraction
    mapper_fn = region_mapper.region_mappers.DiffusionRegionMapper()._calculate_agglomeration_fraction

    assert mapper_fn(coefficients, 12.0e6) < 0.0
    assert plot_fn(coefficients, 12.0e6) == 0.0


def test_shipped_agglomeration_fits_span_the_documented_band(
    plotting, shipped_propellants
):
    """The source docstring records 0.086..0.296 over 1..6.5 MPa for the shipped set."""
    values = [
        plotting.main._calculate_agglomeration_fraction(
            propellant["components"]["Aluminum"]["agglomeration_coefficients"], pressure
        )
        for propellant in shipped_propellants.values()
        for pressure in FITTED_PRESSURES_PA
    ]

    assert min(values) == pytest.approx(0.0862564102565, abs=1e-9)
    assert max(values) == pytest.approx(0.2961098901624, abs=1e-9)


# --- Nested-frame accessors ------------------------------------------------------------


def test_plot_parameters_all_have_labels(plotting):
    assert set(plotting.main.PLOT_PARAMETERS) <= set(plotting.main.PARAMETER_LABELS)


def test_gas_phase_accessors_read_the_right_nested_keys(plotting, shipped_propellants):
    """Skeleton and out-skeleton values live two levels down inside pocket_gas_phase;
    a mis-wired accessor would silently plot the parent phase's number instead."""
    frame = shipped_propellants["Bas_0"]["pressure_frames"][0]
    phases = dict(
        (label, accessor) for label, accessor in plotting.main._gas_phase_phases("lambda_gas")
    )

    assert set(phases) == {
        "Inter-Pocket Gas Phase",
        "Pocket (Diffusion) Gas Phase",
        "Skeleton Gas Phase",
        "Outer Skeleton Phase",
    }
    assert phases["Inter-Pocket Gas Phase"](frame) == frame["inter_pocket_gas_phase"]["lambda_gas"]
    assert phases["Skeleton Gas Phase"](frame) == (
        frame["pocket_gas_phase"]["skeleton_gas_phase"]["lambda_gas"]
    )
    assert phases["Outer Skeleton Phase"](frame) == (
        frame["pocket_gas_phase"]["out_skeleton_gas_phase"]["lambda_gas"]
    )


def test_gas_phase_accessors_return_none_for_an_absent_field(plotting, shipped_propellants):
    """Absent fields are dropped from the curve rather than raising."""
    frame = shipped_propellants["Bas_0"]["pressure_frames"][0]

    for _, accessor in plotting.main._gas_phase_phases("no_such_parameter"):
        assert accessor(frame) is None


def test_temperature_accessors_pick_the_right_flame_per_phase(plotting, shipped_propellants):
    """Flame temperatures sit at different keys per phase -- the inter-pocket and
    skeleton phases expose T_kinetic_flame, the pocket exposes T_diffusion_flame."""
    frame = shipped_propellants["Bas_0"]["pressure_frames"][0]
    phases = dict(plotting.main.TEMPERATURE_PHASES)

    assert phases["Inter-Pocket Gas Phase"](frame) == frame["inter_pocket_gas_phase"]["T_kinetic_flame"]
    assert phases["Skeleton Gas Phase"](frame) == (
        frame["pocket_gas_phase"]["skeleton_gas_phase"]["T_kinetic_flame"]
    )
    assert phases["Diffusion Flame (Pocket)"](frame) == (
        frame["pocket_gas_phase"]["T_diffusion_flame"]
    )
    # And they are genuinely different numbers, so a mix-up would be visible.
    assert phases["Inter-Pocket Gas Phase"](frame) != phases["Skeleton Gas Phase"](frame)


def test_late_bound_closure_bug_is_absent_in_gas_phase_accessors(plotting):
    """`_gas_phase_phases` builds its lambdas via a factory to avoid late binding.

    A naive comprehension would make every accessor close over the final loop
    variable, so all four panels would read the same phase. Verify each accessor
    reads a distinct path.
    """
    frame = {
        "inter_pocket_gas_phase": {"x": 1},
        "pocket_gas_phase": {
            "x": 2,
            "skeleton_gas_phase": {"x": 3},
            "out_skeleton_gas_phase": {"x": 4},
        },
    }

    values = [accessor(frame) for _, accessor in plotting.main._gas_phase_phases("x")]

    assert values == [1, 2, 3, 4]


# --- Output path resolution -------------------------------------------------------------


def test_resolve_output_path_returns_the_bare_filename_with_no_output_dir(plotting):
    """The .NET PythonPlotsRenderer reads the PNGs back from the process working
    directory, so the no-flag path must stay a bare relative filename."""
    assert plotting.main.resolve_output_path(None, "lambda_gas_plot.png") == "lambda_gas_plot.png"
    assert plotting.main.resolve_output_path("", "lambda_gas_plot.png") == "lambda_gas_plot.png"


def test_resolve_output_path_joins_and_creates_the_directory(plotting, tmp_path):
    target = tmp_path / "plots"

    resolved = plotting.main.resolve_output_path(str(target), "temperatures_plot.png")

    assert resolved == str(target / "temperatures_plot.png")
    assert target.is_dir()


def test_resolve_output_path_is_idempotent_for_an_existing_directory(plotting, tmp_path):
    first = plotting.main.resolve_output_path(str(tmp_path), "a.png")
    second = plotting.main.resolve_output_path(str(tmp_path), "b.png")

    assert first == str(tmp_path / "a.png")
    assert second == str(tmp_path / "b.png")


# --- skeleton_layer_plots ----------------------------------------------------------------


def test_skeleton_pressures_are_converted_from_pascals_to_megapascals(plotting):
    """The skeleton plots label their x-axis 'Pressure, MPa' while the sidecar JSON
    stores pascals, so this conversion is the only thing making the label true."""
    fuel = {"pressure_frames": [{"pressure": 1.0e6}, {"pressure": 6.5e6}, {"pressure": 2.5e5}]}

    assert plotting.skeleton_layer_plots._pressures_mpa(fuel) == [1.0, 6.5, 0.25]


def test_skeleton_pressures_match_the_shipped_frames(plotting, shipped_propellants):
    fuel = shipped_propellants["Bas_0"]
    pressures = plotting.skeleton_layer_plots._pressures_mpa(fuel)

    assert pressures[0] == pytest.approx(1.0)
    assert pressures[-1] == pytest.approx(6.5)
    assert pressures == sorted(pressures)


def test_skeleton_series_keys_are_unique_within_each_group(plotting):
    """Duplicate keys would silently draw the same series into two panels."""
    module = plotting.skeleton_layer_plots
    for series in (
        module.HEAT_FLUX_SERIES,
        module.FLAME_HEIGHT_SERIES,
        module.SKELETON_LAYER_SERIES,
        module.TEMPERATURE_SERIES,
    ):
        keys = [key for key, _, _ in series]
        assert len(keys) == len(set(keys))
        assert all(isinstance(label, str) and label for _, label, _ in series)
