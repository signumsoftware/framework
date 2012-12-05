using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Xml.Linq;
using System.Text.RegularExpressions;

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
        public static TimeSpan MaxEnabledTime = TimeSpan.FromMinutes(5);

        public static long? TimeLimit;

        public static int MaxTotalEntriesCount = 1000;


        static bool enabled;
        public static bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;

                if (value)
                {
                    TimeLimit = PerfCounter.Ticks + PerfCounter.FrequencyMilliseconds * (long)MaxEnabledTime.TotalMilliseconds;
                }
                else
                {
                    TimeLimit = null;
                }
            }
        }

        static readonly Variable<HeavyProfilerEntry> current = Statics.ThreadVariable<HeavyProfilerEntry>("heavy"); 

        public static readonly List<HeavyProfilerEntry> Entries = new List<HeavyProfilerEntry>();

        public static void Clean()
        {
            lock (Entries)
            {
                Entries.Clear();
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
            if (!enabled)
                return null;

            var tracer = CreateNewEntry(role, aditionalData, true);

            return tracer;
        }

        public static T LogValue<T>(string role, Func<T> valueFactory)
        {
            if (!enabled)
                return valueFactory();

            using (HeavyProfiler.LogNoStackTrace(role))
            {
                return valueFactory();
            }
        }

        public static Tracer LogNoStackTrace(string role)
        {
            if (!enabled)
                return null;

            var tracer = CreateNewEntry(role, null, false);

            return tracer;
        }

        private static Tracer CreateNewEntry(string role, string aditionalData, bool stackTrace)
        {
            long beforeStart = PerfCounter.Ticks;

            if (TimeLimit.Value < beforeStart)
            {
                Enabled = false;
                Clean();
                return null;
            }

            var saveCurrent = current.Value;

            if (aditionalData != null)
                aditionalData = string.Intern((string)aditionalData);

            var newCurrent = current.Value = new HeavyProfilerEntry()
            {
                BeforeStart = beforeStart,
                Role = role,
                AditionalData = aditionalData,
                StackTrace = stackTrace ? new StackTrace(2, true) : null,
            };

            newCurrent.Start = PerfCounter.Ticks;

            return new Tracer { saveCurrent = saveCurrent };
        }

        public class Tracer : IDisposable
        {
            internal HeavyProfilerEntry saveCurrent;

            public void Dispose()
            {
                var cur = current.Value;
                cur.End = PerfCounter.Ticks;

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

            bool hasStackTrace = current.Value.StackTrace != null;

            tracer.Dispose();

            var newTracer = CreateNewEntry(role, aditionalData, hasStackTrace);

            if (newTracer != null)
            {
                tracer.saveCurrent = newTracer.saveCurrent; 
            }
        }

        public static IEnumerable<HeavyProfilerEntry> AllEntries()
        {
            List<HeavyProfilerEntry> result = new List<HeavyProfilerEntry>();
            foreach (var item in Entries)
            {
                result.Add(item);
                item.FillDescendants(result);
            }
            return result;
        }

        public static XDocument FullXDocument()
        {
            TimeSpan timeSpan = new TimeSpan(Entries.Sum(a => a.Elapsed.Ticks));

            return new XDocument(
                new XElement("Logs", new XAttribute("TotalTime", timeSpan.NiceToString()),
                    Entries.Select(e => e.FullXml(timeSpan.TotalMilliseconds)))); 
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

    [Serializable]
    public class HeavyProfilerEntry
    {
        public List<HeavyProfilerEntry> Entries;
        public HeavyProfilerEntry Parent; 
        public string Role;

        public int Index;

        public int Depth
        {
            get { return this.Parent == null ? 0 : this.Parent.Depth + 1; }
        }

        public string FullIndex()
        {
            return this.FollowC(a => a.Parent).Reverse().ToString(a => a.Index.ToString(), ".");
        }

        public string AditionalData;
        public string AditionalDataPreview()
        {
            if (string.IsNullOrEmpty(AditionalData))
                return "";

            return Regex.Match(AditionalData, @"^[^\r\n]{0,100}").Value;
        }

        public long BeforeStart;
        public long Start;
        public long End; 

        public StackTrace StackTrace;

        public TimeSpan Elapsed
        {
            get
            {
                return TimeSpan.FromMilliseconds(ElapsedMilliseconds);
            }
        }

        public double ElapsedMilliseconds
        {
            get
            {
                return ((End - Start) - Descendants().Sum(a => a.BeforeStart - a.Start)) / (double)PerfCounter.FrequencyMilliseconds;
            }
        }

        public IEnumerable<HeavyProfilerEntry> Descendants()
        {
            var result = new List<HeavyProfilerEntry>();
            FillDescendants(result);
            return result;
        }

        public IEnumerable<HeavyProfilerEntry> DescendantsAndSelf()
        {
            var result = new List<HeavyProfilerEntry>();
            result.Add(this);
            FillDescendants(result);
            return result;
        }

        public void FillDescendants(List<HeavyProfilerEntry> list)
        {
            if (Entries != null)
            {
                foreach (var item in Entries)
                {
                    list.Add(item);
                    item.FillDescendants(list);
                }
            }
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(Elapsed.NiceToString(), Role);
        }

        public XElement FullXml(double elapsedParent)
        {
            var elapsedMs = ElapsedMilliseconds; 

            return new XElement("Log",
                new XAttribute("Time", elapsedMs < 10 ? "{0:.00}ms".Formato(elapsedMs): TimeSpan.FromMilliseconds(elapsedMs).NiceToString()),
                new XAttribute("Ratio", "{0:p}".Formato(elapsedMs / elapsedParent)),
                new XAttribute("Role", Role ?? ""),
                new XAttribute("FullIndex", this.FullIndex()),
                new XAttribute("Data", (AditionalData.TryToString() ?? "").TryLeft(100)),
                Entries == null ? new XElement[0] : Entries.Select(e => e.FullXml(elapsedMs))
                );
        }

        public void CleanStackTrace()
        {
            this.StackTrace = null;
            if (this.Entries != null)
                foreach (var item in this.Entries)
                    item.CleanStackTrace();
        }
    }

    public class PerfCounter
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

        public static long ToMilliseconds(long t1, long t2)
        {
            return (t2 - t1) / FrequencyMilliseconds;
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
}
