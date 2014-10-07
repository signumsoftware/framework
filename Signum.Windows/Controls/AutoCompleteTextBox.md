# AutoCompleteTextBox

`AutoCompleteTextBox` is a `TextBox` with a `ListBox` in a `Popup` that appears when you write text, filling the list from an external source of possible values. 

By double-clicking on an element in the Pop-up (or pressing Enter) the element becomes the `SelecteItem`.

### Properties

```C#
public partial class AutoCompleteTextBox : UserControl
{
   public object SelectedItem;
   public bool AllowFreeText;

   
   public int MinTypedCharacters;
   public TimeSpan Delay;

   public DataTemplate ItemTemplate;
   public DataTemplateSelector ItemTemplateSelector; 
}
```

* **SelectedItem:** The item that the user selected the last time the pop-up was opened, or the text that is written if `AllowFreeText == true`, otherwise `false`. 
* **AllowFreeText**: Controls if the written string or `null` should be placed in `SelectedItem` if no matching element was found. Default is `false`. 
* **MinTypedCharacters**: Controls the minimum number of character to start requesting elements using  `Autocompleting`. Default 1. 
* **Delay:** Minimum amount of time after the last keystroke, in milliseconds, to start requesting elements using `Autocompleting`. Default 300 ms. 
* **ItemTemplate**: The `ItemTemplate` that will be used in the `ListBox` inside the `Popup`.
* **ItemTemplateSelector**: The `ItemTemplateSelector` that will be used in the `ListBox` inside the `Popup`.      

### Events

```C#
public partial class AutoCompleteTextBox : UserControl
{
   public event ClosedEventHandler Closed;
   public event Func<string, CancellationToken, IEnumerable> Autocompleting;
}

public class CloseEventArgs : RoutedEventArgs
{
    public CloseReason Reason { get; }
    public bool IsCommit { get; }
}

public enum CloseReason
{
    ClickList,
    Enter,
    Tab,
    TabExit,
    Esc,
    LostFocus,
    ClickOut
}
```  

The most important even is the mandatory `Autocompleting` that is called in a **background thred** and provides the `string` term to look for and a `CancellationToken` that can be used to cancel any pending request that won't be necessary. 

Additionally, `Closed` events gives detailed information of the reason the popup was `Closed` and if this action commited a change in the `SelectedItem` or not. 

### Example:

```XML
<m:AutoCompleteTextBox 
    x:Name="tbPath"
    Autocompleting="AutoCompleteTextBox_AutoCompleting" />
```

```C#
private IEnumerable AutoCompleteTextBox_AutoCompleting(string arg, CancellationToken token)
{
    if (string.IsNullOrEmpty(arg))
        return null;

    string dir = System.IO.Path.GetDirectoryName(arg);

    if (string.IsNullOrEmpty(dir))
        return null;

    string file = System.IO.Path.GetFileName(arg);
    DirectoryInfo di = new DirectoryInfo(dir);

    var directories = di.GetDirectories(file + "*").Select(a => a.FullName);
    var files = di.GetFiles(file + "*.*").Select(a => a.FullName);

    return directories.Concat(files).ToArray();
}
```