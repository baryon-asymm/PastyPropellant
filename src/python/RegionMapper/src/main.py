import argparse
import os
import sys

from json_reader import read_components, read_propellants
from region_mappers import (
    InterPocketRegionMapper,
    PocketRegionWithoutSkeletonMapper,
    PocketRegionWithSkeletonMapper,
    DiffusionRegionMapper
)
from calculators import RegionCalculator
from json_writer import JSONWriter

def parse_args():
    """
    Parse command-line arguments.

    Returns:
        argparse.Namespace: Parsed arguments.
    """
    parser = argparse.ArgumentParser(
        description="Calculate and export region data for a propellant."
    )
    parser.add_argument(
        "--propellant",
        required=True,
        help="Path to the propellant JSON file (e.g., propellant.json)."
    )
    parser.add_argument(
        "--components",
        required=True,
        help="Path to the components JSON file (e.g., components.json)."
    )
    parser.add_argument(
        "--pressure",
        type=float,
        required=True,
        help="Pressure in Pascals (e.g., 1e6)."
    )
    parser.add_argument(
        "--output-dir",
        required=True,
        help="Path to the output directory where results will be stored."
    )

    # Parse arguments
    args = parser.parse_args()

    # Validate arguments
    if args.pressure <= 0:
        parser.error("Pressure must be a positive value.")

    return args


def ensure_directory_exists(directory_path):
    """
    Ensure that the directory exists. If not, create it.

    Args:
        directory_path (str): The path to the directory.
    """
    if not os.path.exists(directory_path):
        os.makedirs(directory_path)
        print(f"Created directory: {directory_path}")


def main():
    """
    Main function to calculate and export region data.
    """
    try:
        # Parse command-line arguments
        args = parse_args()

        # Load data
        components = read_components(args.components)
        propellants = read_propellants(args.propellant)

        # Ensure the output directory exists
        ensure_directory_exists(args.output_dir)

        # Process each propellant
        for propellant in propellants:
            # Create a subdirectory for the propellant
            propellant_output_dir = os.path.join(args.output_dir, propellant.name)
            ensure_directory_exists(propellant_output_dir)

            # Initialize mappers
            inter_pocket_mapper = InterPocketRegionMapper()
            pocket_without_skeleton_mapper = PocketRegionWithoutSkeletonMapper()
            pocket_with_skeleton_mapper = PocketRegionWithSkeletonMapper()
            diffusion_mapper = DiffusionRegionMapper()

            # Perform calculations for each region
            inter_pocket_data = inter_pocket_mapper.calculate(propellant)
            pocket_without_skeleton_data = pocket_without_skeleton_mapper.calculate(propellant)
            pocket_with_skeleton_data = pocket_with_skeleton_mapper.calculate(propellant)
            diffusion_data = diffusion_mapper.calculate(propellant, args.pressure)

            # Calculate results using RegionCalculator
            inter_pocket_result = RegionCalculator.calculate(inter_pocket_data, components, args.pressure)
            pocket_without_skeleton_result = RegionCalculator.calculate(pocket_without_skeleton_data, components, args.pressure)
            pocket_with_skeleton_result = RegionCalculator.calculate(pocket_with_skeleton_data, components, args.pressure)
            diffusion_result = RegionCalculator.calculate(diffusion_data, components, args.pressure)

            # Define output file paths
            inter_pocket_file = os.path.join(propellant_output_dir, "inter_pocket.json")
            pocket_without_skeleton_file = os.path.join(propellant_output_dir, "pocket_without_skeleton.json")
            pocket_with_skeleton_file = os.path.join(propellant_output_dir, "pocket_with_skeleton.json")
            diffusion_file = os.path.join(propellant_output_dir, "diffusion.json")

            # Write results to JSON files
            JSONWriter.write(inter_pocket_result, inter_pocket_file)
            JSONWriter.write(pocket_without_skeleton_result, pocket_without_skeleton_file)
            JSONWriter.write(pocket_with_skeleton_result, pocket_with_skeleton_file)
            JSONWriter.write(diffusion_result, diffusion_file)

            print(f"All results for propellant '{propellant.name}' successfully written to '{propellant_output_dir}'.")

    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
