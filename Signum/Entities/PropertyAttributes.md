# Other Property Attributes

### FormatAttribute
Applied to a property of numeric type or DateTime, indicates the format that should be used by the UI to show the property value. This information could be used by ValueLine as well as included in the format of the tables of a SearchWindow's result panel.

```C#

[AttributeUsage(AttributeTargets.Property)]
public class FormatAttribute : Attribute
{
    public string Format { get;}
    public FormatAttribute(string format)
}

````

Example: 

```C#
double ratio;
[Format("0.0000")]
public double Ratio 
{
  get {...}
  set {...}
}
```

Four decimals will be used to represent the ratio whenever is shown to the user.

### TimeSpanDateFormatAttribute

Only for properties of type TimeSpan that represent hour of the day (instead of Time interval), allows to set a DateTime format string.


### UnitAttribute
Applied to a property of a (usually) numeric type indicates the unit symbol of the measure. This information could be used by ValueLine as well as included in the format of the tables of a SearchWindow's result panel. 

```C#
[AttributeUsage(AttributeTargets.Property)]
public class UnitAttribute : Attribute
{
    public string UnitName { get; }
    public UnitAttribute(string unitName)

}
````

Example: 

```C#
int amount;
[Unit("€")]
public int Amount 
{
  get {...}
  set {...}
}
```
Then in the User interface a readonly € symbol will appear at the right side of the ValueLines and SearchControl results.

### HiddenPropertyAttribute

Applied to a property, prevent it from being localized, validated, authorized, and in general available to the UI. 


### QueryablePropertyAttribute
Applied to a property, makes it available or hidden for queries (otherwise, depends if the property has a field with the same name or an `PropertyExpression`). 


## Descriptions
In order to change the default names of properties, look at [DescriptionManager](...\Signum.Utilities\DescriptionManager.md)