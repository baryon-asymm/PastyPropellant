using System.Text.Json.Serialization;
using PastyPropellant.PropellantStructure.Configuration;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// Everything one run of the model produced.
/// </summary>
/// <remarks>
/// <para>
/// The type does two jobs, and they pull apart. It is the target of the L2
/// comparison, so it has to carry <b>every</b> number the original printed; and it is
/// the contract a consumer reads, so it has to be small and make a correction branch
/// impossible to substitute. The first draft tried to be both with one set of fields
/// and came out incomplete — no whole-number counters, no <c>da_coef</c>, no
/// conditional block — and imprecise: the pocket mass fraction lost its branch.
/// </para>
/// <para>
/// So there is one storage — <see cref="Printed"/> and <see cref="PrintedLengths"/>,
/// keyed by the original's own names — and typed projections over it. The
/// projections hold nothing of their own: they read the record rather than copying
/// it, which is why they cannot drift from it. Reading is not computing, so the
/// node's ban on calculation stands.
/// </para>
/// </remarks>
public sealed record StructureResult
{
    /// <summary>Names of the printed scalars whose value is a length.</summary>
    /// <remarks>
    /// The original stored metres and printed micrometres, and that gap has already
    /// caused one misreading. Splitting the record in two is how a key's unit stops
    /// being a matter of memory; this set is the split, in machine-readable form, so
    /// the kernel and the tests do not each re-derive it by hand.
    /// </remarks>
    public static IReadOnlySet<string> LengthValuedQuantities { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "dok43_analytic", "dok43_base", "dok43_surrounding", "dok43_sd", "dok_max",
        "dkarm43_v1", "dkarm43_v2", "dkarm43_sd", "dkarm43_cor1", "dkarm43_sd_cor1",
        "dkarm43_cor2", "dkarm43_sd_cor2", "dkarm10", "dkarm10_cor",
        "dagg43_v1", "dagg43_v2", "dagg43_cor1", "dagg43_cor2",
        "dok43_all_v1", "dok43_all_v2",
    };

    /// <summary>The configuration this run actually used, with its warnings.</summary>
    /// <remarks>
    /// A result carries its own input because the original's did not: its report
    /// omitted <c>eta</c> and <c>AK1..AK4</c>, so replaying a run needed the
    /// <c>.dat</c> as well — and 20 of the 43 archived <c>.dat</c> files had been
    /// edited by the time anyone looked.
    /// </remarks>
    public required ResolvedRunRecordSnapshot Run { get; init; }

    /// <summary>Every dimensionless printed scalar, under the original's own name.</summary>
    public required IReadOnlyDictionary<string, Verified<double>> Printed { get; init; }

    /// <summary>Every length-valued printed scalar, under the original's own name.</summary>
    public required IReadOnlyDictionary<string, Verified<Length>> PrintedLengths { get; init; }

    /// <summary>The ten distribution arrays, each with its grid and normalisation.</summary>
    public required IReadOnlyList<Distribution> Distributions { get; init; }

    /// <summary>Surrounding-particle sizes resolved by pocket size.</summary>
    public required ConditionalOxidiserByPocketSize PocketWallDistributions { get; init; }

    /// <summary>How the run went, as opposed to what it computed.</summary>
    public required RunDiagnostics Diagnostics { get; init; }

    /// <summary>
    /// Mass fraction of pockets in the binder-metal composition, by branch —
    /// <c>zkarm</c>, <c>zkarm_cor1</c>, <c>zkarm_cor2</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ A dictionary, not a number. This is the quantity the node was built to
    /// protect and the one the first draft handed out unbranched. It differs from
    /// <c>zkarm_cor2</c> in all 43 archived runs, by up to 66%, and from
    /// <c>zkarm_cor1</c> in five of them, by up to 75%. The published triple
    /// <c>d·Z_p</c> is a product of two branched quantities, so a branch can be
    /// substituted in either factor.
    /// </remarks>
    [JsonIgnore]
    public IReadOnlyDictionary<Correction, Verified<double>> PocketMassFraction =>
        Project(Printed, "PocketMassFraction", ("zkarm", "zkarm_cor1", "zkarm_cor2"));

    /// <summary>Mass-mean pocket diameter, by branch.</summary>
    [JsonIgnore]
    public IReadOnlyDictionary<Correction, Verified<Length>> PocketDiameter43 =>
        Project(PrintedLengths, "PocketDiameter43", ("dkarm43_v2", "dkarm43_cor1", "dkarm43_cor2"));

    /// <summary>Standard deviation of the mass-mean pocket diameter, by branch.</summary>
    [JsonIgnore]
    public IReadOnlyDictionary<Correction, Verified<Length>> PocketDiameter43Sd =>
        Project(PrintedLengths, "PocketDiameter43Sd", ("dkarm43_sd", "dkarm43_sd_cor1", "dkarm43_sd_cor2"));

    /// <summary>Mass-mean agglomerate diameter, by branch.</summary>
    /// <remarks>
    /// Not an independent quantity: it is the pocket diameter times
    /// <see cref="PocketToAgglomerateCoefficient"/> (lines 1376-1379).
    /// </remarks>
    [JsonIgnore]
    public IReadOnlyDictionary<Correction, Verified<Length>> AgglomerateDiameter43 =>
        Project(PrintedLengths, "AgglomerateDiameter43", ("dagg43_v2", "dagg43_cor1", "dagg43_cor2"));

    /// <summary>Mean pocket diameter, by branch — this one has only two.</summary>
    [JsonIgnore]
    public IReadOnlyDictionary<Correction, Verified<Length>> PocketDiameter10 =>
        Project(PrintedLengths, "PocketDiameter10", ("dkarm10", "dkarm10_cor"));

    /// <summary>
    /// Pocket-to-agglomerate size coefficient — the original's <c>mp</c>, printed as
    /// <c>da_coef</c>.
    /// </summary>
    /// <remarks>
    /// Depends on the agglomerated-oxide share and the recipe (lines 1014-1016).
    /// Published alongside the agglomerate diameter because that diameter is this
    /// factor times the pocket diameter: handing out the product while hiding the
    /// factor would pass off a derived number as an independent one.
    /// </remarks>
    [JsonIgnore]
    public Verified<double> PocketToAgglomerateCoefficient => Require(Printed, "da_coef");

    /// <summary>
    /// Mass-mean size of the particles forming pocket walls — <c>Dok43(2)</c>,
    /// surrounding particles only.
    /// </summary>
    [JsonIgnore]
    public Verified<Length> PocketWallDiameter43 => Require(PrintedLengths, "dok43_surrounding");

    /// <summary>Mean bridge-to-particle size ratio between oxidiser particles — <c>Dqmkm1</c>.</summary>
    /// <remarks>
    /// ⚠ Named for what the report says it is: "Medium MKM/Dok size between Dok
    /// particles". The first draft of this contract called it the small-oxidiser mass
    /// share in the pocket, which is a different quantity entirely — the mistranslation
    /// was caught by reading the print statement rather than the field name.
    /// </remarks>
    [JsonIgnore]
    public Verified<double> MeanBridgeToParticleRatio => Require(Printed, "dqmkm1");

    /// <summary>Mean bridge-to-pocket size ratio — <c>Dqmkm2</c>.</summary>
    [JsonIgnore]
    public Verified<double> MeanBridgeToPocketRatio => Require(Printed, "dqmkm2");

    /// <summary>
    /// Share of the oxidiser treated as homogenised into the binder rather than as
    /// discrete particles — <c>fineoxy_fr</c>.
    /// </summary>
    [JsonIgnore]
    public Verified<double> HomogenisedOxidiserFraction => Require(Printed, "fine_oxidiser_fraction");

    /// <inheritdoc />
    public bool Equals(StructureResult? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && Run == other.Run
            && Structural.MapEquals(Printed, other.Printed)
            && Structural.MapEquals(PrintedLengths, other.PrintedLengths)
            && Structural.ListEquals(Distributions, other.Distributions)
            && PocketWallDistributions == other.PocketWallDistributions
            && Diagnostics == other.Diagnostics);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Run);
        Structural.AddMap(ref hash, Printed);
        Structural.AddMap(ref hash, PrintedLengths);
        Structural.AddList(ref hash, Distributions);
        hash.Add(PocketWallDistributions);
        hash.Add(Diagnostics);
        return hash.ToHashCode();
    }

    private static IReadOnlyDictionary<Correction, Verified<T>> Project<T>(
        IReadOnlyDictionary<string, Verified<T>> source,
        string quantity,
        (string None, string Variant1, string Variant2) names) =>
        new BranchedValues<T>(
            quantity,
            new Dictionary<Correction, Verified<T>>
            {
                [Correction.None] = Require(source, names.None, quantity),
                [Correction.Variant1] = Require(source, names.Variant1, quantity),
                [Correction.Variant2] = Require(source, names.Variant2, quantity),
            });

    private static IReadOnlyDictionary<Correction, Verified<T>> Project<T>(
        IReadOnlyDictionary<string, Verified<T>> source,
        string quantity,
        (string None, string Variant1) names) =>
        new BranchedValues<T>(
            quantity,
            new Dictionary<Correction, Verified<T>>
            {
                [Correction.None] = Require(source, names.None, quantity),
                [Correction.Variant1] = Require(source, names.Variant1, quantity),
            });

    /// <summary>
    /// Reads one printed quantity, or says which one is missing.
    /// </summary>
    /// <remarks>
    /// A branch set is fixed per quantity, so a key that is not there means the port
    /// failed to produce it — not that this quantity happens to have fewer branches.
    /// Silently returning a shorter dictionary would turn a broken result into a
    /// plausible one.
    /// </remarks>
    private static Verified<T> Require<T>(
        IReadOnlyDictionary<string, Verified<T>> source,
        string name,
        string? quantity = null)
    {
        if (source.TryGetValue(name, out var value))
        {
            return value;
        }

        var subject = quantity is null ? $"'{name}'" : $"'{name}', needed by {quantity},";
        throw new InvalidOperationException(
            $"{subject} is not in the result. Every quantity the original prints must be present; "
            + "a missing one means the run did not produce it, not that it does not exist.");
    }
}
