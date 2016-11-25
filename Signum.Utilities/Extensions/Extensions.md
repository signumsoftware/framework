# Extensions class

As a general rule we tried to avoid adding ExtensionMethods to basic
types, like object (or a free generic type) to avoid cluttering
IntelliSense.

The class here, however, contains functionality so widely used in our
code that it's worth giving them _license to clutter_.

##Maths

### Parse Number

Contains extension methods to make TryParse more user friendly by using
Nullables:

```C#
public static int? ToInt(this string str) 
public static long? ToLong(this string str)
public static short? ToShort(this string str)
public static float? ToFloat(this string str)
public static double? ToDouble(this string str)
public static decimal? ToDecimal(this string str)
public static bool? ToBool(this string str)
```

And the equivalent overloads that throw a new FormatException with your
own custom error message if the parsing fails:

```C#
public static int ToInt(this string str, string error)
public static long ToLong(this string str, string error)
public static short ToShort(this string str, string error)
public static float ToFloat(this string str, string error)
public static double ToDouble(this string str, string error)
public static decimal ToDecimal(this string str, string error)
public static bool ToBool(this string str, string error)
```

### Mod and DivMod
Implements the [Modulo operation](http://en.wikipedia.org/wiki/Modulo_operation) (not to confuse with reminder operation, or %). 
Useful because `n.mod(4)` will always be in the range `[0..3]`, while `n % 4` is in the range `[-3...3]`  because is negative for `n < 0`. 

```C#
public static int Mod(this int a, int b)
public static long Mod(this long a, long b)
```

```C#
(-3) % 5    //returns -3
(-3).mod(5) //returns 2
```

### DivMod
Additionally, DivMod allows to get the division and the modulo at the same time. Much like DivRem. 

```C#
public static int DivMod(this int a, int b, out int mod)
public static long DivMod(this long a, long b, out long mod)
```

### DivCeiling
Returns the rounded-up division of a and b. Useful for pagination. 

```C#
public static int DivCeiling(this int a, int b)
public static long DivCeiling(this int a, long b)
```

```C#
int rows = 23; 
int rowsPerPage = 10;
int totalPages = rows.DivCeiling(rowsPerPage); //returns 3
```


#Fluent Expressions

### Let

Sometimes you need to store an expression result in a variable to use
more than once _in an expression_:

```C#
FileInfo fi = new FileInfo("MyPicture.bmp"); 

Console.WriteLine("Name {0} Size {1}".FormatWith(fi.Name, fi.Length)); 
```

Let method allows you to avoid 'braking the line' doing this:

```C#
Console.WriteLine(new FileInfo("MyPicture.bmp").Let(fi=>"Name {0} Size {1}".FormatWith(fi.Name, fi.Length))); 
```

Let is defined just like this.

```C#
public static R Let<T, R>(this T t, Func<T, R> func)
{
    return func(t);
}
```

You can think of map as a Try (see below) that doesn't handle
nullability, or as a Select for single elements.


### Do

Sometimes you need to store an expression result in a variable to make
many _actions_ over it:

```C#
Button b = new Button { Text = "Ok" };

Grid.SetColumn(b, 3); 

grid.AddChild(b); 
```

`Do` extension method allows you to avoid 'braking the line' doing this:

```C#
grid.AddChild(new Button { Text = "Ok" }.Do( b => Grid.SetColumn(b,3) ); 
```

`Do` is defined like this.

```C#
public static T Do<T>(this T t, Action<T> action)
{
   action(t);
   return t;
}
```

As you see, it returns the initial object to allow chainability.

`Do` is useful when you need to [an object initializer](http://msdn.microsoft.com/en-us/library/bb384062.aspx) but you need to call
methods or attach events as well.

```C#
return new Button 
{ 
  Text = "Ok" 
}
.Do(b => Grid.SetColumn(b,3) )
.Do(b =>  b += Mouse_Click  ); 
```

#Fluent Expressions with null values

Some of this methods will disappear when C# gets `?.` operator. 
Still if you use them now will be easy to change with a Regex. 

### TryToString

A shortcut for using ToString with Try behavior.

```C#
public static string TryToString(this object obj)
public static string TryToString(this object obj, string defaultValue)
```

Example:

```C#
((int?)null)?.ToString(); //Returns null instead of throwing exception
((int?)null)?.ToString("0.00"); //for IFormattable
```


### ThrowIfNull

Sometimes you want to be sure that an object is not null before using it throwing a special message. 
`ThrowIfNull` allows to do that without braking the line.

```C#
if(Departament.Boss == null)
   throw new NullReferenceException("A Boss is mandatory to Promote");

Departament.Boss.Promote(); 
```

With `ThrowIfNull` you can assert not nullability on the fly:

```C#
Departament.Boss.ThrowIfNullC("Departament has no Boss").Promote(); 

Departament.Boss.ThrowIfNullC(()=>"Departament {0} has no Boss".FormatWith(Departament)).Promote();  //Lambda overload
```

This specially useful on queries, you will have to change to a [
statement body](http://msdn.microsoft.com/en-us/library/bb397687.aspx) to do something like this.


### DefaultToNull

Transforms value type's default value to null, mainly to remove zeros in a table.

```C#
public static T? DefaultToNull<T>(this T value)
{
    return EqualityComparer<T>.Default.Equals(default(T), value) ? (T?)null : value;
}
```

Example:

```C#
DateTime.MinValue.DefaultToNull() == null  // Is True

p1.Name.CompareTo(p2.Name).DefaultToNull() ?? p1.Age.CompareTo(p2.Age)
```

### NotFoundToNull

It's common to return -1 to mean 'index not found'. Unfortunately the
convenient coalesce operator doesn't work with -1, but with (int?)null.

This simple method converts -1 to null.

```C#
public static int? NotFoundToNull(this int value)
{
    return value == -1 ? null : (int?)value; 
}
```

```C#
list.SelecteIndex = countries.FindIndex(a=>a == User.Country).NotFoundToNull() ?? countries.IndexOf(France);
```



##Collection Methods

### For

`For` is the functional equivalent of the **for statement**.

```C#
public static IEnumerable<T> For<T>(this T start, Func<T, bool> condition, Func<T, T> increment)
{
    for (T i = start; condition(i); i = increment(i))
       yield return i;
}
```

Example:

`
1.For(i => i <= 1024, i => i * 2).ToString(","));

// Result: 1,2,4,8,16,32,64,128,256,512,1024
`

`For` is also the generalization of the methods above that make simple
cases simpler to write.

### To

To allows a Rubysh style of generating sequences of numbers to,
presumably, start a query on it.

```C#
  public static IEnumerable<int> To(this int start, int endNotIncluded)
  {
     for (int i = start; i < endNotIncluded; i++)
        yield return i;
  }
```

It has two main differences over Enumerable.Range:

-   It takes the *end* and not the *count* as second parameter.
-   It's way shorter.

```C#
Enumerable.Range(5,5);
//Result: 56789
```

Could be written now:

```C#
5.To(10); 
//Result: 56789
```

`To` uses C language family convention of not reaching the end value in a for.

There's also another overload that takes a step parameter:

```C#
0.To(10, 2); 
//Result: 02468
```

### DownTo

Similar to 'To' but decreasing instead of increasing.

```C#
public static IEnumerable<int> DownTo(this int startNotIncluded, int end)
{
    for (int i = startNotIncluded - 1; i >= end; i--)
        yield return i;
}
```

As you see, it also uses C 'inverse for' convention, so the initial
value *is not* included, while the final one *is* (we did
the opposite with in 'To'). It satisfies the property:

```C#
A.To(B) == B.DownTo(A).Reverse()
```

Example:

```C#
10.DownTo(5).ToString(""); 

//Result: 98765
```

It also has an overload with step (positive!!): Example:

```C#
10.DownTo(0, 2).ToString(""); 

//Result: 86420
```

### Folow

Finally, Follow is another concretion of For method. In this case it
assumes that the sequence will end when the first null value is reached.

```C#
 public static IEnumerable<T> Follow<T>(this T start, Func<T, T> next)
 {
     for (T i = start; i != null; i = next(i))
         yield return i;
 }
```

It's used to generate a sequence of elements by following a path from an
initial and taking and action each time. Like following the Parent
chain:

```C#
 FrameworkElement first = ...; 
 IEnumerable<FrameworkElement> parents = first.FollowC(fe => fe.Parent); 
 Grid grid = parens.OfType<Grid>().First(); 
```

Or the sorter version:

```C#
 Grid grid = first.Follow(fe => fe.Parent).OfType<Grid>().First(); 
```