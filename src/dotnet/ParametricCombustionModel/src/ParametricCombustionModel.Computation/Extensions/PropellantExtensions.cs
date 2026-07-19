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
        return GetAreaVolumeFraction(propellant, 1.0 - propellant.PocketMassFraction);
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
        return GetAreaVolumeFraction(propellant, propellant.PocketMassFraction);
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
    /// The metal (aluminum) melting temperature used by the model, in Kelvins.
    /// <para>
    /// PROVENANCE — this is a deliberate experimental setting of the current line of work, not a handbook
    /// constant and not a fitted parameter. The published value for this model is 2300 K; the 1300 K value
    /// used here is the setting that reconciles the model with the particle-size constraint on this branch.
    /// It is intentionally uniform across all compositions: the model currently assumes a single metal
    /// (aluminum) skeleton, so there is no composition-dependent melting temperature to resolve.
    /// </para>
    /// <para>
    /// Changing this value changes every computed result. Do not "correct" it to 2300 K without the
    /// model owner's approval.
    /// </para>
    /// </summary>
    public const double MetalMeltingTemperatureKelvins = 1300;

    /// <summary>
    /// Gets the metal melting temperature in Kelvins.
    /// </summary>
    /// <remarks>
    /// The value is composition-independent by design — see <see cref="MetalMeltingTemperatureKelvins"/>
    /// for the provenance. This method deliberately takes no <see cref="Propellant"/>: the previous
    /// extension-method signature accepted one and ignored it, which suggested a per-composition lookup
    /// that does not exist.
    /// </remarks>
    /// <returns>
    /// The melting temperature of the metal in Kelvins.
    /// </returns>
    public static double GetMetalMeltingTemperature()
    {
        return MetalMeltingTemperatureKelvins;
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
    /// Calculates the area volume fraction of one of the two mutually exclusive regions of the propellant
    /// (the pocket region or the inter-pocket region).
    /// <para>
    /// Both regions are built from the same compound (aluminum + combustible binder + the small-particle
    /// fraction of ammonium perchlorate) and differ only in how much of that compound they hold: the pocket
    /// region takes <c>PocketMassFraction</c> of it and the inter-pocket region takes the complementary
    /// <c>1 - PocketMassFraction</c>. The compound is assumed to keep the same internal component proportions
    /// in both regions, so the per-component mass fractions are simply scaled by the region's share.
    /// </para>
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the calculation is performed.
    /// </param>
    /// <param name="regionCompoundMassShare">
    /// The share of the compound belonging to the region: <c>PocketMassFraction</c> for the pocket region,
    /// <c>1 - PocketMassFraction</c> for the inter-pocket region.
    /// </param>
    /// <returns>
    /// The area volume fraction of the requested region.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required component (Aluminum, CombustibleBinder, or AmmoniumPerchlorate) is not found in the propellant.
    /// </exception>
    private static double GetAreaVolumeFraction(
        Propellant propellant,
        double regionCompoundMassShare)
    {
        var al = propellant.Components.OfType<Aluminum>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(Aluminum));
        var cb = propellant.Components.OfType<CombustibleBinder>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(CombustibleBinder));
        var ap = propellant.Components.OfType<AmmoniumPerchlorate>().FirstOrDefault()
                 ?? throw new ArgumentNullException(nameof(AmmoniumPerchlorate));

        var cmf = GetCompoundMassFraction(propellant);
        var r_cmf = regionCompoundMassShare * cmf;

        var apsf = ap.MassFraction * ap.SmallParticlesFraction;

        var c_almf = al.MassFraction / cmf;
        var c_cbmf = cb.MassFraction / cmf;
        var c_apsf = apsf / cmf;

        var r_almf = r_cmf * c_almf;
        var r_cbmf = r_cmf * c_cbmf;
        var r_apsf = r_cmf * c_apsf;

        var r_alvf = GetComponentVolumeFraction(propellant, r_almf, al.Density);
        var r_cbvf = GetComponentVolumeFraction(propellant, r_cbmf, cb.Density);
        var r_apvf = GetComponentVolumeFraction(propellant, r_apsf, ap.Density);

        return r_alvf + r_cbvf + r_apvf;
    }

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
