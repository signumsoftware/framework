
# DateTimeExtensions

### IsInInterval, IsInDateInterval

Returns whether a date is in the interval [MinDate, MaxDate[ (max date not included). As usual it follows a C-interval convention.

```c#
public static bool IsInInterval(this DateTime date, DateTime minDate, DateTime maxDate)
{
   return minDate <= date && date < maxDate;
}
public static bool IsInInterval(this DateTime date, DateTime minDate, DateTime? maxDate)
{
   return minDate <= date && (maxDate == null || date < maxDate);
}
public static bool IsInInterval(this DateTime date, DateTime? minDate, DateTime? maxDate)
{
   return (minDate == null || minDate <= date) && 
          (maxDate == null || date < maxDate); 
}
```

Similar but also asserts that all the three arguments have no time part
(hours minutes and seconds).

```c#
public static bool IsInDateInterval(this DateTime date, DateTime minDate, DateTime maxDate)
public static bool IsInDateInterval(this DateTime date, DateTime minDate, DateTime? maxDate)
public static bool IsInDateInterval(this DateTime date, DateTime? minDate, DateTime? maxDate)
```

### YearTo

Returns how many whole years there are between dates (Not year
transitions):

```c#
public static int YearsTo(this DateTime min, DateTime max)
```

It does not takes hours, minutes ... into account.

Example:

```c#
new DateTime(1999, 12, 31).YearsTo(new DateTime(2000, 1, 1)); //Returns 0, no whole year
new DateTime(1999, 12, 31).YearsTo(new DateTime(2001, 1, 1)); //Returns 1
```

### MonthsTo

Returns how many whole months there are between Dates (Calendar months,
not Days divided by something between 28 and 31, or month transitions).

```c#
public static int MonthsTo(this DateTime min, DateTime max)
```

It does not takes hours, minutes ... into account.

Example:

```c#
new DateTime(2009, 2, 4).MonthsTo(new DateTime(2009, 3, 3)); //Returns 0
new DateTime(2009, 2, 4).MonthsTo(new DateTime(2009, 3, 4)); //Returns 1
```

### DateSpanTo and Add(DateSpan)

DateSpan is a calendar-relative version of TimeSpan for big time spans
(Years, Months, and Days). That means that it's not possible to be
transformed to TimeSpan without a calendar context.

```c#
public struct DateSpan
{ 
    public readonly int Years;
    public readonly int Months;
    public readonly int Days;

    public DateSpan(int years, int months, int days){...}
    public static DateSpan FromToDates(DateTime min, DateTime max){...}
    public DateTime AddTo(DateTime date){...}
    public DateSpan Invert(){...}
    public override string ToString(){...}
}

public static DateSpan DateSpanTo(this DateTime min, DateTime max)
public static DateTime Add(this DateTime date, DateSpan dateSpan)
```

Example:

```c#
var cer = new DateTime(1547, 9, 29);
var sha = new DateTime(1564, 4, 26);

DateSpan ds1 = cer.DateSpanTo(sha); // 16 Years, 6 Months, 28 Days
DateSpan ds2 = sha.DateSpanTo(cer); // -16 Years, -6 Months, -28 Days

var sha2= cer.Add(ds1);
Console.WriteLine(sha == sha2); //true

var cer2 = sha.Add(ds2);
Console.WriteLine(cer == cer2); //true
```

*Note*: DateSpan.ToString has year(s), month(s) and day(s) correctly pluralized, and avoids writing them when they are zero 

```c#
new DateSpan(0, 1, 0).ToString(); // returns: "1 Month"
```


### Min and Max

Helper methods to compare dates and get the Min or Max. 

```c#
public static DateTime Min(this DateTime a, DateTime b)
public static DateTime Max(this DateTime a, DateTime b)
public static DateTime Min(this DateTime a, DateTime? b)
public static DateTime Max(this DateTime a, DateTime? b)
public static DateTime? Min(this DateTime? a, DateTime? b)
public static DateTime? Max(this DateTime? a, DateTime? b)
```

### Trim

Helper methods to truncate dates

```c#
public static DateTime TrimToSeconds(this DateTime dateTime)
public static DateTime TrimToMinutes(this DateTime dateTime)
public static DateTime TrimToHours(this DateTime dateTime)
public static DateTime TrimTo(this DateTime dateTime, DateTimePrecision precision)
public static DateTimePrecision GetPrecision(this DateTime dateTime)
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

### SmartDatePattern

Returns a human-friendly representation of two date relative to another day (tipically today)

```c#
public static string SmartDatePattern(this DateTime date)
public static string SmartDatePattern(this DateTime date, DateTime currentdate)
```

Possible return values are "Today", "Yesterday", "Last Monday", "May 10th" or "May 10th, 1996", etc.. all localized in the current culture. 


### ToAgoString

Also returns human-friendly dates, but without taking week days into account and with higher precision (up to seconds). 

```c#
public static string ToAgoString(this DateTime dateTime)
public static string ToAgoString(this DateTime dateTime, DateTime now)
```

Possible return values are "1 day ago", "In 3 months", "In 0 seconds", etc..


### MonthStart

Returns the date of the first day of the month. Useful to differentiate November 2014 from November 2013.
This method can be translated to SQL.  

```c#
public static DateTime MonthStart(this DateTime dateTime)
```


### WeekNumber

Returns the week number of a date, using `CalendarWeekRule.FirstDay` and the current culture `DateTimeFormat.FirstDayOfWeek`.
This method can be translated to SQL.  

```c#
public static int WeekNumber(this DateTime dateTime)
```