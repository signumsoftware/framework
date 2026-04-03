
namespace Signum.Utilities.DataStructures;

public class Square<T1,T2>
    where T1 : struct, IComparable<T1>, IEquatable<T1>
    where T2 : struct, IComparable<T2>, IEquatable<T2>
{
    public readonly Interval<T1> XInterval;
    public readonly Interval<T2> YInterval;

    public Square(Interval<T1> xInterval, Interval<T2> yInterval)
    {
        this.XInterval = xInterval;
        this.YInterval = yInterval;
    }

    public Square(T1 minX, T1 maxX, T2 minY, T2 maxY)
        : this(new Interval<T1>(minX, maxX), new Interval<T2>(minY, maxY))
    {
    }

    public override string ToString()
    {
        return XInterval + " x " + YInterval;
    }
}
