# FluentInclude

At the beginning of a Signum Framework application the `Starter` class calls the `Start` methods of the different modules used in the applications. 

This different `Start` methods tipically: 

* Include some necessary tables in the database using `SchemaBuilder.Include` method
* Maybe include some indexes. 
* Register some queries in DynamicQueryManager.
* Register some expressions in DynamicQueryManager.
* Register some operations in OperationLogic using Graph<T>, tipically save and optionally delete.

There are of course a lot of other stuff, like registering processes, permission, ResetLazy, etc... but this actions are the bread and butter of most of the `Start` methods. 

Example: 

```C#
public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
{
    if(sb.NotIncluded(MethodInfo.GetCurrentMethod()))
	{
		sb.Include<ProjectEntity>();

		sb.AddUniqueIndex((ProjectEntity p) => new { p.Name, p.Year });

		dqm.RegisterQuery(typeof(ProjectEntity), ()=> 
			from p in Database.Query<ProjectEntity>()
			select new 
			{
				Entity = p,
				p.Id,
				p.Name 
			});

		dqm.RegisterExpression((ClientEntity c) => c.Projects())

		new Graph<ProjectEntity>.Execute(ProjectOperation.Save)
		{
		    AllowsNew = true, 
			Lite = false,
			Execute = (p, _) => {}
		}.Register();

		new Graph<ProjectEntity>.Delete(ProjectOperation.Delete)
		{
			Delete = (p, _) => { p.Delete() }
		}.Register();
	}
}
```

With this in mind, the generic method `SchemaBuilder.Include` has been expanded to return a `FluentInclude<T>` instance: 

```C#
public class FluentInclude<T> where T : Entity
{
    public SchemaBuilder SchemaBuilder { get; private set; }
    public Table Table { get; private set; }
}
```

This class contains methods and extension methods that allow you to register stuff with fewer code. 

It doesn't try to cover all the cases, only the common ones, that's why `FluentInclude` can be seen as dead-end road.

Other the other side it helps in removing the syntactic noise and leting you concentrate in the interesting stuff, and promotes keeping all the information for a particular entity together. 

Example: 

```C#
public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
{
    if(sb.NotIncluded(MethodInfo.GetCurrentMethod()))
	{
		sb.Include<ProjectEntity>()
			.WithUniqueIndex(p => new { p.Name, p.Year })
			.WithSave(ProjectOpepration.Save)
			.WithDelete(ProjectOpepration.Delete)
			.WithQuery(p => new 
			{
				Entity = p,
				p.Id,
				p.Name 
			});
	}
}
```

## Available Methods 

### WithIndex / WithUniqueIndex

Register an index in the Included entity, tipically a unique index on multiple columns or with a where condition.  

```C#
public FluentInclude<T> WithUniqueIndex(Expression<Func<T, object>> fields, Expression<Func<T, bool>> where = null)
public FluentInclude<T> WithIndex(Expression<Func<T, object>> fields)
```

Internally calls `SchemaBuilder.AddUniqueIndex`.

### WithQuery

Extension method that registers a simple query in the DynamicQueryManager using `typeof(T)` as `queryName`, with no filters, joins or column customizations, and just using the provided `simpleQuerySelector` as the `selector` of the only `Select` operator. 

```C#
public static FluentInclude<T> WithQuery<T, Q>(this FluentInclude<T> fi, DynamicQueryManager dqm, Expression<Func<T, Q>> simpleQuerySelector) where T : Entity
```

Internally calls `DynamicQueryManager.RegisterQuery`.

### WithExpresssionFrom

Register expressions from another type (`F`) that returns an `IQueryable<T>`, optionally providing a custom niceName, or `null` to keep the method name non-localizable.



```C#
public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, DynamicQueryManager dqm, Expression<Func<F, IQueryable<T>>> lambdaToMethodOrProperty) where T : Entity
public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, DynamicQueryManager dqm, Expression<Func<F, IQueryable<T>>> lambdaToMethodOrProperty, Func<string> niceName) where T : Entity
        
```

> Note: Given `ProjectEntity` with a property of type `ClientEntity`, where is the right place to register `client.Projects()` expression to navigate the property backwards?. 
> Signum Framework encourages you to register it on `ProjectLogic` to keep `ClientLogic` clean of dependencies and more reusable, that's why `WithExpressionFrom`takes an
> Expression from `F` (a `ClientEntity`) to `IQueryable<T>` (a query of `ProjectEntity`). 

Internally calls DynamicQueryManager.RegisterExpression;

### WithSave / WithDelete

Register trivial implementations of `Save` (AllowsNew = true, Lite = false with no body) and Delete (just `e.Delete()` in the body).

```C#
public static FluentInclude<T> WithSave<T>(this FluentInclude<T> fi, ExecuteSymbol<T> saveOperation) where T : Entity
public static FluentInclude<T> WithDelete<T>(this FluentInclude<T> fi, DeleteSymbol<T> delete) where T : Entity
```

Internally instantiates a `Graph<T>.Execute`/`Graph<T>.Delete` and calls the extension method `OperationLogic.Register`. 

### Others...

Most of this methods are implemented using extension methods. If you feel the need to remove some code duplication for registering 
behaviour on a particular entity type, it's a good idea to create your own implementation: 


```C#
public static WithMyCustomBehaviour(this FluentInclude<T> fi, ...)
```

