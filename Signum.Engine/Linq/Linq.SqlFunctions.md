
# LINQ SQL Functions

There are some .Net functions that are supported by Linq to Signum and will be translated to Sql equivalents, so you have compile-time checking and you can forget about what the name of the function in Sql was. The list: 

### String Functions

| .Net Function		| SQL Function
|-------------------|-----------------------
| a.Length	      	| LEN(a)
| a.IndexOf(b)		| CHARINDEX(b, a) -1
| a.IndexOf(b, i)	| CHARINDEX(b, a, i + 1 ) - 1
| a.Trim()		    | LTRIM(RTRIM(a))
| a.TrimStart()		| LTRIM(a)
| a.TrimEnd()		| RTRIM(a)
| a.ToLower()		| LOWER(a)
| a.ToUpper()		| UPPER(a)
| a.TrimStart()		| LTRIM(a)
| a.TrimEnd()		| RTRIM(a)
| a.Replace(b,c)	| REPLACE(a, b, c)
| a.Substring(i,j)	| SUBSTRING(a, i + 1, j)
| a.Contains(b)		| a LIKE '%' + b + '%'
| a.StartWith(b)	| a LIKE b + '%'
| a.EndsWith(b)		| a LIKE '%' + b
| a.Start(i)*		| LEFT( a, i)
| a.End(i)*		    | RIGHT( a, i)
| a.Replicate(n)*	| REPLICATE( a, n )
| a.Reverse()*		| REVERSE(a)
| a.Like(b)*		| a LIKE b
| a.Etc(max, "...")	| a
 

* Extension methods defined in Signum.Utilities. They work in-memory code too. 


### DateTime Functions

| .Net Function		  | Sql function
|---------------------|-----------------
| DateTime.Now		  | GETDATE()
| a.Year			  | YEAR(a)
| a.Month			  | MONTH(a)
| a.Day				  | DAY(a) - 1
| a.DayOfYear		  | DATEPART(dayofyear, a)
| a.Hour			  | DATEPART(hour, a)
| a.Minute			  | DATEPART(minute, a)
| a.Second			  | DATEPART(second, a)
| a.Millisecond		  | DATEPART(millisecond, a)
| a.Date		      | CONVERT(Date, a, 101) 
| a.TimeOfDay		  | CONVERT(Time, bdn.Start)
| a.DayOfWeek		  | DATEPART(weekday, bdn.Start) - 1)
| a.MonthStart()*     | DATEADD(MONTH, DATEDIFF(MONTH, 0, a), 0)
| a.WeekNumber()*     | DATEPART(week, a)
| a.Add(ts)		      | day + ts
| a.Substract(ts)     | day - ts
| a.AddDays(i)		  | DATEADD(day, i, a)
| a.AddHours(i)		  | DATEADD(hour, i, a)
| a.AddMilliseconds(i)|	DATEADD(millisecond, i, a)
| a.AddMinutes(i)	  | DATEADD(minute, i, a)
| a.AddMonths(i)	  | DATEADD(month, i, a)
| a.AddSeconds(i)	  | DATEADD(second, i, a)
| a.AddYears(i)		  | DATEADD(year, i, a)
| a.YearsTo(b)		  | diff =  DATEDIFF(year, a, b); CASE WHEN (DATEADD(year, diff, a) > b) THEN (diff - 1) ELSE diff END
| a.MonthsTo(b)		  | diff =  DATEDIFF(month, a, b); CASE WHEN (DATEADD(month, diff, a) > b) THEN (diff - 1) ELSE diff END
 

* Extension methods defined in Signum.Utilities. They work in-memory code too. 


### TimeSpan Functions

| .Net Function		      | SQL Function
|-------------------------|-----------------
| a.Hour			      | DATEPART(hour, a)
| a.Minute			      | DATEPART(minute, a)
| a.Second			      | DATEPART(second, a)
| a.Millisecond		      | DATEPART(millisecond, a)
| (a-b).TotalDays	      | DATEDIFF(day, b, a)
| (a-b).TotalHours	      | DATEDIFF(hour, b, a)
| (a-b).TotalMilliseconds | DATEDIFF(millisecond, b, a)
| (a-b).TotalSeconds	  | DATEDIFF(second, b, a)
| (a-b).TotalMinutes	  | DATEDIFF(minute, b, a)
 

* Extension methods defined in Signum.Utilities. They work in-memory code too. 

### Math Functions

| .Net Function		  | Sql function
|---------------------|-----------------
| Math.Pi()	          |PI()
| Math.Sign(a)	      |SIGN(a)
| Math.Abs(a)	      |ABS(a)
| Math.Sin(a)	      |SIN(a)
| Math.Asin(a)	      |ASIN(a)
| Math.Cos(a)	      |COS(a)
| Math.Acos(a)	      |ACOS(a)
| Math.Tan(a)	      |TAN(a)
| Math.Atan(a)	      |ATAN(a)
| Math.Atan2(a,b)	  |ATN2(a, b)
| Math.Pow(a,b)	      |POW(a,b)
| Math.Sqrt(a)	      |SQRT(a)
| Math.Exp(a)	      |EXP(a)
| Math.Floor(a)	      |FLOOR(a)
| Math.Log10(a)	      |Log10(a)
| Math.Log(a)	      |Log(a)
| Math.Ceiling(a)	  |CEILING(a)
| Math.Round(a)	      |ROUND(a)
| Math.Truncate(a)	  |Truncate(a)
| decimal.Parse(a)	  |CAST(a as Decimal)
| double.Parse(a)	  |CAST(a as Double)
| float.Parse(a)	  |CAST(a as Float)
| byte.Parse(a)	      |CAST(a as Byte)
| short.Parse(a)	  |CAST(a as Short)
| int.Parse(a)	      |CAST(a as Int) 
| long.Parse(a)	      |CAST(a as Long) 