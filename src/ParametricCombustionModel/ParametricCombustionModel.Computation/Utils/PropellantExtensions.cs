using System.Collections.ObjectModel;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Core.Models.PropellantComponents;

namespace ParametricCombustionModel.Computation.Utils;

public static class PropellantExtensions
{
    public static double GetInterPocketAreaVolumeFraction(this Propellant propellant)
    {
        var al = propellant.Components.OfType<Aluminum>().FirstOrDefault() ??
                 throw new ArgumentNullException(nameof(Aluminum));
        var cb = propellant.Components.OfType<CombustibleBinder>().FirstOrDefault() ??
                 throw new ArgumentNullException(nameof(CombustibleBinder));
        var ap = propellant.Components.OfType<AmmoniumPerchlorate>().FirstOrDefault() ??
                 throw new ArgumentNullException(nameof(AmmoniumPerchlorate));

        var cmf = GetCompoundMassFraction(propellant);
        var ip_cmf = (1.0 - propellant.PocketMassFraction) * cmf;
        
        var apsf = ap.MassFraction * ap.SmallParticlesFraction;

        var c_almf = al.MassFraction / cmf;
        var c_cbmf = cb.MassFraction / cmf;
        var c_apsf = apsf / cmf;
        
        var ip_almf = ip_cmf * c_almf;
        var ip_cbmf = ip_cmf * c_cbmf;
        var ip_apsf = ip_cmf * c_apsf;
        
        var ip_alvf = GetComponentVolumeFraction(propellant, ip_almf, al.Density);
        var ip_cbvf = GetComponentVolumeFraction(propellant, ip_cbmf, cb.Density);
        var ip_apvf = GetComponentVolumeFraction(propellant, ip_apsf, ap.Density);

        return ip_alvf + ip_cbvf + ip_apvf;
    }
    
    public static double GetPocketAreaVolumeFraction(this Propellant propellant)
    {
        var al = propellant.Components.OfType<Aluminum>().FirstOrDefault() ??
                 throw new ArgumentNullException(nameof(Aluminum));
        var cb = propellant.Components.OfType<CombustibleBinder>().FirstOrDefault() ??
                 throw new ArgumentNullException(nameof(CombustibleBinder));
        var ap = propellant.Components.OfType<AmmoniumPerchlorate>().FirstOrDefault() ??
                 throw new ArgumentNullException(nameof(AmmoniumPerchlorate));

        var cmf = GetCompoundMassFraction(propellant);
        var p_cmf = propellant.PocketMassFraction * cmf;
        
        var apsf = ap.MassFraction * ap.SmallParticlesFraction;

        var c_almf = al.MassFraction / cmf;
        var c_cbmf = cb.MassFraction / cmf;
        var c_apsf = apsf / cmf;
        
        var p_almf = p_cmf * c_almf;
        var p_cbmf = p_cmf * c_cbmf;
        var p_apsf = p_cmf * c_apsf;
        
        var p_alvf = GetComponentVolumeFraction(propellant, p_almf, al.Density);
        var p_cbvf = GetComponentVolumeFraction(propellant, p_cbmf, cb.Density);
        var p_apvf = GetComponentVolumeFraction(propellant, p_apsf, ap.Density);

        return p_alvf + p_cbvf + p_apvf;
    }

    private static double GetComponentVolumeFraction(Propellant propellant,
                                                     double componentMassFraction,
                                                     double componentDensity)
    {
        return componentMassFraction / componentDensity * propellant.Density;
    }

    /// <summary>
    /// The Compound mass fraction. The Compound is mixture of aluminum, combustible binder and ammonium perchlorate.
    /// </summary>
    /// <param name="propellant"></param>
    /// <returns>Mass fraction of the Compound.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    private static double GetCompoundMassFraction(Propellant propellant)
    {
        var al = propellant.Components.OfType<Aluminum>().FirstOrDefault() ??
                 throw new ArgumentNullException(nameof(Aluminum));
        var cb = propellant.Components.OfType<CombustibleBinder>().FirstOrDefault() ??
                 throw new ArgumentNullException(nameof(CombustibleBinder));
        var ap = propellant.Components.OfType<AmmoniumPerchlorate>().FirstOrDefault() ??
                 throw new ArgumentNullException(nameof(AmmoniumPerchlorate));

        var apsf = ap.MassFraction * ap.SmallParticlesFraction;

        return al.MassFraction + cb.MassFraction + apsf;
    }

    private static double GetInterPocketAreaDensity(Propellant propellant,
                                                    AmmoniumPerchlorate ammoniumPerchlorate,
                                                    Octogen octogen)
    {
        var pd = propellant.Density;

        var aplf = ammoniumPerchlorate.LargeParticlesFraction;
        var apmf = ammoniumPerchlorate.MassFraction;
        var apd = ammoniumPerchlorate.Density;

        var ocmf = octogen.MassFraction;
        var ocd = octogen.Density;

        return (1 - aplf * apmf - ocmf) / (1 / pd - aplf * apmf / apd - ocmf / ocd);
    }

    public static double GetOctogenAreaVolumeFraction(this Propellant propellant)
    {
        var octogen = propellant.Components.OfType<Octogen>().FirstOrDefault() ??
                      throw new ArgumentNullException(nameof(Octogen));

        var ocmf = octogen.MassFraction;
        var pd = propellant.Density;
        var ocd = octogen.Density;

        return ocmf * pd / ocd;
    }

    public static double GetAverageParticlesDiameter(this Propellant propellant)
    {
        var ammoniumPerchlorate = propellant.Components.OfType<AmmoniumPerchlorate>().FirstOrDefault() ??
                                  throw new ArgumentNullException(nameof(AmmoniumPerchlorate));

        return ammoniumPerchlorate.AverageParticlesDiameter;
    }

    public static double GetPocketSurfaceFraction(this Propellant propellant, double pressure)
    {
        var pocketSurfaceFraction = 0.0;
        var coefficients = propellant.PocketSurfaceFractionCoefficients.ToArray().AsReadOnly();
        for (var i = 0; i < coefficients.Count; i++)
            pocketSurfaceFraction += coefficients[i] * Math.Pow(pressure / 1e6, i);
        return pocketSurfaceFraction / propellant.PocketMassFraction;
    }

    public static double GetMetalMeltingTemperature(this Propellant propellant)
    {
        return 2300;
    }

    public static double GetMetalBoilingTemperature(this Propellant propellant, double pressure)
    {
        var boilingTemperatures = new[]
        {
            2800.0,
            3000,
            3100,
            3200,
            3250,
            3300,
            3350,
            3400,
            3450,
            3490
        };

        var boilingPressures = new double[10];
        for (var i = 0; i < boilingPressures.Length; i++)
            boilingPressures[i] = 1e6 + (10e6 - 1e6) / 9 * i;

        double x1 = boilingPressures[0],
            y1 = boilingTemperatures[0],
            x2 = boilingPressures[1],
            y2 = boilingTemperatures[1];
        for (var i = 2; i < boilingPressures.Length; i++)
            if (pressure > x2)
            {
                x1 = x2;
                y1 = y2;
                x2 = boilingPressures[i];
                y2 = boilingTemperatures[i];
            }

        return LinearInterpolation(pressure, x1, y1, x2, y2);
    }

    private static double LinearInterpolation(double x,
                                              double x1,
                                              double y1,
                                              double x2,
                                              double y2)
    {
        return y1 + (x - x1) * (y2 - y1) / (x2 - x1);
    }

    public static ReadOnlyCollection<ReadOnlyCollection<double>> GetExperimentalBurningRates(
        this IEnumerable<Propellant> propellants,
        IEnumerable<double> pressures)
    {
        using var propellantsIterator = propellants.GetEnumerator();
        var experimentalBurningRates = new List<ReadOnlyCollection<double>>();
        for (var i = 0; i < propellants.Count(); i++)
        {
            propellantsIterator.MoveNext();
            using var pressuresIterator = pressures.GetEnumerator();
            var experimentalBurningRateByPressures = new List<double>();
            for (var j = 0; j < pressures.Count(); j++)
            {
                pressuresIterator.MoveNext();
                experimentalBurningRateByPressures.Add(propellantsIterator.Current
                                                                          .GetExperimentalBurningRate(pressuresIterator
                                                                                                          .Current));
            }

            experimentalBurningRates.Add(experimentalBurningRateByPressures.AsReadOnly());
        }

        return experimentalBurningRates.AsReadOnly();
    }

    public static double GetExperimentalBurningRate(this Propellant propellant, double pressure)
    {
        return propellant.A * Math.Pow(pressure, propellant.Nu);
    }

    public static bool IsAnyoneOutOfRange(this Span<double> span, (double, double) range)
    {
        foreach (var item in span)
            if (item < range.Item1 || item > range.Item2)
                return true;

        return false;
    }
}
/*
 *
 * _MELTING_TEMPERATURE = 2300 # K

    _BOILING_PRESSURES = linspace(start=1e6, stop=10e6, num=10) # Pa
    _BOILING_TEMPERATURES = [2800, 3000, 3100, 3200, 3250, 3300, 3350, 3400, 3450, 3490] # K
 */
