## HeavyProfiler

Intensive profiler that stores detailed information of an execution (accurate times, parent-child relationships, stack-Traces and additional data like SQL commands) by strategically instrumenting your code on operations that are generally used, could take a considerable amount of time (> 15ms?) and are easily recognized by the developer. Example: 

* SQL Query
* LINQ Translation steps
* Save method
* Operation executions
* Asp.Net MVC
* WCF Operation
* ... 


### Enabled and MaxEnabledTime

```C#
public static class HeavyProfiler
{
    public static TimeSpan MaxEnabledTime = TimeSpan.FromMinutes(5);
    public static bool Enabled {get; set;}
}
```

In order to activate the profiler and start recording information your set `Enable` static property to true. 

`HeavyProfiler` can affect performance and, more important, could eat all your memory. For this reason, when enabled, it's automatically disabled after a period of time (default 5 mins). 

### Log and LogNoStatTrace


```C#
public static class HeavyProfiler
{    
    public static Tracer Log(string role);
    public static Tracer Log(string role, Func<string> additionalData);

    public static Tracer LogNoStackTrace(string role);
    public static Tracer LogNoStackTrace(string role, Func<string> additionalData)
}
```

Using any of this methods you create a HeavyProfiler Trace. 

Trances measure the time from when they where created, untill they are disposed. Traces can be neasted and they are automatically embeded in their parent Trace using thread variables.

All traces have a `string role`, a small identified (`SQL`, `Database`, `WCFOperation`...) that will also determine the color, so they have to be stable. 

Also, a funcion generating a `string additionalData` can be provided. This string will be only generated when `HeavyProfiler` is enabled, and can contain additional information that is costly to generate (i.e.: The generated SQL). 

Because taking a StackTrace take a significant amount of time, there are higher-performance alternatives to `Log` that do not take a StackTrace when invoked. You can use `LogNoStackTrace` for fine-grained Traces where the context is clear.


Example: 

```C#
using(HeavyProfiler.Log("SlowMethod"))
{
    var val = SlowMethod();
}
```

Or using `LogStackTrace` and `Using` extension method over `IDisposable`.

```C#
var val = HeavyProfiler.LogNoStackTrace("CalculateA").Using(_ => CalculateA()) +
          HeavyProfiler.LogNoStackTrace("CalculateB").Using(_ => CalculateB());
```

### Switch

```C#
public static class HeavyProfiler
{
    public static void Switch(this Tracer tracer, string role, Func<string> additionalData = null)
}
```

Sometimes having Traces with parent-child in using statements is cumbersome and we need a way to _switch_ from one task to another, disposing one trace and starting a new one right away. `Switch` method in `Trace` does exactly that. 



Example: 

```C#
using(HeavyProfiler.Log("FullLunch"))
using(var t = HeavyProfiler.LogNoStackTrace("FirstCourse"))
{
   EatFirstCourse(); 

   t.Switch("SecondCourse");

   EatSecondCourse();

   t.Switch("Dessert");

   EatDesert();  
}
```

### Entries and AllEntries

```C#
public static class HeavyProfiler
{
    public static readonly List<HeavyProfilerEntry> Entries;
 
    public static IEnumerable<HeavyProfilerEntry> AllEntries()
        
}
```

While `Entries` list stores the top-level entry, `AllEntries` method returns all the entries logged, including roots, children and recursive children.


This is the content of each `HeavyProfilerEntry`. 

```C#
[Serializable]
public class HeavyProfilerEntry
{
    public List<HeavyProfilerEntry> Entries; //Children entries
    public HeavyProfilerEntry Parent; //Parent entry
    public int Depth { get; } //0 for a root, indicates how many recursive parents it has. 


    public string Role; //Stable string indicating the type of action (SQL, DB, Operation...)
    public string AditionalData; // Includes aditional data in the entry (i.e.: SQL query string)

    public int Index; //Zero-ased auto-numeric index. 0 for the first child
    public string FullIndex(); //Sequence of Indexes separated by dot that identify a child in the tree. 

    public long BeforeStart; //Before profiling time (i.e.: StackTrace)
    public long Start; //Before executing the action but after the stack trace
    public long End; //After the action is executed

    //(End - Start) discounting (BeforeStart - Start) for all descendants;
    public double ElapsedMilliseconds { get; }
}
```

### Clean

Removes all the recorded (or imported) entries. 

```C#
public static class HeavyProfiler
{
    public static void Clean();
}
``` 

### Find

Finds a `HeavyProfilerEntry` by a sequence of indices separated by dot (as returned by FullIndex). 

```C#
public static class HeavyProfiler
{
    public static HeavyProfilerEntry Find(string fullIndex)
}
``` 

### ImportEntries

```C#
public static class HeavyProfiler
{
     public static void ImportEntries(List<HeavyProfilerEntry> list, bool rebaseTime);
}
```

ImportEntries allows adding entries recorded in another machine (typically a Windows client application) to compare it with the server. 

The imported entries have to be re-indexed in the server to have a unique index and allow interactive exploration. 

More important, `rebaseTime` parameter allows converting high-performance ticks from the client computer  (export) to the server (import), taking the first log on each side as the reference. This parameter only has effect when the server already has some entries. 

Finally, in order to send the entries from the client to the server, StackTrances have to be removed since they're not serializable. 

```C#
[Serializable]
public class HeavyProfilerEntry
{
    public void CleanStackTrace();
}
```

### ExportXml and ImportXml

Allows exporting and importing entries using a custom XML format. 

While `ImportEntries` is useful using a WCF web service and serializing entries of two running processes (client and server), `ExportXml` and `ImportXml` are useful for comparing executions before and after modifying the code. 

```C#
public static class HeavyProfiler
{
    public static XDocument ExportXml();
    public static void ImportXml(XDocument doc, bool rebaseTime)        
}


[Serializable]
public class HeavyProfilerEntry
{
    public XElement ExportXml(); 
    internal static HeavyProfilerEntry ImportXml(XElement xLog, HeavyProfilerEntry parent);
}
```

### SqlStatistics and SqlStatisticsXDocument

This methods returns statistics for the SQL queries that have been generated, grouped by the query itself.    

```C#
public static class HeavyProfiler
{
    public static IOrderedEnumerable<SqlProfileResume> SqlStatistics()
    public static XDocument SqlStatisticsXDocument()     
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
```
