# Disposable

This simple class implements `IDispoable` by executing an action taken as a parameter.

```C#
public class Disposable: IDisposable
{
    Action action;
    public Disposable(Action action)
    {
        if (action == null)
            throw new ArgumentNullException("action");

        this.action = action;
    }

    public void Dispose()
    {
        if (action != null)
            action(); 
    }
}
```

Handy for simple patterns with [using statement](http://msdn.microsoft.com/en-us/library/yh598w02.aspx) and lambdas, like this:

```C#
static IDisposable Time(string actionName)
{
    Console.WriteLine("Starting {0}", actionName); 
    Stopwatch sw = Stopwatch.StartNew();
    return new Disposable(() => 
    { 
       sw.Stop();
       Console.WriteLine("{0} took {1}", actionName, sw.Elapsed);
    });
}

(..)

using (Time("Siesta"))
{
   Console.WriteLine("zzZzZZ");
   Thread.Sleep(1000); 
}
//Writes: 
//Starting Siesta
//zzZzZZ
//Siesta took 00:00:01.0010197
```