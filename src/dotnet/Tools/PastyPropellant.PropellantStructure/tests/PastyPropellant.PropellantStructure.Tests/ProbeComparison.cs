using PastyPropellant.PropellantStructure.Results;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// Compares a packed result against a probe's printed scalars, one tolerance per print
/// format, collecting every miss instead of stopping at the first.
/// </summary>
/// <remarks>
/// <para>
/// Shared by the probe fixtures because the tolerances are a property of the original's
/// <c>write</c> statements, not of any one run: the same <c>F7.2</c> that gives
/// <c>Dkarm43</c> two decimals gives it two decimals in every probe. Duplicating them
/// per fixture is how a tolerance quietly becomes per-test and then becomes whatever
/// makes the test pass.
/// </para>
/// <para>
/// ⚠ Every miss is reported, not just the first. On a fixture for a branch nobody has
/// run before, the shape of the failure — three scalars or thirty, all in one block or
/// scattered — is most of the diagnosis.
/// </para>
/// </remarks>
internal sealed class ProbeComparison(StructureResult result)
{
    private readonly List<string> _problems = [];

    /// <summary>List-directed output: relative 1e-6, all seven digits a single carries.</summary>
    internal void Relative(string name, double expected) =>
        Absolute(name, expected, Math.Abs(expected) * 1e-6);

    /// <summary>
    /// Relative 1e-5, for the three scalars that read a bridge volume and so inherit the
    /// math library's last bit. See <c>ProbeRun.ThreeScalarsInheritTheMathLibrarysLastBit</c>.
    /// </summary>
    internal void Amplified(string name, double expected) =>
        Absolute(name, expected, Math.Abs(expected) * 1e-5);

    /// <summary><c>F7.2</c> on micrometres: half of the last digit shown.</summary>
    internal void Micrometres(string name, double expected)
    {
        var actual = result.PrintedLengths[name].Value.Micrometers;
        if (Math.Abs(actual - expected) > 0.005)
        {
            _problems.Add($"{name}: {actual:F4} mkm, oracle {expected:F2}.");
        }
    }

    /// <summary>
    /// The quantity the original printed as <c>NaN</c>, and which the port must therefore
    /// also produce as <c>NaN</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ Not a way to excuse a value the port got wrong. It asserts the <b>positive</b>
    /// claim that the original itself printed nothing usable here, so a port returning a
    /// number would be the one departing from the reference.
    /// </remarks>
    internal void NotANumber(string name)
    {
        var actual = result.PrintedLengths[name].Value.Micrometers;
        if (!double.IsNaN(actual))
        {
            _problems.Add($"{name}: {actual:F4} mkm, oracle NaN.");
        }
    }

    internal void Absolute(string name, double expected, double tolerance)
    {
        var actual = result.Printed[name].Value;
        if (Math.Abs(actual - expected) > tolerance)
        {
            _problems.Add($"{name}: {actual:R}, oracle {expected:R}.");
        }
    }

    /// <summary>Fails with every miss at once, or passes.</summary>
    internal void Verify() =>
        Assert.True(_problems.Count == 0, string.Join(Environment.NewLine, _problems));
}
