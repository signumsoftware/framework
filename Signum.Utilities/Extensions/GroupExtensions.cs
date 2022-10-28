using Signum.Utilities.DataStructures;

namespace Signum.Utilities;

public static class GroupExtensions
{
    public static Dictionary<K, T> GroupDistinctToDictionary<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
        where K : notnull
    {
        return collection
            .GroupBy(keySelector)
            .ToDictionaryEx(g => g.Key, g => g.Distinct().AssertSingle(g.Key));
    }

    public static Dictionary<K, V> GroupDistinctToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
        where K : notnull
    {
        return collection
            .GroupBy(keySelector, valueSelector)
            .ToDictionaryEx(g => g.Key, g => g.Distinct().AssertSingle(g.Key));
    }

    public static T AssertSingle<T>(this IEnumerable<T> collection, object? key)
    {
        int c = collection.Count();
        if (c == 0) throw new InvalidOperationException("No element exists with key '{0}'".FormatWith(key));
        if (c > 1) throw new InvalidOperationException("There's more than one element with key '{0}'".FormatWith(key));
        return collection.SingleEx();
    }


    public static Dictionary<K, List<V>> GroupToDictionary<V, K>(this IEnumerable<KeyValuePair<K, V>> collection)
        where K : notnull
    {
        return collection
            .GroupBy(kvp => kvp.Key)
            .ToDictionaryEx(g => g.Key, g => g.Select(kvp => kvp.Value).ToList());
    }

    public static Dictionary<K, List<T>> GroupToDictionary<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
        where K : notnull
    {
        return collection
            .GroupBy(keySelector)
            .ToDictionaryEx(g => g.Key, g => g.ToList());
    }

    public static Dictionary<K, List<T>> GroupToDictionary<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector, IEqualityComparer<K> comparer)
        where K : notnull
    {
        return collection
            .GroupBy(keySelector, comparer)
            .ToDictionary(g => g.Key, g => g.ToList(), comparer);
    }

    public static Dictionary<K, List<V>> GroupToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
        where K : notnull
    {
        return collection
            .GroupBy(keySelector, valueSelector)
            .ToDictionaryEx(g => g.Key, g=>g.ToList());
    }

    public static Dictionary<K, List<V>> GroupToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector, IEqualityComparer<K> comparer)
    where K : notnull
    {
        return collection
            .GroupBy(keySelector, valueSelector, comparer)
            .ToDictionaryEx(g => g.Key, g => g.ToList(), comparer);
    }


    public static Dictionary<K, List<T>> GroupToDictionaryDescending<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
        where K : notnull
    {
        return collection
            .GroupBy(keySelector)
            .OrderByDescending(g => g.Count())
            .ToDictionaryEx(g => g.Key, g => g.ToList());
    }

    public static Dictionary<K, List<V>> GroupToDictionaryDescending<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
        where K : notnull
    {
        return collection
            .GroupBy(keySelector, valueSelector)
            .OrderByDescending(g => g.Count())
            .ToDictionaryEx(g => g.Key, g => g.ToList());
    }


    public static Dictionary<T, int> GroupCount<T>(this IEnumerable<T> collection)
        where T : notnull
    {
        return collection
            .GroupBy(a=>a)
            .ToDictionaryEx(g => g.Key, g => g.Count());
    }

    public static Dictionary<K, int> GroupCount<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
        where K : notnull
    {
        return collection
            .GroupBy(keySelector)
            .ToDictionaryEx(g => g.Key, g => g.Count());
    }

    public static Dictionary<K, V> AgGroupToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<IGrouping<K, T>, V> aggregateSelector)
        where K : notnull
    {
        return collection
            .GroupBy(t => keySelector(t))
            .ToDictionaryEx(g => g.Key, aggregateSelector);
    }

    public static Dictionary<K, V> AgGroupToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<IGrouping<K, T>, V> aggregateSelector, IEqualityComparer<K> comparer)
        where K : notnull
    {
        return collection
            .GroupBy(t => keySelector(t), comparer)
            .ToDictionaryEx(g => g.Key, aggregateSelector, comparer);
    }

    public static Dictionary<K, V> AgGroupToDictionaryDescending<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<IGrouping<K, T>, V> aggregateSelector)
        where K : notnull
    {
        return collection
            .GroupBy(t => keySelector(t))
            .OrderByDescending(g => g.Count())
            .ToDictionaryEx(g => g.Key, aggregateSelector);
    }

    public static IEnumerable<List<T>> Chunk<T>(this IEnumerable<T> collection, Func<T, int> elementSize, int groupSize)
    {
        List<T> newList = new List<T>();
        int accumSize = 0;
        foreach (var item in collection)
        {
            var size = elementSize(item);
            if ((accumSize + size) > groupSize && newList.Count > 0)
            {
                yield return newList;
                newList = new List<T> { item };
                accumSize = size;
            }
            else
            {
                accumSize += size;
                newList.Add(item);
            }
        }

        if (newList.Count != 0)
            yield return newList;
    }

    public static IEnumerable<IntervalWithEnd<T>> IntervalsOf<T>(this IEnumerable<T> collection, int groupSize)
        where T : struct, IEquatable<T>, IComparable<T>
    {
        return collection.OrderBy().Chunk(groupSize).Select(gr => new IntervalWithEnd<T>(gr.Min(), gr.Max()));
    }


    public static List<IGrouping<T, T>> GroupWhen<T>(this IEnumerable<T> collection, Func<T, bool> isGroupKey, BeforeFirstKey beforeFirstKey = BeforeFirstKey.Throw, bool includeKeyInGroup = false)
    {
        List<IGrouping<T, T>> result = new List<IGrouping<T, T>>();
        Grouping<T, T>? group = null;
        foreach (var item in collection)
        {
            if (isGroupKey(item))
            {
                group = new Grouping<T, T>(item);
                if (includeKeyInGroup)
                    group.Add(item);
                result.Add(group);
            }
            else
            {
                if (group == null)
                {
                    switch (beforeFirstKey)
                    {
                        case BeforeFirstKey.Throw:
                            throw new InvalidOperationException("First element should be a group key, or change the value in for beforeFirstKey");
                        case BeforeFirstKey.Skip:
                            break;
                        case BeforeFirstKey.DefaultGroup:
                            group = new Grouping<T, T>(default!);
                            result.Add(group);
                            group.Add(item);
                            break;
                        default:
                            throw new UnexpectedValueException(beforeFirstKey);
                    }
                }
                else
                {
                    group.Add(item);
                }
            }
        }

        return result;
    }

    public static IEnumerable<IGrouping<K, T>> GroupWhenChange<T, K>(this IEnumerable<T> collection, Func<T, K> getGroupKey)
    {
        Grouping<K, T>? current = null;

        foreach (var item in collection)
        {
            if (current == null)
            {
                current = new Grouping<K, T>(getGroupKey(item))
                {
                    item
                };
            }
            else if (object.Equals(current.Key, getGroupKey(item)))
            {
                current.Add(item);
            }
            else
            {
                yield return current;
                current = new Grouping<K, T>(getGroupKey(item))
                {
                    item
                };
            }
        }

        if (current != null)
            yield return current;
    }
}

public enum BeforeFirstKey
{
    Throw,
    Skip,
    DefaultGroup
}
