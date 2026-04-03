using System.Diagnostics;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Signum.Utilities.DataStructures;
using Microsoft.Extensions.Logging;

namespace Signum.Utilities;

public static class HeavyProfiler
{
    public static TimeSpan MaxEnabledTime = TimeSpan.FromMinutes(5);

    public static long? TimeLimit;

    public static ILoggerFactory? LoggerFactory;
    public static ActivitySource? ActivitySource;


    public static bool FullyDisabled => !(Enabled || ActivitySource != null);


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

    static readonly Variable<HeavyProfilerEntry?> current = Statics.ThreadVariable<HeavyProfilerEntry?>("heavy"); 

    public static readonly List<HeavyProfilerEntry> Entries = new List<HeavyProfilerEntry>();

    public static void Clean()
    {
        lock (Entries)
        {
            Entries.Clear();
        }
    }

    public static Tracer? Log(string kind, LogLevel? logLevel = LogLevel.Information)
    {
        if (FullyDisabled)
            return null;

        var tracer = CreateNewTracer(kind, null, null, stackTrace: true, logLevel);

        return tracer;
    }

    public static Tracer? Log(string kind, Func<string?> additionalData, LogLevel logLevel = LogLevel.Information)
    {
        if (FullyDisabled)
            return null;

        var tracer = CreateNewTracer(kind, null, additionalData, stackTrace: true, logLevel);

        return tracer;
    }

    public static Tracer? Log(string kind, StructuredLogMessage logMessage, LogLevel logLevel = LogLevel.Information)
    {
        if (FullyDisabled)
            return null;

        var tracer = CreateNewTracer(kind, logMessage, null, stackTrace: true, logLevel);

        return tracer;
    }

    public static Tracer? LogNoStackTrace(string kind, LogLevel? logLevel = null)
    {
        if (FullyDisabled)
            return null;

        var tracer = CreateNewTracer(kind, null, null, stackTrace: false, logLevel);

        return tracer;
    }

    public static Tracer? LogNoStackTrace(string kind, Func<string?> additionalData, LogLevel? logLevel = null)
    {
        if (FullyDisabled)
            return null;

        var tracer = CreateNewTracer(kind, null, additionalData, stackTrace: false, logLevel);

        return tracer;
    }

    private static void AutoStop(long beforeStart)
    {
        if (enabled && TimeLimit < beforeStart)
        {
            enabled = false;
            Clean();
        }
    }

    private static Tracer? CreateNewTracer(string kind, StructuredLogMessage? logMessage, Func<string?>? additonalData, bool stackTrace, LogLevel? logLevel)
    {
        long beforeStart = PerfCounter.Ticks;

        AutoStop(beforeStart);

        StructuredLogMessage? GetLogMessage()
        {
            if (logMessage != null)
                return logMessage;

            var str = additonalData?.Invoke();

            if (str != null)
                return (logMessage = new StructuredLogMessage(str));

            return null;
        }

        HeavyProfilerEntry? entry = enabled ? CreateEntry(kind, GetLogMessage()?.ToString(), stackTrace, beforeStart) : null;

        Activity? activity = null;
        if(ActivitySource != null && LoggerFactory != null && logLevel != null)
        {
            var logger = LoggerFactory.CreateLogger(kind);

            if(logger.IsEnabled(logLevel.Value))
            {
                activity = ActivitySource.CreateActivity("Signum." + kind, ActivityKind.Internal);
                activity?.Start();

                if(activity != null)
                {
                    var message = GetLogMessage();
                    if (message != null)
                    {
                        if (message.Value.Arguments != null)
                            logger.Log(logLevel.Value, message.Value.Message, message.Value.Arguments);
                        else
                            logger.Log(logLevel.Value, message.Value.Message);
                    }
                }
            }
        }

        if (entry == null && activity == null)
            return null;

        return new Tracer(entry, activity, logLevel);
    }

    private static HeavyProfilerEntry CreateEntry(string kind, string? additionalData, bool stackTrace, long beforeStart)
    {
        var parent = current.Value;

        var newCurrent = current.Value = new HeavyProfilerEntry()
        {
            BeforeStart = beforeStart,
            Kind = kind,
            AdditionalData = additionalData,
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

        return newCurrent;
    }

 
    

    public sealed class Tracer : IDisposable
    {
        internal HeavyProfilerEntry? entry;
        public Activity? activity;
        internal LogLevel? logLevel;

        public Tracer(HeavyProfilerEntry? entry, Activity? activity, LogLevel? logLevel)
        {
            this.entry = entry;
            this.activity = activity;
            this.logLevel = logLevel;
        }

        public void Dispose()
        {
            try
            {
                if (entry != null)
                {

                    if (current.Value != entry)
                        throw new InvalidOperationException("Unexpected");

                    var cur = entry;
                    cur.End = PerfCounter.Ticks;
                    var parent = entry.Parent;

                    current.Value = parent;
                }
            }
            finally
            {
                activity?.Dispose();
            }
        }
    }


    public static void Switch(this Tracer? tracer, string kind, StructuredLogMessage logMessage)
    {
        if (tracer == null)
            return;

        bool hasStackTrace = tracer.entry?.StackTrace != null;

        tracer.Dispose();

        var newTracer = CreateNewTracer(kind, logMessage, null, hasStackTrace, tracer.logLevel);

        tracer.entry = newTracer?.entry;
        tracer.activity = newTracer?.activity;
    }

    public static void Switch(this Tracer? tracer, string kind, Func<string>? additionalData = null)
    {
        if(tracer == null) 
            return;


        bool hasStackTrace = tracer.entry?.StackTrace != null;

        tracer.Dispose();

        var newTracer = CreateNewTracer(kind, null, additionalData, hasStackTrace, tracer.logLevel);

        tracer.entry = newTracer?.entry;
        tracer.activity = newTracer?.activity;
    }

    public static IEnumerable<HeavyProfilerEntry> AllEntries()
    {
        List<HeavyProfilerEntry> result = new List<HeavyProfilerEntry>();
        var count = Entries.Count;
        for (int i = 0; i < count; i++)
        {
            var item = Entries[i];
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
        var list = doc.Element("Logs")!.Elements("Log").Select(xLog => HeavyProfilerEntry.ImportXml(xLog, null)).ToList();

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
        var statistics = AllEntries().Where(a => a.Kind == "SQL").GroupBy(a => a.AdditionalData!).Select(gr =>
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
        var array = fullIndex.Split('-').Select(a => int.Parse(a)).ToArray();

        HeavyProfilerEntry? entry = null;

        List<HeavyProfilerEntry> currentList = Signum.Utilities.HeavyProfiler.Entries;

        for (int i = 0; i < array.Length; i++)
        {
            int index = array[i];

            if (currentList == null || currentList.Count <= index)
                throw new InvalidOperationException("The ProfileEntry is not available");

            entry = currentList[index];

            currentList = entry.Entries;
        }

        return entry!;
    }
}
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class HeavyProfilerEntry
{
    public List<HeavyProfilerEntry> Entries;
    public HeavyProfilerEntry? Parent;
    public string Kind;

    public int Index;

    public int Depth;

    public string FullIndex()
    {
        return this.Follow(a => a.Parent).Reverse().ToString(a => a.Index.ToString(), "-");
    }

    public string? AdditionalData;
    public string AdditionalDataPreview()
    {
        if (string.IsNullOrEmpty(AdditionalData))
            return "";

        return Regex.Match(AdditionalData, @"^[^\n]{0,100}").Value;
    }

    public long BeforeStart;
    public long Start;
    public long? End;
    public long EndOrNow => End ?? PerfCounter.Ticks;

    public StackTrace? StackTrace;
    public List<ExternalStackTrace>? ExternalStackTrace;

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
                var count = Entries.Count;
                for (int i = 0; i < count; i++)
                {
                    var item = Entries[i];
                    list.Add(item);
                    item.FillDescendants(list);
                }
            }
        }
    }

    public override string ToString()
    {
        return "{0} {1}".FormatWith(Elapsed.NiceToString(), Kind);
    }

    public XElement ExportXml(bool includeStackTrace)
    {
        return new XElement("Log",
            new XAttribute("Index", this.Index),
            new XAttribute("Role", this.Kind),
            new XAttribute("BeforeStart", this.BeforeStart),
            new XAttribute("Start", this.Start),
            new XAttribute("End", this.End ?? PerfCounter.Ticks),
            this.AdditionalData == null ? null! :
            new XAttribute("AdditionalData", this.AdditionalData),
             includeStackTrace && StackTrace != null ? StackTraceToXml(StackTrace) : null!,
            Entries?.Select(e => e.ExportXml(includeStackTrace)).ToList()!);
    }

    private XElement StackTraceToXml(StackTrace stackTrace)
    {
        var frames = (from i in 0.To(StackTrace!.FrameCount)
                      let sf = StackTrace!.GetFrame(i)
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
            var parts = a.Attribute("Method")!.Value.Split('.');
            var line = a.Attribute("Line")!.Value;

            return new ExternalStackTrace
            {
                MethodName = parts.LastOrDefault() ?? "",
                Type = parts.ElementAtOrDefault(parts.Length - 2) ?? "",
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

    internal static HeavyProfilerEntry ImportXml(XElement xLog, HeavyProfilerEntry? parent = null)
    {
        var result = new HeavyProfilerEntry
        {
            Parent = parent,
            Index = int.Parse(xLog.Attribute("Index")!.Value),
            Kind = xLog.Attribute("Role")!.Value,
            BeforeStart = long.Parse(xLog.Attribute("BeforeStart")!.Value),
            Start = long.Parse(xLog.Attribute("Start")!.Value),
            End = long.Parse(xLog.Attribute("End")!.Value),
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
    public static readonly long FrequencyMilliseconds;

    static PerfCounter()
    {
        if (!Stopwatch.IsHighResolution)
            throw new InvalidOperationException("Low performance performance counter");

        FrequencyMilliseconds = Stopwatch.Frequency / 1000;
    }

    public static long Ticks
    {
        get
        {
            return Stopwatch.GetTimestamp();
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

public readonly struct StructuredLogMessage
{
    public string Message { get; }
    public object?[]? Arguments { get; }

    public StructuredLogMessage(string format)
    {
        Message = format;
    }
    public StructuredLogMessage(string format, params object?[] arguments)
    {
        Message = format;
        Arguments = arguments;
    }

    public override string ToString()
    {
        var args = this.Arguments;
        if (args.IsNullOrEmpty())
            return this.Message;

        int i = 0;
        return Regex.Replace(this.Message, @"\{(\w+)\}", match =>
        {
            return args[i++]?.ToString() ?? "";
        });
    }
}
