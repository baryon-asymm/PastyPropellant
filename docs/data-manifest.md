# `data/` Manifest

**Purpose:** answer, per file, *what is this, who reads it, and does a run need it.*
Resolves **DOC-3** in [tech-debt.md](tech-debt.md).

**Date:** 2026-07-19. **Method:** repo-wide greps for each filename and for path
literals containing `data`, cross-checked against the project list in
`PastyPropellant.sln` (so "live consumer" means *a project the standard
`dotnet build` / `dotnet test` actually compiles*), plus content inspection of
every file. Nothing under `data/` was modified.

All 14 files are **tracked in git**.

> ⚠️ **`data/telegram_settings.json` holds a credential.** See its row below before touching it.

---

## Summary

| File | Required for a run? | Consumer |
|---|---|---|
| `propellants.01234.json` | **Yes — the only required input** | `ConsoleApp/Program.cs` (both entry paths) |
| `propellants.json` | No — but a **live test** reads it | `ConsoleApp.Tests/ConstructPropellantJsonHelperTest` |
| `propellants.0.json`, `.1.json`, `.234.json` | No | no consumer found |
| `telegram_settings.json` | No | no consumer found — **contains a credential** |
| `thermodynamic_substances.json` | No | no consumer found (stale copy of a live file) |
| `propellant_components.json` | No | no consumer found (exact copy of a live file) |
| `thermodynamic_params.json` | No | no consumer found |
| `TAB.dat` / `substances_file.json` | No | no consumer found (byte-identical pair) |
| `clients.json` | No | no consumer found |
| `chemical_elements.json` | No | **0 bytes** — empty |
| `hh.txt` | No | **0 bytes** — empty |

**One correction to the previous understanding.** The audit note in `CLAUDE.md` says the live
entry point consumes only `data/propellants*.json`. That is accurate *for the entry point*, but it
undersells `data/`: `data/propellants.json` — which the entry point never loads — is read by
`ConstructPropellantJsonHelperTest`, a test in `PastyPropellant.ConsoleApp.Tests`, which **is** a
member of `PastyPropellant.sln`. Deleting it would break a solution test, not just an unused fixture.
Details in its row below.

---

## Propellant definition sets

### `propellants.01234.json` — 64 KB — **required**

The live input. JSON array of 5 propellant objects (`Bas_0`, `Bas_1`, `Bas_2`, `Bas_3`, `Bas_4`),
deserialised as `List<Propellant>` (case-insensitive) by
`DifferentialEvolutionScenarioSettings.Builder.WithPropellantsFromFile`.

Per-object keys: `name`, `a`, `nu` (Vieille law coefficients), `density`,
`specific_heat_capacity`, `initial_temperature`, `pocket_surface_fraction_coefficients`
(6-term polynomial), `pocket_mass_fraction`, `components`, `pressure_frames`
(the per-pressure experimental burn-rate data the fitness function targets).

**Consumers:** `ConsoleApp/Program.cs` — hardcoded literal `"propellants.01234.json"` at the
group-optimisation call site, the forward-eval call site, the plot-rendering call, and the report
call. There is no ticket/config file; the name is baked in.

**Path resolution gotcha:** the literal is a *bare relative path* passed to `File.Exists` /
`File.ReadAllText`, so it resolves against the **process working directory**, not against `data/`
and not against the repo root. A copy therefore also lives at
`artifacts/bin/PastyPropellant.ConsoleApp/Release/net10.0/propellants.01234.json`, which is what a
run launched from the bin output directory actually reads. Editing `data/propellants.01234.json`
alone will not change such a run. (Related: ARCH-5 in the tech-debt register.)

**Fails loudly if wrong:** the scenario partitions by name with `First(...)`, so a file missing any
of `Bas_0` / `Bas_1` / `Bas_2`+`Bas_3`+`Bas_4` throws.

### `propellants.json` — 9.6 KB — not required, but **has a live consumer**

**Older-schema** set of 4 propellants (`Bas_1`, `Bas_2`, `Bas_3`, `Bas_4` — note: **no `Bas_0`**).
Schema differs from the `.01234` file in three ways: it **has** `confidence_intervals`,
`inter_pocket_gas_phase`, and `pocket_gas_phase`; it **lacks** `pressure_frames`. So it predates the
pressure-frame formulation and is not a drop-in substitute for the live input.

**Consumer:** `src/dotnet/Apps/tests/PastyPropellant.ConsoleApp.Tests/ConstructPropellantJsonHelperTest.cs:22`
reads it as `"../../../../../data/propellants.json"` — a repo-root-relative path from the test's bin
directory. That test project is in the solution. The test is `[Trait("Category", "LongRunning")]`
and drives the RegionMapper → Thermodynamics → Porosity Python pipeline, using this file as the
*input* propellant set whose enriched output is written to `artifacts/output_construct/`.

Note the other test projects do **not** use this file — `ParametricCombustionModel.Core.Tests`,
`.Computation.Tests`, and `PorosityCalculation.Tests` each carry their **own** `data/propellants.json`
copy inside the test project, wired in via `<None Update="data/propellants.json">` in their csproj.
Only `ConsoleApp.Tests` reaches out to repo-root `data/`.

### `propellants.0.json` (13 KB), `propellants.1.json` (14 KB), `propellants.234.json` (39 KB)

Single-composition / subgroup slices of the same dataset: `Bas_0` alone; `Bas_1` alone;
`Bas_2`+`Bas_3`+`Bas_4`. Same schema family as `.01234` (all three have `pressure_frames`);
`.1` and `.234` additionally carry `confidence_intervals`.

**Consumers: none found.** They match the split described in `CLAUDE.md` ("the suffix selects which
set is loaded") — that mechanism is real in shape but no longer exercised, since the entry point
hardcodes `01234`. Best read as **per-composition fitting inputs kept for ad-hoc//historical runs**,
usable by pointing `WithPropellantsFromFile` at them.

---

## Credential file

### `telegram_settings.json` — 121 B — not required

> ⚠️ **Contains a live-shaped credential. Do not edit, copy, or paste this file casually, and do not
> quote its contents into logs, issues, PRs, or chat.**

JSON object with three string fields: `token`, `chat_id`, `timeout`. The `token` value matches the
Telegram bot-token shape exactly. It is **tracked in git and has been since the initial commit**, so
it exists in every clone and every history snapshot.

**Consumers: none found.** There is no Telegram integration in the current tree — the
`TelegramBot.Api` project lives under `externals/`, is not in `PastyPropellant.sln`, and nothing in
`src/dotnet` references it or reads this file. So the file is functionally inert *and* a live secret,
which is the worst of both.

**This is tracked separately as NEW-1 (High) in [tech-debt.md](tech-debt.md#new-1); the remediation
there is to revoke and reissue the token via BotFather.** Untracking or scrubbing history is not a
substitute while the token is still valid. Documenting the file here does not close NEW-1.

---

## Thermodynamics inputs (no consumer; duplicates of live files elsewhere)

The thermodynamics pipeline is real and runs — but it reads its inputs from
`src/python/RegionMapper/data/` and `externals/src/python/AerospacePropellantThermodynamics/data/`,
**not** from repo-root `data/`. The three files below are copies that were left behind.

### `propellant_components.json` — 856 B

JSON array of 4 component definitions — `CombustibleBinder`, `AmmoniumPerchlorate`, `Aluminum`,
`Octogen` — each with an elemental `composition` map and a formation `enthalpy` (J/kg).

**Consumers: none found.** It is **content-identical** to
`src/python/RegionMapper/data/components.json`, which *is* the live file — that is the path the
console-app helpers and `ConsoleApp.Tests` actually pass as `componentsFilePath`. Treat the `data/`
copy as a stale duplicate; the RegionMapper copy is authoritative.

### `thermodynamic_substances.json` — 21 KB

JSON array of substance records: `formula`, a 9-element `coefficients` array (Glushko-style
thermodynamic polynomial fit), `phase`, and a `temperature_range` `{min, max}`.

**Consumers: none found.** It is a **near-duplicate of**
`externals/src/python/AerospacePropellantThermodynamics/data/combustion_products.json` (the live
file). The coefficients are identical; the **only** difference across the whole file is
`temperature_range`: this copy says `1000–5000 K`, the live file says `293.15–6000 K`. So this is an
**older, narrower-range snapshot** — do not treat it as an alternative input, it is behind.

### `thermodynamic_params.json` — 159 B

Flat JSON object of 6 scalar gas-phase constants: `lambda_gas` (0.1 W/m·K), `average_molar_mass`
(32.7e-3 kg/mol), `R_gas_constant` (8.31), `T_kinetic_flame` (2350 K), `T_diffusion_flame` (3400 K),
`c_volume` (635.6 J/kg·K).

**Consumers: none found.** The values are physically meaningful and correspond to quantities the
combustion model does use, but the model gets them from elsewhere (propellant JSON fields and
hardcoded constants) rather than from this file. Best read as a **historical parameter card** from
earlier standalone thermodynamics tooling. Note `T_diffusion_flame = 3400 K` and
`T_kinetic_flame = 2350 K` here are *not* the temperatures under investigation on the current
experiment branches — do not use this file as a statement of current modelling assumptions.

---

## Unexplained / legacy

### `TAB.dat` (7,899 B) and `substances_file.json` (7,899 B) — **byte-identical**

Both files have the same MD5. **Despite its `.json` extension, `substances_file.json` is not JSON** —
both are the same whitespace-delimited fixed-format table:

```
 'O'  45.168916  58008.607  5353.7423  -412.44632  …
 'O2' 54.316387  -1836.437  5581.2917  2710.0305   …
```

One row per species: a quoted formula followed by 9 numeric coefficients. Cross-checking the numbers
against `thermodynamic_substances.json` confirms these are **the same Glushko thermodynamic
coefficients in their original tabular form** — `TAB.dat` is evidently the raw source table that was
later transcribed into the JSON record format used by the Python thermodynamics package.

**Consumers: none found** for either name. `substances_file.json` appears to be a copy of `TAB.dat`
made under a name a JSON-based loader would expect, for a loader that either never landed or was
replaced by the JSON transcription. Keeping `TAB.dat` has provenance value (it is the upstream
tabulation); `substances_file.json` is a redundant misnamed twin.

### `clients.json` — 95 B

JSON array with a single object: `{"token": "<uuid>", "name": "test"}`. The token is a plain UUIDv4,
not a bot token or an API key of a recognisable service, and `"name": "test"` reads as placeholder
data.

**Unknown — appears to be** an API-client allowlist for a service front-end (the shape matches a
"registered clients with access tokens" table). Nothing resembling such a service exists in this
repository. **Consumers: none found.** Low sensitivity given the placeholder name, but it is still a
token-shaped string in git; do not assume it is meaningless if the owner recognises it.

### `chemical_elements.json` — **0 bytes**

Empty file. The name implies a periodic-table / atomic-mass reference — a molar-mass table *does*
exist in the Python packages (DUP-2 in the tech-debt register notes it is duplicated between two of
them), so this was plausibly intended to be that shared table and was never populated.
**Unknown; empty. Consumers: none found.** As a zero-byte file it would fail any JSON parse, so
nothing can be reading it successfully.

### `hh.txt` — **0 bytes**

Empty file, no meaningful name. **Unknown; appears to be a stray** — a shell redirect artefact or a
scratch file committed by accident. **Consumers: none found.** Flagged for removal by the hygiene
item in [tech-debt.md](tech-debt.md); left in place here (this pass is documentation-only).

---

## Files that do *not* exist (despite being documented elsewhere)

`data/optimization_tickets.json` — referenced by [docs/README.md](README.md) and `QWEN.md` as the
job-definition file the console app loads at startup. **It does not exist**, and no code reads it.
The ticket/worker/named-pipe architecture those documents describe is not in the current tree; the
run is configured by hardcoded literals in `Program.cs`. This was established when DOC-1/DOC-2 were
closed in `91593f7` (which corrected `CLAUDE.md`); `docs/README.md` and `QWEN.md` still carry the
stale description and are out of scope for this pass.
