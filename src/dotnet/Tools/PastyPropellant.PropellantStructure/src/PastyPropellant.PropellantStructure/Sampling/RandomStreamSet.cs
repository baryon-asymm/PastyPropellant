namespace PastyPropellant.PropellantStructure.Sampling;

/// <summary>
/// The six generator states the model carries (PropStructv3.for, lines 52–63).
/// </summary>
/// <remarks>
/// <para>⚠ These are <b>not</b> six independent streams. They are one orbit of the
/// same generator at consecutive offsets: state 6 is seeded with the identity
/// vector, one step from the identity gives the multiplier vector, which is the
/// seed of state 5, and so on down. The practical consequence is load-bearing —
/// the base particle draws its fraction from state 1 and its position from state
/// 2, so the position variate <b>equals the previous draw of the fraction
/// variate</b>, exactly and forever. The pair walks a lag lattice instead of
/// filling the square.</para>
///
/// <para>State 3 sits on the same orbit but displaced: word 8 of its seed is
/// 3784 where one step from state 4 gives 3748 — two digits transposed. Word 8
/// carries weight 2^-24, so every variate of state 3 is off by exactly 36·2^-24
/// from its orbit position, forever and by a constant. That is a typo in the
/// original and it stays.</para>
///
/// <para><c>X3</c> (pocket size) and <c>X4</c> (both coin flips) share state 6.
/// They must not be modelled as two objects: which of them draws next depends on
/// the run.</para>
/// </remarks>
internal sealed class RandomStreamSet
{
    private RandomStreamSet(
        IRandomStream baseFraction,
        IRandomStream basePosition,
        IRandomStream spacing,
        IRandomStream neighbourFraction,
        IRandomStream neighbourPosition,
        IRandomStream pocketAndCorrections)
    {
        BaseFraction = baseFraction;
        BasePosition = basePosition;
        Spacing = spacing;
        NeighbourFraction = neighbourFraction;
        NeighbourPosition = neighbourPosition;
        PocketAndCorrections = pocketAndCorrections;
    }

    /// <summary>X — base particle's fraction (state 1, line 62).</summary>
    internal IRandomStream BaseFraction { get; }

    /// <summary>X0 — base particle's position in its fraction (state 2, line 60).</summary>
    internal IRandomStream BasePosition { get; }

    /// <summary>X1 — spacing (state 3, line 58; the displaced seed).</summary>
    internal IRandomStream Spacing { get; }

    /// <summary>X2 — surrounding particle's fraction (state 4, line 56).</summary>
    internal IRandomStream NeighbourFraction { get; }

    /// <summary>X21 — surrounding particle's position (state 5, line 54).</summary>
    internal IRandomStream NeighbourPosition { get; }

    /// <summary>X3 and X4 — pocket size and both corrections (state 6, line 52).</summary>
    internal IRandomStream PocketAndCorrections { get; }

    /// <summary>The seeds exactly as the original declares them.</summary>
    internal static RandomStreamSet Historical() => new(
        new Random2Stream(Seeds.State1),
        new Random2Stream(Seeds.State2),
        new Random2Stream(Seeds.State3),
        new Random2Stream(Seeds.State4),
        new Random2Stream(Seeds.State5),
        new Random2Stream(Seeds.State6));

    /// <summary>
    /// <c>gsv = 1</c>: six states of <see cref="Random1Stream"/>, spaced
    /// <paramref name="warmup"/> steps apart along one orbit (lines 344–374).
    /// </summary>
    /// <param name="warmup">
    /// <c>NNZ</c> of the input file — the spacing between consecutive states, 6·10⁶ in
    /// every <c>.dat</c> that carries one.
    /// </param>
    /// <remarks>
    /// <para>
    /// The original records the pair at offsets 0, NNZ, …, 5·NNZ into <c>AX</c>/<c>BX</c>
    /// and then assigns them <b>backwards</b>: <c>A6 = AX(1)</c> down to
    /// <c>A1 = AX(6)</c>. So the stream that draws the base particle's fraction is the
    /// most advanced one and the stream serving the pocket size is the raw seed — the
    /// same descending arrangement as the <c>Random2</c> states, where state 6 is the
    /// identity.
    /// </para>
    /// <para>
    /// ⚠ The states are cut from <b>one</b> orbit, so they are no more independent here
    /// than they are for <c>Random2</c>, and with a lag-2 recurrence they are much less:
    /// the whole set is one sequence read at six offsets. Nothing in the port depends on
    /// their independence, but nothing should be added that does.
    /// </para>
    /// <para>
    /// The walk is 5·NNZ steps — thirty million for the historical value — and is done
    /// once, at construction, exactly as the original does it before the first pass.
    /// </para>
    /// </remarks>
    internal static RandomStreamSet Toy(int warmup)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(warmup);

        var (a, b) = Random1Stream.Origin;
        var states = new (float A, float B)[6];
        states[0] = (a, b);

        var walker = new Random1Stream(a, b);
        for (var offset = 1; offset < states.Length; offset++)
        {
            for (var step = 0; step < warmup; step++)
            {
                walker.Next();
            }

            states[offset] = walker.State;
        }

        // A6 = AX(1) … A1 = AX(6): the least advanced state serves the last consumer.
        return new RandomStreamSet(
            new Random1Stream(states[5].A, states[5].B),
            new Random1Stream(states[4].A, states[4].B),
            new Random1Stream(states[3].A, states[3].B),
            new Random1Stream(states[2].A, states[2].B),
            new Random1Stream(states[1].A, states[1].B),
            new Random1Stream(states[0].A, states[0].B));
    }

    // ⚠ There is deliberately no For(GeneratorSelection, …) here. GeneratorSelection is
    // a Configuration type, and this node depends on nobody - the same reason
    // SizeDistributionLaw is defined in this directory rather than in Configuration. The
    // choice between the two factories is made by the facade, which is the one place
    // allowed to know both.

    /// <summary>
    /// The literal seed vectors. Copied word for word from lines 52–63, including
    /// the degenerate identity of state 6 and the displaced word of state 3.
    /// </summary>
    internal static class Seeds
    {
        /// <summary>u01… (line 62).</summary>
        internal static ReadOnlySpan<int> State1 => [1, 0, 6812, 1081, 7705, 5003, 6576, 7149, 6654, 1302];

        /// <summary>u02… (line 60).</summary>
        internal static ReadOnlySpan<int> State2 => [1, 0, 7088, 2503, 6183, 3683, 6066, 4620, 7519, 1298];

        /// <summary>u03… (line 58). Word 8 is 3784; the orbit gives 3748.</summary>
        internal static ReadOnlySpan<int> State3 => [1, 0, 7364, 3925, 7109, 884, 2888, 4164, 3784, 1899];

        /// <summary>u04… (line 56).</summary>
        internal static ReadOnlySpan<int> State4 => [1, 0, 7640, 5347, 2291, 4799, 945, 6715, 5714, 914];

        /// <summary>u05… (line 54). This is the multiplier vector itself.</summary>
        internal static ReadOnlySpan<int> State5 => [1, 0, 7916, 6769, 8113, 7234, 4142, 5015, 3567, 1526];

        /// <summary>u06… (line 52). The identity: one step from here gives State5.</summary>
        internal static ReadOnlySpan<int> State6 => [1, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    }
}
