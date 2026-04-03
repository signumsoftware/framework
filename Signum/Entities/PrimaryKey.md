# PrimaryKey struct

The structure `PrimaryKey` is used by `Entity` class (and `MListElement`) as the type of the `Id` property. 

```C#
public abstract class Entity : ModifiableEntity, IEntity
{
    [Ignore]
    internal PrimaryKey? id = null; //primary key
}
```

The mission of `PrimaryKey` is to provide the flexibility to any class that inherits from `Entity` to choose the type of the primary key in the database (`int`, `long`, `Guid`... or any other `IComparable`) using [PrimaryKeyAttribute](FieldAttributes.md), and at the same time let any code that manipulates the entity pass `PrimaryKey` structures around independently of this decision.

`PrimaryKey` only has one field, `IComparable Object` that contains the underlying value. 

```C#
[Serializable]
public struct PrimaryKey : IEquatable<PrimaryKey>, IComparable, IComparable<PrimaryKey>, ISerializable 
{
    public readonly IComparable Object;

    public PrimaryKey(IComparable obj)
    {
        if (obj == null)
            throw new ArgumentNullException("obj");

        this.Object = obj;
    }
}
````

This `Object` can not be `null`, in order to represent an optional `Id` a nullable `PrimaryKey?` is required.

### Overloaded operators

`PrimaryKey` is designed to get out of the way, letting you have the illusion that is the base class for all the possible value types (`int`, `long`, `Guid`...). In that sense: 

* It implements `IEquatable<T>` and `IComparable`. You can compare two `PrimaryKeys` to know if they are equal or witch one is bigger (even GUIDs can be ordered). **Note:** Comparing two `PrimaryKey` of different types throws an exception. 
* It provides implicit casting operators to convert **from** the three most common types (`int`, `long` and `Guid`), for any other type, the constructor can be used. There are also operators from the nullable versions of this three types, or the `PrimaryKey.Wrap` methods for the remaining.
* Is provides explicit casting operators to convert **to** the three most common types  (`int`, `long` and `Guid`), for any other type, just access the `Value` property. There are also operators to the nullable versions of the types and the `PrimaryKey.Unwrap` for the remaining.
* It overloads the comparison operators (`==`, `!=`, `<`, `<=`, `>` and `>=`) with other `PrimaryKey`.

### Parse and ToString

`PrimaryKey` struct contains a static [Polymorphic<Type>](../Signum.Utilities/Polymorphic.md) data-structure to know what is the type of the primary key of each entity class.

You can call `PrimaryKey.Parse` passing the type of the entity and it will parse the underlying value accordly. 

```C#
public static bool TryParse(string value, Type entityType, out PrimaryKey id)
public static PrimaryKey Parse(string value, Type type)
```

In order to convert the primary key to string, just use `ToString` or `TryToString`. 

