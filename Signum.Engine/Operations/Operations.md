# Operations

Signum Framework provides an standardized way of writing your business logic to manipulate the entities, called Operations. 

Operations don't let you do anything new that you couldn't do before, but they formalize a standard pattern and bring to your business logic a certain level of homogeneity that other parts of the framework can take advantage of.

Operations scale gracefully with the complexity of your application, from a simple Save button, to a complex state machine. 

### Advantages

By using operations, instead of plain methods, to Create / Modify / Delete your entities you get a lot of benefits: 

* **Automatic UI**: Operations are defined in the server but Signum.Windows, Signum.Web, and Signum.React show a button in the user interface of the associated entity type. Also in the SearchControl the operations are available using a context menu. Of course the buttons can be hidden if necessary. 

* **Preconditions**: Some operations have a precondition that returns an string with the error and are asserted before executing the operation. More important, the preconditions are also evaluated when the automatic buttons are shown, disabling and adding a tool-tip to the operation buttons that do not satisfy the precondition.

* **Automatic logging**: Every time an operation is executed, an `OperationLogEntity` is saved in the database indicting the entity, operation, user, start and end time and possible exception. Even more, using DiffLog module also a dump of the initial and final state of the entity is saved, so you can have a full history of the entity with diffs. 

* **Inheritance support**: If you have complex hierarchies of entities, you can have polymorphic behavior using operations as well, even if they are defined outside of the entity, because internally they are implemented using `Polymorphic<T>`.      

* **Extension point**: Each operation implementation, defined in a module, can be easily replaced by a custom implementation if necessary. By using operations for your business logic you're automatically introducing many extension points. 

* **Common facades**: All the operation share a common set of Web Service Operations (Windows) or Controller Actions (Web / React) saving you hours of code.

* **Transactional**: Your operation implementation is transactional.

* **Operations + Auth module**: When using Authorization module, operation can easily be allowed / disallowed for certain roles, disappearing from the user interface. 

* **Operations + Processes module**: When using Processes module, they can easily be executed for multiple entities at once, using a context menu in the search dialog. 


## Types of operations

There are five types of operations: 

* `Construct`: Create a new entity with no additional context *(e.g., Create new Invoice)*
* `ConstructFrom`: Create a new entity from another one *(e.g., Create Invoice from Customer)*
* `ConstructFromMany`: Create a new entity from many others *(e.g., Create Invoice from a list of Products)*
* `Execute`: Modify an entity *(e.g., Authorize Invoice, Cancel Invoice)*
* `Delete`: Delete an entity from the database *(e.g., Delete Order)*

Additionally, some operations can be embedded in a graph. 


## Declaring Operations

Under the cover, all operation are identified by an `OperationSymbol` (if you don't know what a `Symbol` is, [take a look](../../Signum.Entities/Symbol.md)).

```C#
[Serializable]
public class OperationSymbol : Symbol
{
     ...
}
```

But we don't defined or invoke operations use a raw `OperationSymbol`, instead we use strongly-typed containers that give information to the compiler about the type of the operation (`Construct`, `ConstructFrom`, `ConstructFromMany`, `Execute`, `Delete`...) and the entity type (`BugEntity`, `EmployeeEntity`, ...).

Example for `Execute` and `Delete`: 

```C#
[AutoInit]
public static class AlbumOperation
{
    public static ExecuteSymbol<OrderEntity> SaveNew;
    public static ExecuteSymbol<OrderEntity> Save;
    public static ExecuteSymbol<OrderEntity> Ship;
    public static ExecuteSymbol<OrderEntity> Cancel;

    public static DeleteSymbol<OrderEntity> Delete;
}
```

> Note: As you see, the syntax is verdy declarative, resembling an enum, but we're actually declaring fields in a static class that will be automatically initialized by Signum.MSBuildTask. The `AutoInitAttribute` enables this magic. 

Declaring `Construct`, `ConstructFrom` and `ConstructFromMany` is a bit more complex, example:  

```C#
public static class AlbumOperation
{
    public static ConstructSymbol<OrderEntity>.Simple Create;
    public static ConstructSymbol<OrderEntity>.From<CustomerEntity> CreateOrderFromCustomer;
    public static ConstructSymbol<OrderEntity>.FromMany<ProductEntity> CreateOrderFromProducts;
}
```

> By using inner types we can differentiate the two types of a `ConstructFrom` and `ConstructFromMany` operation. 

Operations **should be declared in the Entities assembly**, so they can also be used in a Windows application.

Each declared operation field name will be used as the label for the UI buttons, accepts the `Description` attribute and can be localized. 

Even more, the framework recognizes the pattern 'CreateXXXFromYYY' in a `ConstructFrom`, simplifying the names in the user interface. 

## Implementing Operations

Operations are **declared** in the entities assembly, using the static factory methods in `OperationSymbol`.

But operations are **implemented** in the logic assembly, by instantiating an objects of some inner classes inside `Graph<T>` (simple) and `Graph<T, S>` (with state) and registering them in `OperationLogic` class with `Register` extension method. 

```C#
public static class OperationLogic
{
    public static void Register(this IOperation operation)
}
```

Simple example in `OrderLogic.Start` instantiating inner classes directly: 

```C#
new Graph<OrderEntity>.Execute(OrderOperation.Save)
{
    CanBeModified = true,
    Execute = (o, _) =>
    {
    }
}.Register();

new Graph<OrderEntity>.Delete(OrderOperation.Delete)
{
    Delete = (o, args) =>
    {
        o.Delete();
    }
}.Register();
```

But since they are inner classes, we can also create a `OrderGraph` class, inheriting from `Graph<OrderEntity>`, and group all the operations for the same type together.

```C#
public class OrderGraph : Graph<OrderEntity>
{   
    public static void Register()
    { 
        new Execute(OrderOperation.Save)
		{
		    CanBeModified = true,
		    Execute = (o, _) =>
		    {
		    }
		}.Register();
		
		new Delete(OrderOperation.Delete)
		{
		    Delete = (o, args) =>
		    {
		        o.Delete();
		    }
		}.Register();
    }
}

//In your OrderLogic.Start method
OrderGraph.Register();
```

Assuming that `OrderEntity` has a `State` property with the following values: 

```C#
public enum OrderState
{
    [Ignore]
    New,
    Ordered, 
    Shipped,
    Canceled,
}
```

Grouping all the operations make even more sense when using `Graph<T, S>` to model state machines, and setting `GetState` at the beginning.  

```C#
public class OrderGraph : Graph<OrderEntity, OrderState>
{   
    public static void Register()
    { 
        GetState = o => o.State; //Common for all the graph

        new Execute(OrderOperation.Save)
        {
            FromStates = { OrderState.Ordered }, //New property
            ToStates = { OrderState.Ordered },
            CanBeModified = true,
            Execute = (o, _) =>
            {
            }
        }.Register();
		
        new Delete(OrderOperation.Delete)
        {
            FromStates = { OrderState.Ordered},
            Delete = (o, args) =>
            {
                o.Delete();
            }
        }.Register();
    }
}

//In your AlbumLogic.Start method
OrderGraph.Register();
```
 

### Execute 

`Graph<T>.Execute` and `Graph<T, S>.Execute` are the most common operation types, they just modify an entity (Save Order, Send Order, Cancel Order). 

In the **UI** this operations are shown as buttons in the top of the entity control, or context menus in the search control.

It has the following members: 

* **Execute:** An `Action<T, object[]>` to be executed when the operation is invoked. The action will be surrounded in a transaction and the entity will also be implicitly saved at the end.
* **CanExecute:** A function that returns whether a method could be executed in the current state of the entity or not. If there is a problem it returns a `string` with the explanation, otherwise `null`. 
* **AllowNew:** A bool controlling whether the operation can be executed over new entities or not. By default `false` and is typically set to `true` for `Save` operations.
* **Lite:** When `true`, the database version of the entity is taken, otherwise the user entity is used (possibly with some changes). By default `true` and is typically set to `false` for `Save` operations.

And, only for `Graph<T, S>`: 
* **FromStates:** The states of the entity from which the operation can be executed. 
* **ToStates:** The valid states the entity could end up at the end of the execution.

Example implementing some `Execute` operations:

```C#
public class OrderGraph : Graph<OrderEntity, OrderState>
{   
    public static void Register()
    { 
        ...

		new Execute(OrderOperation.SaveNew) 
		{
		    FromStates = { OrderState.New }, //The operation can only be executed for new entities
		    ToStates = { OrderState.Ordered }, //After the execution, Ordered state will be asserted
		    CanBeNew = true, //Can be executed for new entities
		    CanBeModified = true, //The whole entity will be sent, and can be dirty
		    Execute = (o, args) =>
		    {
		        o.OrderDate = DateTime.Now;
		        o.State = OrderState.Ordered;
		    }
		}.Register();
		
		new Execute(OrderOperation.Save)
		{
		    FromStates = { OrderState.Ordered },
		    ToStates = { OrderState.Ordered },
		    CanBeModified = true, //The whole entity will be sent, and can be dirty
		    Execute = (o, _) =>
		    {
		    }
		}.Register();
		
		new Execute(OrderOperation.Ship) 
		{
		    CanExecute = o => o.Details.IsEmpty() ? "No order lines" : null, //Special CanExecute
		    FromStates = { OrderState.Ordered },
		    ToStates = { OrderState.Shipped }, 
            //Lite = true by default, so only a lite (or a clean entity) can be used
		    Execute = (o, args) =>
		    {
		        o.ShippedDate = DateTime.Now;
		        o.State = OrderState.Shipped;
		    }
		}.Register();

        ...
   }
}
```

Example invoking the operatons using `OperationLogic.Execute` extension method: 

```C#
var order = new OrderEntity().Execute(OrderOperation.SaveNew);  //Entity is new but works because AllowsNew = true
order.Customer = customer
order.Execute(OrderOperation.Save); //Entity is dirty but works because Lite = false
order.ToLite().Execute(OrderOperation.Ship); //Entity will be retrieved from the database
order.Execute(OrderOperation.Ship); //Also works because entity is clean
```

### Delete
`Graph<T>.Delete` and `Graph<T, S>.Delete` are used to phisically delete the entities from the database. In order to use logical delete, just use `Execute`. It has the following members: 

In the **UI** this operations are shown as buttons in the top of the entity control, or context menus in the search control, both showing a confirmation dialog by default.

* **Delete:** An `Action<T, object[]> ` that deletes the entity, usually by calling `Database.Delete`. The entity is **not** implicitly deleted.  
* **CanDelete:** A function that returns whether the entity can be deleted in the current state. If there is a problem returns an `string` with the explanation, otherwise `null`. 
* **Lite:** When `true`, the database version of the entity is taken, otherwise the UI entity is used (possibly with some changes). By default `true`.

And, only for `Graph<T, S>`: 
* **FromStates:** The states of the entity from which can be deleted. 

```C#
public class OrderGraph : Graph<OrderEntity, OrderState>
{   
    public static void Register()
    { 
        ...
        new Delete(OrderOperation.Delete)
        {
            FromStates = { OrderState.Ordered},
            Delete = (o, args) =>
            {
                o.Delete();
            }
        }.Register();
        ...
   }
}
```


Example invoking the operations using `OperationLogic.Delete` extension method: 

```C#
order.ToLite().Delete(OrderOperation.Delete); //Entity will be retrieved from the database
order.Delete(OrderOperation.Ship); //Also works if entity is clean
```

< NOTE: Do not confuse with the low-level `Database.Delete` extension method, that will not save any log, evaluate CanExecute, etc...

```C#
order.Delete(); 
```

### Construct
`Graph<T>.Construct` and `Graph<T, S>.Construct` are used to create new entities from nothing. The returned entity is usual new (`IsNew = true`) but returning saved entities is also useful in some scenarios. 

In the **UI**, this operations will automatically invoked in the UI when the user press the plus (+) button in the SearchControl or EntityLines. A chooser will be shown if more than one `Construct` is registered. 

It has the following members: 

* **Construct:** A `Func<object[], T>` that will create the entity and, optionally, save it.  

And, only for `Graph<T, S>`: 
* **ToState:** the state the entity should be at the end of the construction.

```C#
public class OrderGraph : Graph<OrderEntity, OrderState>
{   
    public static void Register()
    { 
        ...
        new Construct(OrderOperation.Create)
        {
            ToStates = { OrderState.New },
            Construct = (_) => new OrderEntity
            {
                State = OrderState.New,
                Employee = EmployeeEntity.Current.ToLite(),
                RequiredDate = DateTime.Now.AddDays(3),
            }
        }.Register();
        ...
   }
}
```

Manual invocation using `OperationLogic.Construct`:

```C#
OrderEntity order = OperationLogic.Construct(OrderOperation.Create); //Type inferred from OrderOperation.Create 
```

### ConstructFrom
`Graph<T>.ConstructFrom<F>` and `Graph<T, S>.ConstructFrom<F>` are used to create new entities from other entities. The returned entity is usual new (`IsNew = true`) but returning saved entities is also useful in some scenarios. 

In the **UI** this operations are shown as menu items grouped in the top of the main view of the **from** entity, inside of the `Create...` button, or as context menus in the search control for **from** entities.

It has the following members: 

* **Construct:** A `Func<F, object[], T>` that create the entity (and optionally save it) from the **from** entity.
* **CanConstruct:** A function that returns whether an entity can be executed in the current state of the **from** entity. If there is a problem returns an `string` with the explanation, otherwise `null`. 

And, only for `Graph<T, S>`: 
* **ToState:** the state the entity should be at the end of the construction.

```C#
public class OrderGraph : Graph<OrderEntity, OrderState>
{   
    public static void Register()
    { 
        ...
        new ConstructFrom<CustomerEntity>(OrderOperation.CreateOrderFromCustomer)
        {
            ToStates = { OrderState.New },
            Construct = (c, _) => new OrderEntity
            {
                State = OrderState.New,
                Customer = c,
                Employee = EmployeeEntity.Current.ToLite(),
                ShipAddress = c.Address,
                RequiredDate = DateTime.Now.AddDays(3),
            }
        }.Register();
        ...
   }
}
```

Manual invocation using `OperationLogic.ConstructFrom` extension method:

```C#
//Type inferred from OrderOperation.CreateOrderFromCustomer 
OrderEntity order = customer.ConstructFrom(OrderOperation.CreateOrderFromCustomer); 
```

### ConstructFromMany
`Graph<T>.ConstructFromMany` and `Graph<T, S>.ConstructFromMany` are used to create new entities from a bunch of other entities. The returned entity is usual new (`IsNew=true`) but returning saved entities is also useful in some scenarios. 

In the **UI** this operations are shown only as context menus in the search control of the **from** entities.

It has the following members: 

* **Construct:** A '`Func<List<Lite<F>>, object[], T>` that fill create the entity (and optionally save it) from a set of selected `Lite<F>`. 

And, only for `Graph<T, S>`: 
* **ToState:** the state the entity should be at the end of the construction.

```C#
public class OrderGraph : Graph<OrderEntity, OrderState>
{   
    public static void Register()
    { 
        ...
        new ConstructFromMany<ProductEntity>(OrderOperation.CreateOrderFromProducts)
        {
            ToStates = { OrderState.New },
            Construct = (prods, _) =>
            {
                var dic = Database.Query<ProductEntity>()
                    .Where(p => prods.Contains(p.ToLite()))
                    .Select(p => new KeyValuePair<Lite<ProductEntity>, decimal>(p.ToLite(), p.UnitPrice)).ToDictionary();

                return new OrderEntity
                {
                    State = OrderState.New,
                    Employee = EmployeeEntity.Current.ToLite(),
                    RequiredDate = DateTime.Now.AddDays(3),
                    Details = prods.Select(p => new OrderDetailsEntity
                    {
                        Product = p,
                        UnitPrice = dic[p],
                        Quantity = 1,
                    }).ToMList()
                };
            }
        }.Register();
        ...
   }
}
```

Manual invocation using `OperationLogic.ConstructFromMany` method:

```C#
//Type inferred from OrderOperation.CreateOrderFromProducts 
OrderEntity order = OperationLogic.ConstructFromMany(OrderOperation.CreateOrderFromProducts, products); 
```

## Extra parameters

Maybe you have notice that all the operations take an extra `object[]` in their `Action`/`Func`. In this parameter the client code can pass any extra parameter that he finds necessary.

For example, we could have defined:

```C#
new Execute(OrderOperation.Ship) 
{
    (...)
    Execute = (o, args) =>
    {
        o.ShippedDate = args.TryGetArgS<DateTime>() ?? DateTime.Now;
        o.State = OrderState.Shipped;
    }
}.Register(); 
```

And then we can invoke it like this: 

```C#
order.Execute(OrderOperation.Ship, DateTime.Now.AddDays(10)); 
```

Because of the loose nature of using an `object[]`, we use the extension method defined in `ArgsExtensions` to find the parameters of a particular type: 

```C#
public static class ArgsExtensions
{
    public static T GetArg<T>(this IEnumerable<object> args)
    public static T TryGetArgC<T>(this IEnumerable<object> args) where T : class
    public static T? TryGetArgS<T>(this IEnumerable<object> args) where T : struct
}
```

Using this technique, our code is not that picky if we pass the parameter in the wrong position, simplifying the interaction between client code and implementaton in the absence of a formal function signature. 

Of course, the limitation is that you can not pass more than one parameter of the same type, in that case you'll need to define a custom type with the two arguments: 

```C#
public class DatePair
{
    public DateTime ShipDate;
    public DateTime RequiredDate; 
}
```

This family of methos also try to dynamically cast any `List<object>` into a typed list. This is usefull for simplifying invocations from Json REST services that do not provice type information. Example: 


```C#
order.Execute(OrderOperations.Ship, new List<object>{ DateTime.Now });


new Execute(OrderOperation.Ship) 
{
    (...)
    Execute = (o, args) =>
    {
	    //The List<object> is dynamically converted to a List<DateTime>
        o.ShippedDate = args.TryGetArgS<List<DateTime>>().FirstOrDefault() ?? DateTime.Now;
        o.State = args OrderState.Shipped;
    }
}.Register(); 
```

## Inheritance

In the case of complex entity hierarchies, Operations behave using polymoprhism. 

So, if you declare the operation just once:

```C#
public static class AnimalOperation
{
    public static ExecuteSymbol<AnimalEntity> Eat;   
}
``` 

And implement it in two different ways: 

```C#
new Graph<AnimalEntity>.Execute(AnimalOperation.Eat)
{
    CanBeModified = true,
    Execute = (o, _) =>
    {
         o.State = "just eating like an animal"
    }
}.Register();

new Graph<LionEntity>.Execute(AnimalOperation.Eat)
{
    CanBeModified = true,
    Execute = (lion, _) =>
    {
         lion.State = "Eating like a LION!!!"
    }
}.Register();
```

Then if we execute: 

```C#
AnimalEntity a = Rand() ? myDog : myLion; 

a.Execute(AnimalOperation.Eat);
```

Will execute one of the other implementation depending the result of Rand.

## Replacing operations

Even more interesting is the ability to replace the implementations of operations already defined in a module that we don't have control of.  

We can replace the whole operation using `RegisterReplace`: 

```C#
new Execute(OrderOperation.Ship)
{
    CanExecute = o => o.Details.IsEmpty() ? "No order lines" : null,
    FromStates = { OrderState.Ordered },
    ToStates = { OrderState.Shipped },
    Execute = (o, args) =>
    {
        o.ShippedDate = args.TryGetArgS<DateTime>() ?? DateTime.Now;
        o.State = OrderState.Shipped;
    }
}.RegisterReplace();
```

Or you can get the current definition and replace just some parts like the `Execute` function: 

```C#
OperationLogic.FindExecute<OrderEntity>(OrderOperation.Ship)
    .OverrideExecute(baseExecute => (o, args) =>
    {
        baseExecute(o, args);
        //add some thing at the end here
    });
```

Or the `CanExecute`: 

```C#
OperationLogic.FindExecute<OrderEntity>(OrderOperation.Ship)
     .OverrideCanExecute(baseCanExecute => o =>
     {
         return baseCanExecute(o) ?? "add some preconditions here"
     }); 
```

## Visualize State Machines

While Operations (like most of the framework) promotes C# as the modeling tool and we have choose not to design the operations graphs visually to avoid having to keep two files in sync, `Graph<E, S>` contains some handy methods to return a `DGML` to visualize the operations and transitions between states. 

Example:

```C#
Graph<OrderEntity, OrderState>.ToDGML()
Graph<OrderEntity, OrderState>.ToDirectedGraph()
```

>> Note: Signum.Extensions contains a `Map` module that can show the same state machine diagram when writing `map Order` in the Omnibox.  