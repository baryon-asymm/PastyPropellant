namespace PastyPropellant.PropellantStructure.Sampling;

/// <summary>
/// <c>gsv = 1</c>: the original's <c>RANDOM1</c> (PropStructv3.for, lines 1632–1639).
/// </summary>
/// <remarks>
/// <para>
/// Four lines of Fortran and no state beyond two REAL*4 words:
/// <c>random1 = A + B - int(A+B)</c>, then <c>B = A</c> and <c>A = random1</c>. It is a
/// lagged-Fibonacci generator of lag 2 over the unit interval — the weakest thing that
/// still returns numbers in (0,1), which is presumably why the author wrote a second
/// one.
/// </para>
/// <para>
/// ⚠ The arithmetic is single precision and the port keeps it there. <c>A + B</c> lies
/// in [0,2) and is <b>not</b> in general exact in a float, so where the sum is rounded
/// decides the last bit of every variate the run will ever produce. The subtree's rule
/// applies — evaluate in double, round on assignment to the REAL*4 result — and here
/// that rule is not a convention but a checked fact: it is what reproduces
/// <c>probe4.m</c>. The subtraction itself is exact either way, since a value in [1,2)
/// minus one is representable.
/// </para>
/// </remarks>
internal sealed class Random1Stream : IRandomStream
{
    private float _a;
    private float _b;

    internal Random1Stream(float a, float b)
    {
        _a = a;
        _b = b;
    }

    /// <summary>The pair the original starts from (line 345–346).</summary>
    internal static (float A, float B) Origin => (0.12345678f, 0.87654321f);

    public double Next()
    {
        // The one place precision is decided; see the remarks.
        var sum = (double)_a + _b;
        var value = (float)(sum - Math.Truncate(sum));

        _b = _a;
        _a = value;
        return value;
    }

    /// <summary>The state as the original would have recorded it in <c>AX</c>/<c>BX</c>.</summary>
    internal (float A, float B) State => (_a, _b);
}
