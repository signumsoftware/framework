# Interval


Interval is a immutable structure of an ordered pair. The elements have
to be comparable and Min is always lesser or equals than Max.

```C#
public struct Interval<T> : IEquatable<Interval<T>>, IComparable<Interval<T>> 
     where T: struct, IComparable<T>, IEquatable<T>
{    
    public readonly T min;
    public readonly T max;

    public T Min { get { return min; } }
    public T Max { get { return max; } }

    public Interval(T min, T max) //Constructor
    public bool Contains(T value) //Returns true if val is inside of the interval
    public bool Overlap(Interval<T> other) //Returns true if the intervals overlap

    public Interval<T>? Intersection(Interval<T> other) //Returns the intersection of intervals, if any
    public Interval<T>? Union(Interval<T> other) //Returns the union of intervals
    public bool Subset(Interval<T> other) //returns true if other is a subset of the current instance

    public IEnumerable<T> Elements() //returns Min and Max as an IEnumerable<T>
 
    public int CompareTo(Interval<T> other) //compares two intervals by comparing their Min value
    public override string ToString()
    public bool Equals(Interval<T> other)
    public override bool Equals(object obj)    
    public override int GetHashCode()
}
```

Example:

```C#
   new Interval<int>(1,3).Intersect(new Interval<int>(0,2)).ToString(); //Returns: [1,2]
```