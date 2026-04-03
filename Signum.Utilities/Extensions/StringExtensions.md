# StringExtensions
--------------------

Useful extension methods to deal with strings.

### HasText

Equivalent to `!string.IsNullOrEmpty.`

```C#
public static bool HasText(this string str)
```

Example:

```C#
((string)null).HasText(); //false
"".HasText(); //false
"hello".HasText(); //true
```

### DefaultText

[Coallesce operator](http://msdn.microsoft.com/en-us/library/ms173224.aspx) for strings, considering "" a null string.

```C#
public static string DefaultText(this string str, string defaultText)
```

Example:

```C#
((string)null).DefaultText("Hi"); //"Hi"
"".DefaultText("Hi"); //"Hi"
"hello".DefaultText("Hi"); //"Hello"
```

### AssertHasText

Throws an exception with a custom message and if the string is null or
empty, returns the string to allow chaining.

```C#
public static string AssertHasText(this string str, string errorMessage)
```

Example:

```C#
((string)null).AssertHasText("Arrgg!!"); //throws new ArgumentException("Arrgg!!");
"".AssertHasText("Arrgg!!"); //throws new ArgumentException("Arrgg!!");
"hello".AssertHasText("Arrgg!!"); //"Hello"
```

### Add

Allows concatenating two parts to an string adding a separator only both
parts are not empty;

```C#
public static string Add(this string str, string separator, string part)
{
    if (str.HasText())
    {
        if (part.HasText())
            return str + separator + part;
        else
            return str;
    }
    else
        return part;
}
```

Example:

```C#
((string)null).Add(";", "hi"); //"hi"
"".Add(";", "hi"); //"hi"
"hello".Add(";", "hi"); //"hello;hi"
"hello".Add(";", "");//"hello"
"hello".Add(";", null);//"hello"
```

**Performance consideration:** Do not use this method in a loop, use `EnumerableExtensions.ToString` instead.

### AddLine

Same as Add, but using `"\r\n"` as the separator.

```C#
public static string AddLine(this string str, string part)
```

Example:

```C#
((string)null).AddLine("hi"); //"hi"
"".AddLine("hi"); //"hi"
"hello".AddLine("hi"); //"hello\r\nhi"
```

### RemoveDiacritics 

Removes all diacritics (accents) from a string using [algorithm](http://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net/249126#249126).

```C#
public static string RemoveDiacritics(this string s)
```

Example:

```C#
"âãäåçèéêë ìíîïðñòó ôõöùúûüý".RemoveDiacritics(); // returns "aaaaceeee iiiiðnoo ooouuuuy"
```


### Lines

Splits a string by "\\r\\n", or returns an empty array if the string is
empty.

```C#
@"I've seen things you people wouldn't believe. 
Attack ships on fire off the shoulder of Orion. 
I watched C-beams glitter in the dark near the Tannhauser gate. 
All those moments will be lost in time, like tears in rain. 
Time to die.".Lines(); 

//Result: 
new string[]
{
  "I've seen things you people wouldn't believe.", 
  "Attack ships on fire off the shoulder of Orion.", 
  "I watched C-beams glitter in the dark near the Tannhauser gate.", 
  "All those moments will be lost in time, like tears in rain.", 
  "Time to die."
};
```


## Try?(After|Before)Last?

This family of methods try to simplify splitting a string in two parts by a separator. 

Tipically, this code is made using `Split` (convinient but slow) or combinations of `IndexOf` and `SubString` (error prone).


```C#
public static string Before(this string str, char separator)
public static string Before(this string str, string separator)
public static string After(this string str, char separator)
public static string After(this string str, string separator)
public static string TryBefore(this string str, char separator)
public static string TryBefore(this string str, string separator)
public static string TryAfter(this string str, char separator)
public static string TryAfter(this string str, string separator)

public static string BeforeLast(this string str, char separator)
public static string BeforeLast(this string str, string separator)
public static string AfterLast(this string str, char separator)
public static string AfterLast(this string str, string separator)
public static string TryBeforeLast(this string str, char separator)
public static string TryBeforeLast(this string str, string separator)
public static string TryAfterLast(this string str, char separator)
public static string TryAfterLast(this string str, string separator)
```

There are 16 different method because they can vary in 4 different aspects : 
* **char/string separator:** The use different overloads of `IndexOf`, I assume char is faster.   
* **Try/-:** If the separators is not found, Try methods return null while the default ones throw `InvalidOperationException("Separator '{0}' not found in {1}".FormatWith(separator, str))`
* **Before/After:** Get the string after or before the separator, never including the separator itself. 
* **-/Last:** Find the separator from the beginning or the end of the string.  

```C#
"Pulp Fiction".Before(' '); // "Pulp"
"Lord of the Rings".AfterLast(' '); // "Rings"
"Game of Thrones".TryBefore('4'); // null
"Game of Thrones".After(';'); // throws "Separator ';' not found in Game of Thrones"
```


### Try?Between

This methods get the string between the `firstSeparators` and `secondSeparator` (optional). 

```C#
public static string Between(this string str, string firstSeparator, string secondSeparator = null)
public static string Between(this string str, char firstSeparator, char secondSeparator = (char)0)
public static string TryBetween(this string str, string firstSeparator, string secondSeparator = null)
public static string TryBetween(this string str, char firstSeparator, char secondSeparator = (char)0)
```

If `secondSeparator` is not specified, `firstSeparator` is taken as default. 

The `char` variations use char overload of `IndexOf`. I assume is faster. 
The `Try` variations return `null` if one or both of the separators is not found. The default variations throw `InvalidOperationException("Separator '{0}' not found in {1}".FormatWith(separator, str))` 

```C#
"He is 'The Man'".Between('\''); // "The Man"
"Click <a>here<a/>".Between("<a>", "<a/>"); // "here"
"Game of Thrones".Between('T', "s"); // throws "Separator 's' not found in Game of Thrones"
```


### Try?(Start|End)

Simplify the usage of SubString when the part is at the begining or at the end. 

```C#
public static string Start(this string str, int numChars)
public static string TryStart(this string str, int numChars)
public static string End(this string str, int numChars)
public static string TryEnd(this string str, int numChars)
```

If `str.Length < numCharts`, the `Try` methods return `str`, while the default methods throw `InvalidOperationException("String '{0}' is too short".FormatWith(str))`. Also the `Try` method allow `str == null`.

Example:

```C#
"Pulp Fiction".Start(4); // "Pulp"
"Amelie".Start(7); //throws InvalidOperationExcetion
"Amelie".TryStart(7); //"Amelie"

"Pulp Fiction".End(4); // "tion"
"Amelie".End(7); //throws InvalidOperationExcetion
"Amelie".TryEnd(7,false); //"Amelie"
```

### Try?Remove(Start|End)

Methods that remove a substring from the end or start of the string :

```C#
public static string RemoveStart(this string str, int numChars)
public static string TryRemoveStart(this string str, int numChars)

public static string RemoveEnd(this string str, int numChars)
public static string TryRemoveEnd(this string str, int numChars)
```

The default variations throw an exception if `str.Length < numCharts`,  while the `Try` variations return `""`

Example:

```C#
"Pulp Fiction".RemoveStart(4); // " Fiction"
"Amelie".RemoveStart(7); //throws InvalidOperationExcetion
"Amelie".TryRemoveStart(7); //""

"Pulp Fiction".RemoveEnd(4); // "Pulp Fic"
"Amelie".RemoveEnd(7); //throws InvalidOperationExcetion
"Amelie".TryRemoveEnd(7); //""
```


### PadChopLeft and PadChopRight

Adjusts the size of the string to the provided length at any rate, calling `Substring` or a `PadLeft/Right` depending what is necessary.

```C#
public static string PadChopRight(this string str, int length)
public static string PadChopLeft(this string str, int length)
```

Example:

```C#
string[] lines = new []
{
   "3 Elf", 
   "7 Dwarf", 
   "9 Mortal Men", 
   "1 Dark Lord"
};

lines.ToConsole(s => "|" + s.PadChopRight(10) + "|");      
Writing: 
|3 Elf     |
|7 Dwarf   |
|9 Mortal M|
|1 Dark Lor|


lines.ToConsole(s => "|" + s.PadChopLeft(10) + "|"); 
Writing: 
|     3 Elf|
|   7 Dwarf|
|Mortal Men|
| Dark Lord| 
```

### Etc

Cuts a string to be smaller than a given length, if so, adds some
indicator at the end. "(...)" it's the default.

```C#
public static string Etc(this string str, int max, string etcString = "(...)")
```

Example:

```C#
"En un lugar de la Mancha, de cuyo nombre no quiero acordarme".Etc(15) 

//Writes: 
//"En un luga(...)"
// 012345678901234   //Notice how the string is exactly 15 chars long (0 to 14) with the etcString included


"En un lugar de la Mancha, de cuyo nombre no quiero acordarme".Etc(15, "...") 

//Writes: 
//"En un lugar ..."
// 012345678901234   //Notice how the string is exactly 15 chars long (0 to 14) with the etcString included
```

### FirstNonEmptyLine

Returns the first non-empty line of a text trimmed.

```C#
public static string FirstNonEmptyLine(this string str)
```

Example:

```C#
@"

En un lugar de la Mancha, 
de cuyo nombre no quiero acordarme".EtcLines(100) 

//Writes: 
//"En un lugar de la Mancha,"
```

### RemoveChars

Removes a set of chars from a string:

```C#
public static string RemoveChars(this string str, params char[] chars)
```

Example:

```C#
@"Well, its one for the money,
Two for the show,
Three to get ready,
Now go, cat, go".RemoveChars('a', 'e', 'i', 'o', 'u'); 

//Returns: 
//Wll, ts n fr th mny,
//Tw fr th shw,
//Thr t gt rdy,
//Nw g, ct, g
```

Usefull to remove special chars like ñ, ß, etc..

### FormatWith (Very Useful!!)

FormatWith is just another extension method for calling string.Format in a
sorter way.

string.Format is better than error prone string concatenation:

```C#
"Hello" + friendName + "!"; 
//Result in:  "HelloJoe!"  
//You miss the space!!
```

```C#
string.Format("Hello {0}!", friendName);
//Result in: "Hello Joe!"  
```

However, usually you realize that you will need string format when you
are already writing the string, having to jump back to the start of the
string. Making string.Format an instance method will have solved the
problem, but this is something for Microsoft to do. Our only option is
to make an extension method.

*Why this name?* Well... `Format` is already in use, `FormatWith` is too long, `F`
is too short, so we keep it the way it was before translating everything
to English: `FormatWith`

```C#
public static string FormatWith(string format, object arg0)
public static string FormatWith(string format, object arg0, object arg1)
public static string FormatWith(string format, object arg0, object arg1, object arg2)
public static string FormatWith(this string pattern, params object[] parameters)
public static string FormatWith(this string format, IFormatProvider provider, params object[] args)
```

Example:

```C#
"Hasta la vista {0}".FormatWith("baby"); //Returns: "Hasta la vista baby"
```

### Replace

Applies a dictionary from original -> replacement strings to a given
value.

If more than one replacement could be applied, the one added before to
the dictionary wins.

```C#
public static string Replace(this string str, Dictionary<string, string> replacements)
```

Example: 
```C#
var a = @"a b c abc".Replace(new Dictionary<string, string>() 
{ 
  { "abc", "_abc_" }, 
  { "a", "A" }, 
  { "b", "B" }, 
  { "c", "C" } 
});

//Returns:  
//A B C _abc_
```

### Indent

Adds a number of starting chars (spaces by default) at the beginning of
any line in a given string.

```C#
public static string Indent(this string str, int numChars)
public static string Indent(this string str, int numChars, char indentChar)
```

Example:

```C#
@"Hello
Dolly".Indent(3); 

//Result:
//   Hello
//   Dolly
```

It's useful for code generation, like C# or SQL statements:

```C#
"SELECT\r\n{0}\r\nFROM Customers".FormatWith(
     new[] { "FirstName", "SecondName", "Address" }.ToString(",\r\n").Indent(2));
//Result: 
//SELECT
//  FirstName,
//  SecondName,
//  Address
//FROM Customers
```
### Combine / Combine

Given a separator `this`, combines a sequence of string `params` writing the separator in between.

```C#
public static string Combine(this string separator, params object[] elements)
public static string Combine(this string separator, params object[] elements)
```

Combine ignores null or empty elements.

```C#
",".Combine(0, null, "", "hola"); //returns 0,,,hola
",".Combine(0, null, "", "hola"); //returns 0,hola
```

### ToComputerSize

This simple method that is an extension for **long** (not string). Given
a long number with the length in bytes for some amount of data, returns
a human readable string using Bytes,KiloBytes,MegaBytes...

```C#
public static string ToComputerSize(this long value)
public static string ToComputerSize(this long value, bool useAbbreviations)
```

Example:

```C#
20L.ToComputerSize(); //20.00 Bytes
1005L.ToComputerSize(); //1,005.00 Bytes
1024L.ToComputerSize(); //1.00 KBytes
3024050L.ToComputerSize(); //2.88 MBytes
4013441382L.ToComputerSize(true); //3.74 GB
12345678987654321.ToComputerSize(); //10.97 PBytes
```

> **Note:** As [notes](http://en.wikipedia.org/wiki/Kibibyte), according to the International Electrotechnical Commission in 2000, multiples of 1024 should be noted using Kibi(Ki)
instead of Kilo(K) to avoid confusion with multiples of 1000. It looks that this standard is being ignored and everybody stills using KiloByte(KB) to note 1024 Bytes, so we divide by 1024 in this extension
method.
