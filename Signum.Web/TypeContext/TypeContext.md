# TypeContext

`TypeContext` class is used to give contextual meta-data to any piece of Html that requires it. 

`TypeContext` are required in order to create [Entity Controls](../LineHelpers/EntityControls.md), and passed around as the `model` of any custom view for an entity. 


## Data and Metadata
The three more important pieces of information that a TypeContext carries are:

* **Value:** The current value or entity associated to a piece of Html. Think of it like `DataContext` in WPF. 
* **Prefix:** A string containing a sequence of properties (or collection indexes) separated by underscore (`_`). This information is necessary to ensure that the Id of the Html elements (an the name of the input elements) is unique if the same control is rendered twice. 
* **PropertyRoute:** A [PropertyRoute](../../Signum.Entities/PropertyRoute.md) is a sequence of `PropertyInfo` (or `FieldInfo`) that uniquely identifies a database column. This meta-data information is required to deduce Implementations or hide a control depending of the permissions.      


## Inherited Style Information
Additionally, `TypeContext` is used pass style information that should be inherited. New sub-context can be created from parent context, inheriting their properties but allowing you to override some of them in the child. This way is easier to maintain visual consistency. For example: 

### FormGroupStyle

Each [EntityControl](../LineHelpers/EntityControl.md) contains, as well as the control itself, the label with the localized name of the property. 

This is implemented in HTML using Bootstrap's `form-control` class, if you look at the [Bootstrap's documentation](http://getbootstrap.com/css/#forms) there are different patterns available.

The property `FormGroupStyle` controls witch pattern will be used:
 
* `None`: Just the value control will be written, with no label.  
* `Basic`: Creates a `form-control` as in *"Basic example"* in Bootstrap documentation. 
* `BasicDown`: Creates a `form-control` as in *"Basic example"* in Bootstrap documentation, but writing the label below instead of above the control. 
* `SrOnly`:  Creates a `form-control` as in *"Inline form"* in Bootstrap documentation. 
* `LabelColumns`: Creates a `form-control` as in *"Horizontal form"* in Bootstrap documentation. **Default value.**
   
As noted in Bootstrap documentation, for the `form-control` to look correctly, not only is necessary to write the correct `form-control` pattern, also it has to be wrapped by a `form-inline` container (in *"Inline form"*) or `form-horizontal` container (in *"Horizontal form"*). 
 
The situation gets even more complicated because, in order to make `LabelColumns` the default behavior, we have added `form-horizontal` in *NormalPage.cshtml* and *PopupControl.cshtml*, the razor views that wrap your custom entity view. 

Then, if you need to restore the default state to use `Basic` and `BasicDown` a new container with class `form-vertical` (defined in by us) is required. This should be effectively the same as *removing* `form-horizontal` in this container. 

### FormGroupSize

Bootstrap default form-controls are way too large. They are optimized for simple log-in and sign-in controls, not for data-intensive applications. `FormGroupSize` property let's you control the general size of this controls (`margin`, `padding`, `min-width`, `min-height`, etc..). There are three options.

*  **Normal:** A typical control is 34px height. Useful for controls with just a few fields. Way too large for our typical user case. Adds our own `form-md` class in the `form-control` element but is not used. 
*  **Small:** A typical control is 26px height. **Default behavior**. Useful for most of the custom views. Implemented by adding `form-sm` in the `from-control`.
*  **ExtaSmall:** A typical control is 20px height. Used in the FilterBuilder of a SearchControl. Implemented by adding `form-xs` in the `from-control`.

### BsColumns
 
The default value of `FormGroupStyle`, `LabelColumns` is implemented using [Bootstrap grid](http://getbootstrap.com/css/#grid), so the label takes some columns (i.e: `col-sm-2`) and the value the remaining columns (i.e.: `col-sm-10`). 

The property `LabelColumns` determines how wide is the label, while `ValueColumns` does the same for the value control. When `LabelColumns` is set, `ValueColumns` is automatically set to his complementary (12-x). 

Both properties are of type `BsColumn`:

```C#
public class BsColumn
{
    public readonly short? xs;
    public readonly short? sm;
    public readonly short? md;
    public readonly short? lg;

    public BsColumn(short sm)
    public BsColumn(short? xs, short? sm, short? md, short? lg)
}
```

The recommended behavior for simple cases is to layout the page using only `sm`, so tablets, computers and  large screens will follow your layout, while smart phones will just stack vertically, but you can use the more complex overload of `BsColumn` to give your responsive design more breakpoints. 

### ReadOnly

One important feature of `TypeContext` is the ability to make the whole page, po-pup or parts of them read-only just by setting the `ReadOnly` flag at the right `TypeContext` level. 

This is used by the authorization system or to disable windows for entities with [`EntityKind.System`](../../Signum.Entities/EntityKind.md).

### ViewOverrides

`ViewOverrides` are also transmitted to the View, from the [ `EntitySettings`](../Facades/Navigator.md) using `TypeContext`.


## Class Hierarchy

Now that we know the main responsibilities of the `TypeContext` system as a whole, let's focus on the hierarchy of classes that makes it possible: 

* **Context class**: It only contains the reference to the `Parent`, the  `Prefix` property, and the style information (`FormGroupStyle`, `FormGroupSize`, `PlaceholderLabels`, `LabelColumns`, `ValueColumns` and `ReadOnly`. Is an `IDisposable` to use them in `using` statements, but `Dispose` method is empty. 

	* **TypeContext class:** Abstract base class that already should contain a `UntypedValue`, `Type`, `PropertyRoute` and `ViewOverrides`. 

		* **TypeContext\<T> class:** Implements `TypeContext` but is strongly typed. It can be constructed passing just the `Value` and  and `PropertyRoute`, or also the `PropertyRoute` and the optional parent `TypeContext`. 
        
         ```C#
         public class TypeContext<T> : TypeContext
         {    
             public TypeContext(T value, string prefix)
             public TypeContext(T value, TypeContext parent, string prefix, PropertyRoute propertyRoute)
         }
         ```

         In order to creat sub-context using `SubContext` method is usually more convinient. A `SubContext` can be created just to set different style to a piece of Html code or the same entity, or to create a stronly typed context for a sub-entity. 

         ```C#
         public class TypeContext<T> : TypeContext
         {
             public TypeContext<T> SubContext()
             public TypeContext<S> SubContext<S>(Expression<Func<T, S>> property)
         }
         ```

	        * **TypeElementContext<T> class:** Inherits from `TypeContext<T>` but is used for creating context for each element in a collaction. 
	
	         ```C#
	         public class TypeElementContext<T> : TypeContext<T>
	         {
	              public int Index { get; private set; }
	              public PrimaryKey? RowId { get; private set; }
	             
	             public TypeElementContext(T value, TypeContext parent, int index, PrimaryKey? rowId)
	         }
	         ```
	
	         Is usually created by using `TypeContext<T>.TypeElementContext`: 
	
	         ```C#
	         public class TypeContext<T> : TypeContext
	         {
	             public IEnumerable<TypeElementContext<S>> TypeElementContext<S>(Expression<Func<T, MList<S>>> property)
	         }
	         ```

     * **LineBase class:** Finally `LineBase` class, the base class for any [EntityControl](../LineHelpers/ EntityControl.md), also inherits from `TypeContext`, getting from there the current `Value` and `PropertyRoute`, and letting you change the style information for one particular line easily.    	 
