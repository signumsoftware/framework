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
        public DateTimeOffset LoadedOn = DateTimeOffset.UtcNow; 
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

    Lock syncLock = new();

    bool loading;
    Box? box;

    readonly Type declaringType; 
    public Type DeclaringType
    {
        get { return declaringType; }
    }

    public T Value => GetBox().Value;

    public T GetValue(out DateTimeOffset loadedOn)
    {
        var box = GetBox();
        loadedOn = box.LoadedOn;
        return box.Value;
    }

    private Box GetBox()
    {
        var b1 = this.box;
        if (b1 != null)
        {
            Interlocked.Increment(ref Hits);
            return b1;
        }

        if (mode == LazyThreadSafetyMode.ExecutionAndPublication)
        {
            lock (syncLock)
            {
                var b2 = box;
                if (b2 != null)
                    return b2;

                if (this.loading == true)
                    throw new InvalidOperationException("Cyclic dependency detected loading " + this.GetType());

                this.loading = true;
                try
                {
                    this.box = new Box(InternalLoaded());
                }
                finally
                {
                    this.loading = false;
                }

                return box;
            }
        }

        else if (mode == LazyThreadSafetyMode.PublicationOnly)
        {
            var newValue = InternalLoaded();

            lock (syncLock)
            {
                return box ??= new Box(newValue);
            }
        }
        else
        {
            var b = new Box(InternalLoaded());
            this.box = b;
            return b;
        }
    }

    T InternalLoaded()
    {
        Stopwatch sw = Stopwatch.StartNew();
        var result = valueFactory();
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
                this.loading = false;
            }
        }
        else
        {
            this.box = null;
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
