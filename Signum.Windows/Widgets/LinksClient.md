# LinkClient

`LinkClient` allows you to register links from any entity to navigate to other entities, open `SearchWindows` or do any other custom action. 

This links are currently accessible in two ways:

 * In the `LinkWidget`, available in the `WidgetPanel` of a `NormalWindow`.
 * As `MenuItems` in the `ContextMenu` of a `SearchControl`. 

Internally `LinkClient` uses a [`Polymorphic`](../../Signum.Utilities/Polymorphic.md) to register functions able to return `QuickLink` objects: 


```C#
public static class LinksClient
{
    public static Polymorphic<Func<Lite<Entity>, Control, QuickLink[]>> EntityLinks;

    public static void RegisterEntityLinks<T>(Func<Lite<T>, Control, QuickLink[]> getQuickLinks)
        where T : Entity
}
```

For example, this is how `OperationClient` registers `OperationLogDN` as a `QuickLink` for any entity but `OperationLogDN` itself: 

```C#
LinksClient.RegisterEntityLinks<Entity>((entity, control) => new[]
{ 
    entity.GetType() == typeof(OperationLogDN) ? null : 
        new QuickLinkExplore(new ExploreOptions(typeof(OperationLogDN), "Target", entity)
        {
            OrderOptions = { new OrderOption("Start") }
        }){ IsShy = true}
});
```

You can return a `null` array, or an array with `null` elements, and they will be safely ignored.

There's a small hierarchy of `QuickLinks`:


### QuickLink

```C#
public abstract class QuickLink 
{
    public string Label;
    public abstract string Name { get; }`

    public bool IsVisible { get; set; }
    public bool IsShy { get; set; }
    public string ToolTip { get; set; }
    public ImageSource Icon { get; set; }

    public abstract void Execute();
}
```

Abstract base class that contains: 

* **Label:** That will be shown in the elements of the `LinkWidget` or the `MenuItem`. 
* **Name:** Unique name used for `UIAutomation`. 
* **IsVisible:** Hides the `QuickLink`
* **IsShy:** If LinksWidgets finds some `QuickLinks` for a parcicular entitiy that are `IsShy == false`, it raises `ForceShow` event to make `WidgetPanel` visible. By default is `false`, but can be set to `true` for common QuickLinks, like `OperationLogDN`.  
* **ToolTip:** A ToolTip that will be shown when overing over the elements of the `LinkWidget` or the `MenuItem` in the contextual menu.
* **Icon:** A small 16x16 icon that will be shown in the `LinkWidget` or the `MenuItem`.

The `Execute` method is implemented by inheritors and will be invoked when the user cliks on the quick link. 


### QuickLinkAction

Inherits from `QuickLink` and gives you complete control of the `Name`, `Label` and `Action` that will be executed. It provides two convenient constructors: 

```C#
public class QuickLinkAction : QuickLink
{
    public QuickLinkAction(string name, string label, Action action)

    public QuickLinkAction(Enum enumValue, Action action)
            : this(enumValue.ToString(), enumValue.NiceToString(), action)
}
```

The `action` will be invoked when the user clicks the `QuickLink`. Example:

```C#
LinksClient.RegisterEntityLinks<DashboardDN>((cp, ctrl) => new[]
{  
    new QuickLinkAction(DashboardMessage.Preview, () => Navigate(cp, null)) 
    {
        IsVisible = DashboardPermission.ViewDashboard.IsAuthorized() 
    }
});
```

The `QuickLink` will be visible to any user by default, but you can hide it, for example, assigning to `IsVisible` property the evaluation of `PermissionSymbol.IsAuthorized` (in Authorization module). 



### QuickLinkExplore

Inherits from `QuickLink` and is specialized to open `SearchWindows` by calling `Finder.Explore`. 

```C#
public class QuickLinkExplore : QuickLink
{
   public ExploreOptions Options { get; set; }

   public QuickLinkExplore(ExploreOptions options);
   public QuickLinkExplore(object queryName, string columnName, object value); 
}
```

`QuickLinkExplore` can provide better defaults for the common case of using `Finder.Explore`: 

* **Name** will be the `QueryUtils.GetQueryUniqueKey(Options.QueryName)` of the query. 
* **Label** will be the `QueryUtils.GetNiceName(Options.QueryName)` of the query, but can be replaced. 
* **IsVisible** will be set to `Finder.IsFindable(Options.QueryName)`, but can be changed.  

Additionally, the property `ShowResultCount` let you query for the number of results ahead of time, showing them between parenthesis after the label `(2)`. Of couse this could impact the performance of your server because one query more has to be made that could not be needed.

Example: 

```C#
LinksClient.RegisterEntityLinks<Entity>((entity, control) => new[]
{ 
    entity.GetType() == typeof(OperationLogDN) ? null : 
        new QuickLinkExplore(new ExploreOptions(typeof(OperationLogDN), "Target", entity)
        {
            OrderOptions = { new OrderOption("Start") }
        }){ IsShy = true}
});
```

Finally, an overload of the constructor that takes a `Func<object>` as `value` can be used, that will only be evaluated when clicking (or if `ShowResultCount == true`), usually to access retrieve and access sub-properties of the `Lite<T>` entity.

```C#
public class QuickLinkExplore : QuickLink
{
    public QuickLinkExplore(object queryName, string columnName, Func<object> valueFactory)
}
```

The reason is that we avoid retrieving the entity just to show the quick links, for example when shown in a `SearchControl` `ContextMenu`. 


### QuickLinkNavigate<T>

Inherits from `QuickLink` and is specialized to open `NormalWindows` by calling `Finder.Navigate` after executing a `QueryUnique`. 

```C#
public class QuickLinkNavigate<T> : QuickLink  where T : Entity
{
   public NavigateOptions NavigateOptions { get; set; }
   public UniqueOptions FindUniqueOptions { get; set; }

   public QuickLinkNavigate(string columnName, object value, UniqueType unique = UniqueType.Single, object queryName = null);
   public QuickLinkNavigate(string columnName, Func<object> valueFactory, UniqueType unique = UniqueType.Single, object queryName = null); 
}
```

The idea is that when filtering the query `queryName` (of `typeof(T)` if not provided) by `columnName == value`, the `Entity` column will give-us the entity to navigae straight away.

`QuickLinkNavigate<T>` can provide better defaults for the common case of using `Navigator.Navigate`: 

* **Name** will be just `typeof(T).FullName`. 
* **Label** will be just `typeof(T).NiceName()` of the query, but can be replaced. 
* **IsVisible** will be set to `Finder.IsFindable(FindUniqueOptions.QueryName) && Navigator.IsNavigable(typeof(T), isSearchEntity: true)`, but can be changed.  



 
