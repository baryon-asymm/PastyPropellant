using UnitsNet;

namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// Equality for the values these records hold, comparing quantities in SI.
/// </summary>
/// <remarks>
/// <para>
/// UnitsNet's own equality compares the value <b>and</b> the unit, so
/// <c>Length.FromMicrometers(10)</c> does not equal <c>Length.FromMeters(1e-5)</c> —
/// and a result written to disk comes back in metres, because that is what the
/// converter writes. Left alone, the round-trip guarantee would fail on every length
/// in the result while every number in the file was correct.
/// </para>
/// <para>
/// Comparing in metres also keeps the documented one-ulp fact harmless:
/// <c>Length.FromMicrometers(10).Meters</c> is 9.999999999999999e-06, not 1e-05, and
/// that is the number the file holds, so it is the number that must be compared.
/// </para>
/// </remarks>
internal static class QuantityComparer
{
    internal static IEqualityComparer<T> For<T>() =>
        typeof(T) == typeof(Length)
            ? (IEqualityComparer<T>)(object)LengthInMetres.Instance
            : EqualityComparer<T>.Default;

    private sealed class LengthInMetres : IEqualityComparer<Length>
    {
        internal static readonly LengthInMetres Instance = new();

        public bool Equals(Length x, Length y) => x.Meters.Equals(y.Meters);

        public int GetHashCode(Length obj) => obj.Meters.GetHashCode();
    }
}
