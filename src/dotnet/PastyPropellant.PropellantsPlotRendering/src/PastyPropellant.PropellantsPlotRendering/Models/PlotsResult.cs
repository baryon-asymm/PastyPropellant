namespace PastyPropellant.PropellantsPlotRendering.Models;

public record PlotsResult(
    string ThermalConductivityPlotPath,
    string AverageMolecularWeightPlotPath,
    string ConstantVolumeSpecificHeatPlotPath,
    string TemperaturePlotPath,
    string AgglomerationFactorPlotPath,
    string SkeletonSurfaceFactorPlotPath
);
