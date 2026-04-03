# ProgressEnumerator

Finally, when a `ProgressEnumerator` is 'inserted' between a `IEnumerable` producer and a consumer, it produces some statistics like percentage of completion, elapsed and remaining time, and estimated finish time. Handy in loading applications. 

`ProgressEnumerator` is not meant to be instantiated manually, but instead call `EnumerableExtensions.ToProgressEnumerator`.  

```C#
public static IEnumerable<T> ToProgressEnumerator<T>(this IEnumerable<T> source, out IProgressInfo pi)

public interface IProgressInfo
{
    double Percentage { get; }
    double Ratio { get; }
    TimeSpan Elapsed { get; }
    TimeSpan Remaining { get; }
    DateTime EstimatedFinish { get; }
    //Also, it has a nice ToString()
}
```

Example: 

```C#
IProgressInfo pi;
0.To(20).ToProgressEnumerator(out pi).ForEach(num =>
{
    Console.WriteLine(pi.ToString());
    Thread.Sleep(100); 
}); 
//Writes: 
//5,00% | 0h 00m 00s -> 09/02/2009 09:07:36
//10,00% | 0h 00m 01s -> 09/02/2009 09:07:38
//15,00% | 0h 00m 02s -> 09/02/2009 09:07:39
//20,00% | 0h 00m 02s -> 09/02/2009 09:07:39
//25,00% | 0h 00m 02s -> 09/02/2009 09:07:39
//30,00% | 0h 00m 02s -> 09/02/2009 09:07:39
//35,00% | 0h 00m 02s -> 09/02/2009 09:07:40
//40,00% | 0h 00m 02s -> 09/02/2009 09:07:40
//45,00% | 0h 00m 01s -> 09/02/2009 09:07:40
//50,00% | 0h 00m 01s -> 09/02/2009 09:07:40
//55,00% | 0h 00m 01s -> 09/02/2009 09:07:40
//60,00% | 0h 00m 01s -> 09/02/2009 09:07:40
//65,00% | 0h 00m 01s -> 09/02/2009 09:07:40
//70,00% | 0h 00m 01s -> 09/02/2009 09:07:40
//75,00% | 0h 00m 00s -> 09/02/2009 09:07:40
//80,00% | 0h 00m 00s -> 09/02/2009 09:07:40
//85,00% | 0h 00m 00s -> 09/02/2009 09:07:40
//90,00% | 0h 00m 00s -> 09/02/2009 09:07:40
//95,00% | 0h 00m 00s -> 09/02/2009 09:07:40
//100,00% | 0h 00m 00s -> 09/02/2009 09:07:40
```

Or even better, using SafeConsole.WriteSameLine to use the same as always. 

```C#
IProgressInfo pi;
0.To(20).ToProgressEnumerator(out pi).ForEach(num =>
{
    SafeConsole.WriteSameLine(pi.ToString());
    Thread.Sleep(100); 
}); 
```