namespace PastyPropellant.PropellantStructure;

/// <summary>
/// How far a run has got. Reported on the base-particle boundary.
/// </summary>
/// <param name="Pass">
/// Which pass is running, <c>0..KXX</c>. Zero is the preparatory pass — the one that
/// builds the fine-oxidiser histogram the working passes consult. The original prints
/// it as <c>Cycle = 0</c>.
/// </param>
/// <param name="TotalPasses">
/// <c>1 + KXX</c> after the original's normalisation of <c>KXX &lt;= 1</c> to one, so
/// two for every archived run.
/// </param>
/// <param name="AcceptedParticles">Base particles carried to completion in this pass.</param>
/// <param name="TargetParticles">N — how many this pass will complete.</param>
/// <remarks>
/// ⚠ Progress does not measure work. A base particle is redrawn until a realisation
/// survives, and the number of attempts varies by about a thousandfold between
/// recipes, so equal counts here are not equal times.
/// </remarks>
public readonly record struct StructureProgress(
    int Pass,
    int TotalPasses,
    long AcceptedParticles,
    long TargetParticles);
