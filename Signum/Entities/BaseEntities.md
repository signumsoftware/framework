# Base Entities

Signum Framework provides a clear hierarchy of classes that serve as base classes for your own entities: 

* **Modifiable:** Base class with embedded change tracking.
  * **[MList\<T>](MList.md):** Similar to `List<T>` but with embedded change tracking.
  * **[Lite\<T>](Lite.md):** Allows lazy relationships and lightweight strongly-typed references to entities.
  * **ModifiableEntity:**: Base class for *entities* with change tracking and validation.  
    * **EmbeddedEntity**: Base class for entities without `Id` that live inside other entities.  
	    * **ModelEntity**: Base class for entities that won't be saved in the database (ViewModels)
    * **Entity**: Base class for entities with  their own table in the database, `Id`, `ToString` and optional concurrency control 
		* **[EnumEntity\<T>](EnumEntity.md):** Represents a `enum` table. 
		* **[Symbol and SemiSymbol](Symbols.md):** Like `enums` but can be declared in different types. 
	* **[MixinEntity](Mixin.md):** Type of entity witch properties are effectively appended to the end of another `Entity`. 

## Modifiable

At the very root we find the `Modifiable` class, inheriting from `object`. It's not even an entity, in fact it's so abstract that it's a hard to explain. It's the base class for anything that can be saved and provides change tracking. Even `MList<T>` and `Lite<T>` inherit from this `Modifiable`.

Basically `Modifiable` contains the property `Modified` of type `ModifiedState`. 

`Modifiable`  defines the `PreSaving` and `PostRetriving` virtual methods, that will be called just before saving an object and just after retrieving it. 

Also, `Modifiable` has an important role on Entity Graphs. 


## ModifiableEntity

The simplest entity possible. Your entities shouldn't inherit from `ModifiableEntity` directly.

`ModifiableEntity` implements `Modifiable.Modified` by checking if some fields was modified. To do so, it exposes the protected `Set` method.
 

`ModifiableEntity` also implements [IDataErrorInfo](http://msdn.microsoft.com/en-us/library/system.componentmodel.idataerrorinfo.aspx) and provides the basic plumbing for [Validation](Validation.md).

## EmbeddedEntity
Base class to be used when you want an entity to be embedded inside of the holders Entity. Small entities like Interval, SocialSecurityNumber, Color, GpsLocation or Address could inherit from here. 

In the current implementation, this class adds nothing over ModifiableEntity. Instead it's just a marker class to make it easier to remember what to subclass when you want Embedded behavior. 

On the database, embedded fields are stored in the parent entity table. Let's see an example: 

* If a `PersonEntity` class has an `EmbeddedEntity` of type `AddressEntity` with name `HomeAddress`. 
* And `AddressEntity` has a field `Street`.
* Then `PersonEntity` table will have a column with name `HomeAddress_Street`. 

Since `EmbeddedEntity` is a classes (reference types), by default they are nullable in the database as well, in order to reduce type-mismatch. This behavior is implemented adding a `HasValue` column and forcing nullability to the remaining embedded fields. 

Most of the time this behavior is unnecessary. You can remove it using `[NotNullableAttribute]` in your `EmbeddedEntity` field. 

## ModelEntity

Model entities are entities not meant to be stored in the database, just used as ViewModels for complex windows/webs that do not map exactly to the database, or temporal dialog that could be passed as a parameter to operations. 

Currently they inherit from `EmbeddedEntity` for simplicity, but they are not an  embedded entity. 

`ModelEntity` also has all the powerful validation/change-notification/change-tracking features from `ModifiableEntity`-  


## Entity

This is the base entity with its own table. It also has:

* Defines the `Id` field of type `PrimaryKey`. The property throws a `InvalidOperationExeption` if the entity is null.
* Defines the `IdOrNull` property of type `PrimaryKey?` witch return `null` if the entity is new.
* Defines the `IsNew` property that returns `true` when the entity is new.
* Defines `ToStringProperty` that evaluates `ToString` bus can be invalidated. Useful for binding.
* Generates `ToStr` column with the evaluation of `ToString` before saving if `ToStringExpression` is not defined.
* Overrides `Equals` and `GetHashCode` to depend on the `Id` and `Type`, not in reference equality. 
* Is the basic container of `Mixins`. 

Classes inheriting from `Entity` also need to provide and [EntityKindAttribute](EntityKind.md).

### Concurrency Support

Additionally, `Entity` also contains optional concurrency support using `Ticks` field that stores the current version of the entity. The actual value is just `DateTime.Now.Ticks` of the moment the entity is saved. 

Each time we `Save` an entity we also update the `Ticks` value.

Also, while saving a modified entity, we test if the `Ticks` value of the entity is not the same as the one in the database. If that would happen, an exception will be thrown and the transaction will be rollbacked.

When modifying a `MList<T>` only the necessary commands (INSERT/DELETE/UPDATE) are sent to the database. Applying this changes to an entity different than the one in-memory will create a corrupt state, that's why **MList\<T> fields can only be part of entities with Ticks**

You can disable concurrency control by applying [`TicksAttribute(false)`](FieldAttribute.md) to the type. This is usefull for simple types created by the Synchronizer, like [Enums](EnumEntity.md), [Symbols](Symbols.md) or your own run-time modifiable enumerated types: TypeOfCustomer, Country, State, etc... 
because these classes don't have concurrency problems (they are rarely modified) and they don't have [MList\<T>](MList.md). 

### IEntity interface

Apart from these features, it implements the `IEntity` interface, which is just a marker interface in case you want to use `ImplementedBy` or `ImplmentedByAll` over interfaces. See more about [Inheritance](Inheritance.md). 

This interface is only implemented by `Entity` class and should be inherited by any interface that will be used by Polymorphic Foreign Key. For example: 

```C#
public interface IProcessDataEntity : IEntity
{
}
```

By using an interface inheriting from `IEntity`, instead of a class inheriting from `Entity`, implementers are free to inherit from the class they want. 

