"""End-to-end tests of the RegionMapper -> PorosityCalculation chain, plus CLI helpers.

In production these two packages run as separate child processes joined by a JSON
file on disk. Here they are driven in-process over the real shipped inputs, so the
whole numerical chain is covered without shelling out to anything.
"""

import json

import pytest

# The molar masses the tables ship, used to check the mass-conservation invariant
# end to end.
MOLAR_MASSES = {
    "H": 0.00100784,
    "C": 0.012011,
    "N": 0.014007,
    "O": 0.015999,
    "Al": 0.0269815385,
    "Cl": 0.03545,
}


def test_molar_mass_tables_agree_between_the_two_packages(region_mapper, porosity):
    """Both packages ship their own copy of ELEMENT_MOLAR_MASSES. If they ever drift,
    RegionMapper's mol/kg amounts and PorosityCalculation's mass reconstruction stop
    being inverses of each other and porosity silently shifts."""
    assert region_mapper.molar_masses.ELEMENT_MOLAR_MASSES == (
        porosity.molar_masses.ELEMENT_MOLAR_MASSES
    )


@pytest.mark.parametrize("element, expected", sorted(MOLAR_MASSES.items()))
def test_molar_mass_table_values(region_mapper, element, expected):
    """Spot-check the elements the propellant chemistry actually uses, in kg/mol."""
    assert region_mapper.molar_masses.ELEMENT_MOLAR_MASSES[element] == expected


def test_molar_masses_are_in_kilograms_per_mole(region_mapper):
    """Every entry must be sub-unity; a g/mol entry sneaking in would be ~1000x off."""
    table = region_mapper.molar_masses.ELEMENT_MOLAR_MASSES
    assert table
    assert all(0.0 < mass < 1.0 for mass in table.values())


# --- The full chain over the shipped inputs -------------------------------------------


@pytest.fixture
def shipped_chain(region_mapper, shipped_propellants_path, shipped_components_path):
    components = region_mapper.json_reader.read_components(str(shipped_components_path))
    propellants = region_mapper.json_reader.read_propellants(str(shipped_propellants_path))
    return components, propellants


@pytest.mark.parametrize("region_name", [
    "inter_pocket", "pocket_without_skeleton", "pocket_with_skeleton", "diffusion",
])
def test_every_region_of_every_shipped_propellant_produces_a_finite_result(
    region_mapper, shipped_chain, region_name
):
    components, propellants = shipped_chain
    mappers = {
        "inter_pocket": lambda p: region_mapper.region_mappers.InterPocketRegionMapper().calculate(p),
        "pocket_without_skeleton": lambda p: (
            region_mapper.region_mappers.PocketRegionWithoutSkeletonMapper().calculate(p)
        ),
        "pocket_with_skeleton": lambda p: (
            region_mapper.region_mappers.PocketRegionWithSkeletonMapper().calculate(p)
        ),
        "diffusion": lambda p: (
            region_mapper.region_mappers.DiffusionRegionMapper().calculate(p, 6.5e6)
        ),
    }

    for propellant in propellants:
        region_data = mappers[region_name](propellant)
        result = region_mapper.calculators.RegionCalculator.calculate(
            region_data, components, 6.5e6
        )

        assert result.pressure == 6.5e6
        # Formation enthalpies of these ingredients are negative overall.
        assert result.enthalpy < 0
        assert result.composition
        for element, amount in result.composition.items():
            assert element in MOLAR_MASSES, element
            assert amount > 0, (propellant.name, element)


def test_region_composition_conserves_mass_for_the_shipped_propellants(
    region_mapper, shipped_chain
):
    """Sum over elements of amount * molar mass must recover the region's total mass
    fraction. For the pocket/diffusion regions that total is 1 (they renormalise);
    for the inter-pocket region it is the propellant's own fraction sum."""
    components, propellants = shipped_chain
    mappers = [
        region_mapper.region_mappers.PocketRegionWithoutSkeletonMapper().calculate,
        region_mapper.region_mappers.PocketRegionWithSkeletonMapper().calculate,
    ]

    for propellant in propellants:
        for mapper in mappers:
            region_data = mapper(propellant)
            result = region_mapper.calculators.RegionCalculator.calculate(
                region_data, components, 1.0e6
            )
            recovered = sum(
                amount * MOLAR_MASSES[element]
                for element, amount in result.composition.items()
            )
            assert recovered == pytest.approx(1.0, rel=1e-9), propellant.name


def test_diffusion_region_composition_tracks_pressure(region_mapper, shipped_chain):
    """More agglomeration at higher pressure means a smaller free-Al amount and a
    correspondingly larger share for every other element."""
    components, propellants = shipped_chain
    bas_2 = next(p for p in propellants if p.name == "Bas_2")
    mapper = region_mapper.region_mappers.DiffusionRegionMapper()
    calc = region_mapper.calculators.RegionCalculator.calculate

    low = calc(mapper.calculate(bas_2, 1.0e6), components, 1.0e6).composition
    high = calc(mapper.calculate(bas_2, 6.5e6), components, 6.5e6).composition

    # Bas_2's fit falls from 0.2513 to 0.1902 across the range, so LESS Al agglomerates
    # at high pressure and MORE free Al remains.
    assert high["Al"] > low["Al"]
    assert high["C"] < low["C"]


def test_full_chain_from_propellants_json_to_porosity_json(
    region_mapper, porosity, shipped_chain, shipped_propellants_path, tmp_path
):
    """Drive the exact production sequence in-process: map the diffusion region,
    write it out with RegionMapper's writer, read it back with PorosityCalculation's
    reader, and compute porosity."""
    components, propellants = shipped_chain
    bas_0 = next(p for p in propellants if p.name == "Bas_0")

    region_data = region_mapper.region_mappers.DiffusionRegionMapper().calculate(bas_0, 1.0e6)
    region_result = region_mapper.calculators.RegionCalculator.calculate(
        region_data, components, 1.0e6
    )
    region_file = tmp_path / "diffusion.json"
    region_mapper.json_writer.JSONWriter.write(region_result, str(region_file))

    loaded_region = porosity.json_reader.load_region_result(str(region_file))
    porosity_propellants = porosity.json_reader.load_propellants(str(shipped_propellants_path))
    bas_0_dense = next(p for p in porosity_propellants if p.name == "Bas_0")

    result = porosity.calculators.calculate_porosity(
        porosity.calculators.calculate_region_density(bas_0_dense), bas_0_dense, loaded_region
    )

    output_file = tmp_path / porosity.main.get_output_path("porosity.json")
    porosity.json_writer.write_porosity_result(result, str(output_file))
    written = json.loads(output_file.read_text(encoding="utf-8"))

    assert result.region_density == pytest.approx(1659.212917, abs=1e-5)
    # Regression pin on the end-to-end number for Bas_0's diffusion region at 1 MPa.
    # Cross-checked by hand: agglomeration 0.181014 -> free Al mass fraction 0.176395
    # of the region -> 6.5377 mol/kg Al and 9.1254 mol/kg C; the condensed volume
    # (Al plus a tenth of the carbon) is 11.64% of the region's 6.027e-4 m^3/kg.
    assert result.porosity == pytest.approx(0.8835790704, abs=1e-9)
    assert 0.0 < result.porosity < 1.0

    # The Al amount round-trips: amount * M_Al recovers the region's Al mass fraction.
    assert loaded_region.composition["Al"] * MOLAR_MASSES["Al"] == pytest.approx(
        region_data.components["Aluminum"], rel=1e-9
    )
    assert written["porosity"] == pytest.approx(result.porosity)
    assert written["region_input"]["pressure"] == 1.0e6


# --- CLI helpers ------------------------------------------------------------------------


def test_region_mapper_cli_parses_a_valid_invocation(region_mapper, monkeypatch):
    monkeypatch.setattr(
        "sys.argv",
        ["main.py", "--propellants", "p.json", "--components", "c.json",
         "--pressure", "6.5e6", "--output-dir", "out"],
    )

    args = region_mapper.main.parse_args()

    assert args.propellants == "p.json"
    assert args.components == "c.json"
    assert args.pressure == 6.5e6  # parsed as float, in pascals
    assert args.output_dir == "out"


@pytest.mark.parametrize("pressure", ["0", "-1e6"])
def test_region_mapper_cli_rejects_non_positive_pressure(
    region_mapper, monkeypatch, pressure
):
    monkeypatch.setattr(
        "sys.argv",
        ["main.py", "--propellants", "p.json", "--components", "c.json",
         "--pressure", pressure, "--output-dir", "out"],
    )

    with pytest.raises(SystemExit):
        region_mapper.main.parse_args()


def test_region_mapper_cli_requires_every_argument(region_mapper, monkeypatch):
    monkeypatch.setattr("sys.argv", ["main.py", "--pressure", "1e6"])

    with pytest.raises(SystemExit):
        region_mapper.main.parse_args()


def test_ensure_directory_exists_creates_nested_directories(region_mapper, tmp_path):
    target = tmp_path / "out" / "Bas_0"

    region_mapper.main.ensure_directory_exists(str(target))
    region_mapper.main.ensure_directory_exists(str(target))  # idempotent

    assert target.is_dir()
