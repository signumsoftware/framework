# Signum.Utilities.GroupExtensions class

When doing a Loading process from legacy database, very often poorly
normalized, grouping turns out being one of the most useful operations.

There are so many extensions for Grouping over IEnumerable that is
worth taking them out to a different class: GroupExtensions. (since they
are extension methods they are used the same way).

We will use the following data to make the examples:

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

### GroupToDictionary

Mixes GroupBy and `ToDictionary<K, List<V>>` because
Dictionaries are richer collections, with more features than
`IEnumerable<IGrouping<K,V>>`.

```C#
public static Dictionary<K, List<T>> GroupToDictionary<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
{
   return collection
     .GroupBy(keySelector)
     .ToDictionary(g => g.Key, g => g.ToList());
}

public static Dictionary<K, List<V>> GroupToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
```

Example:

```C#
Dictionary<string,List<Planet>> planetsByColor = planets.GroupToDictionary(a=>a.Color);

//Print results:
Console.WriteLine(planetsByColor.ToString(kvp => "{0}: {1}".FormatWith(kvp.Key, kvp.Value.ToString(", ")), "\r\n")); 

//Writes...
//Gray: Mercury (Gray)
//Yellow: Venus (Yellow), Saturn (Yellow)
//Blue: Earth (Blue), Uranus (Blue), Neptune (Blue)
//Orange: Mars (Orange), Jupiter (Orange)
```

Also, on Loading applications, when trying to normalize a given field
often it is useful to group by the field and to order the groups
descending by number of elements, so you can focus on the most important
values of the field.

```C#
public static Dictionary<K, List<T>> GroupToDictionaryDescending<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
public static Dictionary<K, List<V>> GroupToDictionaryDescending<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
```

> **Note:** When you enumerate a Dictionary the KeyValuePairs are returned in the order they were introduced.

Example:

```C#
Dictionary<string,List<Planet>> planetsByColor = planets.GroupToDictionaryDescending(a=>a.Color);

//Print results:
Console.WriteLine(planetsByColor.ToString(kvp => "{0}: {1}".FormatWith(kvp.Key, kvp.Value.ToString(", ")), "\r\n")); 

//Writes...
//Blue: Earth (Blue), Uranus (Blue), Neptune (Blue)
//Yellow: Venus (Yellow), Saturn (Yellow)
//Orange: Mars (Orange), Jupiter (Orange)
//Gray: Mercury (Gray)
```

### GroupDistinctToDictionary

Sometimes, when normalizing a legacy database, you want to assert that a
given column is Unique for all the elements in a collection.
GroupDistinctToDictionary throws a nice exception when more than one
element appears in a given group.

```C#
public static Dictionary<K, T> GroupDistinctToDictionary<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
public static Dictionary<K, V> GroupDistinctToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
```

```C#
Dictionary<int, Planet> planetByRevolutionDays = planets.GroupDistinctToDictionary(a => a.RevolutionDays);
planetByRevolutionDays.ToConsole();

//Writes:
//[88, Mercury (Gray)]
//[225, Venus (Yellow)]
//[365, Earth (Blue)]
//[687, Mars (Orange)]
//[4380, Jupiter (Orange)]
//[10585, Saturn (Yellow)]
//[30660, Uranus (Blue)]
//[60225, Neptune (Blue)]


//But
Dictionary<int, Planet> planetByRotationDays = planets.GroupDistinctToDictionary(a=>a.RotationDays);

//throws InvalidOperationException("There is more than one element with key: 10");
```

### GroupCount

Shortcut to create a dictionary from Key -\> NumberOfElements that
contain the key.

```C#
public static Dictionary<K, int> GroupCount<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
```

There's also another overload without keySelector that can be used to
look for repetitions of a value.

```C#
public static Dictionary<T, int> GroupCount<T>(this IEnumerable<T> collection)
```

Example:

```C#
Dictionary<string, int> diameterToCount = planets.GroupCount(a => a.Diameter);
diameterToCount.ToConsole(); 

//Writes:
//[Small, 4]
//[Big, 2]
//[Medium, 2]
```

### AgGroupToDictionary

Finally, AgGroupToDictionary takes a lambda to collapse all the values
in a group, using this collapsed value as the value of the dictionary.

```C#
public static Dictionary<K, V> AgGroupToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<IGrouping<K, T>, V> agregateSelector)
```

Example: 
```C#
Dictionary<string, double> colorToAvgRevDays = planets.AgGroupToDictionary(p => p.Color, gr=>gr.Average(p=>p.RevolutionDays));
colorToAvgRevDays.ToConsole(); 

//Writes
//[Gray, 88]
//[Yellow, 5405]
//[Blue, 30416,6666666667]
//[Orange, 2533,5]
```

There's also another overload that orders the groups descending by the
number of elements of each one, so you can focus on the groups with more
elements.

```C#
public static Dictionary<K, V> AgGroupToDictionaryDescending<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<IGrouping<K, T>, V> agregateSelector)
```

Example:

```C#
Dictionary<string, double> colorToAvgRevDays = planets.AgGroupToDictionaryDescending(p => p.Color, gr=>gr.Average(p=>p.RevolutionDays));
colorToAvgRevDays.ToConsole(); 

//Writes
//[Blue, 30416,6666666667]    (there are 3)
//[Yellow, 5405]              (there are 2) 
//[Orange, 2533,5]            (there are 2) 
//[Gray, 88]                  (just 1) 
````


### GroupWhen

Given a flat collection of items, GroupsWhen creates a new group every
time isGroupKey evaluates to true, filling the group with all the
following elements.

All the initial elements are ignored until the first isGroupKey is
found.

```C#
public static List<List<T>> SplitList<T>(this IEnumerable<T> collection, Func<T, bool> isSplitter)
```

Example, let's suppose we have a text file named “countries.txt” like
this:

```C#
Countries by region: 
#Asia
China
Japan
India
#Europe
Germany
France
Spain
#North America
USA
Canada
Mexico
```

Then we can associate each continent with the following countries like
this:

```C#
 File.ReadAllLines("countries.txt")
 .GroupWhen(s => s.StartsWith("#"))
 .Select(g => new
 {
   Region = g.Key.Substring(1),
   Countries = g.ToString(", ")
 }).ToConsoleTable("-=Results=-"); 
```

Printing in the Console:

```C#
-=Results=-
Region        Countries
Asia          China, Japan, India
Europe        Germany, France, Spain
North America USA, Canada, Mexico
```

### GroupWhenChange

Given a flat collection of items, GroupsWhen creates a new group every
time getGroupKey change its value, filling the group with all the
following elements.

```C#
public static IEnumerable<IGrouping<K, T>> GroupWhenChange<T, K>(this IEnumerable<T> collection, Func<T, K> getGroupKey)
```

Example, let's suppose we have a text file named “countries.txt” like
this:

```C#
Countries by region: 

Asia China
Asia Japan
Asia India
Europe Germany
Europe France
Europe Spain
NorthAmerica USA
NorthAmerica Canada
NorthAmerica Mexico
```

Then we can associate each continent with the following countries like
this:

```C#
 File.ReadAllLines("countries.txt")
 .GroupWhen(s => s.Before(" "))
 .Select(g => new
 {
   Region = g.Key,
   Countries = g.ToString(g=>g.After(" "), ", ")
 }).ToConsoleTable("-=Results=-"); 
```

Printing in the Console:

```C#
-=Results=-
Region        Countries
Asia          China, Japan, India
Europe        Germany, France, Spain
North America USA, Canada, Mexico
```

### GroupsOf

Groups the elements taken them by their current order and making groups of `groupSize` each time.  Useful to split database queries with too many parameters. 

```C#
public static IEnumerable<List<T>> GroupsOf<T>(this IEnumerable<T> collection, int groupSize)
```
```C#
new []{1,2,3,4,5,6,7,8,9}.GroupsOf(3);
[[1,2,3], [4,5,6], [7,8,9]]
```

There's another overload taking the weight.

```C#
public static IEnumerable<List<T>> GroupsOf<T>(this IEnumerable<T> collection, Func<T, int> elementSize, int groupSize)
```

If an element size alone is bigger than the limit, the element is included alone anyway. 

```C#
new []{1,2,3,4,5,6,7,8,9}.GroupsOf(i=>i, 10);
[[1,2,3], [4,5], [6], [7], [8], [9]]
```


### IntervalsOf
Given a sequence of int (i.e. Ids in a database), orders them and creates Intervals of size groupSize with the minimum and the maximum. 

Useful to split queries over big numbers of disperse ids (like the Reseeded ids used in Disconnected module)  

```C#
public static IEnumerable<Interval<int>> IntervalsOf(this IEnumerable<int> collection, int groupSize)
```

```C#
new []{1,2,3,4,10,11,12,13,100,1001,1002}.IntervalsOf(5);
[(1,11), (11,1002), (1002, 1003)]
```

