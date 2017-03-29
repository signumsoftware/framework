# SquareDictionary
`SquareDictionary` is the bi-dimensional equivalent to `IntervalDictionary`. It uses `Square<T1,T2>` as keys so the two dimensions can be defined by different types. 

```C#
public class Square<T1,T2>
    where T1 : struct, IComparable<T1>, IEquatable<T1>
    where T2 : struct, IComparable<T2>, IEquatable<T2>
{
    public readonly Interval<T1> XInterval;
    public readonly Interval<T2> YInterval;

    public Square(Interval<T1> xInterval, Interval<T2> yInterval)
    public Square(T1 minX, T1 maxX, T2 minY, T2 maxY)
    (...)
}
```

`SquareDictionary` is completely designed to be a read-only data structure.

Actually, there's no method to modify the state, that has to be complete once it's created using the constructor. That's why the API is so simple.     

```C#
public class SquareDictionary<K1, K2, V>
    where K1 : struct, IComparable<K1>, IEquatable<K1>
    where K2 : struct, IComparable<K2>, IEquatable<K2>
{
    IntervalDictionary<K1, int> xDimension;
    IntervalDictionary<K2, int> yDimension;
    V[,] values;

    public SquareDictionary(IEnumerable<(Square<K1, K2> square, V value)> dictionary)

    //Retrieves the value for a key (not set available)
    //If no interval throws KeyNotFoundException
    public V this[K1 x, K2 y]{get;}

    //.Net 2.0 style TryGetValue, if there's no interval returns false
    public bool TryGetValue(K1 x, K2 y, out V value)

    //Functional style TryGetValue using IntervalValue structure 
    public IntervalValue<V> TryGetValue(K1 x, K2 y)
}
```