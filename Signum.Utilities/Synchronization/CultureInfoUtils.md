# CultureInfoUtils

Contains helper methods to change the culture info in a region of code: 

```C#
public static IDisposable ChangeCulture(string cultureName)
public static IDisposable ChangeCulture(CultureInfo ci)

public static IDisposable ChangeCultureUI(string cultureName)
public static IDisposable ChangeCultureUI(CultureInfo ci)

public static IDisposable ChangeBothCultures(string cultureName)
public static IDisposable ChangeBothCultures(CultureInfo ci)
```