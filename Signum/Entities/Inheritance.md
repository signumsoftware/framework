# Inheritance

Inheritances (and interface implementation) is maybe the most popular feature of Object Oriented Programming ~~since encapsulation is for paranoid people and nobody understands polymorphism :)~~ and it is the one that has more difficulties to map to the relational world. 

Signum Framework approach is a very practical one. We put all the responsibility of inheritance mapping on Polymorphic Foreign Keys because they allow three different scenarios, all of them very important: 

1. Map a hierarchy of classes of your data model in the database: This problem is the one everybody thinks of when speaking about ORM and inheritances mapping. Nothing new here, but actually it's not that common in real data models. Maybe it is more interesting for the intellectual satisfaction than for practical reasons.
2. Reference to potentially 'every entity': Some entities like notes, attached documents, alerts... are potentially attachable to any kind of entity. It's like having System.Object in the database.
3. The combination of polymorphic foreign keys with overriding Attributes through the `SchemaBuilderSettings` allow us to connect different modules (from the UI to the DB) in a different assembly and integrate them easily. 

At the end of this page you could read a bit more about why we ended up implementing inheritance in this way.

So Inheritance is all about Polymorphic Foreign Keys (PFK). What the hell are they???

A PFK is just FK that could point to entities of different types. We have them in two flavors: 

## ImplementedByAttribute

This [FieldAttributes](FieldAttributes.md) take a `params Type[]` as constructor, what it does is to allow the field to have objects of any of the defined types. Let's supose we have a `PlayerEntity` entity with the following field: 

```C#
[ImplemetedBy(typeof(RevolverEntity), typeof(BazookaEntity), typeof(MachineGunEntity)]
IWeapon weapon;
```

The actual implementation in the database is just multiple foreign keys, each one with different tables of each mapped type (`RevolverEntity`, `BazookaEntity`, `MachineGunEntity`). Due to that, the types should be: 

* A subclass of `Entity` (they need their own table)
* A type assignable to the field's Type (in this case, an implementation of `IWeapon`). 

### Performance considerations 

When there are many common fields in the different implementations of an `ImplementedBy` field  (in this example, if `RevolverEntity`, `BazookaEntity` and `MachineGunEntity` share many fields, declared in `IWeapon`) writing polymorphic foreign key can be quite slow. For example: 

```C#
Database.Query<PlayerEntity>().Select(p=>p.Weapon.Ammunition > 0)
```

This query will need to join with the three different implementations and coalesce each of the three Ammunition columns. This case is even worst: 

```C#
Database.Query<PlayerEntity>().Select(p=>p.Weapon.Provider.Name)
```

In this case, the `ProviderEntity` table will be joined with the three different tables, creating a slow join due to the use or `ORs`. 

In our experience, the best idea many times is to fusion all the common fields in a single class (`WeaponEntity`) with an field of type `WeaponExtensionEntity` implemented by `RevolverWX`, `BazookaWS` and  `MachineGunWS` containing the different fields.

When this is not an option, use `CombineStrategyAttribute` on the `ImplementedByField`, or use `CombineSwitch` and `CombineUnion` extensions method on each particular query could help you tuning the performance using SQL `SWITCH` (default) or `UNION` in each case. 

## ImplementedByAllAttribute
This [FieldAttributes](FieldAttributes.md), instead of mapping a finite number of Types and creating this number of FK in the database, assumes that almost 'every entity' could fit in this field. 

```C#
[ImplemetedByAll] // there are too many kinds of weapon in the world to enumerate it...
Entity weapon;
```

The implementation in the database uses just two columns:

* One for the actual Id of the related entity. This column has no Foreign Key restriction.
* Another for the Type Id of the Entity. Referring to the mandatory `TypeEntity` table. 

Think of `TypeEntity` as Signum Engine's equivalent to System.Type. It's a table containing a row for each concrete `Entity` included in the schema. 

That's all you need to know about Inheritance in Signum Engine.... unless you want to know more :).


## Inheritance Support in the Engine

### Lite\<T>

You can use PFK with Lite<T> seamlessly. In fact, the whole reason Lite<T> are covariant is to support these kind of scenarios. 

```C#
[ImplemetedBy(typeof(RevolverEntity), typeof(BazookaEntity), typeof(MachineGunEntity)]
Lite<IWeapon> weapon;
```

### Overriding attributes
The ability of using `SchemaBuilderSetting` to override attributes on entities that you don't control lets you integrate different modules in a type-safe and elegant way. Take a look in [Schema](../Signum.Engine/Schema.md).

### Save and Retrieving
Nothing to learn. Saving and retrieving entities with PFK it is just transparent.

### Query
We support polymorphic foreign keys in queries also. 

* Navigate across PFK to an actual implementation just by casting the field in your queries.
* Test the type of a PFK just by using is operator.
* Test two entities for equality. Our entity comparison algorithm on queries takes PFK into account, you can equalize normal references, `ImplementedBy` references and `ImplementedByAll` references in any combination!!
* Retrieve and ToLite(): You can retrieve and take Lites of elements in any PFK easly, there's just nothing to learn.
* Use polymorphism at the database level when writing your queries in terms of abstract clases or interfaces, but we aware of the **performance considerations** above. 

## Why we do not support other inheritance solutions? (Advanced Topic)

Given the next simple hierarchy:

```C#
public abstract class PersonEntity : Entity
{
    string name;
    (..)
}

public class SoldierEntity : PersonEntity
{
    WeaponEntity weapon;
    (..)
}

public class TeacherEntity : PersonEntity
{
    BookEntity book; 
    (..)
}

```

There are different ways of persisting with inheritance hierarchies, using NHibernate's terminology:


1. **Table-per-class-hierarchy:** A table `PersonEntity {Id, ToStr, Discriminator, Name, idWeapon, idBook, }`. Every Soldier and Teacher goes to the this table using Discriminator values `{'S', 'T'}` to differentiate between them. This approach has some problems:
 * It works only with hierarchies that are not very deep, or you will end up with a lot of null values.
 * Type mismatch appears, since Soldier could have, let's say, int NumberOfKilledPeople, that would need to be null in case the row contains a `TeacherEntity`.
 * It's not easy for an army to have a list of Soldiers, or for a school to have a list of teachers, since they all are in the same table.
2. **Table-per sub-class:** Here we have three tables, `PersonEntity { Id, ToStr, Name }`, `SoldierEntity{idPerson, idWeapon}` and `TeacherEntity{idPerson, idBook}`. Problems:
 * Relationally, you could have someone who is both Soldier and Teacher. This is valid in the real world but not in the Object model :)
 * If you don't put an `Id` column in `SoldierEntity` (or `TeacherEntity`),then `ArmyEntity` and `SchoolEntity` will have the same problems for referencing concrete classes.
 * If you put `Id` column on `SoldierEntity` (or `TeacherEntity`), you will have two different Ids: For a soldier `idPerson` and `idSoldier`, and for a teacher `idPerson` and `idTeacher`. This creates ambiguities.
3. **Table-per-concrete-class:** In this solution, you will have just two tables, `SoldierEntity { id, ToStr, Name, idWeapon }` and `TeacherEntity { id, ToStr, Name, idBook }`. Since `PersonEntity` is an abstract class there's no point in having its own table. Problems:
 * Not easy to have a Foreign Key to Persons in a generic way.

When we designed inheritances in our framework we went just for the option #3 because it is the simplest.

With #1 and #2 you need to add a 'hierarchy concept' in the framework, something that embraces the three classes and puts them together in the same table (#1) or in the same table hierarchy (#2).

Since interfaces allow some kind of multiple inheritance, the same entity could potentially be part of different hierarchies, and this is not suported by #1 or #2.

The algorithm to know where an entity actually resides becomes more complex with hierarchies and we also avoid the type mismatch of solution #1. 

However, the main reason for going for the PFK-only solution is that all these complex solutions solve only the first initial (Saving Entity Hierarchies), while PFK also allows solve the 2nd problem (reference to anything) and 3rd problem (connect modules), that are almost as useful as the first one.