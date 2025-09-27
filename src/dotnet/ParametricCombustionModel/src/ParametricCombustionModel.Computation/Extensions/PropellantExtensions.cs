using System.Collections.ObjectModel;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Core.Models.PropellantComponents;

namespace ParametricCombustionModel.Computation.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Propellant"/> to compute various parameters related to combustion.
/// </summary>
public static class PropellantExtensions
{
#region Public Methods

    /// <summary>
    /// Calculates the area volume fraction of inter-pocket regions in the propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the calculation is performed.
    /// </param>
    /// <returns>
    /// The area volume fraction of the inter-pocket regions.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required component (Aluminum, CombustibleBinder, or AmmoniumPerchlorate) is not found in the propellant.
    /// </exception>
    public static double GetInterPocketAreaVolumeFraction(
        this Propellant propellant)
    {
        var al = propellant.Components.OfType<Aluminum>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(Aluminum));
        var cb = propellant.Components.OfType<CombustibleBinder>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(CombustibleBinder));
        var ap = propellant.Components.OfType<AmmoniumPerchlorate>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(AmmoniumPerchlorate));

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

    /// <summary>
    /// Calculates the area volume fraction of pocket regions in the propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the calculation is performed.
    /// </param>
    /// <returns>
    /// The area volume fraction of the pocket regions.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required component (Aluminum, CombustibleBinder, or AmmoniumPerchlorate) is not found in the propellant.
    /// </exception>
    public static double GetPocketAreaVolumeFraction(
        this Propellant propellant)
    {
        var al = propellant.Components.OfType<Aluminum>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(Aluminum));
        var cb = propellant.Components.OfType<CombustibleBinder>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(CombustibleBinder));
        var ap = propellant.Components.OfType<AmmoniumPerchlorate>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(AmmoniumPerchlorate));

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

    /// <summary>
    /// Gets the average particle diameter of ammonium perchlorate in the propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the average particle diameter is retrieved.
    /// </param>
    /// <returns>
    /// The average particle diameter of ammonium perchlorate.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when ammonium perchlorate is not found in the propellant.
    /// </exception>
    public static double GetAverageParticlesDiameter(
        this Propellant propellant)
    {
        var ammoniumPerchlorate = propellant.Components.OfType<AmmoniumPerchlorate>().FirstOrDefault()
                                  ?? throw new ArgumentNullException(nameof(AmmoniumPerchlorate));

        return ammoniumPerchlorate.AverageParticlesDiameter;
    }

    /// <summary>
    /// Calculates the pocket surface fraction based on the given pressure.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the pocket surface fraction is calculated.
    /// </param>
    /// <param name="pressure">
    /// The pressure in Pascals.
    /// </param>
    /// <returns>
    /// The pocket surface fraction.
    /// </returns>
    public static double GetPocketSurfaceFraction(
        this Propellant propellant,
        double pressure)
    {
        var pocketSurfaceFraction = 0.0;
        var coefficients = propellant.PocketSurfaceFractionCoefficients.ToArray().AsReadOnly();
        for (var i = 0; i < coefficients.Count; i++)
            pocketSurfaceFraction += coefficients[i] * Math.Pow(pressure / 1e6, i);
        return pocketSurfaceFraction / propellant.PocketMassFraction;
    }

    /// <summary>
    /// Gets the metal melting temperature in Kelvins.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the melting temperature is retrieved.
    /// </param>
    /// <returns>
    /// The melting temperature of the metal in Kelvins.
    /// </returns>
    public static double GetMetalMeltingTemperature(
        this Propellant propellant)
    {
        return 2300;
    }

    /// <summary>
    /// Calculates the metal boiling temperature based on the given pressure.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the boiling temperature is calculated.
    /// </param>
    /// <param name="pressure">
    /// The pressure in Pascals.
    /// </param>
    /// <returns>
    /// The boiling temperature of the metal in Kelvins.
    /// </returns>
    public static double GetMetalBoilingTemperature(
        this Propellant propellant,
        double pressure)
    {
        double[] boilingPolynomialCoefficients =
        [
            2421.3333276590047,
            528.1279787134719,
            -186.21637717215654,
            42.95032033617693,
            -5.844551153396972,
            0.4240384454098416,
            -0.012499999369973086
        ];

        var boilingTemperature = 0.0;
        for (var i = 0; i < boilingPolynomialCoefficients.Length; i++)
            boilingTemperature += boilingPolynomialCoefficients[i] * Math.Pow(pressure / 1e6, i);
        
        return boilingTemperature;
    }

#endregion

#region Private Methods

    /// <summary>
    /// Calculates the volume fraction of a component in the propellant based on its mass fraction and density.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the volume fraction is calculated.
    /// </param>
    /// <param name="componentMassFraction">
    /// The mass fraction of the component.
    /// </param>
    /// <param name="componentDensity">
    /// The density of the component.
    /// </param>
    /// <returns>
    /// The volume fraction of the component.
    /// </returns>
    private static double GetComponentVolumeFraction(
        Propellant propellant,
        double componentMassFraction,
        double componentDensity)
    {
        return componentMassFraction / componentDensity * propellant.Density;
    }

    /// <summary>
    /// Calculates the compound mass fraction, which is the sum of the mass fractions of aluminum, combustible binder,
    /// and ammonium perchlorate (adjusted for small particles).
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the compound mass fraction is calculated.
    /// </param>
    /// <returns>
    /// The compound mass fraction.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required component (Aluminum, CombustibleBinder, or AmmoniumPerchlorate) is not found in the propellant.
    /// </exception>
    private static double GetCompoundMassFraction(
        Propellant propellant)
    {
        var al = propellant.Components.OfType<Aluminum>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(Aluminum));
        var cb = propellant.Components.OfType<CombustibleBinder>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(CombustibleBinder));
        var ap = propellant.Components.OfType<AmmoniumPerchlorate>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(AmmoniumPerchlorate));

        var apsf = ap.MassFraction * ap.SmallParticlesFraction;

        return al.MassFraction + cb.MassFraction + apsf;
    }

#endregion
}
