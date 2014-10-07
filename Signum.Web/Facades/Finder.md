# Finder class

The main responsibilities of `Finder` class in the server-side is to return the `ActionResults` that are necessary to open a `SearchPage`, `SearchPopup` or the `SearchResults` after making the database query. 

By default the custom views provided by the framework are used, but theoretically they could be overridden to open any other custom  `SearchControl`.

In order to actually open a SearchPopup using `Find` or `Explore` in the client side, take a look at `Finder` in the [Typescript API](...\Signum\Scripts\Finder.md).

## SearchPage, SearchPopup, and SearchResults

Finder class is responsible for creating the `ActionResults` to return a `SearchPage` or `SearchPopup`, or fill the `SearchResults`.

```C#
public static ViewResult SearchPage(this ControllerBase controller, FindOptions findOptions)

public static PartialViewResult SearchPopup(this ControllerBase controller, 
	FindOptions findOptions, FindMode mode, Context context)
public static PartialViewResult SearchPopup(this ControllerBase controller, 
	FindOptions findOptions, FindMode mode, string prefix)

public static PartialViewResult SearchResults(ControllerBase controller, 
	QueryRequest request, bool allowSelection, bool navigate, bool showFooter, string prefix)
```

This methods are already used by `FinderController`, but could be handy in your custom controllers in certain scenarios. 

### FindOptions

`FindOptions` represents all the configuration settings of a `SearchControl` and is used to configure `SearchControl` or navigate to a `SearchPage`. List of properties: 

* **QueryName:** The query that will be used to search results. Mandatory, and `typeof(T)` will be used for the overloads that take no `FindOptions`. 
* **FilterOptions:** The initial filters that will be used, automatic conversion of `Lite<T>` will be made, and the filters can also be `Frozen` so they can not be changed or removed. 
* **OrderOptions:** The initial orders that will be used.  
* **ColumnOptions** and **ColumnMode:** The initial columns that will be added, removed or replaced, depending of `ColumnMode`.  
* **Pagination**: The initial pagination that will be used, from `All`, `Firsts(elementsPerPage)` or `Paginate(elementsPerPage, currentPage)`.  
* **SearchOnLoad:** Controls if results should be shown automatically when the page with the the `SearchControl` is loaded. Default `false`.
* **ShowFilters:** Control if the filter panel should be visible. Default `true`.
* **ShowFilterButton:** Control if the filter button should be visible. Default `true`.
* **ShowHeader:** Controls if the whole header should be visible, if `false` overrides the two previous property and also hides the search button and the extensible `MenuItems`. Default `true`.
* **ShowFooter:** Controls if the footer should be visible, if `false` hides all the pagination controls in the bottom. Default `true`.  
* **Create:** Allows you to hide create button in one specific search control. 
* **Navigate:** Allows you to hide navigation links in one specific search control.
* **AllowChangeColumns:** Allows you to override the default behavior defined in `Finder.Manager.AllowChangeColumns` to let or forbid users Add/Remove/Rename/Reorder columns. 
* **AllowSelection:** Allows you to remove the selection column. 
* **AllowOrder:** Allows you to make columns not ordenable. 
* **ShowContextMenu:** Allows you to remove the columns.
* **SelectedItemsContextMenu:** Allows you to remove entity-specific options of the context menu, like QuickLinks or Operations.  



## QuerySettings


`QuerySettings` let you configure how a query will be shown in the `SearchControl`, just as `EntitySettings` configures how an entity will be shown in the `NormalPage`. It is non mandatory, and a default `QuerySettings` will be automatically created if none is provided. 

```C#
public class QuerySettings
{
    public object QueryName { get; }
    public QuerySettings(object queryName);

    public string WebQueryName { get; set; }
    public Pagination Pagination { get; set; }
    public bool IsFindable { get; set; } 

    public Dictionary<string, CellFormatter> Formatters;  
}
```

* **QueryName:** The query to configure.
* **WebQueryName:** Let you override the default WebQueryName that will be used as key for the query, including in the URL (`/Find/[WebQueryName]`). By default is the `CleanName` for types, or the `ToString` for any other, and should be unique. 

> Finder also contains the methods `ResolveWebQueryName` and `ResolveQueryName` to convert QueryNames (`object`)  to WebQueryNames (`string`).

* **Pagination:** Override `FindOptions.DefaultPagination` for this particular query. 
* **IsFindable:** Hides this particular query so is not findable. For example if only available in Windows interface. 

> `Finder` also provides the `IsFindable` method to test if a query is findable or not. 

* **Formatters:** Let you customize how to represent the cells of one particular column of this query by registering a `CellFormatter` in the column name.
	* WriteData: Indicates if a `data-value` attribute should be written in the ID with the `ToString` (required for adding filters of non-trivial columns).
	* TextAlign: Indicates if the column should be aligned to the left (text), right (numbers) or center. 
	* Formatter:  Does the actual conversion from the `object` value to a piece of `MvcHtmlString` with the content of the 

```C#
public class CellFormatter
{
    public bool WriteData = true;
    public string TextAlign;
    public Func<HtmlHelper, object, MvcHtmlString> Formatter; 

    public CellFormatter(Func<HtmlHelper, object, MvcHtmlString> formatter); 
}
```

Usually, instead of registering a `CellFormatter` for one particular query column, makes more sense to register it for one particular database column, and is applied in any query that uses it. 

We can do this by using the static dictionary `PropertyFormatters`: 

```C#
public class QuerySettings
{
    public static Dictionary<PropertyRoute, CellFormatter> PropertyFormatters { get; set; }

    public static void RegisterPropertyFormat<T>(Expression<Func<T, object>> propertyRoute, 
		CellFormatter formatter) where T : IRootEntity
}
```

Example: 

```C#
QuerySettings.RegisterPropertyFormat<CategoryDN>(c => c.IndentedName, new CellFormatter((helper, value) =>
                    new HtmlTag("pre").Class("gray").SetInnerText(value.ToString())));
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
    public string Name { get; }

    public Func<Column, CellFormatter> Formatter { get; set; }
    public Func<Column, bool> IsApplicable { get; set; }

    public FormatterRule(string name, 
		Func<Column, bool> isApplicable, 
		Func<Column, CellFormatter> formatter)
}
```

For each column, the first `FormatRule` that returns `true` when evaluating `IsApplicable` is used. Actually, since the rules are ordered from more general to most specific, they are checked backwards using `Last` operator.

```C#
QuerySettings.Rules.Add(new FormatterRule("TimeSpan", 
	isApplicable: c=>c.Type.UnNullify() == typeof(TimeSpan), 
	formatter: c =>new CellFormatter((helper, value) => new HtmlTag("pre").Class("gray").SetInnerText(value.ToString())))),
```

## ParseLiteKeys

Often contextual operations or any other TypeScript code calls a controller with the list of selected entities in a `SearchControl`.

The format is usually a comma-separate list of `Lite` keys. 

In order to parse this information, we can use `Finder.ParseLiteKeys`: 


```C#
public static List<Lite<T>> ParseLiteKeys<T>(string liteKeys) 
	where T : class, IIdentifiable
{
    return liteKeys.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(Lite.Parse<T>).ToList();
}
```

Since this list is usually passed in the request on the `"liteKeys"` key, a more convinient overload exists: 

```C#
public static List<Lite<T>> ParseLiteKeys<T>(this ControllerBase controller) 
	where T : class, IIdentifiable
{
    return ParseLiteKeys<T>(controller.ControllerContext.RequestContext.HttpContext.Request["liteKeys"]);
}
```

Example: 
```C#
public ActionResult ProcessFromMany()
{
    Lite<IdentifiableEntity> lites = this.ParseLiteKeys<IdentifiableEntity>();
    ....
}
```




