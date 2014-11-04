# Finder class 

The main responsibilities of `Finder` is to open query result to search for a particular entity using a modal window (`Find` and `FindMany`) or just take a look at the entities and start actions from there in an independent window (`Explore`). 

By default `SearchWindow` is used, but the `FindManager` can be overridden to open any other kind of control or window.

## Find and FindMany
`Finder.Find` method shows a query result to the user and returns the selected entity. The behavior should block the thread in the meanwhile, and by default is implemented using `ShowDialog` to open a modal `SearchWindow`, but could be overridden to show a different window.

This method is used by default in the find (o-) buttons in `EntityLine` or `EntityList`, and can also be used to define a multi-step process that opens many dialog windows. 

There are three different overloads: 

```C#
public static Lite<T> Find<T>() where T : IdentifiableEntity
public static Lite<T> Find<T>(FindOptions options) where T : IdentifiableEntity
public static Lite<IdentifiableEntity> Find(FindOptions options)
```

All of this overloads will return `null` if the user cancels the operation.

### FindOptions
The optional `FindOptions` let us change some behavior.

Properties that `FindOptions` inherits from `QueryOptions`:

* **QueryName:** The query that will be used to search results. Mandatory, and `typeof(T)` will be used for the overloads that take no `FindOptions`. 
* **FilterOptions:** The initial filters that will be used, automatic conversion of `Lite<T>` will be made, and the filters can also be `Frozen` so they can not be changed or removed. 
* **OrderOptions:** The initial orders that will be used.  
* **ColumnOptions** and **ColumnMode:** The initial columns that will be added, removed or replaced, depending of `ColumnMode`.  
* **Pagination**: The initial pagination that will be used, from `All`, `Firsts(elementsPerPage)` or `Paginate(elementsPerPage, currentPage)`.  

Properties that `FindOptions` inherits from `FindOptionsBase`:

* **SearchOnLoad:** Controls if results should be shown automatically when the windows is opened the `SearchControl` is `Loaded`. Default `false`.
* **ShowFilters:** Control if the filter panel should be visible. Default `true`.
* **ShowFilterButton:** Control if the filter button should be visible. Default `true`.
* **ShowHeader:** Controls if the whole header should be visible, if `false` overrides the two previous property and also hides the search button and the extensible `MenuItems`. Default `true`.
* **ShowFooter:** Controls if the footer should be visible, if `false` hides all the pagination controls in the bottom. Default `true`.

And finally, properties directly from `FindOptions`: 

* **ReturnIfOne:** Executes the query internally to test if there's just one result, and in this case selects the only row as the result without even opening the `SearchWindow`. 

For `EntityList`, `EntityRepeater` or `EntityStrip`, selecting more than one element is possible. This functionality is represented by the method `FindMany` also with three variations: 

```C#
public static List<Lite<T>> FindMany<T>()
public static List<Lite<T>> FindMany<T>(FindManyOptions options)
public static List<Lite<IdentifiableEntity>> FindMany(FindManyOptions options)
```

The behavior is similar, returning a `null` list if the user cancels the `SearchWindow` dialog. 

The options are quite similar because `FindManyOptions` inherits from `FindOptionsBase`. Just `ReturnIfOne` is missing. 

## Explore
`Finder.Explore` method shows an entity to the user in an independent window. The behavior should not block the thread and by default is implemented using `Show` to open an independent `SearchWindow`, but could be overridden to open a new Tab in a tabbed application.

If `NavigationManage` is instantiated with `multithreaded=true` then the `SearchWindow` is opened using a different `Thread` and `Dispatcher`. This way any `ShowDialog` from this new search window won't block the rest of the application, but you have to take more care to [`Freeze`](http://msdn.microsoft.com/en-us/library/system.windows.freezable(v=vs.110).aspx) any resource that can be shared across different threads. 

There's just one overload:

```C#
public virtual void Explore(ExploreOptions options)
```

The method returns `void` because it opens an independent windows.

### ExploreOptions
The class `ExploreOptions` inherits all the options from `FindOptionsBase` but let us change some behavior: 

* **NavigateIfOne:** Executes the query internally to test if there's just one result, and in this case navigates to the only entity without even opening the `SearchWindow`.
* **Closed:** Event that will be fired when the windows is closed, could be used to refresh the original UI, but could be called in another thread.


## QuerySettings

`QuerySettings` let you configure how a query will be shown in the `SearchControl`, just as `EntitySettings` configures how an entity will be shown in the `NormalWindow`. It is non mandatory, and a default `QuerySettings` will be automatically created if none is provided. 

```C#
public class QuerySettings
{
    public object QueryName { get; }
    public QuerySettings(object queryName);

    public ImageSource Icon { get; set; }
    public Pagination Pagination { get; set; }
    public bool IsFindable { get; set; } 

    public Dictionary<string, Func<Binding, DataTemplate>> Formatters;  
}
```

* **QueryName:** The query to configure.
* **Icon:** Optional 16x16 icon that will be shown in the windows and the MenuItems. 
* **Pagination:** Override `FindOptions.DefaultPagination` for this particular query. 
* **IsFindable:** Hides this particular query so is not findable. For example example if only available in Web interface.
 
> `Finder` also provides the `IsFindable` method to test if a query is findable or not.  
 
* **Formatters:** Let you customize how to represent the cells of one particular column of this query by registering a `Func<Binding, DataTemplate>` in the column name. 


Usually, instead of registering a `DataTemplate` for one particular query column, makes more sense to register it for one particular database column, and is applied in any query that uses it. 


We can do this by using the static dictionary `PropertyFormatters`: 

```C#
public class QuerySettings
{
    public static Dictionary<PropertyRoute, Func<Binding, DataTemplate>> PropertyFormatters { get; set; }

    public static void RegisterPropertyFormat<T>(Expression<Func<T, object>> property, 
	    Func<Binding, DataTemplate> formatter)
        where T : IRootEntity
}
```

Example: 

```C#
QuerySettings.RegisterPropertyFormat((AlbumDN a) => a.BonusTrack.Duration, 
                b => Fluent.GetDataTemplate(() => new TextBlock().Bind(TextBlock.TextProperty, b)));
```

> Note: In order to create a `DataTemplate` grammatically, consider using `FrameworkElementFactoryGenerator`. 


Or, if you just want to register a template for one particular data type, or columns that contain certain attributes, you can use `FormatRules`

```C#
public class QuerySettings
{
    public static List<FormatterRule> FormatRules { get; set; }
}

public class FormatterRule
{
    public string Name { get; set; }

    public Func<Column, Func<Binding, DataTemplate>> Formatter { get; set; }
    public Func<Column, bool> IsApplicable { get; set; }

    public FormatterRule(string name, 
		Func<Column, bool> isApplicable, 
		Func<Column, Func<Binding, DataTemplate>> formatter)
}
```

For each column, the first `FormatRule` that returns `true` when evaluating `IsApplicable` is used. Actually, since the rules are ordered from more general to most specific, they are checked backwards using `Last` operator.

```C#
QuerySettings.Rules.Add(new FormatterRule("TimeSpan", 
	isApplicable: c=>c.Type.UnNullify() == typeof(TimeSpan), 
	formatter: c => b => Fluent.GetDataTemplate(() => new TextBlock().Bind(TextBlock.TextProperty, b))),
```

 