# RecentDictionary 
`RecentDictionary` implements a Most Recently Used HashTable. It's usually handy to implement a cache.

Out implementation is based on the work of [Jim Wiese](http://www.codeproject.com/KB/recipes/mostrecentlyused.aspx). 

Externally it behaves just like a dictionary with two small differences: 
* The constructor has a capacity that limits the maximum number of items, the default is 50.  
* You can't trust that an item added to the dictionary will be there for long, if not used and capacity is reached, the element will be removed and Purged event fired.

Internally it has a `LinkedList` that contains all the values:
* Every operation (Add, Contains or the indexer) moves the element to the head
* When a new element is added, and the maximum capacity is reached, the element in the tail of the list is purged. 

```C#
public class RecentDictionary<K, V> 
{
    public RecentDictionary() //Default constructor, capacity = 50
    public RecentDictionary(int capacity) //Explicit capacity constructor

    public void Add(K key, V value) //Adds the key and value, moving the element to the head of the list
    public bool Contains(K key) //Returns true if the key is in the dictionary, moving the element to the head of the list 
    public void Remove(K key) //Explicitly removes an element from the dictionary

    
    public V this[K key]{get;set;} //Gets and sets the Value for the key. If the element is not there throws KeyNotFoundException
    public bool TryGetValue(K key, out V value) //.Net 2.0 style TryGetValue, if the element is not there returns false, otherwise value is filled. 
   
    public V GetOrCreate(K key, Func<V> createValue) //If the value is in the dictionary, it's retrieved. Otherwise is created using createValue and stored. 

    public int Capacity{get;set;} //Maximum number of elements  
    public int Count{get;} //Current number of elements

    public event Action<K,V> Purged //Thrown every time an element is removed because there's not enough space

    public override string ToString()
}
```

## Example
Let's imagine we have a few images that are being continuously retrieved in a non-homogeneous fashion.

```C#
DirectoryInfo di = new DirectoryInfo(@"C:\Users\Public\Pictures\Sample Pictures");
FileInfo[] files = di.GetFiles();
Random r = new Random();

Console.WriteLine(files.Length); 
//Writes 10

Stopwatch sw = new Stopwatch();
sw.Start();
for (int i = 0; i < 10000; i++)
{
    //This formula makes files at the end more probable than those at the beginning of the array
    int index = (int)Math.Sqrt(r.Next(files.Length * files.Length));
    string fileName = files[index].FullName;
    byte[] fileData = File.ReadAllBytes(files[index].FullName); //Expensive operations
}
sw.Stop();
Console.WriteLine(sw.Elapsed)
//Writes: 00:00:04.3913192
```

In order to improve performance, we just create a RecentDictionary and use GetOrCreate in our operation, using fileName as the key. 

This way, the most recent 4 items are kept

```C#
RecentDictionary<string, byte[]> images = new RecentDictionary<string, byte[]>(4);
sw.Reset();
sw.Start();
for (int i = 0; i < 10000; i++)
{
    int index = (int)Math.Sqrt(r.Next(files.Length * files.Length));
    string fileName = files[index].FullName;
    byte[] fileData = images.GetOrCreate(fileName, () => File.ReadAllBytes(fileName)); //Expensive and cached operations
}
sw.Stop();
Console.WriteLine("CacheSize {0} Time {1}".FormatWith(4, sw.Elapsed));
//CacheSize 4 Time 00:00:02.2218371
```

As you see, just by making a cache of 4 elements we have improved performance a 200%

If you are curious, here is the time it takes for different cache sizes in my laptop. 

```C#
//CacheSize 1 Time 00:00:04.0272807
//CacheSize 2 Time 00:00:03.6607723
//CacheSize 3 Time 00:00:02.8913571
//CacheSize 4 Time 00:00:02.2218371
//CacheSize 5 Time 00:00:01.7153703
//CacheSize 6 Time 00:00:01.1834980
//CacheSize 7 Time 00:00:00.7798797
//CacheSize 8 Time 00:00:00.3985586
//CacheSize 9 Time 00:00:00.1697666
//CacheSize 10 Time 00:00:00.0532788
```