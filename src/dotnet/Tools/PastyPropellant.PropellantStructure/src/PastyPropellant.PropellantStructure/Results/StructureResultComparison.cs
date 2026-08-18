using UnitsNet;

namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// Comparisons across several runs that would be wrong if the branch were free to
/// vary between them.
/// </summary>
public static class StructureResultComparison
{
    /// <summary>
    /// Pocket size times pocket mass fraction, one branch for every composition.
    /// </summary>
    /// <param name="results">one result per composition, in the caller's own order</param>
    /// <param name="correction">the branch to use — for both factors, in every result</param>
    /// <returns><c>d·Z_p</c> per composition, in the same order</returns>
    /// <exception cref="BranchMismatchException">
    /// at least one result does not carry the requested branch
    /// </exception>
    /// <remarks>
    /// <para>
    /// This exists because of an established fact about the published numbers. The
    /// triple 80.19 / 73.27 / 52.37 µm comes out only if two compositions are taken
    /// from the uncorrected branch and the third from the second corrected one; read
    /// on one branch throughout, <b>the compositions change order</b>. A comparison
    /// that let the branch vary per composition would reproduce that silently.
    /// </para>
    /// <para>
    /// The result is <see cref="Length"/> rather than a bare number on purpose:
    /// <c>d·Z_p</c> has the dimension of length, the published triple is in
    /// micrometres, and this node's standing rule is that micrometres are never
    /// stored. A bare double here would be the very ambiguity that produced the
    /// misreading in the first place.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<Length> PocketSizeTimesMassFraction(
        IReadOnlyList<StructureResult> results,
        Correction correction)
    {
        ArgumentNullException.ThrowIfNull(results);

        var products = new Length[results.Count];
        for (var index = 0; index < results.Count; index++)
        {
            var result = results[index];

            if (!result.PocketDiameter43.TryGetValue(correction, out var diameter))
            {
                throw new BranchMismatchException(
                    $"Result {index} has no pocket diameter on branch {correction}; it carries "
                    + $"{Describe(result.PocketDiameter43.Keys)}.");
            }

            if (!result.PocketMassFraction.TryGetValue(correction, out var fraction))
            {
                throw new BranchMismatchException(
                    $"Result {index} has no pocket mass fraction on branch {correction}; it carries "
                    + $"{Describe(result.PocketMassFraction.Keys)}.");
            }

            products[index] = diameter.Value * fraction.Value;
        }

        return products;
    }

    private static string Describe(IEnumerable<Correction> branches) =>
        string.Join(", ", branches.Order());
}

/// <summary>
/// Thrown when a comparison is asked for a branch that at least one result does not
/// have.
/// </summary>
/// <remarks>
/// Not an argument error: the request is well formed, and the answer is that the
/// comparison cannot be made on this branch. Returning a shorter list, or falling
/// back to another branch, would be the substitution this node refuses to make.
/// </remarks>
public sealed class BranchMismatchException : Exception
{
    /// <inheritdoc />
    public BranchMismatchException(string message)
        : base(message)
    {
    }
}
