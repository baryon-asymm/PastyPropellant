# PropStructV3 reference runs

Golden-master fixtures for a port of **PropStructV3**, the Fortran-77 Monte-Carlo
microstructure model that produced two numbers `data/propellants*.json` currently
carries as hand-copied constants:

| model output | where it ends up |
|---|---|
| `Zkarm` — mass fraction of pockets in the binder-metal composition | `pocket_mass_fraction` (`Z_p`) |
| `Dkarm43` — mass-mean pocket diameter | the pocket size used by the `f_s` work |

The model source **is** in this repository, as of 2026-08-15: `PropStructv3.for`
next to this file (1776 lines, Compaq Visual Fortran, last modified 2015-03-18;
the shipped `.exe` was built the same minute), byte-identical to the copy in
`PropStructV3.zip`. The 49 input files are vendored too, under `inputs/`, each
with its SHA-256 recorded in `runs.json`. What is **not** here is the archive of
`.m` outputs — `runs.json` is their vendored form — nor the `.exe`.

## Regenerating

```bash
python reference/propstruct/parse_runs.py [ARCHIVE] --report
```

`ARCHIVE` is the zip or an unpacked directory; it defaults to
`/root/projects/PropStructV3/PropStructV3.zip`. Output is `runs.json` (~2 MB).
The script reads only the archive — it contains no physics and copies every
number out verbatim.

## Why each `.m` file is a fixture

Every run wrote a MATLAB `.m` file whose header records nearly all of the input:
densities, mass fractions, the fraction size bounds, and most tunable
coefficients. The `.dat` files cannot substitute for it — they were overwritten
between runs (`HPEPA10.dat` currently holds the x = 0.95 case, not the x = 0.10
one it is named for).

⚠ **Refined 2026-08-15: "cannot substitute" is now decided per run, not
wholesale.** Each `.m` header names its own input file, so the pairing is
recorded by the archive rather than guessed; schema `/3` parses that `.dat` into
`run.input_dat` and compares it with what the `.m` printed. **21 of the 43 still
agree; 20 have drifted; 2 name a file the archive no longer holds.** Where
`input_dat.agrees_with_m` is true the `.dat` is the better input record — it
carries the bounds as they were typed, while the `.m` prints them through `E9.3`
at three significant digits. Where it is false, `input_dat.disagreements` says
how (the `hp*` series were all re-pointed at 100 000 base particles afterwards,
`hp1801` and `r_p35050n` gained fractions, `res_02` had its distribution law
switched), and replaying that file would run a different case.

⚠ **Open the file by `input_dat.archive_file`, never by `input_dat.source_file`.**
The first is the name under `inputs/`; the second is how the `.m` header spells it,
and the two differ in case for 40 of the 41 pairings (`hpepa.dat` against
`HPEPA.dat`). DOS did not distinguish them, so the header's spelling is not wrong —
it just does not open on a case-sensitive filesystem. Both are recorded because
they answer different questions: what the run said it read, and where the bytes
are. `input_dat.sha256` is the digest of that file, and every one of the 41 matches
its vendored copy.

This matters because the three-significant-digit ceiling was the stated reason a
bit-exact replay of an archived run was thought impossible. For 21 runs it no
longer applies: `rnano2`'s widest bound is `314.6612 µm` in the `.dat` against
`0.315E-03` in the `.m`, and the `.dat` value is the one that reproduces the
`F7.2` `dok_max` of `314.66`.

⚠ **Two inputs are missing from the header.** `eta`, the agglomerated-oxide
share, is recovered by inverting `da_coef` (below). And `AK1`–`AK4` — the
size-ratio window `0.5 … 2.0`, the pocket-versus-bridge threshold `0.27` and the
reset radius `4.7` — are read from the `.dat` and printed **nowhere**. They are
the constants that *define* what a pocket is, and they also size the histograms.
They happen to be identical across all 49 archived `.dat` files, so the fixtures
remain usable, but a `.m` is not a self-contained pair. Note the values quoted
inside the rejection-counter labels (`3) Dbase < 0.5Dok`) are hardcoded text and
would keep printing `0.5` whatever `AK1` was.

This is the same discipline as `run_configuration.resolved.json` in the main
host: the record of what a run actually used travels with the run's output.

## What is in `runs.json`

`43` runs, each with:

- `input` — densities, oxidiser/metal mass fractions, the fraction list
  (`mass_fraction`, `d_min_m`, `d_max_m`), distribution law, particle count,
  generator selection.
- `parameters` — `d_min_um` / `di_um` / `dj_um`, the pocket-to-bridge ratio
  window, `k5`, `k7`, `k8`, `eps`, `calculation_variant`, `p_alpha`,
  homogenised-oxidiser share.
- `expected.scalars` — 51 values: the exact integer counters (`nkarm`, `nfx`,
  `nfy`, `nfq`, `nfw`), the six `epsx` generator statistics, the local-structure
  means, all oxidiser sizes, all four pocket-size variants with their standard
  deviations, `zkarm` plus both corrections, `da_coef`, all four agglomerate
  sizes, and the two bridge sizes.
- `expected.conditions_per_particle` — the nine rejection counters. ⚠ **They
  carry two different normalisations**: the model divides counters 1–5 by
  `FI + N` and counters 6–9 by `FI`. Since `FI = N` in every archived run, the
  first five are per *attempted* base particle and the last four per *accepted*
  one, and the two differ by exactly a factor 2. Counters 1–5 also accumulate in
  both passes while 6–9 accumulate only in the second. Treating all nine alike
  puts four of them out by 2×.
- `expected.arrays` — 14 distributions, point by point (`fmdok`, `fmkarm`,
  `fmkarm_cor`, `fmkarm_cor2`, `fqkarm`, `fqkarm_cor`, `fqmkm1`, `fqmkm2`,
  `coef`, `pdoksmall`, `epsdokfr`, `Dkarmcat`, `dokkarm43`, `dokkarm10`).
- `expected.array_steps` — the abscissa step of each distribution. Two families:
  the six size distributions are densities (`Σ × step = 1`) binned on `Di` by
  construction in the source, while `fqmkm1`, `fqmkm2` and `coef` are per-bin
  probabilities that sum to 1 despite being labelled "density distribution
  function". ⚠ **The model mislabels one of them**: `fqmkm1`'s header announces
  `step = 0.01`, but the source bins it at `int(ratio·1000)+1` and averages with
  a 0.001 weight. The source value is recorded; the printed one is kept under
  `fqmkm1_as_printed`. That is the one array where the axis could not have been
  guessed — and the printed comment would have got it wrong by 10×.
- `expected.conditional_dok` — the oxidiser size distribution per pocket size
  category, one row per category.
- `derived` — our interpretation, not data from the file. See below.
- `self_check` — findings from checks the parser runs on every record.

## Excluded runs

`32` files are skipped, all `r_*` / `rc_*` written in 2013 by an **earlier
build**: different array names (`fmdokkarm`, `Dok43karm`, `EPSMdok`), `Zkarm`
printed as a vector, and 448 `NaN` values between them. That build's source is
not in the archive, so those files would test a program we do not have.
`r_kb396.m` is empty. The reason is recorded per file in `skipped`.

Every accepted run post-dates the final source edit, so all 43 come from the
one build whose source we can port.

## The Bas_* family

`derived.bas_composition` marks runs whose fraction set matches a composition in
`data/propellants.01234.json`. The oxidiser there is AP + HMX: the 160–315 µm
fraction at mass 0.486 is the HMX, the remaining 0.514 is AP, and
`derived.fine_ap_mass_share_x` is the fine share of that AP.

| run | composition | `Zkarm` | `Dkarm43` | in `propellants.01234.json` |
|---|---|---|---|---|
| `hp180` | Bas_4 (x = 0) | 0.5600 | 143.20 | `pocket_mass_fraction` 0.56 |
| `hp1`, `r1` | Bas_2 (x = 0.35) | 0.7022 | 104.57 | 0.70 — also Bas_0 and Bas_1, same recipe |
| `hp1050` | Bas_3 (x = 1) | 0.5917 | 222.28 | 0.57 |

⚠ **Only Bas_4 is reproduced by this archive.** Bas_2's published 104.67 µm
matches no branch of `hp1`: uncorrected 104.57, correction 1 104.76, correction 2
206.50. The gap of 0.10 µm is ten times the `F7.2` print granularity, so it is
not rounding. By the same standard Bas_3's published 91.87 matches nothing
either — `hp1050`'s nearest branch is *corrected variant 2* at 91.74, and 0.13 µm
is thirteen times the granularity. Only Bas_4 lands on a printed value exactly
(143.19 against 143.20). So the runs behind two of the three published numbers
are not in this archive, and the nearest branches for those two are not even the
same branch: uncorrected for Bas_2, corrected-variant-2 for Bas_3, which differ
by a factor 2.4 in pocket size. A second Bas_3-like run (`results`, at
0.515/0.485 instead of 0.514/0.486) gives 96.48 at the same particle count,
5.2 % away, which is the scale of the model's own sensitivity here.

So anything built on the three published diameters is mixing two branches of the
program plus one number of unknown origin. Unresolved — flagged so a port can
settle it by experiment rather than by reading `goto` statements.

Runs `hp2 … hp95` sweep x from 0.02 to 0.95 on the Bas recipe. `hp315*`, `r2`
use a variant input that merges coarse AP and HMX into one 113–315 µm fraction,
so no x is derived for them.

## Findings

Recorded rather than corrected — a port must reproduce the model, not improve it.

- `r_p35050n`, `r_p35050nn`: fraction masses sum to **1.1000**, and
  `rc166`, `rs_c166`: to **0.9885**. The model renormalises internally
  (`PARAM` divides by the sum) and accepts these silently.
- **Exactly one element of `pdoksmall` is uninitialised.** The fill loop starts
  at index 2, so `pdoksmall(1)` is whatever was in memory; every further
  anomalous entry is that same value carried forward by the monotone step that
  propagates the running maximum over the leading zeros. Recorded as
  `derived.pdoksmall_uninitialised_value` with the propagated count (2 elements
  in most runs, 10, 22, and the whole array in `rnano2` — 250 of 1450 points).

  In this archive the leftover word is ~10⁻³⁸ and **numerically inert**: the
  three duplicate runs carry three *different* leftovers (7.78 × 10⁻³⁹,
  1.03 × 10⁻³⁸, 7.94 × 10⁻³⁹) and agree in all 51 scalars and all 13 other
  arrays. So a port may zero it. What it must not do is assume that is safe in
  general — a leftover large enough to satisfy the `Dmaxxx` test would change
  the run, and no fixture here would catch it.

  An earlier version of this note claimed the opposite. It was wrong: the
  duplicate runs recorded in this same file refute it.
- `eta`, the agglomerated-oxide share, is a model input that the output file
  **does not record**, although it moves `Dagg43`. It is recovered here by
  inverting `da_coef`, which is monotone in `eta`
  (`derived.recovered_eta`, with the residual). It is 0 in 42 of the 43 runs
  and 0.5 in `p777out1`. Without this, a run is not reproducible from its own
  header.
- Reproducing `da_coef` requires the Fortran expression **literally**: `3.14159`
  rather than π, and the exponent `0.3333` rather than `1/3`. Using the exact
  values shifts the recovered `eta` by 0.002. The same caution applies
  throughout the port.
- Three pairs of runs are identical in every scalar: `p777out` ≡ `p777out2` ≡
  `res_01`, `hp1` ≡ `r1`, `rc166` ≡ `rs_c166` (flagged as `duplicate_of`).

## Self-checks

`981` checks over the 43 runs, `4` findings (the mass sums above).

⚠ **These validate the parser, not the model. Do not count them as coverage.**
Every one of them is satisfiable without simulating anything: the mass sums and
bounds read back the input header; `Dagg = Dkarm × da_coef` holds by
construction in any implementation that computes `Dagg` that way; the
normalisations only confirm that a density was printed as a density; the length
checks test our own row splitting. Their job is to catch a regex that silently
matched the wrong line. Not one of them constrains a physical value.

What they check:

- fraction masses sum to 1 and each fraction's bounds increase;
- `Dagg43 = Dkarm43 × da_coef` for all four variants;
- each size distribution integrates to 1 over **its own printed step**, and
  `fqmkm1`/`fqmkm2`/`coef` sum to 1;
- `Zkarm ∈ [0, 1]`;
- `Dkarmcat`, `dokkarm43`, `dokkarm10` and the conditional rows all have one
  entry per pocket size category.

## What these fixtures do not constrain

Measured across all 43 runs, not estimated. Twelve tunables carry **exactly one
value each**, so a port that hardcodes them passes every fixture:

| frozen at | in |
|---|---|
| `Dmin = Di = Dj = 10 µm` | 43/43 — their three roles are algebraically indistinguishable on this data |
| ratio window `3 … 100`, `k5 = 0.25`, `k7 = 8.2`, `k8 = 7.73`, `eps = 0.05` | 43/43 |
| `P(alpha) = 0`, `Zok* = 0`, calculation variant `0` | 43/43 |
| `Cycles = 1`, generator `GSV = 2` | 43/43 |

Consequently the following are exercised by **no** fixture and must be ported as
explicitly unverified: the cycle loop (`KXX > 1`), the statistical-significance
and convergence machinery, the homogenised-oxidiser path, the first generator,
the retry-policy variant, and any histogram step other than 10 µm. Two branches
are only just covered — the alternate size-distribution law by `r_p35050n` and
`res_02`, the fraction mask and the fine-oxidiser path by `rps01` and `rps02`.

Some **outputs** are equally unconstrained, which is the more dangerous case
because they look tested:

- `zkarm_cor1` equals `zkarm` exactly in 38 of 43 runs and differs materially in
  two (`r_p35050n`: 0.4993 → 0.1249; `rps01`). A stub returning `zkarm` passes
  41 runs.
- `dkarm43_v2` equals `dkarm43_v1` in 30 runs. Of the 13 that differ, eleven
  differ by exactly 0.01 µm and two (`hp95`, `results`) by 0.02 µm. So a stub
  returning `v1` is caught by **13 runs out of 43** — better than it first looks,
  but still a discrimination that rests on the last printed digit. `dagg43_v2` is
  worse: equal in 34, and every one of the other nine differs by a single unit in
  that digit.

  ⚠ Corrected 2026-08-15. This paragraph previously read the `F7.2` tolerance as
  ±0.01 µm and concluded that a `v1` stub «sits exactly on the tolerance» in
  eleven runs. The tolerance is ±0.005 µm — half a unit in the last printed place,
  which is the most a correctly rounded print can hide — so a 0.01 µm discrepancy
  is twice the tolerance and fails.
- `dok_above_dmax` is 0.0 in 43/43, so deleting that rejection test costs
  nothing.

Finally, the tolerance is far tighter than the model's own **convergence**. (Not
its repeatability: the seeds are literals in the source, so a rerun is exact —
the duplicate runs prove it.) The same recipe at 100 k versus 500 k base
particles moves `Zkarm` by 1.8 %, and a 0.2 % shift in the mass split moves
`Dkarm43` by 3.7 %. Matching to ±0.005 µm asserts that the port draws the **same
random sequence**; it says nothing about whether the physics is right.

Also frozen and not listed above because the `.m` never records them:
`AK1 = 0.5`, `AK2 = 2.0`, `AK3 = 0.27`, `AK4 = 4.7` and `alfa = 0` — the last of
which means the `Dokb_max` output is printed by **no** archived run. And there
is a *third* generator (`gsv = 3`, system-seeded and non-reproducible), so two
of the three are unexercised, not one.

## The original runs here, and that changes what "reference" means

Established 2026-08-15. `PropStructV3.exe` is a 32-bit console PE; **wine 9.0
runs it**, and it reproduces an archived run exactly.

```bash
sudo dpkg --add-architecture i386 && sudo apt-get install wine64 wine32:i386
cd <scratch dir with the .exe, dforrt.dll and the .dat files>
printf "nano2\ny\n" | wine PropStructV3.exe 2>&1 | tail -3
```

⚠ Two mechanical points. Run it in a **copy** — the program writes `results.m`
into the working directory and the archived `.m` files are the reference, so a
run in the archive directory would overwrite them. And do **not** redirect stdout
to a file: the program writes to `CONOUT$`, which under wine gives
`forrtl: severe (38)` on a file handle. A pipe (`| tee`, `| tail`) is fine.

Verified on two runs whose `.dat` still agrees with their `.m`:

| run | result |
|---|---|
| `rnano2` | byte-identical except `pdoksmall` and the wall-clock line |
| `hp315` | byte-identical except `pdoksmall`, the wall-clock line, and the case of the echoed filename |

Every computed number matched — all counters, all scalars, all other arrays.
That settles three things at once. The archived runs were made with the default
parameter set (answer `y` at the prompt). `pdoksmall` really is uninitialised
memory, not a computed quantity: two runs of the same input on the same machine
gave `0.569E-38` and `0.872E-38` while agreeing everywhere else — independent
confirmation of the `known_quirks` entry. And a port no longer has only 43 frozen
pairs to check against: any input can be turned into a reference run on demand.

What the oracle does **not** give for free is the interactive settings. `rps01`
and `rps02` do not reproduce under the default answers, because they exercise the
fraction mask and the fine-oxidiser path, which are menu options rather than
`.dat` fields. Their `.m` header does print the resulting parameter block, so the
menu can be driven to match — that just has not been done yet.

### Adding a fixture for an uncovered branch

Decided 2026-08-15: the branches no archived run exercises — `KXX > 1`,
`ivar != 0`, `SFR` beyond the two runs that set it, `eta` other than 0 and 0.5,
`Dmin != Di`, `gsv = 1` — are closed with **real runs**, not with metamorphic
invariants standing in for a reference. The port's `BOOT.md` carries the list;
this is the procedure.

1. Write the `.dat` for the case into a scratch directory — never into
   `/root/projects/PropStructV3/`, where the program overwrites `results.m`.
2. Run the executable under wine with stdout through a **pipe**. A file redirect
   fails with `forrtl: severe (38) ... CONOUT$`.
3. Answer the menu. Default answers (`y`) reproduce the archive; a branch that
   lives in the menu rather than in the `.dat` needs the answers driven
   explicitly, and which ones were given must be written down — the `.m` does not
   record them.
4. Copy the `.dat` into `inputs/` and the `.m` next to the archived ones, then
   regenerate with `parse_runs.py`. The parser is the only thing that may write
   `runs.json`; a hand-edited fixture is not a fixture.
5. Check the diff: only the new run should appear. `source_written` timestamps
   come from the archive's own mtimes, so regenerating from the unpacked
   directory instead of the zip rewrites all 43 of them.

⚠ Two things do not carry over from an archived run and must be stated with the
new one: `pdoksmall` is uninitialised memory and differs between two runs of the
same input, and the wall-clock line is not a result. Neither may be asserted on.

⚠ A generated fixture is evidence about the **executable**, not about the source.
They agree here — that is what the two verification runs above establish — but a
fixture generated under wine and a fixture from 2015 are not the same kind of
claim, and a new one should say in its own entry when and how it was made.

## The six random streams are one stream

The model declares six generator states and the output prints six independent
accuracy statistics, which makes them look like six streams. They are one orbit
of the same generator at consecutive offsets:

```
stream 6: 0.7453298338 0.4466296923 0.9274695223 0.6342372634 ...
stream 5: 0.4466296923 0.9274695223 0.6342372634 0.6361388488 ...
stream 4: 0.9274695223 0.6342372634 0.6361388488 0.6251982045 ...
```

Stream 6 is seeded with the identity vector, and one step from the identity
yields exactly the multiplier — which is stream 5's seed. Streams 6→5, 5→4 and
2→1 are bit-identical under a one-draw shift; stream 3 sits on the same orbit
displaced by a constant `36 × 2⁻²⁴`, which reads as a typo in one seed word.

Because the base particle's fraction and position variates are drawn
back-to-back from streams 1 and 2, `X0(i) = X(i−1)` **exactly, for every
particle**; the same holds for the surrounding particle's fraction and position.
The pair therefore walks the generator's lag-1 lattice instead of filling the
square. The built-in `epsx` diagnostic cannot see this by construction — it
averages each stream separately, and each is individually well-behaved, which is
why two of them print identical values.

The signature is visible in the output: **8 of the 43 runs print two `epsx`
values that are bit-identical** — usually the third and fourth (`hp315`, `r2`,
`r3`, `r_p18050`, `r_p35050nn`, `res_02`, `rps01`), twice the first and second
(`r_p18050`, `rnano2`). Note also that the model computes seven statistics and
prints six: `EPS5` is dropped, and the fifth and sixth printed values are two
statistics on the **same** state, since `X3` and `X4` share it.

A port must reproduce this exactly. Replacing the six states with six genuinely
independent streams is a different model.

## Suggested use by a port

Check in this order — the first group either matches exactly or the port has
diverged, and nothing after it is worth reading:

1. **Integer counters**: `nkarm`, `nfx`, `nfy`, `nfq`, `nfw`, `cycles_done`.
   `nfy` reaches 3.4 × 10⁸ in `hp1` and breaks on any difference in the
   rejection logic or in the order of the checks, which makes it the sharpest
   test available. Hold them in `long`, as the original does — they are declared
   `INTEGER*8` at line 9, so the original does **not** overflow; only a port that
   reaches for `int` would. The largest `nfy` in the fixtures is 1 028 207 857,
   47.9 % of the `int32` range, and the pre-2015 files in the archive reach
   6.06 × 10⁹ (`r_a4.m`) — past `int32` and past `uint32` too.
2. `conditions_per_particle` — the nine rejection counters.
3. Scalars. Sizes are printed at `F7.2`, so the tolerance is ±0.005 µm absolute —
   half a unit in the last printed place, not a whole one;
   values written by list-directed output carry ~7 significant digits.
   `Dqmkm1`/`Dqmkm2` are `F6.4`.
4. Arrays, point by point. `arrayprint` writes `E9.3` — **three** significant
   digits — so half a unit in the last place is up to 5 × 10⁻³ relative for a
   mantissa beginning with 1. A 10⁻³ relative tolerance is tighter than the
   printed precision allows and will produce spurious failures.

Two notes on comparing:

- Reproduce the model's **rounding**, not only its arithmetic. `epsx` is
  computed through an undeclared variable that is implicitly `REAL*4`, so the
  mean is rounded to single precision before the statistic is formed. Rounding
  the same way turns a mismatch in the 5th digit into an exact match in all
  seven printed ones.
- **Bit-exact agreement is the target.** An earlier version of this note said the
  opposite, on the assumption that the original carried 80-bit x87
  intermediates. It did not: the Compaq Visual Fortran runtime sets the x87
  precision control to 53 bits at startup (`_controlfp` is imported by the
  shipped executable, and the Release build carries no `/fltconsistency` or
  `/Op` to change the default). Two consequences:
  - `REAL*8` is plain IEEE-754 double, so the double-precision half of the model
    is reproducible exactly;
  - `REAL*4` is double-rounded 53→24, which is provably innocuous for `+ − × ÷ √`
    when the intermediate carries at least `2p+2 = 50` bits. A single operation
    stored to a `REAL*4` therefore lands on exactly the same value as C# `float`
    arithmetic.

  The one place the two diverge is a **multi-operation `REAL*4` expression held
  in a register**: Fortran evaluates the whole chain at 53 bits and rounds once
  on assignment, whereas `float` arithmetic rounds after every operation. So the
  faithful rule is *evaluate the right-hand side in `double`, round to `float`
  when assigning to a variable declared `REAL*4`* — the split follows Fortran's
  assignment statements, not its declarations. This matters for expressions like
  `AA = rrl - Dr/2. - Db/2.`, a cancelling difference that the `AK3`/`AK4`
  thresholds then classify as pocket or bridge: a one-ulp difference changes a
  branch, not a digit.

  ⚠ **This rule is necessary but not sufficient.** The model calls `dlog` once
  per accepted particle and uses real-valued exponents throughout (`**3.`,
  `**4.`, `**0.5`, `**0.3333`, `**(1/3.)`). Those route through the Compaq
  runtime's math library, which is not IEEE-specified, and .NET's `Math.Log` /
  `Math.Pow` will differ from it in the last place. `dlog` feeds the radial walk
  and therefore `AA` — the cancelling difference that the `AK3`/`AK4` thresholds
  classify — so a one-ulp difference there changes a branch and moves the very
  integer counters nominated above as the sharpest test. Bit-exactness is the
  right target, but reaching it may require matching the transcendentals, and
  whether that is achievable can only be settled by running a port against the
  original. Treat the counters as the experiment, not as a foregone conclusion.

  The random stream is exactly reproducible independently of all this. The state
  update is integer-only with its seeds written into the source; the returned
  value is a 10-term dot product with powers of two, so each product is exact —
  but a port must preserve the summation order.
