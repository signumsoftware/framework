# Polymorphic\<T>

`Polymorphic<T> class` can be seen as a `Dictionary<Type, T>` that understands inheritance and interface implementation. 

On of the main usages of this class is to implement what we call **external polymorphism**: The ability of simulating polymorphic dispatch without declaring methods **inside** of the entities that conform the hierarchy as `virtual`/`override` . 

In Signum Framework is common that we write the entities in a separated assembly (`Signum.Entities`). This assembly should not have a reference to the database (`Signum.Engine`) because could be running in a remote client (`Signum.Windows`). 

It's is common that we need polymorphic dispatch **and** access to the database, in this case `Polymorphic<T>` is useful. 

```C#
public class Polymorphic<T> where T : class
{
     public Polymorphic(PolymorphicMerger<T> merger = null, Type minimumType = null)

     public T GetValue(Type type)
     public T TryGetValue(Type type)

     public T GetDefinition(Type type)
     public void SetDefinition(Type type, T value)

     public void ClearCache()

     public IEnumerable<Type> OverridenTypes { get; }
}
```

Use(less) example: 

```C#
Polymorphic<string> polLegs = new Polymorphic<string>(minimumType: typeof(Animal), merger: PolymorphicMerger.Inheritance);

pol.SetDefinition(typeof(Mammal), "Four");
pol.SetDefinition(typeof(Human), "Two");
pol.SetDefinition(typeof(Dolphin), "None");
pol.SetDefinition(typeof(Spider), "Eight");


pol.GetValue(typeof(Dog)); //returns "Four";
```

In order to construct a `Polymorphic<T>`, we need to provide:
* A `minimumType` that should be a parent for any parameter `type`.
* A `PolymorphicMerger<T>`, the default is `Inheritance`.


### PolymorphicMerger\<T>

`PolymorphicMerger<T>` is just a delegate that defines the inheritance (or interface implementation) behavior that the `Polymorphic<T>` should have: 

```C#
public delegate T PolymorphicMerger<T>(
	KeyValuePair<Type, T> currentValue, 
	KeyValuePair<Type, T> baseValue, 
	List<KeyValuePair<Type, T>> newInterfacesValues) where T : class;
```

There are already generic method available with the most common ones, you can use them as an enum: 

```C#
public static class PolymorphicMerger
{
    Inheritance,
    InheritanceAndInterfaces, 

    InheritDictionary,
    InheritDictionaryInterfaces
}
```

### PolymorphicExtensions

While you could create a `Polymorphic<string>` (a dictionary that will return a different string for each type understanding inheritance), the most common scenario is that `T` is a delegate type. 

For this reason, extensions exist that simplify registering and invoking `Action` and `Func`. 


```C#
public static class PolymorphicExtensions
{
    public static void Register<T, S>(this Polymorphic<Action<T>> polymorphic, Action<S> action) where S : T
    public static void Register<T, S, P0>(this Polymorphic<Action<T, P0>> polymorphic, Action<S, P0> action) where S : T
    public static void Register<T, S, P0, P1>(this Polymorphic<Action<T, P0, P1>> polymorphic, Action<S, P0, P1> action) where S : T
    public static void Register<T, S, P0, P1, P2>(this Polymorphic<Action<T, P0, P1, P2>> polymorphic, Action<S, P0, P1, P2> action) where S : T   
    public static void Register<T, S, P0, P1, P2, P3>(this Polymorphic<Action<T, P0, P1, P2, P3>> polymorphic, Action<S, P0, P1, P2, P3> action) where S : T

    public static void Register<T, S, R>(this Polymorphic<Func<T, R>> polymorphic, Func<S, R> func) where S : T
    public static void Register<T, S, P0, R>(this Polymorphic<Func<T, P0, R>> polymorphic, Func<S, P0, R> func) where S : T
    public static void Register<T, S, P0, P1, R>(this Polymorphic<Func<T, P0, P1, R>> polymorphic, Func<S, P0, P1, R> func) where S : T
    public static void Register<T, S, P0, P1, P2, R>(this Polymorphic<Func<T, P0, P1, P2, R>> polymorphic, Func<S, P0, P1, P2, R> func) where S : T
    public static void Register<T, S, P0, P1, P2, P3, R>(this Polymorphic<Func<T, P0, P1, P2, P3, R>> polymorphic, Func<S, P0, P1, P2, P3, R> func) where S : T


    public static void Invoke<T>(this Polymorphic<Action<T>> polymorphic, T instance)
    public static void Invoke<T, P0>(this Polymorphic<Action<T, P0>> polymorphic, T instance, P0 p0)
    public static void Invoke<T, P0, P1>(this Polymorphic<Action<T, P0, P1>> polymorphic, T instance, P0 p0, P1 p1)
    public static void Invoke<T, P0, P1, P2>(this Polymorphic<Action<T, P0, P1, P2>> polymorphic, T instance, P0 p0, P1 p1, P2 p2)
    public static void Invoke<T, P0, P1, P2, P3>(this Polymorphic<Action<T, P0, P1, P2, P3>> polymorphic, T instance, P0 p0, P1 p1, P2 p2, P3 p3)

    public static R Invoke<T, R>(this Polymorphic<Func<T, R>> polymorphic, T instance)
    public static R Invoke<T, P0, R>(this Polymorphic<Func<T, P0, R>> polymorphic, T instance, P0 p0)
    public static R Invoke<T, P0, P1, R>(this Polymorphic<Func<T, P0, P1, R>> polymorphic, T instance, P0 p0, P1 p1)
    public static R Invoke<T, P0, P1, P2, R>(this Polymorphic<Func<T, P0, P1, P2, R>> polymorphic, T instance, P0 p0, P1 p1, P2 p2)
    public static R Invoke<T, P0, P1, P2, P3, R>(this Polymorphic<Func<T, P0, P1, P2, P3, R>> polymorphic, T instance, P0 p0, P1 p1, P2 p2, P3 p3)
}
```

Use(full) example: 

```C#
Polymorphic<Action<Animal>> polEat = new Polymorphic<Action<Animal>>();

polEat.Register((Dog d)=> EatWhiskas(d));
polEat.Register((Dolphin d)=> EatFish(d));
polEat.Register((Human h)=> EatHamburger(h));

pol.Invoke(new GermanShepherd()); //executes EatWhiskas;
```