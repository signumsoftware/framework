using System.Collections.Concurrent;
using System.Diagnostics;

namespace Signum.Utilities;

public static class TimeTracker
{
    public static ConcurrentDictionary<string, TimeTrackerEntry> IdentifiedElapseds = new ConcurrentDictionary<string, TimeTrackerEntry>();

    public static IDisposable Start(string identifier, string? url = null, Func<object>? getUser = null)
    {
        Stopwatch sp = new Stopwatch();
        sp.Start();

        return new Disposable(() => { sp.Stop(); InsertEntity(identifier, sp.ElapsedMilliseconds, url, getUser?.Invoke()); });
    }

    static void InsertEntity(string identifier, long milliseconds, string? url, object? user)
    {
        var time = new TimeTrackerTime(milliseconds, DateTime.UtcNow, url, user);

        if (IdentifiedElapseds.TryGetValue(identifier, out var entry))
        {
            entry.Include(time);
        }
        else
        {
            if (!IdentifiedElapseds.TryAdd(identifier, new TimeTrackerEntry(identifier, time)))
            {
                IdentifiedElapseds.TryGetValue(identifier, out entry); //Race condition adding the element
                entry!.Include(time);
            }
        }
    }
}


public class TimeTrackerTime
{
    public readonly long Duration;
    public readonly DateTime Date;
    public readonly string? Url;
    public readonly object? User;

    public TimeTrackerTime(long duration, DateTime date, string? url, object? user)
    {
        Duration = duration;
        Date = date;
        Url = url;
        User = user;
    }
}

public class TimeTrackerEntry
{
    public TimeTrackerEntry(TimeTrackerEntry other)
    {
        Identifier = other.Identifier;
        Last = other.Last;
        Count = other.Count;
        Max = other.Max;
        Min = other.Min;
        TotalDuration = other.TotalDuration;
        Max2 = other.Max2 != null && other.Max2 != Min ? other.Max2 : null;
        Max3 = other.Max3 != null && other.Max3 != Min ? other.Max3 : null;
    }

    public TimeTrackerEntry(string identifier, TimeTrackerTime time)
    {
        this.Identifier = identifier;
        this.Last = this.Min = this.Max = time;
        this.TotalDuration = time.Duration;
        this.Count = 1;
    }

    public void Include(TimeTrackerTime time)
    {
        this.Last = time;
        this.TotalDuration += time.Duration;
        this.Count++;
        if (time.Duration < this.Min.Duration || (Max3 ?? Max2 ?? Max).Duration < time.Duration)
        {
            lock (this)
            {
                if (time.Duration < this.Min.Duration)
                {
                    this.Min = time;
                }
                
                if (this.Max.Duration < time.Duration)
                {
                    this.Max3 = this.Max2;
                    this.Max2 = this.Max;
                    this.Max = time;
                }
                else if (this.Max2 == null || this.Max2.Duration < time.Duration)
                {
                    this.Max3 = this.Max2;
                    this.Max2 = time;
                }
                else if (this.Max3 == null || this.Max3.Duration < time.Duration)
                {
                    this.Max3 = time;
                }
            }
        }
    }

    public string Identifier; 

    public TimeTrackerTime Last;
    public TimeTrackerTime Min;


    public TimeTrackerTime Max;
    public TimeTrackerTime? Max2;
    public TimeTrackerTime? Max3;

    public long TotalDuration = 0;
    public int Count = 0;

    public double AverageDuration
    {
        get { return (TotalDuration / Count); }
    }

    public override string ToString()
    {
        return string.Format("Last: {0}ms, Min: {1}ms, Avg: {2}ms, Max: {3}ms, Count: {4}", Last.Duration, Min.Duration, AverageDuration, Max.Duration, Count);
    }
}
