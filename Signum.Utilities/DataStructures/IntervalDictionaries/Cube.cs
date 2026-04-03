
namespace Signum.Utilities.DataStructures;

public class Cube<T1, T2, T3>
    where T1 : struct, IComparable<T1>, IEquatable<T1>
    where T2 : struct, IComparable<T2>, IEquatable<T2>
    where T3 : struct, IComparable<T3>, IEquatable<T3>
{
    public readonly Interval<T1> XInterval;
    public readonly Interval<T2> YInterval;
    public readonly Interval<T3> ZInterval;

    public Cube(Interval<T1> intervalX, Interval<T2> intervalY, Interval<T3> intervalZ)
    {
        this.XInterval = intervalX;
        this.YInterval = intervalY;
        this.ZInterval = intervalZ;
    }

    public Cube(T1 minX, T1 maxX, T2 minY, T2 maxY, T3 minZ, T3 maxZ)
        : this(
        new Interval<T1>(minX, maxX),
        new Interval<T2>(minY, maxY),
        new Interval<T3>(minZ, maxZ))
    {
    }

    public override string ToString()
    {
        return XInterval + " x " + YInterval + " x " + ZInterval;
    }
}
