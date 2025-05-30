# Gruping

Out implementation of LINQ's `IGrouping<K,T>`, and the one that
uses Signum.Engine LINQ Provider.

It inherits from `List<T>` so if you feel like hacking with
undocumented APIs, you can downcast and add some elements more if you
want ;)

```C#
public class Grouping<K, T> : List<T>, IGrouping<K, T>
{
    public Grouping(K key)
    public Grouping(K key, IEnumerable<T> values)
    public K Key {get;}
    (...)
}
```