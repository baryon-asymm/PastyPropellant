namespace PastyPropellant.PropellantStructure.Sampling;

/// <summary>
/// Draws a pocket diameter from the running pocket-size histogram. Covers the
/// table build the main program does inline (lines 587–610) and the interpolation
/// <c>DM</c> performs on it (lines 1573–1586).
/// </summary>
/// <remarks>
/// <para>
/// Arrays are one-based, as in <see cref="FractionSampler"/>, and REAL*4 values
/// are held as <see cref="float"/> with every right-hand side evaluated in double.
/// </para>
/// <para>
/// The build is here rather than in the caller because the window it reduces is a
/// distribution: the caller supplies the histogram and which bins to keep, and
/// gets back a tabulated inverse. What the caller keeps is the arithmetic that
/// picks the window, which is geometry, not sampling.
/// </para>
/// </remarks>
internal sealed class PocketSizeSampler
{
    private readonly float[] _cumulative;  // FQKS, 1..capacity+1
    private readonly float[] _diameters;   // DPOC, 1..capacity+1
    private int _nodeCount;                // NNN1
    private long _clampCount;

    /// <param name="capacity">Nkarm — the number of histogram bins.</param>
    internal PocketSizeSampler(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "At least one bin is required.");
        }

        Capacity = capacity;
        _cumulative = new float[capacity + 2];
        _diameters = new float[capacity + 2];
    }

    /// <summary>Nkarm.</summary>
    internal int Capacity { get; }

    /// <summary>NNN1 — the number of table nodes the last rebuild produced.</summary>
    internal int NodeCount => _nodeCount;

    /// <summary>
    /// How often <see cref="Draw"/> has run off the end of the table. See the
    /// remarks on <see cref="Draw"/>; the count belongs in the run's diagnostics
    /// so the frequency stays visible rather than being smoothed away.
    /// </summary>
    internal long ClampCount => _clampCount;

    /// <summary>
    /// Rebuilds the tabulated inverse over bins <paramref name="firstBin"/>…<paramref name="lastBin"/>
    /// of <paramref name="histogram"/>.
    /// </summary>
    /// <param name="histogram">QKS1, one-based; element zero is ignored.</param>
    /// <param name="firstBin">mindk.</param>
    /// <param name="lastBin">maxdk.</param>
    /// <param name="binWidth">Di, metres.</param>
    /// <returns>
    /// <see langword="false"/> when the window carries no mass at all (AUS = 0),
    /// which is the original's <c>GO TO 11</c> — the caller abandons the base particle.
    /// The table is left unusable in that case.
    /// </returns>
    internal bool Rebuild(ReadOnlySpan<float> histogram, int firstBin, int lastBin, float binWidth)
    {
        if (firstBin < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(firstBin), firstBin, "Bins are one-based.");
        }

        if (lastBin < firstBin)
        {
            throw new ArgumentOutOfRangeException(nameof(lastBin), lastBin, "The window must contain at least one bin.");
        }

        if (lastBin >= histogram.Length || lastBin + 1 > Capacity + 1)
        {
            throw new ArgumentOutOfRangeException(nameof(lastBin), lastBin, "The window runs past the histogram.");
        }

        // lines 587-591
        var aus = 0f;
        for (var i = firstBin; i <= lastBin; i++)
        {
            aus = (float)(aus + histogram[i]);
        }

        if (aus == 0f)
        {
            _nodeCount = 0;
            return false;
        }

        aus = (float)(1.0 / aus);

        // lines 592-599. The whole buffer is cleared every time, as in the
        // original: the shift below reads slots the fill never touches.
        Array.Clear(_cumulative);
        Array.Clear(_diameters);

        _diameters[firstBin] = (float)((double)binWidth * firstBin);
        for (var i = firstBin; i <= lastBin; i++)
        {
            _cumulative[i + 1] = (float)((double)aus * histogram[i] + _cumulative[i]);
            _diameters[i + 1] = (float)(binWidth + (double)_diameters[i]);
        }

        // lines 600-607: drop the tail once the cumulative is within 1e-5 of one,
        // and pin that node to exactly one.
        var last = lastBin;
        for (var i = firstBin; i <= lastBin + 1; i++)
        {
            if (1.0 - _cumulative[i] < (double)1e-5f)
            {
                last = i - 1;
                _cumulative[i] = 1f;
                break;
            }
        }

        _nodeCount = last - firstBin + 2;

        // lines 608-610: shift both tables down so the window starts at index 1.
        for (var i = 1; i <= _nodeCount; i++)
        {
            _diameters[i] = _diameters[firstBin - 1 + i];
            _cumulative[i] = _cumulative[firstBin - 1 + i];
        }

        return true;
    }

    /// <summary>
    /// DM: linear interpolation of the tabulated inverse at <paramref name="x"/>.
    /// </summary>
    /// <remarks>
    /// The original's search loop has no upper bound: if x fell above the top of
    /// the cumulative it would walk off the end of FQKS and interpolate whatever
    /// lies in the next words of memory. The port clamps to the top node and counts
    /// the event instead, because a total function is the only way to be defined
    /// here at all.
    /// <para>
    /// ⚠ The clamp is a guard, not a reproduction: <see cref="ClampCount"/> is
    /// expected to stay <b>zero</b>. An earlier reading of this code held that the
    /// single-precision cumulative settles at 0.99999994 and overruns roughly 0.4
    /// times per run; that was checked in August 2026 and is wrong on both counts.
    /// The truncation in <see cref="Rebuild"/> pins the first node within 1e-5 of
    /// one to exactly one, 0.99999994 is six orders inside that threshold, and a
    /// 1e-5 shortfall is structurally unreachable — an increment too small to move
    /// the cumulative is, to within one binade, the increment too small to move the
    /// normalising sum. See Sampling/API.md. A non-zero count is a signal that
    /// something else is wrong, not a rate to be measured.
    /// </para>
    /// </remarks>
    internal float Draw(double x)
    {
        if (_nodeCount < 2)
        {
            throw new InvalidOperationException(
                "No pocket-size table has been built; call Rebuild and honour its return value first.");
        }

        for (var i = 1; i < _nodeCount; i++)
        {
            if (x >= _cumulative[i] && x < _cumulative[i + 1])
            {
                return (float)(_diameters[i]
                    + (x - _cumulative[i])
                      * ((double)_diameters[i + 1] - _diameters[i])
                      / ((double)_cumulative[i + 1] - _cumulative[i]));
            }
        }

        _clampCount++;
        return _diameters[_nodeCount];
    }

    /// <summary>The table's cumulative column, for tests and diagnostics.</summary>
    internal ReadOnlySpan<float> Cumulative => _cumulative.AsSpan(1, _nodeCount);

    /// <summary>The table's diameter column, for tests and diagnostics.</summary>
    internal ReadOnlySpan<float> Diameters => _diameters.AsSpan(1, _nodeCount);
}
