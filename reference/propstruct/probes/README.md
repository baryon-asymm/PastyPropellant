# probes/

Runs generated on demand under wine, as opposed to the 43 runs recovered from the
archive. They are **not** part of `runs.json` and `parse_runs.py` does not read this
directory: `runs.json` describes what was found, and a probe is something we made.

A probe exists to localise a disagreement. The cheapest archived run draws about ten
million numbers, so when a port's counter comes out one per cent off, the archive can
say only «somewhere». Twenty particles can be followed by hand.

| file | what it is |
|---|---|
| `PROBE.dat` | input, written for this purpose: `hp2`'s recipe as its report prints it, at N = 20 |
| `probe.m` | what the original wrote for it under wine 9.0 |
| `PROBE3.dat` | the same input with one column changed: `KXX` from `0` to `2` |
| `probe3.m` | what the original wrote for **that**, same wine, same session |
| `PROBE4.dat` | the same input with `GSV` changed from `2` to `1` — the toy generator |
| `probe4.m` | its report |
| `PROBE5.dat` | the same input with `JZZ` changed from `1` to `2` — the second size law |
| `probe5.m` | its report |
| `probe6.m` + `probe6.answers.txt` | `PROBE.dat` run with `ivar = 1` |
| `probe7.m` + `probe7.answers.txt` | `PROBE.dat` run with `Dmin = 60 µm` — ⚠ the port refuses this input; see below |
| `PROBE8.dat` + `probe8.m` + `probe8.answers.txt` | `SFR = [1 1 0]` — the coarsest fraction forms no pockets |
| `probe9.m` + `probe9.answers.txt` | `PROBE.dat` run with `eta = 0.25` |

Probes 3–5 differ from `PROBE.dat` by exactly one column, so any difference between
their reports is attributable to that column and nothing else.

⚠ **Probes 6–9 are not a file.** `ivar`, `Dmin`, `SFR` and `eta` sit behind the *«use
default parameters?»* prompt, so the input is `PROBE.dat` **plus a fixed sequence of
answers**, kept verbatim in `probeN.answers.txt`. `probe8` needs both halves: the two
extra lines in `PROBE8.dat` are read only after `[12]` is answered `y`. Regenerate with

```bash
printf "$(cat probe6.answers.txt)" | wine PropStructV3.exe    # answers already newline-separated
```

⚠⚠ **The answer sequences were wrong until 2026-08-16 and are now verified by
regeneration.** Each of `probe6/7/8.answers.txt` carried a spurious `y` after the file
name. There are only two prompts before the menu — the file name and *«use default
parameters?»* — so that `y` accepted the defaults and skipped the menu entirely; the
remaining answers went nowhere. The **reports are genuine**; it was the recipe for
re-making them that was wrong, and nothing caught it because the tests compared the
answers file to a literal rather than running it.

It surfaced while generating `probe9`: the recorded pattern with `14` and `0.25`
produced a report byte-identical to `probe.m`, which cannot be right for a setting that
moves five numbers. All four sequences have since been re-run under wine and each
reproduces its file **byte for byte**. The shape is

```
<input file>
n            <- never y: a run on the defaults is probe.m
<menu item>
<value>
0            <- start
```

## Condition coverage

Between them the probes exercise eight of the nine rejection conditions. Before them,
the archive exercised conditions 3, 4, 5 and 8 and nothing else.

| condition | first fixture where it is not zero |
|---|---|
| 1) `Dok > Dmax` | **none** — cannot fire while fractions are sampled inside their own bounds |
| 2) `Dok < Dmin` | `probe7.m` (16 249) — ⚠ oracle only, the port refuses that input |
| 3) `Dbase < 0.5Dok` | every run |
| 4) `Dbase > 2.0Dok` | every run |
| 5) `l > 4.7Dbase` | every run except `probe7` |
| 6) `Nkarm = 0` | `probe4.m` (1) |
| 7) `Nmkm < 2` | `probe8.m` (5) |
| 8) `Nkarm/Nmkm < min` | every run |
| 9) `Nkarm/Nmkm > max` | `probe8.m` (2) |

## How `probe.m` was made

```bash
cd <scratch copy holding PropStructV3.exe, dforrt.dll and PROBE.dat>
printf "probe\ny\ny\n" | wine PropStructV3.exe 2>&1 | tail -40
```

Three answers, not two: the file name, then *rewrite the existing output?*, then
*use default parameters?*. Output goes to `results.m`, which is the file copied here.
Do not redirect stdout to a file — the program writes to `CONOUT$` and a file handle
gives `forrtl: severe (38)`. See the parent `README.md`, «The original runs here».

## What it pins

`tests/…/ProbeRun.cs` asserts every integer this run printed, and all of them are
reproduced exactly by the port: `NFX = 915`, `NFY = NFZ = 21788`, `NFQ = 694`,
`NFW = 3196`, 3263 pockets, and conditions `3) 633`, `4) 16780`, `5) 198`, `8) 44`
with the other five at zero.

⚠ **Two of those numbers come from the console, not from `probe.m`.** The console
prints the condition counters raw; the file divides them. The file shows `5) 4.95`,
the console `198`, and 198 = 4.95 × 40. That arithmetic is an independent check that
the first five conditions are normalised on **both** passes and the last four only on
the working one. It is worth keeping because the port's own reading of the divisors
was wrong twice before this run existed.

## `probe3.m` — the multi-pass branch

`Cycles` is `1` in all 43 archived runs, and the original normalises `≤ 1` to a single
working pass. So the entire accumulate-across-passes path — which is most of the
summary — had no oracle at all. `PROBE3.dat` is `PROBE.dat` with the `KXX` column
changed from `0` to `2` and nothing else, so every difference between the two reports
is attributable to the pass count alone.

It was made the same way, answering `PROBE3` instead of `probe`.

**What it settles.** The first five conditions are divided by `FI + N` and the last
four by `FI`. On a one-pass run `FI = N`, so `FI + N` and `2N` are the same number and
no archived run — not one of the 43 — can tell the two readings apart. Here
`Nbase = 20 + 40`, so `FI = 40` and `N = 20`:

| | raw (console) | printed (`probe3.m`) | ÷ (FI+N) = 60 | ÷ 2N = 40 |
|---|---|---|---|---|
| 3) `Dbase < 0.5Dok` | 951 | 15.85000 | **15.85** | 23.775 |
| 4) `Dbase > 2.0Dok` | 25598 | 426.6333 | **426.633** | 639.95 |
| 5) `l > 4.7Dbase` | 287 | 4.783333 | **4.7833** | 7.175 |
| 8) `Nkarm/Nmkm < min` | 74 | 1.850000 | 1.2333 | **1.85** (= ÷ FI) |

So the divisor is `FI + N`, and condition 8's is `FI`. A port can be wrong about this
and still pass against the whole archive.

**What it also confirms.** At `KXX > 1` the original prints six per-pass arrays, and
the first entries of two of them are `epsfkarm_n(1) = 0.144206` and
`epszkarm_n(1) = 0.605802` — which are `probe.m`'s `epsfkarm = 0.1442056` and
`Zkarm = 0.6058023`, to every digit `E9.3` keeps. Pass 1 of the two-pass run **is** the
one-pass run. Since these are two independent wine runs, that also confirms the fixture
itself. Its counters: `NFX = 1372`, `NFY = 33270`, `NFQ = 1219`, `NFW = 5453`,
`Nkarm = 5001`.

**What it unblocks.** The `*_n` arrays are the last of the original's fourteen to be
reproduced, and this file is the only oracle they will ever have — `Results/API.md`
made porting them conditional on a fixture existing, not on anyone wanting the
numbers. All six are now checked, both passes, in `ProbeRun3`:

| array | pass 1 | pass 2 | what it is |
|---|---|---|---|
| `epsfkarm_n` | 0.144206 | 0.111264 | sampling error of the pocket histogram |
| `epsm3karm_n` | 0.0843818 | 0.0655596 | of its third moment |
| `epsm4karm_n` | 0.116940 | 0.0898979 | of its fourth |
| `epsdok43_n` | 0.00490468 | 0.000465182 | oxidiser mean **against the recipe**, signed |
| `epsdoksd_n` | 0.0306973 | 0.0235990 | its **variance**, signed |
| `epszkarm_n` | 0.605802 | 0.586985 | the pocket mass fraction — **not an error** |

⚠ Three quantities, six names all ending `eps…_n`. Read the middle column of the
table before comparing any two rows, and `Results/API.md` before quoting one.

⚠ `epsdoksd_n` is the one number in the archive **printed to more digits than it
has**. It cancels twice — three per cent at the top, elevenfold inside `ALLDOKsd` —
so one last bit of the `REAL*4` `ALLDOK243` moves it by 1.36e-6 absolute, 5.8e-5
relative, against the 5e-6 `E12.6` implies. The port lands 0.58 and 0.93 of that bit
away. It also makes this file the **only** fixture that constrains `ALLDOKsd` at all:
nothing else in the report reads it.

## `probe4.m` — the toy generator (`gsv = 1`)

`RANDOM1` is four lines: `random1 = A + B - int(A+B)`, then `B = A`, `A = random1`. A
lag-2 Fibonacci recurrence in single precision. Its six states are cut from **one**
orbit at offsets 0, NNZ, …, 5·NNZ and then assigned **backwards** — `A6 = AX(1)` down
to `A1 = AX(6)` — so the base particle's fraction is drawn from the most advanced state
and the pocket size from the raw seed `0.12345678 / 0.87654321`.

**This fixture found a real defect.** Before it existed the kernel built the `Random2`
stream set unconditionally, so a run configured for `gsv = 1` got `gsv = 2`'s numbers
while the resolved record dutifully recorded the request. Nothing could have caught it:
there was no `gsv = 1` output anywhere to compare against. Counters:
`NFX = 866`, `NFY = 24987`, `NFQ = 815`, `NFW = 3533`, `Nkarm = 3560`.

It is also the only probe where **condition 6** (`Nkarm = 0`) fires — once in twenty
particles, printed as `0.05 = 1/FI`.

⚠ **The report misprints `NNZ`.** Line 1199 writes it through an `E9.3` edit descriptor
although implicit typing makes `NNZ` an INTEGER, so 6 000 000 prints as `0.841E-38` —
the integer's own bit pattern read as a denormal REAL*4. The value a run used is in its
`.dat`, never in its report. Under `gsv = 2` this costs nothing, because there `NNZ` is
read and never used again.

`gsv = 3` is **not** probed and will not be. It calls `random_seed` with no argument
(line 409), so the original does not reproduce such a run twice either; the port refuses
it rather than return numbers no oracle can adjudicate.

## `probe5.m` — the second size law (`JZ = 2`)

`JZ` sets both halves of one inversion — the formula `SIZE` uses to turn a variate into
a diameter, and the weight `PARAM` uses to turn a mass share into a number share — and
the second is the inverse third moment of the first. A disagreement between them is what
L1 checks; this checks that the pair is the original's pair. The analytic mean moves
from 189.57 to 204.66 µm on the same recipe, which is the evidence the law reached
`PARAM` and not only `SIZE`. Counters: `NFX = 314`, `NFY = 7255`, `NFQ = 570`,
`NFW = 1883`, `Nkarm = 1821`.

⚠ **Under this law the original prints `Dok43sd = NaN`, and that is a defect in the
original.** `DOKSD` is accumulated identically under both laws (lines 802 and 900) as
`Σ Gdok·(D₁² + D₁D₂ + D₂²)/3` — the second moment of a distribution uniform in `D`.
Line 903 then subtracts `DOKM²`, and under `JZ = 2` `DOKM` is not that distribution's
mean but `0.8·DOK4/DOK3`. On this recipe the accumulator is ≈ 39 400 µm² against
`DOKM²` ≈ 41 900, so the variance is negative and `DOKSD**0.5` is `NaN`. Two different
means are being subtracted from one second moment; it is not a rounding artefact. The
port reproduces the `NaN` — repairing it would make the port disagree with the program
it exists to reproduce — but **nothing downstream may consume `Dok43sd` under `JZ = 2`**.

## `probe6.m` — the other calculation variant (`ivar = 1`)

Condition 3 (`Dbase < 0.5·Dok`) either restarts the whole base particle (`ivar = 0`, all
43 archived runs) or discards only the neighbour (anything else). The original tests
`ivar` for zero and nothing more — line 549 — so every non-zero value is one branch.

The counters state the difference in arithmetic: condition 3 fires **more** often here
(747 against `probe.m`'s 633) while the run draws **far fewer** numbers (`NFX` 386
against 915). Restarting throws away every draw already spent on the base particle and
re-fires the condition on its replacement; discarding the neighbour keeps the particle
and retries around it. Counters: `NFX = 386`, `NFY = 14990`, `NFQ = 240`, `NFW = 1923`,
`Nkarm = 2525`.

## `probe7.m` — a real minimum particle size (`Dmin = 60 µm`)

Every archived run and every other probe sets `Dmin = Di = 10 µm`, which is the lower
bound of the finest fraction — so condition 2 never fires and no mass is ever diverted.
Raising `Dmin` over the whole 10–50 µm fraction turns on two paths at once: condition 2
fires 16 249 times, and `fineoxy_fr` comes out `1.0191291E-02` instead of exactly zero.

⚠ That second one matters more than it looks. `fine_oxidiser_fraction` is
`gdokns + Gdokleft/GGG`, and it is zero in **41 of the 43** archived runs. The two
exceptions are `rps01` (0.1566) and `rps02` (0.7064), both by way of `SFR` rather than
of `Dmin` — this probe is the only reference for the `Dok < Dmin` route into the same
quantity. `da_coef` also moves, 0.7389501 → 0.7360483, which is the evidence the
diversion reached it.

⚠⚠ **The port refuses this configuration and does not replay it.**
`StructureConfigurationValidator` rejects `Dmin > Di` because `pdoksmall` holds the
below-cutoff share *per histogram cell*, so a cutoff spanning more than one cell has
nowhere to be recorded. This file is the evidence that the guard is right rather than
merely cautious: the original runs and prints, but `pdoksmall` opens with **22 cells of
`0.569E-38`** — uninitialised memory it never wrote — against 2 in `probe.m`. So the
choice is between a refusal and a run whose 51 scalars and 13 arrays are sound while
the fourteenth is unusable. The refusal is what ships; the numbers stay here so the
other choice can be made on evidence, not on re-derivation. See `ProbeRun7`.

## `probe8.m` — a fraction that forms no pockets (`SFR = [1 1 0]`)

Two archived runs carry a non-trivial `SFR`, so the flag is not new; being small enough
to trace is. It lights up the last two dark conditions — **7** (`Nmkm < 2`, five times)
and **9** (`Nkarm/Nmkm > max`, twice) — and it checks the diverted-mass path at a round
number: `fineoxy_fr = 0.4860000`, which is `Gfr(3)` exactly. `da_coef` falls to
0.6363022, its largest move in any probe, as it should when a third of the oxidiser
leaves the pockets.

Its input is `PROBE8.dat` — `PROBE.dat` plus a caption line and `1 1 0` — **and** the
answer `y` to menu item `[12]`, without which those two lines are never read.

## `probe9.m` — the agglomerated-oxide share (`eta = 0.25`)

`eta` is the last setting the port carried with nothing behind it. It is not a branch:
it enters one formula (lines 1015–1016) that scales the agglomerate mass, and reaches
only `da_coef` and the four `Dagg43` variants. The archive holds two values, 0 and 0.5,
but **the report never prints `eta`**, so those two are not readable from the files —
they come from the run notes. Two points also fit almost anything here, since the
formula puts `eta` in three places inside a cube root of a ratio.

This probe is the third point, taken off the ends. Its whole value is that
`probe9.m` differs from `probe.m` in exactly five lines:

```
da_coef       0.7389501  ->  0.7484065
Dagg43(1)        109.78  ->     111.18
Dagg43(2)        109.76  ->     111.17
Dagg43_cor(1)    108.94  ->     110.34
Dagg43_cor(2)    112.20  ->     113.64
```

and is byte-identical everywhere else — every counter, every array. A port that let
`eta` reach the packing rather than only the agglomerate is caught by the equality
half, not by the five numbers.

## Recipe

`PROBE.dat` is not an archived recipe and is not claimed to be physical. It borrows
`hp2`'s printed header — densities 1950 / 1800 kg m⁻³, oxidiser 0.583, metal 0.207,
fractions 0.0103 / 0.504 / 0.486 over 10–50, 113–180 and 160–315 µm, surface law,
generator 2 — and sets N = 20 so the run finishes instantly. Since both sides of the
comparison read the same file, the recipe not matching any real propellant costs
nothing.
