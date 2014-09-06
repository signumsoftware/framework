# Constructor class

Constructor static class is used internally by Singnum.Windows when a create button (+) is pressed to create a new Entity. 

Why worry? Wasn't entity constructor designed for that purpose?

Actually yes, but maybe you need to make some initialization when the entity is constructed on the client that should not apply in the server, tests or load applications. Some examples:

* Initialize some `DateTime` fields to `DateTime.Now`.
* Set the current user as the owner (or creator) of the entity.
* Initialize some embedded entities.
* Inherit `Isolation` from the previous entity. 

## Construct

There are two overloads of `Construct` method, one strongly-typed and one weak-typed.

```C#
public static class Constructor
{
    public static ModifiableEntity Construct(this FrameworkElement element, Type type, List<object> args = null)
    public static T Construct<T>(this FrameworkElement element, List<object> args = null)
       where T : ModifiableEntity
}
```

Notice how the constructor requires an optional `FrameworkElement` that represents the control or windows that requested the construction, if any.

An optional list of arguments can also be included. 

Example: 

```C#
void OrderLine_Create()
{
    return this.Construct<OrderLineDN>();
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

public class ConstructorContext
{
    public Type Type { get; }
    public FrameworkElement Element { get; }
    public List<object> Args { get; }
    public bool CancelConstruction { get; set; }
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

## SurroundConstruct, PreConstructors and PostConstructors (Advanced)

Aditionally, `PreConstructors` and `PostConstructors` can be registered globaly to add code before or after the constructor is called. 

```C#
public event Func<ConstructorContext, IDisposable> PreConstructors;
public event Action<ConstructorContext, ModifiableEntity> PostConstructors;
```

An example of `PreConstructor` is what Isolation modules does: When constructing an entity and not working on a particular isolation, asks the user in which isolation should it be created, and then sets selected isolation for the rest of the construction (using `IDisposable`) or cancels the construction (using `CancelConstruction`) 

One example of `PostConstructor` is the `PostConstructors_AddFilterProperties`, implemented in `ConstructorManager` that copies the values of any `Equal` filter of a matching column to the new entity when constructed from a `SearchControl`. This behaviour is registered by default buy you can un-register it necessary.

Finally, could be necessary to run your particular hard-coded entity construction and initializes in a `PreConstructor` / `PostConstructo` environment, to give an opportunity of a module like Isolation do his magic. This can be done using `SurroundConstructor`. 

```C#
public static class Constructor
{ 
    public static T SurroundConstruct<T>(this FrameworkElement element, 
		Func<ConstructorContext, T> constructor) where T : ModifiableEntity
    public static T SurroundConstruct<T>(this FrameworkElement element, List<object> args,  
        Func<ConstructorContext, T> constructor) where T : ModifiableEntity
    public static ModifiableEntity SurroundConstruct(this FrameworkElement element, Type type, List<object> args, 
        Func<ConstructorContext, ModifiableEntity> constructor)
}
```

Example: 

```C#
void OrderLine_Create()
{
    return this.SurroundConstruct(ctx=>new OrderLineDN
	{
	   Quantity = 1, 
	   Discount = 0,
	});
}
```


