
# SafeConsole

Small class that contains some tools for `Console`. 

### SyncKey

Just an object that you can use as lock to write consecutive blocks of text in the console in multi-threaded console applications.  

```C#
public static readonly object SyncKey = new object();
```

### WriteSameLine & ClearSameLine

Writes in the console using the same like than the previous call to `WriteSameLine`. Useful for progress indicators in long running console processes. 

```C#
public static void WriteSameLine(string str)
public static void WriteSameLine(string format, params object[] parameters)
public static void ClearSameLine()        
```


### WriteColor & WriteLineColor

Writes a line in the console using the specified color. The method saves and restores the current color.

```C#
public static void WriteColor(ConsoleColor color, string str)
public static void WriteColor(ConsoleColor color, string format, params object[] parameters) 

public static void WriteLineColor(ConsoleColor color, string format, params object[] parameters)
public static void WriteLineColor(ConsoleColor color, string str)   
```

### Ask

One-liners for asking the user for information in console applications: 

```C#
//Asks the user a yes/no question
public static bool Ask(string question) 
//Asks the user a yes/no question that can be remembered by writting !
public static bool Ask(ref bool? rememberedAnswer, string question) 

//Asks the user a string question
public static string Ask(string question, params string[] answers)
//Asks the user a string question that can be remembered by writting !
public static string Ask(ref string rememberedAnswer, string question, params string[] answers)   
```

### AskSwitch

Asks the user to choose one of the different values with a ConsoleSwitch format: 

```C#
//Asks the user a yes/no question
public static string AskSwitch(string question, List<string> options)
```

### WaitRows, WaitQuery, WaitExecute

This series of methods are usefull to give some 'working in progresss' indicator for long-runnning operations that do not provide progress information. Tipically database operations like long queries and updates. 

The method counts the number of seconds since the operations was triggered off. The counter runs in a different thread and your code runs in the current one. 

```C#
//Basic variation, just the spinning counter
public static void WaitExecute(Action action)  

//Also writes starting text to give some hint of the current task being done
public static void WaitExecute(string startingText, Action action) 

//Writes the text in yellor and returns the falue returned in the function, usefull for long running queries
public static T WaitQuery<T>(string startingText, Func<T> query)

//Writes the text in gray and the number of affected rows when the function is finished. Usefull for long UnsafeInsert/UnsafeDelete/UnsafeUpdate
public static void WaitRows(string startingText, Func<int> updateOrDelete)
```

Example

```C#
SafeConsole.WaitRows("Removing all exceptions", ()=>Database.Query<ExceptonEntity>().UnsafeDelete());
```



