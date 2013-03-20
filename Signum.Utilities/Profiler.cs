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

        public static Tracer Log(string role)
        {
            if (!enabled)
                return null;

            var tracer = CreateNewEntry(role, null, stackTrace: true);

            return tracer;
        }

        public static Tracer Log(string role, Func<string> aditionalData)
        {
            if (!enabled)
                return null;

            var tracer = CreateNewEntry(role, aditionalData, stackTrace: true);

            return tracer;
        }

        public static Tracer LogNoStackTrace(string role)
        {
            if (!enabled)
                return null;

            var tracer = CreateNewEntry(role, null, stackTrace: false);

            return tracer;
        }

        public static Tracer LogNoStackTrace(string role, Func<string> aditionalData)
        {
            if (!enabled)
                return null;

            var tracer = CreateNewEntry(role, aditionalData, stackTrace: false);

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

        private static Tracer CreateNewEntry(string role, Func<string> aditionalData, bool stackTrace)
        {
            long beforeStart = PerfCounter.Ticks;

            if (enabled == false || TimeLimit.Value < beforeStart)
            {
                enabled = false;
                Clean();
                return null;
            }

            var saveCurrent = current.Value;

            var newCurrent = current.Value = new HeavyProfilerEntry()
            {
                BeforeStart = beforeStart,
                Role = role,
                AditionalData = aditionalData == null ? null: aditionalData(),
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

        public static void Switch(this Tracer tracer, string role, Func<string> aditionalData = null)
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

        public static XDocument ExportXml()
        {
            return new XDocument(
                new XElement("Logs", Entries.Select(e => e.ExportXml()))
                );
        }

        public static void ImportXml(XDocument doc, bool rebaseTime)
        {
            var list = doc.Element("Logs").Elements("Log").Select(xLog => HeavyProfilerEntry.ImportXml(xLog, null)).ToList();

            ImportEntries(list, rebaseTime);
        }

        public static void ImportEntries(List<HeavyProfilerEntry> list, bool rebaseTime)
        {
            if (list.Any())
            {
                lock (Entries)
                {
                    int indexDelta = Entries.Count - list.Min(e => e.Index);
                    foreach (var e in list)
                        e.Index += indexDelta;


                    if (rebaseTime && Entries.Any())
                    {
                        long timeDelta = Entries.Any() ? (Entries.Min(a => a.BeforeStart) - list.Min(a => a.BeforeStart)) : 0;

                        foreach (var e in list)
                            e.ReBaseTime(timeDelta);
                    }

                    Entries.AddRange(list);
                }
            }
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
                fileName = "..." + fileName.TryEnd(67);

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

        public XElement ExportXml()
        {
            return new XElement("Log",
                new XAttribute("Index", this.Index),
                new XAttribute("Role", this.Role),
                new XAttribute("BeforeStart", this.BeforeStart),
                new XAttribute("Start", this.Start),
                new XAttribute("End", this.End),
                this.AditionalData == null ? null :
                new XAttribute("AditionalData", this.AditionalData),
                Entries == null ? null :
                Entries.Select(e => e.ExportXml()).ToList());
        }

        public void CleanStackTrace()
        {
            this.StackTrace = null;
            if (this.Entries != null)
                foreach (var item in this.Entries)
                    item.CleanStackTrace();
        }

        internal static HeavyProfilerEntry ImportXml(XElement xLog, HeavyProfilerEntry parent)
        {
            var result = new HeavyProfilerEntry
            {
                Parent = parent,
                Index = int.Parse(xLog.Attribute("Index").Value),
                Role = xLog.Attribute("Role").Value,
                BeforeStart = long.Parse(xLog.Attribute("BeforeStart").Value),
                Start = long.Parse(xLog.Attribute("Start").Value),
                End = long.Parse(xLog.Attribute("End").Value),
                AditionalData = xLog.Attribute("AditionalData").TryCC(ad => ad.Value),
            };

            if (xLog.Element("Log") != null)
                result.Entries = xLog.Elements("Log").Select(x => ImportXml(x, result)).ToList();
         
            return result;
        }



        public XDocument ExportXmlDocument()
        {
            return new XDocument(
                 new XElement("Logs", ExportXml())
                 );
        }

        internal void ReBaseTime(long timeDelta)
        {
            BeforeStart += timeDelta;
            Start += timeDelta;
            End += timeDelta;

            if (Entries != null)
                foreach (var e in Entries)
                    e.ReBaseTime(timeDelta);
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
