using PastyPropellant.PropellantStructure.Sampling;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// L0 — the generator, checked in isolation. Nothing above this level is
/// diagnosable while these are red: a mismatch in the kernel cannot be localised
/// without a generator known to be right.
/// </summary>
public sealed class Random2StreamTests
{
    private const int SequenceLength = 500;

    /// <summary>
    /// Seed 6 is the identity vector, so one step must produce the multiplier —
    /// which is seed 5. This is the fact the whole orbit hangs on.
    /// </summary>
    [Fact]
    public void OneStepFromTheIdentityGivesTheMultiplierVector()
    {
        var stream = new Random2Stream(RandomStreamSet.Seeds.State6);

        stream.Next();

        Assert.Equal(RandomStreamSet.Seeds.State5.ToArray(), stream.State);
    }

    /// <summary>
    /// States 6→5, 5→4 and 2→1 are the same sequence one draw apart, bit for bit.
    /// </summary>
    [Theory]
    [InlineData(6, 5)]
    [InlineData(5, 4)]
    [InlineData(2, 1)]
    public void AdjacentStatesAreTheSameSequenceShiftedByOneDraw(int ahead, int behind)
    {
        var shifted = Draw(SeedOf(ahead), SequenceLength, skip: 1);
        var plain = Draw(SeedOf(behind), SequenceLength);

        Assert.Equal(plain, shifted);
    }

    /// <summary>
    /// State 3 is on the same orbit but its eighth seed word carries 3784 where
    /// the orbit gives 3748. That word weighs 2^-24, so the displacement is a
    /// constant 36·2^-24 on every draw — not a drift, not a bound.
    /// </summary>
    [Theory]
    [InlineData(4, 3, -36.0)]
    [InlineData(3, 2, +36.0)]
    public void StateThreeIsDisplacedByExactlyThirtySixUnitsOfTheEighthWord(
        int ahead, int behind, double expectedUnits)
    {
        var unit = Math.ScaleB(1.0, -24);
        var shifted = Draw(SeedOf(ahead), SequenceLength, skip: 1);
        var plain = Draw(SeedOf(behind), SequenceLength);

        var displacements = shifted.Zip(plain, (a, b) => (a - b) / unit).Distinct().ToArray();

        Assert.Equal([expectedUnits], displacements);
    }

    /// <summary>
    /// Pins the first draw of each state.
    /// </summary>
    /// <remarks>
    /// ⚠ These are <b>not</b> reference values from the model. The archived runs
    /// print <c>epsx</c>, which constrains the mean of a whole run and not any
    /// individual draw, so nothing in the fixtures pins these numbers. They come
    /// from an independent transcription of lines 1642–1691 in Python — a
    /// different language, arbitrary-precision integers, no shift semantics to
    /// get wrong — and their job is to catch a transcription slip here and to
    /// freeze the sequence against later edits. Treat a mismatch as a defect in
    /// this port, not as evidence about the original.
    /// </remarks>
    [Theory]
    [InlineData(1, 0.625198204543724)]
    [InlineData(2, 0.6361388488279976)]
    [InlineData(3, 0.6342394092115904)]
    [InlineData(4, 0.9274695222582708)]
    [InlineData(5, 0.4466296922991418)]
    [InlineData(6, 0.7453298337605209)]
    public void TheFirstDrawOfEachStateIsFrozen(int state, double expected)
    {
        var stream = new Random2Stream(SeedOf(state));

        Assert.Equal(expected, stream.Next());
    }

    /// <summary>
    /// The variate is the state read as a fraction, so it can never leave (0,1) —
    /// and never reaches either end.
    /// </summary>
    /// <remarks>
    /// Zero is out of reach structurally, not statistically: the multiplier's first
    /// word is 1 and every seed's first word is 1, so <c>u0 ≡ 1</c> for ever and the
    /// variate is at least 2^-128. Both bounds are asserted strictly — the earlier
    /// form used <c>1.0 - double.Epsilon</c> as the upper limit, which rounds back
    /// to 1.0 and made the assertion inclusive.
    /// </remarks>
    [Fact]
    public void EveryDrawLiesStrictlyInsideTheUnitInterval()
    {
        for (var state = 1; state <= 6; state++)
        {
            foreach (var draw in Draw(SeedOf(state), SequenceLength))
            {
                Assert.True(draw > 0.0, $"state {state}: a draw reached zero");
                Assert.True(draw < 1.0, $"state {state}: a draw reached one");
            }
        }
    }

    /// <summary>The state has exactly ten words; anything else is a wiring error.</summary>
    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    public void AStateOfTheWrongWidthIsRejected(int words)
    {
        Assert.Throws<ArgumentException>(() => new Random2Stream(new int[words]));
    }

    private static ReadOnlySpan<int> SeedOf(int state) => state switch
    {
        1 => RandomStreamSet.Seeds.State1,
        2 => RandomStreamSet.Seeds.State2,
        3 => RandomStreamSet.Seeds.State3,
        4 => RandomStreamSet.Seeds.State4,
        5 => RandomStreamSet.Seeds.State5,
        6 => RandomStreamSet.Seeds.State6,
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };

    private static double[] Draw(ReadOnlySpan<int> seed, int count, int skip = 0)
    {
        var stream = new Random2Stream(seed);

        for (var i = 0; i < skip; i++)
        {
            stream.Next();
        }

        var draws = new double[count];

        for (var i = 0; i < count; i++)
        {
            draws[i] = stream.Next();
        }

        return draws;
    }
}
