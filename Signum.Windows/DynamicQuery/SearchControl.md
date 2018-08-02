# SearchControl

We have already seen how to register queries and extensions in [`QueryLogic.Queries`](../../Signum.Engines/DynamicQuery/DynamicQueries.md) and how to open `SearchWindows` using [`Finder`](../Facades/Finder.md).

`SearchControl` is a complex control that, using the services of `QueryLogic.Queries`, let's the used manipulate a query by adding and removing columns, filters, order columns an control pagination. 

`SearchControl` is also the main control contained in `SearchWindows`, but you can also embed this control in the custom control of your entities. 


## FindOption-like properties

Search control contains many dependency properties that are exact copies of the ones available in  `FindOptions`: 


```C#
public partial class SearchControl
{
   public object QueryName;
   public FreezableCollection<FilterOptions> FilterOptions; 
   public ObservableCollection<OrderOption> OrderOptions;
   public ColumnOptionsMode ColumnOptionsMode; 
   public ObservableCollection<ColumnOption> ColumnOptions;
   public Pagination Pagination;
   
   public bool SearchOnLoad;

   public bool ShowFilters; 
   public bool ShowFilterButton;
   public bool ShowHeader;
   public bool ShowFooter;
}
```

* **QueryName:** The query that will be used to search results. Mandatory, and `typeof(T)` will be used for the overloads that take no `FindOptions`. Mandatory.
* **FilterOptions:** The initial filters that will be used, automatic conversion of `Lite<T>` will be made, and the filters can also be `Frozen` so they can not be changed or removed. 
* **OrderOptions:** The initial orders that will be used.  
* **ColumnOptions** and **ColumnMode:** The initial columns that will be added, removed or replaced, depending of `ColumnMode`.  
* **Pagination**: The initial pagination that will be used, from `All`, `Firsts(elementsPerPage)` or `Paginate(elementsPerPage, currentPage)`. By default is `All` to avoid pagination on `SearchControl` embedded in custom controls.  

* **SearchOnLoad:** Controls if results should be shown automatically when the windows is opened the `SearchControl` is `Loaded`. Default `false`.
* **ShowFilters:** Control if the filter panel should be visible. Default `false` for embedded `SearchControl`.
* **ShowFilterButton:** Control if the filter button should be visible. Default `true`.
* **ShowHeader:** Controls if the whole header should be visible, if `false` overrides the two previous property and also hides the search button and the extensible `MenuItems`. Default `true`.
* **ShowFooter:** Controls if the footer should be visible, if `false` hides all the pagination controls in the bottom. Default `false` for embedded `SearchControl`.   


## EntityBase-like properties

`SearchControl` also contains a bunch of properties and events that resemble from [EntityBase](../EntityControls/EntityControls.md).

```C#
public partial class SearchControl
{
   public Type EntityType;
   public Implementations Implementations; 

   public bool Navigate;
   public bool Create;
   public bool NavigateOnCreate;  
   public bool Remove;

   public event Func<Entity> Creating;
   public event Action<Entity> Navigating;
   public event Action<List<Lite<Entity>>> Removing;

}
```
* **EntityType:** The `System.Type` of the `Entity` column, should be an `IEntity` but can be an abstract class or an interface with `Implementations`. 

* **Implementations:** The `Implementations` structure of the `Entity` column. Indicates if the column can contain entities of different types, adjusting the possible interactions by asking the user to decide the concrete type before `Create`.  

* **Navigate:** Shows the navigate (◫) button, by default the value is set to `true` if any of the `Implementations` evaluates true to `Navigator.IsCreable(t, isSearchEntity: true)`.

* **Navigating:** Let's you control what will happen when navigate (◫) button is pressed. By default will call `Navigator.Navigate` with the selected entity.

* **Create:** Shows the create (+) button, by default the value is set to `true` if any of the `Implementations` evaluates `true` to `Navigator.IsCreable(t, isSearchEntity: true)`.

* **Creating:** Let's you control what will happen when create (+) button is pressed. By default will show a selector with the type to create (if necessary), create it using `Constructor.Construct` and then navigate to it using `Navigator.Navigate` if **NavigateOnCreate** is true.


## Other properties

Moreover, `SearchControl` contains some particular dependency properties and events, like: 

```C#
public partial class SearchControl
{
   public int ItemsCount
   public Lite<Entity> SelectedItem;
   public List<Lite<Entity>> SelectedItems;

   public bool MultiSelection;
   public bool AllowChangeColumns;

   public event Action DoubleClick;
   public event Func<Column, bool> OrderClick;
}
```

* **ItemsCount:** The number of rows in the result. 
* **SelectedItem:** The `Lite<Entity>` in the `Entity` column of the selected row.
* **SelectedItems:** The `List<Lite<Entity>>` of the `Entity` column of the selected rows, if multi selection is `true`. 
* **MultiSelection:** Allows multiple row selection, by default is `true` even for `Navigation.Find` to enable multi-entity operations using the context menu.
* **AllowChangeColumns:** Disables the functionality to Add, Remove, Reorder or Rename columns. By default is `true` even for embedded `SearchControl . 

## FilterColumn and FilterRoute

And finally, a pair of properties `FilterColumn` and `FilterRoute` simplify adding embedded `SearchControl` in custom controls.

```C#
public partial class SearchControl
{
   public string FilterColumn;
   public string FilterRoute; 
}
````

* **FilterColumn:** The name of the query column to filter by. A `QueryToken` string. 
* **FilterRoute:** Optional property that will be used as binding expression of the current `DataContext`, similar to what you could write in `Common.Route`. If not provided, the `DataContext` will be used. 

This two properties can only be set at construction time, before it has been loaded. And then when `Loaded` is fired, the `SearchControl` auto-configures for the common case of embedded `SearchControls`: 

* Create Frozen Filter with Column `FilterColumn`, Operation `Equals`, and Value bindded to `DataContext` + `FilterRoute`.
* If `FilterColumn` is in the list of columns, it also removes the column.
* IF `SearchOnLoad` has not been set, set the propery to `true`.

For example: 

```XML
<m:SearchControl QueryName="{x:Type d:PackageLineEntity}"  FilterColumn="Package"/>
```

Is equivalent to:

```XML
<m:SearchControl QueryName="{x:Type d:PackageLineEntity}" ColumnOptionsMode="Remove" SearchOnLoad="True" >
    <m:SearchControl.FilterOptions>
        <m:FilterOption  ColumnName="Package"  Operation="EqualTo" Value="{Binding DataContext}" Frozen="True" />
    </m:SearchControl.FilterOptions>
    <m:SearchControl.ColumnOptions>
        <m:ColumnOption ColumnName="Package"/>
    </m:SearchControl.ColumnOptions>
</m:SearchControl>
````    

