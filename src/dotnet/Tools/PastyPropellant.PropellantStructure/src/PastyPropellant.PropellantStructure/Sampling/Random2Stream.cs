namespace PastyPropellant.PropellantStructure.Sampling;

/// <summary>
/// Port of <c>subroutine random2</c> (PropStructv3.for, lines 1642–1691): a
/// ten-word multiplicative generator carried in base 2^13.
/// </summary>
/// <remarks>
/// <para>The state is ten <see cref="int"/> words. Nine of them hold 13 bits, the
/// last holds 11 (line 1689 masks with 11, not 13), so the state spans 9·13+11 =
/// 128 bits and the variate is the state read as a fraction: word <c>k</c> carries
/// weight 2^(-11-13·(9-k)).</para>
///
/// <para>No overflow is possible and none is guarded against: the largest product
/// the convolution can form is below 6.8·10^8, well inside <see cref="int"/>.</para>
///
/// <para>The shifts are logical in Fortran's <c>ishft</c> and arithmetic in C#'s
/// <c>&gt;&gt;</c>. They agree here because every value involved is non-negative,
/// which the bound above guarantees.</para>
/// </remarks>
internal sealed class Random2Stream : IRandomStream
{
    /// <summary>Number of words in the state.</summary>
    internal const int StateWords = 10;

    // data m0..m9, line 1649-1650. The multiplier, one word per power of 2^13.
    private const int M0 = 1;
    private const int M1 = 0;
    private const int M2 = 7916;
    private const int M3 = 6769;
    private const int M4 = 8113;
    private const int M5 = 7234;
    private const int M6 = 4142;
    private const int M7 = 5015;
    private const int M8 = 3567;
    private const int M9 = 1526;

    // data x0..x9, lines 1651-1660. The source writes them as long decimals; each
    // is exactly a power of two, so ScaleB reproduces the stored double bit for
    // bit and says which power it is, which the decimal does not.
    private static readonly double W0 = Math.ScaleB(1.0, -128);
    private static readonly double W1 = Math.ScaleB(1.0, -115);
    private static readonly double W2 = Math.ScaleB(1.0, -102);
    private static readonly double W3 = Math.ScaleB(1.0, -89);
    private static readonly double W4 = Math.ScaleB(1.0, -76);
    private static readonly double W5 = Math.ScaleB(1.0, -63);
    private static readonly double W6 = Math.ScaleB(1.0, -50);
    private static readonly double W7 = Math.ScaleB(1.0, -37);
    private static readonly double W8 = Math.ScaleB(1.0, -24);
    private static readonly double W9 = Math.ScaleB(1.0, -11);

    private int _u0;
    private int _u1;
    private int _u2;
    private int _u3;
    private int _u4;
    private int _u5;
    private int _u6;
    private int _u7;
    private int _u8;
    private int _u9;

    /// <param name="seed">
    /// The ten state words, in the order the original passes them
    /// (<c>u0</c>…<c>u9</c>).
    /// </param>
    internal Random2Stream(ReadOnlySpan<int> seed)
    {
        if (seed.Length != StateWords)
        {
            throw new ArgumentException(
                $"The random2 state has exactly {StateWords} words; got {seed.Length}.",
                nameof(seed));
        }

        _u0 = seed[0];
        _u1 = seed[1];
        _u2 = seed[2];
        _u3 = seed[3];
        _u4 = seed[4];
        _u5 = seed[5];
        _u6 = seed[6];
        _u7 = seed[7];
        _u8 = seed[8];
        _u9 = seed[9];
    }

    /// <summary>A copy of the current state, for the lockstep invariant tests.</summary>
    internal int[] State => [_u0, _u1, _u2, _u3, _u4, _u5, _u6, _u7, _u8, _u9];

    /// <inheritdoc />
    public double Next()
    {
        // Lines 1661-1670: the convolution. M1 is zero, so five of the products
        // below vanish identically — they are kept because dropping them would
        // make the correspondence with the source a thing to re-derive.
        var c0 = M0 * _u0;
        var c1 = M0 * _u1 + M1 * _u0;
        var c2 = M0 * _u2 + M1 * _u1 + M2 * _u0;
        var c3 = M0 * _u3 + M1 * _u2 + M2 * _u1 + M3 * _u0;
        var c4 = M0 * _u4 + M1 * _u3 + M2 * _u2 + M3 * _u1 + M4 * _u0;
        var c5 = M0 * _u5 + M1 * _u4 + M2 * _u3 + M3 * _u2 + M4 * _u1 + M5 * _u0;
        var c6 = M0 * _u6 + M1 * _u5 + M2 * _u4 + M3 * _u3 + M4 * _u2 + M5 * _u1 + M6 * _u0;
        var c7 = M0 * _u7 + M1 * _u6 + M2 * _u5 + M3 * _u4 + M4 * _u3 + M5 * _u2 + M6 * _u1 + M7 * _u0;
        var c8 = M0 * _u8 + M1 * _u7 + M2 * _u6 + M3 * _u5 + M4 * _u4 + M5 * _u3 + M6 * _u2 + M7 * _u1 + M8 * _u0;
        var c9 = M0 * _u9 + M1 * _u8 + M2 * _u7 + M3 * _u6 + M4 * _u5 + M5 * _u4 + M6 * _u3 + M7 * _u2 + M8 * _u1 + M9 * _u0;

        // Lines 1671-1689: carry propagation. Nine words keep 13 bits; the last
        // keeps 11 — that asymmetry is what fixes the period, not a typo.
        _u0 = c0 - ((c0 >> 13) << 13);
        var n = c1 + (c0 >> 13);
        _u1 = n - ((n >> 13) << 13);
        n = c2 + (n >> 13);
        _u2 = n - ((n >> 13) << 13);
        n = c3 + (n >> 13);
        _u3 = n - ((n >> 13) << 13);
        n = c4 + (n >> 13);
        _u4 = n - ((n >> 13) << 13);
        n = c5 + (n >> 13);
        _u5 = n - ((n >> 13) << 13);
        n = c6 + (n >> 13);
        _u6 = n - ((n >> 13) << 13);
        n = c7 + (n >> 13);
        _u7 = n - ((n >> 13) << 13);
        n = c8 + (n >> 13);
        _u8 = n - ((n >> 13) << 13);
        n = c9 + (n >> 13);
        _u9 = n - ((n >> 11) << 11);

        // Line 1690. Every product is exact (a small integer times a power of
        // two), so only the summation order can move the result — keep it.
        return _u0 * W0 + _u1 * W1 + _u2 * W2 + _u3 * W3 + _u4 * W4
             + _u5 * W5 + _u6 * W6 + _u7 * W7 + _u8 * W8 + _u9 * W9;
    }
}
