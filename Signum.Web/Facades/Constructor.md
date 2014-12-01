# Constructor class

`Constructor` static class is used internally by Singnum.Web when an entity has to be created server-side in the controller (using C#) or client-side in the browser (using TypeScript). 

Why worry? Wasn't entity constructor designed for that purpose?

Actually yes, but maybe you need to make some initialization when the entity is constructed on the UI that should not apply to your business logic, tests or load applications. Some examples:

* Initialize some `DateTime` fields to `DateTime.Now`.
* Set the current user as the owner (or creator) of the entity.
* Initialize some embedded entities.
* Inherit `Isolation` from the previous entity. 

Additionally, using `TypeScript` nothing stops you from opening a popup in the client side to ask the user for additional data or confirmation.

The main difference between `Constructor` class in Signum.Windows and Signum.Web is that, in Signum.Windows there's just one class that handles all the client construction, optionally opening modal windows in the process, while in web is separated in two parts. Server-side and Client-side.


## SERVER-SIDE

The C# constructor API, represented by `ConstructorManager`, is responsible for constructing entities in the server when the request has been received from the client. It's too late already to show any modal window or ask the user for confirmation, so it's only responsibility is to build entities from a `ControllerBase` context. 

### Construct

There are two overloads of `Construct` method, one strongly-typed and one weak-typed.

```C#
public static class Constructor
{
    public static T Construct<T>(this ConstructorContext ctx) where T : ModifiableEntity

    public static ModifiableEntity ConstructUntyped(this ConstructorContext ctx, Type type)
}

public class ConstructorContext
{
    public Type Type { get; internal set; }
    public FrameworkElement Element { get; }
    public OperationInfo OperationInfo { get; }
    public List<object> Args { get; }
    public bool CancelConstruction { get; set; }

    public ConstructorContext(FrameworkElement element = null, OperationInfo operationInfo = null, List<object> args = null)
}
```

Notice how the constructor requires `ConstructorContext` with: 
* Optional `ControllerBase` that represents the controller that requested the construction.
* Optional `OperationInfo` if any.
* An optional list of arguments can also be included. 


An optional list of arguments can also be included. 

Example: 

```C#
public ActionResult Create(string webTypeName)
{
    Type type = Navigator.ResolveType(webTypeName);

    var entity = new ConstructorContext(this).ConstructUntyped(type);

    ...
}
```

But normally you don't need to write such code, since the framework already does if for you. 

### Register

And, in order to register a special constructor for an entity, the `Register` method should be used.

```C#
public static class Constructor
{ 
    public static void Register<T>(Func<ConstructorContext, T> constructor) where T:ModifiableEntity
}

public class ConstructorContext
{
    public Type Type { get; }
    public ControllerBase Controller { get; }
    public List<object> Args { get; }
}
```

Example: 

```C#
Constructor.Register(ctx => new OrderLineDN
{
   Quantity = 1, 
   Discount = 0,
}); 
```

### SurroundConstruct, PreConstructors and PostConstructors (Advanced)

Additionally, `PreConstructors` and `PostConstructors` events, in `ConstructionManager` can be registered globally to add code before or after the constructor is called. 

```C#
public event Func<ConstructorContext, IDisposable> PreConstructors;
public event Action<ConstructorContext, ModifiableEntity> PostConstructors;
```

An example of server-side `PreConstructor` is what Isolation modules does: When construcing an entity and not working in a particular isolation, gets the isolation parameter passed by the client-side pre-constructor as a request parameters, and surrounds the entity construction in a scope that uses this isolation (using `IDisposable`).

One example of server-side `PostConstructor` is the `PostConstructors_AddFilterProperties`: When an entity is created from a `SearchControl`, copies the values of any `Equal` filter of a matching column to the new entity property, because the `FilterOption` has been implicitly passed as a parameter in the request. This behavior is registered by default buy you can unregister it if necessary.

Finally, could be necessary to run your particular hard-coded entity construction and initializes in a `PreConstructor` / `PostConstructor` environment, to give an opportunity of a module like Isolation do his magic. This can be done using `SurroundConstructor`. 

```C#
public static class Constructor
{ 
    public static T SurroundConstruct<T>(this ConstructorContext ctx, 
		Func<ConstructorContext, T> constructor) where T : ModifiableEntity
    
    public static ModifiableEntity SurroundConstructUntyped(this ConstructorContext ctx, Type type, 	
		Func<ConstructorContext, ModifiableEntity> constructor)
}
```

Example: 

```C#
public ActionResult Create(string webTypeName)
{
    Type type = Navigator.ResolveType(webTypeName);

    var entity = this.ConstructUntyped(type);

    ...
}
```

```C#
public ActionResult Create()
{
    var orderLine = new ConstructorContext(this).SurroundConstruct(ctx => new ProductDN
	{
	   UnitsInStock = 0,
	});
    ...
}
```


## CLIENT-SIDE

Client Side construction pipeline let you attach general or entity-specific scripts that will be included before creating an entity in the browser. 

An example of an entity-specific construction work-flow could be a task, that depending on the selected task type (Bug, Deployment, Coding...) different fields will be visible or editable, and it makes sense to ask the user for this information first, and then pass this information to the server to create the entity and the view properly. 

An example of using a global constructor is Isolation module, if the user is a multi-isolation user it requires to ask in which isolation should the entity be created for any entity that is isolated. 

In both cases, the scripts will be registered as `PreConstructors` in `Constructor.ClientManager`, and will take effect whenever the entity is constructed in the client, using the (+) button in an `EntityBase` or an `SearchControl`. 

```C#
public class ClientConstructorManager
{
    public event Func<ClientConstructorContext, JsFunction> GlobalPreConstructors;

    public Dictionary<Type, Func<ClientConstructorContext, JsFunction>> PreConstructors = 
       new Dictionary<Type, Func<ClientConstructorContext, JsFunction>>();
}
```

The `JsFunction` should function that takes a `extraArgs` object (just a JSON dictionary) and returns a **promise** to a new version of `extraArgs`, or `null` to stop the construction process. If more than one script is registered globally or for one specific entity, the method invocations will be chained together. 

Example: 

```C#
//In IsolationClient
public static JsModule Module = new JsModule("Isolation");

//In IsolationClient.Start
Constructor.ClientManager.GlobalPreConstructors += ctx =>
    Module["getIsolation"](ClientConstructorManager.ExtraJsonParams, ctx.Prefix,
    IsolationMessage.SelectAnIsolation.NiceToString(), 
    IsolationLogic.Isolations.Value.Select(iso => iso.ToChooserOption()));
```

```Typescript
//In Isolation.ts
export function getIsolation(extraJsonData: any, prefix: string, title: string, isolations: Navigator.ChooserOption[]) : Promise<any> {

    var iso = getCurrentIsolation(prefix);

    if (iso != null)
        return Promise.resolve(<any>$.extend(extraJsonData, { Isolation: iso }));

    return Navigator.chooser(prefix, title, isolations).then(co=> {
        if (!co)
            return null;

        return <any>$.extend(extraJsonData, { Isolation: co.value })
    });
}
```



  


