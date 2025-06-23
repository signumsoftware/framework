
# TimeSpanExtensions

### Trim

Helper methods to truncate time spans

```c#
public static TimeSpan TrimToSeconds(this TimeSpan time)
public static TimeSpan TrimToMinutes(this TimeSpan time)
public static TimeSpan TrimToHours(this TimeSpan time)
public static TimeSpan TrimToDays(this TimeSpan time)
public static TimeSpan TrimTo(this TimeSpan time, TimeSpanPrecision precision)
public static DateTimePrecision? GetPrecision(this TimeSpan timeSpan)
```


With

```c#
public enum DateTimePrecision
{
    Days,
    Hours,
    Minutes,
    Seconds,
    Milliseconds,
}
```

Note that GetPrecision can return null for TimeSpan when is zero. While the same doesn't happen to DateTime because there's no zero date. 


### NiceToString

Returns a compact but stable ToString for a TimeSpan. 

```c#
public static string NiceToString(this TimeSpan timeSpan)
public static string NiceToString(this TimeSpan timeSpan, DateTimePrecision precission)
```

Possible return values are "1d 2h 34m 5s ", "2d", "54s", etc.. depending the DateTimePrecission and the first non-zero time unit. 


### ToShortString

Returns a compact but stable ToString for a TimeSpan. 

```c#
public static string ToShortString(this TimeSpan ts)
```

A possible return values is "1 Day, 2 Minutes, 5 seconds".

