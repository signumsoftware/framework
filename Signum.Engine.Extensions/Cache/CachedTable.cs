using System.Collections.Concurrent;
using Signum.Engine.Linq;
using System.Data;
using Signum.Entities.Reflection;
using Signum.Engine.Connection;

namespace Signum.Engine.Cache;



class CachedTable<T> : CachedTableBase where T : Entity
{
    Table table;

    ResetLazy<Dictionary<PrimaryKey, object>> rows;

    public Dictionary<PrimaryKey, object> GetRows()
    {
        return rows.Value;
    }

    Func<FieldReader, object> rowReader;
    Action<object, IRetriever, T> completer;
    Expression<Action<object, IRetriever, T>> completerExpression;
    Func<object, PrimaryKey> idGetter;
    Dictionary<Type, ICachedLiteModelConstructor> liteModelConstructors = null!; 

    public override IColumn? ParentColumn { get; set; }

    internal SemiCachedController<T>? semiCachedController;

    public CachedTable(ICacheLogicController controller, AliasGenerator aliasGenerator, string? lastPartialJoin, string? remainingJoins)
        : base(controller)
    {
        this.table = Schema.Current.Table(typeof(T));

        CachedTableConstructor ctr = this.Constructor = new CachedTableConstructor(this, aliasGenerator, table.Columns.Values.ToList());
        var isPostgres = Schema.Current.Settings.IsPostgres;
        //Query
        using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
        {
            string select = "SELECT\r\n{0}\r\nFROM {1} {2}\r\n".FormatWith(
                Table.Columns.Values.ToString(c => ctr.currentAlias + "." + c.Name.SqlEscape(isPostgres), ",\r\n"),
                table.Name.ToString(),
                ctr.currentAlias!.ToString());

            ctr.remainingJoins = lastPartialJoin == null ? null : lastPartialJoin + ctr.currentAlias + ".Id\r\n" + remainingJoins;

            if (ctr.remainingJoins != null)
                select += ctr.remainingJoins;

            query = new SqlPreCommandSimple(select);
        }


        //Reader
        {
            rowReader = ctr.GetRowReader();
        }

        //Completer
        {
            ParameterExpression me = Expression.Parameter(typeof(T), "me");

            var block = ctr.MaterializeEntity(me, table);

            completerExpression = Expression.Lambda<Action<object, IRetriever, T>>(block, CachedTableConstructor.originObject, CachedTableConstructor.retriever, me);

            completer = completerExpression.Compile();

            idGetter = ctr.GetPrimaryKeyGetter((IColumn)table.PrimaryKey);
        }

        rows = new ResetLazy<Dictionary<PrimaryKey, object>>(() =>
        {
            return SqlServerRetry.Retry(() =>
            {
                CacheLogic.AssertSqlDependencyStarted();

                Table table = Connector.Current.Schema.Table(typeof(T));

                Dictionary<PrimaryKey, object> result = new Dictionary<PrimaryKey, object>();
                using (MeasureLoad())
                using (Connector.Override(Connector.Current.ForDatabase(table.Name.Schema?.Database)))
                using (var tr = Transaction.ForceNew(IsolationLevel.ReadCommitted))
                {
                    if (CacheLogic.LogWriter != null)
                        CacheLogic.LogWriter.WriteLine("Load {0}".FormatWith(GetType().TypeName()));

                    Connector.Current.ExecuteDataReaderOptionalDependency(query, OnChange, fr =>
                    {
                        object obj = rowReader(fr);
                        result[idGetter(obj)] = obj; //Could be repeated joins
                    });
                    tr.Commit();
                }

                return result;
            });
        }, mode: LazyThreadSafetyMode.ExecutionAndPublication);

        if(!CacheLogic.WithSqlDependency && lastPartialJoin.HasText()) //Is semi
        {
            semiCachedController = new SemiCachedController<T>(this);
        }
    }

    public override void SchemaCompleted()
    {
        this.liteModelConstructors = Lite.GetAllLiteModelTypes(typeof(T))
            .ToDictionary(modelType => modelType, modelType => 
            {
                var modelConstructor = Lite.GetModelConstructorExpression(typeof(T), modelType);
                var cachedModelConstructor = LiteModelExpressionVisitor.giGetCachedLiteModelConstructor.GetInvoker(typeof(T), modelType)(this.Constructor, modelConstructor);
                return cachedModelConstructor;
            });


        if (this.subTables != null)
            foreach (var item in this.subTables)
                item.SchemaCompleted();

    }


    protected override void Reset()
    {
        if (CacheLogic.LogWriter != null)
            CacheLogic.LogWriter.WriteLine((rows.IsValueCreated ? "RESET {0}" : "Reset {0}").FormatWith(GetType().TypeName()));

        rows.Reset();
        if (this.BackReferenceDictionaries != null)
        {
            foreach (var item in this.BackReferenceDictionaries.Values)
            {
                item.Reset();
            }
        }
    }

    protected override void Load()
    {
        rows.Load();
    }



    public object GetRow(PrimaryKey id)
    {
        Interlocked.Increment(ref hits);
        var origin = this.GetRows().TryGetC(id);
        if (origin == null)
            throw new EntityNotFoundException(typeof(T), id);

        return origin;
    }

    public object GetLiteModel(PrimaryKey id, Type modelType, IRetriever retriever)
    {
        return this.liteModelConstructors.GetOrThrow(modelType).GetModel(id, retriever);
    }

    public object? TryGetLiteModel(PrimaryKey id, Type modelType, IRetriever retriever)
    {
        Interlocked.Increment(ref hits);
        var origin = this.GetRows().TryGetC(id);
        if (origin == null)
            return null;

        return this.liteModelConstructors.GetOrThrow(modelType).GetModel(id, retriever);
    }

    public bool Exists(PrimaryKey id)
    {
        Interlocked.Increment(ref hits);
        var origin = this.GetRows().TryGetC(id);
        if (origin == null)
            return false;

        return true;
    }

    public void Complete(T entity, IRetriever retriever)
    {
        Interlocked.Increment(ref hits);

        var origin = this.GetRows().TryGetC(entity.Id);
        if (origin == null)
            throw new EntityNotFoundException(typeof(T), entity.Id);

        completer(origin, retriever, entity);

        var additional = Schema.Current.GetAdditionalBindings(typeof(T));

        if(additional != null)
        {
            foreach (var ab in additional)
                ab.SetInMemory(entity, retriever);
        }
    }

    internal IEnumerable<PrimaryKey> GetAllIds()
    {
        Interlocked.Increment(ref hits);
        return this.GetRows().Keys;
    }

    public override int? Count
    {
        get { return rows.IsValueCreated ? rows.Value.Count : (int?)null; }
    }

    public override Type Type
    {
        get { return typeof(T); }
    }

    public override ITable Table
    {
        get { return table; }
    }


    internal override bool Contains(PrimaryKey primaryKey)
    {
        return this.GetRows().ContainsKey(primaryKey);
    }

    ConcurrentDictionary<LambdaExpression, ResetLazy<Dictionary<PrimaryKey, List<PrimaryKey>>>> BackReferenceDictionaries =
        new ConcurrentDictionary<LambdaExpression, ResetLazy<Dictionary<PrimaryKey, List<PrimaryKey>>>>(ExpressionComparer.GetComparer<LambdaExpression>(false));

    internal Dictionary<PrimaryKey, List<PrimaryKey>> GetBackReferenceDictionary<R>(Expression<Func<T, Lite<R>?>> backReference)
        where R : Entity
    {
        var lazy = BackReferenceDictionaries.GetOrAdd(backReference, br =>
        {
            var column = GetColumn(Reflector.GetMemberList((Expression<Func<T, Lite<R>>>)br));

            var idGetter = this.Constructor.GetPrimaryKeyGetter(table.PrimaryKey);

            if (column.Nullable.ToBool())
            {
                var backReferenceGetter = this.Constructor.GetPrimaryKeyNullableGetter(column);

                return new ResetLazy<Dictionary<PrimaryKey, List<PrimaryKey>>>(() =>
                {
                    return this.rows.Value.Values
                    .Where(a => backReferenceGetter(a) != null)
                    .GroupToDictionary(a => backReferenceGetter(a)!.Value, a => idGetter(a));
                }, LazyThreadSafetyMode.ExecutionAndPublication);
            }
            else
            {
                var backReferenceGetter = this.Constructor.GetPrimaryKeyGetter(column);
                return new ResetLazy<Dictionary<PrimaryKey, List<PrimaryKey>>>(() =>
                {
                    return this.rows.Value.Values
                    .GroupToDictionary(a => backReferenceGetter(a), a => idGetter(a));
                }, LazyThreadSafetyMode.ExecutionAndPublication);
            }
        });

        return lazy.Value;
    }

    private IColumn GetColumn(MemberInfo[] members)
    {
        IFieldFinder? current = (Table)this.Table;
        Field? field = null;

        for (int i = 0; i < members.Length - 1; i++)
        {
            if (current == null)
                throw new InvalidOperationException("{0} does not implement {1}".FormatWith(field, typeof(IFieldFinder).Name));

            field = current.GetField(members[i]);

            current = field as IFieldFinder;
        }

        var lastMember = members[members.Length - 1];

        if (lastMember is Type t)
            return ((FieldImplementedBy)field!).ImplementationColumns.GetOrThrow(t.CleanType());
        else if (current != null)
            return (IColumn)current.GetField(lastMember);
        else
            throw new InvalidOperationException("Unexpected");
    }
}

public interface ICachedLiteModelConstructor
{
    object GetModel(PrimaryKey pk, IRetriever retriever);
}

public class CachedLiteModelConstructor<M> : ICachedLiteModelConstructor
    where M : notnull
{
    Expression<Func<PrimaryKey, IRetriever, M>> Expression; //For Debugging
    Func<PrimaryKey, IRetriever, M> Function;

    public CachedLiteModelConstructor(Expression<Func<PrimaryKey, IRetriever, M>> expression)
    {
        this.Expression = expression;
        this.Function = expression.Compile();
    }

    public object GetModel(PrimaryKey pk, IRetriever retriever)
    {
        return Function(pk, retriever);
    }
}
