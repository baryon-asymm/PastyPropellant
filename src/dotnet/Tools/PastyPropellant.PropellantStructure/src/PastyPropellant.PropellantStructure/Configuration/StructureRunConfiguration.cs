using PastyPropellant.PropellantStructure.Sampling;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Configuration;

/// <summary>
/// Everything one run of the model reads, in SI.
/// </summary>
/// <remarks>
/// Every default is the historical value — the literal the original sets at lines
/// 66-79 or reads from an archived <c>.dat</c> — with one exception,
/// <see cref="BaseParticles"/>, which has no single historical value and is named
/// as a decision in <c>BOOT.md</c>. "No configuration" is therefore this object
/// with nothing overridden rather than a separate branch, which is what keeps it
/// from drifting away from the documented behaviour.
/// <para>
/// The original's own normalisations are deliberately absent: <see cref="Cycles"/>
/// travels into the kernel exactly as given, and the <c>KXX &lt;= 1 -&gt; 1</c> rule
/// of lines 141-143 is applied there, where it is part of the model being
/// reproduced. Applying it here would put "nothing is substituted silently" in
/// direct conflict with "the port reproduces the original".
/// </para>
/// </remarks>
public sealed record StructureRunConfiguration
{
    /// <summary><c>PLOT1</c>: oxidiser crystal density.</summary>
    public required Density OxidiserDensity { get; init; }

    /// <summary><c>PLOT2</c>: density of the binder-and-metal matrix.</summary>
    public required Density BinderMetalDensity { get; init; }

    /// <summary><c>GGG0</c>: oxidiser share of the propellant mass.</summary>
    public required double OxidiserMassFraction { get; init; }

    /// <summary><c>GM</c>: metal share of the propellant mass.</summary>
    public required double MetalMassFraction { get; init; }

    /// <summary><c>GDOK</c> and <c>DDOK</c>: the oxidiser fractions, in order.</summary>
    public required IReadOnlyList<OxidiserFraction> Fractions { get; init; }

    /// <summary>
    /// <c>N</c>: base particles per pass.
    /// </summary>
    /// <remarks>
    /// ⚠ The one default that is a decision rather than an inheritance. The archive
    /// runs anywhere from 5 000 to 1 000 000 base particles, so there is no
    /// historical value to adopt.
    /// </remarks>
    public long BaseParticles { get; init; } = 100_000;

    /// <summary>
    /// <c>KXX</c>: passes requested, exactly as the input file states it.
    /// </summary>
    /// <remarks>
    /// The archived <c>.dat</c> files all carry 0, and the report prints the
    /// normalised 1. The normalisation happens in the kernel, not here; the
    /// resolved record writes the requested value and the resulting pass count side
    /// by side.
    /// </remarks>
    public int Cycles { get; init; } = 1;

    /// <summary>
    /// <c>ivar</c>: 0 makes condition 3 restart the whole base particle, anything
    /// else discards only the neighbour. All 43 runs use 0, so the other branch has
    /// no reference at all.
    /// </summary>
    public int CalculationVariant { get; init; }

    /// <summary><c>AK1..AK4</c>. Never printed by the original's report.</summary>
    public GeometricCriteria Criteria { get; init; } = GeometricCriteria.Historical;

    /// <summary>
    /// <c>Dmin</c>: particles at or below this size take no part in forming pockets
    /// (condition 2, lines 478 and 533).
    /// </summary>
    public Length MinimumParticleSize { get; init; } = Length.FromMicrometers(10);

    /// <summary><c>Di</c>: cell size of the particle histogram.</summary>
    public Length ParticleHistogramStep { get; init; } = Length.FromMicrometers(10);

    /// <summary><c>Dj</c>: step of the pocket-size axis.</summary>
    public Length PocketHistogramStep { get; init; } = Length.FromMicrometers(10);

    /// <summary>The dimensionless coefficients of lines 66-79.</summary>
    public ModelCoefficients Coefficients { get; init; } = ModelCoefficients.Historical;

    /// <summary><c>gsv</c>: which generator drives the draws.</summary>
    public GeneratorSelection Generator { get; init; } = GeneratorSelection.Random2;

    /// <summary>
    /// <c>NNZ</c>: how many steps apart the six <see cref="GeneratorSelection.Toy"/>
    /// states are cut from a single orbit (lines 351-357).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read from the input file by every run and used by none of them, because the
    /// archive is entirely <see cref="GeneratorSelection.Random2"/>, where the value is
    /// dead. Under <see cref="GeneratorSelection.Toy"/> it decides all six seeds and so
    /// decides the run.
    /// </para>
    /// <para>
    /// ⚠ The original's report prints it wrong. Line 1199 writes an INTEGER through an
    /// <c>E9.3</c> edit descriptor, so 6 000 000 comes out as <c>0.841E-38</c> — the
    /// integer's bit pattern read as a denormal REAL*4. The number in a report is
    /// therefore not the number the run used, and the value here comes from the
    /// <c>.dat</c> file instead.
    /// </para>
    /// </remarks>
    public int GeneratorWarmup { get; init; } = 6_000_000;

    /// <summary><c>JZ</c>: the size law inside a fraction.</summary>
    public SizeDistributionLaw Law { get; init; } = SizeDistributionLaw.Surface;

    /// <summary>
    /// <c>eta</c>: share of the metal oxide that ends up agglomerated.
    /// </summary>
    /// <remarks>
    /// The value the original never wrote to its report, so it had to be recovered
    /// by inverting a printed formula. That single omission is the reason this whole
    /// type gets written to disk before a run starts.
    /// </remarks>
    public double AgglomeratedOxideShare { get; init; }

    /// <summary>
    /// Where to write the resolved record, or <see langword="null"/> to keep it in
    /// memory only. Relative paths resolve against the process working directory,
    /// as everything else in this repository does.
    /// </summary>
    public string? ResolvedRunPath { get; init; } = "structure_run.resolved.json";

    /// <summary>
    /// Value equality, including the fraction list element by element, with every
    /// quantity compared in SI.
    /// </summary>
    /// <remarks>
    /// Two departures from what the compiler would generate, both needed for this
    /// type to work as a repeatability key. <see cref="Fractions"/> would otherwise
    /// be compared by reference, so two identical configurations would come out
    /// unequal. And UnitsNet's own equality is unit-sensitive — 10 µm and 1e-5 m are
    /// different values to it — while the kernel only ever sees metres, so the
    /// comparison here is on the SI number. UnitsNet's tolerant overload is
    /// deliberately not used: a key that treats near values as the same key would
    /// merge runs that are not the same run.
    /// </remarks>
    public bool Equals(StructureRunConfiguration? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
            && OxidiserDensity.KilogramsPerCubicMeter.Equals(other.OxidiserDensity.KilogramsPerCubicMeter)
            && BinderMetalDensity.KilogramsPerCubicMeter.Equals(other.BinderMetalDensity.KilogramsPerCubicMeter)
            && OxidiserMassFraction.Equals(other.OxidiserMassFraction)
            && MetalMassFraction.Equals(other.MetalMassFraction)
            && FractionsEqual(Fractions, other.Fractions)
            && BaseParticles == other.BaseParticles
            && Cycles == other.Cycles
            && CalculationVariant == other.CalculationVariant
            && Criteria.Equals(other.Criteria)
            && MinimumParticleSize.Meters.Equals(other.MinimumParticleSize.Meters)
            && ParticleHistogramStep.Meters.Equals(other.ParticleHistogramStep.Meters)
            && PocketHistogramStep.Meters.Equals(other.PocketHistogramStep.Meters)
            && Coefficients.Equals(other.Coefficients)
            && Generator == other.Generator
            && GeneratorWarmup == other.GeneratorWarmup
            && Law == other.Law
            && AgglomeratedOxideShare.Equals(other.AgglomeratedOxideShare)
            && ResolvedRunPath == other.ResolvedRunPath;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(OxidiserDensity.KilogramsPerCubicMeter);
        hash.Add(BinderMetalDensity.KilogramsPerCubicMeter);
        hash.Add(OxidiserMassFraction);
        hash.Add(MetalMassFraction);
        foreach (var fraction in Fractions)
        {
            hash.Add(fraction.MassFraction);
            hash.Add(fraction.MinSize.Meters);
            hash.Add(fraction.MaxSize.Meters);
            hash.Add(fraction.FormsPockets);
        }

        hash.Add(BaseParticles);
        hash.Add(Cycles);
        hash.Add(CalculationVariant);
        hash.Add(Criteria);
        hash.Add(MinimumParticleSize.Meters);
        hash.Add(ParticleHistogramStep.Meters);
        hash.Add(PocketHistogramStep.Meters);
        hash.Add(Coefficients);
        hash.Add(Generator);
        hash.Add(GeneratorWarmup);
        hash.Add(Law);
        hash.Add(AgglomeratedOxideShare);
        hash.Add(ResolvedRunPath);
        return hash.ToHashCode();
    }

    private static bool FractionsEqual(IReadOnlyList<OxidiserFraction> left, IReadOnlyList<OxidiserFraction> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            var a = left[index];
            var b = right[index];
            if (!a.MassFraction.Equals(b.MassFraction)
                || !a.MinSize.Meters.Equals(b.MinSize.Meters)
                || !a.MaxSize.Meters.Equals(b.MaxSize.Meters)
                || a.FormsPockets != b.FormsPockets)
            {
                return false;
            }
        }

        return true;
    }
}
