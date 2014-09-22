# JsFunction

In order to make AJAX-enabled interactive web applications it's necessary to write some client side `JavaScript` code. 

We use `TypeScript` to improve the development experience, and [external modules](http://www.typescriptlang.org/Handbook#modules-going-external) and [Require.js](http://requirejs.org/) to have more flexibility to include the scripts dynamically in any Razor View, HtmlHelper or even at run-time depending some conditions.

> **Note:** We strongly recommend you to learn about TypeScript, external modules and Require.js before continue reading. 

In order to load an external module and call a function, you'll need to write something like this: 

```Javascript
require(["moduleName"], function(mod) { mod.functionName(arguments...); }
```

Unfortunately, writing something like this in C# is cumbersome and hard to maintain:

```C#
//In your cshtml
<script>
    @MvcHtmlString.Create(@"require([""Path/To/Your/Script""], function(Finder) 
{ Finder.myFunction(" + JsonConvert.SerializeObject(new { hola = "Hola" }) + "); })")
</script>
```

* You're continuously mixing C# and Javascript code.
* If you're in a razor view, you also need to call `MvcHtmlString.Create` to avoid HTML encode. 
* Frequently, the arguments of the function need to be serialized to Javascript using [Json.Net](http://james.newtonking.com/json). 
* The name or path of the module is 'stringly-typed' so renaming the file will break all the references. 


## JsFunction class
Using `JsFunction` you can write this instead: 

In your Client class
```C#
public static readonly JsModule Module = new JsModule("Path/To/Your/Script"); 
```

In your cshtml
```HTML
<script>
    @MyModuleClient.Module["myFunction"](new { text = "This is a message" })
</script>
```

`JsFunction` class is a simple class like this: 

```C#
public sealed class JsFunction : IHtmlString
{
     public JsModule Module { get; set; }
     public string FunctionName { get; set; }
     public object[] Arguments { get; set; }
     public JsonSerializerSettings JsonSerializerSettings { get; set; }

     internal JsFunction(JsModule module, string functionName, params object[] arguments)
     
     public override string ToString()
     public string ToHtmlString()
}
```

The class is an `IHtmlString`, so it's not necessary to encode it. It already contains:
* The `Module`, representing the JavaScript/TypeScript file.
* The `FunctionName` to invoke in this module.
* The `Arguments` that will be passed to the function, as .Net object that will be serialized using Json.Net.
* An optional `JsonSerializerSettings` to override the serialization options. 

Since all the arguments have to be serialized, in order to pass Javascript literals, like a function name or a complex expression [`JRaw`](http://james.newtonking.com/json/help/html/SerializeRawJson.htm) has to be used. 

In order to pass some special JavaScript tokens, like `this` or `event`,  use `JsFunction.This` and `JsFunction.Event` respectively instead.

```C#
public sealed class JsFunction
{
    public static object This = new object();
    public static object Event = new object();
}
```

## JsModule class

Finally, notice how `JsFunction` construct is internal. Instead they are usually created using the indexer in `JsModule`. 


```C#
public class JsModule
{
    public string Name {get;  private set;}

    public JsModule(string name)
    
    public override string ToString()

    public JsFunctionConstructor this[string functionName]
    {
        get { return args => new JsFunction(this, functionName, args); }
    }
}

public delegate JsFunction JsFunctionConstructor(params object[] args);  
```

Using this pattern, we can create a `JsFunction` as if we were invoking a dynamic function, splitting the three parts clearly: 

* **Module name:** The modules name/path is specified once and stored as a global static variable in the Client class. More than one module variable can be created if there are multiple `.ts` files. This way we have auto-completion and on single places to rename. 

* **Function name:** The function name is passed as an independent parameter using the indexer, keeping the illusion that the module is just a dictionary of in-vocable JavaScript functions.

* **Arguments:** The arguments are passed by invoking the `JsFunctionConstructor` delegate. The exact same number arguments and in the same order will be passed to your function.     

In your Client class:
```C#
public static readonly JsModule Module = new JsModule("Path/To/Your/Script"); 
```
In your cshtml:
```HTML
<script>
    @MyModuleClient.Module["myFunction"](new { text = "Blasco blasco" })
</script>
```

In your TypeScript file: 
```TypeScript
export function myFunction(text : string)
{
   ..
}
```

Finally, note that `JsFuncton` is not just a helper class for you to use, many C# API like [`AttachFunction`](../EntityControls/EntityControls.md), [`PreConstruct`](Constructor.md), or [OperationSettings.Click](../Operations/OperationClient.md) only accept a `JsFunction`, effectively becoming a delegate/invokation to the world of TypeScript from C#.

 


 


