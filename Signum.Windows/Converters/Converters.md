# Converters

## Introduction
Binding infrastructure provided by WPF is a great step forward compared with the available in WinForms. Thanks to the expressiveness provided by Xaml and, specifically [Markup Extensions](http://msdn.microsoft.com/en-us/library/cc189022(vs.95).aspx), now we have flexibility choosing the binding Mode and Source.

When a converter is needed, however, the expressiveness of Markup Extensions reach their limit and all the bloat code appears. In order to bind a color property to the background of a Border element (a Brush) you need to: 

1.- Write a class that implements `IValueConverter` (2 methods to implement):

```C#
public class BushConverter : IValueConverter
{
     public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
     {
         return new SolidColorBrush((Color)value);
     }

     public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
     {
         throw new NotImplementedException();
     }
}
```

2.- Probably add the namespace at the top of your Xaml file:
```XML
  <Window
   (...)
   xmlns:w="clr-namespace:Bugs.Windows" />
```

3.- Add an instance of the converter to the Windows Resources:
```XML
    <Window.Resources>
        <w:BushConverter x:Key="brushConverter"/>
    </Window.Resources>
```
4.- Actually use the converter in your binding using StaticResource:
```XML
  <Border .... Background="{Binding SelectedColor, ElementName=cp, Converter={StaticResource brushConverter}}" />
```

## ConverterFactory

Our small step forward in this problem is using an small set of general purpose converters and relying on lambdas to do the actual conversion. This way you need to create a new converter object, not a new class, every time you need a converter: 

1. Create a new converter object using `ConverterFactory` and store then in a `public static readonly` field in your own converter repository class.

```C#
public static class MyConverters
{
     (...)
     public static readonly IValueConverter ColorBrushConverter = ConverterFactory.New(
        (Color color) => new SolidColorBrush(color)); 
     (...)
}
```
2.- Probably add the namespace at the top of your Xaml file:

```XML
  <Window
   (...)
   xmlns:w="clr-namespace:Bugs.Windows" />
```

3.- Actually use the converter in your binding using `x:Static`: 

```XML
 <Border .... Background="{Binding SelectedColor, ElementName=cp, Converter={x:Static w:MyConverters.ColorBrushConverter}}" />
```

> Notice how, by explicitly specifying `Color` as the first parameter in the lambda, we are helping type inference algorithm to succeed. This way we save ourselves from writing the type parameters explicitly: `ConverterFactory.New<Color,SolidColorBrush>(...)`

Let's take a look at the actual method provided by `ConverterFactory`: 

```C#
public static class ConverterFactory
{
    //Converter from Source to Target
    public static Converter<S, T> New<S, T>(Func<S, T> convert)

    //Converter from 2 Sources to Target
    public static DualConvertet<S1, S2, T> New<S1, S2, T>(Func<S1, S2, T> convert)

    //Converter from Many Sources (of the same type) to a Target
    public static MultiConverter<S, T> NewMulti<S, T>(Func<S[], T> convert)

    //Converter from Source to Target and from Target to Source
    public static Converter<S, T> New<S, T>(Func<S, T> convert, Func<T, S> convertBack)

       //Converter from Source to Target and from Target to Source validating the Source. 
    public static ConverterValidator<S, T> New<S, T>(Func<S, T> convert, Func<T, S> convertBack, Func<S, string> validator)
}


public class Converter<S, T> : IValueConverter
{
    internal Func<S, T> convert;
    internal Func<T, S> convertBack;
    (...)
}


public class DualConvertet<S1, S2, T> : IMultiValueConverter
{
    internal Func<S1, S2, T> convert;
    (...)
}


public class MultiConverter<S, T> : IMultiValueConverter
{
    internal Func<S[], T> convert;
    (...)
}

public class ConverterValidator<S, T> : ValidationRule, IValueConverter
{
    internal Func<S, T> convert;
    internal Func<T, S> convertBack;
    internal Func<S, string> validator; 
    (...)
}
```

This technique removes a lot of the bloat code needed to implement the converter, and also centralizes all your converters in the same repository class, making it easier to maintain and reuse them.


## Converters class

Converters static class contains a bunch of general purposes and specific converters used in the framework that you can also take advantange of, just to name a few: 

* `object` -> `bool`
 * **IsNull**:  implemented as `o == null`.
 * **IsNotNull**: implemented as `o != null`
* `bool` -> `Visibility`
 * **BoolToVisibility**:  implemented as ` b ? Visibility.Visible : Visibility.Collapsed`.
 * **NotBoolToVisibility**: implemented as ` b ? Visibility.Collapsed : Visibility.Visible`.
* `object` -> `Visibility`
 * **NullToVisibility**:  implemented as ` b != null ? Visibility.Visible : Visibility.Collapsed`.
 * **NotNullToVisibility**: implemented as ` b != null ? Visibility.Collapsed : Visibility.Visible`.
* `int` -> `Visibility` 
 * **ZeroToVisibility**: implemented as` count == 0 ? Visibility.Visible : Visibility.Collapsed`.
 * **NotZeroToVisibility**: implemented as ` count == 0 ? Visibility.Collapsed : Visibility.Visible`.
* `IIdentifiable` -> `Lite<IIdentifiable>` 
 * **ToLite**: implemented using `ToLite`.
* `Lite<IIdentifiable>` -> `IIdentifiable`
 * **Retrieve**: implemented using `Retrieve`.