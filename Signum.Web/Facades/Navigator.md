# Navigator class

The main responsibilities of `Navigator` class in the server-side is to return the `ActionResults` that are necessary to open a `NormalPage`, `NormalControl` or `PopupControl` after creating or retrieving the entities from the database.

By default the custom views provided by the framework are used, but theoretically they could be overridden to open any other custom  `NormalPage` or `NormalControl`.

In order to actually open a entity pop-up using `navigatePopup` or `viewPopup` in the client side, take a look at `Navigator` in the [Typescript API](...\Signum\Scripts\Navigator.md).


## NormalPage and NormalControl

`Navigator` class is responsible for creating the `ActionResults` to:
* Return a `NormalPage`: usually as a result of a direct navigation, like going to `View/Invoice/3`. Includes all the `NormalControl` plus whatever is defined in `_Layout.cshtml`.
* Return a `NormalControl`: usually the result of reloading a `NormalPage` after executing an operation. Includes the custom control for the entity plus buttons, validation summary and widgets. 

```C#
public static ViewResult NormalPage(this ControllerBase controller, IRootEntity entity, NavigateOptions options = null)

public static PartialViewResult NormalControl(this ControllerBase controller, IRootEntity entity, NavigateOptions options = null)
```

This methods are already used by `NavigatorController`, but are handy in your custom controllers if you also need to return this views.

Both optionally take a `NavigateOptions` with the following properties: 

```C#
public class NavigateOptions
{
    public NavigateOptions(IRootEntity entity){ ... }

    public string PartialViewName { get; set; }

    public bool? ReadOnly { get; set; }

    public bool ShowOperations { get; set; }
}
```
* **PartialViewName**: Overrides the default configuration of the entity to show a different view that the one registered in `EntitySettings`, if any. 
* **ReadOnly**: Overrides the default configuration of the entity to let/forbid the user make changes in all the fields of the entity. 
* **ShowOperations**: Let you hide the operation buttons. Default is `true`.

## PopupView and PopupNavigate

`Navigator` class is also responsible to return the `PartialViewResult` necessary to open pop-ups. Just as in Signum.Windows, there are two modes to open a pop-up:

* `PopupView` represents a modal navigation that returns a modified entity at the end it the [OK] button is pressed, like when pressing the View (->) button in an `EntityBase`. 
* `PopupNavigate` represents opening a popup independently that does not return an entity, like viewing a Note attached to an entity, and does not have a [OK] button. In Signum.Web `PopupNavigate` is rare, and just a direct navigation to the entity (`NavigatePage`) in a new tab is more popular.    

```C#
public static PartialViewResult PopupView(this ControllerBase controller, ModifiableEntity entity, PopupViewOptions options)

public static PartialViewResult PopupNavigate(this ControllerBase controller, IRootEntity entity, PopupNavigateOptions options)
```

Both `PopupViewOptions` and `PopupNavigateOptions` inherit from `PopupOptionsBase` the following properties: 

```C#
public abstract class PopupOptionsBase
{
    public PopupOptionsBase(TypeContext tc) { ... }
 
    public string Prefix { get; set; }

    public bool? ReadOnly { get; set; }

    public string PartialViewName { get; set; }

    public bool ShowOperations { get; set; }
}
```
* **Prefix**: Sets the initial prefix of all the control name and ids that will be rendered to avoid conflicts with other controls already in the page. Mandatory.
* **PartialViewName**: Overrides the default configuration of the entity to show a different view that the one registered in `EntitySettings`, if any. 
* **ReadOnly**: Overrides the default configuration of the entity to let/forbid the user make changes in all the fields of the po. 
* **ShowOperations**: Let you hide the operation buttons. Default is `true`.

And `PopupViewOptions` adds some properties of his own: 

* **PropertyRoute**: Required to call `PopupView` with `EmbeddedEntity`, but most of the time is not necessary because the embedded entities are stored as a template in the `EntityControl`. 
* **SaveProtected:** Overrides the default `SaveProtected` of the entity, controlling if the `View` call can end with a dirty entity, of the user should save the entity before confirming. `null` by default.




## EntitySettings

`EntitySettings<T>` class, and `EmbeddedEntitySettings<T>` class are used to register the default `PartialViewName` of an entity.

* **PartialViewName**: A `Func<T, string>` that will be used as the custom control for an entity. Different controls could be used depending on the entity if necessary, but usually use the same control and modify visibility of some elements is enough. If `PartialViewName` is not set, the entities won't be 'IsViewable`. 

Additionally we can override any default behavior that is not properly deduced from the [EntityKind](../../Signum.Entities/EntityKindAttribute.md) of the entity for rare cases, but as a general rule choosing the right `EntityKind` will make it unnecessary. 

* **IsCreable**: Indicates if the entity can be created from search-like control (like `SearchControl`) and/or a line-like control (like an `EntityLine`). For example:
	* Doesn't make sense create a `CountryDN` on the fly from an address using a line, so it will be `EntityWhen.IsSearch`.
	* Oppositely doesn't make sense to go to `AddressDN` to create the `ShippingAddress` of an `OrderDN`, so it will be `EntityWhen.IsLine`.
* **IsViewable**: Indicates if the entity can be viewed in a modal window from a line-like control.
* **IsNavigable**: Indicates if the entity can be navigable from a search-like control and/or a line-like control. `EntityBase` also turns off `Navigate` if `View=true`.   
* **IsReadOnly**: Indicates if any `View` or `Navigate` should set the custom control as read-only. 


|               |IsCreable    |	IsViewable	|IsNavigable| IsReadOnly  
|--------------:|:-----------:|:-----------:|:---------:|:-----------:
| SystemString	| 	          |      	    |	        | ✓           
| System	    | 	          |✓            |Always	    | ✓           
| Relational	| 	          |             |     	    | ✓           
| String	    | IsSearch    |	            |IsSearch	|             
| Shared	    | Always	  |✓	        |Always	    |             
| Main	        | IsSearch    |✓	        |Always	    |             
| Part	        | IsLine	  |✓	        |    	    |             
| SharedPart	| IsLine	  |✓	        |Always	    |             


### Mappings

[Mapping](../Mapping/Mapping.md) are responsible for applying changes received as the `FORM` of a `HTTP POST` into a new or retrieved entity. They are recursive so they can modify sub-entities and entities in collections, but `EntityKind` is again used to avoid modify master data. 
 
* **MappingMain**: Indicates the `Mapping` that will be used when `ApplyChanges` is called over an entity of type `T` directly, as root.  
* **MappingLine**: Indicates the `Mapping` that will be used when `ApplyChanges` is called over any entity that has a property of type `T` or `Lite<T>`, usually using any kind or [EntityControl](../EntityControl/EntityControl.md) .

|               |MappingMain| MappingLine  
|--------------:|:---------:|:-----------:
| SystemString	|Simple     | Simple
| System	    |Simple	    | Simple
| Relational	|Simple  	| Simple
| String	    |Full	    | Simple 
| Shared	    |Full	    | Full
| Main	        |Full  	    | Simple 
| Part	        |Full       | Full 
| SharedPart	|Full 	    | Full 

  
> When modifying an `AddressDN` you can change the `Country` property (of type `CountryDN` with  `EntityKind` `String`) by any other instance in the database (*Simple MappingLine*), but doesn't make sense that you change the properties of the `CountryDN` itself (*Full MappingLine*). In order to do that, you need to edit the `CountryDN` directly (*Full MappingMain*).
> 
> The same is not true for the `AddressDN` entity, that can be modified from his parent entity without worries (using `EntityKing` `Part` or `SharedPart`).    

In order to register `EntitySettings` in the `Navigator`, `AddSetting` or `AddSettings` should be used. 

```C#
public static void AddSetting(EntitySettings setting)
public static void AddSettings(List<EntitySettings> settings)
````

Example: 

```C#
public static string ViewPrefix = "~/Views/MyApp/{0}.cshtml";
//...

Navigator.AddSettings(new List<EntitySettings>
{
    new EntitySettings<EmployeeDN>() { PartialViewName = e => ViewPrefix.Formato("Employee") },
    ...
});
```

> Use absolute paths to find the `PartialViewName` (with the help of `Formato`) since will be used from different controllers. 

You can also retrieve a `EntitySettings<T>` using the methods with the same name.

```C#
public static EntitySettings<T> EntitySettings<T>() where T : Entity
public static EmbeddedEntitySettings<T> EmbeddedEntitySettings<T>() where T : EmbeddedEntity
public static EntitySettings EntitySettings(Type type)
````

This methods will throw an exception if the `EntitySettings` is not previously registered. 

## ViewOverrides

You can use this methods, in combination with `CreateViewOverride` method, to customize some controls that are not under your control: 

```C#
Navigator.EntitySettings<UserDN>().CreateViewOverrides()
     .AfterLine((UserDN u) => u.Role, (html, tc) => html.ValueLine(tc, u => u.Mixin<UserEmployeeMixin>().AllowLogin))
     .AfterLine((UserDN u) => u.Role, (html, tc) => html.EntityLine(tc, u => u.Mixin<UserEmployeeMixin>().Employee));
```

In Signum.Windows the whole control is exposed as a tree that can be arbitrary manipulated. Such tree does not exists in Signum.Web since Razor returns the HTML as plain text. Instead you have to take advantage of the strategic method defined in `ViewOverride<T>`: 

```C#
public class ViewOverrides<T> : IViewOverrides where T : IRootEntity
{
    public ViewOverrides<T> BeforeTab(string id, Func<HtmlHelper, TypeContext<T>, Tab> constructor) 

    public ViewOverrides<T> AfterTab(string id, Func<HtmlHelper, TypeContext, Tab> constructor)

    public ViewOverrides<T> HideTab(string id)

    public ViewOverrides<T> BeforeLine<S>(Expression<Func<T, S>> propertyRoute, Func<HtmlHelper, TypeContext<T>, MvcHtmlString> constructor)
    public ViewOverrides<T> BeforeLine(PropertyRoute propertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString> constructor)

    public ViewOverrides<T> AfterLine< S>(Expression<Func<T, S>> propertyRoute, Func<HtmlHelper, TypeContext<T>, MvcHtmlString> constructor)
    public ViewOverrides<T> AfterLine(PropertyRoute propertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString> constructor)

    public ViewOverrides<T> HideLine<S>(Expression<Func<T, S>> propertyRoute)
    public ViewOverrides<T> HideLine(PropertyRoute propertyRoute)
}    
```

* **BeforeTab**: Adds a Tab before another tab (if a `Tab` `id` is provided) or at the begining of the TabContainer (if the `TabContainer` `id` is provided). 
* **AfterTab**: Adds a Tab after another tab (if a `Tab` `id` is provided) or at the end of the TabContainer (if the `TabContainer` `id` is provided). 
* **HideTab**: Hides a Tab  (if a `Tab` `id` is provided) or the whole  TabContainer (if the `TabContainer` `id` is provided).
* **BeforeLine**: Adds an arbitrary `MvcHtmlString` before another line, identifying it from his `PropertyRoute` or a more convinient `Expression`.
* **AfterLine**: Adds an arbitrary `MvcHtmlString` after another line, identifying it from his `PropertyRoute` or a more convinient `Expression`. 
* **HideLine**: Hides any line, identifying it from his `PropertyRoute` or a more convinient `Expression`. 

### Check features

Finally, `Navigator` exposes a set of methods to determine if an entity (or a particular instance) can be Navigated, Viewed, Created or should be ReadOnly. 

```C#
public static bool IsCreable(Type type, bool isSearch = false)

public static bool IsReadOnly(Type type)
public static bool IsReadOnly(ModifiableEntity entity)

public static bool IsViewable(Type type, string partialViewName)
public static bool IsViewable(ModifiableEntity entity, string partialViewName)

public static bool IsNavigable(Type type, string partialViewName, bool isSearch = false)
public static bool IsNavigable(IEntity entity, string partialViewName, bool isSearch = false)
``` 

In order to answer this questions, `Navigator` takes into account the configuration in the `EntitySettings` (usualy inherited from the `EntityKind`) as well as the events defined in `NavigationManager`: 

```C#
public class NavigationManager
{
    public event Func<Type, bool> IsCreable;
    public event Func<Type, ModifiableEntity, bool> IsReadOnly;
    public event Func<Type, ModifiableEntity, bool> IsViewable; //Used for IsViewable and IsNavigable
}
```

This events can be used to consistently disable parts of the user interface by third party modules, like Authorization (depending of the role) or Disconnected (depending if working connected or disconnected). 

## RegisterArea

Signum Framework is all about code-reuse. 

In Signum.Windows it's trivial to encapsulate `UserControl`, Images and Resources in reusable assemblies, since the main windows application is also an assembly anyway. 

The same is not true for Signum.Web, where your main web application is a special assembly where some resources are keep untouched (`css`, `js`, `ts`, `cshtml`, images...) while code is compiled to the bin folder as a normal class library. 

In order to store the web layer of a vertical module in a reusable assembly some things are necessary: 

* **EmbeddedFilesRepository:** Store static assets (`css`, `js`, `ts`, images...) as resources in the assembly, and create a controller that is able to return them.
* **CompiledViews:** Compile the `cshtml` files eagerly, at compile time, in a cs class, and teach MVC infrastructure how to find and use this classes as views as well using a `CompiledRazorBuildProvider`. 
* **SignumControllerFactory:**  Teach MVC how to reach the controllers in other assemblies. 

All this steps are simplified in `Navigator.RegisterArea` methods: 

```C#
public static class Navigator
{
    public static void RegisterArea(Type clientType)
    public static void RegisterArea(Type clientType, string areaName)
}
```

The method does:

* Creates an `EmbeddedFilesRepository` to store all the statics assets that have been saved as resources in the assembly of `clientType` and which names start by `[assemblyName].[areaName]` into the virtual directory `~/[areaName]/`, if any. 
* Registers all the compiled views in the assembly of `clientType` with `PageVirtualPathAttribute` starting with `~/[areaName]/` as available for `CompiledViews`.
* Registers all the controllers in the assembly of `clientType` with a namespace that starts as the namespace of `clientType`.  

If the first overload is used, `areaName` will be the name of `clientType` removing `'Client'` suffix. 