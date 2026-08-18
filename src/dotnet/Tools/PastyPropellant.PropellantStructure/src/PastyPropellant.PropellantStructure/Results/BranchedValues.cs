using System.Collections;

namespace PastyPropellant.PropellantStructure.Results;

/// <summary>
/// The branches one quantity has, and a typed refusal for the ones it has not.
/// </summary>
/// <remarks>
/// A plain dictionary would answer a wrong branch with
/// <see cref="KeyNotFoundException"/>, which reads as a lookup slip rather than as
/// what it is: an ask for a value the original never computed. <c>D10</c> is
/// corrected once, <c>D43</c> twice, and a caller who assumes otherwise should be
/// told which branches exist.
/// </remarks>
internal sealed class BranchedValues<T> : IReadOnlyDictionary<Correction, Verified<T>>
{
    private readonly Dictionary<Correction, Verified<T>> _values;
    private readonly string _quantity;

    internal BranchedValues(string quantity, Dictionary<Correction, Verified<T>> values)
    {
        _quantity = quantity;
        _values = values;
    }

    public IEnumerable<Correction> Keys => _values.Keys;

    public IEnumerable<Verified<T>> Values => _values.Values;

    public int Count => _values.Count;

    public Verified<T> this[Correction key] =>
        _values.TryGetValue(key, out var value)
            ? value
            : throw new BranchMismatchException(
                $"{_quantity} has no branch {key}; it carries {string.Join(", ", _values.Keys.Order())}.");

    public bool ContainsKey(Correction key) => _values.ContainsKey(key);

    public bool TryGetValue(Correction key, out Verified<T> value) => _values.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<Correction, Verified<T>>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
