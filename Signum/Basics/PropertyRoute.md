# PropertyRoute

A `PropertyRoute` represents a sequence of `PropertyInfo` objects, starting from the `System.Type` of a root entity (either an `Entity` or a `ModelEntity`). It unambiguously identifies a logical database column.

There is only one canonical representation of a `PropertyRoute` because inheritance is not supported, and the sequence of `PropertyInfo` cannot traverse to a different `Entity`.

`PropertyRoute` is a key concept for property authorization and for overriding [FieldAttributes](FieldAttributes.md).

## PropertyRouteType

The `PropertyRouteType` enum defines the possible types of property routes:

- **Root:** The starting point of the route, e.g., `"(OrderEntity)"`.
- **FieldOrProperty:** The last segment is a `PropertyInfo` or `FieldInfo`. This is the only **complete** type of a `PropertyRoute`, e.g., `"(OrderEntity).CancellationDate"`.
- **Mixin:** A partial route accessing a Mixin, e.g., `"(OrderEntity)[CorruptMixin]"`.
- **LiteEntity:** A partial route accessing the `Entity` property of a `Lite<T>`, e.g., `"(OrderEntity).Employee.Entity"`.
- **MListItems:** A partial route accessing the indexer of an MList, e.g., `"(OrderEntity).Details[0]"`.

## Members

The main members of the `PropertyRoute` class are:

```csharp
public class PropertyRoute : IEquatable<PropertyRoute>, ISerializable
{
    public PropertyRouteType PropertyRouteType { get; }
    public FieldInfo? FieldInfo { get; } // Optional
    public PropertyInfo? PropertyInfo { get; } // Optional
    public PropertyRoute? Parent { get; } // Null for PropertyRouteType.Root

    public Type Type { get; } // The type returned by this PropertyRoute
    public Type RootType { get; } // The type of the top-most parent (Root)
}
```

## ToString and Parse

`PropertyRoute` provides methods for string representation and parsing:

```csharp
public class PropertyRoute
{
    public override string ToString(); // Returns e.g. '(OrderEntity).Details[0].SubTotalPrice'
    public string PropertyString(); // Returns just 'Details[0].SubTotalPrice'
    public static PropertyRoute Parse(Type rootType, string propertyString); // Parses a propertyString given the rootType
}

