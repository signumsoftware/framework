

EnumExtensions
--------------

Just some extensions for any kind of enum.

### ToEnum

Just parses the enum using Enum.Parse, but does the casting for you.

```c#
public static T ToEnum<T>(this string str) where T : struct
public static T ToEnum<T>(this string str, bool ignoreCase) where T : struct
```

Example:

```c#
"Monday".ToEnum<DayOfWeek>()); //returns DayOfWeek.Monday
"monday".ToEnum<DayOfWeek>()); //throws ArgumentException
"Monday".ToEnum<DayOfWeek>(true)); //returns DayOfWeek.Monday
"monday".ToEnum<DayOfWeek>(true)); //returns DayOfWeek.Monday
```


### GetValues

Returns all the values in an enum type in a typed array:

```c#
public static T[] GetValues<T>()
```

Example: 
```c#
EnumExtensions.GetValues<DayOfWeek>();
//Returns: new DayOfWeek[]{DayOfWeek.Sunday, DayOfWeek.Monday, ... }
```

### IsDefined

Just as Enum.IsDefined but saving you to do the typeof(MyEnum)

```c#
public static bool IsDefined<T>(T value) where T : struct
```

Example:

```c#
EnumExtensions.IsDefined(DayOfWeek.Monday); //returns true
EnumExtensions.IsDefined((DayOfWeek)10); // returns false
```

### MinFlag and MaxFlag

Returns the minimum flag in a int (useful for enums decorated with [FlagsAttribute](http://msdn.microsoft.com/en-us/library/system.flagsattribute(VS.71).aspx)).

```c#
public static int MinFlag(int value)
public static int MaxFlag(int value)
```

Example:

```c#
//Binary representation of 14 = 1110 = 8 + 4 + 2
EnumExtensions.MinFlag(14); //Returns 2
EnumExtensions.MaxFlag(14); //Returns 8  
```