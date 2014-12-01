# ColorPicker

ColorPicker is just a drop-down control with a [HSV](http://en.wikipedia.org/wiki/HSL_and_HSV) Color picker that allows fast selection of colors. 

It contains three simple dependency properties: 

```C#
public class ColorPicker : Control
{
   public Color SelectedColor {...}
   public bool IsReadOnly {...}
   public bool IsEditable {...}
   
   (...)
}
```

* **SelectedColor:** The actual System.Windows.Media.Color that is selected.
* **IsReadOnly:** When true, the `SelectedColor` can't be changed from the user interface.
* **IsEditable:** Enables free text modification using the `TextBox`.


Example: 

```XML
  <Border Margin="10" Background="{Binding SelectedColor, ElementName=cp, Converter={x:Static m:Converters.ColorBrushConverter}}"/>
  <m:ColorPicker x:Name="cp" SelectedColor="Red" />
```
 