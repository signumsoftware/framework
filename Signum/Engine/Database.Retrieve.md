## Database.Retrieve

`Database` class has a lot of overloads for retrieving single entities or a list of all the entities of a given type, the entities themselves or just Lazy objects, in a reusable weak-typed way or in a strong-typed way using generics. 

All of this methods are just shortcuts for the LINQ provider, that does the real work, but can also have better performance if **Cache module** is activated.

Retrieving an entity (using `Database.Retrieve` or `Database.Query` directly) requires retrieving the whole graph of related entities and lites. This is usually done by expanding the query, joining with all the related entities, and creating different query (lazy child projection) for the collections. 

In order to simplify the queries consider: 
* Retrieve an anonymous class with the data you need, instead of the whole entity. 
* Simplify the object graph by inverting relationships or using `Lite<T>`. 
* Use **Cache module** to avoid expanding related master entities. 

A single call to `Retrieve` (or `Query`) does not generate cloned entities, but two different calls can do it if not surrounded by a common [`EntityCache`](EntityCache.md) scope. 


### Retrieve

Two simple method let you retrieve entities given the Id. 

```C#
public static Entity Retrieve(Type type, int id) 
public static T Retrieve<T>(int id) where T : Entity
```

Very often you actually retrieve entities by `Lite<T>` instead:

```C#
public static T Retrieve<T>(this Lite<T> lite) where T : class, IEntity
```

Two important considerations: 
* `Lite<T>` is covariant and the actual type of the `Lite` object (returned by `Lite.EntityType` property) is used to retrieve the entity, not the static type `T`.
*  By default, the retrieved entity WON'T be saved in the `Entity` property of the `Lite<T>` to avoid serialization issues. 

Example: 

```C#
var lionLite = myLion.ToLite(); //unloaded lite
var myLion2 = lion.Retrieve(); //first DB query is made.
var myLion3 = lion.Retrieve(); //second query is made, myLion2 != myLion3 (if no EntityCache in scope) and lite still unloaded
```

If you want to avoid the second query by saving the entity inside of the `Lite.Entity` property, use this method instead: 

```C#
public static T RetrieveAndRemember<T>(this Lite<T> lite) where T : class, IEntity
{
    if (lite == null)
        throw new ArgumentNullException(nameof(lite));

    if (lite.EntityOrNull == null)
        lite.SetEntity(Retrieve(lite.EntityType, lite.Id));

    return lite.EntityOrNull!;
}
```

As you see, `RetrieveAndRemember` only retrieves the entity if the lite is not already loaded (`EntityOrNull == null`) saving it in the `EntityOrNull` property for the future.  

Example: 

```C#
var lionLite = myLion.ToLite(); //unloaded lite
var myLion2 = lion.RetrieveAndRemember(); //lite is loaded
var myLion3 = lion.RetrieveAndRemember(); //seccond query not made!
```

**Note:** `Retrieve` is the simplest and recommended method to use with lites most of the time to avoid serialization issues, that's why it has the shortest name. 

### RetrieveLite

Retrieves a `Lite<T>` from the database (basically the `ToString`)

```C#
public static Lite<Entity> RetrieveLite(Type type, int id)
public static Lite<T> RetrieveLite<T>(int id) where T : Entity
```

### FillToStr

If you already have the `Lite<T>` but has no string (i.e.: from Signum.Web `LiteModelBinder`).  

```C#
public static Lite<T> FillToString<T>(this Lite<T> lite) where T : class, IEntity
```

Example
```C#
var lionLite = Lite.Create<LionEntity>(2);
lionLite.ToString();
```

### GetToStr

The last two methods ultimately call `GetToStr`:

```C#
public static string GetToStr(Type type, int id)
public static string GetToStr<T>(int id) where T : Entity
```

### Exists

The previous methods will throw a `EntityNotFoundException` if the entity is not found (or filtered by FilterQuey event), if for some (strange) reason you have an `id` that you don't know if its valid, use `Exists` method: 

```C#
public static bool Exists(Type type, int id)
public static bool Exists<T>(int id) where T : Entity
public static bool Exists<T>(this Lite<T> lite)
```  


### RetrieveAll and RetrieveAllLite 

Retrieves all the entities of a particular table. Obviously could be slow for big tables. 

```C#
public static List<T> RetrieveAll<T>() where T : Entity
public static List<Entity> RetrieveAll(Type type)
```

And similar one for `Lite<T>`:
```C#
public static List<Lite<T>> RetrieveAllLite<T>() where T : Entity
public static List<Lite<Entity>> RetrieveAllLite(Type type)
```

If **Cache module** is activated for the table `T`, no query will be made, except if there are `FilterQuery` registered (like Isolation or TypeConditions), in this case just a simple query retrieving the IDs. 


### RetrieveList and RetrieveListLite

Retrieves all the entities given a list of ids, and throwing an exception if at least one is missing. 

```C#
public static List<T> RetrieveList<T>(List<int> ids) where T : Entity
public static List<Entity> RetrieveList(Type type, List<int> ids)
```

And similar one for `Lite<T>`:

```C#
public static List<Lite<T>> RetrieveListLite<T>(List<int> ids) where T : Entity
public static List<Lite<Entity>> RetrieveListLite(Type type, List<int> ids)
```

The order of the results will be preserved. 

If many Ids are provided, they will be groped in chunks of `SchemaSettings.MaxNumberOfParameters` to avoid reaching SQL Server maximum number of parameters. Still, reconsider creating a query that does require that many parameters. 

Additionally, if **Cache module** is activated for the table `T`, no query will be made, except if there are `FilterQuery` registered (like TypeCondition or Isolation), in this case just a simple query checking that all the IDs provided are visible. 

### RetrieveFromListOfLite

Retrieves all the `IEnumerable<Lite<T>>` as a `List<T>`. 

```C#
public static List<T> RetrieveFromListOfLite<T>(this IEnumerable<Lite<T>> lites) where T : class, IEntity
```

The order of the results will be preserved. 

Since `List<T>` is covariant, the `lites` could be of different types, but `RetrieveFromListOfLite` is smart enough to group them by type and call `RetrieveList` for each type.  

