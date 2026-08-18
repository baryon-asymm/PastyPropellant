namespace PastyPropellant.PropellantStructure.Sampling;

/// <summary>
/// One state of the model's random-number generator.
/// </summary>
/// <remarks>
/// Deliberately minimal: a stream advances and returns, nothing more. The draw
/// counters (<c>NFX</c>, <c>NFY</c>, <c>NFZ</c>, <c>NFQ</c>, <c>NFW</c>) and the
/// running sums behind <c>epsx</c> are incremented at the call site in the
/// original, and the sixth state is served by two different counters — see
/// Sampling/API.md. Putting either on the stream would make one of them
/// unreproducible.
/// </remarks>
internal interface IRandomStream
{
    /// <summary>Advances the state by one step and returns the variate in (0,1).</summary>
    double Next();
}
