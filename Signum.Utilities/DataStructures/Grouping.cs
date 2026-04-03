using System.Diagnostics;
using System.Collections;

namespace Signum.Utilities.DataStructures;

[DebuggerDisplay("Key = {Key}  Count = {Count}")]
[DebuggerTypeProxy(typeof(Proxy))]
public class Grouping<K, T> : List<T>, IGrouping<K, T>
{
    K key;
    public Grouping(K key)
    {
        this.key = key;
    }

    public Grouping(K key, IEnumerable<T> values)
    {
        this.key = key;
        this.AddRange(values);
    }

    public K Key
    {
        get { return this.key; }
    }
}

internal class Proxy
{
    public object? Key;
    public ArrayList List;

    public Proxy(IList bla)
    {
        List = new ArrayList(bla);
        PropertyInfo pi = bla.GetType().GetProperty("Key")!;
        Key = pi.GetValue(bla, null);
    }
}
