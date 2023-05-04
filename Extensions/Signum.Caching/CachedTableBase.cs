using Signum.Utilities.Reflection;
using System.Data;
using Signum.Engine.Sync;
using Microsoft.Data.SqlClient;

namespace Signum.Cache;

public abstract class CachedTableBase
{
    public abstract ITable Table { get; }

    public abstract IColumn? ParentColumn { get; set; }

    internal List<CachedTableBase>? subTables;
    public List<CachedTableBase>? SubTables { get { return subTables; } }
    protected SqlPreCommandSimple query = null!;
    internal ICacheLogicController controller;
    internal CachedTableConstructor Constructor = null!;

    internal CachedTableBase(ICacheLogicController controller)
    {
        this.controller = controller;
    }

    protected void OnChange(object sender, SqlNotificationEventArgs args)
    {
        try
        {
            if (args.Info == SqlNotificationInfo.Invalid &&
                args.Source == SqlNotificationSource.Statement &&
                args.Type == SqlNotificationType.Subscribe)
                throw new InvalidOperationException("Invalid query for SqlDependency") { Data = { { "query", query.PlainSql() } } };

            if (args.Info == SqlNotificationInfo.PreviousFire)
                throw new InvalidOperationException("The same transaction that loaded the data is invalidating it! Table: {0} SubTables: {1} ".
                    FormatWith(Table, subTables?.Select(e=>e.Table).ToString(","))) { Data = { { "query", query.PlainSql() } } };

            if (CacheLogic.LogWriter != null)
                CacheLogic.LogWriter.WriteLine("Change {0}".FormatWith(GetType().TypeName()));

            Reset();

            Interlocked.Increment(ref invalidations);

            controller.OnChange(this, args);
        }
        catch (Exception e)
        {
            e.LogException();
        }
    }

    public void ResetAll(bool forceReset)
    {
        if (CacheLogic.LogWriter != null)
            CacheLogic.LogWriter.WriteLine("ResetAll {0}".FormatWith(GetType().TypeName()));

        Reset();

        if (forceReset)
        {
            invalidations = 0;
            hits = 0;
            loads = 0;
            sumLoadTime = 0;
        }
        else
        {
            Interlocked.Increment(ref invalidations);
        }

        if (subTables != null)
            foreach (var st in subTables)
                st.ResetAll(forceReset);
    }

    public abstract void SchemaCompleted();

    internal void LoadAll()
    {
        Load();

        if (subTables != null)
            foreach (var st in subTables)
                st.LoadAll();
    }

    protected abstract void Load();
    protected abstract void Reset();

    public abstract Type Type { get; }

    public abstract int? Count { get; }

    int invalidations;
    public int Invalidations { get { return invalidations; } }

    protected int hits;
    public int Hits { get { return hits; } }

    int loads;
    public int Loads { get { return loads; } }

    long sumLoadTime;
    public TimeSpan SumLoadTime
    {
        get { return TimeSpan.FromMilliseconds(sumLoadTime / PerfCounter.FrequencyMilliseconds); }
    }

    protected IDisposable MeasureLoad()
    {
        long start = PerfCounter.Ticks;

        return new Disposable(() =>
        {
            sumLoadTime += (PerfCounter.Ticks - start);
            Interlocked.Increment(ref loads);
        });
    }


    internal static readonly MethodInfo ToStringMethod = ReflectionTools.GetMethodInfo((object o) => o.ToString());

    internal abstract bool Contains(PrimaryKey primaryKey);
}
