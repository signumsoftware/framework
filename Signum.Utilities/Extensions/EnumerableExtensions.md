# EnumerableExtensions

Contains many simple and useful methods over that should have been
included in Linq.

I'll try to present them to you in an organized way... somehow. Also,
they are really simple so we won't get into the details if necessary:

### Basic Methods

```C#
//Returns true if the collection is empty
public static bool Empty<T>(this IEnumerable<T> collection)

//Removes all null elements in the sequence: Where(a => a != null)
public static IEnumerable<T> NotNull<T>(this IEnumerable<T> source) where T : class
public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source) where T : struct

//As in List<T> but for any IEnumerable
public static int IndexOf<T>(this IEnumerable<T> collection, T item)

//Same but using a condition, not an element
public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> condition)

//Uses your own errorMessage when a InvalidOperationException is thrown. 
public static T Single<T>(this IEnumerable<T> collection, string errorMessage)
public static T SingleOrDefault<T>(this IEnumerable<T> collection, string errorMessage)
public static T First<T>(this IEnumerable<T> collection, string errorMessage)

//Different custom errorMessages for zero and more than one elements. NEW in SF 2.0
public static T Single<T>(this IEnumerable<T> collection, string errorZero, string errorMoreThanOne)
```

### SingleOrMany and Only

**SingleOrMany:** The opposite of SingleOrDefault. Throws an exception
if the collection has no elements, and returns null when there are more
than one.

**Only:** Like Single but never throws exception. Returns null whether
is empty or contains more than one element.

You typically use this two function when you have to ask the server for
the 'default' entity using the new Navigator.FindUnique and, if it
returns null, you need to open a SearchWindow to let the user pick one
option.

```C#
public static T SingleOrMany<T>(this IEnumerable<T> collection)
public static T Only<T>(this IEnumerable<T> collection)
```

A table comparing .Net methods and the new SF ones.

 | Name               |Empty   |One Element  |Many Elements
 | -------------------|--------|-------------|---------------
 | Single             |Throws  |Returns      |Throws
 | SingleOrDefault    |Null    |Returns      |Throws
 | First              |Throws  |Returns      |Returns first
 | FirstOrDefault     |Null    |Returns      |Returns first
 | SingleOrMany (SF)  |Throws  |Returns      |Null
 | Only (SF)          |Null    |Returns      |Null
 
### SingleEx, SingleOrDefaultEx, FirstEx, SingleOrManyEx

This versions of the methods give a more meaningful error than the original version, informing of the name of the type `T` in the `IEnumerable<T>`. 

### And, PreAnd

Allows you to Add one element at the end (And) or at the start (PreAnd)
of a sequence without having to convert it to a List.

```C#
public static IEnumerable<T> And<T>(this IEnumerable<T> collection, T newItem)
public static IEnumerable<T> PreAnd<T>(this IEnumerable<T> collection, T newItem)
```

Example:

```C#
new[]{ 2, 3 }.Add(4).PreAnd(1).ForeEach(a => Console.Write(a)); 
//Result: 1234
```

> **Note:** Use it with care! It has good performance if you're planning to
add just _ONE_ element, for more than one it's really expensive
because the [cost is quadratic with recursive iterators](http://blogs.msdn.com/grantri/archive/2004/03/24/95787.aspx).

### ToString (Very usefull!)

Allows you to concatenate the string representation of some elements
using a separator between each of them, but not at the beginning or the
end.

```C#
public static string ToString<T>(this IEnumerable<T> collection, string separator)
public static string ToString<T>(this IEnumerable<T> collection, Func<T, string> toString, string separator)
```

Example:

```C#
string str = new[] { 1, 2, 3, 4, 5 }.ToString(", ");

//Result: 1, 2, 3, 4, 5
```

You can also use the second overload that takes a lambda to specify how
to do the ToString of each element:

```C#
string[] sortNames = CultureInfo.InvariantCulture.DateTimeFormat.ShortestDayNames; 

string str = new[] { 1, 2, 3, 4, 5 }.ToString(n => ((DayOfWeek)n).ToString(), ", ");

//Result: Monday, Tuesday, Wednesday, Thursday, Friday
```

Internally, this function uses StringBuilder, so it has good performance even
with lot of elements, but using this function is _way sorter_ and
much more composable than using StringBuilder directly.

How Many times you have seen code like this?

```C#
StrinbBuilder sb = new StringBuilder(); 
foreach(int n in new[] { 1, 2, 3, 4, 5 })
{
   if(sb.Length > 0)
      sb.Write(", ");

   sb.Write(n); 
}
return sb.ToString(); 
```

### Comma, CommaAnd, CommaOr

Quite similar to ToString extensions method, but using a different separator between the last two elements: ' and ' (`CommaAnd`), ' or '
(`CommaOr`) or a custom lastSeparator (`Comma`)

```C#
public static string CommaAnd<T>(this IEnumerable<T> collection)
public static string CommaAnd<T>(this IEnumerable<T> collection, Func<T, string> toString)

public static string CommaOr<T>(this IEnumerable<T> collection)
public static string CommaOr<T>(this IEnumerable<T> collection, Func<T, string> toString)

public static string Comma<T>(this IEnumerable<T> collection, string lastSeparator)
public static string Comma<T>(this IEnumerable<T> collection, Func<T, string> toString, string lastSeparator)
```

Example:

```C#
new[]{ 1, 2, 3 }.CommaAnd(); //Returns: "1, 2 and 3" (or "1, 2 y 3" if CurrentCulture is Spanish)
new[]{ 1, 2, 3 }.Comma(n=>n.ToString("00"), " & "); //Returns: "01, 02 & 03"
```

### ToConsole, ToFile

```C#
//Write each element in collection in a line in the console: 
public static void ToConsole<T>(this IEnumerable<T> collection)
public static void ToConsole<T>(this IEnumerable<T> collection, Func<T, string> toString)

//Write each element in collection in a line in a text file: 
public static void ToFile(this IEnumerable<string> collection, string fileName)
public static void ToFile<T>(this IEnumerable<T> collection, Func<T, string> toString, string fileName)
```


### ToDataTable

```C#
//Creates a DataTable with a column for each public property or field on T. Cool for debugging.
public static DataTable ToDataTable<T>(this IEnumerable<T> collection)
```

### ToStringTable, FormatTable and ToConsoleTable

```C#
//Creates a bidimensional string array with every public member (property or field) in T.
//Member names are placed in the first row (headers), and values for each element are placed in the following rows. 
public static string[,] ToStringTable<T>(this IEnumerable<T> collection)

//Format a bidimensional string array in a big string
//Making room between columns so the longer value (or header if enabled) fits in each column. 
public static string FormatTable(this string[,] table, bool longHeaders = true, string separator = " ")

//Uses WriteFormatedStringTable using Console.Out as the textWriter. 
public static void ToConsoleTable<T>(this IEnumerable<T> collection, string title)
```

All these methods make reusable pieces and create behavior by
composition, but the most useful method from the client side is
ToConsoleTable. Let's see an example:

```C#
new DirectoryInfo(@"C:\Users\Public\Pictures\Sample Pictures").GetFiles()
.Select(a=>new 
{
   a.Name,  
   Size = a.Length.ToComputerSize(), 
   a.LastWriteTime, 
   a.CreationTime 
})
.ToConsoleTable("..::My Images::.."); 

//Returning
..::My Images::..
Name          Size          LastWriteTime       CreationTime
Butterfly.jpg 303,06 KBytes 13/12/2008 0:28:36  13/12/2008 0:28:40
desktop.ini   1,06 KBytes   13/12/2008 0:51:05  13/12/2008 0:28:40
Field.jpg     297,31 KBytes 13/12/2008 0:28:36  13/12/2008 0:28:40
Flower.jpg    298,06 KBytes 13/12/2008 0:28:36  13/12/2008 0:28:40
Leaves.jpg    299,62 KBytes 13/12/2008 0:28:36  13/12/2008 0:28:40
Rocks.jpg     304,26 KBytes 13/12/2008 0:28:36  13/12/2008 0:28:40
Thumbs.db     115,50 KBytes 29/01/2009 17:17:12 18/01/2009 5:10:16
Tulip.jpg     274,95 KBytes 13/12/2008 0:28:36  13/12/2008 0:28:40
Waterfall.jpg 306,83 KBytes 13/12/2008 0:28:36  13/12/2008 0:28:40
Window.jpg    264,67 KBytes 13/12/2008 0:28:36  13/12/2008 0:28:40
```


### WithMin WithMax WithMinMaxPair

Linq already provides a way of retrieving the Min or the Max values in a
collection, but it doesn't provide any help if finding the element that
has the Min (or Max).

```C#
//Returns the element on collection with the minimum selected value
public static T WithMin<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector) where V : IComparable<V>

//Returns the element on collection with the maximum selected value
public static T WithMax<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector) where V : IComparable<V>

//Returns a MinMax structure with the elements on collection with the minimum an maximum selected value 
public static MinMax<T> WithMinMaxPair<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector) where V : IComparable<V>
```

WithMinMaxPair returns a [DataStructures-Simple|MinMax] structure.

Example:

```C#
var oldestFile = new DirectoryInfo(@"C:\Users\Public\Pictures\Sample Pictures").GetFiles().WithMin(a=>a.CreationTime);  
```

### ToInterval

```C#
//Gets the Min and the Max values in the collection iterating over the collection just once. 
public static Interval<T> ToInterval<T>(this IEnumerable<T> collection) where T : struct, IComparable<T>, IEquatable<T>

//Gets the Min and the Max values selected by valueSelector in the collection iterating over the collection just once. 
public static Interval<V> ToInterval<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector)  V:struct, IComparable<V>, IEquatable<V>
```

Example:

```C#
new []{ 2, 10, 100, -4, -120}.ToInterval(); //Return Interval<int>(-120, 100)
```

### BiSelect

If you need to make transformation on a List that need pair of
elements instead of just one element, then Select doesn't fit your needs
and you have to use BiSelect.

```C#
public static IEnumerable<S> BiSelect<T, S>(this IEnumerable<T> collection, Func<T, T, S> func)
```

Let's see an example. You have a list of 5 numbers and you want the list
of differences from one number to the next one:

```C#
new[] { 2, 3, 5, 7, 11}.BiSelect((a, b) => b - a).ToString(", "); 

//Result: 1, 2, 2, 4
```

As you see, the resulting list has only 4 values because the first element (2)
and the last one (29) appear just once. There is another overload that
allows you to turn pick between many options:

```C#
public static IEnumerable<S> BiSelect<T, S>(this IEnumerable<T> collection, Func<T, T, S> func, BiSelectOptions options)

public enum BiSelectOptions
{
    None, // N-1 values (first and last only once)
    Initial, // N values ( [default(T),first] is computed too)
    Final, // N values ( [last,default(T)] is computed too ) 
    InitialAndFinal, // N+1 values ([default(T),first] and [last,default(T)] are computed too)
    Circular, // N values ([last,first] are computed too)
}
```

Let's test all the options at once:

```C#
var nums = new[] { 2, 3, 5, 7, 11};
   EnumExtensions.GetValues<BiSelectOptions>()
   .Select(opt => new
   {
      Option = opt,
      Result = nums.BiSelect((a, b) => b - a, opt).ToString(", ")
   }).ToConsoleTable("-= Test =-");

//-= Test =- 
//Option          Result
//None            ___1, 2, 2, 4
//Initial         2, 1, 2, 2, 4
//Final           ___1, 2, 2, 4, -11
//InitialAndFinal 2, 1, 2, 2, 4, -11
//Circular        ___1, 2, 2, 4, -9

//Note: __Underscores__ Added to make it easy to understand
```

### SelectAggregate

Aggregate function provided by Linq allows to collapse all the elements
in a collection using a function, returning a single element as a
result.

Often you are interested in all the intermediate aggregate values. In
this situations SelectAggregate comes to the rescue!

```C#
public static IEnumerable<S> SelectAggregate<T, S>(this IEnumerable<T> collection, S seed, Func<S, T, S> aggregate)
```

Let's see an example. Given a list of 5 ones, the intermediate values of
summing all of them:

```C#
new[] { 1, 1, 1, 1, 1 }.SelectAggregate(0, (a, b) => a + b).ToString(", ");

//Result: 0, 1, 2, 3, 4, 5
```

As you see, the result has 6 values because the first value (seed) is
also returned.

As a curiosity, if you apply this function to the result of the previous
example (BiSelect) using the first value as the seed you restore the
values:

```C#
new[] { 2, 3, 5, 7, 11}.BiSelect((a, b) => b - a).SelectAggregate(2, (a, b) => a + b).ToString(", ");
//Result: 2, 3, 5, 7, 11
```

You can thing of BiSelect as the derivative, and SelectAggregate as the integral. 

### Distinct
Overload of distinct taking a lambda as a comparer. Keeps only the first element that returns each different value.

```C#
public static IEnumerable<T> Distinct<T, S>(this IEnumerable<T> collection, Func<T, S> func)
```

```C#
public static IEnumerable<T> Distinct<T, S>(this IEnumerable<T> collection, Func<T, S> func, IEqualityComparer<S> comparer)
```

### OrderBy

Just a parameterless overload of OrderBy that works on
`IQuerable<T>` where T is itself an `IQuerable<T>`.

There are two overloads, one for IEnumerable and other for IQueryable:

```C#
public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> collection) where T : IComparable<T>
public static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> collection) where T : IComparable<T>
```

Example:

```C#
new[] { 3,5,1,2 }.OrderBy().ToString("");

//Result: 1235
```

### Zip

Zip operator just mixes two sequences of elements taking an element of
each sequence in pairs. Zip operator is common operator included in C# 4.0, and we add 5 more variations

```C#
//Included in BCL: Zips two sequences using a mixer function finish on the shorter
public static IEnumerable<R> Zip<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB, Func<A, B, R> resultSelector)

//Zips two sequences using Tuples, finish on the shorter
public static IEnumerable<(A, B)> Zip<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB)

//Zips two sequences using a mixer function, finish on the longer and filling the sorter with default(T)
public static IEnumerable<R> ZipOrDefault<A, B, R>(this IEnumerable<A> colA, IEnumerable<B> colB, Func<A, B, R> resultSelector)

//Zips two sequences using Tuples, finish on the longer and filling the sorter with default(T)
public static IEnumerable<(A, B)> ZipOrDefault<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB)


//Zips two sequences using a mixer function, throws InvalidOperationException is they are not the same size
public static IEnumerable<R> ZipStrict<A, B, R>(this IEnumerable<A> colA, IEnumerable<B> colB, Func<A, B, R> resultSelector)

//Zips two sequences using Tuples, throws InvalidOperationException is they are not the same size
public static IEnumerable<(A, B)> ZipStrict<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB)


//Zips two sequences taking an action for each pair and returning void, finishes on the sorter
public static void ZipForeach<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB, Action<A, B> actions)

//Zips two sequences taking an action for each pair and returning void, if not the same length throws InvalidOperationException
public static void ZipForeachStrict<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB, Action<A, B> actions)
```


### JoinStrict

Similar to join but whenever there's a mismatch between the two
collections throws a nice exception message. The keys shouldn't be
repeated in any of the two collections.

```C#
  public static IEnumerable<R> JoinStrict<K, O, N, R>(
           IEnumerable<O> oldCollection,
           IEnumerable<N> newCollection,
           Func<O, K> oldKeySelector,
           Func<N, K> newKeySelector,
           Func<O, N, R> resultSelector, 
           string action )
```

Example:

```C#
new []{1,2,3,4,5}.JoinStrict(new[]{1,3,5,7,9}, a=>a, b=>b, (a,b)=>a, "Joining Numbers");
// Throws an InvalidOperationException with the message: 
// Error Joining Numbers: 
// Missing: 7, 9
// Extra: 2, 4
```

Usefully to build a 1-to-1 correspondence between elements of two
collections, asserting it in the process.


### Conversions

Just some extension methods to convert IEnumerbles to collection types
already available in .Net:

```C#
//Creates a new HashSet with the elements (repeated elements disappear) 
public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)

//Converts the IEnumerable to a ReadOnlyCollection (if necessary)
public static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> collection)
```

AsThreadSafe returns a [Synchronization|ThreadSafeEnumerator] for the
current IEnumerable so you can consume it from different threads at the
same time safely.

```C#
public static IEnumerable<T> AsThreadSafe<T>(this IEnumerable<T> source)
```

ToProgressEnumerator creates a `ProgressEnumerator` keeps tracks of
progress as the IEnumerable gets consumed.

```C#
public static IEnumerable<T> ToProgressEnumerator<T>(this IEnumerable<T> source, out IProgressInfo pi)
```

### AddRange / PushRange / EnqueueRange

Finally, just some extension methods over HashSet, Stack, and Queue to
make them easier to be used with LINQ Queries.

```C#
public static void PushRange<T>(this Stack<T> stack, IEnumerable<T> elements)
public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> elements)
public static void AddRange<T>(this HashSet<T> hashset, IEnumerable<T> collection)
```


### Iterate

Returns a sequence of `Iteration<T>` and object containing, in addition to the value, usefull information for the iteration.  

```C#
public static IEnumerable<Iteration<T>> Iterate<T>(this IEnumerable<T> collection)

public class Iteration<T>
{
    public T Value { get; }
    public bool IsFirst { get; } }
    public bool IsLast { get;  }
    public int Position { get; }
    public bool IsEven { get; }
    public bool IsOdd { get; }
}
```

### CartesianProduct
N-dimensional generalization of a SelectMany. Given a sequence sequences, returns all the possible combination of elements: 

```C#
public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)


new []
{
   new []{1,2},
   new []{10,20},
   new []{100,200},
}.CartesianProdut();

//returns something like:
new []
{
   new []{1,10, 100},
   new []{1,10, 200},
   new []{1,20, 100},
   new []{1,20, 200},

   new []{2,10, 100},
   new []{2,10, 200},
   new []{2,20, 100},
   new []{2,20, 200},
}
```