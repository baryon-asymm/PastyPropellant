namespace PastyPropellant.PropellantStructure.Geometry;

/// <summary>Why <see cref="BridgeVolume.Between"/> did or did not produce a bridge.</summary>
internal enum BridgeOutcome
{
    /// <summary>JJ = 1 on return: the bridge exists and both outputs are valid.</summary>
    Built,

    /// <summary>
    /// Line 1597, <c>A ≥ 2·RK</c>, checked before any arithmetic: the pocket does
    /// not fit in the gap. Equivalently, the triangle on the three centres does not
    /// close.
    /// </summary>
    GapExceedsPocket,

    /// <summary>Line 1612, <c>BB &lt; 0</c>, checked after the width is known: the bridge is degenerate.</summary>
    WidthNegative,
}

/// <summary>
/// The model's one piece of geometry: the volume of the bridge between two
/// oxidiser particles with a pocket sphere seated in the gap. Port of <c>VM</c>,
/// lines 1588–1629.
/// </summary>
/// <remarks>
/// <para>
/// The body is a solid of revolution about the line joining the particle centres.
/// The ray from each centre towards the pocket centre meets that particle's
/// surface in a circle; the body is bounded by the planes of those two circles,
/// laterally by the cone through them, and is reduced by the two spherical caps
/// the particles push into it. <c>VM</c> is the exact closed form of that body —
/// its identity <c>(R1A − R2A)·tan(AL + (π − AL − DE)/2) = AC − Q1 − Q2</c> holds
/// to machine precision, so the cone the formula builds from the triangle's angles
/// really is the cone through both circles.
/// </para>
/// <para>
/// ⚠ Exact, that is, in <c>π</c>. The subroutine uses <c>3.14</c>, and the
/// substitution is not a scale error here: it enters through <c>tan(GA)</c>, and
/// <c>GA → π/2</c> as the two radii converge, so the relative error diverges where
/// the tangent does. See <c>BOOT.md</c> for the measured curve. Reproduced, not
/// repaired.
/// </para>
/// </remarks>
internal static class BridgeVolume
{
    /// <summary>
    /// VM. Every intermediate is a REAL*4 variable in the original, so each
    /// assignment rounds to float; the right-hand sides are evaluated in double.
    /// </summary>
    /// <param name="radiusA">R1 — one particle's radius, metres.</param>
    /// <param name="radiusB">R2 — the other particle's radius, metres.</param>
    /// <param name="pocketRadius">RK — the seated pocket's radius, metres.</param>
    /// <param name="gap">A — the surface-to-surface gap between the particles, metres.</param>
    /// <param name="volume">VMKM, m³. <see cref="float.NaN"/> unless the outcome is <see cref="BridgeOutcome.Built"/>.</param>
    /// <param name="width">
    /// BB — twice the clearance between the pocket sphere and the line of centres.
    /// An output the caller uses, not a local. <see cref="float.NaN"/> when the
    /// subroutine returned before computing it.
    /// </param>
    /// <remarks>
    /// The arguments are taken by value. The original swaps its two radii in place
    /// (lines 1591–1596), but its only call site passes the expressions
    /// <c>Dr/2.</c> and <c>Db/2.</c>, so it swaps temporaries and the caller never
    /// sees it. Passing anything by reference here would change that.
    /// <para>
    /// On the two failure paths the original returns leaving the caller's previous
    /// VMKM and BB in place, and the caller discards them. The port returns NaN
    /// instead so that reading them is loud rather than plausible.
    /// </para>
    /// </remarks>
    internal static BridgeOutcome Between(
        float radiusA,
        float radiusB,
        float pocketRadius,
        float gap,
        out float volume,
        out float width)
    {
        // lines 1591-1596
        var r1 = radiusA;
        var r2 = radiusB;
        if (r2 > r1)
        {
            (r1, r2) = (r2, r1);
        }

        var rk = pocketRadius;

        // line 1597. This is also what guarantees the triangle below closes:
        // AB + BC >= AC reduces to exactly 2*RK >= A.
        if (gap >= 2.0 * rk)
        {
            volume = float.NaN;
            width = float.NaN;
            return BridgeOutcome.GapExceedsPocket;
        }

        // lines 1601-1603: the triangle on the two particle centres and the pocket centre.
        var ab = (float)((double)r1 + rk);
        var bc = (float)((double)r2 + rk);
        var ac = (float)((double)r1 + r2 + gap);

        // lines 1604-1605
        var cosA = (float)(((double)ab * ab + (double)ac * ac - (double)bc * bc) / (2.0 * ab * ac));
        var cosD = (float)(((double)ac * ac + (double)bc * bc - (double)ab * ab) / (2.0 * ac * bc));

        // lines 1606-1609. No clamp on the arccosine argument: at gap just under
        // 2*RK rounding can push it past one, and the resulting NaN is meant to
        // propagate.
        var q1 = (float)((double)r1 * cosA);
        var q2 = (float)((double)r2 * cosD);
        var al = (float)Math.Acos(cosA);
        var de = (float)Math.Acos(cosD);

        // line 1610
        width = (float)(2.0 * ((double)ab * Math.Sin(al) - rk));

        // Line 1611 computes a residual between two ways of getting BB and never
        // reads it again. Not transcribed, under the subtree's dead-assignment
        // rule (see ../BOOT.md); PARAM's Zerror is the other case.

        // line 1612
        if (width < 0)
        {
            volume = float.NaN;
            return BridgeOutcome.WidthNegative;
        }

        // lines 1616-1621: the cone through the two contact circles.
        var be = (float)(3.14f - (double)al - de);
        var ga = (float)((double)al + be / 2.0);
        var r1a = (float)((double)r1 * Math.Sin(al));
        var r2a = (float)((double)r2 * Math.Sin(de));
        var q1m = (float)((double)r1a * Math.Tan(ga));
        var q2m = (float)((double)r2a * Math.Tan(ga));

        // lines 1622-1625: the caps the particles push into the body.
        var h1 = (float)((double)r1 - q1);
        var h2 = (float)((double)r2 - q2);
        var v1 = (float)(3.14f * ((double)h1 * h1) * ((double)r1 - (double)h1 / 3.0));
        var v2 = (float)(3.14f * ((double)h2 * h2) * ((double)r2 - (double)h2 / 3.0));

        // lines 1626-1627, kept as two assignments because each rounds to float.
        volume = (float)(3.14f * ((double)r1a * r1a) * q1m / 3.0
                         - 3.14f * ((double)r2a * r2a) * q2m / 3.0);
        volume = (float)((double)volume - v1 - v2);

        return BridgeOutcome.Built;
    }
}
