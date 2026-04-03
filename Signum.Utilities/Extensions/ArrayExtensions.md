# ArrayExtensions
---------------

Extensions that take or generate arrays

### Initialize

Initializes every cell of the array with a provided function.

```C#
public static T[,] Initialize<T>(this T[,] array, Func<int,int,T> valueXY)
public static T[, ,] Initialize<T>(this T[, ,] array, Func<int, int, int, T> valueXYZ)
```

Example:

```C#
new int[2, 2].Initialize((i, j) => i + j);
//returns: new[,] { { 0, 1 }, { 1, 2 } }; 
```

### ToArray

Another overload of ToArray method that takes a function, shuffling the
values as they are introduced:

```C#
public static S[] ToArray<T,S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos)
public static S[,] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos, Func<T, int> yPos)
public static S[, ,] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos, Func<T, int> yPos, Func<T, int> zPos)
```

Example:

```C#
Point[] points = new[]{new Point(0,0), new Point(1,1)};
bool[,] board = points.ToArray(p=>true, p=>p.X, p.Y); 
//returns: new[,] { { true, false }, { false, true } }; 
```

**SF2:** Now there's also some overload that takes the length of the
resulting array as a parameter. It's more efficient because avoids
enumerating the collection twice.

```C#
public static S[] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos, int xLength)
public static S[,] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos, Func<T, int> yPos, int xLength, int yLength)
public static S[, ,] ToArray<T, S>(this IEnumerable<T> collection, Func<T, S> value, Func<T, int> xPos, Func<T, int> yPos, Func<T, int> zPos, int xLength, int yLength, int zLength)
```

### Row and Column

Given a bi-dimensional array and a row (or column) index, returns all
the elements in this row.

A row is the sequence of elements [\_,row] A column is the sequence of
elements [column,\_]

```C#
public static IEnumerable<T> Row<T>(this T[,] data, int row)
public static IEnumerable<T> Column<T>(this T[,] data, int column)
```

Result:

```C#
int[,] arrays = new[,] { { 0, 1, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };

arrays.Column(1).ToString("");
//Returns: 456

arrays.Row(1).ToString(""); 
//Returns: 158
```

### AddRow and AddColumn

Create a new bi-dimensional array inserting a row (or a column) at a
given position:

```C#
public static T[,] AddRow<T>(this T[,] values, int pos, T[] newValues)
public static T[,] AddRow<T>(this T[,] values, int pos, Func<int, T> newValue)

public static T[,] AddColumn<T>(this T[,] values, int pos, T[] newValues)
public static T[,] AddColumn<T>(this T[,] values, int pos, Func<int, T> newValue)
```

Example:

```C#
 var a = new[,] { { 1, 2 }, { 3, 4 } }.AddColumn(2, new[] { 5, 6 });
```

> **Note:** Note that when using array initialization you are defining Columns, not Rows

### SelectArray

Maps each element from a given bi- and three-dimensional array to a new
one. There are overloads that give the coordinates to the selector as
well.

```C#
public static S[,] SelectArray<T,S>(this T[,] values, Func<T,S> selector)
public static S[,] SelectArray<T, S>(this T[,] values, Func<int, int, T, S> selector)

public static S[, ,] SelectArray<T, S>(this T[, ,] values, Func<T, S> selector)
public static S[, ,] SelectArray<T, S>(this T[, ,] values, Func<int, int, int, T, S> selector)
```

Example:

```C#
var a = new[,] { { 1, 2 }, { 3, 4 } }.SelectArray(n => 4 - n);
//Returns: new[,] { { 3, 2 }, { 1, 0 } } 
```
