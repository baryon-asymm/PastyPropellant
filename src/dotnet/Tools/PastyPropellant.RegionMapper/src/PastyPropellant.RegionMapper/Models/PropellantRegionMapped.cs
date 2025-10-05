using System.Collections.ObjectModel;
using UnitsNet;

namespace PastyPropellant.RegionMapper.Models;

public record PressureFrame(
    Pressure Pressure,
    string InterPocketFilePath,
    string PocketWithoutSkeletonFilePath,
    string PocketWithSkeletonFilePath,
    string DiffusionFilePath
);

public record PropellantRegionMapped(
    string Name,
    ReadOnlyCollection<PressureFrame> PressureFrames
);
