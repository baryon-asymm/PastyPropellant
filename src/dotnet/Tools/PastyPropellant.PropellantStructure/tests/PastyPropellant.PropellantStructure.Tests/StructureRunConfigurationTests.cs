using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Sampling;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// The configuration node: defaults, validation, and the resolved run record.
/// </summary>
/// <remarks>
/// These are not model tests — nothing here computes anything the original also
/// computed. They check the two promises of the node: that "no configuration" and
/// "the historical configuration" are the same object, and that a bad input stops
/// the run before it starts rather than in the middle.
/// </remarks>
public sealed class StructureRunConfigurationTests
{
    /// <summary>A minimal valid configuration; every test that needs one starts here.</summary>
    private static StructureRunConfiguration Minimal() => new()
    {
        OxidiserDensity = Density.FromKilogramsPerCubicMeter(1950),
        BinderMetalDensity = Density.FromKilogramsPerCubicMeter(1800),
        OxidiserMassFraction = 0.583,
        MetalMassFraction = 0.207,
        Fractions =
        [
            new OxidiserFraction(0.18, Length.FromMicrometers(10), Length.FromMicrometers(50)),
            new OxidiserFraction(0.82, Length.FromMicrometers(113), Length.FromMicrometers(180)),
        ],
        ResolvedRunPath = null,
    };

    [Fact]
    public void DefaultsAreTheHistoricalValues()
    {
        var configuration = Minimal();

        Assert.Equal(GeometricCriteria.Historical, configuration.Criteria);
        Assert.Equal(ModelCoefficients.Historical, configuration.Coefficients);
        Assert.Equal(10.0, configuration.MinimumParticleSize.Micrometers, 12);
        Assert.Equal(10.0, configuration.ParticleHistogramStep.Micrometers, 12);
        Assert.Equal(10.0, configuration.PocketHistogramStep.Micrometers, 12);
        Assert.Equal(GeneratorSelection.Random2, configuration.Generator);
        Assert.Equal(SizeDistributionLaw.Surface, configuration.Law);
        Assert.Equal(0, configuration.CalculationVariant);
        Assert.Equal(0.0, configuration.AgglomeratedOxideShare);
        Assert.Equal(1, configuration.Cycles);
    }

    /// <summary>
    /// The one default that is a decision rather than an inheritance, so it is
    /// pinned separately and with the reason attached.
    /// </summary>
    [Fact]
    public void BaseParticleDefaultIsTheChosenValueNotAHistoricalOne()
    {
        Assert.Equal(100_000, Minimal().BaseParticles);

        var archived = ReferenceRuns.All.Select(run => run.BaseParticles).ToArray();
        Assert.True(archived.Distinct().Count() > 1, "the archive would have to disagree for this to be a decision");
        Assert.InRange(100_000L, archived.Min(), archived.Max());
    }

    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void EveryArchivedRunValidates(string id)
    {
        StructureConfigurationValidator.Validate(ReferenceRuns.ById(id).ToConfiguration());
    }

    /// <summary>
    /// No archived run should look unverified: the coverage table in
    /// <c>Configuration/BOOT.md</c> was built from these very runs, so a hit here
    /// means the table and the archive have drifted apart.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void ArchivedRunsCarryNoUnverifiedSettings(string id)
    {
        var record = ResolvedRunRecord.Write(ReferenceRuns.ById(id).ToConfiguration(), path: null);

        Assert.Empty(record.UnverifiedSettings);
    }

    [Fact]
    public void ValidationReportsEveryProblemAtOnce()
    {
        var broken = Minimal() with
        {
            BaseParticles = 0,
            Cycles = -1,
            Criteria = new GeometricCriteria(2.0, 0.5, 0.27, 4.7),
        };

        var exception = Assert.Throws<StructureConfigurationException>(
            () => StructureConfigurationValidator.Validate(broken));

        Assert.Equal(3, exception.Problems.Count);
        Assert.Contains(nameof(broken.BaseParticles), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(broken.Cycles), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(GeometricCriteria.Ak1), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InvertedFractionBoundsAreRejected()
    {
        var broken = Minimal() with
        {
            Fractions = [new OxidiserFraction(1.0, Length.FromMicrometers(50), Length.FromMicrometers(50))],
        };

        var exception = Assert.Throws<StructureConfigurationException>(
            () => StructureConfigurationValidator.Validate(broken));

        Assert.Contains("MinSize", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MinimumParticleSizeAboveTheHistogramCellIsRejected()
    {
        var broken = Minimal() with { MinimumParticleSize = Length.FromMicrometers(20) };

        var exception = Assert.Throws<StructureConfigurationException>(
            () => StructureConfigurationValidator.Validate(broken));

        Assert.Contains(nameof(broken.MinimumParticleSize), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SolidsThatLeaveNoBinderAreRejected()
    {
        var broken = Minimal() with { OxidiserMassFraction = 0.8, MetalMassFraction = 0.25 };

        var exception = Assert.Throws<StructureConfigurationException>(
            () => StructureConfigurationValidator.Validate(broken));

        Assert.Contains("binder", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AFractionSetThatFormsNoPocketsAtAllIsRejected()
    {
        var broken = Minimal() with
        {
            Fractions =
            [
                new OxidiserFraction(1.0, Length.FromMicrometers(10), Length.FromMicrometers(50), FormsPockets: false),
            ],
        };

        Assert.Throws<StructureConfigurationException>(() => StructureConfigurationValidator.Validate(broken));
    }

    /// <summary>
    /// Zero passes is what every archived <c>.dat</c> holds, so rejecting it would
    /// reject the reference. The normalisation to one pass belongs to the kernel;
    /// the configuration keeps the value it was given.
    /// </summary>
    [Fact]
    public void ZeroCyclesIsLegalAndIsNotNormalisedHere()
    {
        var configuration = Minimal() with { Cycles = 0 };

        StructureConfigurationValidator.Validate(configuration);

        Assert.Equal(0, configuration.Cycles);
        var record = ResolvedRunRecord.Write(configuration, path: null);
        Assert.Contains(record.Warnings, warning => warning.Contains("single pass", StringComparison.Ordinal));
    }

    [Fact]
    public void NegativeCyclesAreRejected()
    {
        Assert.Throws<StructureConfigurationException>(
            () => StructureConfigurationValidator.Validate(Minimal() with { Cycles = -1 }));
    }

    /// <summary>
    /// Fourteen archived runs have fraction shares that do not sum to one — two of
    /// them by a tenth. Making that an error would delete them from the reference.
    /// </summary>
    [Fact]
    public void FractionSharesNeedNotSumToOne()
    {
        var configuration = Minimal() with
        {
            Fractions =
            [
                new OxidiserFraction(0.6, Length.FromMicrometers(10), Length.FromMicrometers(50)),
                new OxidiserFraction(0.5, Length.FromMicrometers(113), Length.FromMicrometers(180)),
            ],
        };

        StructureConfigurationValidator.Validate(configuration);

        var record = ResolvedRunRecord.Write(configuration, path: null);
        Assert.Contains(record.Warnings, warning => warning.Contains("sum to", StringComparison.Ordinal));
    }

    [Fact]
    public void ArchivedRunsWithASumAwayFromOneAreWarnedAboutAndNotRejected()
    {
        var run = ReferenceRuns.ById("rc166").ToConfiguration();

        StructureConfigurationValidator.Validate(run);

        Assert.Contains(
            ResolvedRunRecord.Write(run, path: null).Warnings,
            warning => warning.Contains("sum to", StringComparison.Ordinal));
    }

    [Fact]
    public void AGeneratorNoArchivedRunUsedIsValidButUnverified()
    {
        var configuration = Minimal() with { Generator = GeneratorSelection.SystemSeeded };

        StructureConfigurationValidator.Validate(configuration);

        Assert.Contains(
            ResolvedRunRecord.Write(configuration, path: null).UnverifiedSettings,
            entry => entry.Contains(nameof(configuration.Generator), StringComparison.Ordinal));
    }

    [Fact]
    public void AnUndefinedGeneratorValueIsRejected()
    {
        Assert.Throws<StructureConfigurationException>(
            () => StructureConfigurationValidator.Validate(Minimal() with { Generator = (GeneratorSelection)9 }));
    }

    [Fact]
    public void MovingAPocketCriterionMarksTheRunUnverified()
    {
        var configuration = Minimal() with { Criteria = GeometricCriteria.Historical with { Ak3 = 0.3 } };

        var record = ResolvedRunRecord.Write(configuration, path: null);

        Assert.Contains(record.UnverifiedSettings, entry => entry.Contains("Ak3", StringComparison.Ordinal));
    }

    [Fact]
    public void EqualityComparesFractionsByValueNotByListIdentity()
    {
        var left = Minimal();
        var right = Minimal();

        Assert.NotSame(left.Fractions, right.Fractions);
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.NotEqual(left, left with { BaseParticles = 1 });
        Assert.NotEqual(
            left,
            left with
            {
                Fractions =
                [
                    new OxidiserFraction(0.18, Length.FromMicrometers(10), Length.FromMicrometers(50)),
                    new OxidiserFraction(0.82, Length.FromMicrometers(113), Length.FromMicrometers(181)),
                ],
            });
    }

    /// <summary>
    /// ⚠ 10 µm and 1e-5 m are <em>not</em> the same key, and this pins it rather
    /// than papering over it: converting 10 µm to metres lands one ulp below the
    /// literal 1e-5. A configuration built from a <c>.dat</c>, which states metres,
    /// therefore differs from one built from the report, which states micrometres,
    /// even where both describe the same run. The difference is 2e-21 m and cannot
    /// move any result; what it can do is make two records compare unequal, so a
    /// caller comparing configurations across sources must compare the numbers it
    /// cares about, not the whole object.
    /// </summary>
    [Fact]
    public void AUnitConversionMovesTheLastBitAndSoChangesTheKey()
    {
        Assert.NotEqual(Length.FromMicrometers(10).Meters, Length.FromMeters(1e-5).Meters);

        Assert.NotEqual(
            Minimal(),
            Minimal() with { MinimumParticleSize = Length.FromMeters(1e-5) });
    }
}
