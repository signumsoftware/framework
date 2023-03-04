# PropertyRoute

A `PropertyRoute` is a sequence of `PropertyInfo`, starting from a `System.Type` of an root entity (a `Entity` or a `ModelEntity`) that ultimately refers unambiguously to a logical database column. 

There's just one canonical representation of a `PropertyRoute` because inheritance is not supported, neither the sequence of `PropertyInfo` can travel to a different `Entity`. 

A `PropertyRoute` is an important concept to authorize properties, and override [FieldAttributes](FieldAttributes.md).

### PropertyRouteType

There are just a few different types of property routes, defined by `PropertyRouteType` enum:

* **Root:** Where the root starts. i.e: `"(OrderEntity)"` 
* **FieldOrProperty:** The last part has a `PropertyInfo` or `FieldInfo`.This is the only **complete** type of a `PropertyRoute`. i.e: `"(OrderEntity).CancellationDate"`. 
* **Mixin:** Partial route accessing a Mixin. Like `"(OrderEntity)[CorruptMixin]"` 
* **LiteEntity:** Partial route accessing the property `Entity` of a `Lite<T>`. Like `"(OrderEntity).Employee.Entity"`.
* **MListItems:** Partial route accessing the indexer of a `Lite<T>`. Like `"(OrderEntity).Details[0]"`.

### Members

Here are the members of a `PropertyRoute`: 

```C#
public class PropertyRoute : IEquatable<PropertyRoute>, ISerializable
{
    public PropertyRouteType PropertyRouteType { get; }
    public FieldInfo FieldInfo { get; } // optional
    public PropertyInfo PropertyInfo { get; } // optional
    public PropertyRoute Parent { get; } // null for PropertyRouteType.Root

    public Type Type { get; } // returning type of this PropertyRoute
    public Type RootType { get; } // Type of the top-most parent (Root)
}
```


### ToString and Parse

`PropertyRoute` have `ToString` defined and can also be parsed: 

```C#
public class PropertyRoute
{
    public override string ToString() //returns '(OrderEntity).Details[0].SubTotalPrice'
    public string PropertyString() //returns just: 'Details[0].SubTotalPrice'
    public static PropertyRoute Parse(Type rootType, string propertyString) //parses a propertyString given the rootType
}
``` 

