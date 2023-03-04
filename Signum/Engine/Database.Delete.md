# Database.Delete

`Database` class has a some overloads for deleting entities. All of this methods are just shortcuts for calling `UnsafeDelete` in the LINQ provider, that does the real work.

Deleting an entity (with `Delete` or `UnsafeDelete` method) will delete the entity itself and his related MList elements, but not the related entities (including `Part` entities). In order to do that, use `PreUnsafeDelete` in `EntityEvents<T>`. 

### Delete

Deletes an entity from the database. 

```C#
public static void Delete(Type type, int id)
public static void Delete<T>(int id) where T : Entity
```

If the entity does not exist throws a `EntityNotFoundException`. 

There are also overloads that are extension methods for entities or lites:

```C#
public static void Delete<T>(this Lite<T> lite) where T : class, IEntity
public static void Delete<T>(this T ident) where T : class, IEntity
```

As usual, this overloads take the run-time type of the argument, not the static type `T`. 

Example: 

```C#
new UserEntity().Save().Delete(); //I'm feeling productive...
```


### DeleteList

Deletes all the entities in a list of `ids`: 

```C#
public static void DeleteList(Type type, IList<int> ids)
public static void DeleteList<T>(IList<int> ids) where T : Entity
```

If the entities are not found, throws an `InvalidOperationException` and rollbacks the transaction. 

There are also overloads taking lists of lites and entities:

```C#
public static void DeleteList<T>(IList<Lite<T>> collection) where T : class, IEntity
public static void DeleteList<T>(IList<T> collection) where T : IEntity
```

If the lists contain entities (or covariant lites) of different types, they are grouped by type before calling `DeleteList(ids)`.

