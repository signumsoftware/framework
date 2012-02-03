using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Xml.Linq;

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

        static readonly Variable<HeavyProfilerEntry> current = Statics.ThreadVariable<HeavyProfilerEntry>("heavy"); 

        public static readonly List<HeavyProfilerEntry> Entries = new List<HeavyProfilerEntry>();

        public static void Clean()
        {
            lock (Entries)
            {
                Entries.Clear();
                TotalEntriesCount = 0;
            }
        }

        public static Tracer Log(string role, Func<string> aditionalData)
        {
            if (!Enabled)
                return null;

            return Log(role, aditionalData());
        }

        public static Tracer Log(string role = null, string aditionalData = null)
        {
            if (!Enabled)
                return null;

            if (TotalEntriesCount > MaxTotalEntriesCount)
            {
                Enabled = false;
                return null;
            }

            var saveCurrent = CreateNewEntry(role, aditionalData);

            return new Tracer { saveCurrent = saveCurrent };
        }

        private static HeavyProfilerEntry CreateNewEntry(string role, string aditionalData)
        {
            long beforeStart = PerfCounter.Ticks;


            var saveCurrent = current.Value;

            if (aditionalData != null)
                aditionalData = string.Intern((string)aditionalData);

            var newCurrent = current.Value = new HeavyProfilerEntry()
            {
                BeforStart = beforeStart,
                Role = role,
                AditionalData = aditionalData,
                StackTrace = new StackTrace(2, true),
            };

            newCurrent.Start = PerfCounter.Ticks;

            return saveCurrent;
        }

        public class Tracer : IDisposable
        {
            internal HeavyProfilerEntry saveCurrent;

            public void Dispose()
            {
                var cur = current.Value;
                cur.End = PerfCounter.Ticks;

                TotalEntriesCount++;

                if (saveCurrent == null)
                {
                    lock (Entries)
                    {
                        cur.Index = Entries.Count;
                        cur.Parent = null;
                        Entries.Add(cur);
                    }
                }
                else
                {
                    if (saveCurrent.Entries == null)
                        saveCurrent.Entries = new List<HeavyProfilerEntry>();

                    cur.Index = saveCurrent.Entries.Count;
                    cur.Parent = saveCurrent;
                    saveCurrent.Entries.Add(cur);
                }

                current.Value = saveCurrent;
            }
        }

        public static void Switch(this Tracer tracer, string role, Func<string> aditionalData)
        {
            if (tracer == null)
                return;

            tracer.Switch(role, aditionalData()); 
        }

        public static void Switch(this Tracer tracer, string role = null, string aditionalData = null)
        {
            if (tracer == null)
                return;

            tracer.Dispose();

            tracer.saveCurrent = CreateNewEntry(role, aditionalData); 
        }

        public static void CleanCurrent() //To fix possible non-dispossed ones
        {
            current.Value = null; 
        }

        public static IEnumerable<HeavyProfilerEntry> AllEntries()
        {
            return from pe in Entries
                   from p in pe.Descendants().PreAnd(pe)
                   select p;
        }

        public static XDocument FullXDocument()
        {
            TimeSpan timeSpan = new TimeSpan(Entries.Sum(a => a.Elapsed.Ticks));

            return new XDocument(
                new XElement("Logs", new XAttribute("TotalTime", timeSpan.NiceToString()),
                    Entries.Select(e => e.FullXml(timeSpan)))); 
        }

        public static XDocument SqlStatisticsXDocument()
        {
            var statistics = SqlStatistics();

            return new XDocument(
                new XElement("Sqls",
                        statistics.Select(a => new XElement("Sql",
                            new XAttribute("Sum", a.Sum.NiceToString()),
                            new XAttribute("Count", a.Count),
                            new XAttribute("Avg", a.Avg.NiceToString()),
                            new XAttribute("Min", a.Min.NiceToString()),
                            new XAttribute("Max", a.Max.NiceToString()),
                            new XElement("Query", a.Query),
                            new XElement("References", a.References)))));
        }

        public static IOrderedEnumerable<SqlProfileResume> SqlStatistics()
        {
            var statistics = AllEntries().Where(a => a.Role == "SQL").GroupBy(a => (string)a.AditionalData).Select(gr =>
                        new SqlProfileResume
                        {
                            Query = gr.Key,
                            Count = gr.Count(),
                            Sum = new TimeSpan(gr.Sum(a => a.Elapsed.Ticks)),
                            Avg = new TimeSpan((long)gr.Average((a => a.Elapsed.Ticks))),
                            Min = new TimeSpan(gr.Min((a => a.Elapsed.Ticks))),
                            Max = new TimeSpan(gr.Max((a => a.Elapsed.Ticks))),
                            References = gr.Select(a => new SqlProfileReference { FullKey = a.FullIndex(), Elapsed = a.Elapsed }).ToList(),
                        }).OrderByDescending(a => a.Sum);
            return statistics;
        }


        public static string GetFileLineAndNumber(this StackFrame frame)
        {
            string fileName = frame.GetFileName();

            if (fileName == null)
                return null;

            if (fileName.Length > 70)
                fileName = "..." + fileName.TryRight(67);

            return fileName + " ({0})".Formato(frame.GetFileLineNumber());
        }
        

        public static HeavyProfilerEntry Find(string indices)
        {
            var array = indices.Split('.').Select(a => int.Parse(a)).ToArray();

            HeavyProfilerEntry entry = null;

            List<HeavyProfilerEntry> currentList = Signum.Utilities.HeavyProfiler.Entries;

            for (int i = 0; i < array.Length; i++)
            {
                int index = array[i];

                if (currentList == null || currentList.Count <= index)
                    throw new InvalidOperationException("The ProfileEntry is not available");

                entry = currentList[index];

                currentList = entry.Entries;
            }

            return entry;
        }
    }

    public class HeavyProfilerEntry
    {
        public List<HeavyProfilerEntry> Entries;
        public HeavyProfilerEntry Parent; 
        public string Role;

        public int Index;

        public string FullIndex()
        {
            return this.FollowC(a => a.Parent).Reverse().ToString(a => a.Index.ToString(), ".");
        }

        public string AditionalData;

        public long BeforStart;
        public long Start;
        public long End; 

        public StackTrace StackTrace;

        public TimeSpan Elapsed
        {
            get
            {
                return TimeSpan.FromMilliseconds(((End - Start) - Descendants().Sum(a => a.BeforStart - a.Start)) / PerfCounter.FrequencyMilliseconds);
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

        public override string ToString()
        {
            string basic = "{0} {1}".Formato(Elapsed.NiceToString(), Role);

            if (Entries == null)
                return basic;

            return basic + " --> (" +
                    GetDescendantRoles().ToString(kvp => "{0} {1}".Formato(kvp.Key, kvp.Value.ToString(this)), " | ")
                    + ")";
        }

        public XElement FullXml(TimeSpan elapsedParent)
        {
            return new XElement("Log",
                new XAttribute("Ratio", "{0:p}".Formato(this.Elapsed.Ticks / (double)elapsedParent.Ticks)),
                new XAttribute("Time", Elapsed.NiceToString()),
                new XAttribute("Role", Role ?? ""),
                new XAttribute("Data", (AditionalData.TryToString() ?? "").TryLeft(100)),
                new XAttribute("FullIndex", this.FullIndex()),
                new XAttribute("Resume", GetDescendantRoles().ToString(kvp => "{0} {1}".Formato(kvp.Key, kvp.Value.ToString(this)), " | ")),
                Entries == null ? new XElement[0] : Entries.Select(e => e.FullXml(this.Elapsed))
                );
        }
    }

    class PerfCounter
    {
        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        public static readonly long FrequencyMilliseconds;

        static PerfCounter()
        {
            long freq;
            if (!QueryPerformanceFrequency(out freq))
                throw new InvalidOperationException("Low performance performance counter");

            FrequencyMilliseconds = freq / 1000; 
        }

        public static long Ticks
        {
            get
            {
                long count;
                QueryPerformanceCounter(out count);
                return count;
            }
        }
    }

    public class SqlProfileResume
    {
        public string Query;
        public int Count;
        public TimeSpan Sum;
        public TimeSpan Avg;
        public TimeSpan Min;
        public TimeSpan Max;
        public List<SqlProfileReference> References;
    }

    public class SqlProfileReference
    {
        public string FullKey;
        public TimeSpan Elapsed;
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
            return "{0} {1:00}% ({2})".Formato(Time.NiceToString(), (Time.Ticks * 100.0) / parent.Elapsed.Ticks, Count);
        }
    }
}
