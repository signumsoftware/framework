
# CubeDictionary

`CubeDictionary` is the three-dimensional equivalent to `IntervalDictionary` and `SquareDictionary`. It uses `Cube<T1,T2,T3>` as keys so the three dimensions can be defined by different types. 

```C#
public class Cube<T1, T2, T3>
    where T1 : struct, IComparable<T1>, IEquatable<T1>
    where T2 : struct, IComparable<T2>, IEquatable<T2>
    where T3 : struct, IComparable<T3>, IEquatable<T3>
{
    public readonly Interval<T1> XInterval;
    public readonly Interval<T2> YInterval;
    public readonly Interval<T3> ZInterval;

    public Cube(Interval<T1> intervalX, Interval<T2> intervalY, Interval<T3> intervalZ){...}
    public Cube(T1 minX, T1 maxX, T2 minY, T2 maxY, T3 minZ, T3 maxZ){...}
    (...)
}
```

As `SquareDictionary`, `CubeDictionary` is completely designed to be a read-only data structure.

It also has no method to modify the state, using the constructor as the only source of data.

That's why the API is so simple.     

```C#
public class CubeDictionary<K1, K2, K3, V>
    where K1 : struct, IComparable<K1>, IEquatable<K1>
    where K2 : struct, IComparable<K2>, IEquatable<K2>
    where K3 : struct, IComparable<K3>, IEquatable<K3>
{
    IntervalDictionary<K1, int> xDimension;
    IntervalDictionary<K2, int> yDimension;
    IntervalDictionary<K3, int> zDimension;
    V[,,] values;

    public CubeDictionary(IEnumerable<(Cube<K1, K2, K3> cube, V value)> dic) 

    //Retrieves the value for a key (not set available)
    //If no interval throws KeyNotFoundException       
    public V this[K1 x, K2 y, K3 z]{get;}

    //.Net 2.0 style TryGetValue, it there's no interval returns false
    public bool TryGetValue(K1 x, K2 y, K3 z, out V value)

    //Functional style TryGetValue using IntervalValue structure     
    public IntervalValue<V> TryGetValue(K1 x, K2 y, K3 z)
}
```