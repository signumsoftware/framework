using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace Signum.Utilities;

public interface IResetLazy
{
    void Reset();
    void Load();
    Type DeclaringType { get; }
    ResetLazyStats Stats();
}

public class ResetLazyStats
{
    public ResetLazyStats(Type type)
    {
        this.Type = type;
    }

    public Type Type;
    public int Loads;
    public int Invalidations;
    public int Hits;
    public TimeSpan SumLoadTime;
}


[ComVisible(false)]
public class ResetLazy<T>: IResetLazy
{
    class Box
    {
        public Box(T value)
        {
            this.Value = value;
        }

        public readonly T Value;
    }

    public ResetLazy(Func<T> valueFactory, LazyThreadSafetyMode mode = LazyThreadSafetyMode.PublicationOnly, Type? declaringType = null)
    {
        if (valueFactory == null)
            throw new ArgumentNullException(nameof(valueFactory));

        this.mode = mode;
        this.valueFactory = valueFactory;
        this.declaringType = declaringType ?? valueFactory.Method.DeclaringType!;
    }

    LazyThreadSafetyMode mode; 
    Func<T> valueFactory;

    public int Loads;
    public int Hits;
    public int Invalidations;
    public TimeSpan SumLoadtime;
    public DateTimeOffset? LoadedOn; 

    object syncLock = new();

    Box? box;

    readonly Type declaringType; 
    public Type DeclaringType
    {
        get { return declaringType; }
    }

    public T Value
    {
        get
        {
            var b1 = this.box;
            if (b1 != null)
            {
                Interlocked.Increment(ref Hits);
                return b1.Value;
            }

            if (mode == LazyThreadSafetyMode.ExecutionAndPublication)
            {
                lock (syncLock)
                {
                    var b2 = box;
                    if (b2 != null)
                        return b2.Value;

                    this.box = new Box(InternalLoaded());

                    return box.Value;
                }
            }

            else if (mode == LazyThreadSafetyMode.PublicationOnly)
            {
                var newValue = InternalLoaded(); 

                lock (syncLock)
                {
                    var b2 = box;
                    if (b2 != null)
                        return b2.Value;

                    this.box = new Box(newValue);

                    return box.Value;
                }
            }
            else
            {
                var b = new Box(InternalLoaded());
                this.box = b;
                return b.Value;
            }
        }
    }

    T InternalLoaded()
    {
        Stopwatch sw = Stopwatch.StartNew();
        var result = valueFactory();
        LoadedOn = DateTimeOffset.UtcNow;
        sw.Stop();
        this.SumLoadtime += sw.Elapsed;
        Interlocked.Increment(ref Loads);
        return result;
    } 


    public void Load()
    {
        var a = Value;
    }
   
    public bool IsValueCreated
    {
        get { return box != null; }
    }

    public void Reset()
    {
        if (mode != LazyThreadSafetyMode.None)
        {
            lock (syncLock)
            {
                this.box = null;
                this.LoadedOn = null;
            }
        }
        else
        {
            this.box = null;
            this.LoadedOn = null;
        }

        Interlocked.Increment(ref Invalidations);
        OnReset?.Invoke(this, null!);
    }

    ResetLazyStats IResetLazy.Stats()
    {
        return new ResetLazyStats(typeof(T))
        {
            SumLoadTime = this.SumLoadtime,
            Hits = this.Hits,
            Loads = this.Loads,
            Invalidations = this.Invalidations,
        };
    }

    public event EventHandler? OnReset; 
}
