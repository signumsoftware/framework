## Dynamic Queries

The Dynamic query system is a layer, on top of the LINQ provider, that allows a more dynamic manipulation of queries at run-time. 

This system is not meant to be used directly from client code, but used as a service from the Windows or Web `SearchControl`, `CounSearchControl`, or Email, Reporting and Chart module.


## DynamicQueryManager

The `DynamicQueryManager` is the main facade of the dynamic query system, and is usually passed as a parameter, along with the `SchemaBuilder`, to the `Start` method of each module. 

This class is responsible of: 
* Registering queries 
* Registering expressions 
* Execute the different flavors of `QueryRequest`.

## Registering queries

`RegisterQuery` method is used to add a query to the pool of queries that will be available to the `SearchControl` in the user interface. This queries will only be the starting point, since the `SearchControl` is able to change order, add filters and add or remove columns.

There are two variants: 

```C#
public class DynamicQueryManager
{
    public void RegisterQuery<T>(object queryName, Func<IQueryable<T>> lazyQuery, Implementations? entityImplementations = null)
```

Example in `OrderLogic.Start`: 

```C#
dqm.RegisterQuery(typeof(OrderEntity), () =>
    from o in Database.Query<OrderEntity>()
    select new
    {
        Entity = o,
        o.Id,
        o.State,
        o.Customer,
        o.Employee,
        o.OrderDate,
        o.RequiredDate,
        o.ShipAddress,
        o.ShipVia,
    });

dqm.RegisterQuery(OrderQuery.OrderLines, () =>
    from o in Database.Query<OrderEntity>()
    from od in o.Details
    select new
    {
        Entity = o,
        o.Id,
        od.Product,
        od.Quantity,
        od.UnitPrice,
        od.Discount,
        od.SubTotalPrice,
    });
```

Some interesting points: 

* **QueryName**: This object is the key that will be used to access the query. By convention, the default query for a entity of type `T` will be `typeof(T)`, but enums can also be used for alternative non-default views. 
* **Lazy init**: Instead of a `IQueryable<T>`, `RegisterQuery` requires a `Func<IQueryable<T>>`. The only reason is to avoid creating thousands of expression trees every time the application starts, but the lambda will be called just once. 
* **Entity property**: The first property, `Entity`, is mandatory and represents the entity that will be 'behind' each row in the result. The one that will be opened when double-click / view button and the one that will receive the contextual operations. 
* **Lite is optional**: While calling `ToLite` has important performance consequences when using the LINQ provider directly, here the registered queries will be always manipulated by the `DynamicQueryManager`, and one of this changes is adding `ToLite` to every column of type entity automatically.

### Metadata
Additionally, the query will be processed not only to be translated to the database, also to get some metadata. This metadata is used to inherit some information from the property/ies used in each column expression:

* **Localized names** that will be used in the headers.
* **Implementations** to allow smarter filters.
* **Unit** and **Format** to show the results properly.
* The **PropertyRoute** itself to allow removing any unauthorized column. 

This meta-data can be overriden using a different variation of `RegisterQuery`:

```C#
public class DynamicQueryManager
{
    public void RegisterQuery<T>(object queryName, Func<DynamicQueryCore<T>> lazyQueryCore, Implementations? entityImplementations = null)
}
```

And using `Column` method (or `ColumnDisplayName`) to override each column meta-data like this:

```C#
dqm.RegisterQuery(typeof(OrderEntity), () =>DynamicQueryCore.Auto(
    from o in Database.Query<OrderEntity>()
    select new
    {
        Entity = o.ToLite(),
        o.Id,
        o.State,
        o.Customer,
        o.Employee,
        o.OrderDate,
        Lines = o.Details.Count
    }).ColumnDisplayName(a => a.Lines, () => OrderMessage.Lines.NiceToString());
```

### Manual queries (Advanced)
So far we have seen how to use `RegisterQuery` to create a automatic dynamic queries. This types of queries are super-concise and inherit as much as they can from your entities. 

Sometimes you need more fine-grained control over how the query is executed. The typical scenario is concatenating rows from two different tables in the same result. 

In this case we use `RegisterQuery` in combination of `DynamicQuery.Manual`.

```C#
public static ManualDynamicQueryCore<T> Manual<T>(Func<QueryRequest, QueryDescription, DEnumerableCount<T>> execute)
```

`DynamicQuery.Manual` gives you complete control to return the results that you want from a `QueryRequest` from the user and a `QueryDefinition` required to create a `DQueryable`. 

Example: 

```C#
dqm.RegisterQuery(typeof(CustomerEntity), () => DynamicQuery.Manual((QueryRequest request, QueryDescription descriptions) =>
{
    var persons = Database.Query<PersonEntity>().Select(p => new
    {
        Entity = p.ToLite<CustomerEntity>(),
        Id = "P " + p.Id,
        Name = p.FirstName + " " + p.LastName,
        p.Address,
        p.Phone,
        p.Fax
    }).ToDQueryable(descriptions).AllQueryOperations(request);

    var companies = Database.Query<CompanyEntity>().Select(p => new
    {
        Entity = p.ToLite<CustomerEntity>(),
        Id = "C " + p.Id,
        Name = p.CompanyName,
        p.Address,
        p.Phone,
        p.Fax
    }).ToDQueryable(descriptions).AllQueryOperations(request);

    return persons.Concat(companies)
        .OrderBy(request.Orders)
        .TryPaginate(request.Pagination);

})
.ColumnProperyRoutes(a => a.Id, 
    PropertyRoute.Construct((PersonEntity comp) => comp.Id), 
    PropertyRoute.Construct((CompanyEntity p) => p.Id))
.ColumnProperyRoutes(a => a.Name, 
    PropertyRoute.Construct((PersonEntity comp) => comp.FirstName), 
    PropertyRoute.Construct((PersonEntity comp) => comp.LastName), 
    PropertyRoute.Construct((CompanyEntity p) => p.CompanyName))
.ColumnProperyRoutes(a => a.Address, 
    PropertyRoute.Construct((PersonEntity comp) => comp.Address), 
    PropertyRoute.Construct((PersonEntity comp) => comp.Address))
.ColumnProperyRoutes(a => a.Phone, 
    PropertyRoute.Construct((PersonEntity comp) => comp.Phone), 
    PropertyRoute.Construct((CompanyEntity p) => p.Phone))
.ColumnProperyRoutes(a => a.Fax, 
    PropertyRoute.Construct((PersonEntity comp) => comp.Fax), 
    PropertyRoute.Construct((CompanyEntity p) => p.Fax))
, entityImplementations: Implementations.By(typeof(PersonEntity), typeof(CompanyEntity)));
```

Using `ToDQueryable` we get a dynamic query (`DQueryable<T>`) that can be manipulated using the dynamic versions of `Select`, `SelectMany`, `Where`, `OrderBy`, `Unique`, `TryTake` and `TryPaginate`. Most of the time you just need to execute all the operations requested by the `SearchControl` using `AllQueryOperations` method.

> `DQueryable<T>` is generic, but the type T doesn't change after executing any operation (like `Select`), instead, the generic type is only used to make the compiler ensure that two manual queries have the exact same column when using `Concat` (in this case `persons` and  `companies`).

Finally, note how manual queries have no way to inherit the matadata, and all this information has to be manually set using `ColumnProperyRoutes`, and from there, the columns now where to get the `NiceName`, `Format`, `Unit` and athorization. 



## Registering expressions

The other main usage of `DynamicQueryManager` is to call `RegisterExpression`, that let any of our `expressionMethod` to be available for the user as a query token that he can use in the `SearchControl` for adding filters, columns or use it in any other extension (chart, word and email templates, etc...).

There are many overloads: 

```C#
public class DynamicQueryManager
{
    public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty)
    public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty, Func<string> niceName)
    public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> extensionLambda, Func<string> niceName, string key)
    public ExtensionInfo RegisterExpression(ExtensionInfo extension)
}

public class ExtensionInfo
{
   public ExtensionInfo(Type sourceType, LambdaExpression lambda, Type type, string key, Func<string> niceName)
}
```

Typically you only need to use the two first ones, and all the information is taken from there.

Let's suppose that we have an `expressionMethod` like this one: 

```C#
static Expression<Func<RegionEntity, IQueryable<TerritoryEntity>>> TerritoriesExpression =
    r => Database.Query<TerritoryEntity>().Where(a => a.Region == r);
public static IQueryable<TerritoryEntity> Territories(this RegionEntity r)
{
    return TerritoriesExpression.Evaluate(r);
}
```

This `expressionMethod` let's us simplify queries like this one: 

```C#
Database.Query<RegionEntity>().Where(r => !r.Territories().Any()).UnsafeDelete();
```

But `Territories` is a concept that is only available for programmers, without `RegisterExpression` the user is not able to take advantage of it in the SearchControl. Let's do it then: 

```C#
//In TerritoryLogic.Start
dqm.RegisterExpression((RegionEntity r) => r.Territories());
```

Now, the a new expression with key `"Territories"` has been registered on `RegionEntity` and returns an `IQueryable<TerritoryEntity>`. 

Unfortunately, the `NiceName` will always be `"Territories"`, independently of the user `CultureInfo` (logic assembly and arbitrary methods are not localized).

Let's fix that re-using the `NicePluralName` of `TerritoryEntity`: 

```C#
//In TerritoryLogic.Start
dqm.RegisterExpression((RegionEntity r) => r.Territories(), () => typeof(TerritoryEntity).NiceName());
```

## Executing queries (Advanced)

`DynamicQueryManager` also has a bunch of method that are used as a service by the `SearchControl`, `CountSearchControl`, etc... You shoudn't need to know about them if you're not doing internal plumbing.

```C#
public class DynamicQueryManager
{
   public QueryDescription QueryDescription(object queryName)
   public ResultTable ExecuteQuery(QueryRequest request)
   public int ExecuteQueryCount(QueryCountRequest request)
   public ResultTable ExecuteGroupQuery(QueryGroupRequest request)
   public Lite<IdentifiableEntity> ExecuteUniqueEntity(UniqueEntityRequest request)
}
```

* `QueryDescription`: Returns all the registered metadata of a query to configure the `SearchControl`, already filtered and localized for a particular user. 
* `ResultTable`: Represents a result that will be shown in the `SearchControl`. Think of it as a `DataTable` that can also contains complex types, like `Lite<T>`. 
* `QueryRequest`: Contains the `Columns`, `Filters`, `Orders`, and `Pagination` requested by the `SearchControl` when pressing search.   
* `QueryGroupRequest`: Contains the `Columns`, `Filters`, `Orders` requested by a control with `GroupBy` capabilities (like charting).

Additionally, many of this objects make use of the concept of `QueryToken`.

## QueryToken (Advanced)

A `QueryToken` is a chain of identifiers that can be used as a filter, or be added as a column in query. When the user explores the tables using a sequence of ComboBoxes in the `SearchControl`, he is ultimately creating a `QueryToken`.

There are many different types of `QueryToken`,  like `ColumnToken`, `EntityPropertyToken`, `CountToken`, `AggregateToken`,... all with a `Parent` property creating a chain.   

`QueryToken` can be parsed using `QueryUtils.Parse` and ultimately can be converted to a `System.Expression` that can be used in the LINQ provider.  