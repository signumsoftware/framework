# IntervalDictionary

`IntervalDictionary` allows you to create a dictionary that has `Intervals` as keys instead of values. Internally, an IntervalDictionary is implemented with a `SortedList` of intervals, but only some methods are provided to keep the consistency.

Also, `IntervalDictionary` is designed to be a read-only source of information, not to be modified frequently. Currently there's no support to remove intervals for example. 

```C#
public class IntervalDictionary<K,V>: IEnumerable<KeyValuePair<Interval<K>, V>> 
    where K: struct, IComparable<K>, IEquatable<K>
{
    SortedList<Interval<K>, V> dic = new SortedList<Interval<K>, V>();

    //Constructors
    public IntervalDictionary()
    public IntervalDictionary(IEnumerable<(Interval<K> interval, V value)> pares)
  
    public IList<Interval<K>> Intervals {get;}
    public int Count {get;}

    //Adds a new interval, throwing ArgumentException if overlaps with a previous one
    public new void Add(Interval<K> time, V value)
    public void Add(K min, K max, V value)

    //Retrieves the value for a key (not set available). 
    //If no interval throws KeyNotFoundException
    public V this[K key]{ get; }

    //.Net 2.0 style TryGetValue, it there's no interval returns false
    public bool TryGetValue(K key, out V value)

    //Functional style TryGetValue using IntervalValue structure 
    public IntervalValue<V> TryGetValue(K key)

    public K? TotalMin {get;} //Min value in the minimum interval 
    public K? TotalMax {get;} //Max value in the maximum interval

    public IEnumerator<KeyValuePair<Interval<K>, V>> GetEnumerator()
    IEnumerator IEnumerable.GetEnumerator()

    public override string ToString()
}

public struct IntervalValue<T>
{
    public readonly bool HasInterval;
    public readonly T Value;

    public IntervalValue(T value){...}
}
```

Example: 

```C#
var interval = new IntervalDictionary<decimal, string>
{
    {0,1, "baby" },
    {1,3, "toddler" },
    {3,5, "preschool" },
    {5,9, "child" },
    {9,12, "tween" },
    {12,18, "teenager" },
    {18,65, "Adult" },
    {65,int.MaxValue, "Senior" },
};

new decimal[]{0,0.5m,1,2,3,4,5,7,9,11.5m,15,20,30,45,65,70,80,90}
.ToConsole(a => "When I was a {0} years old {1}...".FormatWith(a, interval[a]));

//Writes:
//When I was a 0 years old baby...
//When I was a 0,5 years old baby...
//When I was a 1 years old toddler...
//When I was a 2 years old toddler...
//When I was a 3 years old preschool...
//When I was a 4 years old preschool...
//When I was a 5 years old child...
//When I was a 7 years old child...
//When I was a 9 years old tween...
//When I was a 11,5 years old tween...
//When I was a 15 years old teenager...
//When I was a 20 years old Adult...
//When I was a 30 years old Adult...
//When I was a 45 years old Adult...
//When I was a 65 years old Senior...
//When I was a 70 years old Senior...
//When I was a 80 years old Senior...
//When I was a 90 years old Senior...
```