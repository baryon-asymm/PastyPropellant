"""Parsing tests for both packages' JSON readers.

RegionMapper and PorosityCalculation each ship their own reader with different
defaulting rules for the same input file. These tests pin what each one actually
does, including where they disagree.
"""

import json

import pytest


def _write(tmp_path, name, payload):
    path = tmp_path / name
    path.write_text(json.dumps(payload), encoding="utf-8")
    return str(path)


# --- RegionMapper: read_components ---------------------------------------------------


def test_read_components_parses_the_list_of_single_key_objects(region_mapper, tmp_path):
    path = _write(
        tmp_path,
        "components.json",
        [
            {"CombustibleBinder": {"composition": {"C": 23.85, "H": 71.18}, "enthalpy": -2543930.0}},
            {"Aluminum": {"composition": {"Al": 1}, "enthalpy": 0.0}},
        ],
    )

    components = region_mapper.json_reader.read_components(path)

    assert set(components) == {"CombustibleBinder", "Aluminum"}
    assert components["CombustibleBinder"].name == "CombustibleBinder"
    assert components["CombustibleBinder"].composition == {"C": 23.85, "H": 71.18}
    assert components["CombustibleBinder"].enthalpy == -2543930.0
    assert components["Aluminum"].enthalpy == 0.0


def test_read_components_defaults_missing_fields(region_mapper, tmp_path):
    path = _write(tmp_path, "components.json", [{"Mystery": {}}])

    components = region_mapper.json_reader.read_components(path)

    assert components["Mystery"].composition == {}
    assert components["Mystery"].enthalpy == 0.0


def test_read_components_silently_drops_all_but_the_first_key(region_mapper, tmp_path):
    """ACTUAL CURRENT BEHAVIOUR, and a latent trap.

    `read_components` does `next(iter(item.items()))`, so if two components are ever
    written into one JSON object the second is discarded without any warning. The
    shipped `data/propellant_components.json` uses one component per object, so
    this never bites today. Pinned, not fixed.
    """
    path = _write(
        tmp_path,
        "components.json",
        [{"First": {"composition": {"C": 1}, "enthalpy": 1.0},
          "Second": {"composition": {"H": 1}, "enthalpy": 2.0}}],
    )

    components = region_mapper.json_reader.read_components(path)

    assert set(components) == {"First"}
    assert "Second" not in components


def test_read_components_reads_the_shipped_file(region_mapper, shipped_components_path):
    components = region_mapper.json_reader.read_components(str(shipped_components_path))

    assert set(components) == {
        "CombustibleBinder",
        "AmmoniumPerchlorate",
        "Aluminum",
        "Octogen",
    }
    assert components["Octogen"].composition == {"C": 4, "H": 8, "O": 8, "N": 8}
    assert components["Octogen"].enthalpy == pytest.approx(313000.0)
    assert components["AmmoniumPerchlorate"].enthalpy == pytest.approx(-2512000.0)


# --- RegionMapper: read_propellants --------------------------------------------------


def test_read_propellants_parses_components_and_optional_fields(region_mapper, tmp_path):
    path = _write(
        tmp_path,
        "propellants.json",
        [
            {
                "name": "Bas_0",
                "components": {
                    "CombustibleBinder": {"mass_fraction": 0.21},
                    "AmmoniumPerchlorate": {
                        "mass_fraction": 0.3,
                        "large_particles_fraction": 0.65,
                    },
                    "Aluminum": {
                        "mass_fraction": 0.2073,
                        "agglomeration_coefficients": [0.19, -0.005],
                    },
                },
            }
        ],
    )

    propellants = region_mapper.json_reader.read_propellants(path)

    assert len(propellants) == 1
    bas_0 = propellants[0]
    assert bas_0.name == "Bas_0"
    assert bas_0.components["CombustibleBinder"].mass_fraction == 0.21
    assert bas_0.components["CombustibleBinder"].large_particles_fraction is None
    assert bas_0.components["AmmoniumPerchlorate"].large_particles_fraction == 0.65
    assert bas_0.components["Aluminum"].agglomeration_coefficients == [0.19, -0.005]


def test_read_propellants_defaults_a_missing_mass_fraction_to_zero(region_mapper, tmp_path):
    """ACTUAL CURRENT BEHAVIOUR: the reader defaults, the mapper then rejects.

    A component with no `mass_fraction` parses as 0.0 rather than failing at read
    time; the failure surfaces later as the mapper's "Missing or zero mass fraction"
    ValueError. Contrast PorosityCalculation's reader, which fails at read time
    (see test_load_propellants_requires_a_mass_fraction).
    """
    path = _write(
        tmp_path,
        "propellants.json",
        [{"name": "X", "components": {"CombustibleBinder": {}}}],
    )

    propellants = region_mapper.json_reader.read_propellants(path)

    assert propellants[0].components["CombustibleBinder"].mass_fraction == 0.0


def test_read_propellants_defaults_agglomeration_coefficients_to_empty_list(
    region_mapper, tmp_path
):
    """ACTUAL CURRENT BEHAVIOUR: `[]` here, `None` in PorosityCalculation's reader.

    Both are falsy, so both trip the same downstream guard, but the parsed value
    differs between the two packages for the same input file.
    """
    path = _write(
        tmp_path,
        "propellants.json",
        [{"name": "X", "components": {"Aluminum": {"mass_fraction": 0.2}}}],
    )

    propellants = region_mapper.json_reader.read_propellants(path)

    assert propellants[0].components["Aluminum"].agglomeration_coefficients == []


def test_read_propellants_rejects_non_dict_components(region_mapper, tmp_path):
    path = _write(tmp_path, "propellants.json", [{"name": "X", "components": ["oops"]}])

    with pytest.raises(ValueError, match="Expected a dictionary for propellant data"):
        region_mapper.json_reader.read_propellants(path)


def test_read_propellants_reads_the_shipped_file(region_mapper, shipped_propellants_path):
    propellants = region_mapper.json_reader.read_propellants(str(shipped_propellants_path))

    assert [p.name for p in propellants] == ["Bas_0", "Bas_1", "Bas_2", "Bas_3", "Bas_4"]
    for propellant in propellants:
        assert set(propellant.components) >= {
            "CombustibleBinder",
            "AmmoniumPerchlorate",
            "Aluminum",
            "Octogen",
        }
        assert propellant.components["Aluminum"].agglomeration_coefficients
        assert propellant.components["AmmoniumPerchlorate"].large_particles_fraction is not None


# --- PorosityCalculation: load_propellants ------------------------------------------


def test_load_propellants_parses_densities(porosity, tmp_path):
    path = _write(
        tmp_path,
        "propellants.json",
        [
            {
                "name": "Bas_0",
                "density": 1659,
                "components": {
                    "CombustibleBinder": {"mass_fraction": 0.21, "density": 950},
                    "Aluminum": {"mass_fraction": 0.2073, "density": 2700},
                },
            }
        ],
    )

    propellants = porosity.json_reader.load_propellants(path)

    assert len(propellants) == 1
    assert propellants[0].density == 1659
    assert propellants[0].components["Aluminum"].density == 2700
    assert propellants[0].components["Aluminum"].large_particles_fraction is None
    assert propellants[0].components["Aluminum"].agglomeration_coefficients is None


@pytest.mark.parametrize("missing", ["name", "density", "components"])
def test_load_propellants_requires_top_level_fields(porosity, tmp_path, missing):
    record = {
        "name": "Bas_0",
        "density": 1659,
        "components": {"Aluminum": {"mass_fraction": 0.2, "density": 2700}},
    }
    del record[missing]
    path = _write(tmp_path, "propellants.json", [record])

    with pytest.raises(KeyError, match=f"Missing required field '{missing}'"):
        porosity.json_reader.load_propellants(path)


def test_load_propellants_requires_a_mass_fraction(porosity, tmp_path):
    path = _write(
        tmp_path,
        "propellants.json",
        [{"name": "X", "density": 1500, "components": {"Aluminum": {"density": 2700}}}],
    )

    with pytest.raises(KeyError, match="Missing required field 'mass_fraction'"):
        porosity.json_reader.load_propellants(path)


def test_load_propellants_requires_a_component_density(porosity, tmp_path):
    path = _write(
        tmp_path,
        "propellants.json",
        [{"name": "X", "density": 1500, "components": {"Aluminum": {"mass_fraction": 0.2}}}],
    )

    with pytest.raises(KeyError, match="Missing required field 'density'"):
        porosity.json_reader.load_propellants(path)


def test_missing_field_error_names_the_record_and_the_fields_present(porosity, tmp_path):
    path = _write(
        tmp_path,
        "propellants.json",
        [{"name": "Bas_7", "components": {}}],
    )

    with pytest.raises(KeyError) as excinfo:
        porosity.json_reader.load_propellants(path)

    message = str(excinfo.value)
    assert "Bas_7" in message
    assert "propellants.json" in message
    assert "Fields present" in message


def test_load_propellants_rejects_a_non_list_document(porosity, tmp_path):
    path = _write(tmp_path, "propellants.json", {"name": "not-a-list"})

    with pytest.raises(ValueError, match="JSON array of propellants"):
        porosity.json_reader.load_propellants(path)


def test_load_propellants_reads_the_shipped_file(porosity, shipped_propellants_path):
    propellants = porosity.json_reader.load_propellants(str(shipped_propellants_path))

    assert [p.name for p in propellants] == ["Bas_0", "Bas_1", "Bas_2", "Bas_3", "Bas_4"]
    for propellant in propellants:
        assert propellant.density > 0
        for component in propellant.components.values():
            assert component.density > 0
            assert 0 <= component.mass_fraction <= 1


# --- PorosityCalculation: load_region_result -----------------------------------------


def test_load_region_result_round_trips_a_region_mapper_output(
    region_mapper, porosity, tmp_path
):
    """The two packages are chained in production: RegionMapper writes, Porosity reads."""
    result = region_mapper.calculators.RegionCalculationResult(
        pressure=6.5e6,
        enthalpy=-1234.5,
        composition={"C": 0.125, "Al": 0.15},
    )
    path = tmp_path / "diffusion.json"
    region_mapper.json_writer.JSONWriter.write(result, str(path))

    loaded = porosity.json_reader.load_region_result(str(path))

    assert loaded.pressure == pytest.approx(6.5e6)
    assert loaded.enthalpy == pytest.approx(-1234.5)
    assert loaded.composition == {"C": 0.125, "Al": 0.15}


@pytest.mark.parametrize("missing", ["pressure", "enthalpy", "composition"])
def test_load_region_result_requires_every_field(porosity, tmp_path, missing):
    payload = {"pressure": 1e6, "enthalpy": -1.0, "composition": {"C": 1.0}}
    del payload[missing]
    path = _write(tmp_path, "region.json", payload)

    with pytest.raises(KeyError, match=f"Missing required field '{missing}'"):
        porosity.json_reader.load_region_result(path)


# --- Writers -------------------------------------------------------------------------


def test_porosity_writer_emits_the_expected_document(porosity, region_mapper, tmp_path):
    region_input = porosity.models.RegionCalculationResult(
        pressure=1.0e6, enthalpy=-500.0, composition={"C": 0.2, "Al": 0.3}
    )
    result = porosity.calculators.PorosityCalculationResult(
        region_density=1659.2, porosity=0.4242, region_input=region_input
    )
    path = tmp_path / "porosity.json"

    porosity.json_writer.write_porosity_result(result, str(path))

    written = json.loads(path.read_text(encoding="utf-8"))
    assert written == {
        "region_density": 1659.2,
        "porosity": 0.4242,
        "region_input": {
            "pressure": 1.0e6,
            "enthalpy": -500.0,
            "composition": {"C": 0.2, "Al": 0.3},
        },
    }
