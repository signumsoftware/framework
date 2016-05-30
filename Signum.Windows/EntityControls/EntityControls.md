## Entity Controls

Entity Controls are a family of controls designed to manipulate any property of your entities using **just a simple line of XAML**.

There are a few reasons why we can simplify to code so much: 

* Use [`Common.Route`](../Facades/Common.md) attached property, as a simple alternative to `Binding`.
* Rely on meta-data available in the entities itself, or the `EntitySettings` registered in the `Navigator` to get excellent default behaviors. 
* Integrate `Label` as part of the control in the same line.

### LineBase (abstract)

`LineBase` is a simple abstract control that is the root of all the entity controls. Represents a control with a `Type` and a `LabelText`. 

```C#
public partial class LineBase : UserControl
{
   public string LabelText;
   public Type Type;
   public event EventHandler PreLoad;
}
```

*  **LabelText:** The string that will be used as a label, typically at the left of the control.  
*  **Type:** The `System.Type` of bound property (if any) or the value that this control represents.
*  **PreLoad:** Event that will be called just before `Load`. Necessary for `Common.Route` in certain scenarios.  


### ValueLine

`ValueLine` is the necessary control to display simple values like numbers (`int`, `long`, `float`, `double`, `decimal`...), `string`, `bool`, `DateTime`, `TimeSpan`, `Color`, etc... 

```XML
 <m:ValueLine m:Common.Route="Start" />
```

`ValueLine` is inherits directly from `LineBase`, and contains the following dependency properties: 

```C#
public partial class ValueLine : LineBase
{
   public ValueLineType ValueLineType;
   public Control ValueControl;
   public object Value;
}
```
*  **ValueLineType:** Determines the type of ValueLine that will be shown, deduced from the inherited `Type` property and ultimately affecting the automatic `ValueControl`. The following table is used to determine `ValeLineType`: 

| Type          | ValueLineType |
| ------------- |:-------------:|
| bool          | Boolean      
|byte, short, int, long | Number |
|sbyte, ushort, uint, ulong | Number |
| single double, decimal | Number |
| DateTime      | DateTime      |
| TimeSpan      | TimeSpan      |
| ColorEntity       | Color         |
| any Enum      | Enum          |
| string, char  | String      |
| any object    | String      |


*  **ValueControl:** The control that will be used to represent the `Value`. Automatically deduced from `ValueLineType` using this table: 

| ValueLineType | Control | Binding Property | ReadOnly Property |
| ------------- |---------|------------------|-------------------|
| Boolean  |ComboBox |IsChecked|!IsEnabled|
| Number  |NumericTextBox |Value |IsReadOnly|
| String  |TextBox |Text |IsReadOnly|
| DateTime  |DateTimePicker | SlectedDate|IsReadOnly|
| TimeSpan  |TimePicker | TimePart|IsReadOnly|
| Enum  |ComboBox | SelectedItem|!IsEnabled|
| TimeSpan  |TimePicker | TimePart|IsReadOnly|
| Color  |ColorPicker | SelectedColor|IsReadOnly|


*  **Value:** This property contains the actual value that will be bound.

> **Important Note:** If you create a binding to `ValueLine.Value`, the binding will be cleared and re-created using the 'Binding Property' from `ValueControl`, completely ignoring `ValueLine.Value`. 


Additionally, `ValueLine` contains other properties that only make sense for certain `ValueLineType`. 

```C#
public partial class ValueLine : UserControl
{
   public string UnitText;
   public string Format;   
   public IEnumerable ItemSource; 
   public int? MaxTextLength; 
}
```

*  **UnitText:** The unit that will be shown at the right side of the (usually numeric) value (i.e.: €, $, Kg, Km/h...). Typically inherited from `UnitAttribute` in the bounded property. 

*  **Format:** The standard or custom format string to convert numbers, `DateTime` or `TimeSpan` to and from `string` (i.e.: g, dd/MM/yyyy, p...). Typically inherited from `FormatAttribute` in the data-bound property. 

* **ItemSource:** The elements to populate the `ComboBox` if `ValueLineType=Enum`. Typically the ones defined in the Enum Type. 

* **MaxTextLength:** The `MaxLength` of the `TextBox` used if `ValueLineType=String`. Typically from the `StringLengthValidatorAttribute`. 

## EntityBase (abstract)

`EntityBase` is the abstract base control for `EntityLine`, `EntityCombo`, `EntityDetail` and `EntityListBase`, and inherits from `LineBase`. It represents a control that can modify a reference to a `ModifiableEntity` or `Lite<T>`. 

`EntityBase` defines many shared properties and events:

```C#
public class EntityBase: LineBase
{
   public object Entity;
   public Implementations? Implementations;
   public DataTemplate EntityTemplate;
   public DataTemplateSelector EntityTemplateSelector;
}
```

* **Entity:** The `ModifiableEntity` or `Lite<T>` behind the control. 

* **Implementations:** The `Implementations` structure, mandatory for non-embedded entities, that indicates if the reference can contain entities of different types, adjusting the possible interactions by asking the user to decide the concrete type before `Create` or `Find`. Deduced from `ImplementedBy` and `ImplementedByAll` attributes in the bounded property.  

* **EntityTemplate and EntityTemplateSelector:** Optional, allows you to change how the entities are shown in the blue pill of an `EntityLine` or the elements of the `EntityCombo`.   

```C#
public class EntityBase: LineBase
{
   public bool Create;
   public bool Find;
   public bool View;
   public bool Navigate;   
   public bool Remove;

   public bool ViewOnCreate;
   public bool ReadOnlyEntity;
    
   public event Func<object> Creating;
   public event Func<object> Finding;
   public event Func<object, object> Viewing;
   public event Action<object> Navigating; 
   public event Func<object, bool> Removing;
}
```

Additionally, `EntityBase` provides the four (now five!)  typical buttons for manipulating entity references. 

### Create

The `Create` property controls the visibility of the create (+) button that let the user instantiate a new entity and is visible only when `Entity` is `null`.

By default, clicking on create button does the following: 
1. Chooses the appropriate type (maybe asking the user using a `SelectorWindow` if there are many `Implementations`) 
2. Instantiates the new entity using `Constructor.Construct`. 
3. Shows the new entity to the user using `Navigator.View` if `ViewOnCreate=true` (default). 

But you can handle `Creating` event to customize this process, returning `null` to cancel changes or a `ModifiableEntity`/`Lite<T>` at the end (automatic conversion will be made) to confirm then.

Example of using `Creating` to initialize to customize the entity initialization: 

```XML
<m:EntityCombo m:Common.Route="Category"  Creating="EntityCombo_Creating" x:Name="category"/>
```

```C#
public Product()
{
    InitializeComponent();
    this.category.Remove = true;
    this.category.Create = Navigator.IsCreable(typeof(CategoryEntity), isSearch: true); 
}

private object EntityCombo_Creating()
{
    return Navigator.View(new CategoryEntity
    {
        CategoryName = ((ProductEntity)this.DataContext).ProductName
    });
}
``` 


### Find

The `Find` property controls the visibility of the find (o-) button that let the user choose an already created entity from the database, and is visible only when `Entity` is `null`.

By default, clicking on find button chooses the appropriate type (maybe asking the user using a `SelectorWindow` if there are many `Implementations`) 
and opens `SearchWindow` entity using `Finder.Find`. 

But you can handle `Finding` event to customize this process, returning `null` or a `ModifiableEntity`/`Lite<T>`. For example to open a `SearchControl` already filtered by some business criteria. 

Example of using `Finding` to search using a custom `FindOptions`: 

```XML
 <m:EntityLine m:Common.Route="Customer" Finding="EntityLine_Fining" />
```

```C#
private object EntityLine_Fining()
{
   return Finder.Find<CustomerEntity>(new FindOptions()
   {
       FilterOptions = { new FilterOption("IsActive", true) { Frozen = true } },
   }); 
}
``` 

### View

The `View` property controls the visibility of the view (->) button that let the user open the current entity and, optionally, modify it. It is visible only when `Entity` is **not** `null`.

By default, clicking on view button just shows the entity using `Navigator.View`, but you can handle `Viewing` event to customize this process, returning `null` to cancel changes or a `ModifiableEntity`/`Lite<T>` to confirm them.  

Example of using `Viewing` to to customize the view: 

```XML
 <m:EntityLine m:Common.Route="Customer" Viewing="EntityLine_Viewing" />
```

```C#
private object EntityLine_Viewing(object entity)
{
   return Navigator.ViewUntyped(entity new ViewOptions()
   {
       View = new CustomerUI { Parent = this, FastMode = true },
   }); 
}
``` 

### Navigate

The `Navigate` property controls the visibility of the navigate (◫) button that let the user open the current entity independently, and optionally modify it, but without affecting the parent window. It is visible only when `Entity` is **not** `null`. 

>**Note:** `Navigate` is automatically turned off if `View` is visible to avoid confusing the user.

By default, clicking on navigate button just shows the entity using `Navigator.Navigating`, but you can handle `Viewing` event to customize this process, returning `null` to cancel changes or a `ModifiableEntity`/`Lite<T>` to confirm them.  

Example of using `Navigating` to to customize the view: 


```XML
 <m:EntityLine m:Common.Route="Customer" Navigating="EntityLine_Navigating" />
```

```C#
private void EntityLine_Navigating(object entity)
{
   Navigator.Navigate(entity new ViewOptions()
   {
       View = new CustomerUI { FastMode = true },
   }); 
}
``` 

### Remove

The `Remove` property controls the visibility of the remove (x) button that let the user disassociate the current entity with the related one by setting the `Entity` property to `null`. It is visible only when `Entity` is **not** `null`. 

By default, clicking on navigate button removes the relationship setting the bounded property to `null`, but you can handle `Removing` to stop this from happening.  

Example of using `Removing` to ask the user for permission: 

```XML
 <m:EntityLine m:Common.Route="Customer" Removing="EntityLine_Removing" />
```

```C#
private bool EntityLine_Removing(object entity)
{
   if(MessageBox.Show("Sure?") == MessageBoxResult.OK)
       return true;

   return false;
}
``` 

> Note that `Remove` will just dissociate the relationship, not delete the related entity from the database. If you want to delete the related entity from the database, consider doing so in the server side using `Save` or any other operation, or even using `EntityEvents`. 


###  EntityChange

Finally, the event `EntityChange` is defined in `EntityBase` control and fired every time the `Entity` property changes its value. 

```C#
public event EntityChangedEventHandler EntityChanged;

public delegate void EntityChangedEventHandler(object sender, bool userInteraction, 
    object oldValue, object newValue);
```

`EntityChanged` will be fired independently of which button is pressed, or even if the property is changed programmatically, in this case `userInteraction` parameter will be `false`. 
   
Example: 


```XML
<m:EntityLine m:Common.Route="Customer" EntityChanged="EntityLine_EntityChanged" />
```

```C#
private void EntityLine_EntityChanged(object sender, bool userInteraction, object oldValue, object newValue)
{
    if (userInteraction)
        this.OrderEntity.ShipAddress = ((CustomerEntity)newValue)?.Address.Clone();
}
``` 

## EntityLine

`EntityLine` is a `EntityBase` control that contains, in his right side, a placeholder for an entity. This placeholder is just a box with the four buttons (Create, Find, View and Remove) conveniently hidden depending if the entity is `null` or not.

`EntityLine` can be used to represent associations with embedded entities witch details are not important enough to take room in the parent entity control, but usually `EntityDetail` is more suited for embedded entities. 

### Autocomplete
Where `EntityLine` shines is representing relationships with high-populated entities (for witch a `ComboBox` wouldn't work) by using `Autocomplete`.

By double-clicking (single-click if `Entity` is `null`), or pressing [F2] when focused, the blue place holder of the `EntityLine` becomes a `AutocompleteTextBox` already configured to query the database. 

By default, `Autocomplete` is even able to find candidate entity by `ToString` using the default `ToString` of the entity,  or `Id`. You can use quotes to find ID-like values in the text, like: `'1`.

`Autocomplete` is also able to understand `Implementations`, querying multiple tables if necessary. 


```C#
public partial class EntityLine : EntityBase
{ 
    public bool Autocomplete; 
    public event Func<string, IEnumerable<Lite<Entity>>> Autocompleting;
    public int AutocompleteElements = 5;
}
```

Example: 

```XML
 <m:EntityLine m:Common.Route="Customer" Autocompleting="EntityLine_Autocompleting" />
```

```C#
private IEnumerable<Lite<Entity>> EntityLine_Autocompleting(string term)
{
   return Server.Returning((ICustomServer c) => c.CustomAutocomplete(term)); 
}
```
```C#
//And in the server side
return Database.Query<PersonEntity>()
    .Where(p => !p.Corrupt)
    .Autocomplete(term, 5);  //Defined in AutoCompleteUtils
```

> **Note:** For languages with **accents** (like Spanish or French), you need to change the SQL Server Collation options at the column, database or server level. 

## EntityCombo

`EntityCombo` is used to represent entity properties when the expected range of possible entities to choose from is smaller and you want to show them all. 

### LoadData
In order show the entities of the combo, it has to be loaded. `EntityCombo` adds two members to control how and when this loading has to be done. 

```C#
public partial class EntityCombo : EntityBase
{ 
    public LoadDataTrigger LoadDataTrigger;
    public bool SortElements;
    public event Func<IEnumerable<Lite<IEntity>>> LoadData;
    public bool NullValue;
}
```

* **LoadDataTrigger:** Indicates when the `ComboBox` should be loaded, when the `EntityCombo.Loaded` fires, or then is expanded the first time (default). 

* **SortElements:** Indicates if the elements should be sorted alphabetically before showing it (default true). 

* **LoadData:** This event lets you customize the entities that will be shown when the `EntityCombo` is loaded, by default all the visible `Lite<T>` of the different `Implementations` are retrieved from the database. 

* **NullValue:** If `true`, a faked `null` value with `ToString == " - "` will be added, letting the user turn back to `null` value once something is selected. It's more intuitive and takes less space than making `Remove` visible.  

> EntityCombo hides by default `Remove`, `Find` and `Navigate`. 


```XML
 <m:EntityCombo m:Common.Route="Country" LoadData="EntityCombo_LoadData" />
```

```C#
private IEnumerable<Lite<Entity>> EntityCombo_LoadData(string term)
{
   return new List<Lite<CountryEntity>>
   {
      brazil, rusia, india, china
   }; 
}
```

## EntityDetail

`EntityDetail` is mainly used to represent embedded entities, since it embeds the entity control of the related entity inside of the parent control. 

Sill, `EntityDetail` provides a blue header with the entity `ToString`, and optional icon, and the four buttons to manipulate the reference. 


```C#
public partial class EntityDetail : EntityBase
{ 
    public ImageSource Icon;
    public object EntityControl;
}
```  

* **Icon:** An optional icon to show before the `Label` in the blue header. 

* **EntityControl:** The control that will be used represent the entity. By default a [`DataBorder`](../DataBorder.md) with `AutoChild = true` is added. With this property the `DataBorer` will look for the specific control of the current entity using the `EntitySettings` registered in `Navigator`.

  If you want to specify set any specific control. Consider adding a `DataBorder` to your control to hide it when `Entity` is `null`,  the `DataContext` of this control 

Example:

```XML
<m:EntityDetail m:Common.Route="ShipAddress"  Margin="2,0">
    <m:EntityDetail.EntityControl>
        <m:DataBorder>
            <sc:Address/>
        </m:DataBorder>
    </m:EntityDetail.EntityControl> 
</m:EntityDetail>
```

## EntityListBase (abstract)

The abstract class `EntityBaseList` inherits from `EntityBase` and is, at the same time, the base class for all the controls that represent and [`MList`](../../Signum.Entities/MList.md) of `Lite<T>` or `ModifiableEntity`, for example `EntityList`,  `EntityRepeater` and `EntityStrip`. 

```C#
public partial class EntityListBase : EntityBase
{ 
    public Type EntitiesType
    public IList Entities
    public bool Move
}
```

* **Entities:** Replaces `EntityBase.Entity` as the principal control that you have to bind to.
 * Typically bound to a `MList<T>` or `ObservableCollection<T>` or any other collection that implements `INotifyCollectionChange`. 
 * `T` should be a `Lite<T>` or `ModifiableEntity`. 
 * `EntityBase.Entity` will be used as the selected element if any that makes sense (`EntityList`).
 
* **EntitiesType:** The `ElementType` of the collection.  For example if `EntityBase.Type` is `MList<CustomerEntity>`, `EntitiesType` will be just `CustomerEntity`.  All the controls inheriting from `EntityBase `EntityBase.Implementations` will also 

### Move

Shows Up (↑) and Down (↓) buttons to re-order the entities in the list. 

By default this property is set by a [`Common`](../Common.md) task if the `MList` property has a `PreserveOrderAttribute`. 

### Finding

`EntityBase.Finding`, of type `Func<object>` get's now one extra feature: More than one entitis can be added by returning a `IEnumerable`. 

In fact, the default implementation is overriden in `EntityListBase` to use `Finder.FindMany`, but you can write your own. 

```XML
 <m:EntityList m:Common.Route="Customers" Finding="EntityList_Finding" />
```

```C#
private object EntityList_Finding()
{
   return Finder.FindMany<CustomerEntity>(new FindOptions()
   {
       FilterOptions = { new FilterOption("IsActive", true) { Frozen = true } },
   }); 
}
``` 


## EntityList

`EntityList` is one of the controls provided by Signum Windows to represent lists on the user interface, using  a simple `ListBox` to visualize the results.

```C#
public partial class EntityList : EntityListBase
{ 
    public SelectionMode SelectionMode
    public IList SelectedEntities
}
```

* **SelectionMode:** Controls the ListBox [SelectionMode](http://msdn.microsoft.com/es-es/library/system.windows.forms.selectionmode(VS.80).aspx).
* **SelectedEntities:** If more than one entity is selected, this property returns all the selected items, while `EntityBase.Entity` just the first one. 

`EntityList` has two typical use cases: As a simple list and using pop-ups to view and create entities, or as a master-detail view:

### Simple list

Just add a `EntityList` without setting any property should do the job:

```XML
<m:EntityList m:Common.Route="Customers" Finding="EntityList_Fining" />
``` 

### Master-Detail

Use and `EntityList` but:
1.  Turn off `View` and `ViewOnCreate` to avoid pop-ups. 
2.  Create a `Grid` or any other container to layout the list and the detail view together. 
3.  Use WPF master-detail DataBinding by apending `/` to bind the current element of a list.
4.  Use a [`DataBorder`](../DataBorder.md) to hide the detail view if nothing is selected, and make a small fade-in/fade-out animation when the selection changes. 


```XML
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" MinWidth="200"/>
        <ColumnDefinition Width="*" MinWidth="200"/>
    </Grid.ColumnDefinitions>
    <m:EntityList m:Common.Route="Comments" ViewOnCreate="False" Grid.Column="0"/>
    <m:DataBorder m:Common.Route="Comments/" Grid.Column="1">
        <StackPanel>
            <m:ValueLine m:Common.Route="Date" />
            <m:EntityCombo m:Common.Route="Writer"/>
        </StackPanel>
    </m:DataBorder>
</Grid>
```

Or, if you have registered a default view using `Navigator` and `EntitySettings`, just set `AutoChild=true` in the `DataBorder`:

```XML
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" MinWidth="200"/>
        <ColumnDefinition Width="*" MinWidth="200"/>
    </Grid.ColumnDefinitions>
    <m:EntityList m:Common.Route="Comments" ViewOnCreate="False" Grid.Column="0"/>
    <m:DataBorder m:Common.Route="Comments/" Grid.Column="1" AutoChild="true"/>
</Grid>
```

### EntityRepeater

`EntityRepeater` control shows all the entities detail in the collection embedded in the parent entity control. Think of it as a mixture of `EntityList` and `EntityDetail`, it also contains a blue header with the create (+) and find (o-) buttons, but more than one element can be added, each with his owns buttons for view (->), remove (x), and move up (↑) and down (↓).

The main important property of the `EntityRepeater` is the  inherited, but now **mandatory**, `EntityTemplate`. This `DataTemplate` can be defined in-line and defines how each entity will be shown. 

```C#
public partial class EntityRepeater : EntityListBase
{ 
    public ImageSource Icon

    public ScrollBarVisibility VerticalScrollBarVisibility
    public ScrollBarVisibility HorizontalScrollBarVisibility

    public ItemsPanelTemplate ItemsPanel
    public Style ItemContainerStyle

    public Orientation ButtonsOrientation
}
```

* **VerticalScrollBarVisibility:** Shows a vertical scroll for the repeated controls, `Disabled` by default.
* **HorizontalScrollBarVisibility:** Shows a horizontal scroll for the repeated controls, `Disabled` by default.
* **ItemsPanel:** Let's you change the `ItemsPanelTemplate` that will be used to arrange the elements using some exotic layout, like a `Grid`, or a `WrapPanel`. 
* **ItemContainerStyle:** Controls the template that will be used **around** your `EntityTemplate` to change the buttons and default border of each element. 
* **ButtonsOrientation:** Controls the orientation of the buttons in the default `ItemContainerStyle`, by default they are vertical, optimized for large `EntityTemplates` but horizontal is more suited for controls with just one line. 

Example: 

```XML
<m:EntityRepeater m:Common.Route="Links">
    <m:EntityRepeater.EntityTemplate>
        <DataTemplate>
            <Grid m:Common.LabelVisible="False" HorizontalAlignment="Stretch">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" MinWidth="60"/>
                    <ColumnDefinition Width="2*" />
                </Grid.ColumnDefinitions>
                <m:ValueLine m:Common.Route="Label" Grid.Column="0" />
                <m:ValueLine m:Common.Route="Link" Grid.Column="1" />
            </Grid>
        </DataTemplate>
    </m:EntityRepeater.EntityTemplate>
</m:EntityRepeater>
```


### EntityStrip

`EntityStrip` control shows all the entities as small pills that can be added using `Autocomplete`. Think of it as a control for choosing Tags. At the end the tag strip there are buttons to create (+) and find (o-) entities, but `Autocomplete` is the most usefull way to use it. 

Each element is just a small pill, but can optionally have his owns buttons for view (->), remove (x), and move up (↑) and down (↓).

Just as `EntityLine`, `EntityStrip` defined three properties to control `Autocomplete` functionality:

```C#
public partial class EntityStrip : EntityListBase
{ 
    public bool Autocomplete
    public int AutocompleteElements 
    public event Func<string, IEnumerable<Lite<Entity>>> Autocompleting
}
```

Additionally, `EntityStrip` defines an `Orientation` property that changes the layout of the control: 

* **Orientation.Horizontal:** Optimized for a few number of small tag-like entities where the order doesn't matter. 
* **Orientation.Vertical:** Optimized for a larger number of longer entities, or the order does matter and move buttons are required.

Finally, just like in `EntityRepeater`, you can customize how the elements are laid out using `ItemsPanel` and `ItemContainerStyle` properties.


Example: 

```XML
<m:EntityStrip m:Common.Route="Territories"/>
```
    