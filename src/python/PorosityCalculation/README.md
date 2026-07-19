# PorosityCalculation

Computes the **density and porosity of a combustion region** produced by
[RegionMapper](../RegionMapper/README.md).

Once a region's elemental composition is known, only part of that region is condensed
matter: the retained carbon and the aluminium. This tool works out how much volume
those condensed species occupy relative to the region's total component volume, and
reports the remainder as porosity — the void fraction available to the gas phase.

Pure standard library — no third-party dependencies.

## Method

- **Region density** — the inverse of the volume of one kilogram of propellant,
  `1 / Σ(mass_fraction / density)` over all components.
- **Condensed volume** — carbon and aluminium volumes from the region's elemental
  composition and their molar masses. Only `CARBON_RETENTION = 0.1` (10%) of the
  carbon is treated as retained in the region; the rest is assumed to leave as gas.
  Carbon is taken at `CARBON_DENSITY = 2267 kg/m³`, aluminium at the density declared
  for the propellant's `Aluminum` component.
- **Porosity** — `1 - (V_carbon + V_aluminium) / V_total`.

## Invocation

```bash
cd src/python/PorosityCalculation/src
python3 main.py \
    --propellants-file /path/to/propellants.json \
    --propellant-name  Bas_1 \
    --region-file      /path/to/output/Bas_1/diffusion.json
```

All three arguments are required. The .NET side drives this through
`PythonPorosityCalculator` (`src/dotnet/Tools/PastyPropellant.PorosityCalculation`).

## Inputs

- `--propellants-file` — JSON array of propellants; each needs a `name`, a `density`,
  and a `components` object where every component carries `mass_fraction` and
  `density`.
- `--propellant-name` — which propellant in that file to use.
- `--region-file` — a region JSON written by RegionMapper (`diffusion.json`,
  `inter_pocket.json`, …), supplying `pressure`, `enthalpy` and `composition`.

Missing fields are reported rather than defaulted: the loader raises a `KeyError`
naming the file, the offending record and the field, and listing the fields that *are*
present. A propellant with no `Aluminum` component, or a non-positive total volume, is
likewise an explicit error — porosity is undefined in those cases.

## Output

Writes `porosity.json` **next to the region file** (same directory), and prints a
short summary:

```
Results saved to: .../Bas_1/porosity.json
Region density: 1659.2 kg/m³
Porosity: 0.8793
```

The file embeds the region input it was derived from, so each result is
self-describing:

```json
{
    "region_density": 1659.212916632951,
    "porosity": 0.8793256911641496,
    "region_input": {
        "pressure": 4000000.0,
        "enthalpy": -1235458.4220253818,
        "composition": { "C": 9.047, "H": 33.652, "...": 0 }
    }
}
```

`region_density` is in kg/m³ and `porosity` is a dimensionless void fraction in
`[0, 1]`.

## Layout

| Module | Role |
|--------|------|
| `main.py` | CLI entry point; loads inputs, runs the calculation, writes the result. |
| `calculators.py` | `calculate_region_density`, `calculate_porosity`, and the `PorosityCalculationResult` DTO. |
| `models.py` | Frozen dataclasses: `Propellant`, `PropellantComponent`, `RegionCalculationResult`. |
| `json_reader.py` / `json_writer.py` | Input parsing (with required-field validation) and result serialisation. |
| `molar_masses.py` | `ELEMENT_MOLAR_MASSES` lookup table. |
