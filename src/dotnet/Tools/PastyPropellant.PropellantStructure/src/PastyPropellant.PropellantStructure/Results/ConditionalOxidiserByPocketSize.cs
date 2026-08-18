using UnitsNet;

namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// The sizes of the particles surrounding a pocket, resolved by the size of the
/// pocket itself.
/// </summary>
/// <param name="Categories">
/// the pocket-size categories the other three members are indexed by — the original's
/// <c>Dkarmcat</c>
/// </param>
/// <param name="MassMeanOxidiser">
/// mass-mean size of the particles around a pocket of that category — <c>dokkarm43</c>
/// </param>
/// <param name="MeanOxidiser">
/// mean size of the same particles — <c>dokkarm10</c>
/// </param>
/// <param name="Rows">
/// one numeric density distribution of particle sizes per category — the original's
/// <c>fqdokkarm(i,:)</c>
/// </param>
/// <remarks>
/// <para>
/// This block is the direct evidence behind the node's claim that large pockets are
/// surrounded by large particles; the first draft of the contract had nowhere to put
/// it, leaving only the single moment <see cref="StructureResult.PocketWallDiameter43"/>
/// standing for it. It does not fit a list of distributions because it is
/// two-dimensional: forty categories, each carrying a distribution of its own.
/// </para>
/// <para>
/// <c>Dkarmcat</c>, <c>dokkarm43</c> and <c>dokkarm10</c> are counted among the
/// original's fourteen "arrays" but are not distributions — their values are lengths
/// and they carry no normalisation. Handing them out as
/// <see cref="Distribution"/> would promise a normalisation that does not exist.
/// </para>
/// </remarks>
public sealed record ConditionalOxidiserByPocketSize(
    IReadOnlyList<Length> Categories,
    IReadOnlyList<Verified<Length>> MassMeanOxidiser,
    IReadOnlyList<Verified<Length>> MeanOxidiser,
    IReadOnlyList<Distribution> Rows)
{
    /// <summary>How many pocket-size categories the block resolves.</summary>
    public int CategoryCount => Categories.Count;

    /// <inheritdoc />
    public bool Equals(ConditionalOxidiserByPocketSize? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && Structural.ListEquals(Categories, other.Categories)
            && Structural.ListEquals(MassMeanOxidiser, other.MassMeanOxidiser)
            && Structural.ListEquals(MeanOxidiser, other.MeanOxidiser)
            && Structural.ListEquals(Rows, other.Rows));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        Structural.AddList(ref hash, Categories);
        Structural.AddList(ref hash, MassMeanOxidiser);
        Structural.AddList(ref hash, MeanOxidiser);
        Structural.AddList(ref hash, Rows);
        return hash.ToHashCode();
    }
}
