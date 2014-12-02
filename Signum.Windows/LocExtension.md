# Localization

[DescriptionManager](../Signum.Utilities/DescriptionManager.md) provides the basic infrastructure to support localization of  `PropertyInfo`, `Enums`, `Symbols` and `Types` (singular and plural). 

In Signum.Web, there are some three `MarkupExtensions` to simplify integrating localized texts in your code. 

### LocExtension

`MarkupExtension` to evaluate `Enum.NiceToString` in XAML. 

```C#
public class LocExtension : MarkupExtension
{
    [ConstructorArgument("key")]
    public Enum Key { get; set; }

    public LocExtension(Enum key)
}
```

Example: 
```XML
<TextBlock Text="{m:Loc {x:Static d:SearchMessage.Filters}}" VerticalAlignment="Center" Margin="3,0,0,0"/>
```

### LocSymbolExtension

`MarkupExtension` to evaluate `Symbol.NiceToString` in XAML. 

```C#
public class LocSymbolExtension : MarkupExtension
{
    [ConstructorArgument("key")]
    public Symbol Key { get; set; }

    public LocSymbolExtension(Symbol key)
}
```

Example: 
```XML
<Button Content="{m:LocSymbol {x:Static d:OrderOperation.Save}}" />
```

### LocTypeExtension

`MarkupExtension` to evaluate `Type.NiceName` in XAML. 

```C#
public class LocTypeExtension : MarkupExtension
{
    [ConstructorArgument("type")]
    public Type Type { get; set; }

    public LocTypeExtension(Type type)
}
```

Example: 
```XML
<GridViewColumn Header="{m:LocType {x:Type dn:OperationSymbol}}" DisplayMemberBinding="{Binding Resource}" />
```

### LocTypePluralExtension

`MarkupExtension` to evaluate `Type.NicePluralName` in XAML. 

```C#
public class LocTypePluralExtension : MarkupExtension
{
    [ConstructorArgument("type")]
    public Type Type { get; set; }

    public LocTypePluralExtension(Type type)
}
```


Example: 
```XML
<MenuItem Header="{m:LocTypePlural {x:Type d:OrderEntity}}">...</MenuItem>
```