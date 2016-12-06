# Schema.EntityEvents

`EntityEvents<T>` is a generic class that contains the events that will when Signum.Engine interacts with your entities (i.e. `Save`, `Retrieve`, etc..). 

`Schema` class gives you two ways of accessing `EntityEvents`: 

```C#
public class Schema
{
   public EntityEvents<Entity> EntityEventsGlobal { get; }
   
   public EntityEvents<T> EntityEvents<T>()
       where T : Entity
}
```

* `EntityEventsGlobal` property lets you attach/detach event for every `Entity`.
* `EntityEvents<T>` method lets you attach/detach event for one particular entity type `T`.

`EntityEvent<T>` class defines a bunch of events:  

```C#
public class EntityEvents<T> : IEntityEvents
            where T : Entity
{
    public event PreSavingEventHandler<T> PreSaving;
    public event SavingEventHandler<T> Saving;
    public event SavedEventHandler<T> Saved;

    public event RetrievedEventHandler<T> Retrieved;

    public event FilterQueryEventHandler<T> FilterQuery;

    public event PreUnsafeDeleteHandler<T> PreUnsafeDelete;
    public event PreUnsafeDeleteMlistHandler<T> PreUnsafeMListDelete;
    public event PreUnsafeUpdateHandler<T> PreUnsafeUpdate;
    public event PreUnsafeInsertHandler<T> PreUnsafeInsert;
}
```

## PreSaving

`PreSaving` is executed before saving. Allows changing the object graph but, if done, `graphModified` must be set to `true`.

```C#
public delegate void PreSavingEventHandler<T>(T ident, ref bool graphModified) where T : Entity;
```

Consider overriding `ModifiableEntity.PreSaving` instead if you have control of the entity and the changes do not require interacting with the `Logic` assembly.

```C#
//in BugLogic.Start..

sb.Schema.EntityEvents<BugEntity>().PreSaving += Bug_PreSaving;

static void Bug_PreSaving(BugEntity bug, ref bool graphModified)
{
   if(bug.LastUser.Is(UserEntity.Current))
      return;

   bug.LastUser = UserEntity.Current;
   graphModified = true;
}

```

## Saving

`Saving` is executed after `PreSaving` is executed for all the entities in the graph. The graph is validated, and the modifications propagated. Let you test the final entity that will be saved in the database.   

```C#
public delegate void SavingEventHandler<T>(T ident) where T : Entity;
```

Saving is used tipically to assert for permission and throw an exception if necessary before any change is made in the database. 

## Saved

`Saved` is executed after all the objects in the graph are saved. 

```C#
public delegate void SavedEventHandler<T>(T ident, SavedEventArgs args) where T : Entity;
```

Use `Saved` to create related entities after some entity is saved, since you already have the Id. One good example are `MList<T>` fields with `[Ignore]` attribute that are represented as an independent `EntityKind.Relational` entity in the database.

If you want to assert for integrity, instead of `Saved`, you can use a combination of `Saving` and [Transaction](../Connection/Transaction.md) `PreRealCommit` / `PostRealCommit`.

## Retrieved 

`Retrieved` is executed for each retrieved entity after a LINQ query is completely processes. 

```C#
public delegate void RetrievedEventHandler<T>(T ident) where T : Entity;
```

Consider using `ModifiableEntity.PostRetrieving` instead if you have control of the entity and the changes do not require interacting with the `Logic` assembly.

## FilterQuery

`FilterQuery` is processed at the early stages of the LINQ provider pipeline to allow you add filters to `Database.Query<T>()` and `Database.MListQuery<E, V>()` expressions. 

```C#
public delegate FilterQueryResult<T> FilterQueryEventHandler<T>() where T : Entity;

public class FilterQueryResult<T> : IFilterQueryResult where T : Entity
{
    public readonly Expression<Func<T, bool>> InDatabaseExpresson;
    public readonly Func<T, bool> InMemoryFunction; //Optional
}
```

Use `FilterQuery` to filter all the queries implicitly, the way Authorization and Isolation module do it. 

`FilterQuery` takes effects in all queries of the LINQ provider, including simple `Database.Retrieve`, `UnsafeDelete`, `UnsafeUpdate`, `UnsafeInsert`, etc..

When executing `Database.MListQuery<E, V>()`, will be automatically filtered based in the `FilterQuery` event of the `Parent` property (generic argument `E`).

`InMemoryFunction` is a function that can be executed in-memory to test for whether a object is visible or not. This is usuefull for filtering out entities in in-memory caches. 

## PreUnsafeDelete

`PreUnsafeDelete` is invoked before whenever a `UnsafeDelete` is executed.

```C#
public delegate void PreUnsafeDeleteHandler<T>(IQueryable<T> entityQuery);
```

This event is really useful for cascade deleting and cache invalidation: 

```C#
Schema.Current.EntityEvents<ProjectEntity>().PreUnsafeDelete += query => 
	query.SelectMany(proj => Database.Query<BugEntity>().Where(b=>b.Project.RefersTo(proj)))
    .UnsafeDelete();
```
## PreUnsafeMListDelete

`PreUnsafeMListDelete` is invoked whenever `UnsafeDeleteMList` is invoked for a MListTable that belongs to the entity `T`. 

```C#
public delegate void PreUnsafeMListDeleteHandler<T>(IQueryable mlistQuery, IQueryable<T> entityQuery);
```

## PreUnsafeUpdate

`PreUnsafeUpdate` is invoked whenever `UnsafeUpdate` is invoked in the table `T` or one of his MListTables that belongs to the entity `T`. 

```C#
public delegate void PreUnsafeUpdateHandler<T>(IUpdateable update, IQueryable<T> entityQuery);
```


## PreUnsafeInsert

`PreUnsafeInsert` is invoked whenever `UnsafeInsert` is invoked in the table `T` or one of his MListTables that belongs to the entity `T`. 

```C#
public delegate void PreUnsafeInsertHandler<T>(IQueryable query, LambdaExpression constructor, IQueryable<T> entityQuery);
```



