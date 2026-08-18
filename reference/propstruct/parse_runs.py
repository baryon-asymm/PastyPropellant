#!/usr/bin/env python3
"""Turn the archived PropStructV3 output files into a golden-master fixture set.

PropStructV3 is the Fortran-77 Monte-Carlo microstructure model that produced the
pocket mass fraction (``Zkarm`` -> ``pocket_mass_fraction``) and the pocket size
(``Dkarm43``) that ``data/propellants*.json`` carries as hand-copied constants.
Every run it performed wrote a MATLAB ``.m`` file whose header records the full
input, so each ``.m`` is a self-contained (input, expected output) pair.

This script parses those pairs into ``runs.json`` so that a port of the model can
be checked against them.  Numbers are copied out verbatim; the one exception is
``recover_eta``, which reimplements the model's own agglomerate-coefficient
expression - physical constants included - in order to invert it.  Everything it
produces sits under ``derived``.

⚠ A ``.m`` is not quite a complete input record: ``AK1..AK4`` live only in the
``.dat``.  See the ``known_quirks`` block in the output.

Only files written by the **current** build are accepted.  The ``r_*``/``rc_*``
files from 2013 come from an earlier build with a different output format (array
names ``fmdokkarm``/``Dok43karm``/``EPSMdok``, ``Zkarm`` printed as a vector);
that source is not in the archive, so they cannot serve as fixtures for a port of
the source that is.  They are reported as skipped, with the reason.

Usage::

    python reference/propstruct/parse_runs.py [ARCHIVE] [-o runs.json] [--report]

ARCHIVE is the PropStructV3 zip or an unpacked directory.
"""

from __future__ import annotations

import argparse
import datetime as _dt
import hashlib
import json
import os
import re
import struct
import sys
import zipfile

# The .m files are CP866 (Russian DOS); the Fortran source is CP1251.
ENCODING = "cp866"

# Marker that identifies the current output format.
FORMAT_MARKER = "Dkarm43_cor(2)"

NUM = r"[-+]?(?:\d+\.?\d*|\.\d+)(?:[EeDd][-+]?\d+)?"

# Smallest normal float32.  Values below it in a printed array did not come from
# arithmetic - they are uninitialised memory reinterpreted as REAL*4.
FLOAT32_MIN_NORMAL = 2.0**-126


def _f32(value: float) -> float:
    """The float64 that holds exactly what a REAL*4 would hold."""
    return struct.unpack("<f", struct.pack("<f", value))[0]


class DatRecords:
    """The input file as Fortran's sequential READ sees it: a cursor over records.

    List-directed READ consumes whole records until it has its values, and
    ``FORMAT(2X)`` skips exactly one.  Anything that reads a ``.dat`` has to
    follow the same bookkeeping or it will silently take a comment line for a
    number - the files carry a Russian caption above each data block.
    """

    def __init__(self, text: str):
        self._records = text.splitlines()
        self._at = 0

    def skip(self) -> None:
        """READ(2,1000), i.e. FORMAT(2X): consume one record, whatever is in it."""
        self._at += 1

    def values(self, count: int) -> list[float]:
        """List-directed READ of ``count`` values, consuming whole records."""
        out: list[float] = []
        while len(out) < count:
            if self._at >= len(self._records):
                raise ValueError("input file ended mid-list")
            line = self._records[self._at]
            self._at += 1
            for token in line.replace(",", " ").split():
                out.append(_num(token))
        if len(out) != count:
            raise ValueError(
                f"a record carried {len(out)} values where {count} were expected; "
                "list-directed READ would leave the surplus unread"
            )
        return out

    def rest(self) -> list[str]:
        return [line for line in self._records[self._at:] if line.strip()]


def parse_dat(text: str) -> dict:
    """The original's own input file, at the precision it was actually written.

    Mirrors lines 124-133 of PropStructv3.for record for record, then the
    conditional conversion of lines 260-266.  That conversion is **not** a units
    field: it multiplies by 1e-6 only where the value is at least 0.1, so one
    file may legitimately carry micrometres and metres side by side, and
    ``P35050n.dat`` does exactly that on one line.  Anything that assumes a
    single convention will be wrong on that run.
    """
    r = DatRecords(text)
    r.skip()
    plot1, plot2, ggg, gm = r.values(4)
    r.skip()
    ak1, ak2, ak3, ak4 = r.values(4)
    r.skip()
    nmm, jzz, kxx, n, nnz, gsv = r.values(6)
    nmm = int(nmm)
    r.skip()
    gdok = r.values(nmm)
    r.skip()
    ddok = r.values(2 * nmm)

    def to_metres(raw: float) -> float:
        # lines 261-262: REAL*4 throughout, and the test is on the stored value
        stored = _f32(raw)
        return _f32(stored * _f32(1e-6)) if stored >= 0.1 else stored

    fractions = [
        {
            "mass_fraction": _f32(gdok[i]),
            "d_min_m": to_metres(ddok[2 * i]),
            "d_max_m": to_metres(ddok[2 * i + 1]),
            "d_min_raw": ddok[2 * i],
            "d_max_raw": ddok[2 * i + 1],
            "scaled_from_micrometres": [
                _f32(ddok[2 * i]) >= 0.1,
                _f32(ddok[2 * i + 1]) >= 0.1,
            ],
        }
        for i in range(nmm)
    ]

    return {
        "oxidiser_density": _f32(plot1),
        "binder_metal_density": _f32(plot2),
        "oxidiser_mass_fraction": _f32(ggg),
        "metal_mass_fraction": _f32(gm),
        # AK1..AK4 appear in NO .m file; this is their only record
        "ak1": _f32(ak1),
        "ak2": _f32(ak2),
        "ak3": _f32(ak3),
        "ak4": _f32(ak4),
        "fraction_count": nmm,
        "size_distribution_law": int(jzz),
        "cycles_requested": int(kxx),
        "base_particles": int(n),
        "generator_warmup": nnz,
        "generator": int(gsv),
        "fractions": fractions,
        "trailing_records": r.rest(),
    }


class UnknownHeaderField(Exception):
    """A labelled value in the .m file that this parser does not consume.

    Raised rather than ignored.  ``eta`` was lost precisely because nothing
    objected to a field going unread, and the same silence would hide the next
    one.  This mirrors the run-configuration rule in the main host, where an
    unknown JSON member throws at startup instead of being skipped.
    """


# --------------------------------------------------------------------------- #
# primitive extractors
# --------------------------------------------------------------------------- #
def _num(text: str) -> float:
    return float(text.replace("D", "E").replace("d", "e"))


def scalar(body: str, pattern: str, cast=_num, required: bool = True):
    """First capture group of ``pattern``, or None when absent and optional."""
    m = re.search(pattern, body)
    if m is None:
        if required:
            raise KeyError(pattern)
        return None
    return cast(m.group(1))


def vector(body: str, name: str, required: bool = True) -> list[float] | None:
    """Read an ``arrayprint`` block: ``name = [ v v v ...\\n  v v ];``.

    Continuation lines end with the MATLAB ``...`` marker, which is stripped.
    """
    start = re.search(rf"(?m)^\s*{re.escape(name)}\s*=\s*\[", body)
    if start is None:
        if required:
            raise KeyError(name)
        return None
    end = body.index("]", start.end())
    raw = body[start.end():end].replace("...", " ")
    return [_num(tok) for tok in raw.split()]


STEP_RE = re.compile(r"\(step\s*=\s*(" + NUM + r")\s*(mkm)?\s*\)", re.IGNORECASE)

# The model's own step comment is wrong for one array.  fqmkm1 is binned with
# int(ratio*1000)+1 and averaged with a 0.001 weight in the source, but its
# header line is copied from fqmkm2 and announces 0.01.  Trusting the printed
# comment there puts the abscissa out by 10x, which is exactly the failure the
# steps were extracted to prevent, so the source wins and the disagreement is
# recorded rather than hidden.
STEP_SOURCE_OVERRIDES = {"fqmkm1": 0.001}


def array_steps(body: str, names: list[str]) -> dict[str, float]:
    """Bin width of each distribution, read from the comment above it.

    The model prints the abscissa step in the header line of every distribution
    (``(step = 10mkm)``, ``(step = 0.01)``).  Without it the x-axis of an array
    is guesswork: every archived run has ``Dmin = Di = Dj = 10 um``, so a port
    that confused the histogram step with any of those would look correct.

    Only the two lines directly above the array are searched, so an array with
    no step comment of its own does not inherit the previous one.
    """
    steps: dict[str, float] = {}
    for name in names:
        start = re.search(rf"(?m)^\s*{re.escape(name)}\s*=\s*\[", body)
        if start is None:
            continue
        preceding = "\n".join(body[: start.start()].splitlines()[-2:])
        found = list(STEP_RE.finditer(preceding))
        if found:
            steps[name] = _num(found[-1].group(1))
        if name in STEP_SOURCE_OVERRIDES:
            steps[name + "_as_printed"] = steps.get(name)
            steps[name] = STEP_SOURCE_OVERRIDES[name]
    return steps


# Every labelled value the parser knows about, normalised by LABEL_INDEX_RE.
# A label outside this set means the .m format carries something we do not read.
KNOWN_LABELS = {
    # input header
    "Input filename", "Plot1", "Plot2", "Gdok", "Gm", "Gfr", "Dfr",
    "Nfr", "JZ", "Cycles", "N", "GSV", "NNZ", "sfr",
    "Dmin", "Di", "Dj", "Nkarm/Nmkm(min, max)", "k5",
    "Statistical significance P(alpha)", "eps", "Calculation variant",
    "P(karm-in-karm) coef", "P(karm-in-MKM) coef", "Zok*",
    # counters and generator statistics
    "Cycles:", "Nbase", "Nkarm", "NFX", "NFY", "NFQ", "NFW", "epsx(n)",
    # oxidiser
    "Dok43a", "Dok43all", "Dok43(n)", "epsalldok", "epsdok(n)", "Ddok_max",
    "Dokb_max", "Dok43sd", "epsdokfr", "fineoxy_fr",
    # pockets, bridges, agglomerates
    "epsfkarm", "epsmkarm(n)", "Dkarm43(n)", "Dkarm43sd", "Dkarm43_cor(n)",
    "Dkarm43sd_cor(n)", "Dkarm10", "Dkarm10_cor", "Zkarm", "Zkarm_cor",
    "da_coef", "Dagg43(n)", "Dagg43_cor(n)", "Dqmkm1", "Dqmkm2",
    # distributions and the conditional table
    "fmdok", "fmkarm", "fmkarm_cor", "fmkarm_cor2", "fqkarm", "fqkarm_cor",
    "fqmkm1", "fqmkm2", "coef", "pdoksmall", "Dkarmcat", "dokkarm43",
    "dokkarm10", "Dkarm", "fqdokkarm(n)",
    # convergence arrays (no archived run contains them)
    "epsfkarm_n", "epsm3karm_n", "epsm4karm_n", "epsdok43_n", "epsdoksd_n",
    "epszkarm_n",
    # rejection counters, normalised to "C)" by LABEL_INDEX_RE
    "C) Dok > Dmax", "C) Dok < Dmin", "C) Dbase < 0.5Dok", "C) Dbase > 2.0Dok",
    "C) l > 4.7Dbase", "C) Nkarm", "C) Nmkm < 2", "C) Nkarm/Nmkm < min",
    "C) Nkarm/Nmkm > max",
    # colon-delimited values - invisible to a check that only knows "="
    "Medium coef [(Lij/Ddok)+1]", "Medium number of bridges",
    "Medium ratio between number of pockets and bridges",
    "Medium fraction of paricles number in jammed pack",
    "Calculation time", "Input filename",
    # abscissa steps, consumed by array_steps()
    "step",
}

LABEL_INDEX_RE = re.compile(r"\(\s*(\d+)\s*,?\s*:?\s*\)")
LABEL_CONDITION_RE = re.compile(r"^\d+\)\s*")
# A labelled value is "<label> <separator> <number-or-bracket>".  BOTH
# separators must be here: half the model's output is colon-delimited (the
# local-structure means, all nine condition lines, the calculation time), and a
# check that only knows "=" is blind to that half - which is how the first
# version of this function passed while four whole families went unexamined.
# \w with re.UNICODE also admits a Cyrillic label, which the ASCII form missed.
ASSIGNMENT_RE = re.compile(
    r"([^\s=:;\[][^=:;\n]*?)\s*[=:]\s*(?=\[|" + NUM + r"\s*(?:[;,\]]|$))",
    re.UNICODE,
)
# indices seen on each base name, so a NEW index on a known name is reported
# rather than collapsed into the one already covered
KNOWN_INDICES = {
    "epsx": {1, 2, 3, 4, 5, 6}, "Dagg43": {1, 2}, "Dagg43_cor": {1, 2},
    "Dkarm43": {1, 2}, "Dkarm43_cor": {1, 2}, "Dkarm43sd_cor": {1, 2},
    "Dok43": {1, 2}, "epsdok": {1, 2}, "epsmkarm": {1, 2},
}


def unknown_labels(body: str) -> list[str]:
    """Labelled values in the file that KNOWN_LABELS does not cover."""
    seen: set[str] = set()
    for line in body.splitlines():
        text = line.strip().lstrip("%").strip()
        for part in re.split(r"[;]", text):
            for m in ASSIGNMENT_RE.finditer(part):
                raw = m.group(1).strip()
                index = LABEL_INDEX_RE.search(raw)
                label = LABEL_INDEX_RE.sub("(n)", raw)
                label = LABEL_CONDITION_RE.sub("C) ", label)
                # a distribution's header line ends in "(step = <value>)"; the
                # text in front of it names the distribution in prose
                if label.lower().endswith("step") or "(step" in label.lower():
                    label = "step"
                # "12 : 30" in the calculation-time line, and the ") " left by a
                # ':' inside a subscript, are separator debris, not labels
                if not any(ch.isalpha() for ch in label):
                    continue
                if label not in KNOWN_LABELS:
                    seen.add(label)
                elif index is not None:
                    base = label.replace("(n)", "")
                    allowed = KNOWN_INDICES.get(base)
                    if allowed is not None and int(index.group(1)) not in allowed:
                        seen.add(f"{base}({index.group(1)})")
    return sorted(seen)


# --------------------------------------------------------------------------- #
# the input half of a run (the .m header block)
# --------------------------------------------------------------------------- #
def parse_input(body: str) -> dict:
    gfr = vector(body, "Gfr")
    dfr = vector(body, "Dfr")
    nfr = int(scalar(body, r"%\s*Nfr\s*=\s*(\d+)", int))
    if len(dfr) != 2 * nfr:
        raise ValueError(f"Dfr has {len(dfr)} bounds for {nfr} fractions")

    fractions = [
        {"mass_fraction": gfr[i], "d_min_m": dfr[2 * i], "d_max_m": dfr[2 * i + 1]}
        for i in range(nfr)
    ]

    return {
        "input_filename": scalar(body, r"%\s*Input filename:\s*(\S+)", str),
        # densities: PLOT1 = oxidiser, PLOT2 = binder+metal composition, kg/m3
        "oxidiser_density": scalar(body, r"%\s*Plot1\s*=\s*(" + NUM + r")"),
        "binder_metal_density": scalar(body, r"Plot2\s*=\s*(" + NUM + r")"),
        "oxidiser_mass_fraction": scalar(body, r"Gdok\s*=\s*(" + NUM + r")"),
        "metal_mass_fraction": scalar(body, r"Gm\s*=\s*(" + NUM + r")"),
        "fractions": fractions,
        "size_distribution_law": int(scalar(body, r"JZ\s*=\s*(\d+)", int)),
        "cycles": int(scalar(body, r"Cycles\s*=\s*(\d+)", int)),
        "base_particles": int(scalar(body, r";\s*N\s*=\s*(\d+)", int)),
        "generator": int(scalar(body, r"GSV\s*=\s*(\d+)", int)),
        "generator_warmup": scalar(body, r"NNZ\s*=\s*(" + NUM + r")", required=False),
        "pocket_forming_fractions": vector(body, "sfr", required=False),
    }


def parse_parameters(body: str) -> dict:
    nn = re.search(rf"%\s*Nkarm/Nmkm\(min, max\)\s*=\s*\[\s*({NUM})\s+({NUM})", body)
    return {
        # printed in micrometres as integers
        "d_min_um": int(scalar(body, r"(?m)^\s*Dmin\s*=\s*(\d+)", int)),
        "di_um": int(scalar(body, r"(?m)^\s*Di\s*=\s*(\d+)", int)),
        "dj_um": int(scalar(body, r"(?m)^\s*Dj\s*=\s*(\d+)", int)),
        "pocket_bridge_ratio_min": _num(nn.group(1)),
        "pocket_bridge_ratio_max": _num(nn.group(2)),
        "k5_surrounding_volume": scalar(body, r"%\s*k5\s*=\s*(" + NUM + r")"),
        "p_alpha": scalar(body, r"Statistical significance P\(alpha\)=\s*(" + NUM + r")"),
        "eps": scalar(body, r"%\s*eps\s*=\s*(" + NUM + r")"),
        "calculation_variant": int(
            scalar(body, r"%\s*Calculation variant\s*=\s*(-?\d+)", int)
        ),
        "k7_pocket_in_pocket": scalar(body, r"P\(karm-in-karm\) coef\s*=\s*(" + NUM + r")"),
        "k8_pocket_in_bridge": scalar(body, r"P\(karm-in-MKM\) coef\s*=\s*(" + NUM + r")"),
        "homogenised_oxidiser": scalar(body, r"%\s*Zok\*\s*=\s*(" + NUM + r")"),
    }


# --------------------------------------------------------------------------- #
# the expected-output half
# --------------------------------------------------------------------------- #
CONDITION_LABELS = [
    "dok_above_dmax",
    "dok_below_dmin",
    "base_below_half_dok",
    "base_above_two_dok",
    "gap_above_4p7_base",
    "no_pockets",
    "fewer_than_two_bridges",
    "pocket_bridge_ratio_below_min",
    "pocket_bridge_ratio_above_max",
]

SCALARS = {
    # counters that must match exactly for the port to be equivalent
    "cycles_done": (r"%\s*Cycles:\s*(\d+)", int),
    "nkarm": (r"(?m)^\s*Nkarm\s*=\s*(\d+)", int),
    "nfx": (r"NFX\s*=\s*(\d+)", int),
    "nfy": (r"NFY\s*=\s*(\d+)", int),
    "nfq": (r"NFQ\s*=\s*(\d+)", int),
    "nfw": (r"NFW\s*=\s*(\d+)", int),
    # random-number quality, one per stream (epsx(5) is X3, epsx(6) is X4)
    "epsx1": (r"epsx\(1\)=\s*(" + NUM + r")", _num),
    "epsx2": (r"epsx\(2\)=\s*(" + NUM + r")", _num),
    "epsx3": (r"epsx\(3\)=\s*(" + NUM + r")", _num),
    "epsx4": (r"epsx\(4\)=\s*(" + NUM + r")", _num),
    "epsx5": (r"epsx\(5\)=\s*(" + NUM + r")", _num),
    "epsx6": (r"epsx\(6\)=\s*(" + NUM + r")", _num),
    # local structure
    "gap_coefficient": (r"Medium coef \[\(Lij/Ddok\)\+1\]:\s*(" + NUM + r")", _num),
    "bridges_per_particle": (r"Medium number of bridges:\s*(" + NUM + r")", _num),
    "pocket_bridge_ratio": (
        r"Medium ratio between number of pockets and bridges\s*:\s*(" + NUM + r")",
        _num,
    ),
    "jammed_fraction": (
        r"Medium fraction of paricles number in jammed pack:\s*(" + NUM + r")",
        _num,
    ),
    # oxidiser particle sizes, micrometres
    "dok43_analytic": (r"Dok43a\s*=\s*(" + NUM + r")", _num),
    "dok43_base": (r"Dok43\(1\)\s*=\s*(" + NUM + r")", _num),
    "dok43_surrounding": (r"Dok43\(2\)\s*=\s*(" + NUM + r")", _num),
    "dok43_sd": (r"Dok43sd\s*=\s*(" + NUM + r")", _num),
    "dok_max": (r"Ddok_max\s*=\s*(" + NUM + r")", _num),
    "dok_base_max": (r"Dokb_max\s*=\s*(" + NUM + r")", _num),
    "eps_all_dok": (r"epsalldok\s*=\s*(" + NUM + r")", _num),
    "eps_dok_base": (r"epsdok\(1\)\s*=\s*(" + NUM + r")", _num),
    "eps_dok_surrounding": (r"epsdok\(2\)\s*=\s*(" + NUM + r")", _num),
    "fine_oxidiser_fraction": (r"fineoxy_fr\s*=\s*(" + NUM + r")", _num),
    # pockets
    "eps_pocket_distribution": (r"epsfkarm\s*=\s*(" + NUM + r")", _num),
    "eps_pocket_moment4": (r"epsmkarm\(1\)=\s*(" + NUM + r")", _num),
    "eps_pocket_moment3": (r"epsmkarm\(2\)=\s*(" + NUM + r")", _num),
    "dkarm43_v1": (r"Dkarm43\(1\)\s*=\s*(" + NUM + r")", _num),
    "dkarm43_v2": (r"Dkarm43\(2\)\s*=\s*(" + NUM + r")", _num),
    "dkarm43_sd": (r"Dkarm43sd\s*=\s*(" + NUM + r")", _num),
    "dkarm43_cor1": (r"Dkarm43_cor\(1\)\s*=\s*(" + NUM + r")", _num),
    "dkarm43_sd_cor1": (r"Dkarm43sd_cor\(1\)\s*=\s*(" + NUM + r")", _num),
    "dkarm43_cor2": (r"Dkarm43_cor\(2\)\s*=\s*(" + NUM + r")", _num),
    "dkarm43_sd_cor2": (r"Dkarm43sd_cor\(2\)\s*=\s*(" + NUM + r")", _num),
    "dkarm10": (r"Dkarm10\s*=\s*(" + NUM + r")", _num),
    "dkarm10_cor": (r"Dkarm10_cor\s*=\s*(" + NUM + r")", _num),
    # pocket mass fraction -> pocket_mass_fraction in propellants*.json
    "zkarm": (r"(?m)^\s*Zkarm\s*=\s*(" + NUM + r")", _num),
    # agglomerates
    "da_coef": (r"da_coef\s*=\s*(" + NUM + r")", _num),
    "dagg43_v1": (r"Dagg43\(1\)\s*=\s*(" + NUM + r")", _num),
    "dagg43_v2": (r"Dagg43\(2\)\s*=\s*(" + NUM + r")", _num),
    "dagg43_cor1": (r"Dagg43_cor\(1\)\s*=\s*(" + NUM + r")", _num),
    "dagg43_cor2": (r"Dagg43_cor\(2\)\s*=\s*(" + NUM + r")", _num),
    # inter-pocket bridges
    "dqmkm1": (r"Dqmkm1\s*=\s*(" + NUM + r")", _num),
    "dqmkm2": (r"Dqmkm2\s*=\s*(" + NUM + r")", _num),
}

OPTIONAL_SCALARS = {"dok_base_max", "epsx5", "epsx6"}

ARRAYS = [
    "fmdok",
    "fmkarm",
    "fmkarm_cor",
    "fmkarm_cor2",
    "fqkarm",
    "fqkarm_cor",
    "fqmkm1",
    "fqmkm2",
    "coef",
    "pdoksmall",
    "epsdokfr",
    "Dkarmcat",
    "dokkarm43",
    "dokkarm10",
]

CONVERGENCE_ARRAYS = [
    "epsfkarm_n",
    "epsm3karm_n",
    "epsm4karm_n",
    "epsdok43_n",
    "epsdoksd_n",
    "epszkarm_n",
]


def parse_expected(body: str) -> dict:
    scalars: dict[str, float | int] = {}
    for key, (pattern, cast) in SCALARS.items():
        value = scalar(body, pattern, cast, required=key not in OPTIONAL_SCALARS)
        if value is not None:
            scalars[key] = value

    # "Nbase = N + FI": the per-cycle target and the cumulative accepted count
    # (FI grows by N each cycle).  The two are equal only because Cycles = 1 in
    # every archived run; calling the second one "extra" would mislead exactly
    # where the fixtures stop covering, at KXX > 1.
    nbase = re.search(rf"Nbase\s*=\s*(\d+)\s*\+\s*(\d+)", body)
    scalars["nbase_per_cycle"] = int(nbase.group(1))
    scalars["nbase_accepted_cumulative"] = int(nbase.group(2))

    zc = re.search(rf"Zkarm_cor\s*=\s*\[\s*({NUM})\s+({NUM})", body)
    scalars["zkarm_cor1"] = _num(zc.group(1))
    scalars["zkarm_cor2"] = _num(zc.group(2))

    da = re.search(rf"Dok43all\s*=\s*\[\s*({NUM})\s+({NUM})", body)
    scalars["dok43_all_v1"] = _num(da.group(1))
    scalars["dok43_all_v2"] = _num(da.group(2))

    # NOTE the two divisors: the model prints counters 1-5 over (FI+N) and
    # counters 6-9 over FI.  FI == N in every archived run, so the first five
    # are per *attempted* base particle and the last four per accepted one, and
    # the two differ by exactly a factor 2.  Normalising all nine alike puts
    # four of them out by 2x.
    conditions = {}
    for index, label in enumerate(CONDITION_LABELS, start=1):
        conditions[label] = scalar(
            body, rf"(?m)^\s*%\s*{index}\)[^:\n]*:\s*({NUM})"
        )

    arrays = {name: vector(body, name, required=False) for name in ARRAYS}
    arrays = {k: v for k, v in arrays.items() if v is not None}

    convergence = {
        name: vector(body, name, required=False) for name in CONVERGENCE_ARRAYS
    }
    convergence = {k: v for k, v in convergence.items() if v is not None}
    if convergence:
        arrays.update(convergence)

    # conditional oxidiser size distribution, one row per pocket size category
    rows, categories = [], []
    for m in re.finditer(r"%\s*Dkarm =\s*(-?\d+)\s*mkm", body):
        categories.append(int(m.group(1)))
    for m in re.finditer(r"fqdokkarm\(\s*\d+,:\)\s*=\s*\[", body):
        end = body.index("]", m.end())
        rows.append([_num(t) for t in body[m.end():end].replace("...", " ").split()])

    time = re.search(r"Calculation time:\s*(-?\d+)\s*:\s*(-?\d+)\s*:\s*(-?\d+)", body)
    seconds = None
    if time:
        h, mnt, s = (int(g) for g in time.groups())
        seconds = h * 3600 + mnt * 60 + s

    return {
        "scalars": scalars,
        "conditions_per_particle": conditions,
        "arrays": arrays,
        # abscissa step per distribution, as the model itself printed it
        "array_steps": array_steps(body, list(arrays)),
        "conditional_dok": {"pocket_category_um": categories, "rows": rows},
        "calculation_time_s": seconds,
    }


# --------------------------------------------------------------------------- #
# self-checks: these validate THE PARSER, and nothing else.
#
# They are not a physics oracle and must not be counted as coverage by a port.
# Every one of them is satisfiable without simulating anything: the mass sums
# and bounds test the input header we just read back; Dagg = Dkarm * da_coef
# holds by construction in any implementation that computes Dagg that way; the
# normalisations test that a density was printed as a density; the length checks
# test our own row splitting.  Their job is to catch a regex that silently
# matched the wrong line.
# --------------------------------------------------------------------------- #
def self_check(run: dict) -> tuple[list[str], int]:
    problems: list[str] = []
    performed = 0
    inp, exp = run["input"], run["expected"]
    s, arrays = exp["scalars"], exp["arrays"]
    di = run["parameters"]["di_um"]

    performed += 1
    mass = sum(f["mass_fraction"] for f in inp["fractions"])
    if abs(mass - 1.0) > 2e-3:
        problems.append(f"fraction masses sum to {mass:.4f}")

    for i, f in enumerate(inp["fractions"]):
        performed += 1
        if not f["d_min_m"] < f["d_max_m"]:
            problems.append(f"fraction {i} bounds not increasing")

    # Dagg = Dkarm * da_coef, printed at F7.2 -> absolute tolerance 0.01 um
    for agg, karm in (
        ("dagg43_v1", "dkarm43_v1"),
        ("dagg43_v2", "dkarm43_v2"),
        ("dagg43_cor1", "dkarm43_cor1"),
        ("dagg43_cor2", "dkarm43_cor2"),
    ):
        if agg in s and karm in s:
            performed += 1
            expect = s[karm] * s["da_coef"]
            if abs(s[agg] - expect) > 0.02 + 1e-4 * expect:
                problems.append(
                    f"{agg}={s[agg]:.2f} != {karm}*da_coef={expect:.2f}"
                )

    # Two families, distinguished by the step the model printed above each array
    # rather than by Di - which only looks equivalent because every archived run
    # has Di = 10 um.  The size distributions are densities and integrate to 1
    # over their own step; fqmkm1/fqmkm2/coef are per-bin probabilities that sum
    # to 1 despite being labelled "density distribution function".
    steps = exp.get("array_steps", {})
    for name in ("fmdok", "fmkarm", "fqkarm", "fmkarm_cor", "fmkarm_cor2",
                 "fqkarm_cor"):
        if name in arrays and arrays[name]:
            performed += 1
            # no silent fallback to di: a missing step is itself the finding,
            # and today it would be indistinguishable because both are 10
            if name not in steps:
                problems.append(f"{name} has no recorded abscissa step")
                continue
            step = steps[name]
            total = sum(arrays[name]) * step
            if abs(total - 1.0) > 2e-3:
                problems.append(f"{name} integrates to {total:.4f} at step {step:g}")

    for name in ("fqmkm1", "fqmkm2", "coef"):
        if name in arrays and arrays[name]:
            performed += 1
            total = sum(arrays[name])
            if abs(total - 1.0) > 2e-3:
                problems.append(f"{name} sums to {total:.4f}")

    performed += 1
    if not 0.0 <= s.get("zkarm", -1) <= 1.0:
        problems.append(f"zkarm={s.get('zkarm')} outside [0,1]")

    cats = exp["conditional_dok"]["pocket_category_um"]
    rows = exp["conditional_dok"]["rows"]
    performed += 1
    if len(cats) != len(rows):
        problems.append(f"{len(cats)} pocket categories but {len(rows)} rows")
    for name in ("Dkarmcat", "dokkarm43", "dokkarm10"):
        if name in arrays:
            performed += 1
            if len(arrays[name]) != len(rows):
                problems.append(
                    f"{name} has {len(arrays[name])} entries, {len(rows)} rows"
                )

    return problems, performed


# --------------------------------------------------------------------------- #
# derived tags (our interpretation, not data from the file)
# --------------------------------------------------------------------------- #
HMX_FRACTION = (160e-6, 315e-6)  # the octogen, present in every Bas recipe

# fraction sets of the three modelled compositions: (mass, d_min_m, d_max_m)
BAS_RECIPES = {
    "Bas_4": [(0.514, 113e-6, 180e-6), (0.486, *HMX_FRACTION)],
    "Bas_2": [(0.180, 10e-6, 50e-6), (0.334, 113e-6, 180e-6), (0.486, *HMX_FRACTION)],
    "Bas_3": [(0.514, 10e-6, 50e-6), (0.486, *HMX_FRACTION)],
}


def _same_fractions(fractions: list[dict], recipe) -> bool:
    if len(fractions) != len(recipe):
        return False
    for f, (mass, lo, hi) in zip(fractions, recipe):
        if abs(f["mass_fraction"] - mass) > 3e-3:
            return False
        if abs(f["d_min_m"] - lo) > 1e-6 or abs(f["d_max_m"] - hi) > 1e-6:
            return False
    return True


def recover_eta(run: dict) -> tuple[float | None, float | None]:
    """Recover ``eta`` (the agglomerated-oxide share) from ``da_coef``.

    ``eta`` is the one model input the output file does NOT record, and it moves
    ``Dagg43``.  It enters only through the pocket-to-agglomerate coefficient,
    which is monotone in ``eta``, so it can be inverted.  Without this a run is
    not reproducible from its own header.
    """
    import math

    inp, par = run["input"], run["parameters"]
    scalars = run["expected"]["scalars"]

    g = inp["oxidiser_mass_fraction"]
    rho_ox, rho_bm = inp["oxidiser_density"], inp["binder_metal_density"]
    gm = inp["metal_mass_fraction"]
    left = max(0.0, (scalars["fine_oxidiser_fraction"] - par["homogenised_oxidiser"]) * g)
    structural = g - left
    rho_mix = (1.0 - structural) / (1.0 / rho_bm - structural / rho_ox)
    # Only the EXPONENT matters here.  pi appears as 3.14159/6 and again as
    # 0.75/3.14159, and 0.75/pi' * pi'/6 = 0.125 for any pi', so substituting
    # math.pi moves the recovered eta by less than 1e-9.  Substituting 1/3 for
    # the literal 0.3333 moves it by 0.002.  (An earlier note here blamed both;
    # the caution about literal transcription stands, the attribution did not.)
    base = 3.14159 / 6.0 * rho_mix * gm / (1.0 - g + left)

    def coefficient(eta: float) -> float:
        oxide = 1.0 + 3 * 0.016 * eta / (2 * 0.027 + 3 * 0.016 * (1 - eta))
        density = (1 - eta) / 2000.0 + eta / 3000.0
        return 2.0 * (0.75 / 3.14159 * base * oxide * density) ** 0.3333

    # da_coef is printed at single precision, so allow the bracket to be missed
    # by a few ulps of a 7-digit decimal
    target = scalars["da_coef"]
    slack = 2e-6 * target
    lo, hi = 0.0, 1.0
    if target < coefficient(lo) - slack or target > coefficient(hi) + slack:
        return None, None
    zero_residual = abs(coefficient(0.0) - target) / target
    if zero_residual <= slack / target:
        # eta = 0 already explains da_coef to the precision it was printed at
        return 0.0, zero_residual
    for _ in range(80):
        mid = 0.5 * (lo + hi)
        if coefficient(mid) < target:
            lo = mid
        else:
            hi = mid
    eta = 0.5 * (lo + hi)
    return eta, abs(coefficient(eta) - target) / target


def derive(run: dict) -> dict:
    """Our interpretation of a run: which composition it is, and what x it sits at.

    In the Bas family the oxidiser is AP + HMX.  The HMX is the 160-315 um
    fraction; the rest is AP, split between a fine and a coarse fraction, and x
    is the fine share of the AP.  Runs whose fraction set has no 160-315 um
    entry (the hpepa2 inputs, which merge coarse AP and HMX into one 113-315 um
    fraction) get no x - the split is not recoverable there.
    """
    derived: dict = {}

    # The fill loop runs from index 2, so exactly ONE element - pdoksmall(1) -
    # is uninitialised.  Every further anomalous entry is that same value
    # propagated by the monotone step that carries the running maximum forward
    # over the leading zeros.  So the block is detected structurally, by
    # equality with element 1, not by a magnitude threshold: the leftover word
    # is usually subnormal but lands just above the subnormal boundary in six
    # runs, and a threshold test would also miss the one case that actually
    # matters - a leftover large enough to change Dmaxxx.
    small = run["expected"]["arrays"].get("pdoksmall") or []
    if small and small[0] != 0.0:
        block = 0
        for value in small:
            if value != small[0]:
                break
            block += 1
        derived["pdoksmall_uninitialised_value"] = small[0]
        derived["pdoksmall_propagated_count"] = block
        derived["pdoksmall_is_subnormal"] = abs(small[0]) < FLOAT32_MIN_NORMAL

    eta, residual = recover_eta(run)
    if eta is not None:
        derived["recovered_eta"] = round(eta, 6)
        derived["recovered_eta_residual"] = residual

    fractions = run["input"]["fractions"]
    hmx = [
        f
        for f in fractions
        if abs(f["d_min_m"] - HMX_FRACTION[0]) < 1e-6
        and abs(f["d_max_m"] - HMX_FRACTION[1]) < 1e-6
    ]
    # the Bas family carries a fixed 0.486 of HMX; without that the 160-315 um
    # fraction is coarse oxidiser and the AP split is not recoverable
    if len(hmx) == 1 and abs(hmx[0]["mass_fraction"] - 0.486) < 5e-3:
        ap_mass = sum(f["mass_fraction"] for f in fractions) - hmx[0]["mass_fraction"]
        fine = sum(f["mass_fraction"] for f in fractions if f["d_max_m"] <= 60e-6)
        if ap_mass > 0:
            derived["fine_ap_mass_share_x"] = round(fine / ap_mass, 4)
            derived["x_assumes_hmx_is_the_160_315_fraction"] = True

    for name, recipe in BAS_RECIPES.items():
        if _same_fractions(fractions, recipe):
            # Bas_0 and Bas_1 share the Bas_2 recipe; only the binder differs
            derived["bas_composition"] = name
            break

    return derived


# --------------------------------------------------------------------------- #
# driver
# --------------------------------------------------------------------------- #
def load_sources(archive: str):
    """Yield (name, mtime_iso, text) for every .m file in a zip or directory."""
    if os.path.isdir(archive):
        for name in sorted(os.listdir(archive)):
            if name.lower().endswith(".m"):
                path = os.path.join(archive, name)
                mtime = _dt.datetime.fromtimestamp(os.path.getmtime(path))
                with open(path, "rb") as fh:
                    yield name, mtime.isoformat(timespec="minutes"), fh.read().decode(
                        ENCODING, errors="replace"
                    )
        return

    with zipfile.ZipFile(archive) as z:
        for info in sorted(z.infolist(), key=lambda i: i.filename):
            if not info.filename.lower().endswith(".m"):
                continue
            stamp = _dt.datetime(*info.date_time).isoformat(timespec="minutes")
            yield (
                os.path.basename(info.filename),
                stamp,
                z.read(info).decode(ENCODING, errors="replace"),
            )


def load_inputs(archive: str) -> dict[str, tuple[str, str]]:
    """Every .dat in the archive as (name as stored, text), keyed by lower-case name.

    Keyed lower-case because the ``.m`` headers spell the file in lower case while
    the archive stores it in upper (``hpepa.dat`` against ``HPEPA.dat``, in 40 of
    the 41 pairings).  DOS did not distinguish them; a case-sensitive filesystem
    does, so the name as stored has to survive the lookup - it is the one a reader
    can actually open.
    """
    out: dict[str, tuple[str, str]] = {}
    if os.path.isdir(archive):
        for name in sorted(os.listdir(archive)):
            if name.lower().endswith(".dat"):
                with open(os.path.join(archive, name), "rb") as fh:
                    out[name.lower()] = (
                        name,
                        fh.read().decode(ENCODING, errors="replace"),
                    )
        return out

    with zipfile.ZipFile(archive) as z:
        for info in sorted(z.infolist(), key=lambda i: i.filename):
            if info.filename.lower().endswith(".dat"):
                name = os.path.basename(info.filename)
                out[name.lower()] = (
                    name,
                    z.read(info).decode(ENCODING, errors="replace"),
                )
    return out


def check_dat_against_m(run: dict) -> tuple[list[str], int]:
    """Does the .dat still describe the run its .m recorded?

    ⚠ For 20 of the 43 runs it does not, and that is the single most important
    thing this script has to say about the input files.  A ``.dat`` is a working
    file that was edited between runs and kept only its **last** state: the
    ``hp*`` series were all re-pointed at 100 000 base particles afterwards,
    ``hp1801`` and ``r_p35050n`` have had fractions added, ``res_02`` had its
    distribution law switched.  The archive names the input file in each ``.m``
    header, so the pairing is certain; what is not certain is the content.

    A ``.dat`` is therefore usable as a full-precision input record **only** where
    this check passes.  Everywhere else it is a later revision of the same file
    and using it would silently replay a different run.
    """
    dat = run.get("input_dat")
    if not dat:
        return [], 0

    findings: list[str] = []
    checks = 0
    printed = run["input"]["fractions"]
    exact = dat["fractions"]
    if len(printed) != len(exact):
        return (
            [f"{run['id']}: .dat has {len(exact)} fractions, .m printed {len(printed)}"],
            1,
        )

    for i, (p, e) in enumerate(zip(printed, exact), start=1):
        for key in ("d_min_m", "d_max_m"):
            a, b = p[key], e[key]
            if a == 0 and b == 0:
                continue
            checks += 1
            # E9.3 keeps three significant digits: half a unit in the last one
            quantum = 10 ** (_math_ceil_log10(a) - 3) if a else 0.0
            if abs(a - b) > 0.5 * quantum * 1.000001:
                findings.append(
                    f"{run['id']}: fraction {i} {key} printed {a:.6E} but .dat "
                    f"gives {b:.6E}, outside the E9.3 quantum {quantum:.3E}"
                )

    for field, printed_value in (
        ("size_distribution_law", run["input"]["size_distribution_law"]),
        ("base_particles", run["input"]["base_particles"]),
        ("generator", run["input"]["generator"]),
    ):
        checks += 1
        if dat[field] != printed_value:
            findings.append(
                f"{run['id']}: .dat {field} = {dat[field]}, .m printed {printed_value}"
            )
    return findings, checks


def _math_ceil_log10(value: float) -> int:
    import math

    return math.ceil(math.log10(abs(value)))


def main(argv: list[str] | None = None) -> int:
    here = os.path.dirname(os.path.abspath(__file__))
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "archive",
        nargs="?",
        default="/root/projects/PropStructV3/PropStructV3.zip",
        help="PropStructV3 zip or unpacked directory",
    )
    parser.add_argument("-o", "--output", default=os.path.join(here, "runs.json"))
    parser.add_argument("--report", action="store_true", help="print a summary table")
    args = parser.parse_args(argv)

    inputs = load_inputs(args.archive)

    runs, skipped = [], []
    for name, mtime, text in load_sources(args.archive):
        stem = name[:-2]
        if not text.strip():
            skipped.append((stem, "empty file"))
            continue
        if FORMAT_MARKER not in text:
            skipped.append((stem, "output format of the pre-2015 build"))
            continue

        # loud and up-front: a field we do not read is a field that can go
        # missing from the fixture without anyone noticing, which is exactly how
        # eta was lost.  Not caught by the parse-error handler below - an
        # incomplete parser must stop the generation, not skip one run.
        unknown = unknown_labels(text)
        if unknown:
            raise UnknownHeaderField(
                f"{name}: {len(unknown)} labelled value(s) not consumed by this "
                f"parser: {', '.join(unknown)}.  Add them to SCALARS/ARRAYS or, "
                f"if they are genuinely not data, to KNOWN_LABELS."
            )

        try:
            run = {
                "id": stem,
                "source_file": name,
                "source_written": mtime,
                "input": parse_input(text),
                "parameters": parse_parameters(text),
                "expected": parse_expected(text),
            }
        except Exception as exc:  # noqa: BLE001 - report and continue
            skipped.append((stem, f"parse error: {exc!r}"))
            continue

        # The .m names its own input file, so the pairing is recorded by the
        # archive rather than reconstructed by us.  Where the .dat is present it
        # is the better record of the input: the .m prints the bounds through
        # E9.3, which keeps three significant digits, while the .dat carries what
        # was actually typed.  AK1..AK4 exist nowhere else at all.
        dat_name = (run["input"].get("input_filename") or "").lower()
        entry = inputs.get(dat_name)
        if entry is not None:
            archive_name, dat_text = entry
            try:
                run["input_dat"] = parse_dat(dat_text)
                run["input_dat"]["source_file"] = dat_name
                # source_file is how the .m header spells it, which is lower case
                # for 40 of the 41 pairings; archive_file is the name under
                # inputs/, and the only one that opens on a case-sensitive
                # filesystem.  Both are kept: the first is what the run recorded,
                # the second is where the bytes are.
                run["input_dat"]["archive_file"] = archive_name
                # the vendored copy under inputs/ is checkable against this
                run["input_dat"]["sha256"] = hashlib.sha256(
                    dat_text.encode(ENCODING)
                ).hexdigest()
            except Exception as exc:  # noqa: BLE001 - report and continue
                run["input_dat_error"] = f"{dat_name}: {exc!r}"

        # A disagreeing .dat is a fact about the archive, not a defect in the
        # fixture, so it is recorded on the pairing and kept out of self_check -
        # 20 findings there would bury the four that are about fixture integrity.
        if "input_dat" in run:
            disagreements, _ = check_dat_against_m(run)
            run["input_dat"]["agrees_with_m"] = not disagreements
            run["input_dat"]["disagreements"] = disagreements

        run["derived"] = derive(run)
        run["self_check"], run["self_check_count"] = self_check(run)
        runs.append(run)

    # runs identical in their results are flagged, not dropped; wall-clock time
    # is excluded because it differs between two runs of the same case
    seen: dict[str, str] = {}
    for run in runs:
        comparable = {
            "scalars": run["expected"]["scalars"],
            "conditions": run["expected"]["conditions_per_particle"],
        }
        key = json.dumps(comparable, sort_keys=True)
        if key in seen:
            run["duplicate_of"] = seen[key]
        else:
            seen[key] = run["id"]

    document = {
        # /2 adds expected.array_steps and replaces pdoksmall_leading_garbage
        # with pdoksmall_garbage_count (the garbage is up to a whole array, not
        # one element)
        # /3 adds run.input_dat: the original .dat named in the .m header, at the
        # precision it was written, with AK1..AK4 - which no .m records at all.
        # It carries agrees_with_m, and that flag is load-bearing: only 21 of the
        # 43 .dat files still describe the run they are paired with.
        "schema": "propstruct-reference-runs/3",
        "source_archive": os.path.basename(args.archive),
        "model_source": "PropStructv3.for (Compaq Visual Fortran, 1776 lines, 2015-03-18)",
        "generated_by": "reference/propstruct/parse_runs.py",
        "generated_at": _dt.datetime.now().isoformat(timespec="seconds"),
        "units": {
            "sizes": "micrometres unless the field name ends in _m",
            "densities": "kg/m3",
            "conditions_per_particle":
                "counters 1-5 are divided by FI+N, counters 6-9 by FI (lines "
                "1248-1256).  BOTH divisors count BASE PARTICLES, not draws and "
                "not attempts: FI = I-1 (line 1154), which equals N at one "
                "cycle, and the fixtures confirm it "
                "(nbase_per_cycle == nbase_accepted_cumulative in 43/43).  So "
                "the first five are normalised on 2N - a divisor twice too "
                "large.  That is a defect of the original, not a second "
                "meaningful normalisation: reproduce it, but never add or "
                "compare the first five against the last four",
        },
        "known_quirks": [
            "the .dat files are working files kept only in their LAST state: 20 "
            "of the 43 no longer describe the run whose .m names them (the hp* "
            "series were re-pointed at 100000 base particles, hp1801 and "
            "r_p35050n gained fractions, res_02 had its distribution law "
            "switched).  input_dat.agrees_with_m says which are still usable as "
            "an input record; where it is false the .dat is a later revision and "
            "replaying it would run a different case",
            "exactly ONE element of pdoksmall is uninitialised - the fill loop "
            "starts at index 2.  The further anomalous entries are that same "
            "value carried forward by the monotone step (see "
            "derived.pdoksmall_uninitialised_value and _propagated_count).  In "
            "this archive the leftover word is ~1e-38 and inert: the three "
            "duplicate runs carry three different values and agree in every "
            "scalar and every other array.  A port may zero it; what it must "
            "not do is assume that is safe in general, because a large leftover "
            "would move Dmaxxx and change the run",
            "the model's printed step for fqmkm1 is wrong: it announces 0.01 "
            "but the source bins at int(ratio*1000)+1 and averages with 0.001. "
            "array_steps carries the source value and keeps the printed one "
            "under fqmkm1_as_printed",
            "AK1..AK4 (0.5 / 2.0 / 0.27 / 4.7 - the size-ratio window, the "
            "pocket-vs-bridge threshold and the reset radius) are read from the "
            ".dat and are NOT printed anywhere in the .m.  So a .m is not a "
            "complete input record, and the values quoted inside the condition "
            "labels are hardcoded text that would keep printing 0.5 whatever "
            "AK1 was",
            "eta (the agglomerated-oxide share) is not recorded by the model; it is "
            "recovered by inverting da_coef and reported under derived.recovered_eta",
            "the six random streams are one orbit of the same generator at "
            "consecutive offsets, not independent sequences: the position variate "
            "equals the previous draw of the fraction variate, exactly and "
            "forever.  epsx cannot detect this because it averages each stream "
            "separately",
            "self_check validates the parser only.  Every check is satisfiable "
            "without simulating anything, so a port must not count them as "
            "coverage",
        ],
        "skipped": [{"id": i, "reason": r} for i, r in skipped],
        "runs": runs,
    }

    with open(args.output, "w", encoding="utf-8") as fh:
        json.dump(document, fh, indent=1, ensure_ascii=False)
        fh.write("\n")

    flagged = [r for r in runs if r["self_check"]]
    checks = sum(r["self_check_count"] for r in runs)
    print(f"{len(runs)} runs -> {args.output}", file=sys.stderr)
    print(
        f"{len(skipped)} skipped; {checks} self-checks run, "
        f"{sum(len(r['self_check']) for r in runs)} findings in {len(flagged)} runs",
        file=sys.stderr,
    )

    if args.report:
        print(
            f"{'id':<13}{'input':<15}{'N':>8}{'x':>6}{'comp':>7}{'eta':>6}"
            f"{'Zkarm':>9}{'Dkarm43':>9}{'Dagg43':>8}  notes"
        )
        for run in sorted(runs, key=lambda r: r["id"]):
            s, d = run["expected"]["scalars"], run["derived"]
            notes = []
            if run.get("duplicate_of"):
                notes.append(f"= {run['duplicate_of']}")
            notes += run["self_check"]
            x, eta = d.get("fine_ap_mass_share_x"), d.get("recovered_eta")
            print(
                f"{run['id']:<13}{run['input']['input_filename'] or '?':<15}"
                f"{run['input']['base_particles']:>8}"
                f"{('%.2f' % x) if x is not None else '':>6}"
                f"{d.get('bas_composition') or '':>7}"
                f"{('%.3f' % eta) if eta is not None else '  n/a':>6}"
                f"{s['zkarm']:>9.4f}{s['dkarm43_v1']:>9.2f}{s['dagg43_v1']:>8.2f}"
                f"  {'; '.join(notes)}"
            )
        for stem, reason in skipped:
            print(f"{stem:<13}skipped: {reason}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
