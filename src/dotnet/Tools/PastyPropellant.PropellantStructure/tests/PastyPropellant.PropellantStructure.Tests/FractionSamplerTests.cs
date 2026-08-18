using PastyPropellant.PropellantStructure.Sampling;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// L0/L1 for <see cref="FractionSampler"/>.
/// </summary>
/// <remarks>
/// The load-bearing test here is <see cref="MassFractionsAreRecoveredFromTheSampledDiameters"/>.
/// PARAM's weights and SIZE's inversion are two halves of one identity — the
/// weight is the reciprocal third moment of the law SIZE inverts — so sampling
/// the pair and re-weighting by D³ must give the mass fractions back. That closes
/// the loop against the distributions themselves rather than against a second
/// transcription of the same formulas, and it catches any mismatch between the
/// two subprograms, which no fixture in the archive can.
/// </remarks>
public sealed class FractionSamplerTests
{
    /// <summary>
    /// Reproduces how the original gets a bound into memory: the .dat carries
    /// micrometres, line 261 scales by a REAL*4 1e-6, and the product lands in a
    /// REAL*4 array.
    /// </summary>
    private static float ToModelMetres(double metres)
    {
        var micrometres = (float)(metres * 1e6);
        return (float)((double)micrometres * 1e-6f);
    }

    private static FractionSampler Build(ReferenceRun run, out float maximumSize, out double alpha)
    {
        var massFractions = run.Fractions.Select(f => (float)f.MassFraction).ToArray();
        var bounds = new float[2 * run.Fractions.Count];
        for (var i = 0; i < run.Fractions.Count; i++)
        {
            bounds[2 * i] = ToModelMetres(run.Fractions[i].MinSizeMetres);
            bounds[2 * i + 1] = ToModelMetres(run.Fractions[i].MaxSizeMetres);
        }

        alpha = run.Alpha;
        return FractionSampler.Build(
            (SizeDistributionLaw)run.Law, massFractions, bounds, ref alpha, out maximumSize);
    }

    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void TheCumulativeRunsFromExactlyZeroToExactlyOne(string id)
    {
        var sampler = Build(ReferenceRuns.ById(id), out _, out _);
        var cumulative = sampler.Cumulative.ToArray();

        Assert.Equal(sampler.FractionCount + 1, cumulative.Length);
        Assert.Equal(0.0, cumulative[0]);
        Assert.Equal(1.0, cumulative[^1]);

        for (var i = 1; i < cumulative.Length; i++)
        {
            Assert.True(cumulative[i] >= cumulative[i - 1], $"cumulative fell at {i}");
        }
    }

    /// <summary>
    /// The normaliser ZS is REAL*4 while the weights it divides are REAL*8, so the
    /// normalised weights do not sum to one in double precision — they miss by a
    /// fraction of a single-precision epsilon. Line 1732 pins the top of the
    /// cumulative to exactly one to cover it, and <c>Zerror</c> on line 1733
    /// measures the gap and throws it away. The pin is load-bearing, not cosmetic:
    /// without it the last interval would end short of the variates that land in it.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void TheNumberFractionsMissOneByAboutASinglePrecisionEpsilon(string id)
    {
        var sampler = Build(ReferenceRuns.ById(id), out _, out _);
        var sum = sampler.Probabilities.ToArray().Sum();

        Assert.InRange(sum, 1.0 - 2.0 * float.Epsilon - 2e-7, 1.0 + 2e-7);
        Assert.Equal(1.0, sampler.Cumulative[^1]);
    }

    [Fact]
    public void TheMissIsRealAndNotAnArtefactOfSummationOrder()
    {
        var sampler = Build(ReferenceRuns.ById("hp1"), out _, out _);
        var sum = sampler.Probabilities.ToArray().Sum();

        Assert.NotEqual(1.0, sum);
        Assert.InRange(Math.Abs(sum - 1.0), 1e-9, 2e-7);
    }

    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void EveryNumberFractionIsPositive(string id)
    {
        var run = ReferenceRuns.ById(id);
        var sampler = Build(run, out _, out _);

        foreach (var probability in sampler.Probabilities.ToArray())
        {
            Assert.True(probability > 0.0, "a fraction with mass got zero probability");
        }
    }

    /// <summary>
    /// Dmax comes back through SIZE at the top of the widest fraction, where both
    /// laws collapse to the upper bound. Checks the fraction PARAM selects and the
    /// x1 = 1 identity of both inversion formulas at once.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void TheReportedMaximumIsTheWidestFractionsUpperBound(string id)
    {
        var run = ReferenceRuns.ById(id);
        Build(run, out var maximumSize, out _);

        var widest = ToModelMetres(run.Fractions.Max(f => f.MaxSizeMetres));

        Assert.True(
            Math.Abs(maximumSize - widest) <= 1e-6 * widest,
            $"{id}: Dmax {maximumSize} against the widest bound {widest}");
    }

    /// <summary>
    /// The archived <c>dok_max</c> is the same quantity at F7.2, while the bounds
    /// in the fixture file survive only E9.3. All the assertion may claim is that
    /// the two agree inside three significant digits — see <see cref="ReferenceRuns"/>.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void TheArchivedMaximumAgreesWithTheBoundsToThePrecisionTheyWerePrintedAt(string id)
    {
        var run = ReferenceRuns.ById(id);
        Build(run, out var maximumSize, out _);

        var computed = maximumSize * 1e6;
        var archived = run.Scalars["dok_max"];

        // E9.3 keeps three significant digits, so the last printed digit of the
        // bound is worth 10^(ceil(log10(value)) - 3) micrometres.
        var quantum = Math.Pow(10, Math.Ceiling(Math.Log10(archived)) - 3);

        Assert.True(
            Math.Abs(computed - archived) <= 0.5 * quantum + 0.005,
            $"{id}: computed {computed:F4} µm vs archived {archived:F2} µm, quantum {quantum} µm");
    }

    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void AlphaIsLeftUntouchedInEveryArchivedRun(string id)
    {
        var run = ReferenceRuns.ById(id);
        Build(run, out _, out var alpha);

        // The mutating branch needs alpha above the widest fraction's number
        // fraction. Every archived run passes zero, so it never fires — which is
        // exactly why the port cannot claim that branch is verified.
        Assert.Equal(0.0, run.Alpha);
        Assert.Equal(0.0, alpha);
    }

    [Theory]
    [InlineData("hp1")]
    [InlineData("res_02")]
    public void MassFractionsAreRecoveredFromTheSampledDiameters(string id)
    {
        var run = ReferenceRuns.ById(id);
        var sampler = Build(run, out _, out _);

        var streams = RandomStreamSet.Historical();
        var cubes = new double[run.Fractions.Count + 1];
        var fractionIndex = 0;

        const int draws = 4_000_000;
        for (var i = 0; i < draws; i++)
        {
            // The call order of the main program at lines 449-450 and 460.
            var x = streams.BaseFraction.Next();
            var x1 = streams.BasePosition.Next();
            var diameter = sampler.Diameter(x, x1, ref fractionIndex);
            cubes[fractionIndex] += (double)diameter * diameter * diameter;
        }

        var total = cubes.Sum();
        for (var j = 1; j <= run.Fractions.Count; j++)
        {
            var expected = run.Fractions[j - 1].MassFraction;
            var actual = cubes[j] / total;
            Assert.True(
                Math.Abs(actual - expected) < 0.02 * expected,
                $"{id} fraction {j}: recovered mass {actual:F5} against {expected:F5}");
        }
    }

    /// <summary>
    /// The sharper half of the same identity, and the one with the lower variance:
    /// inside a fraction the sampled third moment must equal the analytic third
    /// moment of the law <c>SIZE</c> inverts. That moment is exactly what PARAM's
    /// weight is the reciprocal of, so a mismatch between the two subprograms shows
    /// up here per fraction instead of being averaged away.
    /// </summary>
    [Theory]
    [InlineData("hp1")]
    [InlineData("res_02")]
    public void TheSampledThirdMomentOfEachFractionMatchesItsAnalyticValue(string id)
    {
        var run = ReferenceRuns.ById(id);
        var sampler = Build(run, out _, out _);
        var law = (SizeDistributionLaw)run.Law;

        var streams = RandomStreamSet.Historical();
        var counts = new long[run.Fractions.Count + 1];
        var cubes = new double[run.Fractions.Count + 1];
        var fractionIndex = 0;

        const int draws = 4_000_000;
        for (var i = 0; i < draws; i++)
        {
            var x = streams.BaseFraction.Next();
            var x1 = streams.BasePosition.Next();
            var diameter = sampler.Diameter(x, x1, ref fractionIndex);
            counts[fractionIndex]++;
            cubes[fractionIndex] += (double)diameter * diameter * diameter;
        }

        for (var j = 1; j <= run.Fractions.Count; j++)
        {
            // The number share must follow PARAM's weights.
            var share = (double)counts[j] / draws;
            var probability = sampler.Probabilities[j - 1];
            Assert.True(
                Math.Abs(share - probability) < 0.03 * probability + 5.0 / draws,
                $"{id} fraction {j}: number share {share:E4} against {probability:E4}");

            if (counts[j] < 5_000)
            {
                continue;
            }

            double a = ToModelMetres(run.Fractions[j - 1].MinSizeMetres);
            double b = ToModelMetres(run.Fractions[j - 1].MaxSizeMetres);

            // Surface: the density goes as D^-3, so <D³> = 2(b−a)/(1/a²−1/b²).
            // Volume: D is uniform, so <D³> = (b⁴−a⁴)/(4(b−a)).
            var analytic = law == SizeDistributionLaw.Volume
                ? (b * b * b * b - a * a * a * a) / (4 * (b - a))
                : 2 * (b - a) / (1 / (a * a) - 1 / (b * b));

            var sampled = cubes[j] / counts[j];
            Assert.True(
                Math.Abs(sampled - analytic) < 0.03 * analytic,
                $"{id} fraction {j}: sampled <D³> {sampled:E6} against analytic {analytic:E6}");
        }
    }

    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void EveryDiameterLiesInsideTheFractionItWasDrawnFrom(string id)
    {
        var run = ReferenceRuns.ById(id);
        var sampler = Build(run, out _, out _);
        var cumulative = sampler.Cumulative.ToArray();

        for (var j = 1; j <= run.Fractions.Count; j++)
        {
            var lower = ToModelMetres(run.Fractions[j - 1].MinSizeMetres);
            var upper = ToModelMetres(run.Fractions[j - 1].MaxSizeMetres);

            foreach (var t in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
            {
                var x = cumulative[j - 1] + t * (cumulative[j] - cumulative[j - 1]);
                if (x <= cumulative[j - 1] || x > cumulative[j])
                {
                    continue;
                }

                foreach (var x1 in new[] { 0.0, 0.5, 1.0 })
                {
                    var index = 0;
                    var diameter = sampler.Diameter(x, x1, ref index);

                    Assert.Equal(j, index);
                    Assert.InRange(diameter, lower * 0.999999f, upper * 1.000001f);
                }
            }
        }
    }

    [Theory]
    [InlineData(SizeDistributionLaw.Surface)]
    [InlineData(SizeDistributionLaw.Volume)]
    public void TheEndpointsOfAFractionAreItsBounds(SizeDistributionLaw law)
    {
        var alpha = 0.0;
        var sampler = FractionSampler.Build(
            law,
            [0.4f, 0.6f],
            [10e-6f, 50e-6f, 113e-6f, 180e-6f],
            ref alpha,
            out _);

        var cumulative = sampler.Cumulative.ToArray();
        var index = 0;

        void AssertEndpoint(float expected, double x, double x1)
        {
            var diameter = sampler.Diameter(x, x1, ref index);
            Assert.True(
                Math.Abs(diameter - expected) <= 1e-6 * expected,
                $"{law}: got {diameter} for {expected}");
        }

        AssertEndpoint(10e-6f, cumulative[1], 0.0);
        AssertEndpoint(50e-6f, cumulative[1], 1.0);
        Assert.Equal(1, index);

        AssertEndpoint(113e-6f, cumulative[2], 0.0);
        AssertEndpoint(180e-6f, cumulative[2], 1.0);
        Assert.Equal(2, index);
    }

    /// <summary>
    /// SIZE writes its fraction index only when the variate falls strictly inside
    /// an interval, and the scan starts at a value it never initialises. A variate
    /// of exactly zero therefore leaves the caller's index alone. The defect is
    /// reproduced, not repaired.
    /// </summary>
    [Fact]
    public void AZeroVariateLeavesTheFractionIndexWhereTheCallerLeftIt()
    {
        var alpha = 0.0;
        var sampler = FractionSampler.Build(
            SizeDistributionLaw.Surface,
            [0.4f, 0.6f],
            [10e-6f, 50e-6f, 113e-6f, 180e-6f],
            ref alpha,
            out _);

        var index = 2;
        var diameter = sampler.Diameter(0.0, 0.5, ref index);

        Assert.Equal(2, index);
        Assert.InRange(diameter, 113e-6f, 180e-6f);
    }

    [Fact]
    public void AZeroVariateWithNoEarlierCallIsRefusedRatherThanReadOutOfBounds()
    {
        var alpha = 0.0;
        var sampler = FractionSampler.Build(
            SizeDistributionLaw.Surface,
            [1.0f],
            [10e-6f, 50e-6f],
            ref alpha,
            out _);

        var index = 0;
        Assert.Throws<InvalidOperationException>(() => sampler.Diameter(0.0, 0.5, ref index));
    }

    [Fact]
    public void BoundsThatDoNotPairUpWithTheFractionsAreRejected()
    {
        var alpha = 0.0;
        Assert.Throws<ArgumentException>(() =>
            FractionSampler.Build(SizeDistributionLaw.Surface, [0.5f, 0.5f], [10e-6f, 50e-6f], ref alpha, out _));
    }

    /// <summary>
    /// ZS is REAL*4 while the weights it sums are REAL*8, and it leaves this node
    /// unnormalised. Its magnitude is a scale the caller depends on, so it must
    /// not quietly become 1.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceRuns.AllIds), MemberType = typeof(ReferenceRuns))]
    public void TheWeightSumIsReportedUnnormalised(string id)
    {
        var sampler = Build(ReferenceRuns.ById(id), out _, out _);

        Assert.True(sampler.ProbabilitySum > 0f);
        Assert.NotEqual(1f, sampler.ProbabilitySum);
    }
}
