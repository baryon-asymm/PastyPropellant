namespace PastyPropellant.PropellantStructure.Kernel;

/// <summary>
/// What one surrounding particle did to the realisation it belongs to.
/// </summary>
/// <remarks>
/// <para>
/// The three values are the original's three jumps out of the innermost block, and
/// they are a return value here rather than a <c>goto</c> because the jump crosses
/// what in C# are method boundaries. The mapping is exact:
/// </para>
/// <list type="table">
///   <item><term><c>GO TO 501</c></term><description><see cref="Next"/></description></item>
///   <item><term><c>GO TO 550</c></term><description><see cref="Accounted"/></description></item>
///   <item><term><c>GO TO 11</c></term><description><see cref="RestartParticle"/></description></item>
/// </list>
/// <para>
/// ⚠ The difference between <see cref="Next"/> and <see cref="Accounted"/> is not
/// cosmetic: only <see cref="Accounted"/> reaches line 715, where the solid-angle
/// budget is spent. A rejected neighbour costs the realisation nothing.
/// </para>
/// <para>
/// ⚠ <see cref="RestartParticle"/> is raised from four different depths — conditions
/// 1 and 2 on the base particle, condition 3 at <c>ivar = 0</c>, condition 5, and an
/// empty pocket window on line 590, which sits inside the bridge branch. Any step
/// that cannot report it is mis-cut.
/// </para>
/// </remarks>
internal enum NeighbourOutcome
{
    /// <summary>Take the next surrounding particle; the budget is untouched.</summary>
    Next,

    /// <summary>Accounted for; spend the budget and decide whether to continue.</summary>
    Accounted,

    /// <summary>Discard the whole realisation and draw the base particle again.</summary>
    RestartParticle,
}
