using System.Collections;

namespace Signum.Utilities;

public class ProgressEnumerator<T>: IEnumerable<T>, IEnumerator<T>, IProgressInfo
{
    static long fiveSecconds = PerfCounter.FrequencyMilliseconds * 5 * 1000;

    long start;

    long lastTicks;
    int lastCurrent;

    long lastLastTick;
    int lastLastCurrent;

    int countStep;

    int count;
    int current = 0;

    IEnumerator<T> enumerator;

    public ProgressEnumerator(IEnumerable<T> source, int count)
    {
        enumerator = source.GetEnumerator();
        this.count = count;
        this.lastCurrent = this.lastLastCurrent = 0;
        this.lastTicks = this.lastLastTick = this.start = PerfCounter.Ticks;
        this.countStep = GetCountStep(countStep);
    }

    public static int GetCountStep(int num)
    {
        if (num < 0x1000)
            return 0xFF;

        return 0xFFF;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this;
    }

    public T Current
    {
        get { return enumerator.Current; }
    }

    object IEnumerator.Current
    {
        get { return current; }
    }

    object syncLock = new object();

    public bool MoveNext()
    {
        if (enumerator.MoveNext())
        {
            if ((current & countStep) == 0)
            {
                var now = PerfCounter.Ticks;
                lastLastTick = lastTicks;
                lastLastCurrent = lastCurrent;

                lastTicks = now;
                lastCurrent = current;
            }

            current++;

            return true;
        }
        return false;
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {

    }

    double IProgressInfo.Percentage
    {
        get { return 100 * ((IProgressInfo)this).Ratio; }
    }

    double IProgressInfo.Ratio
    {
        get { return SafeDiv(current, count); }
    }

    private double SafeDiv(int current, int count)
    {
        if (count == 0)
            return 0;

        return current / (double)count;
    }

    TimeSpan IProgressInfo.Elapsed
    {
        get { return TimeSpan.FromMilliseconds((PerfCounter.Ticks - start) / PerfCounter.FrequencyMilliseconds); }
    }

    TimeSpan IProgressInfo.Remaining
    {
        get
        {
            double ratio = SafeDiv(current - lastLastCurrent, count - lastLastCurrent);

            if (ratio == 0)
                return TimeSpan.Zero;

            var now = PerfCounter.Ticks;

            long lastToNow = (now - lastLastTick);

            long lastToFinish = (long)(lastToNow / ratio);

            return TimeSpan.FromMilliseconds((lastToFinish - lastToNow) / PerfCounter.FrequencyMilliseconds);
        }
    }

    DateTime IProgressInfo.EstimatedFinish
    {
        get { return (DateTime.UtcNow + ((IProgressInfo)this).Remaining).ToLocalTime(); }
    }

    public override string ToString()
    {
        IProgressInfo me = (IProgressInfo)this;
        TimeSpan rem = me.Remaining;
        TimeSpan ela = me.Elapsed;
        return "{0:0.00}% | {1}/{2} | Elap: {3} + Rem: {4} = Total: {5} -> Finish: {6:u}".FormatWith(
            me.Percentage, current, count,
            ela.NiceToString(DateTimePrecision.Seconds),
            rem.NiceToString(DateTimePrecision.Seconds),
            (ela + rem).NiceToString(DateTimePrecision.Seconds),
            (DateTime.UtcNow + rem).ToLocalTime());
    }
}

public interface IProgressInfo
{
    double Percentage { get; }

    double Ratio { get; }

    TimeSpan Elapsed { get; }

    TimeSpan Remaining { get; }
    DateTime EstimatedFinish { get; }
}
