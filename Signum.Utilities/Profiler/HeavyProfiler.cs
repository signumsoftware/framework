using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Signum.Utilities.DataStructures;

namespace Signum.Utilities
{
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

        public static Tracer Log(string role, Func<string> additionalData)
        {
            if (!enabled)
                return null;

            var tracer = CreateNewEntry(role, additionalData, stackTrace: true);

            return tracer;
        }

        public static Tracer LogNoStackTrace(string role)
        {
            if (!enabled)
                return null;

            var tracer = CreateNewEntry(role, null, stackTrace: false);

            return tracer;
        }

        public static Tracer LogNoStackTrace(string role, Func<string> additionalData)
        {
            if (!enabled)
                return null;

            var tracer = CreateNewEntry(role, additionalData, stackTrace: false);

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

        public static T LogValueNoStackTrace<T>(string role, Func<T> valueFactory)
        {
            if (!enabled)
                return valueFactory();

            using (HeavyProfiler.LogNoStackTrace(role))
            {
                return valueFactory();
            }
        }

        private static Tracer CreateNewEntry(string role, Func<string> additionalData, bool stackTrace)
        {
            long beforeStart = PerfCounter.Ticks;
         
            if (enabled == false || TimeLimit.Value < beforeStart)
            {
                enabled = false;
                Clean();
                return null;
            }

            var parent = current.Value;

            var newCurrent = current.Value = new HeavyProfilerEntry()
            {
                BeforeStart = beforeStart,
                Role = role,
                AdditionalData = additionalData == null ? null : additionalData(),
                StackTrace = stackTrace ? new StackTrace(2, true) : null,
                Parent = parent,
                Depth = parent == null ? 0 : (parent.Depth + 1),
            };

            newCurrent.Start = PerfCounter.Ticks;

            if (parent == null)
            {
                lock (Entries)
                {
                    if (newCurrent.Depth != 0)
                        throw new InvalidOperationException("Invalid depth");
                    newCurrent.Index = Entries.Count;
                    Entries.Add(newCurrent);
                }
            }
            else
            {
                if (parent.Entries == null)
                    parent.Entries = new List<HeavyProfilerEntry>();

                if (newCurrent.Depth != parent.Depth + 1)
                    throw new InvalidOperationException("Invalid depth");

                newCurrent.Index = parent.Entries.Count;
                parent.Entries.Add(newCurrent);
            }

            return new Tracer { newCurrent = newCurrent };
        }

        public class Tracer : IDisposable
        {
            internal HeavyProfilerEntry newCurrent;

            public void Dispose()
            {
                if (newCurrent == null) //After a Switch sequence disabled in the middle
                    return;

                if (current.Value != newCurrent)
                    throw new InvalidOperationException("Unexpected");

                var cur = newCurrent;
                cur.End = PerfCounter.Ticks;
                var parent = newCurrent.Parent;
              

                current.Value = parent;
            }
        }

        public static void Switch(this Tracer tracer, string role, Func<string> additionalData = null)
        {
            if (tracer == null)
                return;

            bool hasStackTrace = current.Value.StackTrace != null;

            tracer.Dispose();

            var newTracer = CreateNewEntry(role, additionalData, hasStackTrace);

            tracer.newCurrent = newTracer?.newCurrent;
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

        public static XDocument ExportXml(bool includeStackTrace = false)
        {
            return new XDocument(
                new XElement("Logs", Entries.Select(e => e.ExportXml(includeStackTrace)))
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
            var statistics = AllEntries().Where(a => a.Role == "SQL").GroupBy(a => (string)a.AdditionalData).Select(gr =>
                        new SqlProfileResume
                        {
                            Query = gr.Key,
                            Count = gr.Count(),
                            Sum = TimeSpan.FromMilliseconds(gr.Sum(a => a.ElapsedMilliseconds)),
                            Avg = TimeSpan.FromMilliseconds((long)gr.Average((a => a.ElapsedMilliseconds))),
                            Min = TimeSpan.FromMilliseconds(gr.Min((a => a.ElapsedMilliseconds))),
                            Max = TimeSpan.FromMilliseconds(gr.Max((a => a.ElapsedMilliseconds))),
                            References = gr.Select(a => new SqlProfileReference { FullKey = a.FullIndex(), ElapsedToString = a.ElapsedToString() }).ToList(),
                        }).OrderByDescending(a => a.Sum);
            return statistics;
        }

        public static HeavyProfilerEntry Find(string fullIndex)
        {
            var array = fullIndex.Split('.').Select(a => int.Parse(a)).ToArray();

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

        public int Depth;

        public string FullIndex()
        {
            return this.Follow(a => a.Parent).Reverse().ToString(a => a.Index.ToString(), ".");
        }

        public string AdditionalData;
        public string AdditionalDataPreview()
        {
            if (string.IsNullOrEmpty(AdditionalData))
                return "";

            return Regex.Match(AdditionalData, @"^[^\r\n]{0,100}").Value;
        }

        public long BeforeStart;
        public long Start;
        public long? End;
        public long EndOrNow => End ?? PerfCounter.Ticks;

        public StackTrace StackTrace;
        public List<ExternalStackTrace> ExternalStackTrace;

        public TimeSpan Elapsed
        {
            get
            {
                return TimeSpan.FromMilliseconds(ElapsedMilliseconds);
            }
        }

        public string ElapsedToString()
        {
            var ms = ElapsedMilliseconds;

            if (ms < 10)
                return ms.ToString("0.0000") + "ms";

            return TimeSpan.FromMilliseconds(ms).NiceToString();
        }

        public double ElapsedMilliseconds
        {
            get
            {
                return ((EndOrNow - Start) - Descendants().Sum(a => a.BeforeStart - a.Start)) / (double)PerfCounter.FrequencyMilliseconds;
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
            var result = new List<HeavyProfilerEntry> { this };
            FillDescendants(result);
            return result;
        }

        public void FillDescendants(List<HeavyProfilerEntry> list)
        {
            if (Entries != null)
            {
                lock (Entries)
                {
                    foreach (var item in Entries)
                    {
                        list.Add(item);
                        item.FillDescendants(list);
                    }
                }
            }
        }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(Elapsed.NiceToString(), Role);
        }

        public XElement ExportXml(bool includeStackTrace)
        {
            return new XElement("Log",
                new XAttribute("Index", this.Index),
                new XAttribute("Role", this.Role),
                new XAttribute("BeforeStart", this.BeforeStart),
                new XAttribute("Start", this.Start),
                new XAttribute("End", this.End ?? PerfCounter.Ticks),
                this.AdditionalData == null ? null :
                new XAttribute("AdditionalData", this.AdditionalData),
                 includeStackTrace && StackTrace != null ? StackTraceToXml(StackTrace) : null,
                Entries?.Select(e => e.ExportXml(includeStackTrace)).ToList());
        }

        private XElement StackTraceToXml(StackTrace stackTrace)
        {
            var frames = (from i in 0.To(StackTrace.FrameCount)
                          let sf = StackTrace.GetFrame(i)
                          let mi = sf.GetMethod()                          
                          select new XElement("StackFrame",
                              new XAttribute("Method", mi.DeclaringType?.FullName + "." + mi.Name),
                              new XAttribute("Line", sf.GetFileName() + ":" + sf.GetFileLineNumber())
                              )).ToList();

            return new XElement("StackTrace", frames);
        }

        private static List<ExternalStackTrace> ExternalStackTraceFromXml(XElement st)
        {
            return st.Elements("StackFrame").Select(a =>
            {
                var parts = a.Attribute("Method").Value.Split('.');
                var line = a.Attribute("Line").Value;

                return new ExternalStackTrace
                {
                    MethodName = parts.LastOrDefault(),
                    Type = parts.ElementAtOrDefault(parts.Length - 2),
                    Namespace = parts.Take(parts.Length - 2).ToString("."),
                    FileName = line.BeforeLast(":"),
                    LineNumber = line.AfterLast(":").ToInt(),
                };
            }).ToList();
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
                AdditionalData = xLog.Attribute("AdditionalData")?.Value,
                Depth = parent == null ? 0 : parent.Depth + 1
            };

            if (xLog.Element("StackTrace") is XElement st)
                result.ExternalStackTrace = ExternalStackTraceFromXml(st);

            if (xLog.Element("Log") != null)
                result.Entries = xLog.Elements("Log").Select(x => ImportXml(x, result)).ToList();
         
            return result;
        }

        public XDocument ExportXmlDocument(bool includeStackTrace)
        {
            return new XDocument(
                 new XElement("Logs", ExportXml(includeStackTrace))
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

        public bool Overlaps(HeavyProfilerEntry e)
        {
            return new Interval<long>(this.BeforeStart, this.EndOrNow)
                .Overlaps(new Interval<long>(e.BeforeStart, e.EndOrNow));
        }
    }

    public class ExternalStackTrace
    {
        public string Namespace;
        public string Type;
        public string MethodName;
        public string FileName;
        public int? LineNumber;
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
            if (!QueryPerformanceFrequency(out long freq))
                throw new InvalidOperationException("Low performance performance counter");

            FrequencyMilliseconds = freq / 1000; 
        }

        public static long Ticks
        {
            get
            {
                QueryPerformanceCounter(out long count);
                return count;
            }
        }

        public static long ToMilliseconds(long start, long end)
        {
            return (end - start) / FrequencyMilliseconds;
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
        public string ElapsedToString;
    }
}
