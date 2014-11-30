# Common class
Common static class defines the [Attached Properties](http://msdn.microsoft.com/en-us/library/ms749011.aspx) that, in conjunction with [Entity Controls](EntityControls.md), simplifies building UI code of business applications. 

Let's see what these properties are and why they are useful. 

## Routes

As you already know, [DataContext property](http://msdn.microsoft.com/en-us/library/system.windows.frameworkelement.datacontext.aspx) is a property of type object defined in the `FrameworkElement` class. It's meant to be used for [data binding](http://msdn.microsoft.com/en-us/library/ms752347.aspx) scenarios and is the default Binding Source. 

Unfortunately, the actual type of the object inside `DataContext` **is only known until run-time**, when a value is set. This is the reason there's no IntelliSense in Binding properties. 

In order to have smarter controls, they need to know the exact [PropertyRoute](../Signum.Entities/PropertyRoute.md) they are binding ahead of time, and from there they can deduce a lot of information, like the property type and implementations, the localized name, the format and unit, or even if the control has to be hidden or read-only for this particular user. 


### PropertyRoute, TypeContext and Route attached properties

Three attached properties are used to accomplish this: 

* `Common.PropertyRoute`: This attached property, of type `PropertyRoute` gives the necessary meta-data to any entity control. Just as `DataContext`, this property is inherited for child elements. Unfortunately, setting a value of type `PropertyRoute` is not particularly convenient, that's why we need the other two properties.  

* `Common.TypeContext`: This attached property, of type `System.Type`, is a short-cut for assigning the root type of the control. It just assigns `PropertyRoute.Root(type)` in the element's `Common.PropertyRoute` attached property. 

* `Common.Route`: This attached property, of type `string`, can be almost any expression that can be used as the `Path` int a `BindingExpression` but evaluates ahead of time to define the `Common.PropertyRoute` of an element by *continuing* the `Common.ProperyRoute` of a parent element. When this property is set, a sequence of extensible actions take place. 

Example: 

```XML
<UserControl x:Class="Southwind.Windows.Controls.Territory"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:m="clr-namespace:Signum.Windows;assembly=Signum.Windows"
    xmlns:d="clr-namespace:Southwind.Entities;assembly=Southwind.Entities"
    m:Common.TypeContext="{x:Type d:TerritoryEntity}"
    MinWidth="300">
    <StackPanel>
        <m:ValueLine m:Common.Route="Description" />
        <m:EntityCombo m:Common.Route="Region"  />
    </StackPanel>
</UserControl>
````

This is what this code does:

* Created a `UserControl` for `DataContext` of type `TerritoryEntity` by setting the `Common.TypeContext` property. 
* Add a `ValueLine` for the property `Description` of type string by setting `Common.Route` property.
* Add a `EntityCombo` for the property `Region` of type `RegionEntity` by setting `Common.Route` property.


### Route tasks

When `Route` property is set, a battery of extensible tasks are executed to configure each entity controls, for example: 

* **Update `Common.PropertyRoute` conveniently**.
* **Set `Type` property** when applied over any Entity Control (ValueLine, EntityLine, EntityCombo, EntityList or FileLine).
* **Create the Binding** arranging all the binding options correctly (Mode, NotifyOnValidationError, ValidatesOnExceptions...) using the right binding `Target` property depending of the control.
* **Set `Implementations` property** when applied over any EntityBase control (EntityLine/EntityCombo/EntityList/...).
* **Update `LabelText`** with the localized name of the property (`NiceName`).
* **Set `Unit` and `Format` properties** for value lines from the [property attributes](../Signum.Entities/PropertyAttributes.md). 
* **Set `Move` property** for `EntityListBase` controls from the `PreserveOrderAttribute`. 
* ...

So writing something like this: 

```XML
<StackPanel m:Common.TypeContext="{x:Type d:MyEntityEntity}">
    <m:EntityLine m:Common.Route="MyProperty"/>
</StackPanel>
```

Will expand to this pseudo-XAML:

```XML
<StackPanel m:Common.TypeContext="d:MyEntityEntity">
  <m:EntityLine 
     m:Common.Route="MyProperty" 
     m:Common.TypeContext="TypeSubContext MyProperty"
     EntityType="MyPropertyType"
     Entity="{Binding MyProperty, NotifyOnValidationError=true, ValidatesOnExceptions=true, ValidatesOnDataErrors=true}"
     Implementations="Server.FindImplementations(MyEntityEntity, MyProperty)"
     LabelText="MyProperty"/>
</StackPanel>
```

This battery of automatic actions for each control is what makes Signum.Windows so expressive, removing redundancy and rising the level of abstraction by inheriting more information from the entities. 

This sequence is also extensible, for example the authorization module registers himself to hide / make read-only each control depending of the permissions. 



## IsReadOnly attached property 

This attached property is **inherited** by all the sub tree, changing the behaviour of any Entity Control, disabling anything that can actually modify the entity. (Remove, Search and Create buttons, disable ComboBox, etc...) 

You can see more information for when each button is displayed in Entity Controls.

## LabelVisible attached property 
Many EntityControls (ValueLine, EntityLine, FileLine and EntityCombo) have a label at the left side. 

This label is visible by default and simplifies the layout for the very common scenario of label-control pair (you can write the whole pair in one line).

You can turn off the visibility of these labels just by coding `m:Common.IsLabelVisible="false"`.

This property is **inherited** so it affects the whole sub-tree.

## MinLabelWidth attached property
This property lets you control the minimum label width available for the labels on the left side. If one particular label is bigger, room will be made automatically, but this breaks the aligment with the previous/next controls. Changin `MinLabelWidth` we can restore this aligment. 

This property is **inherited** so it affects the whole sub-tree. 


## AutoHide attached property
The AutoHide system is designed to hide parts of a screen consitently, avoiding empty `TabItems` or `GroupBox`. 

`Common.AutoHide` attached property has possible values: 

* `Undefined`: Initial default state.
* `Visible`: Has some visible child.
* `Collapsed`: All the childrens are collapsed or undefined, no child is visible. 

When an user control with `AutoHide` `Collapsed` set is `Loaded` its visibility is automatically change to `Collapsed`. 

Before this, the child controls have to call one of this helper methods:

* `VoteVisible`: Marks all the parents as `AutoHide = Visible` no mater what.
* `VoteCollapsed`: Marks all the parents as `AutoHide = Collapsed`, till the firs one with `AutoHide == Visible` is found. 

`SearchControl` and `EntityLine` already call `VoteVisible` and `VoteCollapsed` when necessary, so you shouldn't need to thing about it


## Order attached property
This property is usually set in controls that are created automatically by the framework (like Operations creating `ToolBalButtons` or `MenuItems`) to control their order.  