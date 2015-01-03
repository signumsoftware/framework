# Server class

`Server` static class is the main class for the client application to communicate with the server. Internally it's just a holder of your Server WCF TransparentProxy. 

It's your responsibility to plug a WPF TransparentProxy that implements `IBaseServer` into `Server` static class using `SetNewServerCallback` as we seen in [Service]("../../Signum.Entities/Services/Services.md").

```C#
public static class Server
{
    public static void SetNewServerCallback(Func<IBaseServer> server);
}
```

## Execute and Return

Unfortunately, WPF Transparent Proxies can become a corrupt when a communication exception happens and they become unusable, the `Server` needs a way to restore communication. Two kind of exceptions could happen: 

* Session Expired: In this case the most convenient is to reconnect and retry the operation that causes the error. Maybe showing a log-in windows if necessary.
* Any Unexpected Exception: Abort and show to the user. 

In order to allow this retry functionality, the calls to the server have to be made using a lambda and calling `Serve.Execute` or `Server.Return`:


```C#
public static class Server
{
    public static void Execute<S>(Action<S> action) where S : class
    public static R Return<S, R>(Func<S, R> function) where S : class
}
```

In both cases, the parameter `S` corresponds to one particular interface implemented by your server, so by using this syntax we get two benefits:

   * The `Server` class automatically retries on case of Session Expired.
   * Your code becomes more modular, working in any application that has a service that implements this interface, without relying in one particular interface specific of one application.  

Example: 

```C#
Server.Return((IBaseServer s) => s.Retrieve(typeof(PersonEntity), id)); 
Server.Execute((IDynamicQueryServer s) => s.ExecuteQuery(request)); 
```

## Connecting event

`Server.Connecting` is a `Action` event can be used to download some information just after the server is connected. Think of it as the equivalent of `Schema.Initialize` for the client side. 

## OnOperation event
`Server.OnOperation` is a `Action<OperationContext>` event will be called on every `Execute` and `Return` method and gives you the opportunity to add some custom information. For example, the Isolation module uses it to implicitly transfer the current `IsolationEntity`. 


## Database-like methods

Additionally, `Server` class defines a set of static methods, some of them extensional, to simplify the client code by exposing some of the funcionality in the  

```C#
public static class Server
{
    public static T Save<T>(this T entidad) where T : Entity
    public static Entity Save(Entity entidad)

    public static bool Exists<T>(int id) where T : Entity
    public static bool Exists<T>(Lite<T> lite) where T : class, IEntity
    public static bool Exists<T>(T entity) where T : class, IEntity

    public static T Retrieve<T>(int id) where T : Entity
    public static Entity Retrieve(Type type, int id)
    public static T Retrieve<T>(this Lite<T> lite) where T : class, IEntity
    public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IEntity

    public static List<T> RetrieveAll<T>() where T : Entity
    public static List<Entity> RetrieveAll(Type type)

    public static Lite<T> FillToStr<T>(this Lite<T> lite) where T : class, IEntity

    public static List<Lite<Entity>> RetrieveAllLite(Type type)
    public static List<Lite<T>> RetrieveAllLite<T>() where T : class, IEntity    
}
```

## ServerTypes

`Server` class also provides a bunch of methods and public dictionaries to find the types registered in the server schema, and the implementations of any `PropertyRoute` (could potentially be polymorphic and overriden in the `SchemaSettings`). 

```C#
public static class Server
{
    public static Dictionary<Type, TypeEntity> ServerTypes { get; }
    public static Dictionary<string, Type> NameToType { get; }

    public static Type GetType(string cleanName) //Throws exception if not found
    public static Type TryGetType(string cleanName) //returns null if not found
    
    public static Implementations FindImplementations(PropertyRoute propertyRoute)
}
```

## Convert

Finally `Server` class exposes method to convert `Lite<T>` to `Entity` by calling `Retrieve` and back (`ToLite`):

```C#
public static class Server
{
    public static object Convert(object obj, Type type)
    public static bool CanConvert(object obj, Type type)
}
```