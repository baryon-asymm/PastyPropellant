using PastyPropellant.PropellantStructure.Configuration;

namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// What the reference can say about a value.
/// </summary>
/// <remarks>
/// Three states rather than a flag. Until 2026-08-15 the original was thought to be
/// unrunnable, so "not confirmed" and "not confirmable" were the same thing. It runs
/// under wine now, and a branch no archived run covers can be given a real fixture,
/// so the two have to be told apart — a bool would start lying as fixtures land.
/// </remarks>
public enum Verification
{
    /// <summary>
    /// The original printed this quantity, and this run's settings all sit where the
    /// archive pins them.
    /// </summary>
    /// <remarks>
    /// Read this as "the archive is able to adjudicate a run like this one", not as
    /// "this number has been compared against a run". The comparison is the job of
    /// L2; the flag states whether there is anything to compare against.
    /// </remarks>
    ConfirmedByArchivedRun,

    /// <summary>
    /// The original never printed this quantity, so no run of it can confirm the
    /// value — attempt counts, clamp counts, the NaN counter.
    /// </summary>
    NotPrintedByOriginal,

    /// <summary>
    /// The original prints this quantity, but the run used a setting no archived run
    /// covers, so the archive holds no run to compare with. A fixture can be
    /// generated under wine; see <c>reference/propstruct/README.md</c>.
    /// </summary>
    NotCoveredByAnyRun,
}

/// <summary>A value together with what the reference can say about it.</summary>
/// <param name="Value">the value itself, at full precision</param>
/// <param name="Status">what the reference can say about it</param>
public readonly record struct Verified<T>(T Value, Verification Status)
{
    private static readonly IEqualityComparer<T> ValueComparer = QuantityComparer.For<T>();

    /// <summary>
    /// Value equality, with quantities compared in SI.
    /// </summary>
    /// <remarks>
    /// The generated version would compare a <c>Length</c> by value <b>and</b> unit,
    /// so a value read back from disk — always in metres — would not equal the same
    /// value held in micrometres. See <see cref="QuantityComparer"/>.
    /// </remarks>
    public bool Equals(Verified<T> other) =>
        Status == other.Status && ValueComparer.Equals(Value, other.Value);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Value is null ? 0 : ValueComparer.GetHashCode(Value), Status);

    /// <summary>A printed quantity from a run the archive can adjudicate.</summary>
    public static Verified<T> Reference(T value) => new(value, Verification.ConfirmedByArchivedRun);

    /// <summary>A quantity the original never printed.</summary>
    public static Verified<T> Unprinted(T value) => new(value, Verification.NotPrintedByOriginal);

    /// <summary>A printed quantity from a run no archived run covers.</summary>
    public static Verified<T> Uncovered(T value) => new(value, Verification.NotCoveredByAnyRun);
}

/// <summary>
/// Decides the verification status of a printed quantity for one run.
/// </summary>
/// <remarks>
/// The rule is deliberately the only one that is actually determinate when the
/// result is built: a run whose resolved record lists no unverified settings is a run
/// the archive can adjudicate; a run that moved any setting off the value the archive
/// pins is not. Anything finer — "an archived run exists with this exact recipe" —
/// would be a claim this type cannot check, and the honest place for it is the L2
/// test, which does the comparison.
/// </remarks>
internal static class VerificationPolicy
{
    /// <summary>The status every printed quantity of this run carries.</summary>
    internal static Verification ForPrintedQuantity(ResolvedRunRecordSnapshot run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return run.UnverifiedSettings.Count == 0
            ? Verification.ConfirmedByArchivedRun
            : Verification.NotCoveredByAnyRun;
    }
}
