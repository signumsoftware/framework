# OperationServer

`OperationServer` class contains extension methods over entities to call `IOperationServer` from a Signum.Windows client application simulating the same strongly-typed experience that you could have in the server using [`OperationLogic`](../../Signum.Engine/Operations/Operation.md) class. 

> If you find this method signatures hard to understand, asume that `T == B` and `F == FB`. The complexity comes from the operations being co-variant.  

Using this methods we can invoke, in the windows client, similar code that in the server: 

### Execute: 

```C#
public static class OperationServer
{
    public static T Execute<T, B>(this T entity, ExecuteSymbol<B> symbol, params object[] args) 
		where T : class, IIdentifiable
        where B : class, IIdentifiable, T

    public static T ExecuteLite<T, B>(this Lite<T> lite, ExecuteSymbol<B> symbol, params object[] args)
        where T : class, IIdentifiable
        where B : class, IIdentifiable, T
}
```

> If you find this method signatures hard to understand, asume that `T == B` the complexity comes from the operations being co-variant.  

Example: 

```C#
var order = new OrderEntity().Execute(OrderOperation.SaveNew);  //Entity is new but works because AllowsNew = true
oder.Customer = customer
order = order.Execute(OrderOperation.Save); //Entity is dirty but works because Lite = false
order = order.ToLite().Execute(OrderOperation.Ship); //Entity will be retrieved from the database
order.Execute(OrderOperation.Ship); //Also works because entity is clean
```

> Note that in the server, `Execute` returns the entity to be fluent, but modified the entity **by reference**. But in the client the new modified instance is returned back and have to be **assigned** again to the variable. 


### Delete

```C#
public static class OperationServer
{
    public static void Delete<T, B>(this Lite<T> lite, DeleteSymbol<B> symbol, params object[] args)
        where T : class, IIdentifiable
        where B : class, IIdentifiable, T

    public static void DeleteLite<T, B>(this Lite<T> lite, DeleteSymbol<B> symbol, params object[] args)
        where T : class, IIdentifiable
        where B : class, IIdentifiable, T
}
```

> If you find this method signatures hard to understand, asume that `T == B` the complexity comes from the operations being co-variant.  

Example: 

```C#
order.ToLite().Delete(OrderOperation.Delete); //Entity will be retrieved from the database
order.Delete(OrderOperation.Ship); //Also works if entity is clean
```

### Construct

```C#
public static class OperationServer
{
    public static T Construct<T>(ConstructSymbol<T>.Simple symbol, params object[] args)
        where T : class, IIdentifiable
}
```

Execute: 

```C#
OrderEntity order = OperationLogic.Construct(OrderOperation.Create); //Type inferred from OrderOperation.Create 
```

### ConstructFrom


```C#
public static class OperationServer
{
    public static T ConstructFrom<F, FB, T>(this F entity, ConstructSymbol<T>.From<FB> symbol, params object[] args)
        where T : class, IIdentifiable
        where FB : class, IIdentifiable
        where F : class, IIdentifiable, FB

    public static T ConstructFromLite<F, FB, T>(this Lite<F> lite, ConstructSymbol<T>.From<FB> symbol, params object[] args)
        where T : class, IIdentifiable
        where FB : class, IIdentifiable
        where F : class, IIdentifiable, FB
}
```

Execute:

```C#
//Type inferred from OrderOperation.CreateOrderFromCustomer 
OrderEntity order = customer.ConstructFrom(OrderOperation.CreateOrderFromCustomer); 
```

### ConstructFromMany

```C#
public static class OperationServer
{
    public static T ConstructFromMany<F, FB, T>(List<Lite<F>> lites, ConstructSymbol<T>.FromMany<FB> symbol, params object[] args)
        where T : class, IIdentifiable
        where FB : class, IIdentifiable
        where F : class, IIdentifiable, FB
}
```

Execute:

```C#
//Type inferred from OrderOperation.CreateOrderFromProducts 
OrderEntity order = OperationLogic.ConstructFromMany(OrderOperation.CreateOrderFromProducts, products); 
```