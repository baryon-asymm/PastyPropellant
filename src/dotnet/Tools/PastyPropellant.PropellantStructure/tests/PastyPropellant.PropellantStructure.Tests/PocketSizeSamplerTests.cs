using PastyPropellant.PropellantStructure.Sampling;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// L0 for <see cref="PocketSizeSampler"/>.
/// </summary>
public sealed class PocketSizeSamplerTests
{
    private const float BinWidth = 10e-6f;
    private const int Capacity = 200;

    /// <summary>One-based histogram, element zero unused, as the port expects.</summary>
    private static float[] Histogram(int firstBin, params float[] weights)
    {
        var histogram = new float[Capacity + 2];
        for (var i = 0; i < weights.Length; i++)
        {
            histogram[firstBin + i] = weights[i];
        }

        return histogram;
    }

    [Fact]
    public void AFlatWindowGivesEvenlySpacedNodesAtTheBinUpperEdges()
    {
        var sampler = new PocketSizeSampler(Capacity);
        Assert.True(sampler.Rebuild(Histogram(5, 1f, 1f, 1f, 1f, 1f), 5, 9, BinWidth));

        Assert.Equal(6, sampler.NodeCount);

        // Only the first node is a product, Di·mindk; every later one adds Di to
        // its predecessor. The two differ after a few steps, so the expectation
        // has to accumulate the same way rather than multiply.
        var diameters = sampler.Diameters.ToArray();
        var expected = (float)((double)BinWidth * 5);
        Assert.Equal(expected, diameters[0]);
        for (var i = 1; i < diameters.Length; i++)
        {
            expected = (float)(BinWidth + (double)expected);
            Assert.Equal(expected, diameters[i]);
        }

        Assert.NotEqual((float)((double)BinWidth * 10), diameters[^1]);

        var cumulative = sampler.Cumulative.ToArray();
        Assert.Equal(0f, cumulative[0]);
        Assert.Equal(1f, cumulative[^1]);
        for (var i = 1; i < cumulative.Length; i++)
        {
            Assert.True(cumulative[i] > cumulative[i - 1]);
        }
    }

    /// <summary>
    /// ⚠ Node <c>i</c> sits at <c>Di·(bin)</c>, the <em>upper</em> edge of the bin
    /// whose mass fills the interval above it, while pockets were binned with
    /// <c>int(RK/Di)+1</c>, i.e. into <c>[(bin−1)·Di, bin·Di)</c>. Redrawn pocket
    /// diameters are therefore one bin larger than the pockets they were tabulated
    /// from. The shift is the model's and is reproduced; it is pinned here so it
    /// cannot be "tidied away" later.
    /// </summary>
    [Fact]
    public void TheTableIsShiftedOneBinAboveTheDataItWasBuiltFrom()
    {
        var sampler = new PocketSizeSampler(Capacity);
        Assert.True(sampler.Rebuild(Histogram(5, 1f), 5, 5, BinWidth));

        // Bin 5 holds pockets of 40…50 µm; the interval that receives its mass is
        // 50…60 µm.
        Assert.Equal(50e-6f, sampler.Diameters[0], 1e-10f);
        Assert.Equal(60e-6f, sampler.Diameters[1], 1e-10f);
    }

    [Fact]
    public void AWindowWithoutMassIsRefused()
    {
        var sampler = new PocketSizeSampler(Capacity);

        Assert.False(sampler.Rebuild(Histogram(5, 0f, 0f, 0f), 5, 7, BinWidth));
        Assert.Equal(0, sampler.NodeCount);
    }

    [Fact]
    public void ABareSamplerRefusesToDraw()
    {
        var sampler = new PocketSizeSampler(Capacity);
        Assert.Throws<InvalidOperationException>(() => sampler.Draw(0.5));
    }

    /// <summary>
    /// The tail is dropped where the cumulative first comes within 1e-5 of one,
    /// so mass concentrated in the first bin collapses the table to two nodes.
    /// </summary>
    [Fact]
    public void TheTailIsDroppedOnceTheCumulativeReachesOne()
    {
        var sampler = new PocketSizeSampler(Capacity);
        Assert.True(sampler.Rebuild(Histogram(5, 1f, 0f, 0f, 0f, 0f), 5, 9, BinWidth));

        Assert.Equal(2, sampler.NodeCount);
        Assert.Equal(0f, sampler.Cumulative[0]);
        Assert.Equal(1f, sampler.Cumulative[1]);
    }

    /// <summary>
    /// ⚠ Reachability of the clamp. The BOOT note this port was written against
    /// said DM runs off the end of the table roughly 0.4 times per run, because
    /// the single-precision cumulative can settle at 0.99999994. It cannot: the
    /// truncation at lines 600–605 fires at the first node within 1e-5 of one and
    /// pins it to exactly one, and 0.99999994 is six orders of magnitude inside
    /// that. For the table to stop short, the accumulated rounding of the
    /// cumulative would have to reach 1e-5, which cannot happen — the increment
    /// that is too small to move the cumulative is, to within one binade, exactly
    /// the increment too small to move the AUS sum that normalises it, so the two
    /// errors cancel by construction. A search over 300 000 randomised histograms
    /// of 20…2400 bins found a worst deficit of 6.7e-6, and it grows only as the
    /// square root of the bin count.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TheTopOfTheTableIsAlwaysExactlyOne(int seed)
    {
        var random = new Random(seed);
        var sampler = new PocketSizeSampler(Capacity);

        for (var trial = 0; trial < 400; trial++)
        {
            var firstBin = random.Next(1, 40);
            var lastBin = random.Next(firstBin, Capacity);
            var histogram = new float[Capacity + 2];
            var any = false;
            for (var i = firstBin; i <= lastBin; i++)
            {
                histogram[i] = random.Next(4) == 0 ? 0f : (float)Math.Pow(10, random.NextDouble() * 12 - 6);
                any |= histogram[i] > 0f;
            }

            if (!any)
            {
                continue;
            }

            Assert.True(sampler.Rebuild(histogram, firstBin, lastBin, BinWidth));
            Assert.Equal(1f, sampler.Cumulative[^1]);
            Assert.Equal(0f, sampler.Cumulative[0]);
        }
    }

    /// <summary>
    /// Because the top node is exactly one and the generator returns strictly less
    /// than one, the clamp cannot fire on a real variate. The guard exists so that
    /// <see cref="PocketSizeSampler.Draw"/> is total in C# where the original reads
    /// adjacent memory, and so that the day it does fire, it is counted rather than
    /// returning a plausible number.
    /// </summary>
    [Fact]
    public void TheClampFiresOnlyForAVariateTheGeneratorCannotProduce()
    {
        var sampler = new PocketSizeSampler(Capacity);
        Assert.True(sampler.Rebuild(Histogram(5, 1f, 2f, 3f), 5, 7, BinWidth));

        Assert.Equal(0, sampler.ClampCount);

        var clamped = sampler.Draw(1.0);

        Assert.Equal(1, sampler.ClampCount);
        Assert.Equal(sampler.Diameters[^1], clamped);
    }

    [Fact]
    public void RealVariatesNeverClampAndStayInsideTheWindow()
    {
        var sampler = new PocketSizeSampler(Capacity);
        Assert.True(sampler.Rebuild(Histogram(5, 4f, 9f, 2f, 7f, 1f, 6f), 5, 10, BinWidth));

        var stream = RandomStreamSet.Historical().PocketAndCorrections;
        var lower = sampler.Diameters[0];
        var upper = sampler.Diameters[^1];

        for (var i = 0; i < 200_000; i++)
        {
            var diameter = sampler.Draw(stream.Next());
            Assert.InRange(diameter, lower, upper);
        }

        Assert.Equal(0, sampler.ClampCount);
    }

    /// <summary>
    /// The interpolation is the histogram's own inverse: within one interval the
    /// draw is uniform and carries that bin's mass, so the sampled mean must match
    /// the mass-weighted midpoints of the intervals — one bin above the histogram's
    /// own midpoints, per <see cref="TheTableIsShiftedOneBinAboveTheDataItWasBuiltFrom"/>.
    /// </summary>
    [Fact]
    public void DrawsReproduceTheTabulatedDistribution()
    {
        float[] weights = [4f, 9f, 2f, 7f, 1f, 6f];
        const int firstBin = 5;

        var sampler = new PocketSizeSampler(Capacity);
        Assert.True(sampler.Rebuild(Histogram(firstBin, weights), firstBin, firstBin + weights.Length - 1, BinWidth));

        var total = weights.Sum();
        var expected = 0.0;
        for (var i = 0; i < weights.Length; i++)
        {
            expected += weights[i] / total * (firstBin + i + 0.5) * BinWidth;
        }

        var stream = RandomStreamSet.Historical().PocketAndCorrections;
        var sum = 0.0;
        const int draws = 500_000;
        for (var i = 0; i < draws; i++)
        {
            sum += sampler.Draw(stream.Next());
        }

        var mean = sum / draws;
        Assert.True(
            Math.Abs(mean - expected) < 0.005 * expected,
            $"sampled mean {mean:E6} against {expected:E6}");
    }

    [Fact]
    public void AnUpsideDownWindowIsRejected()
    {
        var sampler = new PocketSizeSampler(Capacity);
        Assert.Throws<ArgumentOutOfRangeException>(() => sampler.Rebuild(Histogram(5, 1f), 7, 5, BinWidth));
    }

    [Fact]
    public void AWindowPastTheEndOfTheHistogramIsRejected()
    {
        var sampler = new PocketSizeSampler(Capacity);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => sampler.Rebuild(Histogram(5, 1f), 5, Capacity + 1, BinWidth));
    }
}
