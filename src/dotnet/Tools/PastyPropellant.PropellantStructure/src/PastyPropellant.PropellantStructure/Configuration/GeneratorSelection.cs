namespace PastyPropellant.PropellantStructure.Configuration;

/// <summary>
/// <c>gsv</c> of the original: which pseudo-random source drives the draws.
/// </summary>
/// <remarks>
/// All 43 archived runs use <see cref="Random2"/>. <see cref="Toy"/> is deterministic
/// and is covered by a fixture generated for it
/// (<c>reference/propstruct/probes/probe4.m</c>). <see cref="SystemSeeded"/> is refused
/// by the kernel, because the original does not reproduce such a run either and a
/// result nothing can check is not a result. See <c>BOOT.md</c>, "Параметры и их
/// покрытие эталоном".
/// </remarks>
public enum GeneratorSelection
{
    /// <summary>
    /// <c>gsv = 1</c>: the two-word congruential toy generator (<c>RANDOM1</c>).
    /// </summary>
    Toy = 1,

    /// <summary>
    /// <c>gsv = 2</c>: the ten-word generator (<c>RANDOM2</c>) every archived run
    /// used.
    /// </summary>
    Random2 = 2,

    /// <summary>
    /// <c>gsv = 3</c>: the Compaq runtime's own <c>drand</c>, seeded from outside
    /// the program. Not reproducible even in the original.
    /// </summary>
    SystemSeeded = 3,
}
