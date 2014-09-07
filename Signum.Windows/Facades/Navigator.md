# Navigator class

The main responsibilities of `Navigator` is showing entities in a modal window (`View`) or an independent window (`Navigate`) using the custom control registered using `EntitySettings<T>`. 

By default the entities are shown using a `NormalWindow`, that provides a common frame for any entity with a title bar, a button bar, validation summary and widgets panel, but the `NavigationManager` can be overriden to open any other kind of   

## View

`Navigator.View` method shows an entity to the user and returns the entity with potential changes. The behavior should block the thread in the meanwhile, and by default is implemented using `ShowDialog` to open a modal `NormalWindow`, but could be overridden to show always a different window.

This method is used by default in the view (->) and create (+) buttons in `EntityLine` or `EntityList`, and can also be used to define a multi-step process that opens many dialog windows. 

There are three different overloads: 

```C#
//Takes and returns a ModifiableEntity
public static T View<T>(T entity, ViewOptions options = null) where T : ModifiableEntity
 
//Takes and returns a Lite<T> (thin or fat)
public static Lite<T> View<T>(Lite<T> entity, ViewOptions options = null) where T: class, IIdentifiable

//A ModifiableEntity or a Lite<T> should be passed, the same type will be returned 
public static object ViewUntyped(object entity, ViewOptions options = null)
```

All of this overloads will return `null` if the user cancels the operation.

The optional `ViewOptions` let us change some behavior: 

* **Clone:** When `True` shows and modifies a clone instead of the original entity to allow canceling cleanly, but this could be problematic if cycles are involved. `True` by default.
* **ReadOnly:** Overrides `EntitySettings.IsReadOny` to show the custom control with all the controls in non-editable mode. `null` by default.
* **ShowOperations:** Controls `ButtonBarEventArgs.ShowOperations` that ultimately shows or hides operations. `True` by default.
* **View:** Overrides the default `EntitySettings.View` to show a different custom control. Can also be used to initialize some properties the same default control. `null` by default.
* **PropertyRoute:** Necessary to view an `EmbeddedEntity` to properly configure authorization rules and `Implementation`.
* **SaveProtected:** Overrides the default `SaveProtected` of the entity, controlling if the `View` call can end with a dirty entity, of the user should save the entity before confirming. `null` by default.
* **AllowErrors:** Controls if the `View` call can end with an entity with validation errors. Has three different values: 
	* Ask: If the entity has errors, the user will have to confirm a `MessageBox`. Default behavior. 
	* Yes: The entity could have validation errors, no `MessageBox` will be shown.
	* No: If the entity has errors, a `MessageBox` will be shown to fix them, giving no other option.

Example: 


```C#
var model = Navigator.View(new OrderModificationModel
{
    Reason = "Specify the reason to change the order #" + order.OrderNumber
}, new ViewOptions {
  AllowErrors = false, 
  Clone = false, 
});

if(model == null)
   return;

order.Execute(OrderOperation.Modify, model);
```

## Navigate
`Navigator.Navigate` method shows an entity to the user in an independent window. The behavior should not block the thread and by default is implemented using `Show` to open an independent `NormalWindow`, but could be overridden to open a new `Tab` in a tabbed application. 

If `NavigationManager` is instantiated with `multithreaded = true` then the `NormalWindow` is opened using a different `Thread` and `Dispatcher`. This way any `ShowDialog` from this new entity won't block the rest of the application, but you have to take more care of [`Freeze`](http://msdn.microsoft.com/en-us/library/system.windows.freezable(v=vs.110).aspx) any resource that can be shared across different threads. 


There are also three different overloads: 

```C#
//Takes a IIdentifiable, you can not navigate to an EmbeddedEntity or ModelEntity!
public static void Navigate<T>(T entity, NavigateOptions options = null) where T : IIdentifiable
 
//Takes a Lite<T> (thin or fat)
public static void Navigate<T>(Lite<T> entity, NavigateOptions options = null) where T : class, IIdentifiable

//A ModifiableEntity or a Lite<T> should be passed
public static void Navigate<T>(T entity, NavigateOptions options = null) where T : IIdentifiable
```

All of this methods return `void` because they open independent windows.

The optional `NavigateOptions` let us change some behavior: 

* **Clone:** When `True` shows a clone instead of the original entity to avoid changes by reference, but this could be problematic if cycles are involved. `False` by default.
* **ReadOnly:** Overrides `EntitySettings.IsReadOny` to show the custom control with all the controls in non-editable mode. `null` by default.
* **ShowOperations:** Controls `ButtonBarEventArgs.ShowOperations` that ultimately shows or hides operations. `True` by default.
* **View:** Overrides the default `EntitySettings.View` to show a different custom control. Can also be used to initialize some properties the same default control. Is a `Func<Control>` instead of a `Control` because could need to be created in another thread. `null` by default.
* **Closed:** Event that will be fired when the windows is closed, could be used to refresh the original UI, but could be called in another thread.


## EntitySettings

`EntitySettings<T>` class, and `EmbeddedEntitySettings<T>` class are used to register the default `View` of an entity.

* **View**: A `Func<T, Control>` that will be used as the custom control for an entity. Different controls could be used depending on the entity if necessary, but usually use the same control and modify visibility of some elements is enough. If `View` is not set, the entities won't be `Viewable`. 

Additionally we can override any default behavior that is not properly deduced from the [EntityKind](../../Signum.Entities/EntityKindAttribute.md) of the entity, for strange cases, but as a general rule choosing the right `EntityKind` will make it unnecessary. 

* **IsCreable**: Indicates if the entity can be created from search-like control (like `SearchControl`) and/or a line-like control (like an `EntityLine`). For example:
	* Doesn't make sense create a `CountryDN` on the fly from an address using a line, so it will be `EntityWhen.IsSearch`.
	* Oppositely doesn't make sense to go to `AddressDN` to create the `ShippingAddress` of an `OrderDN`, so it will be `EntityWhen.IsLine`.
* **IsViewable**: Indicates if the entity can be viewed in a modal window from a line-like control.
* **IsNavigable**: Indicates if the entity can be navigable from a search-like control and/or a line-like control. `EntityBase` also turns off `Navigate` if `View=true`.   
* **IsReadOnly**: Indicates if any `View` or `Navigate` should set the custom control as read-only. 


In order to register `EntitySettings` in the `Navigator`, `AddSetting` or `AddSettings`  should be used. 

```C#
public static void AddSetting(EntitySettings setting)
public static void AddSettings(List<EntitySettings> settings)
````

Example: 

```C#
Navigator.AddSettings(new List<EntitySettings>
{
    new EntitySettings<EmployeeDN>() { View = e => new Employee()},
    ...
});
```

You can also retrieve a `EntitySettings<T>` using the methods with the same name.

```C#
public static EntitySettings<T> EntitySettings<T>() where T : IdentifiableEntity
public static EmbeddedEntitySettings<T> EmbeddedEntitySettings<T>() where T : EmbeddedEntity
public static EntitySettings EntitySettings(Type type)
````

This methods will throw an exception if the `EntitySettings` is not previously registered. 

You can use this methods, in combination with `OverrideView` event, to customize some controls that are not under your control: 

```C#
Navigator.EntitySettings<UserDN>().OverrideView += (usr, ctrl) =>
{
    ctrl.Child<EntityLine>("Role").After(new ValueLine().Set(Common.RouteProperty, "[UserEmployeeMixin].AllowLogin"));
    ctrl.Child<EntityLine>("Role").After(new EntityLine().Set(Common.RouteProperty, "[UserEmployeeMixin].Employee"));
    
    return ctrl;
});
```
