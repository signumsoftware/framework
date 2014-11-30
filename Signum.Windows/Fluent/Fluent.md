# Fluent

Fluent are a set of static classes that contain extension method to make the creation and exploration of WPF objects simpler from C#. Think of it as a small subset of **jQuery for WPF**. 

## Fluent class

Contains extension methods that modify a `DependencyObject` and return the same object to continue manipulating it, creating a fluent API on top of WPF. 


### Set
Simple extensions method to set a dependency property and return the receiver object.  

```C#
public static T Set<T>(this T depObj, DependencyProperty prop, object value) 
	where T : DependencyObject
```

Useful for attached properties, example:

```C#
static MenuItem GetMenuItem(QuickLink ql)
{
    var mi = new MenuItem
    {
        Header = ql.Label,
        Icon = ql.Icon.ToSmallImage(),
    }
    .Set(AutomationProperties.NameProperty, ql.Name);
}
```

### Handle

Simple extension method to handle a `RoutedEvent` returning the caller method. 

```C#
public static T Handle<T>(this T uiElement, RoutedEvent routedEvent, RoutedEventHandler handler) 
	where T : UIElement
```

### Bind
Set of extension methods to simplify creating bindings from C#, instead of using `BindingOperations.SetBinding` directly. The simple overload takes the whole `BindingBase`:  

```C#
public static T Bind<T>(this T bindable, DependencyProperty property, BindingBase binding) 
	where T : DependencyObject
```

But there are overloads that take the `sourcePath` (as an `string` or as an `Expression`) from the `DataContext` or an explicit `source` object. 

```C#
public static T Bind<T>(this T bindable, DependencyProperty property, string sourcePath) 
	where T : DependencyObject
public static T Bind<T, S>(this T bindable, DependencyProperty property, Expression<Func<S, object>> sourcePath)
	where T : DependencyObject
public static T Bind<T>(this T bindable, DependencyProperty property, object source, string sourcePath) 
	where T : DependencyObject
public static T Bind<T, S>(this T bindable, DependencyProperty property, S source, Expression<Func<S, object>> sourcePath) 
	where T : DependencyObject
```

The same overloads also exist with an optional `IValueConverter`. 

```C#
public static T Bind<T>(this T bindable, DependencyProperty property, string sourcePath, IValueConverter converter)
	where T : DependencyObject
public static T Bind<T, S>(this T bindable, DependencyProperty property, Expression<Func<S, object>> sourcePath, IValueConverter converter) 
	where T : DependencyObject
public static T Bind<T>(this T bindable, DependencyProperty property, object source, string sourcePath, IValueConverter converter) 
	where T : DependencyObject
public static T Bind<T, S>(this T bindable, DependencyProperty property, S source, Expression<Func<S, object>> sourcePath, IValueConverter converter) 
	where T : DependencyObject
```

Example:


```C#
static MenuItem GetMenuItem(QuickLink ql)
{
    var mi = new MenuItem
    {
        DataContext = ql,
    }
    .Bind(MenuItem.HeaderProperty, "Label");
}
```

### Visibility

This three methods let you modify `Visibility` property of any `UIElement`.  

```C#
public static T Hide<T>(this T uiElement) where T : UIElement //Visibility.Hidden
public static T Collapse<T>(this T uiElement) where T : UIElement //Visibility.Collapsed
public static T Visible<T>(this T uiElement) where T : UIElement //Visibility.Visible
```

### ReadOnly

This tho methods let you modify `Common.SetIsReadOnly` property of any `UIElement`.  

```C#
public static T ReadOnly<T>(this T uiElement) where T : UIElement //ReadOnly = true
public static T Editable<T>(this T uiElement) where T : UIElement //ReadOnyl = false
```

### After and Before

This tho methods let you modify add a `FrameworkElement` after of before any other element which parent is a `Panel`.  

```C#
public static void After(this FrameworkElement element, FrameworkElement newElement)
public static void Before(this FrameworkElement element, FrameworkElement newElement)
```

### IsSet/NotSet
This methods let you determine if a `DependencyProperty` has been set manually (`BaseValueSource.Local`) or not. 

```C#
public static bool IsSet(this DependencyObject depObj, DependencyProperty prop)
public static bool NotSet(this DependencyObject depObj, DependencyProperty prop)
```

### FromVisibility/ToVisibility
This two methods let you convert `bool` to `Visibility`, assuming `true` is `Visible` and `false` is `Collapsed`. 

```C#
public static Visibility ToVisibility(this bool val)
public static bool FromVisibility(this Visibility val)
```

### FormatSpan 

Similar to [`String.Format`](http://msdn.microsoft.com/en-us/library/system.string.format(v=vs.110).aspx) but replaces each indexed component of the format string with the corresponding item in `params Inlines[]` argument, returning an `Span` as a result. Useful for inserting links or colored spans in localizable messages. 

```C#
public static Span FormatSpan(this string format, params Inline[] inlines)
```

### AddTab
Simplifies adding a new `TabItem` to an existing `TabControl`.

```C#
public static TabControl AddTab(this TabControl tabControl, string header, FrameworkElement content)
```

### GetDataTemplate and GetFrameworkElementFactory

This two methods radically simplify creating a `FrameworkElementFactory` or `DataTemplate` programatically by translating an `Expression<Func<FrameworkElement>>` to a `FrameworkElementFactory`. 

```C#
public static FrameworkElementFactory GetFrameworkElementFactory(Expression<Func<FrameworkElement>> constructor)
public static DataTemplate GetDataTemplate(Expression<Func<FrameworkElement>> constructor)
```

Example: 
```
Binding b = ...;
DataTemplate d = Fluent.GetDataTemplate(() => new TextBlock().Bind(TextBlock.TextProperty, b)); 
```

### OnDataContextPropertyChanged and OnEntityPropertyChanged

This two methods simplify the process of getting `PropertyChanged` events from the properties of the entity in the `DataContext` (for `OnDataContextPropertyChanged`) or an `EntityBase` (for `OnEntityPropertyChanged`).

```C#
public static void OnDataContextPropertyChanged(this FrameworkElement fe, PropertyChangedEventHandler propertyChanged)
public static void OnEntityPropertyChanged(this EntityBase eb, PropertyChangedEventHandler propertyChanged)
```

Example: 

```C#
public partial class Album : UserControl
{
    public Album()
    {
        InitializeComponent();
        this.OnDataContextPropertyChanged((sender, args) =>
        {
            var album = (AlbumEntity)sender;

            if (args.PropertyName == "Name" && album.Name == null)
                album.Songs.Clear(); 
        }); 
    }
}
```

## WhereExtensions

### Child

This group of overloads let you explore the logical or visual tree looking for an element that satisfies a condition, returning only the first result if found, r throwing and `InvalidOperationException` if not. 

You can find the element just by `System.Type`, or also by `Common.Route` or a arbitrary `predicate`. 

```C#
public static T Child<T>(this DependencyObject parent)
    where T : DependencyObject
public static T Child<T>(this DependencyObject parent, string route)
    where T : DependencyObject
public static T Child<T>(this DependencyObject parent, Func<T, bool> predicate)
    where T : DependencyObject
````  

Also, a set of overloads let you control the `WhereFlags` to tune the search settings. 

```C#
public static T Child<T>(this DependencyObject parent, WhereFlags flags)
    where T : DependencyObject
public static T Child<T>(this DependencyObject parent, string route, WhereFlags flags)
    where T : DependencyObject
public static T Child<T>(this DependencyObject parent, Func<T, bool> predicate, WhereFlags flags)
    where T : DependencyObject


public enum WhereFlags
{
    Default = NonRecursive | BreathFirst | LogicalTree | StartOnChildren,
    NonRecursive = 0,
    Recursive = 1,
    BreathFirst = 0,
    DepthFirst = 2,
    LogicalTree = 0,
    VisualTree = 4,
    StartOnChildren = 0,
    StartOnParent = 8
}
```
### Children

Similarly, `Children` returns all the found elements, or an empty `IEnumerable<T>` if not found. The `IEnumerable<T>` is generated on-demand using an iterator. 


```C#
public static IEnumerable<T> Children<T>(this DependencyObject parent)
    where T : DependencyObject
public static IEnumerable<T> Children<T>(this DependencyObject parent, string route)
    where T : DependencyObject
public static IEnumerable<T> Children<T>(this DependencyObject parent, Func<T, bool> predicate)
    where T : DependencyObject
````  

Also, a set of overloads let you control the `WhereFlags` to tune the search settings. 

```C#
public static IEnumerable<T> Children<T>(this DependencyObject parent, WhereFlags flags)
    where T : DependencyObject
public static IEnumerable<T> Children<T>(this DependencyObject parent, string route, WhereFlags flags)
    where T : DependencyObject
public static IEnumerable<T> Children<T>(this DependencyObject parent, Func<T, bool> predicate, WhereFlags flags)
    where T : DependencyObject
```


### VisualParents and LogicalParents

Returns the chain of parents in each WPF tree: 

```C#
public static IEnumerable<DependencyObject> VisualParents(this DependencyObject child)
public static IEnumerable<DependencyObject> LogicalParents(this DependencyObject child)
```

### BreathFirstVisual, BreathFirstLogical, DepthFirstVisual and DepthFirstLogical

This four methods explores in each WPF tree using a breath-first or depth-first strategy to find the `DependencyObjects` that satisfy a `pridicate`. 

```C#
public static IEnumerable<DependencyObject> BreathFirstVisual(DependencyObject parent, bool startOnParent, bool recursive, Func<DependencyObject, bool> predicate)
public static IEnumerable<DependencyObject> BreathFirstLogical(DependencyObject parent, bool startOnParent, bool recursive, Func<DependencyObject, bool> predicate)
public static IEnumerable<DependencyObject> DepthFirstVisual(DependencyObject parent, bool startOnParent, bool recursive, Func<DependencyObject, bool> predicate)
public static IEnumerable<DependencyObject> DepthFirstLogical(DependencyObject parent, bool startOnParent, bool recursive, Func<DependencyObject, bool> predicate)
```
* `startOnParent` parameter controls if the parent element should be a candidate or not. 
* `recursive` parameter controls if children or returned elements can also be returned. 

## TreeExtensions

Small class to find the `ContainerFromItem` and `ItemFromContiner` recursively in a [hierarchially databound TreeView](http://blogs.msdn.com/b/chkoenig/archive/2008/05/24/hierarchical-databinding-in-wpf.aspx).

```C#
public static TreeViewItem ContainerFromItem(this TreeView treeView, object item)
public static object ItemFromContainer(this TreeView treeView, TreeViewItem container)
```

