## ThreadSafeEnumerator

Have you ever considered why there's two different interfaces for enumeration, `IEnumerable<T>` and `IEmunerator<T>`?

In order to enumerate a collection you need to keep some data (the current item, current index, current node...) this information does not belong to the collection itself, because otherwise you couldn't enumerate it from different threads at the same time (or twice in the same thread, a nested loop for example).

But sometimes you want to make different threads work over the same `IEnumerator` at the same time, so that each element is yielded to just one of the consumer threads and 'no element is left behind'.

```C#
public class TreadSafeEnumerator<T>: IEnumerable<T>, IEnumerator<T>
{
    public TreadSafeEnumerator(IEnumerable<T> source)
    
    (...) //interface implementation and threading code
}
```

This is not exactly the same than `Parallel.Foreach`, since it is still an `IEnumerable` so you can append a.. 'Where' statement for some thread. 

It's also not the same thing as [IParallelEnumerable](http://msdn.microsoft.com/en-us/magazine/cc163329.aspx) interface, the root of PLINQ, a much more ambitious initiative to parallelize every Linq-to-Objects operator.

It just distributes the elements of the source enumerator to all the interested threads, like cards in a deck. 

Also, it's not meant to be instantiated manually, instead use the more convinient `AsThreadSafe` method in `EnumerableExtensions`.

```C#
public static IEnumerable<T> AsThreadSafe<T>(this IEnumerable<T> source)
```

Example:

```C#
IEnumerable<int> numbers = 0.To(100);

IEnumerable<int> threadSafeNumbers = numbers.AsThreadSafe();

Thread[] threads = 0.To(10).Select(i => new Thread(() =>
{
    foreach (var num in threadSafeNumbers)
    {
        Console.WriteLine("{0} Getting {1}".FormatWith(Thread.CurrentThread.Name, num));
        Thread.Sleep(100); //To force some thread changes
    }
}) { Name = "Thread #" + i }).ToArray();

threads.ForEach(a => a.Start());
threads.ForEach(a => a.Join()); 
//Writes: 
//Thread #0 Getting 0
//Thread #2 Getting 1
//Thread #1 Getting 3
//Thread #3 Getting 2
//Thread #5 Getting 4
//Thread #4 Getting 5
//Thread #6 Getting 6
//Thread #7 Getting 7
//Thread #8 Getting 8
//Thread #9 Getting 9
//Thread #0 Getting 11  Start to get our of order!
//Thread #2 Getting 10
//Thread #5 Getting 12
//Thread #1 Getting 14
//Thread #4 Getting 15
//Thread #3 Getting 13
//Thread #6 Getting 16
//Thread #7 Getting 17
//Thread #8 Getting 18
//Thread #9 Getting 19
//Thread #0 Getting 20
//Thread #2 Getting 21
//Thread #5 Getting 22
//Thread #6 Getting 23
//Thread #7 Getting 27
//Thread #1 Getting 26
//Thread #3 Getting 24
//Thread #8 Getting 28
//Thread #4 Getting 25
//Thread #9 Getting 29
//Thread #2 Getting 30
//Thread #0 Getting 31
//Thread #5 Getting 32
//Thread #6 Getting 34
//Thread #7 Getting 33
//Thread #1 Getting 35
//Thread #3 Getting 36
//Thread #4 Getting 38
//Thread #9 Getting 39
//Thread #8 Getting 37
//Thread #2 Getting 40
//Thread #0 Getting 41
//Thread #5 Getting 42
//Thread #6 Getting 43
//Thread #7 Getting 44
//Thread #1 Getting 45
//Thread #3 Getting 46
//Thread #9 Getting 47
//Thread #8 Getting 49
//Thread #4 Getting 48
//Thread #2 Getting 50
//Thread #0 Getting 51
//Thread #5 Getting 52
//Thread #6 Getting 53
//Thread #7 Getting 54
//...
```


Look how the same enumerator ({{0.To(100)}}) is accessed from 10 different threads, and every element goes to just one thread and no element is lost. 
