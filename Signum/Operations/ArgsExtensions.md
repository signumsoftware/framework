# ArgsExtensions
---------------

Signum Framework sometimes uses array of objects (params object[]) in order to provide extra parameters, for example in operations. 

In order to avoid the order of the elements affect the result, just one element of each type should be added. 

If more than one is needed, a custom object with custom properties will give more semantic information. 


### GetArg

Get's the only element of type T in the list, throwing an exception if 0 or more than 1 are found.

```C#
public static T GetArg<T>(this IEnumerable<object> args)
```

Example:

```C#
new object[]{1, "hi"}.GetArg<string>() // returns "hi";
new object[]{1, 2}.GetArg<int>() // throws "Sequence contains more than one Int32";
new object[]{1 }.GetArg<string>() // throws "Sequence contains no String";
```


### TryGetArg

Get's the only element of type T in the list or null. Two overloads for reference and value types.

```C#
public static T TryGetArgC<T>(this IEnumerable<object> args) where T : class
public static T? TryGetArgS<T>(this IEnumerable<object> args) where T : struct
```

Example:

```C#
new object[]{1, "hi"}.TryGetArgC<string>() // returns "hi";
new object[]{1, 2}.TryGetArgS<int>() // throws "Sequence contains more than one Int32";
new object[]{1 }.TryGetArgC<string>() // returns null;
```


