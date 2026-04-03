
# ReferenceEqualityComparer

By default the default implementation of `Equals` and `GetHashCode` for all .Net uses the object reference, but when you override both of the methods you get the default implementation lost.

i.e: `Entity` for example overrides it to use Type and Id for convenience when writing business logic.

`ReferenceEqualityComparer` allows you to change back this behavior in whatever data structure or algorithm that takes an `IEqualityComparer`.    

```C#
public class ReferenceEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer where T : class
{
   public static ReferenceEqualityComparer<T> Default {get; }
}
```

Example: 

```C#
var memoryGraph = new DirectedGraph<Modifiable>(ReferenceEqualityComparer<Modifiable>.Default);
```