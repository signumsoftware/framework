## ValueLineBox 

`ValueLineBox` shows a small modal windows with a [ValueLine](EntityControls/EntityControls.md) to choose a simple value during a client work-flow.  

Just as `MessageBox`, `ValueLineBox` is meant to be used in one line of code, using `Show` generic method, or `ShowUntyped`: 

```C#
public partial class SelectorWindow : Window
{
   public static bool Show<T>(ref T value, string title = null, string text = null, 
       string labelText = null, string format = null, string unitText = null, 
       Window owner = null)

   public static bool ShowUntyped(Type type, ref object value, string title = null, string text = null, 
       string labelText = null, string format = null, string unitText = null, 
       Window owner = null)
}
```

* **type or `T`:** Determines the type of the underlying `ValueLine`.   
* **value:** This `ref` parameter should contain the default value at the beginning, and will contain the selected value if `Show` returned `true`. 
* **title:** Optional title for the windows. `ChooseAValue.NiceToString()` is the default. 
* **text:** Optional title for the windows. `PleaseChooseAValueToContinue.NiceToString()` is the default.
* **labelText:** Optional label for the value, if null label is not visible. 
* **format:** Optional format (i.e.: `g`)
* **unitText:** Optional unit (i.e:  `€`)
* **owner:** To set the [Windows.Owner](http://msdn.microsoft.com/en-us/library/system.windows.window.owner(v=vs.110).aspx). 