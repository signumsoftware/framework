# Symbols

## Why Not Just Enums?

Enums are convenient because their values are known at compile time, enabling easy referencing, auto-completion, compile-time checking, and support for refactoring.

```csharp
if (order.State == OrderState.Shipped) // Good: strongly-typed
if (order.State == "Shipped") // Bad: stringly-typed
```

Enums are also smart: calling `ToString()` on an enum returns its textual name.

```csharp
DayOfWeek day = DayOfWeek.Monday;
day.ToString(); // returns "Monday"
```

Additionally, the Signum schema synchronizer ensures code and database enums stay in sync, and the localization system provides user-friendly descriptions with `NiceToString`.

However, enums have a limitation: all values must be declared in one place. This makes them unsuitable for extensible concepts, such as permissions, where new values may be needed by different modules.

For example, if the Authorization module defines:

```csharp
public enum Permission
{
    CreateUsers,
    ModifyUsers,
    DeleteUsers,
}

public bool IsAllowed(Permission permission) { ... }
```

You cannot add new permissions (e.g., `CreateOrder`) from another module. Using `Enum` instead of `Permission` in `IsAllowed` is weakly-typed and does not work well with the database.

## What Are Symbols?

Symbols solve this problem. A `Symbol` is a class with a fixed set of instances declared in static fields (like enums), but these fields can be spread across different classes (unlike enums).

### Declaring a Symbol Type

To declare a new symbol type, e.g., `PermissionSymbol`:

```csharp
public class PermissionSymbol : Symbol
{
    private PermissionSymbol() { }

    public PermissionSymbol(Type declaringType, string fieldName)
        : base(declaringType, fieldName)
    {
    }
}
```

Then create a logic class to manage registration:

```csharp
//In PermissionLogic.cs
public static class PermissionLogic
{
    static HashSet<PermissionSymbol> RegisteredPermissions = new HashSet<PermissionSymbol>();

    // Called by each module to register its permissions
    public static void RegisterPermission(PermissionSymbol permission)
    {
        RegisteredPermissions.Add(permission);
    }

    // Called during application startup to initialize SymbolLogic
    public static void Start(SchemaBuilder sb)
    {
        SymbolLogic<PermissionSymbol>.Start(sb, () => RegisteredPermissions);
    }
}
```

### Declaring Symbol Instances

Declare symbol instances as static fields, using `[AutoInit]` to ensure initialization:

```csharp

[AutoInit]
public static class OrderPermission
{
    public static PermissionSymbol CreateOrder;
}
```

A symbol instance has two types:
- The symbol type (e.g., `PermissionSymbol`)
- The declaring type (e.g., `OrderPermission`)

Then register the symbol in the module's logic class:

```csharp

//In OrderLogic.cs 
public static class OrderLogic
{
    public static void Starter(SchemaBuilder sb)
    {
        PermissionLogic.RegisterPermission(OrderPermission.CreateOrder);
    }
}
```


Both logic classes should be started in your application startup.

```csharp
PermissionLogic.Start(sb);
//(...)
OrderLogic.Start(sb);
```

This pattern enables each module to independently register its own symbols (such as permissions), allowing the set of symbols to be easily extended as new modules are added.

### Symbol.ToString

With the help of `Signum.MSBuildTask` and `AutoInitAttribute`, symbols provide a smart `ToString()` similar to enums:

```csharp
PermissionSymbol permission = AuthorizationPermission.CreateUsers;
permission.ToString(); // returns "AuthorizationPermission.CreateUsers"
```

### Symbols as Entities

Symbols inherit from entities and can be stored and referenced in the database. Not all declared symbols are registered in the database — only those registered by modules. `SymbolLogic<T>` tracks used symbols and can parse them.

```csharp
public static class SymbolLogic<T>
    where T : Symbol
{
    public static void Start(SchemaBuilder sb, Func<IEnumerable<T>> getSymbols);
    public static ICollection<T> Symbols { get; }
    public static HashSet<string> AllUniqueKeys();
    public static T TryToSymbol(string key);
    public static T ToSymbol(string key);
}
```

Once registered, `SymbolLogic<T>` synchronizes the table for `T` to contain the relevant symbols. On initialization, it retrieves symbols from the database and assigns IDs to the corresponding static fields.

### Symbol.NiceToString

Symbols also provide a user-friendly, localizable `NiceToString` (Pascal-spaced), making them almost as convenient as enums.

### Examples

Signum.Extensions includes several symbol types:
- PermissionSymbol
- TypeConditionSymbol
- FileTypeSymbol
- ProcessAlgorithmSymbol
- SimpleTaskSymbol
- OperationSymbol (used internally)

Consider using a symbol if you are creating a reusable module with an extensible set of options, strategies, or commands defined by client code.

## SemiSymbols

A `SemiSymbol` is a hybrid: some instances behave like symbols (declared in static readonly fields), while others are created at runtime (e.g., by users). Examples include `AlertType` and `NoteType`, which can be used both by business logic and created dynamically.

## Entity Types by Row Change Frequency

Entities can be classified by how frequently their rows are added, removed, or modified:

**Most Static**
1. **Enums:** Rows are fixed at compile time and never change (e.g., `OrderState`).
2. **Symbols:** Rows change only when developers add new symbols in code (e.g., `PermissionSymbol`).
3. **SemiSymbols:** Rows change occasionally, either by developers or by users with admin privileges (e.g., `AlertType`).
4. **String Master Entities:** Rows change as admin users add or modify entries (e.g., `CountryEntity`).
5. **Master Entities:** Rows change as users with some admin privileges manage entries (e.g., `ProductEntity`).
6. **Transactional Entities:** Rows change frequently as regular users create, update, or delete records (e.g., `OrderEntity`).
7. **Log Entities:** Rows change very frequently; records are created automatically and often removed after some time (e.g., `OperationLogEntity`).

**Most Dynamic**
