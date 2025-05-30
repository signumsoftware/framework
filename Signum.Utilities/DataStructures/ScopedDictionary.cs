using System.Diagnostics.CodeAnalysis;

namespace Signum.Utilities.DataStructures;

public class ScopedDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey: notnull
{
    ScopedDictionary<TKey, TValue>? previous;
    Dictionary<TKey, TValue> map;

    public IEqualityComparer<TKey> Comparer { get { return map.Comparer; } }
    public ScopedDictionary<TKey, TValue>? Previous { get { return previous; } }

    public ScopedDictionary(ScopedDictionary<TKey, TValue>? previous)
        :this(previous, EqualityComparer<TKey>.Default)
    {
        this.previous = previous;
        this.map = new Dictionary<TKey, TValue>();
    }

    public ScopedDictionary(ScopedDictionary<TKey, TValue>? previous, IEqualityComparer<TKey> comparer)
    {
        this.previous = previous;
        this.map = new Dictionary<TKey, TValue>(comparer);
    }

    public void Add(TKey key, TValue value)
    {
        this.map.Add(key, value);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        for (ScopedDictionary<TKey, TValue>? scope = this; scope != null; scope = scope.previous)
        {
            if (scope.map.TryGetValue(key, out value))
                return true;
        }
        value = default!;
        return false;
    }

    public bool ContainsKey(TKey key)
    {
        for (ScopedDictionary<TKey, TValue>? scope = this; scope != null; scope = scope.previous)
        {
            if (scope.map.ContainsKey(key))
                return true;
        }
        return false;
    }

    public override string ToString()
    {
        var str = map.ToString(kvp => kvp.Key + " -> " + kvp.Value, "\n");

        if (this.previous == null)
            return str;

        return str + "\n-----------------------\n" + previous.ToString();
    }

    public string ToString(Func<TKey, string> keyRenderer, Func<TValue, string> valueRenderer)
    {
        var str = map.ToString(kvp => keyRenderer(kvp.Key) + " -> " + valueRenderer(kvp.Value), "\n");

        if (this.previous == null)
            return str;

        return str + "\n-----------------------\n" + previous.ToString(keyRenderer, valueRenderer);
    }

    public TValue GetOrCreate(TKey key, Func<TValue> valueFactory)
    {
        if (!TryGetValue(key, out TValue? result))
        {
            result = valueFactory();
            Add(key, result);
        }
        return result;
    }


    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        for (ScopedDictionary<TKey, TValue>? sd = this; sd != null; sd = sd.previous)
        {
            foreach (var item in sd.map)
            {
                yield return item;
            }
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
