import json

from typing import Dict
from dataclasses import asdict

from calculators import RegionCalculationResult

class JSONWriter:
    """
    Writes the results of region calculations to a JSON file.

    Methods:
        write(result: RegionCalculationResult, file_path: str) -> None:
            Writes the calculation result to the specified file in JSON format.
    """

    @staticmethod
    def write(result: RegionCalculationResult, file_path: str) -> None:
        """
        Write the calculation result to a JSON file.

        Args:
            result (RegionCalculationResult): The result of the region calculation.
            file_path (str): The path to the output JSON file.

        Example:
            result = RegionCalculationResult(
                pressure=1e6,
                enthalpy=-1250.0,
                composition={"C": 0.125, "H": 0.5, "O": 1.0, "N": 0.125, "Cl": 0.125, "Al": 0.15}
            )
            JSONWriter.write(result, "output.json")
        """
        # Convert the dataclass to a dictionary
        result_dict = asdict(result)

        # Write the dictionary to a JSON file
        with open(file_path, "w", encoding="utf-8") as file:
            json.dump(result_dict, file, indent=4, ensure_ascii=False)

        print(f"Results successfully written to {file_path}")
