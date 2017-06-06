## Entity Controls

Entity Controls are a family of MVC Helpers and TypeScript clases designed to manipulate any property of your entities using **just a simple line of Razor**.

There are a few reasons why we can simplify to code so much: 

* Using [`TypeContext`](../TypeContext/TypeContext.md), we can transfer not only the property value or property name, but the complete chain of properties, necessary for getting authorization or implementations information. 
* Rely on meta-data available in the entities itself, or the `EntitySettings` registered in the `Navigator` to get excellent default behaviors. 
* Integrate `Label` as part of the control in the same line.

While in Signum Windows each Entity Control is just one control, in Signum Web it's implemented with three different components: 

1. **HTML Helper:** Following the convention of ASP.Net MVC, there is an HTML Helper for each entity control implemented in their own static class. This helper is responsible for generating the necessary HTML and giving behavior to the control instantiating the TypeScript class in a small script tag.

   ```C#
    @Html.ValueLine(ec2, e => e.TitleOfCourtesy)
   ``` 
2. **C# options class:** Since each entity control has many options, a class with the same name exists containing the necessary properties. This class receives the default values from the Tasks registered in [Common](../Common.md) class and then is up to you to make the final changes. 

    ```C#
    @Html.ValueLine(ec2, e => e.TitleOfCourtesy, vl =>
    {
        vl.ValueHtmlProps["size"] = 4;
        vl.LabelText = "Title";
    })
   ```

3. **TypeScript class:** Finally, in order to give interaction to the controls, a hierarchy of TypeScript classes is provided. Those classes have the events necessary to override user interaction (`creating`, `finding`, etc...). From C#, only `AttachFunction` is available in `EntityBase`, a [JsFunction](../Facades/JsFunction.md) that serves as a bridge between C# and TypeScript. 

### LineBase (abstract)

`LineBase` is a simple abstract control that is the root of all the entity controls. Represents a control with a `Type` and a `LabelText`. 

```C#
public abstract class LineBase : TypeContext
{
   public string LabelText;
   public readonly RouteValueDictionary LabelHtmlProps; 
   public readonly RouteValueDictionary FormGroupHtmlProps; 
   public bool Visible;
   public bool HideIfNull;
}
```

*  **LabelText:** The string that will be used as a label, typically at the left of the control.  
*  **Visible:** When false, avoids rendering the control in HTML.
*  **LabelHtmlProps:**  
*  **FormGroupHtmlProps:**  
*  **HideIfNull:** When true, avoids rendering the control in HTML is the `UntypedValue` property (inherited from `TypeContext`) is `null`.  


### ValueLine

`ValueLine` is the necessary control to display simple values like numbers (`int`, `long`, `float`, `double`, `decimal`...), `string`, `bool`, `DateTime`, `TimeSpan`, `Color`, etc... 

```XML
 @Html.ValueLine(tc, s => s.Start)
```

`ValueLine` is inherits directly from `LineBase`, and contains the following dependency properties: 

```C#
public class ValueLine : LineBase
{
   public ValueLineType? ValueLineType;
   public string Format;
   public string UnitText;
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
| ColorEmbedded       | Color         |
| any Enum      | Enum          |
| string, char  | String      |
| any object    | String      |


Ultimately, the `ValueLineType` determines the HtmlHelper that will be used. 

| ValueLineType | Control |
| ------------- |---------|
| Boolean   |CheckBox     |
| Number    |NumericTextbox|
| String    |TextboxInLine      |
| TextArea    |TextAreaInLine |
| DateTime  |DateTimePicker|
| TimeSpan  |TimeSpanPicker   |
| Enum      |EnumComboBox     |
| TimeSpan  |TimePicker   |
| Color  |ColorTextbox   |

If you want to use a non-standart helper just use `FormGroup` extension method directly.

Additionally, `ValueLine` contains other properties that only make sense for certain `ValueLineType`. 

```C#
public class ValueLine : LineBase
{
   public string UnitText;
   public string Format;   
   public List<SelectListItem> EnumComboItems; 
}
```

*  **UnitText:** The unit that will be shown at the right side of the (usually numeric) value (i.e.: €, $, Kg, Km/h...). Typically inherited from `UnitAttribute` in the bounded property. 

*  **Format:** The standard or custom format string to convert numbers, `DateTime` or `TimeSpan` to and from `string` (i.e.: g, dd/MM/yyyy, p...). Typically inherited from `FormatAttribute` in the data-bound property. 

* **EnumComboItems:** The elements to populate the `ComboBox` if `ValueLineType=Enum`. Typically the ones defined in the Enum Type. 

`ValueLine` has no representation in TypeScript. You'll need to manipulate the generated control manually using jQuery. 

## EntityBase (abstract)

`EntityBase` is the abstract base control for `EntityLine`, `EntityCombo`, `EntityDetail` and `EntityListBase`, and inherits from `LineBase`. It represents a control that can modify a reference to a `ModifiableEntity` or `Lite<T>`. 

`EntityBase` defines `Implementations` property for the inheritors:

```C#
public class EntityBase: LineBase
{
   public Implementations? Implementations;

   public string PartialViewName { get; set; }
}
```

* **Implementations:** The `Implementations` structure, mandatory for non-embedded entities, that indicates if the reference can contain entities of different types, adjusting the possible interactions by asking the user to decide the concrete type before `Create` or `Find`. Deduced from `ImplementedBy` and `ImplementedByAll` attributes in the bounded property. 

* **PartialViewName:** Shortcut to overrides the ViewName that will be used when creating or viewing entities from this control. 


Also there are properties to control witch buttons are visible. The default values depend on the type of the control, the type of entity or the permissions. 

```C#
public class EntityBase: LineBase
{
   public bool Create;
   public bool Find;
   public bool View;
   public bool Navigate;   
   public bool Remove;
}
```

As you see, there's no way in C# to handle the interaction manually. Instead there's just one pseudo-event called `AttachFunction`.

```C#
public class EntityBase: LineBase
{
    public JsFunction AttachFunction;
}
```

The `AttachFunction` property is a [JsFunction](../Facades/JsFunction.md) that you can set, providing a **TypeScript** function that will be called when the control is loaded. In this `TypeScript` function you'll have access to the `creating`, `viewing`, `finding`, etc..

### EntityBase in TypeScript

Let's take a look at EntityBase in `TypeScript`. 


```TypeScript
export class EntityBase {
    element: JQuery;
    options: EntityBaseOptions;    
}
```

* **element:** represents the jQuery element that contains the control. Typically a div with the control in his `SF-control`.
* **options:** a property of type `EntityBaseOptions` containing the intial options of the `EntityBase`, as generated from the HtmlHelpers, to customize the behaviour.  

### EntityBaseOptions

```TypeScript
export interface EntityBaseOptions {
    prefix: string;
    partialViewName: string;
    isReadonly: boolean;
    template?: string;
    templateToString?: string;

    autoCompleteUrl?: string;

    types: Entities.TypeInfo[]; 

    isEmbedded: boolean;
 
    rootType?: string;
    propertyRoute?: string;
}
```

* **prefix:** contains the name and if of the control itself. Every sub-control starts by prefix. 
* **partialViewName:** the ViewName that will be sent back to the server to customize the view that will be used when creating or viewing entities.
* **isReadonly:** information that will be sent back back to the server to make the view read-only when viewing the entity. 
* **template:** for embedded entities, the string template that will be used when creating new entities, without calling the server. 
* **templateToString:** the string that will be used when creating new entities (i.e.: In a `EntityList`)
* **autoCompleteUrl:** controller Url that will be, as expected by `AjaxEntityAutocompleter`. 
* **types:** represents the different implementations, if more than one. Each of the with:

    ```TypeScript
	export interface TypeInfo {
        name: string;
        niceName: string;
        creable?: boolean;
        findable?: boolean;
        preConstruct?: (extraJsonArgs?: FormObject) => Promise<any>;
    }	    
    ```
    * **name:** webTypeName of the type. Unique and non-localized
    * **niceName:** localized and spaced name of the type that will be displayed in the selector. 
    * **creable:** informs if the type is not creable, filtering the selector apropiately. 
    * **findable:** informs if the type is not findable, filtering the selector apropiately. 
    * **preConstruct:** method that will be used when constructing the entity. Provided by [ClientConstructor](../Facades/Constructor.m).  

Additionally,  some properties of `EntityBaseOptions` for `EmbeddedEntity` only:

```TypeScript
export interface EntityBaseOptions {
    isEmbedded: boolean;
 
    rootType?: string;
    propertyRoute?: string;
}
```


## EntityBase events

The typical usage of `EntityBase` class in TypeScript is to handle the events of the controls to customize the interaction.

In order to keep things clean, but allow complex work flows of interaction that call the server or even open pop-up, intensive use of [ES6 Promises](http://www.html5rocks.com/en/tutorials/es6/promises/?redirect_from_locale=de) is made. You need to understand promises and how to work with them in TypeScript in order to write client-side interactions. 

Those are the events provided by `EntityBase`. 

```TypeScript
export class EntityBase {
    removing: (prefix: string) => Promise<boolean>;
    creating: (prefix: string) => Promise<Entities.EntityValue>;
    finding: (prefix: string) => Promise<Entities.EntityValue>;
    viewing: (entityHtml: Entities.EntityHtml) => Promise<Entities.EntityValue>;
}
```

### Creating

The create (+) button lets the user instantiate a new entity. This button is only when `Entity` is `null`.

By default, clicking on create button does the following: 
1. Chooses the appropriate type (maybe asking the user using a `Navigator.typeChooser` if there are many `Implementations`) 
2. Instantiates the new entity using the selected type `preConstructor`. 
3. Shows the new entity to the user using `Navigator.viewPopup`. 

But you can handle `creating` event to customize this process, returning `null` to cancel changes or a `ModifiableEntity`/`Lite<T>` at the end (automatic conversion will be made) to confirm then. 

Example of using `creating` to initialize to customize the entity initialization: 

```C#
@Html.EntityCombo(pc, p => p.Category, ec =>
{
    ec.AttachFunction = SouthwindClient.ProductModule["attachCategory"](ec);
    ec.Create = Navigator.IsCreable(typeof(CategoryEntity), isSearch: true);
    ec.Remove = true;
})
```

```TypeScript
export function attachCategory(ec: Lines.EntityCombo)
{
    ec.creating = prefix =>
    {
        var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(ec.singleType(), null, true), lang.signum.newEntity);

        var productName = ec.prefix.parent().child("ProductName").get().val(); 

        var options = ec.defaultViewOptions(null);

        options.onPopupLoaded = div => prefix.child("CategoryName").get(div).val(productName); 

        return Navigator.viewPopup(newEntity, options);
    };
}
``` 


### Finding

The find (o-) button lets the user choose an already created entity from the database, and is visible only when `Entity` is `null`.

By default, clicking on find button chooses the appropriate type (maybe asking the user using a `Navigator.typeChooser` if there are many `Implementations`) 
and opens `SearchPopup` using `Finder.find`. 

But you can handle `finding` event to customize this process, returning `null` or a `ModifiableEntity`/`Lite<T>`. For example to open a `SearchControl` already filtered by some business criteria. 

Example of using `finding` to search using a custom `FindOptions`:

```C#
@Html.EntityLine(oc, o => o.Customer, el =>
{
    el.AttachFunction = SouthwindClient.OrderModule["attachCustomerEntityLine"](el, new FindOptions(typeof(CustomerEntity)).ToJS(el.Prefix));
})
```

```TypeScript
export function attachCustomerEntityLine(el: Lines.EntityLine, fo: Finder.FindOptions) {
    el.finding = (prefix) => Finder.find(fo);
}
``` 

### Viewing

The view (->) button lets the user open the current entity and, optionally, modify it. It is visible only when `Entity` is **not** `null`.

By default, clicking on view button just shows the entity using `Navigator.viewPopup`, but you can handle `viewing` event to customize this process, returning `null` to cancel changes or a `ModifiableEntity`/`Lite<T>` to confirm them.  

Example of using `viewing` to customize the view: 

```C#
@Html.EntityLine(oc, o => o.Customer, el =>
{
    el.AttachFunction = SouthwindClient.OrderModule["attachCustomerEntityLine"](el, 
     SouthwindClient.ViewPrefix.FormatWith("CustomerUI"));
})
```

```TypeScript
export function attachCustomerEntityLine(el: Lines.EntityLine, partialViewName: string) {
    el.viewing = entity => Navigator.viewPopup(entity, { partialViewName: partialViewName })
}
``` 

### Remove

The remove (x) button lets the user disassociate the current entity with the related one by setting the `Entity` property to `null`. It is visible only when `Entity` is **not** `null`. 

By default, clicking on navigate button removes the relationship setting the bounded property to `null`, but you can handle `removing` to stop this from happening.  

Example of using `removing` to ask the user for permission: 

```XML
@Html.EntityLine(oc, o => o.Customer, el =>
{
    el.AttachFunction = SouthwindClient.OrderModule["attachCustomerEntityLine"](el,
	   OrderMessage.AreYouSure.NiceToString());
})
```

```C#
export function attachCustomerEntityLine(el: Lines.EntityLine, message: string) {
    el.removing = prefix => confirm(message);
}
``` 

> Note that `Remove` will just disassociate the relationship, not delete the related entity from the database. If you want to delete the related entity from the database, consider doing so in the server side using `Save` or any other operation, or even using `EntityEvents`. 

###  EntityChange

Finally, the event `EntityChange` is defined in `EntityBase` control and fired every time the `Entity` property changes its value. 

```TypeScript
export class EntityBase {
    entityChanged: () => void;
}
```

`EntityChanged` will be fired independently of which button is pressed, or even if the property is changed programatically, in this case `userInteraction` parameter will be `false`. 

```C#
@Html.EntityLine(oc, o => o.Customer, el =>
{
    el.AttachFunction = SouthwindClient.OrderModule["attachCustomerEntityLine"](el);
})
```

```TypeScript
export function attachCustomerEntityLine(el: Lines.EntityLine, fo: Finder.FindOptions) {

    el.entityChanged = () =>
    {
        el.getOrRequestEntityHtml().then(e=> {

            var shipAddress = el.prefix.parent("Customer").child("ShipAddress"); 

            var copy = (part: string) =>
                shipAddress.child(part).get().val(e == null ? "" : e.getChild("Address_" + part).val());

            copy("Address");
            copy("City");
            copy("Region");
            copy("PostalCode");
            copy("Country");
        }); 
    };
}
``` 
      
## EntityLine

`EntityLine` is a `EntityBase` control that contains, in his right side, a placeholder for an entity. This placeholder is just a box with the four buttons (Create, Find, View and Remove) conveniently hidden depending if the entity is `null` or not.

`EntityLine` can be used to represent associations with embedded entities witch details are not important enough to take room in the parent entity control, but usually `EntityDetail` is more suited for embedded entities. 

### Autocomplete
Where `EntityLine` shines is representing relationships with high-populated entities (for witch a `ComboBox` wouldn't work) by using `Autocomplete`.


```C#
public partial class EntityLine : EntityBase
{ 
    public bool Autocomplete { get; set; }
    public string AutocompleteUrl { get; set; }
}
```

Just as `EntityLine`, `EntityStrip` degines 

When the `Entity` is `null`, the place holder of the `EntityLine` becomes a Bootstrap typeahead already configured to query the database. 

By default, `autoCompleter` is even able to find candidate entity by `ToString` using the default `ToString` of the entity,  or `Id`. You can use quotes to find ID-like values in the text, like: `'1`.

`autoCompleter` is also able to understand `Implementations`, querying multiple tables if necessary. 

In TypeScript, `EntityBase` has a field of type `EntityAutocompleter`.

```TypeScript
export class EntityBase {
    autoCompleter: EntityAutocompleter;
}
```

An `EntityAutocompleter` is nothing more than an object with a method `getResults` that, given a term,  returns the an list of `EntityValue` in the future. All the timeout handling and templating is already solved. 

```TypeScript
export interface EntityAutocompleter {
    getResults(term: string): Promise<Entities.EntityValue[]>;
}
```

`EntityAutocompleter` does not assumes that an AJAX request will be made, but since this is the default behavior, an `AjaxEntityAutocompleter` is also implemented. 

```C#
export class AjaxEntityAutocompleter implements EntityAutocompleter {

   controllerUrl: string;
   getData: (term: string) => FormObject;

   constructor(controllerUrl: string, getData: (term: string) => FormObject)
    
   getResults(term: string): Promise<Entities.EntityValue[]>(){ ... };
}
```

Example: 

```C#
@Html.EntityLine(e, f => f.Customer, el =>
{
    el.AutocompleteUrl = Url.Action("PersonAutocomplete", "Home");
})
```

```C#
[HttpPost]
public JsonNetResult PersonAutocomplete(string types, string q, int l)
{
    var result = Database.Query<PersonEntity>()
        .Where(p => !p.Corrupt)
        .Select(p => p.ToLite())
        .Autocomplete(q, l)
        .Select(o => new AutocompleteResult(o))
        .ToList();

    return this.JsonNet(result);
}
```

> **Note:** For languages with **accents** (like Spanish or French), you need to change the SQL Server Collation options at the column, database or server level. 

## EntityCombo

`EntityCombo` is used to represent entity properties when the expected range of possible entities to choose from is smaller and you want to show them all. 

### Data
In order show the entities of the combo, it has to be loaded. `EntityCombo` adds two members to control how and when this loading has to be done. 

```C#
public class EntityCombo : EntityBase
{
    public readonly RouteValueDictionary ComboHtmlProperties = new RouteValueDictionary();
    
    public IEnumerable<Lite<IEntity>> Data { get; set; }
    public int Size { get; set; }
}
```

* **ComboHtmlProperties:** Dictionary with the Html attributes that the `select` element will have. 

* **Data:** This collection lets you customize the entities that will be shown in the`EntityCombo`, by default all the visible `Lite<T>` of the different `Implementations` are retrieved from the database. Always with a faked new element. In order to change the available elements in the client side, manipulate the `options` elements directly. 

* **Size:** When greater than 0, sets the `size` attribute of the `select` element. Typically showing a list instead of a drop-down list. 

> `EntityCombo` hides by default `View`, `Remove`, `Find` and `Navigate`. 

```C#
@Html.EntityCombo(tc, s => s.Country, ec =>
{
    ec.Data = new List<Lite<CountryEntity>>
    {
      brazil, rusia, india, china
    }
})
```

## EntityDetail

`EntityDetail` is mainly used to represent embedded entities, since it embeds the entity control of the related entity inside of the parent control. 

Sill, `EntityDetail` provides a fieldset with the entity `ToString` as header, and the buttons to manipulate the reference. 

In order to change the view that will be used to as detail view, change the inherited `PartialViewName` property. 

Example:

```C# 
@Html.EntityDetail(oc, o => o.Customer, el => el.PartialViewName = SouthwindClient.ViewPrefix.FormatWith("CustomerUI"))
```

## EntityListBase (abstract)

The abstract class `EntityBaseList` inherits from `EntityBase` and is, at the same time, the base class for all the controls that represent and [`MList`](../../Signum.Entities/MList.md) of `Lite<T>` or `ModifiableEntity`, for example `EntityList`,  `EntityRepeater` and `EntityStrip`. 

```C#
public partial class EntityListBase : EntityBase
{ 
    public int? MaxElements { get; set; }
    public bool Move { get; set; }
}
```

* **MaxElements:** Limits the maximum number of elements that can be added to the collection.
 

### Move

Shows Up (↑) and Down (↓) buttons to re-order the entities in the list. 

By default this property is set by a [`Common`](../Common.md) task if the `MList` property has a `PreserveOrderAttribute`. 

### Finding

In TypeScript, EntityBase deprecates `finding` event and defines a new `findingMany` that let the user select many elements at the same time by returning a promise of array of `EntityValue`: 

```TypeScript
export class EntityListBase extends EntityBase {
    options: EntityListBaseOptions;
    finding: (prefix: string) => Promise<Entities.EntityValue>;  // DEPRECATED!
    findingMany: (prefix: string) => Promise<Entities.EntityValue[]>;
}
```


`EntityBase.Finding`, of type `Func<object>` get's now one extra feature: More than one entitis can be added by returning a `IEnumerable`. 

In fact, the default implementation is overridden in `EntityListBase` to use `Finder.findMany`, but you can write your own. 


```C#
@Html.EntityList(oc, o => o.Customers, el =>
{
    el.AttachFunction = SouthwindClient.OrderModule["attachCustomerEntityLine"](el, new FindOptions(typeof(CustomerEntity)).ToJS(el.Prefix));
})
```

```TypeScript
export function attachCustomerEntityLine(el: Lines.EntityLine, fo: Finder.FindOptions) {
    el.findinMany = (prefix) => Finder.findMany(fo);
}
``` 

## EntityList

`EntityList` is one of the controls provided by Signum Windows to represent lists on the user interface, using  a simple `ListBox` to visualize the results.

```C#
public partial class EntityList : EntityListBase
{ 
    public readonly RouteValueDictionary ListHtmlProps = new RouteValueDictionary();
}
```

* **ListHtmlProps:** The extra Html Attributes of the `select` element.  

Example, just add a `EntityList` without setting any property should do the job:

```C#
@Html.EntityList(ec, e => e.Territories)
``` 


### EntityListDetail

`EntityListDetail` is the an hybrid between a `EntityList`, that can contain a list of entities, and a `EntityDetail`, that lets you see the details of the refereed entity embedded in the parent view.

Using `EntityListDetail` you can create a master-detail view of a `MList<T>` property, and contains a `fieldset` with the buttons to create, find or remove entities in the list. 

In order to change the view that will be used to as detail view, change the inherited `PartialViewName` property.

```C#
public class EntityListDetail : EntityListBase
{
    public readonly RouteValueDictionary ListHtmlProps = new RouteValueDictionary();

    public string DetailDiv { get; set; }
}
```

* **ListHtmlProps:** The extra Html Attributes of the `select` element.  
* **DetailDiv:** The Id of the Div where the .  


```HTML
<div class="row">
    <div class="col-sm-6">@Html.EntityListDetail(e, f => f.Members, eld => { eld.DetailDiv = e.Compose("CurrentMember"); eld.FormGroupStyle = FormGroupStyle.None; })</div>
    <div class="col-sm-6" id="@e.Compose("CurrentMember")"></div>
</div>
```


### EntityRepeater

`EntityRepeater` control shows all the entities detail in the collection embedded in the parent entity control. Think of it as another mixture of `EntityList` and `EntityDetail` but in this one all the element details are shown at the same time.

`EntityRepeater` and contains a `fieldset` with the buttons to create (+), find (o-), and each detail view also has button to for view (->), remove (x), and move up (↑) and down (↓).

In order to change the view that will be used to as detail view, change the inherited `PartialViewName` property. 

Example: 

```XML
@Html.EntityRepeater(sc, o => o.Details)
```

### EntityTabRepeater

`EntityTabRepeater` is a variation of `EntityRepeater` that shows the different elements in tabs instead of vertically stacked. 

This makes sense when each element is too big to be stacked, for example the different localized messages of a EmailTemplate. 

Example: 

```XML
@Html.EntityTabRepeater(sc, o => o.Messages)
```

### EntityStrip

`EntityStrip` control shows all the entities as small pills that can be added using `Autocomplete`. Think of it as a control for choosing Tags. At the end the tag strip there are buttons to create (+) and find (o-) entities, but `Autocomplete` is the most usefull way to use it. 

Each element is just a small pill, but can optionally have his owns buttons for view (->), remove (x), and move up (↑) and down (↓).

Just as `EntityLine`, `EntityStrip` defined two properties to control `Autocomplete` functionality:

```C#
public partial class EntityStrip : EntityListBase
{ 
    public bool Autocomplete { get; set; }
    public string AutocompleteUrl { get; set; }

    public bool Vertical { get; set; }
}
```

Additionally, `EntityStrip` defines an `Vertical` property that changes the layout of the control: 

* **Vertical=false:** Optimized for a few number of small tag-like entities where the order doesn't matter. 
* **Vertical=true:** Optimized for a larger number of longer entities, or when the order does matter and move buttons are required.

Example: 

```XML
@Html.EntityStrip(sc, o => o.Territories)
```
    