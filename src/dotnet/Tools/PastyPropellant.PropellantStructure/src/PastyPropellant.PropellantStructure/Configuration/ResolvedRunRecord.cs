using System.Text.Json;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Configuration;

/// <summary>
/// Writes <c>structure_run.resolved.json</c> — the record of what a run actually
/// used.
/// </summary>
/// <remarks>
/// This is not a convenience. The original's report omitted <c>eta</c>, which had to
/// be recovered by inverting a printed formula, and omitted <c>AK1..AK4</c>, the
/// constants that decide what counts as a pocket at all. A run's output was
/// therefore not a record of its own input: replaying it needed the <c>.dat</c> as
/// well, and 20 of the 43 archived <c>.dat</c> files had been edited by the time
/// anyone looked. The record below closes that hole, which is why it is written
/// before the first draw rather than alongside the result.
/// </remarks>
public static class ResolvedRunRecord
{
    /// <summary>The lowest base-particle count any archived run used.</summary>
    private const long SmallestReferenceSample = 5_000;

    /// <summary>The highest base-particle count any archived run used.</summary>
    private const long LargestReferenceSample = 1_000_000;

    /// <summary>
    /// Builds the record and, when <paramref name="path"/> is given, writes it.
    /// </summary>
    /// <param name="configuration">the configuration about to be run</param>
    /// <param name="path">
    /// where to write, or <see langword="null"/> to build the record without a file
    /// </param>
    /// <returns>the same content that reached the file</returns>
    public static ResolvedRunRecordSnapshot Write(StructureRunConfiguration configuration, string? path)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var snapshot = new ResolvedRunRecordSnapshot(
            configuration,
            CollectWarnings(configuration),
            CollectUnverifiedSettings(configuration));

        if (path is not null)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(snapshot, ConfigurationJson.Options));
        }

        return snapshot;
    }

    /// <summary>Reads back a record this class wrote.</summary>
    /// <exception cref="StructureConfigurationException">
    /// the file is missing or does not hold a record of this shape
    /// </exception>
    public static ResolvedRunRecordSnapshot Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            var snapshot = JsonSerializer.Deserialize<ResolvedRunRecordSnapshot>(
                File.ReadAllText(path),
                ConfigurationJson.Options);
            return snapshot
                ?? throw new StructureConfigurationException($"{path}: the resolved run record is JSON null.");
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new StructureConfigurationException($"{path}: {exception.Message}");
        }
    }

    private static IReadOnlyList<string> CollectWarnings(StructureRunConfiguration configuration)
    {
        var warnings = new List<string>();

        var sum = configuration.Fractions.Sum(fraction => fraction.MassFraction);
        if (Math.Abs(sum - 1.0) > 1e-6)
        {
            warnings.Add(
                $"The oxidiser fraction mass shares sum to {sum:R}, not 1. The model normalises them internally, "
                + "and four archived runs come to 1.1 and 0.9885, so this is recorded rather than rejected.");
        }

        if (configuration.Cycles <= 1)
        {
            warnings.Add(
                $"Cycles = {configuration.Cycles} is normalised to a single pass by the kernel (the original's "
                + "lines 141-143). The requested value is kept above; the run makes 1 pass.");
        }

        var idle = configuration.Fractions
            .Select((fraction, index) => (fraction, index))
            .Where(entry => !entry.fraction.FormsPockets)
            .Select(entry => entry.index)
            .ToArray();
        if (idle.Length > 0)
        {
            warnings.Add(
                $"Fractions {string.Join(", ", idle)} have FormsPockets = false: their mass moves into the "
                + "homogenised oxidiser and takes no part in forming pockets.");
        }

        return warnings;
    }

    private static IReadOnlyList<string> CollectUnverifiedSettings(StructureRunConfiguration configuration)
    {
        var unverified = new List<string>();

        var criteria = configuration.Criteria;
        var historicalCriteria = GeometricCriteria.Historical;
        Compare(criteria.Ak1, historicalCriteria.Ak1, "Criteria.Ak1", unverified);
        Compare(criteria.Ak2, historicalCriteria.Ak2, "Criteria.Ak2", unverified);
        Compare(criteria.Ak3, historicalCriteria.Ak3, "Criteria.Ak3", unverified);
        Compare(criteria.Ak4, historicalCriteria.Ak4, "Criteria.Ak4", unverified);

        var coefficients = configuration.Coefficients;
        var historicalCoefficients = ModelCoefficients.Historical;
        Compare(
            coefficients.SurroundingVolumeShare,
            historicalCoefficients.SurroundingVolumeShare,
            "Coefficients.SurroundingVolumeShare",
            unverified);
        Compare(coefficients.PocketInPocket, historicalCoefficients.PocketInPocket, "Coefficients.PocketInPocket", unverified);
        Compare(coefficients.PocketInBridge, historicalCoefficients.PocketInBridge, "Coefficients.PocketInBridge", unverified);
        Compare(coefficients.Accuracy, historicalCoefficients.Accuracy, "Coefficients.Accuracy", unverified);
        Compare(
            coefficients.StatisticalSignificance,
            historicalCoefficients.StatisticalSignificance,
            "Coefficients.StatisticalSignificance",
            unverified);
        Compare(
            coefficients.HomogenisedOxidiser,
            historicalCoefficients.HomogenisedOxidiser,
            "Coefficients.HomogenisedOxidiser",
            unverified);
        Compare(
            coefficients.PocketBridgeRatioMin,
            historicalCoefficients.PocketBridgeRatioMin,
            "Coefficients.PocketBridgeRatioMin",
            unverified);
        Compare(
            coefficients.PocketBridgeRatioMax,
            historicalCoefficients.PocketBridgeRatioMax,
            "Coefficients.PocketBridgeRatioMax",
            unverified);

        var tenMicrometres = Length.FromMicrometers(10);
        Compare(configuration.MinimumParticleSize, tenMicrometres, "MinimumParticleSize", unverified);
        Compare(configuration.ParticleHistogramStep, tenMicrometres, "ParticleHistogramStep", unverified);
        Compare(configuration.PocketHistogramStep, tenMicrometres, "PocketHistogramStep", unverified);

        // eta is the one setting with more than one covered value. The comparison is
        // loose because the archive does not hold eta at all: it is recovered by
        // inverting a printed formula, so the one run that used 0.5 comes back as
        // 0.500001. probe9 adds 0.25 as a value that was set deliberately and is
        // recorded with its answer sequence, which is why an interior value no longer
        // reports as unverified.
        // The 1e-5 is that recovery's slop, not a tolerance on the setting: p777out1
        // comes back as 0.500001 and is the run that defines the upper reference.
        var eta = configuration.AgglomeratedOxideShare;
        if (eta < 0.0 || eta > 0.5 + 1e-5)
        {
            unverified.Add(
                $"AgglomeratedOxideShare = {configuration.AgglomeratedOxideShare:R}: the references are 0 "
                + "(42 archived runs), 0.25 (probe9) and 0.5 (one archived run), so anything outside "
                + "[0, 0.5] extrapolates past all three.");
        }

        if (configuration.CalculationVariant != 0)
        {
            unverified.Add(
                $"CalculationVariant = {configuration.CalculationVariant}: every archived run uses 0, so the "
                + "branch where condition 3 discards only the neighbour has never been executed.");
        }

        if (configuration.Cycles > 1)
        {
            unverified.Add(
                $"Cycles = {configuration.Cycles}: no archived run makes more than one pass. The accumulators are "
                + "zeroed once, before all passes, so a second pass continues the first rather than starting over. "
                + "The branch is covered by one generated fixture (reference/propstruct/probes/probe3.m, KXX = 2, "
                + "20 particles) and by no measured run, so the arithmetic is checked but the sample size is not.");
        }

        if (configuration.BaseParticles < SmallestReferenceSample
            || configuration.BaseParticles > LargestReferenceSample)
        {
            unverified.Add(
                $"BaseParticles = {configuration.BaseParticles}: the archive runs from {SmallestReferenceSample} "
                + $"to {LargestReferenceSample}.");
        }

        // ⚠ Both non-Random2 generators are recorded, and for opposite reasons. Toy is
        // implemented and covered by one small fixture; SystemSeeded is refused outright
        // by the facade. Recording only one of them would leave a configuration that can
        // never run looking like an ordinary one right up to the throw.
        if (configuration.Generator == GeneratorSelection.SystemSeeded)
        {
            unverified.Add(
                $"Generator = {configuration.Generator}: seeded from outside the program, so the original does "
                + "not reproduce such a run either. The facade refuses it - this configuration cannot be run at "
                + "all, and the record says so before anything tries.");
        }

        if (configuration.Generator == GeneratorSelection.Toy)
        {
            unverified.Add(
                $"Generator = {configuration.Generator}: all 43 archived runs use {GeneratorSelection.Random2}. "
                + "This branch is covered by one generated fixture (reference/propstruct/probes/probe4.m, "
                + $"20 particles, GeneratorWarmup = {configuration.GeneratorWarmup}) and by no measured run. "
                + "It is also a lag-2 recurrence over six offsets of one orbit — reproduced faithfully, but not "
                + "a generator to draw conclusions from.");
        }

        return unverified;
    }

    /// <summary>
    /// Is this value still the historical one, as far as the original could tell?
    /// </summary>
    /// <remarks>
    /// The comparison is to single precision because the original held these
    /// coefficients in <c>REAL*4</c> — its own report prints <c>eps</c> as
    /// 0.050000001, which is what 0.05 becomes there. An exact comparison would
    /// therefore flag a reference run as departing from its own value. Where the
    /// historical value is zero the tolerance collapses to nothing, which is right:
    /// any non-zero <c>alfa</c> opens a branch no run has taken.
    /// </remarks>
    private static void Compare(double value, double historical, string name, List<string> unverified)
    {
        if (!(Math.Abs(value - historical) <= 1e-6 * Math.Abs(historical)))
        {
            unverified.Add(
                $"{name} = {value:R}: every archived run uses {historical:R}, so the reference pins the value "
                + "and says nothing about either side of it.");
        }
    }

    private static void Compare(Length value, Length historical, string name, List<string> unverified)
    {
        if (!value.Meters.Equals(historical.Meters))
        {
            unverified.Add(
                $"{name} = {value.Micrometers:R} µm: all 43 runs set this and its two siblings to "
                + $"{historical.Micrometers:R} µm, which is also why their roles are indistinguishable in the "
                + "reference.");
        }
    }
}

/// <summary>
/// The content of <c>structure_run.resolved.json</c>, also carried inside the run's
/// result so a result knows its own input without a file on disk.
/// </summary>
/// <param name="Configuration">the configuration as given, with nothing normalised</param>
/// <param name="Warnings">
/// things the run does that are legal but worth stating — a fraction sum away from
/// one, a pass count the kernel will normalise, a fraction excluded from pocket
/// formation
/// </param>
/// <param name="UnverifiedSettings">
/// settings whose value no archived run covers. Not errors: a statement that a
/// number produced under them is unchecked
/// </param>
public sealed record ResolvedRunRecordSnapshot(
    StructureRunConfiguration Configuration,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> UnverifiedSettings)
{
    /// <summary>
    /// Names the shape of this file, so a reader that finds one on disk years later
    /// can tell what wrote it.
    /// </summary>
    public string Schema { get; init; } = "propstruct-resolved-run/1";

    /// <summary>Value equality, with both lists compared element by element.</summary>
    /// <remarks>
    /// As for the configuration itself: the generated version would compare the
    /// lists by reference, so a record read back from disk would never equal the one
    /// that was written, and a round-trip check would be vacuous.
    /// </remarks>
    public bool Equals(ResolvedRunRecordSnapshot? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && Schema == other.Schema
            && Configuration == other.Configuration
            && Warnings.SequenceEqual(other.Warnings, StringComparer.Ordinal)
            && UnverifiedSettings.SequenceEqual(other.UnverifiedSettings, StringComparer.Ordinal));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Schema);
        hash.Add(Configuration);
        foreach (var warning in Warnings)
        {
            hash.Add(warning);
        }

        foreach (var setting in UnverifiedSettings)
        {
            hash.Add(setting);
        }

        return hash.ToHashCode();
    }
}
