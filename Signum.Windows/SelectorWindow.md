## SelectorWindow 

`SelectorWindow` shows a small modal windows with different options, each of them represented by a button that will select the option and close the windows at the same time. 

`SelectorWindows` is used by the framework to let the user choose between the different `Implementations` or the different `Constructor` if there are more than one registered, but you can use it in your own business work-flows to let the user chose an item among a small list of options. 

Just as `MessageBox`, `SelectorWindow` is meant to be used in one line of code, using `ShowDialog` generic method: 

```C#
public partial class SelectorWindow : Window
{
   public static bool ShowDialog<T>(IEnumerable<T> elements, out T selectedElement,
       Func<T, ImageSource> elementIcon = null,
       Func<T, string> elementText = null,
       string title = null,
       string message = null,
       bool autoSelectOnlyElement = true,
       Window owner = null)
}
```

* **elements:** The list of options to choose from. 
* **selectedElement:** The selected element if `ShowDialog` returns `true`, `default(T)` otherwise. 
* **elementIcon:** Optional function to give each option button an icon.
* **elementText:** Optional function to override the text in each option button. `ToString` used by default.
* **title:** The title use by the windows. The `NiceToString` of `SelectAnElement` used by default.
* **message:** The message that will be written above the option buttons. The `NiceToString` of `SelectAnElement` used by default.
* **autoSelectOnlyElement:** `true` to not even open the windows if there's just one option. `true` is default. 
* **owner:** To set the [Windows.Owner](http://msdn.microsoft.com/en-us/library/system.windows.window.owner(v=vs.110).aspx). 
 
