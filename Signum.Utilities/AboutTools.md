# AboutTools
This class contains three handy static methods to retrieve the information you usually put in your application 'About ...' window: 

### NiceWindowsVersion

Returns the commercial name of the windows installed in the current machine (Windows 95, NT 4.0, XP, 2003 Server, Vista)

```C#
public static string NiceWindowsVersion(this OperatingSystem os)
```

Example: 

```C#
Console.WriteLine(Environment.OSVersion.NiceWindowsVersion());
//Writes: Windows 8
```

### CompilationTime
Returns the DateTime of the moment that an assembly was compiled. 

```C#
public static DateTime CompilationTime(this Version v)
```

In order to enable this functionality, you have to use a * symbol in sour `AssemblyVersionAttribute` (usually in AssemblyInfo.cs).

```C#
[assembly: AssemblyVersion("1.0.*")] 
```

Otherwise it will return 31/12/1999 (first they of the .Net religion? :P)

Example: 

```C#
Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version.CompilationTime());
//Writes: 08/02/2009 18:20:36
//(yeah, I'm working on a Sunday)
```

### FrameworkVersions
Returns a list of all the .Net FrameworkVersions installed in the current machine in the shape of NetFrameworkVersion objects., by looking at the Registry. 


```C#
public static List<NetFrameworkVersion> FrameworkVersions()

public class NetFrameworkVersion
{
    public string GlobalVersion { get; }
    public string FullVersion { get; }
    public int? ServicePack { get; }

    public override string ToString()
    {
        return GlobalVersion + (ServicePack != null ? " SP" + ServicePack : ""); 
    }
}
```

Example: 

```C#
AboutTools.FrameworkVersions().ToConsole();
//Writes in my machine:
//v2.0.50727 SP2
//v3.0 SP2
//v3.5 SP1
```