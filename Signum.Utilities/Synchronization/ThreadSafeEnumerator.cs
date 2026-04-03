using System.Collections;
using System.Threading;

namespace Signum.Utilities.Synchronization;

public class TreadSafeEnumerator<T>: IEnumerable<T>, IEnumerator<T>
{
    Lock key = new ();
    IEnumerator<T> enumerator;
    
    volatile bool moveNext = true;

    readonly ThreadLocal<T> current = new ThreadLocal<T>();
 
    public TreadSafeEnumerator(IEnumerable<T> source)
    {
        enumerator = source.GetEnumerator(); 
    }

    public IEnumerator<T> GetEnumerator()
    {
        return this; 
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this; 
    }

    public T Current
    {
        get { return current.Value!; }
    }

    object? IEnumerator.Current
    {
        get { return current.Value; }
    }

    public bool MoveNext()
    {
        lock (key)
        {
            if (moveNext && (moveNext = enumerator.MoveNext()))
                current.Value = enumerator.Current;
            else
                current.Value = default!;

            return moveNext; 
        }
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}
