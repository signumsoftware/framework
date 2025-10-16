## Dynamic Queries

The Dynamic Query system is a layer built on top of the Signum LINQ provider, enabling dynamic manipulation of queries at runtime.

This system is not intended for direct use in client code. Instead, it is consumed as a service by components such as the Windows or Web `SearchControl`, `SearchValue`, and modules for Email, Reporting, Charting, etc...

## QueryLogic

`QueryLogic` is the main entry point for the dynamic query system. It is responsible for:
- Registering queries
- Registering expressions
- Executing various types of `QueryRequest`

## Registering Queries

The recommended way to register queries in Signum Framework is using the fluent `WithQuery` style inside your static logic registration method:

```csharp
public static void Start(SchemaBuilder sb)
{
    if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
    {
        sb.Include<OrderEntity>()
            .WithQuery(o => new
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
    }
}
```

**Explanation:**
- `sb.Include<OrderEntity>()` registers the entity and returns a `FluentInclude<OrderEntity>`.
- `.WithQuery(...)` registers the main query for the entity, using a LINQ projection.
- You can chain other methods like `.WithUniqueIndex`, `.WithSave`, `.WithDelete`, etc., to keep all logic for the entity together.
- This style is concise, readable, and encourages grouping all entity logic in one place.

---

Alternatively, you can use the more general `Register` method, which is still supported and useful for more complex scenarios or advanced manual queries:

```csharp
public void Register<T>(object queryName, Func<IQueryable<T>> lazyQuery, Implementations? entityImplementations = null)
```

Example:

```csharp
QueryLogic.Queries.Register(typeof(OrderEntity), () =>
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
```

`QueryLogic.Queries.Register` is more verbose but clearly expresses the main point: an IQueryable is registered with a query name (object). It also allows you to customize the metadata using overloads or by returning a DynamicQueryCore (see below for examples).

For more advanced scenarios, you can use overloads that accept a `DynamicQueryCore<T>` and chain metadata overrides as needed.

## Registering Named Queries

Named queries allow you to register queries using an enum as a key (instead of `typeof(T)`) for alternative views of the same entity or custom joins.

Example:

```csharp
// Default query for OrderEntity
QueryLogic.Queries.Register(typeof(OrderEntity), () =>
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

// Named query for OrderEntity (alternative view)
QueryLogic.Queries.Register(OrderQuery.OrderLines, () =>
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

While named queries are supported, they should be reserved for exceptional cases where you need special joins or filters that cannot be achieved by configuring the SearchControl. Most projections and views should be handled by customizing the SearchControl with filters, orders, and columns. Prefer keeping your queries simple and use named queries only when necessary.

### Metadata

Queries are processed not only for translation to the database, but also to extract metadata, such as:

- **Localized names** for column headers
- **Implementations** for smarter filters
- **Unit** and **Format** for proper display
- The **PropertyRoute** for authorization and column removal

You can override this metadata using a different overload:

```csharp
public void RegisterQuery<T>(object queryName, Func<DynamicQueryCore<T>> lazyQueryCore, Implementations? entityImplementations = null)
```

And use the `Column` or `ColumnDisplayName` methods to override column metadata:

```csharp
QueryLogic.Queries.Register(typeof(OrderEntity), () => DynamicQueryCore.Auto(
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
    }).ColumnDisplayName(a => a.Lines, () => OrderMessage.Lines.NiceToString()));
```

### Manual Queries (Advanced)

For advanced scenarios, such as combining rows from multiple tables, use `RegisterQuery` with `DynamicQuery.Manual`:

```csharp
public static ManualDynamicQueryCore<T> Manual<T>(Func<QueryRequest, QueryDescription, DEnumerableCount<T>> execute)
```

Example:

```csharp
QueryLogic.Queries.Register(typeof(CustomerEntity), () => DynamicQuery.Manual((QueryRequest request, QueryDescription description) =>
{
    var persons = Database.Query<PersonEntity>().Select(p => new
    {
        Entity = p.ToLite<CustomerEntity>(),
        Id = "P " + p.Id,
        Name = p.FirstName + " " + p.LastName,
        p.Address,
        p.Phone,
        p.Fax
    }).ToDQueryable(description).AllQueryOperations(request);

    var companies = Database.Query<CompanyEntity>().Select(p => new
    {
        Entity = p.ToLite<CustomerEntity>(),
        Id = "C " + p.Id,
        Name = p.CompanyName,
        p.Address,
        p.Phone,
        p.Fax
    }).ToDQueryable(description).AllQueryOperations(request);

    return persons.Concat(companies)
        .OrderBy(request.Orders)
        .TryPaginate(request.Pagination);

})
.ColumnPropertyRoutes(a => a.Id, 
    PropertyRoute.Construct((PersonEntity p) => p.Id), 
    PropertyRoute.Construct((CompanyEntity c) => c.Id))
.ColumnPropertyRoutes(a => a.Name, 
    PropertyRoute.Construct((PersonEntity p) => p.FirstName), 
    PropertyRoute.Construct((PersonEntity p) => p.LastName), 
    PropertyRoute.Construct((CompanyEntity c) => c.CompanyName))
.ColumnPropertyRoutes(a => a.Address, 
    PropertyRoute.Construct((PersonEntity p) => p.Address), 
    PropertyRoute.Construct((CompanyEntity c) => c.Address))
.ColumnPropertyRoutes(a => a.Phone, 
    PropertyRoute.Construct((PersonEntity p) => p.Phone), 
    PropertyRoute.Construct((CompanyEntity c) => c.Phone))
.ColumnPropertyRoutes(a => a.Fax, 
    PropertyRoute.Construct((PersonEntity p) => p.Fax), 
    PropertyRoute.Construct((CompanyEntity c) => c.Fax)),
    entityImplementations: Implementations.By(typeof(PersonEntity), typeof(CompanyEntity)));
```

`ToDQueryable` returns a dynamic query (`DQueryable<T>`) that supports dynamic versions of `Select`, `SelectMany`, `Where`, `OrderBy`, `Unique`, `TryTake`, and `TryPaginate`. Usually, you just need to call `AllQueryOperations(request)` to apply all requested operations.

> Note: `DQueryable<T>` is generic, but the type parameter is only used to ensure that manual queries have matching columns when using `Concat`.

Manual queries do not inherit metadata automatically. All metadata must be set using `ColumnPropertyRoutes`, which provides the necessary information for display names, formats, units, and authorization.


## Executing Queries (Advanced)

`QueryLogic.Queries` provides several methods used by `SearchControl`, `SearchValue`, etc. You typically do not need to use these directly unless doing internal plumbing.

```csharp
public QueryDescription QueryDescription(object queryName)
public ResultTable ExecuteQuery(QueryRequest request)
public int ExecuteQueryCount(QueryCountRequest request)
public ResultTable ExecuteGroupQuery(QueryGroupRequest request)
public Lite<IdentifiableEntity> ExecuteUniqueEntity(UniqueEntityRequest request)
```

- `QueryDescription`: Returns all registered metadata for a query, filtered and localized for the user.
- `ResultTable`: Represents a result for the `SearchControl`, similar to a `DataTable` but can contain complex types like `Lite<T>`.
- `QueryRequest`: Contains the `Columns`, `Filters`, `Orders`, and `Pagination` requested by the `SearchControl`.

Many of these objects use the concept of a `QueryToken`.

## QueryToken (Advanced)

A `QueryToken` is a chain of identifiers used as filters or columns in a query. When the user explores tables using a sequence of DropDownList in the `SearchControl`, they are building a `QueryToken`.

There are several types of `QueryToken`, such as `ColumnToken`, `EntityPropertyToken`, `CountToken`, and `AggregateToken`, all with a `Parent` property forming a chain.

`QueryToken` can be parsed using `QueryUtils.Parse` and ultimately converted to a `System.Expression` for use in the LINQ provider.
