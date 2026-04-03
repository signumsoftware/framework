# Lite\<T>

Every Persistence Framework has to deal with laziness in some way. Linq to SQL and Entity Framework, for example, follow a run-time laziness approach, meaning that you can define at run-time if a relationship is lazy or not.

Singum Framework, however, needs you to define that a field/property type as `Lite<T>` instead of `T` to create a lazy relationship (where `T` is some `IEntity`).

That means that laziness is structural (you have to define it at compile time), and also non-transparent (since you have to explicitly load a `Lite<T>` before accessing it). 

This is mandatory because Signum Entities are meant to be easy `Serializable`, so you can't pretend that the database is always going to be there. For the same reason, lites affects validations, since it's not safe to define your [Validation](Validation.md) based on a Lite relationships. Entity validation ends where lites appear. 

Given that lites are an important decision when designing your entities, hiding it in the property getter/setter is not a good idea because you will make your entities dependent on the engine. 

```C#
 Lite<PersonEntity> person;
 public PersonEntity Person
 {
    get { return person.Retrieve(); }  //Don't do this!!
    set { Set(ref person, value.ToLite()); } //Don't do this!!
 }
```

> **Note:*** *Usually, any kind of mismatch between field and property types is not recommended, since is would affect Linq queries as well.

### Lite\<T> class

```C#
public interface Lite<out T> : IComparable, IComparable<Lite<Entity>>
    where T : class, IEntity
{
    T Entity { get; }
    T EntityOrNull { get; }

    int Id { get; }
    bool IsNew { get;  }
    int? IdOrNull { get; }
    Type EntityType { get; }
    Entity UntypedEntityOrNull { get; }
}

public static class Lite
{ 
    public static Lite<T> ToLite<T>(this T entity) where T : class, IEntity
    public static Lite<T> ToLite<T>(this T entity, string toStr) where T : class, IEntity
    public static Lite<T> ToLiteFat<T>(this T entity) where T : class, IEntity
    public static Lite<T> ToLiteFat<T>(this T entity, string toStr) where T : class, IEntity
    public static Lite<T> ToLite<T>(this T entity, bool fat) where T : class, IEntity

    public static Lite<Entity> Create(Type type, int id)
    public static Lite<Entity> Create(Type type, int id, string toStr)
    public static Lite<Entity> Create(Type type, int id, string toStr, ModifiedState state)
}
```

### Beyond lazy relationships

While an important use of `Lite<T>` is to create lazy relationships between entities, `Lazy<T>` is also massively used as a lightweight and strongly-typed way of referring to database entities. 

Internally, a `Lite<T>` basically contains: 

* The type (`T`) of the referred entity. 
* The Id of the referred entity (if not new). 
* The `ToString` of the referred entity.

This means that `Lite<T>` are a perfect identity card for entities and much better than passing `Ids` around becaus: 
* They are strongly typed (so no risk of confusing an `Id` of `OrderEntity` with an `Id` of `OrderLineEntity`)
* They carry a cached `ToString` of the entity, simplifying debugging and allowing them to be shown in the user interface in ComboBoxes, AutoCompletes, or Search Control columns. 
* They are more convinient to use using `ToLite` and `Retrieve` extension methods. 

You can expect many APIs of Signum Framework taking and returning `Lite<T>` instead of heavyweight  entities and the LINQ provider also has excelent support for retrieving and comparing `Lite<T>`. 

### Thin and Fat
First of all, there's no such thing as a `Lite<T>` pointing to `null`. A `Lite<T>` pointing to `null` is just `null`.

However, Lite<T> could have two possible states, Thin or Fat
* **Thin:** Stores only the type of the field, the Id and a textual description of it (`ToString`).
* **Fat:** The lite actually has a reference to the related entity, and will be saved (or serialized) with it. 

In the current implementation, every `Lite` retrieved from an entity field will be in thin state (maybe in the future we will add hints to the LINQ provider to pre-load lites). But once retrived it will store the entity. If needed you can return it to Thin calling `ClearEntity()`

On the other hand, every `Lite` pointing to a new entity needs to be in fat state because the new entity doesn't have an `Id` jet there's no way to identify it but by reference. 

### Lite covariance 

If you paid attention to the `Lite<T>` definition above you'll realize that.... **is an interface!**. The only reason it's an interface is to support [covariance](http://msdn.microsoft.com/en-us/library/ee207183.aspx), and is not supported that you create your custom `Lite<T>` implementation. 

Thanks to lite covariance you can do this: 

```C#
//Returns a GiraffeEntity, but can be assigned to AnimalEntity variable
AnimalEntity animal = Database.Retrieve<GiraffeEntity>(3); 


//Returns a Lite<GiraffeEntity>, but can be assigned to Lite<AnimalEntity> variable
Lite<AnimalEntity> animalLite = Database.RetrieveLite<GiraffeEntity>(3);
```

Also you can test the object as you expected

```C#
animalLite is Lite<GiraffeEntity>
animalLite.EntityType == typeof(GiraffeEntity)
```
But if you use GetType() you'll find the trick, the internal `LiteImp<T>` class

```C#
animalLite.GetType() == typeof(Lite<GiraffeEntity>) 
//False, actually returns typeof(LiteImp<GiraffeEntity>))
```

Also, be careful when comparing lites and references because **unfortunately it compiles**. 

```C#
animalLite == animal // returns false BUT COMPILES
//theoretically a class inheriting from Animal could also implement Lite<T>

animalLite.Is(animal) // Ok
animalLite.Is(animal.ToLite()) // Ok
```

```Note:``` Signum.Analyzer restores the compile-time errors when he finds comparishons between `Lite<OrangeEntity>` and `AppleEntity`, or between `Lite<OrangeEntity>` and `Lite<AppleEntity>`. 


### Lite Keys

Even in scenarios outside of the .Net Type system (like the Web or reading and writing files) is useful to keep the entity type to avoid confusing numeric ids of different entity types. That's why Lites can be serialized with `Key` method (returning `"Type;Id"`) or  `KeyLong` method (returning `"Type;Id;ToString"`)

```C#
public interface Lite<out T> : IComparable, IComparable<Lite<Entity>>
    where T : class, IEntity
{
    string Key(); //Returns "Person;3"
    string KeyLong(); //Returns "Person;3;John connor"
}

public static class Lite
{ 
    public static Lite<Entity> Parse(string liteKey)
    public static Lite<T> Parse<T>(string liteKey) where T : class, IEntity

    public static string TryParseLite(string liteKey, out Lite<Entity> result)
    public static string TryParse<T>(string liteKey, out Lite<T> lite) where T : class, IEntity
}
```
