namespace PastyPropellant.PropellantStructure.Sampling;

/// <summary>
/// Inverts the oxidiser size distribution. Port of two subprograms:
/// <c>PARAM</c> (lines 1695–1757), which turns per-fraction mass fractions into
/// number fractions and their cumulative, and <c>SIZE</c> (lines 1761–1776),
/// which maps a pair of variates onto a particle diameter.
/// </summary>
/// <remarks>
/// <para>
/// Arrays are one-based throughout, mirroring the Fortran; element zero exists
/// and is never used. This is deliberate: it lets every index expression be read
/// straight off the source.
/// </para>
/// <para>
/// Quantities the original declares REAL*4 are held as <see cref="float"/> and
/// REAL*8 as <see cref="double"/>. Every right-hand side is evaluated in double
/// and rounded to float only where the original <em>assigns</em> to a REAL*4
/// variable — the split follows assignment statements, not declarations, because
/// the build set the x87 to 53-bit precision and kept intermediates in registers.
/// </para>
/// </remarks>
internal sealed class FractionSampler
{
    // PARAM writes 3/3.14 and 24/3.14 as constant expressions of type REAL*4, so
    // they fold to single precision at compile time rather than at the 53 bits
    // the runtime uses. The factor is common to every fraction and normalisation
    // cancels it, leaving only ZS — which is REAL*4 and so rounds at every step —
    // to carry the difference, in its eighth digit. The choice is named here
    // rather than left implicit in an expression.
    //
    // ⚠ An earlier version of this comment added "nothing archived can tell the
    // two apart", and that is not true. epsdokfr[0] on hp1, r1 and r2 is
    // |share - ZX| / ZX with share within four parts per million of ZX, so it
    // amplifies ZX by 2.6e5 and the eighth digit lands in the third printed one.
    // Measured on hp1 against an oracle of 3.78e-6: this folding gives 3.7518e-6,
    // folding 3/3.14 in double gives 3.7681e-6, evaluating the whole term in
    // REAL*4 gives 3.7441e-6.
    //
    // So the archive sees the choice but does not settle it — every candidate
    // sits below the oracle, and the printed cell carries only three digits, half
    // of its last being 5e-9 against a spread of 2.4e-8. Reproducing that cell
    // would need ZX to 31 bits, which is more than the REAL*4 recipe data it is
    // built from holds. The L2 tolerance therefore carries an explicit floor for
    // this family rather than pretending one of the candidates was verified; see
    // StructureSimulationTests.CancellationFloor.
    private const float SurfaceWeight = 3f / 3.14f;
    private const float VolumeWeight = 24f / 3.14f;

    private readonly SizeDistributionLaw _law;
    private readonly int _count;
    private readonly float[] _bounds;        // DOK, 1..2*count, metres
    private readonly double[] _probabilities; // Z / ZX, 1..count
    private readonly double[] _cumulative;   // Z1 / Z11, 1..count+1
    private float _probabilitySum;           // ZS / ZSS

    private FractionSampler(SizeDistributionLaw law, int count)
    {
        _law = law;
        _count = count;
        _bounds = new float[2 * count + 1];
        _probabilities = new double[count + 1];
        _cumulative = new double[count + 2];
    }

    /// <summary>Number of oxidiser fractions (NM).</summary>
    internal int FractionCount => _count;

    /// <summary>
    /// Normalised number fractions (ZX). One entry per fraction, in input order.
    /// </summary>
    internal ReadOnlySpan<double> Probabilities => _probabilities.AsSpan(1, _count);

    /// <summary>
    /// Cumulative number fractions (Z11), <c>count + 1</c> entries running from
    /// exactly 0 to exactly 1.
    /// </summary>
    internal ReadOnlySpan<double> Cumulative => _cumulative.AsSpan(1, _count + 1);

    /// <summary>
    /// The un-normalised weight sum (ZSS). Accumulated in single precision by the
    /// original and used outside this node to scale the specific surface, so it is
    /// not an implementation detail of the normalisation.
    /// </summary>
    internal float ProbabilitySum => _probabilitySum;

    /// <summary>
    /// PARAM: builds the inversion tables and, as a side effect the original
    /// relies on, reports the largest diameter the sampler can return.
    /// </summary>
    /// <param name="law">JZ.</param>
    /// <param name="massFractions">G, one per fraction.</param>
    /// <param name="bounds">DOK, two per fraction (lower, upper), metres.</param>
    /// <param name="statisticalSignificance">
    /// alfa. Mutated exactly where the original mutates it — see the remarks.
    /// </param>
    /// <param name="maximumSize">
    /// Dmax. Not the largest bound: the original overwrites its own maximum by
    /// calling SIZE at the top of the widest fraction (line 1754), so this is the
    /// diameter that inversion produces there, float rounding and all.
    /// </param>
    /// <remarks>
    /// The alfa branch is reachable only when <c>alfa</c> exceeds the number
    /// fraction of the widest oxidiser fraction. All 43 archived runs pass zero,
    /// so that branch has no reference behaviour at all and is reproduced on the
    /// strength of the source alone.
    /// </remarks>
    internal static FractionSampler Build(
        SizeDistributionLaw law,
        ReadOnlySpan<float> massFractions,
        ReadOnlySpan<float> bounds,
        ref double statisticalSignificance,
        out float maximumSize)
    {
        var n = massFractions.Length;
        if (n < 1)
        {
            throw new ArgumentException("At least one oxidiser fraction is required.", nameof(massFractions));
        }

        if (bounds.Length != 2 * n)
        {
            throw new ArgumentException(
                $"Expected {2 * n} bounds for {n} fractions, got {bounds.Length}.", nameof(bounds));
        }

        var sampler = new FractionSampler(law, n);

        var g = new float[n + 1];
        for (var j = 1; j <= n; j++)
        {
            g[j] = massFractions[j - 1];
        }

        var dok = sampler._bounds;
        for (var i = 1; i <= 2 * n; i++)
        {
            dok[i] = bounds[i - 1];
        }

        // lines 1700-1719: the weight is the reciprocal third moment of the
        // intra-fraction law, which is what converts mass into number.
        var z = sampler._probabilities;
        {
            var i = 1;
            var j = 1;
            while (true)
            {
                double lo = dok[i];
                double hi = dok[i + 1];

                z[j] = law == SizeDistributionLaw.Volume
                    ? g[j] * (double)VolumeWeight * (hi - lo) / (hi * hi * (hi * hi) - lo * lo * (lo * lo))
                    : g[j] * (double)SurfaceWeight * (lo + hi) / (lo * lo * (hi * hi));

                if (i == 2 * n - 1)
                {
                    break;
                }

                i += 2;
                j++;
            }
        }

        // lines 1721-1728. ZS is REAL*4 although Z is REAL*8, so the sum rounds
        // to float on every step.
        var zs = 0f;
        for (var j = 1; j <= n; j++)
        {
            zs = (float)(zs + z[j]);
        }

        sampler._probabilitySum = zs;

        for (var j = 1; j <= n; j++)
        {
            z[j] /= zs;
        }

        var z1 = sampler._cumulative;
        z1[1] = 0.0;
        for (var j = 1; j <= n; j++)
        {
            z1[j + 1] = z1[j] + z[j];
        }

        // line 1732: forced, so the top of the cumulative is exact whatever the
        // accumulated sum came to. Zerror on line 1733 measures the gap this hides
        // and is then discarded; it is one of the two dead assignments the port
        // does not transcribe — see API.md.
        z1[n + 1] = 1.0;

        // lines 1734-1756
        var selectable = new bool[n + 1];
        Array.Fill(selectable, true);
        var imax = 0;

        while (true)
        {
            var dmax = 0f;
            var selected = false;
            for (var i = 1; i <= n; i++)
            {
                if (selectable[i] && dok[2 * i] > dmax)
                {
                    dmax = dok[2 * i];
                    imax = i;
                    selected = true;
                }
            }

            if (!selected)
            {
                // The original leaves Imax at its previous value here and loops
                // for ever. Unreachable at alfa = 0, which is every archived run.
                throw new InvalidOperationException(
                    "No oxidiser fraction remains selectable while resolving Dmax; " +
                    "the original spins here. Check alfa against the number fractions.");
            }

            var x = z1[imax + 1] - statisticalSignificance;

            if (x - z1[imax] < 0.0)
            {
                selectable[imax] = false;
                statisticalSignificance -= z1[imax + 1] - z1[imax];
                continue;
            }

            var x1 = (x - z1[imax]) / (z1[imax + 1] - z1[imax]);

            // PARAM passes its OWN Nfract here, a different variable from the one
            // the two main-program call sites share, and it is uninitialised on
            // entry. Reachable only if x matched no interval, which x cannot.
            var fractionIndex = 0;
            maximumSize = sampler.Diameter(x, x1, ref fractionIndex);
            return sampler;
        }
    }

    /// <summary>
    /// SIZE: locates the fraction <paramref name="x"/> falls in and inverts the
    /// intra-fraction law at <paramref name="x1"/>.
    /// </summary>
    /// <param name="x">Variate selecting the fraction, in (0, 1).</param>
    /// <param name="x1">Variate selecting the size inside it, in (0, 1).</param>
    /// <param name="fractionIndex">
    /// One-based fraction index. Passed by reference rather than returned because
    /// the original neither initialises it nor writes it when nothing matches: the
    /// stale value belongs to the <em>caller's</em> variable, and the two main-program
    /// call sites deliberately share one while PARAM keeps its own.
    /// </param>
    /// <returns>The diameter in metres, rounded to float as the original's REAL*4 target is.</returns>
    internal float Diameter(double x, double x1, ref int fractionIndex)
    {
        // lines 1764-1766: no early exit, and no assignment at all when x matches
        // nothing. Both reproduced.
        for (var i = 1; i <= _count; i++)
        {
            if (x > _cumulative[i] && x <= _cumulative[i + 1])
            {
                fractionIndex = i;
            }
        }

        var minv = fractionIndex;
        if (minv < 1 || minv > _count)
        {
            throw new InvalidOperationException(
                $"SIZE reached with fraction index {minv}: the variate {x} matched no interval and " +
                "no earlier call left a usable one, so the original would read outside DOK. " +
                "Only x <= 0 or x > 1 can get here.");
        }

        double lo = _bounds[2 * minv - 1];
        double hi = _bounds[2 * minv];

        double d;
        if (_law == SizeDistributionLaw.Volume)
        {
            d = x1 * (hi - lo) + lo;
        }
        else
        {
            var inverseLowerSquared = 1.0 / (lo * lo);
            d = 1.0 / Math.Sqrt(inverseLowerSquared - x1 * (inverseLowerSquared - 1.0 / (hi * hi)));
        }

        return (float)d;
    }
}
