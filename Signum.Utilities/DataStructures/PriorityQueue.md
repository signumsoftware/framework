# PriorityQueue

A [priority queue](http://en.wikipedia.org/wiki/Priority_queue) is a data structure similar to a queue, but where every element has priority associated. At any given moment the object with less (or most) priority is retrieved.

It's usually implemented using a [heap](http://en.wikipedia.org/wiki/Heap_(data_structure)). Our version, based on the work of [BenDi](http://www.codeproject.com/KB/recipes/priorityqueue.aspx), uses a [binary heap](http://en.wikipedia.org/wiki/Binary_heap) in a flat List, and instead of setting priority explicitly for each element we use `IComparable<T>` interface of `Comparison<T>` delegate. Let's see  what it looks like!

```C#
public class PriorityQueue<T>
{
    List<T> list = new List<T>(); //where the elements are actually stored
    Comparison<T> comparer; //The comparison delegate used internally
    
    //Default constructor, returns default comparator for a T that implements IComparable
    public PriorityQueue():this(Comparer<T>.Default.Compare) 
    //Constructor with explicit IComparer (Think about using LambdaComparer)
    public PriorityQueue(IComparer<T> comparer) 
    //Constructor with explicit Comparisor
    public PriorityQueue(Comparison<T> comparer)

    public int Count{get;} // number of elements in the queue
    public bool Empty{get;} //returns true if no element is in the queue

    public int Push(T element) //Enqueue an element
    public void PushAll(IEnumerable<T> elements) //Enqueue all the elements 
    public T Pop() //Dequeue and returns the smallest element 
    public T Peek() //Returns the smallest element without dequeuing 
      
    //Forces a position update of an element already in the queue
    //Useful if its' comparison value has changed some how
    public void Update(T element) 

    public bool Contains(T element) //returns true if element is in the queue
    public void Clear() //Removes all the elements in the queue
}
```

Example: 

```C#
PriorityQueue<int> numbers = new PriorityQueue<int>(); 
numbers.Push(1);
numbers.Push(10);
numbers.Push(8);
numbers.Push(12);
numbers.Push(4);
numbers.Push(54);

while (!numbers.Empty)
    Console.Write(numbers.Pop() + ", ");

//Writes: 1, 4, 8, 10, 12, 54,
```

Example using an explicit `IComparer` (`LambdaComparer`):

```C#
PriorityQueue<string> numbers = new PriorityQueue<string>(new LambdaComparer<string, int>(s => s.Length));
numbers.Push("Teutons");
numbers.Push("Mongols");
numbers.Push("Persians");
numbers.Push("Sarracens");
numbers.Push("Goths");
numbers.Push("Japanese");
numbers.Push("Celts");
numbers.Push("Chinese");
numbers.Push("Wikings");
numbers.Push("Britons");
numbers.Push("Turks");
numbers.Push("Franks");
numbers.Push("Byzantines");

while (!numbers.Empty)
    Console.WriteLine(numbers.Pop());

//Goths
//Turks
//Celts
//Franks
//Britons
//Chinese
//Vikings
//Teutons
//Mongols
//Persians
//Japanese
//Sarracens
//Byzantines
```

> **Note:** Note that, in the current implementation, the priority is the only information taken into account, not the order they where introduced.