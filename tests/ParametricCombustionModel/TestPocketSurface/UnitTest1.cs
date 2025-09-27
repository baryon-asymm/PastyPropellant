using System.Collections.ObjectModel;
using System.Text.Json;
using MathNet.Numerics.Distributions;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Params;
using ParametricCombustionModel.Computation.Units;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.Models;
using UnitsNet;

namespace TestPocketSurface;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var propellants =
            GetPropellants(
                @"C:\Projects\PastyPropellant\src\ParametricCombustionModel\ParametricCombustionModel.ProcessWorker\bin\Release\data\propellant_bas_2.json");
        var solvers = MixedPropellantSolversBuilder.FromPropellants(propellants)
                                                   .ForPressures([Pressure.FromAtmospheres(43)])
                                                   .Build();
        double[] values =
        [
            1.0953581629649699E+308, 4089805.326986677, 39348106041.73285, 199136.43211252883, 14514342.274935542,
            50000.063738034034,
            1311867835.413114, 105642.08691187386, 1.3856, 25564624908.404716, 329530.9272203463, 622053.027122523,
            0.10394228947409136
        ];
        var burnParams = new BurnParams
        {
            ADecompose = MassFlux.FromKilogramsPerSecondPerSquareMeter(values[0]),
            EDecompose = MolarEnergy.FromJoulesPerMole(values[1]),
            AKineticFlameInterPocket = Frequency.FromHertz(values[2]),
            EKineticFlameInterPocket = MolarEnergy.FromJoulesPerMole(values[3]),
            AKineticFlamePocketOutSkeleton = Frequency.FromHertz(values[4]),
            EKineticFlamePocketOutSkeleton = MolarEnergy.FromJoulesPerMole(values[5]),
            AKineticFlamePocketSkeleton = Frequency.FromHertz(values[6]),
            EKineticFlamePocketSkeleton = MolarEnergy.FromJoulesPerMole(values[7]),
            Nu = values[8],
            HMetalBurning = HMetalBurningCoefficient.FromWattsPerKelvinPerSquareMeter(values[9]),
            EMetalBurning = MolarEnergy.FromJoulesPerMole(values[10]),
            DeltaH = SpecificEnergy.FromJoulesPerKilogram(values[11]),
            KDiffusionHeight = values[12]
        };

        var meanBurnRate = 0.0;
        var dispersionBurnRate = 0.0;
        var meanInterPocketSurfaceTemperature = 0.0;
        var dispersionInterPocketSurfaceTemperature = 0.0;
        var meanPocketSurfaceTemperature = 0.0;
        var dispersionPocketSurfaceTemperature = 0.0;
        var meanPocketSurfaceFraction = 0.0;
        var dispersionPocketSurfaceFraction = 0.0;
        var meanBasePocketSurfaceFraction = 0.0;
        var dispersionBasePocketSurfaceFraction = 0.0;
        for (int i = 0; i < 1000000; i++)
        {
            var sample = Normal.Sample(0.25, 0.015);
            ref var propellantParams = ref solvers[0][0].PocketPropellantSolver.GetPropellantParams();
            propellantParams.PocketSurfaceFraction = Ratio.FromDecimalFractions(
                sample / propellants[0].PocketMassFraction);
            // solvers[0][0].PocketPropellantSolver.SetPocketSurfaceFraction(Ratio.FromDecimalFractions(
            //     sample / propellants[0].PocketMassFraction));
            
            var surfaceTemperaturesSuccess = solvers[0][0]
                .TryGetSurfaceTemperatures(burnParams,
                                           out var interPocketSurfaceTemperature,
                                           out var pocketSurfaceTemperature);
            
            Assert.True(surfaceTemperaturesSuccess);

            var burnRate = solvers[0][0].GetBurnRate(interPocketSurfaceTemperature, pocketSurfaceTemperature, burnParams);
            var burnRateValue = burnRate.MillimetersPerSecond;
            
            UpdateMetrics(ref meanBurnRate, ref dispersionBurnRate, burnRateValue, i);
            UpdateMetrics(ref meanPocketSurfaceFraction, ref dispersionPocketSurfaceFraction, propellantParams.PocketSurfaceFraction.DecimalFractions, i);
            UpdateMetrics(ref meanBasePocketSurfaceFraction, ref dispersionBasePocketSurfaceFraction, propellantParams.PocketSurfaceFraction.DecimalFractions * propellants[0].PocketMassFraction, i);
            UpdateMetrics(ref meanInterPocketSurfaceTemperature, ref dispersionInterPocketSurfaceTemperature, interPocketSurfaceTemperature.Kelvins, i);
            UpdateMetrics(ref meanPocketSurfaceTemperature, ref dispersionPocketSurfaceTemperature, pocketSurfaceTemperature.Kelvins, i);
        }
        var sqrtDispersionBurnRate = Math.Sqrt(dispersionBurnRate);
        var sqrtDispersionInterPocketSurfaceTemperature = Math.Sqrt(dispersionInterPocketSurfaceTemperature);
        var sqrtDispersionPocketSurfaceTemperature = Math.Sqrt(dispersionPocketSurfaceTemperature);
        var sqrtDispersionPocketSurfaceFraction = Math.Sqrt(dispersionPocketSurfaceFraction);
        var sqrtDispersionBasePocketSurfaceFraction = Math.Sqrt(dispersionBasePocketSurfaceFraction);

        var tmp = 0;
    }
    
    private void UpdateMetrics(ref double mean, ref double dispersion, double value, int i)
    {
        var prevMean = mean;
        mean += (value - prevMean) / (i + 1);
        dispersion += ((value - prevMean) * (value - mean) - dispersion) / (i + 1);
    }

    private ReadOnlyCollection<Propellant> GetPropellants(
        string filePath)
    {
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = JsonSerializer.DeserializeAsync<List<Propellant>>(fileStream).Result;
        return result.AsReadOnly();
    }
}
