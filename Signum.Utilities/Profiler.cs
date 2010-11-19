using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Signum.Utilities
{
    public static class TimeTracker
    {
        public static bool ShowDebug = false;
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

        public static void InsertEntity(long milliseconds, string identifier)
        {
            lock (IdentifiedElapseds)
            {
                TimeTrackerEntry entry;
                if (!IdentifiedElapseds.TryGetValue(identifier, out entry))
                {
                    entry = new TimeTrackerEntry();
                    IdentifiedElapseds.Add(identifier, entry);
                }

                entry.LastTime = milliseconds;
                entry.LastDate = DateTime.UtcNow;
                entry.TotalTime += milliseconds;
                entry.Count++;

                if (milliseconds < entry.MinTime) { entry.MinTime = milliseconds; entry.MinDate = DateTime.UtcNow; }
                if (milliseconds > entry.MaxTime) { entry.MaxTime = milliseconds; entry.MaxDate = DateTime.UtcNow; }

                if (ShowDebug)
                    Debug.WriteLine(identifier + " - " + entry);
            }
        }
    }

    public class TimeTrackerEntry
    {
        public long LastTime = 0;
        public long TotalTime = 0;
        public int Count = 0;
        public long MinTime = long.MaxValue;
        public long MaxTime = long.MinValue;

        public double Average
        {
            get { return (TotalTime / Count); }
        }

        public DateTime MinDate;
        public DateTime MaxDate;
        public DateTime LastDate;

        public override string ToString()
        {
            return "Last: {0}ms, Min: {1}ms, Avg: {2}ms, Max: {3}ms, Count: {4}".Formato(
                LastTime, MinTime, Average, MaxTime, Count);
        }
    }

    public static class HeavyProfiler
    {
        public static int MaxTotalEntriesCount = 1000;

        public static int TotalEntriesCount { get; private set; }

        public static bool Enabled { get; set; }

        [ThreadStatic]
        static HeavyProfilerEntry current;
        public static readonly List<HeavyProfilerEntry> Entries = new List<HeavyProfilerEntry>();

        public static void Clean()
        {
            lock (Entries)
            {
                Entries.Clear();
                TotalEntriesCount = 0;
            }
        }

        public static IDisposable Log(string role = null, object aditionalData = null)
        {
            if (!Enabled)
                return null;

            if (TotalEntriesCount > MaxTotalEntriesCount)
            {
                Enabled = false;
                return null;
            }

            Stopwatch discount = Stopwatch.StartNew();

            var saveCurrent = current;

            current = new HeavyProfilerEntry()
            {
                Discount = discount,
                Role = role,
                AditionalData = aditionalData,
                StackTrace = new StackTrace(1, true),
            };

            discount.Stop(); 
            
            current.Stopwatch = Stopwatch.StartNew();
            return new Disposable(() =>
            {
                current.Stopwatch.Stop();

                TotalEntriesCount++;

                if (saveCurrent == null)
                {
                    lock (Entries)
                        Entries.Add(current);
                }
                else
                {
                    if (saveCurrent.Entries == null)
                        saveCurrent.Entries = new List<HeavyProfilerEntry>();

                    saveCurrent.Entries.Add(current);
                }

                current = saveCurrent;
            });
        }

        public static void CleanCurrent() //To fix possible non-dispossed ones
        {
            current = null; 
        }

        public static IEnumerable<HeavyProfilerEntry> AllEntries()
        {
            return from pe in Entries
                   from p in pe.Descendants().PreAnd(pe)
                   select p;
        }

        public static string GetFileLineAndNumber(this StackFrame frame)
        {
            string fileName = frame.GetFileName();

            if (fileName == null)
                return null;

            return fileName.Right(50, false) + " ({0})".Formato(frame.GetFileLineNumber());
        }
    }

    public class HeavyProfilerEntry
    {
        public List<HeavyProfilerEntry> Entries;
        public string Role;

        public object AditionalData;

        internal Stopwatch Stopwatch;
        internal Stopwatch Discount;

        public StackTrace StackTrace;

        public TimeSpan Elapsed
        {
            get
            {
                return Stopwatch.Elapsed - new TimeSpan(Descendants().Sum(a => a.Discount.Elapsed.Ticks));
            }
        }
     
        public Type Type { get { return StackTrace.GetFrame(0).GetMethod().TryCC(m=>m.DeclaringType); } }
        public MethodBase Method { get { return StackTrace.GetFrame(0).GetMethod(); } }

        public ProfileResume GetEntriesResume()
        {
            if(Entries == null || Entries.Count == 0)
                return null;

            return new ProfileResume(Entries); 
        }

        public Dictionary<string, ProfileResume> GetDescendantRoles()
        {
            return Descendants()
                .Where(a => a.Role != null)
                .AgGroupToDictionary(a => a.Role, gr => new ProfileResume(gr));
        }

        public IEnumerable<HeavyProfilerEntry> Descendants()
        {
            if (Entries == null)
                return Enumerable.Empty<HeavyProfilerEntry>();

            return from pe in Entries
                   from p in pe.Descendants().PreAnd(pe)
                   select p;
        }
    }

    public class ProfileResume
    {
        int Count;
        TimeSpan Time;

        public ProfileResume(IEnumerable<HeavyProfilerEntry> entries)
        {
            Count = entries.Count();
            Time = new TimeSpan(entries.Sum(a => a.Elapsed.Ticks)); 
        }

        public override string ToString()
        {
            return "{0} ({1})".Formato(Time.NiceToString(), Count);
        }

        public string ToString(HeavyProfilerEntry parent)
        {
            return "{0} {1:00}% ({2})".Formato(Time.NiceToString(), (Time.Ticks * 100.0) / parent.Stopwatch.Elapsed.Ticks,  Count);
        }
    }
}
