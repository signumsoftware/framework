# NumericTextBox 

A `NumericTextBox` extends `TextBox` to allow easy manipulation of numeric values. 

Internally it just uses a Converter to convert string to decimal and the other way around. So it doesn't force you to write only numeric characters (allowing Copy-Paste), but will be boxed with a red square if the input is not a valid number. 


```C#
public class NumericTextBox : TextBox
{
    public decimal? Value 
    public decimal LargeIncrement  
    public decimal SmallIncrement 
    public NullableDecimalConverter NullableDecimalConverter 
    public event RoutedPropertyChangedEventHandler<decimal?> ValueChanged
}
```

* **Value:** Returns the `TextBox` text converted to `decimal` if possible, `null` otherwise.
* **LargeIncrement:** Fixes the amount of increment when Shift+Up/Shift+Down keys are pressed. Default value is 10m.
* **SmallIncrement:** Fixes the amount of increment when Up/Down keys are pressed. Default value is 1.0m.
* **NullableDecimalConverter:** Get and set the current `NullableDecimalConverter`, a mixture of `IValueConverter` and `ValidationRule` to make the string to decimal conversions and notify if it's not possible errors. Default is `NullableDecimalConverter.Number`. 

`NullableDecimalConverter` class contains two read-only static fields with `NullableDecimalConverter` instances already created, but you can create your own:

* Number: Uses "N0" format for the conversion.
* Integer: Uses "N2" format for the conversion.
