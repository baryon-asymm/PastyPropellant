using System.Globalization;
using UnitsNet;
using UnitsNet.Units;

namespace ParametricCombustionModel.Optimization.Tests.DependencyContracts;

/// <summary>
/// Characterises <b>UnitsNet 6.0.0-pre011</b> — the one dependency on a prerelease version, and so
/// the one with no semver obligation to keep any of this stable (tech-debt TEST-7).
///
/// The PDF reports build their table cells with <c>quantity.ToUnit(...).ToString()</c>. That output
/// is both <b>culture-sensitive</b> and <b>rounding-sensitive</b>, so a UnitsNet bump can silently
/// change every number in the report without touching a line of our code and without failing any
/// test of our own logic.
///
/// Everything here runs under <see cref="CultureInfo.InvariantCulture"/>, set explicitly per test.
/// Production does <b>not</b> pin the culture — it relies on the ambient one — so these pins record
/// the invariant-culture baseline rather than what a given machine will actually print. That gap is
/// noted in <see cref="DefaultToString_IsCultureSensitive_WhichProductionDoesNotPin"/>.
///
/// The five quantity/unit pairs pinned below are exactly the ones the reports print, confirmed by
/// tallying <c>ToUnit(...)</c> call sites across <c>src/dotnet</c>:
/// Speed/mm·s⁻¹ (8), Length/µm (8), HeatFlux/W·m⁻² (5), Temperature/K (2), Pressure/MPa (2).
/// </summary>
public class UnitsNetFormattingContractTests
{
    private static IDisposable InvariantCulture() => new CultureScope(CultureInfo.InvariantCulture);

    #region Formatted output — the exact strings the reports put in table cells

    [Theory]
    // value in m/s,     expected formatted mm/s
    [InlineData(0.00777, "7.77 mm/s")]   // the value ReportMaking.Tests/PressureTablesReportTests.cs:149 relies on
    [InlineData(0.001, "1 mm/s")]
    [InlineData(0.0, "0 mm/s")]
    [InlineData(0.0123456789, "12.35 mm/s")]  // rounds to 2 decimals, half-away-from-zero
    public void Speed_FormatsAsMillimetersPerSecond(double metersPerSecond, string expected)
    {
        using var _ = InvariantCulture();

        var actual = Speed.FromMetersPerSecond(metersPerSecond)
                          .ToUnit(SpeedUnit.MillimeterPerSecond)
                          .ToString();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(7_770_000, "7.77 MPa")]
    [InlineData(6_500_000, "6.5 MPa")]
    [InlineData(100_000, "0.1 MPa")]
    public void Pressure_FormatsAsMegapascals(double pascals, string expected)
    {
        using var _ = InvariantCulture();

        var actual = Pressure.FromPascals(pascals)
                             .ToUnit(PressureUnit.Megapascal)
                             .ToString();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(900, "900 K")]
    [InlineData(1300.5, "1,300.5 K")]   // note the group separator — invariant culture uses ","
    [InlineData(0.123456789, "0.12 K")]
    public void Temperature_FormatsAsKelvins(double kelvins, string expected)
    {
        using var _ = InvariantCulture();

        var actual = Temperature.FromKelvins(kelvins)
                                .ToUnit(TemperatureUnit.Kelvin)
                                .ToString();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0, "0 W/m²")]
    [InlineData(123456.789, "123,456.79 W/m²")]
    [InlineData(999_000, "999,000 W/m²")]
    // Above 1e6 UnitsNet switches to scientific notation. Combustion heat fluxes live right across
    // this boundary, so the reports print some cells in full and neighbouring cells in exponent form.
    [InlineData(1_000_000, "1e+06 W/m²")]
    [InlineData(1_234_567, "1.23e+06 W/m²")]
    [InlineData(9_999_000, "1e+07 W/m²")]   // three significant figures lost to rounding
    [InlineData(15_000_000, "1.5e+07 W/m²")]
    public void HeatFlux_FormatsAsWattsPerSquareMeter(double wattsPerSquareMeter, string expected)
    {
        using var _ = InvariantCulture();

        var actual = HeatFlux.FromWattsPerSquareMeter(wattsPerSquareMeter)
                             .ToUnit(HeatFluxUnit.WattPerSquareMeter)
                             .ToString();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1e-6, "1 µm")]
    [InlineData(250.5e-6, "250.5 µm")]
    [InlineData(0.000123456, "123.46 µm")]
    public void Length_FormatsAsMicrometers(double meters, string expected)
    {
        using var _ = InvariantCulture();

        var actual = Length.FromMeters(meters)
                           .ToUnit(LengthUnit.Micrometer)
                           .ToString();

        Assert.Equal(expected, actual);

        // The µ is U+00B5 MICRO SIGN, not U+03BC GREEK SMALL LETTER MU. PDF font coverage and any
        // byte-level comparison of report output depend on which one UnitsNet emits.
        Assert.Contains('µ', actual);
    }

    /// <summary>
    /// Pins the shape of the default format rather than individual values: two decimal places,
    /// trailing zeros trimmed, group separators on, and a switch to scientific notation outside
    /// roughly [1e-3, 1e6). This is the rule that generates every number in the report tables.
    /// </summary>
    [Fact]
    public void DefaultToString_UsesTwoDecimalPlacesAndSwitchesToScientificNotationOutsideOneThousandthToOneMillion()
    {
        using var _ = InvariantCulture();

        Assert.Equal("0.001 µm", Length.FromMicrometers(0.001).ToUnit(LengthUnit.Micrometer).ToString());
        Assert.Equal("1e-04 µm", Length.FromMicrometers(0.0001).ToUnit(LengthUnit.Micrometer).ToString());
        Assert.Equal("999,000 W/m²", HeatFlux.FromWattsPerSquareMeter(999_000).ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
        Assert.Equal("1e+06 W/m²", HeatFlux.FromWattsPerSquareMeter(1_000_000).ToUnit(HeatFluxUnit.WattPerSquareMeter).ToString());
    }

    /// <summary>
    /// Demonstrates — deliberately, as an assertion rather than a comment — that the report output
    /// depends on ambient culture, which production never pins. On a de-DE machine the decimal
    /// separator becomes a comma and every table cell in the PDF changes.
    /// </summary>
    [Fact]
    public void DefaultToString_IsCultureSensitive_WhichProductionDoesNotPin()
    {
        var german = new CultureInfo("de-DE");

        var invariant = Pressure.FromMegapascals(6.5).ToUnit(PressureUnit.Megapascal).ToString(CultureInfo.InvariantCulture);
        var localised = Pressure.FromMegapascals(6.5).ToUnit(PressureUnit.Megapascal).ToString(german);

        Assert.Equal("6.5 MPa", invariant);
        Assert.Equal("6,5 MPa", localised);

        Assert.True(
            invariant != localised,
            "UnitsNet formatting is no longer culture-sensitive. That would be a behaviour change worth "
            + "understanding before upgrading, since the report currently inherits whatever culture the host "
            + "process happens to run under.");
    }

    #endregion

    #region Factory-method round trips — the conversion factors the model depends on

    /// <summary>
    /// Pins the conversion arithmetic behind the factory methods used across the model
    /// (28 × <c>Temperature.FromKelvins</c>, 13 × <c>HeatFlux.FromWattsPerSquareMeter</c>,
    /// 7 × <c>Pressure.FromMegapascals</c>, and so on). These are exact, so they are asserted exactly;
    /// a changed factor here would move every physical result without any visible error.
    /// </summary>
    [Fact]
    public void FactoryMethods_RoundTripThroughTheirBaseUnitsExactly()
    {
        Assert.Equal(7.77, Speed.FromMetersPerSecond(0.00777).MillimetersPerSecond, precision: 12);
        Assert.Equal(0.00777, Speed.FromMillimetersPerSecond(7.77).MetersPerSecond, precision: 12);

        Assert.Equal(6_500_000.0, Pressure.FromMegapascals(6.5).Pascals, precision: 6);
        Assert.Equal(6.5, Pressure.FromPascals(6_500_000).Megapascals, precision: 12);

        Assert.Equal(900.0, Temperature.FromKelvins(900).Kelvins, precision: 12);

        Assert.Equal(1.0, Length.FromMeters(1e-6).Micrometers, precision: 12);
        Assert.Equal(1e-6, Length.FromMicrometers(1).Meters, precision: 15);

        Assert.Equal(1e7, HeatFlux.FromWattsPerSquareMeter(1e7).WattsPerSquareMeter, precision: 6);

        Assert.Equal(1600.0, Density.FromKilogramsPerCubicMeter(1600).KilogramsPerCubicMeter, precision: 12);
        Assert.Equal(1e6, SpecificEnergy.FromJoulesPerKilogram(1e6).JoulesPerKilogram, precision: 6);
    }

    /// <summary>
    /// The standard-atmosphere constant is data, not arithmetic — UnitsNet could revise it.
    /// <c>Pressure.FromAtmospheres</c> has two call sites in the model.
    /// </summary>
    [Fact]
    public void PressureFromAtmospheres_UsesTheStandardAtmosphereOf101325Pascals()
    {
        Assert.Equal(101_325.0, Pressure.FromAtmospheres(1).Pascals, precision: 6);
    }

    /// <summary>
    /// <c>MolarMass.FromKilogramsPerMole</c> has 10 call sites in the thermodynamics path.
    /// </summary>
    [Fact]
    public void MolarMass_ConvertsKilogramsPerMoleToGramsPerMole()
    {
        Assert.Equal(28.9647, MolarMass.FromKilogramsPerMole(0.0289647).GramsPerMole, precision: 10);
    }

    #endregion

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previousCulture;
        private readonly CultureInfo _previousUiCulture;

        public CultureScope(CultureInfo culture)
        {
            _previousCulture = CultureInfo.CurrentCulture;
            _previousUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previousCulture;
            CultureInfo.CurrentUICulture = _previousUiCulture;
        }
    }
}
