# ConsoleSwitch

This class simplifies the creations of menus in Console applications. It's meant to be used using collection initializer syntax.

```C#
public class ConsoleSwitch<K, V> : IEnumerable<KeyValuePair<string, WithDescription<V>>> where V : class
{
    public ConsoleSwitch()
    public ConsoleSwitch(string welcomeMessage)


    public void Add(string value)
    public void Add(K key, V value)   
    public void Add(K key, V value, string description)


    public V Choose()
    public V Choose(string endMessage)
    public V[] ChooseMultiple()
    public V[] ChooseMultiple(string endMessage)

    public WithDescription<V> ChooseTuple()
    public WithDescription<V> ChooseTuple(string endMessage)
    public WithDescription<V>[] ChooseMultipleWithDescription()
    public WithDescription<V>[] ChooseMultipleWithDescription(string endMessage)
}

public class WithDescription<T>
{
    public T Value { get; }
    public string Description { get; }

    public WithDescription(T value)
    public WithDescription(T value, string description)
}

```

Example: 

```C#
Action action = new ConsoleSwitch<string, Action>
{
    {"N", NewDatabase},
    {"S", Synchronize},
    {"L", Load},
}.Choose();
```