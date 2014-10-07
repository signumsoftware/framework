## DateTimePicker

`DateTimePicker` is a drop-down control with a `TextBox` for writing dates and a [WPF Calendar](http://msdn.microsoft.com/en-us/magazine/dd882520.aspx) inside of the `Popup`. It's useful for selecting a date, just like Calendar, but it takes much less space and allows you to write the date manually. 

Here are the most important properties in DateTimePicker class: 

```C#
public class DateTimePicker: Control
{ 
   public DateTime? SelectedDate;
   public bool IsReadOnly;   
   public bool IsEditable;
   public DateTimeConverter DateTimeConverter; 
}
```

* **SelectedDate:** The actual selected date. `null` if nothing is selected. Default is `null`.
* **IsReadOnly:** When `true`, the `SelectedDate` can't be changed from the user interface.
* **IsEditable:** Enables free text modification using the `TextBox`.
* **DateTimeConverter:** Get and set the current `DateTimeConverter`, a mixture of `IValueConverter` and `ValidationRule` to make the string to `DateTime` conversions and notify if it's not possible. Default is `DateTimeConverter.DateAndTime`. 

`DateTimeConverter` class contains two readonly static fields with `DateTimeConverter` instances already created:

* `DateOnly`: Uses "g" format for the conversion and validation.
* `DateAndTime`: Use "d" format for the conversion and validation. 

but feel free to write your own if necessary. 