namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// Sequence and map equality for the result types.
/// </summary>
/// <remarks>
/// Every record here holds a list or a dictionary, and a record's generated equality
/// compares those by reference — so a result read back from disk would never equal
/// the one that was written and the round-trip guarantee would be vacuous. The same
/// trap already caught <c>StructureRunConfiguration</c> and
/// <c>ResolvedRunRecordSnapshot</c>; this is the third time, so the comparison lives
/// in one place rather than being written out again.
/// </remarks>
internal static class Structural
{
    internal static bool ListEquals<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        var comparer = QuantityComparer.For<T>();
        for (var index = 0; index < left.Count; index++)
        {
            if (!comparer.Equals(left[index], right[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Map equality that does not depend on enumeration order — two dictionaries
    /// holding the same pairs are equal however they were built.
    /// </summary>
    internal static bool MapEquals<TValue>(
        IReadOnlyDictionary<string, TValue> left,
        IReadOnlyDictionary<string, TValue> right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        var comparer = QuantityComparer.For<TValue>();
        foreach (var (key, value) in left)
        {
            if (!right.TryGetValue(key, out var other) || !comparer.Equals(value, other))
            {
                return false;
            }
        }

        return true;
    }

    internal static void AddList<T>(ref HashCode hash, IReadOnlyList<T> values)
    {
        // Hashing has to use the same comparer as equality, or a Length held in
        // micrometres would hash apart from the identical value held in metres.
        var comparer = QuantityComparer.For<T>();
        hash.Add(values.Count);
        foreach (var value in values)
        {
            hash.Add(value is null ? 0 : comparer.GetHashCode(value));
        }
    }

    /// <summary>
    /// Order-independent hash of a map, to match <see cref="MapEquals{TValue}"/>.
    /// </summary>
    internal static void AddMap<TValue>(ref HashCode hash, IReadOnlyDictionary<string, TValue> map)
    {
        hash.Add(map.Count);

        var comparer = QuantityComparer.For<TValue>();
        var combined = 0;
        foreach (var (key, value) in map)
        {
            combined ^= HashCode.Combine(key, value is null ? 0 : comparer.GetHashCode(value));
        }

        hash.Add(combined);
    }
}
