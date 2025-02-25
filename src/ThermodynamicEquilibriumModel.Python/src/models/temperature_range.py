"""Module for representing temperature ranges.

This module defines the TemperatureRange class, which encapsulates a temperature range with minimum
and maximum values.
"""

from dataclasses import dataclass

@dataclass(frozen=True)
class TemperatureRange:
    """Represents a temperature range.

    Attributes:
        min (float): The minimum temperature.
        max (float): The maximum temperature.
    """
    min: float
    max: float
