# Disposable

This simple class implements `IDisposable` by executing an action taken as a parameter.

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

## Disposable.Combine

Combines two disposables into one that executes the first one and the seccond one if both are non-null. 

```C#
public static IDisposable Combine(IDisposable first, IDisposable second)
{
    if (first == null || second == null)
        return first ?? second;

    return new Disposable(() => { try { first.Dispose(); } finally { second.Dispose(); } });
}
```

A more complicated variation allows the typical use case: Combine events that return `IDisposable`.

```C#
 public static IDisposable Combine<Del>(Del delegated, Func<Del, IDisposable> invoke)
```

Example:


```C#
public static event Func<Pop3ConfigurationEntity, IDisposable> SurroundReceiveEmail;

public static Pop3ReceptionEntity ReceiveEmails(this Pop3ConfigurationEntity config)
{
    using (Disposable.Combine(SurroundReceiveEmail, func => func(config)))
	{
	    //...
	}
}
```
