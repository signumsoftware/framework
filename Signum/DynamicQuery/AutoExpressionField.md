# AutoExpressionField Pattern

## Overview

`AutoExpressionField` is the main pattern in Signum Framework for defining calculated properties on entities or methods with reusable query fragments. It allows you to encapsulate calculations or query logic in a single place, making them available both in-memory and in LINQ queries.

---

## Using AutoExpressionField In Properties

You can use `[AutoExpressionField]` on a property to define its logic with `As.Expression`, making it available both in-memory and in LINQ queries.

**Example: Scalar Calculation**

```csharp
public class OrderEntity : Entity
{
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    [AutoExpressionField]
    public decimal TotalPrice => As.Expression(() => UnitPrice * Quantity);
}
```

* TIP: Use `expressionProperty` snippet to save some keystrokes.

## Using AutoExpressionField In Methods

You can also use `[AutoExpressionField]` on static extension methods to encapsulate reusable query fragments.

**Example: Query Fragment (IQueryable as Extension Method)**

```csharp
public static class CustomerLogic
{
    [AutoExpressionField]
    public static IQueryable<OrderEntity> RecentOrders(this CustomerEntity c) =>
        As.Expression(() => Database.Query<OrderEntity>()
            .Where(o => o.Customer.Is(c) && o.OrderDate > DateTime.Now.AddMonths(-1)));
}
```
* TIP: Use `expressionMethod` or `expressionMethodQuery` snippet to save some keystrokes.

- You can now use `customer.RecentOrders()` in LINQ queries, and it will be translated to SQL, or in-memory when working with loaded entities.
- If you want to expose this method in the UI (e.g., SearchControl, charts, templates), you need to register it as described below.

- 
### Registering Expressions Methods for the UI

 While properties in entities are automatically discovered, methods in static classes need to be registered to be available in the UI (e.g., SearchControl, charts, templates). To do this, register the expression using:

```csharp
QueryLogic.Expressions.Register((ProductEntity e) => e.RecentOrders());
```

This enables the property or method to be used as a query token in the UI and other dynamic query scenarios.

More information on registering expressions can be found in the [Query Expressions documentation](./DynamicQueries.md). 
---

## Advanced: ExpressionField Attribute
Internally, `[AutoExpressionField]` pattern is translated by  `Signum.MSBuildTask` to using the `[ExpressionField]` attribute and a generated static expression field.

```csharp
public class OrderEntity : Entity
{
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public static Expression<Func<OrderEntity, decimal>> TotalPriceExpression =
        o => o.UnitPrice * o.Quantity;

    [ExpressionField(nameof(TotalPriceExpression))]
    public decimal TotalPrice => TotalPriceExpression.Evaluate(this);
}
```

Then, the LINQ provider uses `TotalPriceExpression` for translating queries, while in-memory evaluation uses the `TotalPrice` property.

In most cases, you should use `AutoExpressionField` for simplicity and consistency. However, you can use `[ExpressionField]` directly for advanced scenarios where you want to separate the database and in-memory implementations. This is useful, for example, when you want to use cached entities in-memory but generate an expression for database queries.

**Example: Separate In-Memory and DB Logic Using Lite<T>.Entity**

```csharp
public class OrderEntity : Entity
{
    public Lite<CustomerEntity> Customer { get; set; }

    public static Expression<Func<OrderEntity, string>> CustomerNameExpression =
        o => o.Customer.Entity.Name; //Used in queries

    [ExpressionField(nameof(CustomerNameExpression))]
    public string CustomerName => this.Customer.RetrieveFromCache().Name; // Used in-memory, 
}
```
