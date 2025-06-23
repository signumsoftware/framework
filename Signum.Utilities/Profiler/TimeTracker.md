# TimeTracker

This class records aggregate performance information of actions in a dictionary. 

```C#
public static class TimeTracker
{
   public static Dictionary<string, TimeTrackerEntry> IdentifiedElapseds;
 

   public static T Start<T>(string identifier, Func<T> func)
   public static IDisposable Start(string identifier)

   public static string GetTableString()
}
```

Just one `TimeTrackerEntry` will be stored for each different `identifier`, so **as long as they are stable** is  is safe to use `TimeTracker` in production scenarios.  

```C#
public class TimeTrackerEntry
{
    public long LastTime = 0;
    public DateTime LastDate;

    public long MinTime;
    public DateTime MinDate;
    
    public long MaxTime;
    public DateTime MaxDate;

    public long TotalTime;
    public int Count;

    public double Average {get;}

    public override string ToString()
    {
        return "Last: {0}ms, Min: {1}ms, Avg: {2}ms, Max: {3}ms, Count: {4}".FormatWith(
            LastTime, MinTime, Average, MaxTime, Count);
    }
}
```

