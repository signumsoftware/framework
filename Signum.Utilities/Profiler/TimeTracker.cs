using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Signum.Utilities
{
    public static class TimeTracker
    {
        public static Dictionary<string, TimeTrackerEntry> IdentifiedElapseds = new Dictionary<string, TimeTrackerEntry>();

        public static T Start<T>(string identifier, Func<T> func)
        {
            using (Start(identifier))
                return func();
        }

        public static IDisposable Start(string identifier)
        {
            Stopwatch sp = new Stopwatch();
            sp.Start();

            return new Disposable(() => { sp.Stop(); InsertEntity(sp.ElapsedMilliseconds, identifier); });
        }

        public static string GetTableString()
        {
            return IdentifiedElapseds.Select(kvp => new
            {
                Identified = kvp.Key,
                kvp.Value.LastTime,
                kvp.Value.MinTime,
                kvp.Value.Average,
                kvp.Value.MaxTime,
                kvp.Value.Count
            }).ToStringTable().FormatTable();
        }

        static void InsertEntity(long milliseconds, string identifier)
        {
            lock (IdentifiedElapseds)
            {
                TimeTrackerEntry entry = IdentifiedElapseds.GetOrCreate(identifier);

                entry.LastTime = milliseconds;
                entry.LastDate = DateTime.UtcNow;
                entry.TotalTime += milliseconds;
                entry.Count++;

                if (milliseconds < entry.MinTime) { entry.MinTime = milliseconds; entry.MinDate = DateTime.UtcNow; }
                if (milliseconds > entry.MaxTime) { entry.MaxTime = milliseconds; entry.MaxDate = DateTime.UtcNow; }
            }
        }
    }

    public class TimeTrackerEntry
    {
        public long LastTime = 0;
        public DateTime LastDate;

        public long MinTime = long.MaxValue;
        public DateTime MinDate;
     
        public long MaxTime = long.MinValue;
        public DateTime MaxDate;

        public long TotalTime = 0;
        public int Count = 0;

        public double Average
        {
            get { return (TotalTime / Count); }
        }

        public override string ToString()
        {
            return "Last: {0}ms, Min: {1}ms, Avg: {2}ms, Max: {3}ms, Count: {4}".FormatWith(
                LastTime, MinTime, Average, MaxTime, Count);
        }
    }
}
