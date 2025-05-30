using Microsoft.Data.SqlClient;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Data;

namespace Signum.Engine;

public static class BulkInserter
{
    public const SqlBulkCopyOptions SafeDefaults = SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.KeepNulls;

    public static int BulkInsert<T>(this IEnumerable<T> entities,
        SqlBulkCopyOptions copyOptions = SafeDefaults,
        bool preSaving = true,
        bool validateFirst = true,
        bool disableIdentity = false,
        int? timeout = null,
        string? message = null)
        where T : Entity
    {
        using (HeavyProfiler.Log(nameof(BulkInsert), () => typeof(T).TypeName()))
        using (var tr = new Transaction())
        {
            var table = Schema.Current.Table(typeof(T));

            if (!disableIdentity && table.IdentityBehaviour && table.TablesMList().Any())
                throw new InvalidOperationException($@"Table {typeof(T)} contains MList but the provided entities have no IDs. Consider:
* Using {nameof(BulkInsertQueryIds)}. This method will query the inserted rows and use the IDs to insert the MList elements.
* Set {nameof(disableIdentity)} = true, and set manually the Ids of the entities before calling this method using {nameof(UnsafeEntityExtensions.SetId)}.
* If you know what you doing, call {nameof(BulkInsertTable)} manually (The MLists won't be saved)."
);

            var list = entities.ToList();

            var rowNum = BulkInsertTable(list, copyOptions, preSaving, validateFirst, disableIdentity, timeout, message);

            BulkInsertMLists<T>(list, copyOptions, timeout, message);

            return tr.Commit(rowNum);
        }
    }

    /// <param name="keySelector">Unique key to retrieve ids</param>
    /// <param name="isNewPredicate">Optional filter to query only the recently inseted entities</param>
    public static int BulkInsertQueryIds<T, K>(this IEnumerable<T> entities,
        Expression<Func<T, K>> keySelector,
        Expression<Func<T, bool>>? isNewPredicate = null,
        SqlBulkCopyOptions copyOptions = SafeDefaults,
        bool preSaving = true,
        bool validateFirst = true,
        int? timeout = null,
        string? message = null)
        where T : Entity
        where K : notnull
    {
        using (HeavyProfiler.Log(nameof(BulkInsertQueryIds), () => typeof(T).TypeName()))
        using (var tr = new Transaction())
        {
            var t = Schema.Current.Table(typeof(T));

            var list = entities.ToList();

            if (isNewPredicate == null)
                isNewPredicate = GetFilterAutomatic<T>(t);

            var rowNum = BulkInsertTable<T>(list, copyOptions, preSaving, validateFirst, false, timeout, message);

            var dictionary = Database.Query<T>().Where(isNewPredicate).Select(a => KeyValuePair.Create(keySelector.Evaluate(a), a.Id)).ToDictionaryEx();

            var getKeyFunc = keySelector.Compile();

            list.ForEach(e =>
            {
                e.SetId(dictionary.GetOrThrow(getKeyFunc(e)));
                e.SetIsNew(false);
            });

            BulkInsertMLists(list, copyOptions, timeout, message);

            GraphExplorer.CleanModifications(GraphExplorer.FromRoots(list));

            return tr.Commit(rowNum);
        }
    }

    static void BulkInsertMLists<T>(List<T> list, SqlBulkCopyOptions options, int? timeout, string? message) where T : Entity
    {
        var mlistPrs = PropertyRoute.GenerateRoutes(typeof(T), includeIgnored: false).Where(a => a.PropertyRouteType == PropertyRouteType.FieldOrProperty && a.Type.IsMList()).ToList();
        foreach (var pr in mlistPrs)
        {
            giBulkInsertMListFromEntities.GetInvoker(typeof(T), pr.Type.ElementType()!)(list, pr, options, timeout, message);
        }
    }

    static Expression<Func<T, bool>> GetFilterAutomatic<T>(Table table) where T : Entity
    {
        if (table.PrimaryKey.Identity)
        {
            var max = ExecutionMode.Global().Using(_ => Database.Query<T>().Max(a => (PrimaryKey?)a.Id));
            if (max == null)
                return a => true;

            return a => a.Id > max;
        }

        var count = ExecutionMode.Global().Using(_ => Database.Query<T>().Count());

        if (count == 0)
            return a => true;

        throw new InvalidOperationException($"Impossible to determine the filter for the IDs query automatically because the table is not Identity and has rows");
    }

    public static int BulkInsertTable<T>(IEnumerable<T> entities,
        SqlBulkCopyOptions copyOptions = SafeDefaults,
        bool preSaving = true,
        bool validateFirst = true,
        bool disableIdentity = false,
        int? timeout = null,
        string? message = null)
        where T : Entity
    {
        using (HeavyProfiler.Log(nameof(BulkInsertTable), () => typeof(T).TypeName()))
        {

            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"BulkInsering {entities.Count()} {typeof(T).TypeName()}" : message,
                    () => BulkInsertTable(entities, copyOptions, preSaving, validateFirst, disableIdentity, timeout, message: null));

            if (disableIdentity)
                copyOptions |= SqlBulkCopyOptions.KeepIdentity;

            if (copyOptions.HasFlag(SqlBulkCopyOptions.UseInternalTransaction))
                throw new InvalidOperationException("BulkInsertDisableIdentity not compatible with UseInternalTransaction");

            var list = entities.ToList();

            if (preSaving)
            {
                Saver.PreSaving(() => GraphExplorer.FromRoots(list));
            }

            if (validateFirst)
            {
                Validate<T>(list);
            }

            var t = Schema.Current.Table<T>();
            bool disableIdentityBehaviour = copyOptions.HasFlag(SqlBulkCopyOptions.KeepIdentity);

            var isPostgres = Schema.Current.Settings.IsPostgres;

            DataTable dt = new DataTable();
            var columns = t.Columns.Values.Where(c => (disableIdentityBehaviour || !c.IdentityBehaviour) && c.GetGeneratedAlwaysType() == Sync.GeneratedAlwaysType.None && c.ComputedColumn == null).ToList();
            foreach (var c in columns)
                dt.Columns.Add(GetColumn(c, isPostgres));

            using (disableIdentityBehaviour ? Administrator.DisableIdentity(t, behaviourOnly: true) : null)
            {
                foreach (var e in list)
                {
                    if (!e.IsNew)
                        throw new InvalidOperationException("Entites should be new");
                    t.SetToStrField(e);
                    var values = t.BulkInsertDataRow(e);
                    dt.Rows.Add(values);
                }
            }

            using (var tr = new Transaction())
            {
                Schema.Current.OnPreBulkInsert(typeof(T), inMListTable: false);

                Executor.BulkCopy(dt, columns, t.Name, copyOptions, timeout);

                foreach (var item in list)
                    item.SetNotModified();

                return tr.Commit(list.Count);
            }
        }
    }

    private static DataColumn GetColumn(IColumn c, bool isPostgres)
    {
        var dc = new DataColumn(c.Name, ConvertType(c.Type, isPostgres));
        if (c.Type.UnNullify() == typeof(DateTime))
            dc.DateTimeMode = ToDataSetDateTime(c.DateTimeKind);
        return dc;
    }

    private static DataSetDateTime ToDataSetDateTime(DateTimeKind dateTimeKind)
    {
        return dateTimeKind switch
        {
            DateTimeKind.Utc => DataSetDateTime.Utc,
            DateTimeKind.Unspecified => DataSetDateTime.Unspecified,
            DateTimeKind.Local => DataSetDateTime.Local,
            _ => throw new UnexpectedValueException(Schema.Current.DateTimeKind)
        };
    }

    private static Type ConvertType(Type type, bool isPostgres)
    {
        var result = type.UnNullify();

        //if (!isPostgres)
        //{
        //    if (result == typeof(DateOnly))
        //        return typeof(DateTime);

        //    if (result == typeof(TimeOnly))
        //        return typeof(TimeSpan);
        //}

        return result;
    }

    static void Validate<T>(IEnumerable<T> entities) where T : Entity
    {
#if DEBUG
        var errors = new List<IntegrityCheckWithEntity>();
        foreach (var e in entities)
        {
            var ic = e.FullIntegrityCheck();

            if (ic != null)
            {
                var withEntites = ic.WithEntities(GraphExplorer.FromRoot(e));
                errors.AddRange(withEntites);
            }
        }
        if (errors.Count > 0)
            throw new IntegrityCheckException(errors);
#else
        var errors = new Dictionary<Guid, IntegrityCheck>();
        foreach (var e in entities)
        {
            var ic = e.FullIntegrityCheck();

            if (ic != null)
            {
                errors.AddRange(ic);
            }
        }
        if (errors.Count > 0)
            throw new IntegrityCheckException(errors);
#endif
    }

    static readonly GenericInvoker<Func<IList, PropertyRoute, SqlBulkCopyOptions, int?, string?, int>> giBulkInsertMListFromEntities =
        new((entities, propertyRoute, options, timeout, message) =>
        BulkInsertMListTablePropertyRoute<Entity, string>((List<Entity>)entities, propertyRoute, options, timeout, message));

    static int BulkInsertMListTablePropertyRoute<E, V>(List<E> entities, PropertyRoute route, SqlBulkCopyOptions copyOptions, int? timeout, string? message)
         where E : Entity
    {
        return BulkInsertMListTable<E, V>(entities, route.GetLambdaExpression<E, MList<V>>(safeNullAccess: false), copyOptions, disableMListIdentity: false, timeout, message);
    }

    public static int BulkInsertMListTable<E, V>(
        List<E> entities,
        Expression<Func<E, MList<V>>> mListProperty,
        SqlBulkCopyOptions copyOptions = SafeDefaults,
        bool disableMListIdentity = false,
        int? timeout = null,
        string? message = null)
        where E : Entity
    {
        using (HeavyProfiler.Log(nameof(BulkInsertMListTable), () => $"{mListProperty} ({typeof(E).TypeName()})"))
        {
            try
            {
                var func = PropertyRoute.Construct(mListProperty).GetLambdaExpression<E, MList<V>>(safeNullAccess: true).Compile();

                var mlistElements = (from e in entities
                                     from mle in func(e).EmptyIfNull().Select((iw, i) => new MListElement<E, V>
                                     {
                                         RowOrder = i,
                                         Element = iw,
                                         Parent = e,
                                     })
                                     select mle).ToList();

                return BulkInsertMListTable(mlistElements, mListProperty, copyOptions, disableMListIdentity, timeout, updateParentTicks: false, message: message);
            }
            catch (InvalidOperationException e) when (e.Message.Contains("has no Id"))
            {
                throw new InvalidOperationException($"{nameof(BulkInsertMListTable)} requires that you set the Id of the entities manually using {nameof(UnsafeEntityExtensions.SetId)}");
            }
        }
    }

    public static int BulkInsertMListTable<E, V>(
        this IEnumerable<MListElement<E, V>> mlistElements,
        Expression<Func<E, MList<V>>> mListProperty,
        SqlBulkCopyOptions copyOptions = SafeDefaults,
        bool disableMListIdentity = false,
        int? timeout = null,
        bool? updateParentTicks = null, /*Needed for concurrency and Temporal tables*/
        string? message = null)
        where E : Entity
    {
        using (HeavyProfiler.Log(nameof(BulkInsertMListTable), () => $"{mListProperty} ({typeof(MListElement<E, V>).TypeName()})"))
        {
            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"BulkInsering MList<{ typeof(V).TypeName()}> in { typeof(E).TypeName()}" : message,
                    () => BulkInsertMListTable(mlistElements, mListProperty, copyOptions, disableMListIdentity, timeout, updateParentTicks, message: null));

            if (copyOptions.HasFlag(SqlBulkCopyOptions.UseInternalTransaction))
                throw new InvalidOperationException("BulkInsertDisableIdentity not compatible with UseInternalTransaction");

            if (disableMListIdentity)
                copyOptions |= SqlBulkCopyOptions.KeepIdentity;

            var mlistTable = ((FieldMList)Schema.Current.Field(mListProperty)).TableMList;

            if (updateParentTicks == null)
            {
                updateParentTicks = mlistTable.PrimaryKey.Type != typeof(Guid) && mlistTable.BackReference.ReferenceTable.Ticks != null;
            }

            var maxRowId = updateParentTicks.Value ? Database.MListQuery(mListProperty).Max(a => (PrimaryKey?)a.RowId) : null;

            bool disableIdentityBehaviour = copyOptions.HasFlag(SqlBulkCopyOptions.KeepIdentity);

            var isPostgres = Schema.Current.Settings.IsPostgres;

            var dt = new DataTable();
            var columns = mlistTable.Columns.Values.Where(c => (!c.IdentityBehaviour || disableIdentityBehaviour) && c.GetGeneratedAlwaysType() == Sync.GeneratedAlwaysType.None && c.ComputedColumn == null).ToList();
            foreach (var c in columns)
                dt.Columns.Add(GetColumn(c, isPostgres));

            var list = mlistElements.ToList();

            foreach (var e in list)
            {
                dt.Rows.Add(mlistTable.BulkInsertDataRow(e.Parent, e.Element!, e.RowOrder, disableMListIdentity ? e.RowId : null));
            }

            using (var tr = new Transaction())
            {
                Schema.Current.OnPreBulkInsert(typeof(E), inMListTable: true);

                using (disableIdentityBehaviour ? Administrator.DisableIdentity(mlistTable) : null)
                {
                    Executor.BulkCopy(dt, columns, mlistTable.Name, copyOptions, timeout);
                }

                var result = list.Count;

                if (updateParentTicks.Value)
                {
                    Database.MListQuery(mListProperty)
                        .Where(a => maxRowId == null || a.RowId > maxRowId)
                        .Select(a => a.Parent)
                        .UnsafeUpdate()
                        .Set(e => e.Ticks, a => Clock.Now.Ticks)
                        .Execute();
                }

                return tr.Commit(result);
            }
        }
    }

    public static int BulkInsertView<T>(this IEnumerable<T> entities,
      SqlBulkCopyOptions copyOptions = SafeDefaults,
      int? timeout = null,
      string? message = null)
      where T : IView
    {
        using (HeavyProfiler.Log(nameof(BulkInsertView), () => typeof(T).Name))
        {
            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"BulkInsering {entities.Count()} {typeof(T).TypeName()}" : message,
                    () => BulkInsertView(entities, copyOptions, timeout, message: null));

            if (copyOptions.HasFlag(SqlBulkCopyOptions.UseInternalTransaction))
                throw new InvalidOperationException("BulkInsertDisableIdentity not compatible with UseInternalTransaction");

            var t = Schema.Current.View<T>();

            var list = entities.ToList();

            bool disableIdentityBehaviour = copyOptions.HasFlag(SqlBulkCopyOptions.KeepIdentity);

            var isPostgres = Schema.Current.Settings.IsPostgres;

            var columns = t.Columns.Values.Where(c => (!c.IdentityBehaviour || disableIdentityBehaviour) && c.GetGeneratedAlwaysType() == Sync.GeneratedAlwaysType.None && c.ComputedColumn == null).ToList();
            DataTable dt = new DataTable();
            foreach (var c in columns)
                dt.Columns.Add(GetColumn(c, isPostgres));

            foreach (var e in entities)
            {
                dt.Rows.Add(t.BulkInsertDataRow(e));
            }

            using (var tr = new Transaction())
            {
                Schema.Current.OnPreBulkInsert(typeof(T), inMListTable: false);

                Executor.BulkCopy(dt, columns, t.Name, copyOptions, timeout);

                return tr.Commit(list.Count);
            }
        }
    }
}
