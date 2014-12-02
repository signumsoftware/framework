# Constructor class

Constructor static class is used internally by Singnum.Windows when a create button (+) is pressed to create a new Entity. 

Why worry? Wasn't entity constructor designed for that purpose?

Actually yes, but maybe you need to make some initialization when the entity is constructed on the UI that should not apply in your business logic, tests or load applications. Some examples:

* Initialize some `DateTime` fields to `DateTime.Now`.
* Set the current user as the owner (or creator) of the entity.
* Initialize some embedded entities.
* Inherit `Isolation` from the previous entity. 

Additionally, nothing stops you from opening a modal window in the constructor lambda to ask the user for additional data or confirmation.

## Construct

There are two overloads of `Construct` method, one strongly-typed and one weak-typed.

```C#
public static class Constructor
{
    public static T Construct<T>(this ConstructorContext ctx) where T : ModifiableEntity

    public static ModifiableEntity ConstructUntyped(this ConstructorContext ctx, Type type)
}

public class ConstructorContext
{
    public Type Type { get; }
    public FrameworkElement Element { get; }
    public OperationInfo OperationInfo { get; }
    public List<object> Args { get;  }
    public bool CancelConstruction { get; set; }

    public ConstructorContext(FrameworkElement element = null, 
		OperationInfo operationInfo = null, List<object> args = null)
}
```

Notice how the constructor requires `ConstructorContext` with: 
* Optional `FrameworkElement` that represents the control or windows that requested the construction.
* Optional `OperationInfo` if any.
* An optional list of arguments can also be included. 

Example: 

```C#
void OrderLine_Create()
{
    return new ConstructorContext(this).Construct<OrderLineEntity>();
}
```

But normally you don't need to write such code, since the framework already does if for you. 

## Register

And, in order to register a special constructor for an entity, the `Register` method should be used.

```C#
public static class Constructor
{ 
    public static void Register<T>(Func<ConstructorContext, T> constructor)
            where T : ModifiableEntity
}
```

Example: 

```C#
Constructor.Register(ctx => new OrderLineEntity
{
   Quantity = 1, 
   Discount = 0,
}); 
```

## SurroundConstruct, PreConstructors and PostConstructors (Advanced)

Additionally, `PreConstructors` and `PostConstructors` events, in `ConstructionManager` can be registered globally to add code before or after the constructor is called. 

```C#
public event Func<ConstructorContext, IDisposable> PreConstructors;

public event Action<ConstructorContext, ModifiableEntity> PostConstructors;
```

An example of `PreConstructor` is what Isolation modules does: When constructing an entity and not working on a particular isolation, asks the user in which isolation should it be created, and then sets selected isolation for the rest of the construction (using `IDisposable`) or cancels the construction (using `CancelConstruction`). 

One example of `PostConstructor` is the `PostConstructors_AddFilterProperties`: When an entity is created from a `SearchControl`, copies the values of any `Equal` filter of a matching column to the new entity property. This behavior is registered by default buy you can unregister it if necessary.

Finally, could be necessary to run your particular hard-coded entity construction and initializes in a `PreConstructor` / `PostConstructo` environment, to give an opportunity of a module like Isolation do his magic. This can be done using `SurroundConstructor`. 

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
void OrderLine_Create()
{
    return new ConstructorContext(this).SurroundConstruct(ctx => new ProductEntity
	{
	   UnitsInStock = 0,
	});
}
```


