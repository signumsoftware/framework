using Signum.Engine.Linq;
using Signum.Engine.Sync;
using System.Data;

namespace Signum.Cache;

class CachedTableMList<T> : CachedTableBase
{
    public override IColumn? ParentColumn { get; set; }

    TableMList table;

    ResetLazy<FrozenDictionary<PrimaryKey, Dictionary<PrimaryKey, object>>> relationalRows;

    static ParameterExpression result = Expression.Parameter(typeof(T));

    Func<FieldReader, object> rowReader;
    Expression<Func<object, IRetriever, MList<T>.RowIdElement>> activatorExpression;
    Func<object, IRetriever, MList<T>.RowIdElement> activator;
    Func<object, PrimaryKey> parentIdGetter;
    Func<object, PrimaryKey> rowIdGetter;

    public CachedTableMList(ICacheLogicController controller, TableMList table, AliasGenerator aliasGenerator, string lastPartialJoin, string? remainingJoins)
        : base(controller)
    {
        this.table = table;
        var isPostgres = Schema.Current.Settings.IsPostgres;
        CachedTableConstructor ctr = this.Constructor = new CachedTableConstructor(this, aliasGenerator, table.Columns.Values.ToList());

        //Query
        using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
        {
            string select = "SELECT\r\n{0}\r\nFROM {1} {2}\r\n".FormatWith(
                ctr.table.Columns.Values.ToString(c => ctr.currentAlias + "." + c.Name.SqlEscape(isPostgres), ",\r\n"),
                table.Name.ToString(),
                ctr.currentAlias!.ToString());

            ctr.remainingJoins = lastPartialJoin + ctr.currentAlias + "." + table.BackReference.Name.SqlEscape(isPostgres) + "\r\n" + remainingJoins;

            query = new SqlPreCommandSimple(select);
        }

        //Reader
        {
            rowReader = ctr.GetRowReader();
        }

        //Completer
        {
            List<Expression> instructions = new List<Expression>
            {
                Expression.Assign(ctr.origin, Expression.Convert(CachedTableConstructor.originObject, ctr.tupleType)),
                Expression.Assign(result, ctr.MaterializeField(table.Field))
            };
            var ci = typeof(MList<T>.RowIdElement).GetConstructor(new []{typeof(T), typeof(PrimaryKey), typeof(int?)})!;

            var order = table.Order == null ? Expression.Constant(null, typeof(int?)) :
                 ctr.GetTupleProperty(table.Order).Nullify();

            instructions.Add(Expression.New(ci, result, CachedTableConstructor.NewPrimaryKey(ctr.GetTupleProperty(table.PrimaryKey)), order));

            var block = Expression.Block(typeof(MList<T>.RowIdElement), new[] { ctr.origin, result }, instructions);

            activatorExpression = Expression.Lambda<Func<object, IRetriever, MList<T>.RowIdElement>>(block, CachedTableConstructor.originObject, CachedTableConstructor.retriever);

            activator = activatorExpression.Compile();

            parentIdGetter = ctr.GetPrimaryKeyGetter(table.BackReference);
            rowIdGetter = ctr.GetPrimaryKeyGetter(table.PrimaryKey);
        }

        relationalRows = new ResetLazy<FrozenDictionary<PrimaryKey, Dictionary<PrimaryKey, object>>>(() =>
        {
            return SqlServerRetry.Retry(() =>
            {
                CacheLogic.AssertSqlDependencyStarted();
                
                Dictionary<PrimaryKey, Dictionary<PrimaryKey, object>> result = new Dictionary<PrimaryKey, Dictionary<PrimaryKey, object>>();

                using (MeasureLoad())
                using (Connector.Override(Connector.Current.ForDatabase(table.Name.Schema?.Database)))
                using (var tr = Transaction.ForceNew(IsolationLevel.ReadCommitted))
                {
                    if (CacheLogic.LogWriter != null)
                        CacheLogic.LogWriter.WriteLine("Load {0}".FormatWith(GetType().TypeName()));

                    Connector.Current.ExecuteDataReaderOptionalDependency(query, OnChange, fr =>
                    {
                        object obj = rowReader(fr);
                        PrimaryKey parentId = parentIdGetter(obj);
                        var dic = result.TryGetC(parentId);
                        if (dic == null)
                            result[parentId] = dic = new Dictionary<PrimaryKey, object>();

                        dic[rowIdGetter(obj)] = obj;
                    });
                    tr.Commit();
                }

                return result;
            });
        }, mode: LazyThreadSafetyMode.ExecutionAndPublication);
    }

    protected override void Reset()
    {
        if (CacheLogic.LogWriter != null)
            CacheLogic.LogWriter.WriteLine((relationalRows.IsValueCreated ? "RESET {0}" : "Reset {0}").FormatWith(GetType().TypeName()));


        relationalRows.Reset();
    }

    protected override void Load()
    {
        relationalRows.Load();
    }

    public MList<T> GetMList(PrimaryKey id, IRetriever retriever)
    {
        Interlocked.Increment(ref hits);

        MList<T> result;
        var dic = relationalRows.Value.TryGetC(id);
        if (dic == null)
            result = new MList<T>();
        else
        {
            result = new MList<T>(dic.Count);
            var innerList = ((IMListPrivate<T>)result).InnerList;
            foreach (var obj in dic.Values)
            {
                innerList.Add(activator(obj, retriever));
            }
            ((IMListPrivate)result).ExecutePostRetrieving(null!);

        }

        CachedTableConstructor.resetModifiedAction(retriever, result);

        return result;
    }

    public override int? Count
    {
        get { return relationalRows.IsValueCreated ? relationalRows.Value.Count : (int?)null; }
    }

    public override Type Type
    {
        get { return typeof(MList<T>); }
    }

    public override ITable Table
    {
        get { return table; }
    }

    internal override bool Contains(PrimaryKey primaryKey)
    {
        throw new InvalidOperationException("CacheMListTable does not implements contains");
    }

    public override void SchemaCompleted()
    {
        if (this.subTables != null)
            foreach (var item in this.subTables)
                item.SchemaCompleted();
    }
}
