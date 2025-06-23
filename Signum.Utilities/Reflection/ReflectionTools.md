# ReflectionTools class
ReflectionTools class contains some handy methods for some reflection scenarios, like [strong typing reflection](http://weblogs.asp.net/cazzu/archive/2006/07/06/Linq-beyond-queries_3A00_-strong_2D00_typed-reflection_2100_.aspx), or improving reflection performance through [dynamic methods](http://jachman.wordpress.com/2006/08/22/2000-faster-using-dynamic-method-calls/).   

### FieldEquals, PropertyEquals, MethodEquals and MemberEquals
Comparing if two System.Type objects are the same is as easy as using == operator. Unfortunately, that doesn't apply to any other object inheriting from `MemberInfo` (`FieldInfo`, `PropertyInfo` or `MethodInfo`). In order to compare them we use this methods: 

```C#
public static bool FieldEquals(FieldInfo f1, FieldInfo f2)
public static bool PropertyEquals(PropertyInfo p1, PropertyInfo p2)
public static bool MethodEqual(MethodInfo m1, MethodInfo m2)
public static bool MemeberEquals(MemberInfo m1, MemberInfo m2)
```

Internally, they use the algorithm explained [here](http://blogs.msdn.com/kingces/). 

### Strong Typed Reflection
Reflection is all about adding dynamic features and introspection to .Net, but since System.Type has the cool [typeof operator](http://msdn.microsoft.com/en-us/library/58918ffs(VS.80).aspx) that allows strong-typed retrieval of `System.Type` objects, why should we rely on error-prone strings to retrieve other reflection entities, like `FieldInfo`, `PropertyInfo`, etc..?

Strongly Typed Reflection uses [Expression Trees(http://msdn.microsoft.com/en-us/library/bb397951.aspx) to find the `MemberInfo` you are interested about. All you have to do is write a lambda witch root access (or invokes) the Field, Property or you are interested in.

This way you have IntelliSense and refactoring over your Reflection constants. 

{s:SF2|In the New version, some overloading have changed getting rid of object in favor of another generic parameter for performance reasons (now is extensively used in every entity property). In order to use the overloads that have two generic parameters is convenient to specify the argument in the parameter definition and let the compiler infer the return type.  }

```C#
public static PropertyInfo GetPropertyInfo<R>(Expression<Func<R>> property)
public static PropertyInfo GetPropertyInfo<T, R>(Expression<Func<T, R>> property)
public static PropertyInfo BasePropertyInfo(LambdaExpression property)

public static FieldInfo GetFieldInfo<R>(Expression<Func<R>> field)
public static FieldInfo GetFieldInfo<T,R>(Expression<Func<T, R>> field)
public static FieldInfo BaseFieldInfo(LambdaExpression field)

public static MemberInfo GetMemberInfo<R>(Expression<Func<R>> member)
public static MemberInfo GetMemberInfo<T, R>(Expression<Func<T, R>> member)
public static MemberInfo BaseMemberInfo(LambdaExpression member)

public static MethodInfo GetMethodInfo(Expression<Action> method)
public static MethodInfo GetMethodInfo<R>(Expression<Func<R>> method)
public static MethodInfo GetMethodInfo<T, R>(Expression<Func<T, R>> method)
public static MethodInfo BaseMethodInfo(LambdaExpression method)
```

Example: 

```C#
//Retrieving an static (readonly) field from string class
FieldInfo fi = ReflectionTools.GetFieldInfo(() => String.Empty);

//Retrieving an instance property info
PropertyInfo pi = ReflectionTools.GetPropertyInfo((string s) => s.Length);

//Choosing between different overloads of the static methods Abs 
MethodInfo mi1 = ReflectionTools.GetMethodInfo((decimal d) => Math.Abs(d));
MethodInfo mi2 = ReflectionTools.GetMethodInfo((double d) => Math.Abs(d));
```

As you see, as long as the end (the root) of the expression tree you have the right field, property, or method, it will work fine. 


### DynamicMethod builders

Reflection is great. It makes huge code reductions. But it's famous for being slow, and this fame is well deserved. According to [this article](http://msdn.microsoft.com/en-us/magazine/cc163759.aspx), there are important performance differences when calling a Method and, presumably, the same could be said for Fields and Properties.

DynamicMethods, however, let you __compile__ a small function dynamically generated, returning a delegate. This way you can cache the delegate somewhere and not pay the performance penalty when doing this operation, like assigning a particular field in a particular type of objects, or calling a particular method. 

ReflectionTools have some methods to build instance property and fields getters and setters, and constructors:

```C#
//Build a DynamicMethod like: (T obj)=>obj.m
public static Func<T, P> CreateGetter<T, P>(MemberInfo m)
//Build a DynamicMethod like: (T obj)=>(object)obj.m
public static Func<T, object> CreateGetter<T>(MemberInfo m)
//Builds a DynamicMethod like: (object obj)=>(object)(('type')obj).m 
public static Func<object, object> CreateGetterUntyped(Type type, MemberInfo m)

//Build a DynamicMethod like:  (T obj, P value)=>obj.m = value
public static Action<T, P> CreateSetter<T, P>(MemberInfo m)
//Build a DynamicMethod like:  (T obj, object value)=>obj.m = (P)value
public static Action<T, object> CreateSetter<T>(MemberInfo m)
//Build a DynamicMethod like:  (object obj, object value)=>(('type')obj).m = (P)value
public static Action<object, object> CreateSetterUntyped(Type type, MemberInfo m)

```

Example: 

```C#
//retrieving PropertyInfo with strong typed reflection 
//PropertyInfo piLength = ReflectionTools.GetPropertyInfo((string s) => s.Length);
//retrieving PropertyInfo old-style
PropertyInfo piLength = typeof(string).GetProperty("Length");

//Compiling a delegate to get the property. Paying performance penalty once.
Func<string, object> getLenght = ReflectionTools.CreateGetter<string>(piLength);

string[] cultureNames = CultureInfo.GetCultures(CultureTypes.NeutralCultures).Select(c => c.EnglishName).ToArray();

foreach (var name in cultureNames)
{
     //using the getter many times with no overhead 
    Console.WriteLine("{0} -> {1}",  getLenght(name), name); 
}
//Writes:
//6 -> Arabic
//9 -> Bulgarian
//7 -> Catalan
//20 -> Chinese (Simplified)
//5 -> Czech
//6 -> Danish
//6 -> German
//5 -> Greek
//7 -> English
//7 -> Spanish
//7 -> Finnish
//6 -> French
//6 -> Hebrew
//9 -> Hungarian
//9 -> Icelandic
//7 -> Italian
//8 -> Japanese
//6 -> Korean
//5 -> Dutch
//9 -> Norwegian
//6 -> Polish
//10 -> Portuguese
//...
```

### Parse
Tries to Parse the string in an object of the given type. Works with Enums and Nullables.

```C#
public static T Parse<T>(string value)
public static object Parse(string value, Type type)
public static T Parse<T>(string value, CultureInfo culture) 
public static object Parse(string value, Type type, CultureInfo culture)

public static bool TryParse<T>(string value, out T result)
public static bool TryParse(string value, Type type, out object result)
public static bool TryParse<T>(string value,  CultureInfo ci, out T result)
public static bool TryParse(string value, Type type, CultureInfo ci, out object result)
```

Example:

```C#
ReflectionTools.Parse<double>("1.0"); //returns 1.0
ReflectionTools.Parse("1.0", typeof(double)); //return 1.0 boxed
```

### ChangeType

Tries to convert an object to another type. Works with Enums and Nullables.

```C#
public static T ChangeType<T>(object value)
public static object ChangeType(object value, Type type)
```

Example:

```C#
ReflectionTools.Convert<DayOfWeek>(0); //returns Sunday
ReflectionTools.Convert(0, typeof(DayOfWeek)); //return Sunday 
```