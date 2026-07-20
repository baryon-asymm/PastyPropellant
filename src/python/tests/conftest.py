"""Shared fixtures for the src/python/* test suite.

Import strategy
---------------
None of the three packages under ``src/python`` is an importable Python package:
each is a flat directory whose modules import their siblings by bare name
(``from models import Component``). Worse, ``RegionMapper`` and
``PorosityCalculation`` each define a module named ``models``, ``molar_masses``,
``json_reader``, ``json_writer`` and ``calculators`` — with *different* contents.
Putting both directories on ``sys.path`` simultaneously would let whichever came
first shadow the other, and the tests would silently exercise the wrong code.

So each package is imported once, in isolation: its source directory is pushed onto
``sys.path``, the colliding names are evicted from ``sys.modules``, the modules are
imported, and then the names are evicted again. The returned module objects keep
direct references to everything they imported at module-import time (all imports in
these packages are top-level), so they stay fully functional after the eviction.

This is deliberately done at conftest import time rather than lazily in a fixture:
the isolation only works if the loads do not interleave.
"""

import importlib
import os
import sys
from pathlib import Path
from types import SimpleNamespace

import pytest

# Rendering is never exercised, but PropellantsPlotRendering imports pyplot at module
# level, so pin a headless backend before anything can pick an interactive one.
os.environ.setdefault("MPLBACKEND", "Agg")

PYTHON_ROOT = Path(__file__).resolve().parent.parent
REPO_ROOT = PYTHON_ROOT.parent.parent
DATA_DIR = REPO_ROOT / "data"

# Every module name that appears in more than one package, plus the package-local
# ones, so a load never sees a stale sibling from a previously loaded package.
_COLLIDING_NAMES = (
    "calculators",
    "json_reader",
    "json_writer",
    "main",
    "models",
    "molar_masses",
    "region_mappers",
    "skeleton_layer_plots",
    "utils",
)


def _load_isolated(src_dir: Path, names) -> SimpleNamespace:
    """Import `names` from `src_dir` with the colliding module names quarantined."""
    saved = {name: sys.modules.pop(name, None) for name in _COLLIDING_NAMES}
    sys.path.insert(0, str(src_dir))
    try:
        loaded = {name: importlib.import_module(name) for name in names}
    finally:
        sys.path.remove(str(src_dir))
        for name in _COLLIDING_NAMES:
            sys.modules.pop(name, None)
            if saved[name] is not None:
                sys.modules[name] = saved[name]
    return SimpleNamespace(**loaded)


_REGION_MAPPER = _load_isolated(
    PYTHON_ROOT / "RegionMapper" / "src",
    ("models", "molar_masses", "utils", "region_mappers", "calculators",
     "json_reader", "json_writer", "main"),
)

_POROSITY = _load_isolated(
    PYTHON_ROOT / "PorosityCalculation" / "src",
    ("models", "molar_masses", "json_reader", "json_writer", "calculators", "main"),
)

_PLOTTING = _load_isolated(
    PYTHON_ROOT / "PropellantsPlotRendering" / "src",
    ("main", "skeleton_layer_plots"),
)


@pytest.fixture(scope="session")
def region_mapper():
    """The RegionMapper package's modules."""
    return _REGION_MAPPER


@pytest.fixture(scope="session")
def porosity():
    """The PorosityCalculation package's modules."""
    return _POROSITY


@pytest.fixture(scope="session")
def plotting():
    """The PropellantsPlotRendering package's modules."""
    return _PLOTTING


@pytest.fixture(scope="session")
def shipped_propellants_path():
    """Path to the checked-in propellants set the console host actually loads.

    Read-only: no test may write to anything under data/.
    """
    path = DATA_DIR / "propellants.01234.json"
    if not path.is_file():
        pytest.skip(f"shipped propellants file not present at {path}")
    return path


@pytest.fixture(scope="session")
def shipped_components_path():
    """Path to the checked-in component-composition file (read-only)."""
    path = DATA_DIR / "propellant_components.json"
    if not path.is_file():
        pytest.skip(f"shipped components file not present at {path}")
    return path


@pytest.fixture(scope="session")
def shipped_propellants(shipped_propellants_path):
    """The shipped propellants parsed as raw JSON, keyed by name."""
    import json

    with open(shipped_propellants_path, "r", encoding="utf-8") as handle:
        return {item["name"]: item for item in json.load(handle)}


# --- Builders for hand-made propellants -------------------------------------------
# The two packages have different `PropellantComponent` shapes (PorosityCalculation's
# carries a density, RegionMapper's does not), so each gets its own builder.


@pytest.fixture
def make_region_propellant(region_mapper):
    """Build a RegionMapper `Propellant` from {name: (mass_fraction, lpf, coeffs)}."""
    models = region_mapper.models

    def _build(name="Test", **components):
        built = {}
        for comp_name, spec in components.items():
            mass_fraction, lpf, coeffs = spec
            built[comp_name] = models.PropellantComponent(
                mass_fraction=mass_fraction,
                large_particles_fraction=lpf,
                agglomeration_coefficients=coeffs,
            )
        return models.Propellant(name=name, components=built)

    return _build


@pytest.fixture
def make_porosity_propellant(porosity):
    """Build a PorosityCalculation `Propellant` from {name: (mass_fraction, density)}."""
    models = porosity.models

    def _build(name="Test", density=1500.0, **components):
        built = {
            comp_name: models.PropellantComponent(
                mass_fraction=mass_fraction,
                density=comp_density,
                large_particles_fraction=None,
                agglomeration_coefficients=None,
            )
            for comp_name, (mass_fraction, comp_density) in components.items()
        }
        return models.Propellant(name=name, density=density, components=built)

    return _build
