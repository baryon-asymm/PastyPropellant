import json
from typing import Any, Dict, List

from models import Propellant, PropellantComponent, RegionCalculationResult


def _require(data: Dict[str, Any], key: str, context: str) -> Any:
    """
    Read a required key, raising a message that names the file and the offending record.

    Every key routed through here is a measured physical quantity with no safe
    default, so a missing one is reported rather than substituted.

    Args:
        data (Dict[str, Any]): The JSON object to read from.
        key (str): The required key.
        context (str): Human-readable description of where `data` came from.

    Returns:
        Any: The value at `key`.

    Raises:
        KeyError: If `key` is absent.
    """
    if key not in data:
        raise KeyError(
            f"Missing required field '{key}' in {context}. "
            f"Fields present: {sorted(data)}."
        )
    return data[key]


def load_propellants(file_path: str) -> List[Propellant]:
    with open(file_path, 'r') as f:
        data = json.load(f)

    if not isinstance(data, list):
        raise ValueError(
            f"Expected '{file_path}' to contain a JSON array of propellants, "
            f"got {type(data).__name__}."
        )

    propellants = []
    for index, item in enumerate(data):
        record = f"propellant #{index} of '{file_path}'"
        name = _require(item, 'name', record)
        record = f"propellant '{name}' of '{file_path}'"

        components = {}
        for comp_name, comp_data in _require(item, 'components', record).items():
            comp_context = f"component '{comp_name}' of {record}"
            components[comp_name] = PropellantComponent(
                mass_fraction=_require(comp_data, 'mass_fraction', comp_context),
                density=_require(comp_data, 'density', comp_context),
                large_particles_fraction=comp_data.get('large_particles_fraction'),
                agglomeration_coefficients=comp_data.get('agglomeration_coefficients')
            )

        propellant = Propellant(
            name=name,
            density=_require(item, 'density', record),
            components=components
        )
        propellants.append(propellant)
    return propellants


def load_region_result(file_path: str) -> RegionCalculationResult:
    with open(file_path, 'r') as f:
        data = json.load(f)

    context = f"region result file '{file_path}'"
    return RegionCalculationResult(
        pressure=_require(data, 'pressure', context),
        enthalpy=_require(data, 'enthalpy', context),
        composition=_require(data, 'composition', context)
    )