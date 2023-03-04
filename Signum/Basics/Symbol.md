# Symbols 


### Enums rules :)
Enums are really convenient to use because they are values know at compile-time, so we can reference them easily from our code with auto-completion, are compile-time checked and affected by refactorings.

```C#
if(order.State == OrderState.Shipped) //Nice :)

if(order.State == "Shipped") // 'stringly'-typed :(
```

While constant could also do all this, `enums` are smarter, because when you call `ToString` on them they remember their textual definition. 

```C#
DayOfWeek day = DayOfWeek.Monday;
day.ToString() // returns "Monday"!
```

So, enums are, at the same time, a value and and a member.

Additionally, the schema synchronizer ensures that the code and the database will be at sync, and the localization system gives a user-friendly description with `NiceToString`,  making them even more convenient. 

### Enums sucks :(

There's one important caveat however: Enum values have to be enumerated **all in one place** so they are not good to model things that can have more or less items as you register more modules, like a permission.

Imagine the Authorization module would create a `enum Permission` like this  

```C#
public enum Permission
{
   CreateUsers,
   ModifyUsers,
   DeleteUsers,
}

public bool IsAllowed(Permission permission) { ... }
```

Then there's no way for you, in your application, to add new items to this `enum Permission` (for example `CreateOrder`).

One possible solution is to use `Enum` (in Uppercase) instead of `Permission` in our `IsAllowed` method, but this solution is more weakly-typed and doesn't work in the database. 

### What is a Symbols?

`Symbols` are our answer to this problem. 

A `Symbol` is a class with a fixed amount of instances declared in static fields (like `enums`) but this static fields can be in different classes (unlike `enums`). 


### Declaring a Symbol Type

This is how you declare a new `Symbol` type, in this case `PermissionSymbol`. 

```C#
public class PermissionSymbol : Symbol
{
    private PermissionSymbol() { } 

    public PermissionSymbol(Type declaringType, string fieldName)
        : base(declaringType, fieldName)
    {
    }
}
```

### Declaring Symbol instances

And this is how you declare a new Symbol instance/value: 

```C#
[AutoInit]
public static class AuthorizationPermission
{
    public static PermissionSymbol CreateUsers;
    public static PermissionSymbol ModifyUsers;
    public static PermissionSymbol DeleteUsers;
}
```

But now we can create new instances of `PermissonSymbol` in another modules: 

```C#
[AutoInit]
public static class OrdersPermission
{
    public static PermissionSymbol CreateOrder;
}
```

So a symbol instance has two different types:
* The field type (in our example, `PermissionSymbol`).
* The type where it was declared (`AuthorizationPermission`, `OrdersPermission`, ...).

### Symbol.ToString

Some magic using `Signum.MSBuildTask` and `AutoInitAttribute` is done so symbols preserve the same smart `ToString` than `enums` have: 

```C#
PermissionSymbol permission = AuthorizationPermission.CreateUsers;
permission.ToString() // returns "AuthorizationPermission.CreateUsers"!
```

### Symbols are Entities

Aditionally, `Symbol` inherits from entities and can be stored and referenced in the database. 

They don't need an intermediary entity, like enum's `EnumEntity<T>`, but not all the declared Symbols are registered in the database, only the ones that *are used* by the different modules. 

`SymbolLogic<T>` is the class that is responsible of tracking what are the used symbols, usually by looking at some data structure where they are registered. This class is also able to parse symbols. 

```C#
public static class SymbolLogic<T>
    where T: Symbol
{
    public static void Start(SchemaBuilder sb, Func<IEnumerable<T>> getSymbols)

    public static ICollection<T> Symbols { get; }
    public static HashSet<string> AllUniqueKeys()

    public static T TryToSymbol(string key)
    public static T ToSymbol(string key)
}

```

Once the symbols are registered in this data structure, `SymbolLogic<T>` will generate / synchronize the table `T` to contain this particular symbols. 

Additionaly, (and this is an implementation detail) when the application initializes `SymbolLogic<T>` retrieves the Symbols from the database and asigns the Id's to each coresponging entity instance in the `static readonly` fields 

### Symbol.NiceToString

Symbols also have a user-friendly `NiceToString` definiton, that is pascal-spaced and localizable, making Symbols almost as convenient to use as `enums`. 

### Examples

There are already a few examples in Signum.Extensions of Symbols: 

* PermissionSymbol
* TypeConditionSymbol
* FileTypeSymbol
* ProcessAlgorithmSymbol
* SimpleTaskSymbol
* OperationSymbol (but is hidden inside of a type-safe container)
* ...

Consider creating a symbol if you create a reusable module with an expansible number of options/strategies/algorithms/commands that will be defined by the client code. 


## SemiSymbols

`SemiSymbols` is the *impure* brother of `Symbol`: A type that contain instances that behave like a `Symbol` and are instantiated in a `static readonly` field, and other instances that behave like a *Type of* entity. `AlertType` and `NoteType` are good examples because sometimes are used by the business logic (`Symbol`) and sometimes created by the user with run-time created types.   

In fact, we could classify entities depending their *staticness-dynamicness* like this: 


**UP: Static and frequently used** 

1. **Enums:** Can only created by the developer in control of the type. (i.e.: `OrderState`)
* **Symbols:** Can be created by any developer. (i.e.: `PermissionSymbol`)
* **SemiSymbols:** Can be created by any developer or user with administrative privileges. (i.e.: `AlertType`)
* **String Master Entities:** Can be created by any user with administrative privileges. (i.e.: `CountryEntity`)
* **Master Entities:** Can be created by any user with some administrative privileges. (i.e.: `ProductEntity`)
* **Transactional Entities:** Can be created by any user. (i.e.: `OrderEntity`)
* **Log Entities:** Automatically created by the system, and removed after some time. (i.e.: `OperationLogEntity`)

**DOWN: Dynamic and unfrequently used**
