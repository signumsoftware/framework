ListExtensions class
--------------

### Extract

Extracts to a new List all the elements that satisfy a condition,
removing them from the original list.

```c#
public static List<T> Extract<T>(this IList<T> list, Func<T, bool> condition)
```

Example:

```c#
var nums = new List<int> { 1, 2, 3, 4 };
var odds = nums.Extract(n => n % 2 == 0);
//nums = new List<int> { 1, 3 }
//odds = new List<int> { 2, 4 }
```

### Sort

An in-place list Sort (as opposed to [OrderBy](http://msdn.microsoft.com/en-us/library/system.linq.enumerable.orderby.aspx)) that takes a lambda to the
field instead of a `IComparer<T>` or `Comparison<T>`.

```c#
public static void Sort<T, A>(this List<T> list, Func<T, A> keySelector)
```

Example:

```c#
var nums = new List<DayOfWeek> 
{  
  DayOfWeek.Monday,
  DayOfWeek.Friday,
  DayOfWeek.Saturday, 
  DayOfWeek.Tuesday
};

nums.Sort(a => a.ToString()); 

//nums is now shorted alphabetically:
//new List<DayOfWeek> 
//{  
//  DayOfWeek.Friday, 
//  DayOfWeek.Monday,
//  DayOfWeek.Saturday, 
//  DayOfWeek.Tuesday
//};
```

### RemoveAll

Removes all the elements that satisfy a given condition (works over any
IList):

```c#
public static void RemoveAll<T>(this IList<T> list, Func<T, bool> condition)
```

Example:

```c#
IList<int> nums = new List<int> { 1, 2, 3, 4, 5, 6 };
nums.RemoveAll(a => a % 3 == 0); 
//nums = new List<int> { 1, 2, 4, 5 };
```

### AddRange

Adds many items in one operation (works over any IList):

```c#
public static void AddRange<T>(this IList<T> list, IEnumerable<T> elements)
public static void AddRange<T>(this IList<T> list, params T[] elements)
```

Example:

```c#
IList<int> nums = new List<int> { 1, 2 };
nums.AddRange(3,4);
nums.AddRange(6.To(10)); 
//nums = new List<int> { 1, 2, 3, 4, 6, 7, 8, 9 };
```

### AddAllRanges

Adds all items from all ranges in one operation (works over any IList):

```c#
public static void AddAllRanges<T>(this IList<T> list, params IEnumerable<T>[] ranges)
```

Example:

```c#
IList<int> nums = new List<int> { 1, 2 };
IList<int> range1 = new List<int> { 3, 4 };
IList<int> range2 = new List<int> { 5, 6 };
nums.AddAllRanges(range1, range2);
//nums = new List<int> { 1, 2, 3, 4, 6 };
```