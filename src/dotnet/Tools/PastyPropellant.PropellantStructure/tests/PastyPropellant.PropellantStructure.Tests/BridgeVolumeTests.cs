using PastyPropellant.PropellantStructure.Geometry;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// L1 for <see cref="BridgeVolume"/> — the one place in the model that can be
/// checked against independent truth rather than against an archived run.
/// </summary>
/// <remarks>
/// The truth is built twice over, in this file, from geometry alone: once as a
/// numerical quadrature of the body and once as its closed form. Neither reads the
/// port. What they establish is that <c>VM</c> is exact in π — and therefore that
/// everything separating it from the truth is the literal <c>3.14</c>.
/// </remarks>
public sealed class BridgeVolumeTests
{
    /// <summary>
    /// The triangle on the two particle centres and the pocket centre, in double
    /// and with a real π. Nothing here is transcribed from the subroutine: the
    /// sides come from the definition of the configuration and the angles from the
    /// law of cosines.
    /// </summary>
    private sealed record Triangle(double R1, double R2, double Rk, double Gap)
    {
        internal double Ac => R1 + R2 + Gap;
        private double Ab => R1 + Rk;
        private double Bc => R2 + Rk;

        internal double AngleAtLarger =>
            Math.Acos((Ab * Ab + Ac * Ac - Bc * Bc) / (2 * Ab * Ac));

        internal double AngleAtSmaller =>
            Math.Acos((Ac * Ac + Bc * Bc - Ab * Ab) / (2 * Ac * Bc));

        /// <summary>Axial position of the circle where the ray to the pocket meets particle 1.</summary>
        internal double LeftPlane => R1 * Math.Cos(AngleAtLarger);

        /// <summary>Axial position of the matching circle on particle 2.</summary>
        internal double RightPlane => Ac - R2 * Math.Cos(AngleAtSmaller);

        internal double LeftRadius => R1 * Math.Sin(AngleAtLarger);

        internal double RightRadius => R2 * Math.Sin(AngleAtSmaller);

        internal static Triangle Of(double r1, double r2, double rk, double gap) =>
            r2 > r1 ? new Triangle(r2, r1, rk, gap) : new Triangle(r1, r2, rk, gap);
    }

    /// <summary>
    /// The body, integrated. A solid of revolution between the two planes, with
    /// the lateral radius interpolated linearly between the two circles and the
    /// intruding spheres cut out at every station.
    /// </summary>
    private static double IntegratedVolume(Triangle t, int stations = 400_000)
    {
        var x0 = t.LeftPlane;
        var x1 = t.RightPlane;
        var step = (x1 - x0) / stations;
        var sum = 0.0;

        for (var k = 0; k < stations; k++)
        {
            var x = x0 + (k + 0.5) * step;
            var outer = t.LeftRadius + (x - x0) / (x1 - x0) * (t.RightRadius - t.LeftRadius);

            var left = t.R1 * t.R1 - x * x;
            var right = t.R2 * t.R2 - (t.Ac - x) * (t.Ac - x);
            var inner = Math.Max(
                left > 0 ? Math.Sqrt(left) : 0.0,
                right > 0 ? Math.Sqrt(right) : 0.0);

            sum += Math.Max(0.0, outer * outer - inner * inner);
        }

        return Math.PI * sum * step;
    }

    /// <summary>The same body in closed form: a frustum less two spherical caps.</summary>
    private static double ClosedFormVolume(Triangle t)
    {
        var height = t.RightPlane - t.LeftPlane;
        var frustum = Math.PI * height / 3
            * (t.LeftRadius * t.LeftRadius + t.LeftRadius * t.RightRadius + t.RightRadius * t.RightRadius);

        var h1 = t.R1 - t.LeftPlane;
        var h2 = t.R2 - (t.Ac - t.RightPlane);

        return frustum
            - Math.PI * h1 * h1 * (t.R1 - h1 / 3)
            - Math.PI * h2 * h2 * (t.R2 - h2 / 3);
    }

    public static TheoryData<double, double, double, double> Configurations => new()
    {
        { 100, 55, 30, 10 },
        { 100, 70, 30, 10 },
        { 100, 80, 30, 10 },
        { 100, 60, 10, 2 },
        { 100, 60, 60, 20 },
        { 300, 151, 80, 40 },
        { 45, 30, 12, 3 },
        { 200, 105, 40, 60 },
    };

    /// <summary>
    /// The angle construction <c>GA = AL + (π − AL − DE)/2</c> is not an
    /// approximation to the cone through the two contact circles — it is that cone.
    /// Checking it separately is what lets the volume test below treat the closed
    /// form as truth.
    /// </summary>
    [Theory]
    [MemberData(nameof(Configurations))]
    public void TheConeTheFormulaBuildsIsTheConeThroughBothContactCircles(
        double r1, double r2, double rk, double gap)
    {
        var t = Triangle.Of(r1, r2, rk, gap);

        var ga = t.AngleAtLarger + (Math.PI - t.AngleAtLarger - t.AngleAtSmaller) / 2;
        var modelHeight = (t.LeftRadius - t.RightRadius) * Math.Tan(ga);
        var geometricHeight = t.RightPlane - t.LeftPlane;

        Assert.Equal(geometricHeight, modelHeight, 1e-9 * geometricHeight);
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void TheClosedFormMatchesTheQuadrature(double r1, double r2, double rk, double gap)
    {
        var t = Triangle.Of(r1, r2, rk, gap);

        Assert.Equal(IntegratedVolume(t), ClosedFormVolume(t), 1e-5 * ClosedFormVolume(t));
    }

    /// <summary>
    /// With the radii well apart the <c>3.14</c> costs about a percent, so the port
    /// can be checked against the true body directly. The deficit is one-sided:
    /// the model always understates.
    /// </summary>
    [Theory]
    [MemberData(nameof(Configurations))]
    public void TheModelTracksTheTrueBodyWhereTheRadiiDiffer(double r1, double r2, double rk, double gap)
    {
        var outcome = BridgeVolume.Between(
            (float)r1, (float)r2, (float)rk, (float)gap, out var volume, out _);

        Assert.Equal(BridgeOutcome.Built, outcome);

        var truth = ClosedFormVolume(Triangle.Of(r1, r2, rk, gap));

        Assert.True(volume < truth, "the 3.14 substitution must understate, never overstate");
        Assert.True(
            Math.Abs(volume / truth - 1) < 0.025,
            $"model {volume:E6} against true body {truth:E6}");
    }

    /// <summary>
    /// ⚠ The finding this file exists to freeze. The <c>3.14</c> enters through
    /// <c>tan(GA)</c>, and <c>GA → π/2</c> as the radii converge, so the relative
    /// error diverges with the tangent: the model loses 0.7 % at a size ratio of
    /// 0.5 — the widest pair the AK1/AK2 window admits — 4.4 % at 0.9, 37 % at
    /// 0.99, and changes sign below about 0.999. That is not the "roughly 0.1 % on
    /// the final number" the design notes assumed. The numbers are pinned here so
    /// that a later "tidying" of the constant is a test failure and not a silent
    /// change of results.
    /// </summary>
    [Theory]
    [InlineData(0.50, -0.69)]
    [InlineData(0.60, -0.93)]
    [InlineData(0.70, -1.33)]
    [InlineData(0.80, -2.11)]
    [InlineData(0.90, -4.43)]
    [InlineData(0.95, -8.88)]
    [InlineData(0.98, -20.88)]
    [InlineData(0.99, -37.35)]
    [InlineData(0.995, -61.33)]
    public void TheDeficitGrowsWithoutBoundAsTheRadiiConverge(double ratio, double expectedPercent)
    {
        const double r1 = 100, rk = 30, gap = 10;
        var r2 = r1 * ratio;

        var outcome = BridgeVolume.Between(
            (float)r1, (float)r2, (float)rk, (float)gap, out var volume, out _);

        Assert.Equal(BridgeOutcome.Built, outcome);

        var truth = ClosedFormVolume(Triangle.Of(r1, r2, rk, gap));
        var percent = 100 * (volume / truth - 1);

        Assert.Equal(expectedPercent, percent, 0.05);
    }

    /// <summary>
    /// Below roughly a 0.1 % size difference the understatement exceeds the whole
    /// volume and the bridge comes out negative, which the caller accumulates
    /// without complaint. Reproduced, and pinned so it stays visible.
    /// </summary>
    [Theory]
    [InlineData(0.999)]
    [InlineData(1.0)]
    public void NearlyEqualParticlesProduceANegativeBridgeVolume(double ratio)
    {
        var outcome = BridgeVolume.Between(
            100f, (float)(100 * ratio), 30f, 10f, out var volume, out var width);

        Assert.Equal(BridgeOutcome.Built, outcome);
        Assert.True(width > 0, "the bridge is not rejected — only its volume is nonsense");
        Assert.True(volume < 0, $"expected a negative volume, got {volume:E6}");
    }

    [Fact]
    public void SubstitutingARealPiWouldChangeTheResult()
    {
        // Guards the taboo: if someone "improves" 3.14 into Math.PI, the model
        // stops agreeing with itself and this test says so.
        BridgeVolume.Between(100f, 70f, 30f, 10f, out var volume, out _);

        var withRealPi = ClosedFormVolume(Triangle.Of(100, 70, 30, 10));

        Assert.True(Math.Abs(volume / withRealPi - 1) > 0.005);
    }

    [Fact]
    public void APocketTooSmallForTheGapIsRejectedBeforeAnyArithmetic()
    {
        var outcome = BridgeVolume.Between(100f, 70f, 20f, 40f, out var volume, out var width);

        Assert.Equal(BridgeOutcome.GapExceedsPocket, outcome);
        Assert.True(float.IsNaN(volume));
        Assert.True(float.IsNaN(width));
    }

    /// <summary>
    /// The second rejection is a different event, not a variant of the first: the
    /// pocket fits the gap but sits so close to the line of centres that the bridge
    /// has no width. Both must stay separately countable.
    /// </summary>
    [Fact]
    public void APocketSeatedTooDeepIsRejectedAfterTheWidthIsKnown()
    {
        var outcome = BridgeVolume.Between(100f, 100f, 200f, 350f, out var volume, out var width);

        Assert.Equal(BridgeOutcome.WidthNegative, outcome);
        Assert.True(width < 0, "the computed width is reported, unlike the volume");
        Assert.True(float.IsNaN(volume));
    }

    [Fact]
    public void TheWidthIsTwiceTheClearanceBetweenThePocketAndTheLineOfCentres()
    {
        const double r1 = 100, r2 = 70, rk = 30, gap = 10;

        BridgeVolume.Between((float)r1, (float)r2, (float)rk, (float)gap, out _, out var width);

        // Perpendicular distance from the pocket centre to the line of centres,
        // by Heron rather than by the subroutine's own trigonometry.
        var t = Triangle.Of(r1, r2, rk, gap);
        var a = r1 + rk;
        var b = r2 + rk;
        var c = t.Ac;
        var s = (a + b + c) / 2;
        var area = Math.Sqrt(s * (s - a) * (s - b) * (s - c));
        var expected = 2 * (2 * area / c - rk);

        Assert.Equal(expected, width, 1e-5 * expected);
    }

    [Fact]
    public void TheOrderOfTheTwoParticlesDoesNotMatter()
    {
        var first = BridgeVolume.Between(100f, 70f, 30f, 10f, out var volumeA, out var widthA);
        var second = BridgeVolume.Between(70f, 100f, 30f, 10f, out var volumeB, out var widthB);

        Assert.Equal(first, second);
        Assert.Equal(volumeA, volumeB);
        Assert.Equal(widthA, widthB);
    }

    /// <summary>
    /// The arccosine really can be handed an argument above one — but only the one
    /// belonging to the <em>smaller</em> particle, and only where the width test
    /// rejects the configuration anyway.
    /// <para>
    /// At the degeneracy <c>A = 2·RK</c> both cosines equal exactly one. Reaching
    /// that value, <c>cosD = (AC² + BC² − AB²)/(2·AC·BC)</c> cancels <c>AB²</c>
    /// against itself, and <c>AB ≥ BC</c>, so the cancellation is catastrophic and
    /// the result overshoots. <c>cosA = (AB² + AC² − BC²)/(2·AB·AC)</c> has no such
    /// cancellation — its numerator reduces to <c>2·AB·(AB + BC)</c>, all positive.
    /// A search over four million near-degenerate configurations found <c>cosA</c>
    /// above one exactly zero times and <c>cosD</c> above one 12 203 times, and in
    /// every one of those the width came out negative (never above −1).
    /// </para>
    /// <para>
    /// So the NaN is caught by line 1612 before it can reach the volume, and the
    /// archive's clean record is structural rather than lucky. What the port must
    /// not do is add a clamp — that would turn a rejected bridge into an accepted
    /// one and change the counters.
    /// </para>
    /// </summary>
    [Fact]
    public void TheArccosineOvershootsOnlyWhereTheWidthTestAlreadyRejects()
    {
        const float r1 = 229.14190673828125f;
        const float r2 = 27.43547821044922f;
        const float rk = 0.7933260798454285f;
        const float gap = 1.5866326093673706f;

        // Independently: the smaller particle's cosine is above one, so its
        // arccosine is NaN inside the subroutine.
        double ab = r1 + rk, bc = r2 + rk, ac = r1 + r2 + gap;
        var cosD = (ac * ac + bc * bc - ab * ab) / (2 * ac * bc);
        Assert.True(cosD > 1.0, $"the fixture no longer overshoots: cosD = {cosD:R}");

        var outcome = BridgeVolume.Between(r1, r2, rk, gap, out var volume, out var width);

        Assert.Equal(BridgeOutcome.WidthNegative, outcome);
        Assert.True(width < 0, $"expected a negative width, got {width}");
        Assert.True(float.IsNaN(volume));
    }

    /// <summary>
    /// There is still no guard: a NaN arriving from the caller sails through every
    /// test — <c>NaN &gt;= 2·RK</c> and <c>NaN &lt; 0</c> are both false — and comes
    /// back as an accepted bridge of NaN volume, which the caller then adds to its
    /// running total. Reproduced deliberately.
    /// </summary>
    [Fact]
    public void ANaNInputIsNotGuardedAgainstAndIsReportedAsBuilt()
    {
        var outcome = BridgeVolume.Between(100f, 70f, 30f, float.NaN, out var volume, out _);

        Assert.Equal(BridgeOutcome.Built, outcome);
        Assert.True(float.IsNaN(volume));
    }
}
