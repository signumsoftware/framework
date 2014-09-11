# Finder class 

The main responsibilities of `Finder` is to open query result to search for a particular entity using a modal window (`Find` and `FindMany`) or just take a look at the entities and start actions from there in an independent window (`Explore`). 

By default the entities are shown using a `SearchWindow`, but the `FindManager` can be overriden to open any other kind of control or window.

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

The class `ExploreOptions` inherits all the options from `FindOptionsBase` but let us change some behavior: 

* **NavigateIfOne:** Executes the query internally to test if there's just one result, and in this case navigates to the only entity without even opening the `SearchWindow`.
* **Closed:** Event that will be fired when the windows is closed, could be used to refresh the original UI, but could be called in another thread.
