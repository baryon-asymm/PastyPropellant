# RegionMapper

Splits a propellant into its **combustion regions** and computes the elemental
composition and enthalpy of each one.

The pasty-propellant combustion model treats the burning surface as a set of distinct
regions rather than one homogeneous mixture. This tool takes the bulk propellant
formulation and produces, per region, the normalized mass fractions of the components
present there, then converts those into a normalized elemental composition and an
overall enthalpy that the downstream thermodynamics code consumes.

Pure standard library — no third-party dependencies.

## Regions

| Region | Contents |
|--------|----------|
| Inter-pocket | Homogeneous mixture of all components; mass fractions equal the bulk propellant's. |
| Pocket without skeleton | Remaining homogeneous mixture, excluding the large AP particles. |
| Pocket with skeleton | Binder and the small AP fraction only. |
| Diffusion | All components plus only the **non-agglomerated** portion of the Aluminum. |

The diffusion region is the only pressure-dependent one: the agglomerated Aluminum
fraction is a polynomial in pressure (evaluated in MPa) read from each propellant's
`agglomeration_coefficients`. That fraction is a mass fraction in `[0, 1]`, and it is
a fit valid only over the pressure range it was fitted on (1–6.5 MPa for the shipped
propellants) — extrapolating far above that range can drive it negative.

## Invocation

```bash
cd src/python/RegionMapper/src
python3 main.py \
    --propellants ../data/propellants.json \
    --components  ../data/components.json \
    --pressure    4e6 \
    --output-dir  /path/to/output
```

All four arguments are required. `--pressure` is in **Pascals** and must be positive.

The .NET side drives this through `PythonRegionMapper`
(`src/dotnet/Tools/PastyPropellant.RegionMapper`), which invokes it once per pressure.

## Inputs

- `--propellants` — JSON array of propellants. Each has a `name` and a `components`
  object keyed by component name (`CombustibleBinder`, `AmmoniumPerchlorate`,
  `Aluminum`, `Octogen`), each carrying `mass_fraction` and, where applicable,
  `large_particles_fraction` and `agglomeration_coefficients`.
- `--components` — JSON array mapping each component name to its elemental
  `composition` and its `enthalpy` (J/kg).

Required physical quantities are validated rather than defaulted: a missing
`mass_fraction`, `large_particles_fraction`, or `agglomeration_coefficients` raises a
`ValueError` naming the component, because silently substituting zero would fabricate
a composition.

## Outputs

One subdirectory per propellant under `--output-dir`, each containing four files:

```
<output-dir>/<propellant>/inter_pocket.json
<output-dir>/<propellant>/pocket_without_skeleton.json
<output-dir>/<propellant>/pocket_with_skeleton.json
<output-dir>/<propellant>/diffusion.json
```

Every file holds a `RegionCalculationResult`:

```json
{
    "pressure": 4000000.0,
    "enthalpy": -1235458.4220253818,
    "composition": {
        "C": 9.047685570260045,
        "H": 33.65217607327984,
        "O": 23.155277537871203,
        "N": 12.808631856240456,
        "Cl": 3.501062594801998,
        "Al": 6.798269815393217
    }
}
```

`pressure` is in Pascals, `enthalpy` in J/kg, and `composition` is the normalized
elemental composition of the region. These files are the direct input to
[PorosityCalculation](../PorosityCalculation/README.md) and to the thermodynamics
tooling.

## Layout

| Module | Role |
|--------|------|
| `main.py` | CLI entry point; wires readers, mappers, calculator and writer together. |
| `region_mappers.py` | One mapper class per region; returns normalized mass fractions. |
| `calculators.py` | `RegionCalculator` — turns region mass fractions into elemental composition and enthalpy. |
| `models.py` | Frozen dataclasses: `Component`, `PropellantComponent`, `Propellant`. |
| `json_reader.py` / `json_writer.py` | Input parsing and result serialisation. |
| `utils.py` | Elemental-composition and enthalpy helpers. |
| `molar_masses.py` | `ELEMENT_MOLAR_MASSES` lookup table. |
