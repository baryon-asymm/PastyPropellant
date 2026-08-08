"""One shard of the phi_C(T_pore, p) scan behind the f_s inverse problem.

A single Gibbs minimisation takes 6..17 s and the library threads it across the whole
machine, so running the sweep as one process is slower than running many single-threaded
ones: the scan is embarrassingly parallel in (propellant, pressure, temperature).  This is
the worker.  Pin the BLAS threads to 1 and start as many shards as there are cores.

Each shard writes its own JSONL file, so nothing is shared and nothing is lost if one dies.
`invert_skeleton_surface_fraction.py` reads them all back together.

USAGE
  OMP_NUM_THREADS=1 python scan_carbon_shard.py <shard index> <shard count> <output file>
                                                [comma-separated T_pore, K]
"""
from __future__ import annotations

import json
import os
import sys
import time

ROOT = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, ROOT)

from generate_skeleton_carbon_equilibrium import (  # noqa: E402
    CARBON_MOLAR_MASS,
    CARBON_RESIDUE_DENSITY,
    condensed_carbon,
    pocket_matrix,
    pocket_volumetry,
)

# The whole picture in one grid: the cold branch (T_s = -900..-100 K), the reachable window
# ends (T_s = 0 and T_s = T_m), and the carbon maximum near T_pore = 1750 K.  The interior
# 1450..1600 K is already in data/skeleton_carbon_equilibrium.json and is not repeated.
PORE_TEMPERATURES_K = [700.0, 850.0, 1000.0, 1100.0, 1150.0, 1300.0,
                       1750.0, 1900.0, 2050.0, 2200.0, 2300.0]


def main() -> None:
    index, count, target = int(sys.argv[1]), int(sys.argv[2]), sys.argv[3]
    temperatures = [float(t) for t in sys.argv[4].split(",")] if len(sys.argv) > 4 \
        else PORE_TEMPERATURES_K
    propellants = json.load(open(os.path.join(ROOT, "data/propellants.01234.json")))

    jobs = []
    for propellant in propellants:
        for frame in propellant["pressure_frames"]:
            for pore_temperature in temperatures:
                jobs.append((propellant, frame["pressure"], pore_temperature))

    started = time.time()
    with open(target, "w") as sink:
        for job, (propellant, pressure, pore_temperature) in enumerate(jobs):
            if job % count != index:
                continue
            matrix = pocket_matrix(propellant)
            specific_volume, aluminium_fraction = pocket_volumetry(propellant)
            stable = condensed_carbon(matrix, pore_temperature, pressure)
            sink.write(json.dumps({
                "name": propellant["name"],
                "pressure": pressure,
                "poreTemperature": pore_temperature,
                "condensedCarbonMolesPerKilogram": stable,
                "carbonFraction": stable * CARBON_MOLAR_MASS / CARBON_RESIDUE_DENSITY / specific_volume,
                "aluminiumVolumeFraction": aluminium_fraction,
            }) + "\n")
            sink.flush()

    print(f"shard {index}/{count} done in {time.time() - started:.0f} s")


if __name__ == "__main__":
    main()
