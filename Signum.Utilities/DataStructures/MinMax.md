## MinMax

MinMax is a immutable structure. Is just a Tuple of elements of the same
type where Min is considered the minimum and Max the maximum in any
given order. It's different from Interval because the elements per-se
are not comparables, but usually have a member that is. It is returned
by `WithMinMaxPair`
function.

```C#
[Serializable]
public struct MinMax<T> : IEquatable<MinMax<T>>
{
    public readonly T Min;
    public readonly T Max;
    (..)
}
```

Example

```C#
countries.WithMinMaxPair(c=>c.Population).ToString(); //Returns [Min = Pitcaim Islands, Max = China]
```