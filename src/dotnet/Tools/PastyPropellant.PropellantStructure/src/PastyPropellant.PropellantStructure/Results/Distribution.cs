using System.Text.Json.Serialization;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// What a distribution's cells are indexed by.
/// </summary>
/// <remarks>
/// <para>
/// A closed hierarchy: the constructor is private, so no fourth kind can be added
/// from outside and every read is an exhaustive switch. The first draft of this
/// contract declared a single <c>Length GridStep</c>, which fits six of the ten
/// distributions and silently misdescribes the rest — <c>fqmkm1</c>, <c>fqmkm2</c>
/// and <c>coef</c> are indexed by a dimensionless ratio.
/// </para>
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ByLength), "byLength")]
[JsonDerivedType(typeof(ByRatio), "byRatio")]
[JsonDerivedType(typeof(ByCategory), "byCategory")]
public abstract record DistributionGrid
{
    private DistributionGrid()
    {
    }

    /// <summary>Cells of equal width along a size axis.</summary>
    /// <param name="Origin">the lower edge of cell 0</param>
    /// <param name="Step">the cell width — the original's <c>Di</c></param>
    public sealed record ByLength(Length Origin, Length Step) : DistributionGrid
    {
        /// <inheritdoc />
        public bool Equals(ByLength? other) =>
            ReferenceEquals(this, other)
            || (other is not null
                && Origin.Meters.Equals(other.Origin.Meters)
                && Step.Meters.Equals(other.Step.Meters));

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Origin.Meters, Step.Meters);
    }

    /// <summary>Cells of equal width along a dimensionless ratio.</summary>
    /// <param name="Origin">the lower edge of cell 0</param>
    /// <param name="Step">the cell width</param>
    /// <remarks>
    /// ⚠ For <c>fqmkm1</c> the step printed by the report (0.01) is <b>not</b> the
    /// step the array was built with (0.001) — they differ by a factor of ten. Fill
    /// this from the model's own constant, never from the report; the reference
    /// keeps both under <c>array_steps.fqmkm1</c> and
    /// <c>array_steps.fqmkm1_as_printed</c> for exactly that reason.
    /// </remarks>
    public sealed record ByRatio(double Origin, double Step) : DistributionGrid;

    /// <summary>Cells that are named, not measured — one per oxidiser fraction.</summary>
    /// <param name="Labels">one label per cell, in cell order</param>
    public sealed record ByCategory(IReadOnlyList<string> Labels) : DistributionGrid
    {
        /// <inheritdoc />
        public bool Equals(ByCategory? other) =>
            ReferenceEquals(this, other)
            || (other is not null && Structural.ListEquals(Labels, other.Labels));

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            Structural.AddList(ref hash, Labels);
            return hash.ToHashCode();
        }
    }
}

/// <summary>How a distribution's cells add up.</summary>
public enum DistributionNormalisation
{
    /// <summary>A density: the sum times the grid step is 1.</summary>
    DensityPerGridUnit,

    /// <summary>A probability per cell: the cells sum to 1.</summary>
    ProbabilityPerBin,

    /// <summary>
    /// Not normalised at all — each cell is a quantity in its own right.
    /// </summary>
    /// <remarks>
    /// <c>pdoksmall</c> is the one such array: each cell is the probability of
    /// pocket-in-pocket for its own base-particle size, and the cells have no reason
    /// to sum to anything.
    /// </remarks>
    Unnormalised,
}

/// <summary>One of the original's distribution arrays, with its grid and normalisation.</summary>
/// <param name="Name">the original's own name for the array, e.g. <c>fmkarm_cor</c></param>
/// <param name="Grid">what the cells are indexed by</param>
/// <param name="Normalisation">how the cells add up</param>
/// <param name="Values">the cells, in cell order, at full precision</param>
/// <remarks>
/// Ten of the original's fourteen arrays are distributions. <c>Dkarmcat</c>,
/// <c>dokkarm43</c> and <c>dokkarm10</c> are curves of length against pocket
/// category and live in <see cref="ConditionalOxidiserByPocketSize"/>;
/// <c>epsdokfr</c> is a draw-accuracy diagnostic and lives in
/// <see cref="RunDiagnostics"/>.
/// </remarks>
public sealed record Distribution(
    string Name,
    DistributionGrid Grid,
    DistributionNormalisation Normalisation,
    IReadOnlyList<double> Values)
{
    /// <inheritdoc />
    public bool Equals(Distribution? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && Name == other.Name
            && Grid == other.Grid
            && Normalisation == other.Normalisation
            && Structural.ListEquals(Values, other.Values));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        hash.Add(Grid);
        hash.Add(Normalisation);
        Structural.AddList(ref hash, Values);
        return hash.ToHashCode();
    }
}
