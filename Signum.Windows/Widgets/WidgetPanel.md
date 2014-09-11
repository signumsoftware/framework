# WidgetPanel

`WidgetPanel` control is the control that is typically at the right side of the entity. It contains a place for widgets that can be registered for any entity. 

`WidgetPanel` is not meant to provide direct access to add Widgets imperatively, instead you have to register a function in the `GetWidgets` and is **static**: 

```C#
public partial class WidgetPanel : UserControl
{
    public static event GetWidgetDelegate GetWidgets; 
}
```

The event will provide you the current entity and control, and you can return a control that implements `IWidget` or `null`.

```C#
public delegate IWidget GetWidgetDelegate(ModifiableEntity entity, Control mainControl); 
```

An `IWidget` is jut any control that exposes an optional `ForceShow` event that, when raised, will make the `WidgetPanel` visible if it was collapsed. 

```C#
public interface IWidget
{
    event Action ForceShow;
}
```

Example:

```C#
WidgetPanel.GetWidgets += (obj, mainControl) => new LinksWidget() { Control = mainControl };
```

