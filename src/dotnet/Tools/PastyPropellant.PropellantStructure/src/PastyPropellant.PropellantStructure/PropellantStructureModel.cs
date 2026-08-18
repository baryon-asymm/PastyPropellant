using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Results;
using PastyPropellant.PropellantStructure.Sampling;

namespace PastyPropellant.PropellantStructure;

/// <summary>
/// The whole model, as one call.
/// </summary>
/// <remarks>
/// <para>
/// Everything below this type is <c>internal</c>. A caller supplies a configuration
/// and receives a <see cref="StructureResult"/>; the kernel, the generator states and
/// the geometry are not reachable from outside the assembly, and are not meant to be —
/// a caller who could hold a <c>RunState</c> could read a half-finished Monte Carlo
/// and mistake it for a result of lower precision rather than of unknown precision.
/// </para>
/// <para>
/// ⚠ The generator seeds are not a parameter, and that is deliberate. The original
/// declares six literal seed vectors and never varies them, so a run is a pure
/// function of its configuration — which is what makes an archived report an oracle at
/// all. Offering a seed here would produce runs that no archived report can adjudicate
/// while looking exactly like runs that one can. If independent replicates are ever
/// wanted, they need their own entry point and their own name.
/// </para>
/// </remarks>
public static class PropellantStructureModel
{
    /// <summary>Runs the model to completion.</summary>
    /// <param name="configuration">the recipe and the model's coefficients</param>
    /// <returns>every quantity the original's report printed, under its own names</returns>
    /// <exception cref="StructureConfigurationException">
    /// the configuration is rejected — thrown before any drawing begins, never partway.
    /// </exception>
    public static StructureResult Run(StructureRunConfiguration configuration) =>
        Run(configuration, null);

    /// <summary>Runs the model to completion, reporting progress and honouring cancellation.</summary>
    /// <param name="configuration">the recipe and the model's coefficients</param>
    /// <param name="progress">
    /// receives a <see cref="StructureProgress"/> on each base-particle boundary, or
    /// <see langword="null"/>. ⚠ Watching a run does not change it: no draw depends on
    /// whether a listener is subscribed.
    /// </param>
    /// <param name="cancellationToken">
    /// checked on the same boundary. A cancelled run yields no partial result — half a
    /// Monte-Carlo sample is not a result of lower precision but one of unknown
    /// precision.
    /// </param>
    /// <exception cref="StructureConfigurationException">
    /// the configuration is rejected — thrown before any drawing begins.
    /// </exception>
    /// <exception cref="OperationCanceledException">the run was cancelled.</exception>
    /// <exception cref="NotSupportedException">
    /// <see cref="GeneratorSelection.SystemSeeded"/>: the original seeds it from outside
    /// the program and does not reproduce such a run itself, so no oracle can adjudicate
    /// a port of it.
    /// </exception>
    public static StructureResult Run(
        StructureRunConfiguration configuration,
        IProgress<StructureProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // ⚠ Built from the configuration, not fixed. This used to be an unconditional
        // Historical(), which meant a run asking for gsv = 1 silently got gsv = 2 while
        // the resolved record recorded the request - the one failure mode the record
        // exists to prevent.
        var simulation = new StructureSimulation(configuration, StreamsFor(configuration));
        return simulation.Run(progress, cancellationToken);
    }

    /// <summary>The generator the configuration asks for.</summary>
    /// <remarks>
    /// <para>
    /// The switch lives here rather than on <c>RandomStreamSet</c> because
    /// <see cref="GeneratorSelection"/> is a configuration type and <c>Sampling/</c>
    /// depends on nothing — the same reason <c>SizeDistributionLaw</c> is declared in
    /// <c>Sampling/</c> instead. This type is the composition root and the one place
    /// allowed to know both sides.
    /// </para>
    /// <para>
    /// ⚠ <c>gsv = 3</c> is refused rather than ported, and not for lack of effort: it
    /// calls <c>random_seed</c> with no argument (line 409), so the original itself does
    /// not reproduce such a run twice. A port of it could be checked against nothing —
    /// including against the original. The other two are deterministic and each has a
    /// fixture.
    /// </para>
    /// </remarks>
    private static RandomStreamSet StreamsFor(StructureRunConfiguration configuration) =>
        configuration.Generator switch
        {
            GeneratorSelection.Random2 => RandomStreamSet.Historical(),
            GeneratorSelection.Toy => RandomStreamSet.Toy(configuration.GeneratorWarmup),
            GeneratorSelection.SystemSeeded => throw new NotSupportedException(
                "Generator = SystemSeeded (gsv = 3) seeds itself from outside the program, so the original "
                + "does not reproduce such a run either. There is nothing to check a port of it against, and "
                + "this refuses it rather than return numbers no oracle can adjudicate."),
            _ => throw new NotSupportedException(
                $"Generator = {configuration.Generator} is not one the original defines."),
        };
}
