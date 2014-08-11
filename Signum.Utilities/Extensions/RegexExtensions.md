ReflectionExtensions
--------------------

Some extensions to enrich [Reflection API][http://msdn.microsoft.com/en-us/library/f7ykdhsy(VS.80).aspx]. 
See also ReflectionUtils.

### Nullify, UnNullify and IsNullable

Extension methods over System.Type to deal with Nullable Types.

```c#
public static bool IsNullable(this Type type)
public static Type Nullify(this Type type)
public static Type UnNullify(this Type type)
```

Examples:

```c#
Type intType = typeof(int);
Console.WriteLine(intType.IsNullable());  //false
Type nIntType = intType.Nullify();
Console.WriteLine(nIntType.IsNullable()); //true
Console.WriteLine(nIntType.UnNullify() == intType); //true
```

### ReturningType

Gets the returning type for a PropertyInfo, FieldInfo, MethodInfo,
ConstructorInfo or EventInfo in a... carefree way. Returns null for
System.Type (i.e: nested Type).

```c#
public static Type ReturningType(this MemberInfo m)
```

Example:

```c#
MemberInfo member = typeof(Person).GetMember(DateTime.Today.Day % 2 == 0 ? "_name" : "Name");
member.ReturningType();
//Returns string type, whether a FieldInfo or a PropertyInfo is retrieved.
```

### HasAttribute and SigleAttribute

Allows to know easily if a MemeberInfo contains attributes of some type
and retrieves them (if any).

```c#
public static bool HasAttribute<T>(this ICustomAttributeProvider mi) where T : Attribute
public static T SingleAttribute<T>(this ICustomAttributeProvider mi) where T : Attribute
```


Example:

```c#
[Serializable]
public class Person
{

}

typeof(Person).HasAttribute<SerializableAttribute>(); //Returns true
typeof(Person).SingleAttribute<SerializableAttribute>(); //returns the SerializableAttribute
```

### IsInstantiationOf

Fast way to know if a System.Type represents a 'concretization' of a
generic type.

```c#
public static bool IsInstantiationOf(this Type type, Type genericType)
```

Example:

```c#
typeof(List<int>).IsInstantiationOf(typeof(List<>)); // returns true 
```

### FieldEquals and PropertyEquals

Simple way to compare a FieldInfo (or PropertyInfo) using
[Reflection|Strong Typed Reflection].

```c#
public static bool FieldEquals<T>(this FieldInfo fi, Expression<Func<T, object>> lambdaToFiel)
public static bool PropertyEquals<T>(this PropertyInfo fi, Expression<Func<T, object>> lambdaToProperty)
```

Example:

```c#
typeof(string).GetProperty("Length").PropertyEquals((string s) => s.Length); //returns true
```

### IsReadOnly

Returns whether a property has no setter or it's not public.

```c#
public static bool IsReadOnly(this PropertyInfo pi)
```

Example:

```c#
typeof(string).GetProperty("Length").IsReadOnly(); //returns true
```


RegexExtensions
---------------

Some methods to make common patterns involving [1][] and IEnumerables
easier.

*Performance Consideration:* Creating a Regex object is an
expensive operation (have to be parsed and compiled). The methods here
cache the object locally, using the same for all the elements in the
collection, but not from one call to the next one. This is a good idea
for Loading applications, but for server methods it's a better idea to
store the Regex object in a static field, not using the methods here.

### Match, MatchPair

Applies a Regex string to all the elements in collection, returning only
the ones who actually matched.

`
public static IEnumerable<Match> Match(this IEnumerable<string> collection, string regex)
public static IEnumerable<Match> Match<T>(this IEnumerable<T> collection, Func<T, string> stringSelector, string regex)

public static IEnumerable<Tuple<string, Match>> MatchPair(this IEnumerable<string> collection, string regex)
public static IEnumerable<Tuple<T, Match>> MatchPair<T>(this IEnumerable<T> collection, Func<T, string> stringSelector, string regex)
`

Example:

`
typeof(Console).Assembly.GetTypes()
.MatchPair(a => a.Name, "^.*Console.*$")
.ToConsole(p => p.First.Name);

//Console
//ConsoleCancelEventHandler
//ConsoleCancelEventArgs
//ConsoleColor
//ConsoleKey
//ConsoleKeyInfo
//ConsoleModifiers
//ConsoleSpecialKey
//ConsoleCtrlHandlerRoutine
//__ConsoleStream
`

### MostSimilar

Uses [StringDistance] to find the item with the selected string with min
string distance to a given pattern.

`
public static T MostSimilar<T>(this IEnumerable<T> collection, Func<T, string> stringSelector, string pattern)
`

Example:

`
typeof(string).Assembly.GetTypes().MostSimilar(a => a.Name, "Quonsole"); //return the System.Console Type object. 
`

  [1]: http://msdn.microsoft.com/en-us/library/system.text.regularexpressions.regex.aspx|Regex