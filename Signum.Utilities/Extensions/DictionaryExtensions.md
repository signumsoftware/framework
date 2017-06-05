# Signum.Utilities.DictionaryExtensions class

Some useful extension methods to deal with Dictionaries.

We will use the same data as in [GroupExtensions] for our examples:

```C#
public class Planet
{
    public string Name;
    public string SunDistance;
    public int RevolutionDays;
    public int RotationDays;
    public string Diameter;
    public int Satellites;
    public string Color; 
}


 |Name     |SunDistance  |RevolutionDays  |RotationDays  |Diameter  |Satellites  |Color	 |
 |---------|-------------|----------------|--------------|----------|------------|-------|
 |Mercury  |Close        |88              |1416          |Small     |0           |Gray	 |
 |Venus    |Close        |225             |5832          |Small     |0           |Yellow |
 |Earth    |Close        |365             |24            |Small     |1           |Blue	 |
 |Mars     |Close        |687             |25            |Small     |2           |Orange |
 |Jupiter  |Medium       |4380            |10            |Big       |63          |Orange |
 |Saturn   |Medium       |10585           |10            |Big       |48          |Yellow |
 |Uranus   |Far          |30660           |18            |Medium    |27          |Blue	 |
 |Neptune  |Far          |60225           |18            |Medium    |13          |Blue	 |
```

### TryGet

In the days of .Net 1.1, before generics came onto the scene, the old
[HashTable](http://msdn.microsoft.com/en-us/library/system.collections.hashtable.aspx) had the good property of [returning null](http://msdn.microsoft.com/en-us/library/system.collections.hashtable.item.aspx) when a key is not found.
`Dictionary<K,V>` change this so it throws a [throws KeyNotFoundException](http://msdn.microsoft.com/en-us/library/9tee9ht2.aspx)
when the key is not found, because V could be a value type so null is
not an option.

Alternatively, Dictionary exposes [2](http://msdn.microsoft.com/en-us/library/bb347013.aspx) but it uses an out parameter, so
it's not very convenient to use when writing code in a functional style.

For this reasons we provide TryGet methods. There are many overloads,
TryGetC when V is a class (so null is allowed) and TryGetS when V is a
struct (so the output is V? to allow nulls).

`
public static V TryGetC<K, V>(this IDictionary<K, V> dictionary, K key) where V : class
public static V? TryGetS<K, V>(this IDictionary<K, V> dictionary, K key) where V : struct
public static V? TryGetS<K, V>(this IDictionary<K, V?> dictionary, K key) where V : struct
`

In the following example we use TryGet**S** because the V (not K) is a
value type:

```C#
Dictionary<string, int> colorToSatelitesSum = planets.AgGroupToDictionary(a => a.Color, gr => gr.Sum(a => a.Satellites)); 
colorToSatelitesSum.TryGetS("Pink");
//Return: null
int? blueSatellites = colorToSatelitesSum.TryGetS("Blue"); 
//Return: 41
```

### GetOrCreate

When a Dictionary is used for implementing a cache, often the following
pattern appears:

-   If the value is in the dictionary: Retrieve the value from the
    dictionary.
-   Else: Compute the value and store it in the dictionary.

This common pattern can be expressed easily using GetOrCreate extension
method:

```C#
//If key is not found, it's inserted with new V() as the Value
public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key) where V : new()

//If key is not found, it's inserted with value as the Value
public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, V value)

//If key is not found, it's inserted with generator() as the value
public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> generator)
```

Pedagogical example:

```C#
Dictionary<string, string> dic = new Dictionary<string, string>();

//Adds the pair and return "Bart"
dic.GetOrCreate("Simpson", "Bart");
//Returns "Bart"
dic.GetOrCreate("Simpson", "Lisa");

//Adds the pair and returns "Like"
dic.GetOrCreate("Skywalker", () => "Luke");
dic.GetOrCreate("Skywalker", () => "Anakin");

//DOES NOT COMPILE because there's no parameterless constructor for string
dic.GetOrCreate("Dalton"); 
```

Practical example:

```C#
static Dictionary<Type, string[]> fieldNames = new Dictionary<Type, string[]>();

(...)

static string[] GetFields(Type type)
{
   lock(fieldNames)
     fieldNames.GetOrCreate(type, ()=>type.GetFields().Select(fi=>fi.Name).ToArray()); 
}
```

*Note*: Note that GetOrCreate does not save you from locking the data
structure to avoid concurrency problems. For a lock-free concurrency
take a look at Immutable Data Structures, or [`ConcurrentDictionary<K,V>`](http://msdn.microsoft.com/en-us/library/dd287191.aspx).}


### GetOrThrow

Equivalent to using a dictionary indexer, but throws your own custom error message in a KeyNotFoundException when the
key is not found in the dictionary, or a better exception message thet informs about the dictionary type and missing key. 

```C#
public static V GetOrThrow<K, V>(this IDictionary<K, V> dictionary, K key)
public static V GetOrThrow<K, V>(this IDictionary<K, V> dictionary, K key, string messageWithFormat)
public static V GetOrThrow<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, Exception> exception)
```

Example:

```C#
Dictionary<string, List<Planet>> dictionary = planets.GroupToDictionary(a => a.Color);
List<Planet> redPlanets = dictionary.GetOrThrow("Red", "No planet with {0} color found"); 
//throws new KeyNotFoundException("No planet with Red color found")

List<Planet> redPlanets = dictionary.GetOrThrow("Red"); 
//throws new KeyNotFoundException("Key 'Red' (string) not found on Dictionary<string, List<Planet>>")
```

### AddOrThrow

Throws your own custom error message in a AddOrThrow when the
key is already in the dictionary:

```C#
//Message could contain a '{0}' to insert the key value
public static void AddOrThrow<K, V>(this IDictionary<K, V> dictionary, K key, V value, string messageWithFormat)
```

Example:

```C#
Dictionary<string, List<Planet>> dictionary = planets.GroupToDictionary(a => a.Color);
List<Planet> redPlanets = dictionary.AdOrThrow("Orange", "There's already {0} planets in the dictionary"); 

//throws new ArgumentException("There's already Orange planets in the dictionary") 
```


### SelectDictionary

Just a Select method for Dictionaries, taking two mapping functions, one
for keys and another for values, saving you to deal with KeyValuePairs.

```C#
//Overload that takes mapKey: k=>k'  mapValue: v=>v'
public static Dictionary<K2, V2> SelectDictionary<K1, V1, K2, V2>(this IDictionary<K1, V1> dictionary, Func<K1, K2> mapKey, Func<V1, V2> mapValue)

//Overload that takes mapKey: k=>k'  mapValue: (k,v)=>v'
public static Dictionary<K2, V2> SelectDictionary<K1, V1, K2, V2>(this IDictionary<K1, V1> dictionary, Func<K1, K2> mapKey, Func<K1, V1, V2> mapValue)
```

Example:

```C#
Dictionary<string, List<Planet>> dictionary = planets.GroupToDictionary(a => a.Color);
dictionary.SelectDictionary(k => k[0], v => v.Count).ToConsole();

//Writes: 
//[G, 1]
//[Y, 2]
//[B, 3]
//[O, 2]
```

*Note*: It's your responsibility to create a mapKey that preserves
uniqueness in the data set. i.e. If there were any Green planets this
operation will fail because G will conflict with Gray.

### ToDictionary with errorContext 

This sets of overloads mimics the ones provided by `System.Linq.Enumerable` class have an aditional parameter 'errorContext' and throws better exception messages:

```C#
public static Dictionary<K, T> ToDictionary<T, K>(this IEnumerable<T> source, Func<T, K> keySelector, string errorContext)
public static Dictionary<K, V> ToDictionary<T, K, V>(this IEnumerable<T> source, Func<T, K> keySelector, Func<T, V> elementSelector, string errorContext)
public static Dictionary<K, T> ToDictionary<T, K>(this IEnumerable<T> source, Func<T, K> keySelector, IEqualityComparer<K> comparer, string errorContext)
public static Dictionary<K, V> ToDictionary<T, K, V>(this IEnumerable<T> source, Func<T, K> keySelector, Func<T, V> elementSelector, IEqualityComparer<K> comparer, string errorContext)      
```

Example: 
```C#
new []{"Hi", "Bye", "Now"}.ToDictionary(s=>s.Length, "string lengths"); 
//throws ArgumentException(@"There are some repeated string lengths:
//3 (Bye, Now)");
```

Additionaly, an overload of ToDitionary with no lambda expression is provided to convert   `IEnumerable<KeyValuePair<K,V>>` directly. 

```C#
public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> collection)
public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> collection, string errorContext)
```

This method is specially useful to create efficient database queries that generate dictionaries, because Select is translated but ToDictionary is not. 

```C#

Database.Que<PersonEntity>().ToDictionary(p => p.Id, p => p.Name); //Retrieves whole persons! SLOW!! 
Database.Que<PersonEntity>().Select(p=>new { p.Id, p.Name }).ToDictionary(p => p.Id, p => p.Name);  //Efficient but to long
Database.Que<PersonEntity>().Select(p=>KVP.Create(p.Id, p.Name)).ToDictionary(); //Efficient and a little bit sorter
```

### JumpDictionary

Joins the Values of a dictionary with the Keys of another (if types match).

```C#
public static Dictionary<K, V> JumpDictionary<K, Z, V>(this IDictionary<K, Z> dictionary, Dictionary<Z, V> other)
```

Example:

```C#

Dictionary<Color, string> dicColors = new Dictionary<Color, string>()
{
   {Colors.Gray, "Gray"}, 
   {Colors.Yellow, "Yellow"},
   {Colors.Blue, "Blue"},
   {Colors.Orange, "Orange"}
};

Dictionary<string, List<Planet>> dictionary = planets.GroupToDictionary(a => a.Color);

dicColors.JumpDictionary(dictionary).ToConsole(kvp => "{0} -> {1}".FormatWith(kvp.Key, kvp.Value.ToString(", "))); 

//Writes: 
//#FF808080 -> Mercury (Gray)
//#FFFFFF00 -> Venus (Yellow), Saturn (Yellow)
//#FF0000FF -> Earth (Blue), Uranus (Blue), Neptune (Blue)
//#FFFFA500 -> Mars (Orange), Jupiter (Orange)
```


### JoinDictionary

Joins two dictionaries using the keys as the join key (should have the
same type), returns a new Dictionary and using mixer function to
generate the values.

If there are keys in one dictionary and not in the other, the keys will
not appear in the final dictionary (Inner Join).

```C#
public static Dictionary<K, V3> JoinDictionary<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1, V2, V3> mixer)
```

Example:

```C#
Dictionary<string, List<Planet>> dictionary = planets.GroupToDictionary(a => a.Color);

Dictionary<string, Color> dicColors = new Dictionary<string, Color>()
{
   {"Gray", Colors.Gray}, 
   {"Yellow", Colors.Yellow},
   {"Blue", Colors.Blue},
   {"Orange", Colors.Orange}
};

dicColors.JoinDictionary(dictionary, (n, c, list) => new 
{ 
  Color = c, 
  Planets = list.ToString(a => a.Name, ", ") 
}).ToConsole(); 

//Writes: 
//[Gray, { Color = #FF808080, Planets = Mercury }]
//[Yellow, { Color = #FFFFFF00, Planets = Venus, Saturn }]
//[Blue, { Color = #FF0000FF, Planets = Earth, Uranus, Neptune }]
//[Orange, { Color = #FFFFA500, Planets = Mars, Jupiter }]
```

### JoinDictionaryStrict

Joins to dictionaries by key, but throws a nice exception if the keys do not match **exactly**:

```C#
public static Dictionary<K, R> JoinDictionaryStrict<K, C, S, R>(
    Dictionary<K, C> currentDictionary,
    Dictionary<K, S> shouldDictionary,
    Func<C, S, R> resultSelector, string errorContext)

public static void JoinDictionaryForeachStrict<K, C, S>(
    Dictionary<K, C> currentDictionary,
    Dictionary<K, S> shouldDictionary,
    Action<K, C, S> mixAction, string errorContext)
```

Example:

```C#
Dictionary<string, List<Planet>> dictionary = planets.GroupToDictionary(a => a.Color);

Dictionary<string, Color> dicColors = new Dictionary<string, Color>()
{
   {"Gray", Colors.Gray}, 
   {"Yellow", Colors.Yellow},
   {"Blue", Colors.Blue},
   {"Orange", Colors.Orange}
};

dictionary.JoinDictionaryStrict(dicColors, (n, list, c) => new
{
   Color = c,
   Planets = list
}, "combining Planets"); 

//throws InvalidOperationException(@"Error combining Planets
// Extra: Magenta
// Missing: Yellow"
```

Useful to assert, at application start time, if the entities in the database match the ones in code.  


### OuterJoinDictionary

There's also support for OuterJoin with dictionaries. Because of the
recurrent problem with value types, there are many (nasty) overloads to
deal with value types and nullables:

```C#
public static Dictionary<K, V3> OuterJoinDictionaryCC<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1, V2, V3> mixer) where V1 : class where V2 : class

public static Dictionary<K, V3> OuterJoinDictionarySC<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1?, V2, V3> mixer) where V1 : struct where V2 : class
public static Dictionary<K, V3> OuterJoinDictionarySC<K, V1, V2, V3>(this IDictionary<K, V1?> dic1, IDictionary<K, V2> dic2, Func<K, V1?, V2, V3> mixer) where V1 : struct where V2 : class

public static Dictionary<K, V3> OuterJoinDictionaryCS<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1, V2?, V3> mixer) where V1 : class where V2 : struct
public static Dictionary<K, V3> OuterJoinDictionaryCS<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2?> dic2, Func<K, V1, V2?, V3> mixer) where V1 : class where V2 : struct

public static Dictionary<K, V3> OuterJoinDictionarySS<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1?, V2?, V3> mixer) where V1 : struct where V2 : struct
public static Dictionary<K, V3> OuterJoinDictionarySS<K, V1, V2, V3>(this IDictionary<K, V1?> dic1, IDictionary<K, V2?> dic2, Func<K, V1?, V2?, V3> mixer) where V1 : struct where V2 : struct
```

Example:

```C#
Dictionary<string, List<Planet>> dictionary = planets.GroupToDictionary(a => a.Color);

Dictionary<string, Color> dicColors = new Dictionary<string, Color>()
{
   {"Gray", Colors.Gray}, 
   {"Yellow", Colors.Yellow},
   {"Blue", Colors.Blue},
   {"Orange", Colors.Orange}
};

dicColors.OuterJoinDictionarySC(dictionary, (n, c, list) => new
{
   Color = c,
   Planets = list == null ? null : list.ToString(a => a.Name, ", ")
}).ToConsole(); 

//Writes: 
//[Gray, { Color = #FF808080, Planets = Mercury }]
//[Blue, { Color = #FF0000FF, Planets = Earth, Uranus, Neptune }]
//[Orange, { Color = #FFFFA500, Planets = Mars, Jupiter }]
//[Magenta, { Color = #FFFFA500, Planets =  }]
//[Yellow, { Color = , Planets = Venus, Saturn }]
```

*Note*: Notice how Magenta has no planets and Yellow has no color.


### AddRange

Adds many values in a dictionary at once. If a key already exists throws
an ArgumentException.

There are many overloads.

```C#
public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> collection)
public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values)
public static void AddRange<K, V, T>(this IDictionary<K, V> dictionary, IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
```

Aditionally, overloads exist that take an `errorContext`.

```C#

public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> collection, string errorContext)
public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values, string errorContext)
public static void AddRange<K, V, T>(this IDictionary<K, V> dictionary, IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector, string errorContext)
```

### SetRange

Sets many values in a dictionary at once. If a key already exists the
value is overridden.

There are many overloads.

```C#
public static void SetRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> collection)
public static void SetRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values)
public static void SetRange<K, V, A>(this IDictionary<K, V> dictionary, IEnumerable<A> collection, Func<A, K> getKey, Func<A, V> getValue)
```

### DefaultRange

Sets many values in a dictionary at once. If a key already exists the
value is **not** overridden and the default value is preserved.

There are many overloads.

```C#
public static void DefaultRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> collection)
public static void DefaultRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values)
public static void DefaultRange<K, V, A>(this IDictionary<K, V> dictionary, IEnumerable<A> collection, Func<A, K> getKey, Func<A, V> getValue)
```

### RemoveRange

Removes many key values entries at once given the collection of keys to
remove

```C#
public static void RemoveRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys)
```

### RemoveAll

Removes all the key-value pairs that satisfy a condition.

```C#
public static void RemoveAll<K, V>(this IDictionary<K, V> dictionary, Func<KeyValuePair<K, V>, bool> condition)
```

### Union

Unites two dictionaries creating a new one:

```C#
public static Dictionary<K, V> Union<K, V>(this IDictionary<K, V> dictionary, IDictionary<K, V> other)
```

### Extract

Moves all the entries in a dictionary that satisfies a condition to a
new one, removing them from the original one.

```C#
public static Dictionary<K, V> Extract<K, V>(this IDictionary<K, V> dictionary, Func<K, bool> condition)
public static Dictionary<K, V> Extract<K, V>(this IDictionary<K, V> dictionary, Func<K, V, bool> condition)
```

There's also another overloads that extracts one single element, returning the value:

```C#
public static V Extract<K, V>(this IDictionary<K, V> dictionary, K key)
public static V Extract<K, V>(this IDictionary<K, V> dictionary, K key, string messageWithFormat)
public static V Extract<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, Exception> exception)
```

### Inverse

Creates a new Dictionary using values as keys and keys as values. There are overloads to get good exception messaages if values are repeated.

```C#
public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic)
public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic, IEqualityComparer<V> comparer)
public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic, string errorContext)
public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic, IEqualityComparer<V> comparer, string errorContext)

### ToNameValueCollection

Converts the dictionary to a new NameValueCollection.

```C#
public static NameValueCollection ToNameValueCollection<K, V>(this IDictionary<K, V> dic)
```