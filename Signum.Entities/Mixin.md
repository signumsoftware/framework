# MixinEntity

`MixinEntity` is the base class to create **Mixins**, an alternative to inheritance to expand types.

`Mixins` are used to append fields, properties, columns or methods to types in two important use-cases: 

* When you don't have control the type because it's in a reusable module. (i.e.: Add a property `EmployeeDN` in `UserDN` only for this application). 
* When you need to add properties to different types independently of their position in the inheritance hierarchy (i.e.: `AddressDN`, `OrderDN` and `EmployeeDN` all need to be `Corrupt`, or `Isolated`, or `Disconnected`).

Under the covers a `MixinEntity` inherits from `ModifiableEntity` and is ensured to be appended to any instance of types that include the Mixin.

## Mixins vs Inheritance

Mixins have two advantages over inheritance:

### Real type expansion

When you inherit from a type (i.e.: `AnimalDN`) and add some new properties to it you are actually creating a new type, not modifying the original one. That's Ok if you want to create a hierarchy of types (i.e.: `LionDN`) but if you want expand the type (i.e.: `CustomAnimalDN`) you'll need to find all the points where the old type is instantiated (i.e.: `new AnimalDN`) and replaced by the new type (i.e.: `new CustomAnimalDN`). You could use a factory instead, but this doesn't play nicely with [object initializers](http://msdn.microsoft.com/en-us/library/bb384062.aspx). 

With Mixins you just associate the type (i.e.: `AnimalDN`) with your custom Mixin (i.e.: `CustomAnimalMixin`) and then every single instantiated animal will carry your mixin with it, no need to change the 
instantiations (i.e.: `new AnimalDN`). 


### Multi-directional expansion

With inheritance you only have one base class. This is OK to model some simple hierarchies but commonly types expand in different unrelated dimensions.

For example, imagine that we have a SAAS application using **Isolation module** and some entities require a field with the current isolation. 

Additionally, some entities can be used off-line in a boat, using **Disconnected module**, and require some fields to know who is the current owner. 

And finally, some entities have been loaded from a legacy application and have some validations disabled, using corruption. 

Using inheritance it will be a mess. For example, a `LionDN`: 

* should inherit from `AnimalDN`?, 
* or from `IsolatedAnimalDN`? 
* or from `DisconnectedIsolatedAnimalDN`? 
* or from `CorruptedDisconnctedIsolatedAnimalDN`?

maybe `AnimalDN` should inherit from `CorruptedDisconnctedIsolatedEntityDN`?  but what if `GiraffeDN` doesn't need to be used off-line, neither isolated or corrupted...

Using mixin the problem is simple, `LionDN` inherits from `AnimalDN`, but also includes `IsolationMixin`, `DisconnectedMixin` and `CorruptMixin`. 

## MixinEntity vs EmbeddedEntity

The implementation of `MixinEntity` is quite similar to `EmbeddedEntity`, in fact both inherit directly from `ModifiableEntity` but there are two important differences:

1. `MixinEntity` can be included in types you don't have control of (using `static class MixinDeclaratons`).

2. Only one `MixinEntity` instance of each type can be associated to an entity (i.e.: is not possible to have two `IsolationMixin`), while the same is not true fro `EmbeddedEntity` fields, that have a name (i.e.: `ShippingAddress` and `BillingAddress`). 



## Declaring a new Mixin Type 

Let's see how to declare a new Mixin type: 

```C#
[Serializable]
public class UserEmployeeMixin : MixinEntity
{
    protected UserEmployeeMixin(IdentifiableEntity mainEntity, MixinEntity next)
        : base(mainEntity, next)
    {
    }

    EmployeeDN employee;
    public EmployeeDN Employee
    {
        get { return employee; }
        set { Set(ref employee, value); }
    }
}
``` 

As you see, a `MixinEntity` looks like a normal entity, with normal properties and fields and the same capabilities for validation, change tracking, change notifications for data-binding etc...

The only important difference is the constructor: 

* The constructor should be `protected` to avoid client code instantiate any `MixinEntity`. An instance with Mixins is automatically instantiated with all their mixins and there's no way to get rid of them. They are effectively an expansion of the type.  
* The constructor passes a `IdentifiableEntity mainEntity` to the base constructor, this value is stored in the `MainEntity` property in `MixinEntity` to let any Mixin have access the main entity. 
* The constructor passes a `MixinEntity next` to the base constructor, the reason is that, in-memory mixins are stored as a linked list.

## Associating Types with Mixins

There's two ways of associating an entity with a mixin: 

* The simpler one, if you have control of the type, is to use an attribute:

```C#
[Mixin(typeof(DisconnectedMixin))]
[Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
public class OrderDN : Entity
{
   ...
}
```

* But frequently you want to add mixins to entities that you can not modify, in this case just include this line **before including the entitiy in the schema**: 

```C#
MixinDeclarations.Register<UserDN, UserEmployeeMixin>();
```

> **Note:*** *Signum.Windows also requires this information, but is automatically transited by the WCF webservice when started.

## Business logic support

We have paid a lot of attention to make Mixin first-class citizens of Signum Framework, so they have support at every level. If you are reading the documentation in order, some things could not make a lot of sense yet. 

### Reading and Writing properties

`IdentifiableEntity` is the base class that can contain mixins. It has the following members: 

```C#
public class IdentifiableEntity
{
    public M Mixin<M>() where M : MixinEntity  // Typed variant  
    public MixinEntity GetMixin(Type mixinType) // Un-typed variant
    public MixinEntity this[string mixinName] // For WPF data-binding
    public IEnumerable<MixinEntity> Mixins // Returns all the mixins walking the linked list
}
```

Most of the time you just use the first method. 

Example: 

```C#
UserDN user = new UserDN(); 
user.Mixin<UserEmployeeMixin>().Employee = new Employee(); //The UserEmployeeMixin is already there!
Console.WriteLine(user.Mixin<UserEmployeeMixin>().Employee.ToString()); 
```

### Object initializers

If you're intializing an entity, you can use `SetMixin` extension method to inititialize properties without *breaking the expression*.

```C#
public static T SetMixin<T, M, V>(this T entity, Expression<Func<M, V>> mixinProperty, V value)
    where T : IIdentifiable
    where M : MixinEntity
```

Example: 

```C#
return new UserDN
{
   UserName = "bmarley",
   Password = Security.EncodePassword("jamaica")
}.SetMixin((UserEmployeeMixin m) => m.Employee, new EmployeeDN
	{
       FirsName = "Bob",
       LastName = "Marley"
	});
```

### LINQ

Mixins are completely supported by the Linq provider. 

* **Queries:**
```C#
Database.Query<UserDN>()
    .Select(u => u.Mixin<UserEmployeeMixin>().Employee)
    .ToList();
````

* **UnsafeUpdates:**
```C#
Database.Query<UserDN>().UnsafeUpdate()
    .Set(u => u.Mixin<UserEmployeeMixin>().Employee, u => null)
    .Execute();
````

* **UnsafeInserts:**
```C#
Database.Query<UserDN>()
    .UnsafeInsert(u=>new UserDN
	{
	   UserName = u.UserName + "2",
	   Password = u.Password,
       Role = u.Role, 
	}.SetMixin((UserEmployeeMixin m) => m.Employee, u.Mixin<UserEmployeeMixin>().Employee));
````

### PropertyRoute

PropertyRoute also support Mixins 

```C#
//returns (UserDN)[UserEmployeeMixin]
PropertyRoute.Construct((UserDN a) => a.Mixin<UserEmployeeMixin>()) 

//returns (UserDN)[UserEmployeeMixin].Employee
PropertyRoute.Construct((UserDN a) => a.Mixin<UserEmployeeMixin>().Employee) 
```

That means that other parts of the framework that are based on `PropertyRoute` also support mixins, like `OverrideAttributes` or property authorization. 


## User interface support

From the user point of view, mixin properties try to be indistinguishable from normal properties of the entity. 

### QueryTokens 

A `QueryToken` represents the sequence of combos in the `SearchControl`. 

* in Employee.cshtml 
```C#
@Html.SearchControl(new FindOptions(typeof(UserDN), "Entity.Employee", e.Value), new Context(e, "users"))
```

* in Employee.xaml
```xml
<m:SearchControl QueryName="{x:Type d:UserDN}" FilterColumn="Entity.Employee" />
```

### Signum.Windows

Mixins won't make a lot of sense if the new properties could not be shown to the user in WPF controls, that's why we allow you to manipulate the default view of an entity (i.e. `User.xaml`) to include controls for your new properties.

in `App.xaml.cs` or `EmployeeClient.cs`: 

```C#
Navigator.EntitySettings<UserDN>().OverrideView((usr, ctrl) =>
{
    using (Common.DelayRoutes())
    {
        ctrl.Child<EntityLine>("Role").After(new EntityLine().Set(Common.RouteProperty, "[UserEmployeeMixin].Employee"));
    }
    return ctrl;
});
```
* Using `Child` and `After` extension methods you can find controls in the visual (or logical) tree and manipulate them. It's like jQuery for WPF!. 
* `Common.DelayedRoutes` is necessary because the `EntityLine` is being created, then the `Route` is set, and finally added to the visual tree. If written in XAML will be the other way arround.
* `[UserEmployeeMixin]` is a valid binding expression because takes advantage of the `IdentifiableEntity` indexer `MixinEntity this[string mixinName]`. 


### Signum.Web

Mixins also won't make sense if the new properties could not be shown to the user in Web controls, that's why we allow you to manipulate the default view of an entity (i.e. `User.xaml`) to include controls for your new properties.

in `Global.asax.cs` or `EmployeeClient.cs`:

```C#
Navigator.EntitySettings<UserDN>().CreateViewOverride()
    .AfterLine((UserDN u) => u.Role, (html, tc) => html.ValueLine(tc, u => u.Mixin<UserEmployeeMixin>().AllowLogin));
```

* In ASP.Net MVC there's no server-side tree to manipulate because Razor generates plain text, but `CreateViewOverride` gives some extension points in strategic locations: `BeforeLine`, `AfterLine`, `HideLine`, `BeforeTab` and `AfterTab`. 
* The **expression trees** used to create controls in Signum.Windows (`ValueLine`, `EntityLine`, ...) have  support for `.Mixin<T>()` method. 




