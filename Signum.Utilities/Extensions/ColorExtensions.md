# ColorExtensions


Provides extension method to work with Color structure in System.Drawing

### Interpolate

Interpolates linearly the A, R, G and B component of two colors.

```C#
public static Color Interpolate(this Color from, float ratio, Color to)
```

### ToHtml / TryToHtml

Returns a HTML hexadecimal string of the color. Alpha is ignored. 

```C#
public static string ToHtml(this Color color)
public static string TryToHtml(this Color? color)
public static string ToHtmlColor(int value)
```
### FromHsv

Creates a Color structure from the Hue, Saturation and Value components. 

```C#
public static Color FromHsv(double h, double S, double V)
```