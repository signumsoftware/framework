# Statics class

This class contains methods to create **thread and session static variables**. 


## Thread variables

Thread variables (`ThreadVariable<T>`) store information in the current thread, internally using `ThreadLocal<T>` class. 

`ThreadVariable<T>` are useful to override values in a scope of code, and are typically used in combination of methods returning an `IDisposable` interface and invoked inside `using` statements.

ThreadVariables are created using the `ThreadVariable<T>` static method.

```C#
public class Statics
{
    public static ThreadVariable<T> ThreadVariable<T>(string name)
}
```

```C#
public class ThreadVariable<T> : Variable<T>
{    
    public string Name { get; }

    public T Value { get; set; }
    public object UntypedValue { get; set; }

    public void Clean()
    public bool IsClean();
} 

```

Example: 

```C#
static readonly Variable<int?> scopeTimeout = Statics.ThreadVariable<int?>("scopeTimeout");
public static int? ScopeTimeout { get { return scopeTimeout.Value; } }

public static IDisposable CommandTimeoutScope(int? timeoutMilliseconds)
{
    var old = scopeTimeout.Value;
    scopeTimeout.Value = timeoutMilliseconds;
    return new Disposable(() => scopeTimeout.Value = old);
}
```


```C#

ScopeTimeout; //null

using(CommandTimeoutScope(200 * 1000))
{
   ScopeTimeout; // 200000  

    //Query taking long time here
}

ScopeTimeout; // null again
```

The benefit of using `ThreadVariable<T>` instead of plain `ThreadLocal<T>` or `[ThreadStaticAttribute]` are that all the defined `ThreadVariable<T>` are stored in a global dictionary allowing: 

### Import / Export Thread Conext

Using `ExportThreadContext` we can export all the thread values to a dictionary, annd using `ImportThreadContext` restore the in another thread. This is usefull for asynchronous code. 

```C#
public class Statics
{
    public static Dictionary<string, object> ExportThreadContext()
    public static IDisposable ImportThreadContext(Dictionary<string, object> context)
}
```

> **Note:** Importing and Exporting the context is similar to calling `ExecutionContext.Capture`/`Run` and store the values using `CallContext.LogicalSetData`/`LogicalGetData`. But AFAIK there's no way to use `LogicalSetData` without serialization (slow). 

### CleanThreadContextAndAssert

`CleanThreadContextAndAssert` method asserts that all the thread variables have been cleaned, cleaning them and throwing and exception if not. 

Since all the thread variables should be modified in methods invoked inside of `using` statements, they should be cleaned at the end of a ASP.Net request or WFC command, so by calling this method at this points we assert that they are properly called.

In this situation, the variables are also cleaned anyway to return the thread with default values to the thread pool. 

## Session variables  

Session variables store information in an abstract logical session. The session is just an abstraction witch actual meaning depends on the value in the SessionFactory property. 

```C#
public class Statics
{
    public static SessionVariable<T> SessionVariable<T>(string name)
    public static ISessionFactory SessionFactory {get; set; }
}
```

`SessionVariable<T>` can be set and get by accessing his `Value` property with no special ceremony (as opposed to `ThreadVariable<T>`, that are meant to be used in `using` statements).

```C#
public abstract class SessionVariable<T> : Variable<T>
{
    public string Name { get; }

    public T Value { get; set; }
    public object UntypedValue { get; set; }

    public void Clean()
    public bool IsClean();

    public abstract Func<T> ValueFactory { get; set; }
}
```

Example: 

```C#

public static class UserHolder
{
    public static readonly SessionVariable<IUserEntity> CurrentUserVariable = Statics.SessionVariable<IUserEntity>("user");
    public static IUserEntity Current
    {
        get { return CurrentUserVariable.Value; }
        set { CurrentUserVariable.Value = value; }
    }
}
```

```C#
UserHolder.Current = Database.Retrieve<UserEntity>(1);
//...
Console.WriteLine(UserHolder.Current);
```

### ValueFactory

Session variables also contain an optional `ValueFactory` property that will be called the first time the value is accessed to generate the initial value. You can use other Session variables (like `UserEntity.Current`) to calculate your own value. In this sense, `SessionVariable<T>` steal some behavior from `Lazy<T>`.   

### ISessionFactory

The actual meaning of 'session' depends on the object stored in `SessionFactory` static property, that has to be set just once at the very beginning of the application.     

* **`SingletonSessionFactory`:** This is the simplest type of session, just a static `Dictionary<string, object>` shared by all threads. Enough for Load applications and Tests. 

* **`ScopedSessionFactory`:** This session factory  wraps any other session giving the user the ability to override the session in a block of code using `ScopeSessionFactory.OverrideSession`. 
Internally is implemented, ironically, with a `ThreadVariable<Dictionary<string, object>>` and used intensively in WCF services. 

* **`AspNetSessionFactory`:** Implemented in Signum.Web, stores the session in `HttpContext.Current`. 

The default value for `SessionFactory` is `new ScopeSessionFactory(new SingletonSessionFactory())`. 



