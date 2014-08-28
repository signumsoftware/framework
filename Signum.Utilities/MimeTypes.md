# MimeTypes 

This class simplifies converting mime types to extensions and the other way around. Useful for Web and Mailing.

It contains a pre-initialized caches and also access the Windows registry for unknown values. 


```C#
public static class MimeType
{
    public static string FromFileName(string fileName)
    public static string FromExtension(string extension)

    public static string GetDefaultExtension(string mimeType)
}
```