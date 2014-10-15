# OperationClient

`OperationClient` is the class responsible of providing the services to `NormalPage` and `SearchControl` to show the available [Operations](../../Signum.Engine/Operations/Operations.md) for an entity as `ToolBarButton` (in the first case) or `IMenuItem` (for the second). 

Just as `Finder` and `Navigator` have a manager, `OperationClient` has an `OperationManager` that can be overridden if necessary to customize the behavior. 

`OperationClient` also let you customize properties of one particular operation using `OperationSettings`.

## OperationSettings

`OperationClient` class contains a set of methods to customize the appearance and behavior of particular operations using `OperationSettings`. 

```C#
public static class OperationClient
{
    public static void AddSetting(OperationSettings setting)
    public static void AddSettings(List<OperationSettings> settings)
}
```

Under the covers, this method add the `OperationSettings` to the `Settings` polymorphic dictionary in `OperationManager`.

`OperationSettings` is an abstract class used to customize the properties of one particular operation. 

```C#
public abstract class OperationSettings
{
    public OperationSymbol OperationSymbol { get; }
    public string Text { get; set; }

    public abstract Type OverridenType { get; }

    public OperationSettings(IOperationSymbolContainer symbol)
}
```

* **OperationSymbol:** The `OperationSymbol` is used as key to identify the operation to override, but a `IOperationSymbolContainer` has to be used to the constructor, like the ones you declare manually:
	* `ExecuteSymbol<in T>`
	* `DeleteSymbol<in T>`
	* `ConstructSymbol<T>.Simple`
	* `ConstructSymbol<T>.From<in F>`
	* `ConstructSymbol<T>.FromMany<in F>`
* **Text:** Let you override the `Text` that will be used in buttons, menu items, or constructor choosers. By default the `NiceToString` of the `OperationSymbol` will be used. 
* **OverridenType:** The `OperationSettings` are registered in `OperationClient` using a a [`Polymorphic<T>`](../../Signum.Utilities/Polymorphic.md) (just as in `OperationLogic`), allowing you to override the client implementation of an operation (i.e. `Eat`) for a particular type (i.e. `Lion`) even if the operation is defined for a more general type (i.e. `Animal`). 

There are three types operation settings: 

* **ConstructorOperationSettings:** To customize how a **`Construct`** operation behaves when clicking create button (+) in a [EntityControl](../EntityControls/EntityControls.md), or `SearchControl`. Rarely used.
* **ContextualOperationSettings:** To customize how a **`ConstructFromMany`** operation behaves when called from a `ContextMenu` in a `SearchControl`. Also available for `EntityOperationSettings` operations. 
* **EntityOperationSettings:** To customize how a **`Execute` / `Delete` / `ConstructFrom`** operation behaves when called from a `ToolBarButton` in a `NormalPage`. The most frequently used, and also contains two `ContextualOperationSettings` properties to call this operations contextually in a `SearchControl`. 

## ConstructorOperationSettings

`ConstructorOperationSettings<T>` allows you to customize how the client execution of a `Construct` operation should be made. 

By default, the `Construct` operation is used automatically by the [Constructor](../Facades/Constructor.md), so will be invoked whenever the user presses the create button (+) on a [EntityControl](../EntityControls/EntityControls.md), or `SearchControl`.

In the rare case that more than one `Construct` operation is registered for a type, a [SelectorWindow](../SelectorWindow.md) will let the user choose which one. 

So, `ConstructorOperationSettings<T>` is then useful only in the rare case that more than one `Construct` operation exists for an entity and you want to override just one of them.

```C#
public class ConstructorOperationSettings<T> : OperationSettings /*Simplification*/
{
    public Func<ClientConstructorOperationContext<T>, bool> IsVisible { get; set; }
    public Func<ClientConstructorOperationContext<T>, JsFunction> ClientConstructor { get; set; }

    public Func<ConstructorOperationContext<T>, T> Constructor { get; set; }

    public ConstructorOperationSettings(ConstructSymbol<T>.Simple symbolContainer)
        : base(symbolContainer)
    {
    }
}
```
* **IsVisible:** Let you hide a particular constructor from the user interface, removing the option from the `SelectorWindow` or even removing the create (+) button when there are `Constructor` operations defined in the logic, but none of them are visible or authorized in the client. 
* **ClientConstructor:** Let you add a [`JsFunction`](../Facades/JsFunction.md) that will be executed client-side after the create (+) button is clicked in any `EntityBase` or `SearchControl` and before the client requests the server for the new view, giving you an opportunity to show dialog and add some extra parameters to `extraJsonArgs` (available from C# using `ClientConstructorManager.ExtraJsonParams`).

 
* **Constructor:** Let you control how the create operation will be executed in the server when a new entity is created, giving you the opportunity to read the extra arguments and pass them as a parameter to the constructor.   


With `ClientConstructorOperationContext` and `ConstructorOperationContext` defined as: 

```C#
public class ClientConstructorOperationContext<T> /*Simplification*/
	where T : class, IEntity
{
    public OperationInfo OperationInfo { get; private set; }
    public ClientConstructorContext ClientConstructorContext { get; private set; }
    public ConstructorOperationSettings<T> Settings { get; private set; }
}

public class ConstructorOperationContext<T> /*Simplification*/
	where T : class, IEntity
{
    public OperationInfo OperationInfo { get; private set; }
    public ConstructorContext ConstructorContext { get; private set; }
    public ConstructorOperationSettings<T> Settings { get; private set; }
}
```

With `ConstructorContext` and `ClientConstructorContext` defined in [Constructor](../Facades/Constructor.md). 

### Example:

> in Client class, the `ClientConstructor` calls the exported function `createOrder` defined in `Order.ts` with the necessary parameters.

```C#
public static JsModule OrderModule = new JsModule("Order");
...
OperationClient.AddSettings(new List<OperationSettings>()
{
    new ConstructorOperationSettings<OrderDN>(OrderOperation.Create)
    {
         ClientConstructor = ctx => OrderModule["createOrder"](ClientConstructorManager.ExtraJsonParams, 
             new FindOptions(typeof(CustomerDN)){ SearchOnLoad = true }.ToJS(ctx.ClientConstructorContext.Prefix, "cust")),
         
         Constructor = ...
    }
});
```

> in Order.ts, the user selects the optional `customer` that is added to `extraJsonArgs` and passed to the server.

```TypeScript
export function createOrder(extraJsonArgs: FormObject, findOptions: Finder.FindOptions): Promise<FormObject> {

    return Finder.find(findOptions).then(cust=> {

        if (cust)// could return null, but we let it continue 
            extraJsonArgs = $.extend(extraJsonArgs, { customer: cust.key() });

        return extraJsonArgs;
    });
}
```

> Bath in the Client class, the `Constructor` **in the same** `ConstructorOperationSettings` parses the `customer` as an optional lite using `TryParseLite` and passes it to `OrderOperation.Create` operation, returning the generated entity.  

```C#
OperationClient.AddSettings(new List<OperationSettings>()
{
    new ConstructorOperationSettings<OrderDN>(OrderOperation.Create)
    {
         ClientConstructor = ...,
         
         Constructor = ctx=>
         {
             var cust = ctx.ConstructorContext.Controller.TryParseLite<CustomerDN>("customer");
             return OperationLogic.Construct(OrderOperation.Create, cust);
         }
    }
});
```


## ContextualOperationSettings

`ContextualOperationSettings` is used to configure the appearance and behavior of `ConstructFromMany` operations being shown in a `ContextMenu` of, for example, the selected rows of a `SearchControl`. 

It has the following options. 

```C#
public class ContextualOperationSettings<T>  : OperationContext  /*Simplification*/ 
	where T : class, IEntity
{
    public ContextualOperationSettings(IConstructFromManySymbolContainer<T> symbolContainer)

    public double Order { get; set; }
    public Func<ContextualOperationContext<T>, bool> IsVisible { get; set; }    
    public Func<ContextualOperationContext<T>, string> ConfirmMessage { get; set; }
    public Func<ContextualOperationContext<T>, JsFunction> Click { get; set; }
    
}
```
* **Order:** Set this property to place the `MenuItem` before or after other items. The `MenuItems` are placed sorted by `Order` property, and then by the order they where registered in the server. The default value is 0.    
* **IsVisible:** Function that returns a `bool` indicating if the `MenuItem` should be visible. Since it's a function you can return a different value depending the entity state (but will require retrieving the entity!), the current user or any other complex condition.    
* **Click:** Let you have **full control** to completely override what the menu item should do when clicked using a [`JsFunction`](../Facades/JsFunction.md).
* **ConfirmMessage:** Shortcut to add a confirmation message without having to re-implement the `Click` method. By default is only provided for `Delete` operations. 

All of this events take a `ContextualOperationContext`: 


```C#
public class ContextualOperationContext<T>  /*Simplification*/
	where T : class, IEntity
{
    public List<Lite<T>> Entities { get; private set; }
    public Type SingleType { get; }

    public OperationInfo OperationInfo { get; }
    public ContextualOperationSettings<T> OperationSettings { get; }

    public SelectedItemsMenuContext Context { get; }
    public string Prefix { get;  }    
    public UrlHelper Url { get; }
    public object QueryName { get; }

    public string CanExecute { get; set; }
}
```

* **Entities:** The list of selected `Lite<T>` in the `SearchControl` that could receive the operation invocation.
* **SingleType** The `EntityType` of all the selected entities. If entities of different types are selected no `ConstructFromMany` operation will be candidate to avoid implementation ambiguities, but you can register the menu item maually if necessary.
* **OperationInfo:** Information over the operation, like the returned type or whether is lite or not.
* **OperationSettings:** The registered `ContextualOperationSettings` itself.   
* **Context:** The original `SelectedItemsMenuContext` of the `SearchControl`.    
* **Prefix Url and QueryName:** Shortcuts to access the same properties in `SelectedItemsMenuContext`. 
* **CanExecute:** The `string` that indicates a potential pre-condition error to execute the operation. By default this will disable the `MenuItem` and show the message as tool-tip, but you can hide it overriding `IsVisible`. Only available for `Execute` / `Delete` / `ConstructFrom` operations!. 

### Example: 

> in Client class the `ContextualOperationSettings` overrides `Click` calling `createOrderFromProducts` with the right parameters: 
* The `OperationOptions` accessible using `ctx.Options()`
* The `FindOptions` that requires `ToJS` to convert to `Finder.FindOptions`
* The `url` to the controller, just an `string` that can be generated using `ctx.Url.Action` with an strongly-typed lambda
* The `openNewWindowOrEvent` required to detect if the user clicked with the middle button (or ctrl pressed) to create the entity in a different window.

```C#
public static JsModule OrderModule = new JsModule("Order");
...
OperationClient.AddSettings(new List<OperationSettings>()
{
     new ContextualOperationSettings<ProductDN>(OrderOperation.CreateOrderFromProducts)
     {
          Click = ctx => OrderModule["createOrderFromProducts"](ctx.Options(), 
              new FindOptions(typeof(CustomerDN)){ SearchOnLoad = true }.ToJS(ctx.Prefix, "cust"), 
               ctx.Url.Action((HomeController c)=>c.CreateOrderFromProducts()), 
              JsFunction.Event)
     },
});
```

> in Orders.ts, the TypeScript function `createOrderFromProducts` calls `Finder.find`, and includes the selected customer (if any) in `customer` key of the `requestExtraJsonData` and calls `constructFromManyDefault` as defined in [Operations module](../Signum/Scripts/Operations.md). 

```TypeScript
export function createOrderFromProducts(options: Operations.OperationOptions, findOptions: Finder.FindOptions, url: string, openNewWindowOrEvent: any): Promise<void> {

    options.controllerUrl = url;

    return Finder.find(findOptions).then(cust=> {

        if (cust)// could return null, but we let it continue 
            options.requestExtraJsonData = { customer: cust.key() };

        return Operations.constructFromManyDefault(options, openNewWindowOrEvent);
    });
}
```

> finally the controller action `CreateOrderFromProducts` receives the request, parses `customer`, calls the `OrderOperation.CreateOrderFromProducts` and returns the result as a `NormalWindows` (or `NormalPopup`)  using `DefaultConstructResult`

```C#
public ActionResult CreateOrderFromProducts()
{
    Lite<CustomerDN> customer = this.TryParseLite<CustomerDN>("customer");

    var products = this.ParseLiteKeys<ProductDN>(); 

    var order = OperationLogic.ConstructFromMany(products, OrderOperation.CreateOrderFromProducts, customer);

    return this.DefaultConstructResult(order);
}
```


## EntityOperationSettings

`EntityOperationSettings` represents an operation that can be executed from an entity page, like `NormalPage` of `PopupControl`, using a `ToolBarButton`. In this sense, it unifies:

  * `ExecuteSymbol<in T>`: Typically shown as a `ToolBarButton` in the NormalWindow of `T`. 
  * `DeleteSymbol<in T>`: Typically **red** `ToolBarButton` in the NormalWindow of `T` that requires confirmation. 
  * `ConstructSymbol<T>.From<in F>`: Typically shown as a `MenuItem` grouped in the the *'Create...'* `ToolBarButton` from the entity `F` (not `T`!!).

`EntityOperationSettings` provides new options, that only make sense for operations in a `NormalPage` tool bar: 

```C#
public class EntityOperationSettings<T> : OperationSettings /*Simplification*/
	where T : class, IEntity
{
    public EntityOperationSettings(IEntityOperationSymbolContainer<T> symbolContainer)

    public Func<EntityOperationContext<T>, bool> IsVisible { get; set; }
    public Func<EntityOperationContext<T>, JsFunction> Click { get; set; }
    public Func<EntityOperationContext<T>, string> ConfirmMessage { get; set; }
}
```

* **IsVisible:** Function that returns a `bool` indicating if the button should be visible. Since is a function you can return a different value depending the entity state, the current user or any other complex condition.    
* **Click:** Let you have **full control** to completely override what the menu item should do when clicked using a [`JsFunction`](../Facades/JsFunction.md).
* **ConfirmMessage:** Shortcut to add a confirmation message without having to re-implement the `Click` method. By default is only provided for `Delete` operations. 

All this method receive a `EntityOperationContext<T>` with the following properties: 

```C#
public class EntityOperationContext<T> /*Simplification*/
	where T : class, IEntity
{
    public EntityButtonContext Context { get; }
    public UrlHelper Url { get; }
    public string PartialViewName { get; }
    public string Prefix { get; }
    public ViewMode ViewMode { get; }
    public bool ShowOperations { get; }

    public OperationInfo OperationInfo { get; }
    public EntityOperationSettings<T> OperationSettings { get;}

    public T Entity { get; }
    public string CanExecute { get; set; }
}
```

* **Context:** The `EntityButtonContex` as provided by the `ButtonBarEntityHelper`.
* **Url, PartialViewName, Prefix, ViewMode and ShowOperations:** Shortcut to get the similar properties in `EntityButtonContext`.       
* **OperationInfo:** Information over the operation, like the returned type or whether is lite or not.
* **OperationSettings:**  The registered `EntityOperationContext<T>` itself.    
* **Entity:** The `Entity` (of type `T`) in the `NormalWindow` that could receive the operation invocation.
* **CanExecute:** The `string` that indicats a potential pre-condition error to execute the operation. By default this will disable the button and show the message as tooltip, but you can hide it overriding `IsVisible`.  

### Examples:

> Example overriding `IsVisible` to avoid shoing `Save` and `SaveNew` at the same time: 

```C#
OperationClient.AddSettings(new List<OperationSettings>()
{
    new EntityOperationSettings(OrderOperation.SaveNew){ IsVisible = ctx=> ctx.Entity.IsNew }, 
    new EntityOperationSettings(OrderOperation.Save){ IsVisible = ctx=> !ctx.Entity.IsNew }, 
}); 
```

> Example overriding `ConfirmMessage`: 

```C#
OperationClient.AddSettings(new List<OperationSettings>()
{
    new EntityOperationSettings(OrderOperation.Cancel)
    { 
        ConfirmMessage = ctx=> ((OrderDN)ctx.Entity).State != OrderState.Shipped ? null :
           OrderMessage.CancelShippedOrder0.NiceToString(ctx.Entity) 
    }, 
}); 
```

> Example overriding `Click`:

```C#
OperationClient.AddSettings(new List<OperationSettings>()
{
    new EntityOperationSettings<OrderDN>(OrderOperation.Ship)
    { 
        Click = ctx => OrderModule["shipOrder"](ctx.Options(), 
            ctx.Url.Action((HomeController c)=>c.ShipOrder()), 
            GetValueLineOptions(ctx.Prefix), 
            false)
    }, 
}); 
```

> More explanations below.

Additionally, `EntityOperationSettings` contains more properties that can be usefull: 

```C#
public class EntityOperationSettings<T> : OperationSettings /*Simplification*/
	where T : class, IEntity
{
    public double Order { get; set; }

    public EntityOperationGroup Group { get; set; }
} 
```

* **EntityOperationGroup:** Operations can be grouped together in `ToolBarButtons` with a `ContextMenu` using `EntityOperationGroup`. By grouping similar operations together we can simplify the UI and hide operations that should not be executed frequently. Each grouped button can specify a `Text` that will be used as the content of the button, a background color and a lambda to simplify the name. 

> By default all the `ConstructFrom` operations are grouped in `EntityOperationGroup.Create` that has the name `"Create..."` and simplifies patterns like `"Create Order from Customer"` to just `"Order"`. 

* **Order:** Set this property to arrange buttons before or after other ones. The buttons are placed ordered by `Order` and then by the order they where registered in the server. The default value is 0, and for the groups is 100. 

Finally, `EntityOperationSettings` contains a pair of `ContextualOperationSettings` used to configure how the operation will be shown when used from the `SearchControl` in a `ContextMenu`. 

```C#
public class EntityOperationSettings<T> : OperationSettings /*Simplification*/
{
    public ContextualOperationSettings<T> ContextualFromMany { get; private set; }
    public ContextualOperationSettings<T> Contextual { get; private set; }
} 
```

* **ContextualFrom:** Configures how the operation will be shown in a `ContextMenu` when **just one element** is selected in the `SearchControl`. 

* **ContextualFromMany:** Configures how the operation will be shown in a `ContextMenu` when **more than one element** is selected in the `SearchControl`, typically only available if using the Process module. 

### Automatic `IsVisible` for `EntityOperationSettings.ContaxtualFrom/ContextualFromMany`

If `EntityOperationSettings.IsVisible` is overridden but `EntityOperationSettings.Contextual/ContextualFromMany.IsVisible` is not, then the `Contextual/ContextualFromMany` is automatically hided to avoid showing operations that should be hidden.

Even more, if `EntityOperationSettings.Click` is overridden but `EntityOperationSettings.ContextualFrom/Many.Click` it not, then the `Contextual/ContextualFromMany` is also hidden, to avoid showing operations that won't work because if not overridden. 

So, as a rule of thumb: If you override the `EntityOperationSettings` then the `Contextual/ContextualFromMany` will be disabled until you override them too.


### Example: 

> Example overriding `EntityOperationSettings.Click` and `EntityOperationSettings.Contextual.Click` but not `EntityOperationSettings.ContextualFromMany.Click`:

> in Client class the `EntityOperationSettings` overrides `Click` and `Contextual.Click` calling `shipOrder` with the right parameters: 
* The `OperationOptions` accessible using `ctx.Options()`
* The `url` to the controller, just an `string` that can be generated using `ctx.Url.Action` with an strongly-typed lambda
* The `valueLineOptions` required to open a `ValueLineBox` in TypeScript, but can be generated from C# and serialized (simplifying localized strings). In this case the code has been factored out to `GetValueLineOptions`. 
* `contextual` parameter indicating if the operation is being executed from a `NormalPage` or a `SearchControl`.

```C#
public static JsModule OrderModule = new JsModule("Order");
...
OperationClient.AddSettings(new List<OperationSettings>()
{
    new EntityOperationSettings<OrderDN>(OrderOperation.Ship)
    { 
        Click = ctx => OrderModule["shipOrder"](ctx.Options(), 
            ctx.Url.Action((HomeController c)=>c.ShipOrder()), 
            GetValueLineOptions(ctx.Prefix), 
            false),

        Contextual = 
        { 
            Click = ctx => OrderModule["shipOrder"](ctx.Options(), 
                ctx.Url.Action((HomeController c)=>c.ShipOrder()), 
                GetValueLineOptions(ctx.Prefix), 
                true),
        }
    }
}); 

private static ValueLineBoxOptions GetValueLineOptions(string prefix)
{
    return new ValueLineBoxOptions(ValueLineType.DateTime, prefix)
    {
        labelText = DescriptionManager.NiceName((OrderDN o) => o.ShippedDate),
        value = DateTime.Now
    };
}
```

> in Orders.ts the TypeScript function `shipOrder` calls `Navigator.valueLineBox`, and includes the selected shipDate (or cancels operation) in `shipDate` key of the `requestExtraJsonData`, then calls `executeDefault` or `executeDefaultContextual` as defined in [Operations module](../Signum/Scripts/Operations.md) depending the `contextual` parameter.

```TypeScript
export function shipOrder(options: Operations.EntityOperationOptions, url: string,
    valueLineOptions: Navigator.ValueLineBoxOptions, contextual: boolean) : Promise<void> {

    return Navigator.valueLineBox(valueLineOptions).then(shipDate =>
    {
        if (!shipDate)
            return null;

        options.requestExtraJsonData = { shipDate: shipDate };

        options.controllerUrl = url;

        if (contextual)
            return Operations.executeDefaultContextual(options);
        else
            return Operations.executeDefault(options);
    }); 
}
```

> finally in the controller action `ShipOrder` receives the request, parses `shipDate`, calls the `OrderOperation.Ship` and returns the result as a `NormalControl` (or `NormalPopup`)  using `DefaultExecuteResult`

```C#
public ActionResult ShipOrder()
{
    var order = this.ExtractEntity<OrderDN>();

    var shipDate = this.ParseValue<DateTime>("shipDate");

    order.Execute(OrderOperation.Ship, shipDate);

    return this.DefaultExecuteResult(order);
}
```



 


