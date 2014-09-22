# OperationClient

`OperationClient` is the class responsible of providing the services to `NormalWindow` and `SearchControl` to show the available [Operations](../../Signum.Engine/Operations/Operations.md) for an entity as `ToolBarButton` (in the first case) or `MenuItem` (for the second). 

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
    public ImageSource Icon { get; set; }
    public Color? Color { get; set; }

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
* **Icon:** Let you set an optional 16 x 16px icon that will be used in buttons, menu items, or constructor selectors. 
* **Color:** Let you set an optional WPF Color structure icon that will be used in buttons (on `IsMouseOver`) or menu items. 
* **OverridenType:** The `OperationSettings` are registered in `OperationClient` using a a [`Polymorphic<T>`](../../Signum.Utilities/Polymorphic.md) (just as in the server), allowing you to override the client implementation of a operation (i.e. `Eat`) for a particular type (i.e. `Lion`) even if the operation is defined for a more general type (i.e. `Animal`). 

There are three types operation settings: 

* **ConstructorOperationSettings:** To customize how a **`Construct`** operation behaves when clicking create button (+) in a [EntityControl](../EntityControls/EntityControls.md), or `SearchControl`. Rarely used.
* **ContextualOperationSettings:** To customize how a **`ConstructFromMany`** operation behaves when called from a `ContextMenu` in a `SearchControl`. Also available for `EntityOperationSettings` operations. 
* **EntityOperationSettings:** To customize how a **`Execute` / `Delete` / `ConstructFrom`** operation behaves when called from a `ToolBarButton` in a `NormalWindow`. The most frequently used, and also contains two `ContextualOperationSettings` properties to call this operations contextually in a `SearchControl`. 

## ConstructorOperationSettings

`ConstructorOperationSettings<T>` allows you to customize how the client execution of a `Construct` operation should be made. 

By default, the `Construct` operation is used automatically by the [Constructor](../Facades/Constructor.md), so will be invoked whenever the user presses the create button (+) on a [EntityControl](../EntityControls/EntityControls.md), or `SearchControl`.

In the rare case that more than one `Construct` operation is registered for a type, a [SelectorWindow](../SelectorWindow.md) will let the user choose which one. 

So, `ConstructorOperationSettings<T>` is then useful only in the rare case that more than one `Construct` operation exists for an entity and you want to override just one of them.

```C#
public class ConstructorOperationSettings<T> : OperationSettings /*Simplification*/
{
    public Func<ConstructorOperationContext<T>, bool> IsVisible { get; set; }
    public Func<ConstructorOperationContext<T>, T> Constructor { get; set; }

    public ConstructorOperationSettings(ConstructSymbol<T>.Simple symbolContainer)
        : base(symbolContainer)
    {
    }
}
```

* **Constructor:** Let you control how the operation will be executed client-side. By default just calls  `
`OperationServer.Construct`. 
* **IsVisible:** Let you hide a particular constructor from the user interface, removing the option from the `SelectorWindow` or even removing the create (+) button when there are `Constructor` operations defined in the server, but none of them are visible or authorized in the client. 

With `ConstructorOperationContext` defined as: 

```C#
public class ConstructorOperationContext<T> /*Simplification*/
	where T : class, IIdentifiable
{
    public OperationInfo OperationInfo { get; private set; }
    public ConstructorContext ConstructorContext { get; private set; }
    public ConstructorOperationSettings<T> Settings { get; private set; }
}
```

And `ConstructorContext` as defined in [Constructor](../Facades/Constructor.md). 

### Example:

> The constructor is overriden to open a `SearchWindow` before to asking for the optional `CustomerDN`

```C#
new ConstructorOperationSettings<OrderDN>(OrderOperation.Create)
{
    Constructor = ctx=>
    {
        var cust = Finder.Find<CustomerDN>(); // could return null, but we let it continue 

        return OperationServer.Construct(OrderOperation.Create, cust);
    },
}
```


## ContextualOperationSettings

`ContextualOperationSettings` is used to configure the appearance and behavior of `ConstructFromMany` operations being shown in a `ContextMenu` of, for example, the selected rows of a `SearchControl`. 

It has the following options. 

```C#
public class ContextualOperationSettings<T>  : OperationContext  /*Simplification*/ 
	where T : class, IIdentifiable
{
    public ContextualOperationSettings(IConstructFromManySymbolContainer<T> symbolContainer)

    public double Order { get; set; }
    public Func<ContextualOperationContext<T>, string> ConfirmMessage { get; set; }
    public Action<ContextualOperationContext<T>> Click { get; set; }
    public Func<ContextualOperationContext<T>, bool> IsVisible { get; set; }
}
```
* **Order:** Set this property to place the `MenuItem` before or after other items. The `MenuItems` are placed sorted by `Order` property, and then by the order they where registered in the server. The default value is 0.    
* **IsVisible:** Function that returns a `bool` indicating if the `MenuItem` should be visible. Since it's a function you can return a different value depending the entity state (but will require retrieving the entity!), the current user or any other complex condition.    
* **Click:** Let you have **full control** to completely override what the menu item should do when clicked. 
* **ConfirmMessage:** Shortcut to add a confirmation message without having to re-implement the `Click` method. By default is only provided for `Delete` operations. 

All of this events take a `ContextualOperationContext`: 

```C#
public class ContextualOperationContext<T>  /*Simplification*/
	where T : class, IIdentifiable
{
    public List<Lite<T>> Entities { get; private set; }
    public Type SingleType { get; }

    public OperationInfo OperationInfo { get; }
    public ContextualOperationSettings<T> OperationSettings { get; }

    public SearchControl SearchControl { get; }
    public string CanExecute { get; set; }
    public MenuItem SenderMenuItem { get; set; }

    public bool ConfirmMessage();
}
```

* **Entities:** The list of selected `Lite<T>` in the `SearchControl` that could receive the operation invocation.
* **SingleType** The `EntityType` of all the selected entities. If entities of different types are selected no `ConstructFromMany` operation will be candidate to avoid implementation ambiguities, but you can register the menu item maually if necessary.
* **OperationInfo:** Information over the operation, like the returned type or whether is lite or not.
* **OperationSettings:** The registered `ContextualOperationSettings` itself.   
* **SearchControl:** The `SearchControl` from witch the `ContextMenu` is opened.    
* **SenderMenuItem:** The menu item that was clicked.
* **CanExecute:** The `string` that indicats a potential pre-condition error to execute the operation. By default this will disable the `MenuItem` and show the message as tooltip, but you can hide it overriding `IsVisible`. Only available for `Execute` / `Delete` / `ConstructFrom` operations!. 
 

### Example: 

> The `ContextualOperationSettings.Click` is overridden to open a `SearchWindow` before asking for the optional `CustomerDN`

```C#
OperationClient.AddSettings(new List<OperationSettings>()
{
    new ContextualOperationSettings<ProductDN>(OrderOperation.CreateOrderFromProducts)
    {
         Click = ctx =>
         {
             var cust = Finder.Find<CustomerDN>(); // could return null, but we let it continue 

             var result = OperationServer.ConstructFromMany(ctx.Entities, OrderOperation.CreateOrderFromProducts, cust);

             Navigator.Navigate(result);
         },
    },
});
```

## EntityOperationSettings

`EntityOperationSettings` represents an operation that can be executed from an entity windows like `NormalWindow` using a `ToolBarButton`. In this sense, it unifies:

  * `ExecuteSymbol<in T>`: Typically shown as a `ToolBarButton` in the NormalWindow of `T`. 
  * `DeleteSymbol<in T>`: Typically **red** `ToolBarButton` in the NormalWindow of `T` that requires confirmation. 
  * `ConstructSymbol<T>.From<in F>`: Typically shown as a `MenuItem` grouped in the the *'Create...'* `ToolBarButton` from the entity `F` (not `T`!!).

`EntityOperationSettings` provides new options, that only make sense for operations in a `NormalWindow` tool bar: 

```C#
public class EntityOperationSettings<T> : OperationSettings /*Simplification*/
	where T : class, IIdentifiable
{
    public EntityOperationSettings(IEntityOperationSymbolContainer<T> symbolContainer)

    public Func<EntityOperationContext<T>, bool> IsVisible { get; set; }
    public Func<EntityOperationContext<T>, IdentifiableEntity> Click { get; set; }
    public Func<EntityOperationContext<T>, string> ConfirmMessage { get; set; }
}
```

* **IsVisible:** Function that returns a `bool` indicating if the button should be visible. Since is a function you can return a different value depending the entity state, the current user or any other complex condition.    
* **Click:** Let you have **full control** to completely override what the button should do when clicked. The returned `IdentifiableEntity` will be the new version of the entity in the screen, or `null` if didn't change (not the newly constructed entity!).  
* **ConfirmMessage:** Shortcut to add a confirmation message without having to re-implement the `Click` method. By default is only provided for `Delete` operations. 

All this method receive a `EntityOperationContext<T>` with the following properties: 

```C#
public class EntityOperationContext<T>
	where T : class, IIdentifiable
{
    public FrameworkElement EntityControl;
    public Control SenderButton;
    public OperationInfo OperationInfo;
    public ViewMode ViewButtons;
    public bool ShowOperations;
    public string CanExecute;

    public T Entity;
    public EntityOperationSettings<T> OperationSettings;

    public bool ConfirmMessage();
}
```

* **EntityControl:** The custom control that is contained by (typically) a `NormalWindow`.    
* **SenderButton:** The button that was clicked, typically a `ToolBarButton`.
* **OperationInfo:** Information over the operation, like the returned type or whether is lite or not.
* **ViewMode:** Indicates if the entity is being shown using `View` (modal) or `Navigate` (independent).
* **ShowOperations:** Indicates if the navigation has been requested with or without operations. You can still override `IsVisible` and show a particular operation anyway.
* **CanExecute:** The `string` that indicats a potential pre-condition error to execute the operation. By default this will disable the button and show the message as tooltip, but you can hide it overriding `IsVisible`.  
* **Entity:** The `IdentifiableEntity` (of type `T`) in the `NormalWindow` that could receive the operation invocation.
* **OperationSettings:**  The registered `EntityOperationContext<T>` itself.    

Example overriding `IsVisible` to avoid shoing `Save` and `SaveNew` at the same time: 

```C#
OperationClient.AddSettings(new List<OperationSettings>()
{
    new EntityOperationSettings(OrderOperation.SaveNew){ IsVisible = ctx=> ctx.Entity.IsNew }, 
    new EntityOperationSettings(OrderOperation.Save){ IsVisible = ctx=> !ctx.Entity.IsNew }, 
}); 
```

Example overriding `ConfirmMessage`: 

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

In order to implement `Click`, some methods could be handy: 

* `EntityOperationContext.ConfirmMessage`: To show a `MessageBox` for any potentially irreversible operation. 
* `EntityExtensions.LooseChangesIfAny`: To ask the user to loose changes before invoking a `Lite = true` operation.
* The methods in [`OperationServer`](OperationServer.md).
* The methods in [`Navigator`](../Facades/Navigator.md).
* [`ValueLineBox`](ValueLineBox.md).
* [`SelectorWindow`](SelectorWindow.md).


```C#
OperationClient.AddSettings(new List<OperationSettings>()
{
    new EntityOperationSettings<OrderDN>(OrderOperation.Ship)
    { 
        Click = ctx=>
        {
			if (!ctx.EntityControl.LooseChangesIfAny())
                return null;

            DateTime shipDate = DateTime.Now;
            if (!ValueLineBox.Show(ref shipDate, labelText: DescriptionManager.NiceName((OrderDN o) => o.ShippedDate)))
                return null;

            return ctx.Entity.Execute(OrderOperation.Ship, shipDate); 
        }
    }, 
}); 
```

Additionally, `EntityOperationSettings` contains more properties that can be usefull: 

```C#
public class EntityOperationSettings : OperationSettings
{
    public bool AvoidMoveToSearchControl { get; set; }
    public double Order { get; set; }

    public EntityOperationGroup Group { get; set; }
} 
```

* **AvoidMoveToSearchControl:** By default `ConstructSymbol<T>.From<in F>` operations have the feature of looking for a `SearchControl` in the custom control of the current entity which `EntityType` is `T`  and, if found, move the button to the search control and remove it from the tool bar. This property let you avoid this behavior. 

 > i.e: Move `ConstructOrderFromCustomer` to the `SearchControl` of orders in the `Customer` control
  
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

> Example overriding `EntityOperationSettings.Click` and `EntityOperationSettings.Contextual.Click` (but not `EntityOperationSettings.ContextualFromMany.Click`) to show a `ValueLineBox` asking for the `shipDate`:

```C#
OperationClient.AddSettings(new List<OperationSettings>()
{
    new EntityOperationSettings<OrderDN>(OrderOperation.Ship)
    { 
        Click = ctx=>
        {
			if (!ctx.EntityControl.LooseChangesIfAny())
                return null;

            DateTime shipDate = DateTime.Now;
            if (!ValueLineBox.Show(ref shipDate, labelText: DescriptionManager.NiceName((OrderDN o) => o.ShippedDate)))
                return null;

            return ctx.Entity.Execute(OrderOperation.Ship, shipDate); 
        }


        Contextual = 
        { 
            Click = ctx =>
            {
                DateTime shipDate = DateTime.Now;
                if (!ValueLineBox.Show(ref shipDate, labelText: DescriptionManager.NiceName((OrderDN o) => o.ShippedDate)))
                    return;

                ctx.Entities.SingleEx().ExecuteLite(OrderOperation.Ship, shipDate); 
            }
        }
    }, 
}); 
```




 


