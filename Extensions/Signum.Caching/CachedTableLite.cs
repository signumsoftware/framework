using Signum.Utilities.Reflection;
using Signum.Engine.Linq;
using System.Data;
using Signum.Entities.Internal;
using Signum.Engine.Sync;
using System.Collections.Frozen;

namespace Signum.Cache;

class CachedTableLite<T> : CachedTableBase where T : Entity
{
    public override IColumn? ParentColumn { get; set; }

    Table table;

    Alias currentAlias;
    string lastPartialJoin;
    string? remainingJoins;

    Func<FieldReader, object> rowReader = null!;
    ResetLazy<FrozenDictionary<PrimaryKey, object>> rows = null!;
    Func<object, PrimaryKey> idGetter;
    FrozenDictionary<Type, ICachedLiteModelConstructor> liteModelConstructors = null!;

    SemiCachedController<T>? semiCachedController;

    public IDictionary<PrimaryKey, object> GetRows()
    {
        return rows.Value;
    }

    public CachedTableLite(ICacheLogicController controller, AliasGenerator aliasGenerator, string lastPartialJoin, string? remainingJoins)
        : base(controller)
    {
        this.table = Schema.Current.Table(typeof(T));

        HashSet<IColumn> columns = new HashSet<IColumn> { table.PrimaryKey };

        foreach (var modelType in Lite.GetAllLiteModelTypes(typeof(T)))
        {
            var modelConstructor = Lite.GetModelConstructorExpression(typeof(T), modelType);

            ToStringColumnsFinderVisitor.GatherColumns(modelConstructor, this.table, columns);
        }

        var ctr = this.Constructor = new CachedTableConstructor(this, aliasGenerator, columns.ToList());

        this.lastPartialJoin = lastPartialJoin;
        this.remainingJoins = remainingJoins;
        this.currentAlias = aliasGenerator.NextTableAlias(table.Name.Name);
        var isPostgres = Schema.Current.Settings.IsPostgres;

        //Query
        using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
        {
            string select = "SELECT {0}\nFROM {1} {2}\n".FormatWith(
                ctr.columns.ToString(c => currentAlias + "." + c.Name.SqlEscape(isPostgres), ", "),
                table.Name.ToString(),
                currentAlias.ToString());

            select += this.lastPartialJoin + currentAlias + "." + table.PrimaryKey.Name.SqlEscape(isPostgres) + "\n" + this.remainingJoins;

            query = new SqlPreCommandSimple(select);
        }

        //Reader
        {
            rowReader = ctr.GetRowReader();

            idGetter = ctr.GetPrimaryKeyGetter((IColumn)table.PrimaryKey);
        }

        rows = new ResetLazy<FrozenDictionary<PrimaryKey, object>>(() =>
        {
            return SqlServerRetry.Retry(() =>
            {
                CacheLogic.AssertSqlDependencyStarted();

                Dictionary<PrimaryKey, object> result = new Dictionary<PrimaryKey, object>();

                using (MeasureLoad())
                using (Connector.Override(Connector.Current.ForDatabase(table.Name.Schema?.Database)))
                using (var tr = Transaction.ForceNew(IsolationLevel.ReadCommitted))
                {
                    if (CacheLogic.LogWriter != null)
                        CacheLogic.LogWriter.WriteLine("Load {0}".FormatWith(GetType().TypeName()));

                    Connector.Current.ExecuteDataReaderOptionalDependency(query, OnChange, fr =>
                    {
                        var obj = rowReader(fr);
                        result[idGetter(obj)] = obj; //Could be repeated joins
                    });
                    tr.Commit();
                }

                return result.ToFrozenDictionary();
            });
        }, mode: LazyThreadSafetyMode.ExecutionAndPublication);
        
        semiCachedController = new SemiCachedController<T>(this);
    }

    public override void SchemaCompleted()
    {
        this.liteModelConstructors = Lite.GetAllLiteModelTypes(typeof(T))
          .ToFrozenDictionaryEx(modelType => modelType, modelType =>
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
        if (rows == null)
            return;

        if (CacheLogic.LogWriter != null )
            CacheLogic.LogWriter.WriteLine((rows.IsValueCreated ? "RESET {0}" : "Reset {0}").FormatWith(GetType().TypeName()));

        rows.Reset();
    }

    protected override void Load()
    {
        if (rows == null)
            return;

        rows.Load();
    }


    public Lite<T> GetLite(PrimaryKey id, IRetriever retriever, Type modelType)
    {
        Interlocked.Increment(ref hits);

        var model = liteModelConstructors.GetOrThrow(modelType).GetModel(id, retriever);

        var lite = Lite.Create<T>(id, model);
        retriever.ModifiablePostRetrieving((LiteImp)lite);
        return lite;
    }

    public override int? Count
    {
        get { return rows.IsValueCreated ? rows.Value.Count : (int?)null; }
    }

    public override Type Type
    {
        get { return typeof(Lite<T>); }
    }

    public override ITable Table
    {
        get { return table; }
    }

    class ToStringColumnsFinderVisitor : ExpressionVisitor
    {
        ParameterExpression param;

        HashSet<IColumn> columns;

        Table table;

        public ToStringColumnsFinderVisitor(ParameterExpression param, HashSet<IColumn> columns, Table table)
        {
            this.param = param;
            this.columns = columns;
            this.table = table;
        }

        public static Expression GatherColumns(LambdaExpression lambda, Table table,  HashSet<IColumn> columns)
        {
            ToStringColumnsFinderVisitor toStr = new ToStringColumnsFinderVisitor(
                lambda.Parameters.SingleEx(),
                columns,
                table
            );

            var result = toStr.Visit(lambda.Body);

            return result;
        }

        static MethodInfo miMixin = ReflectionTools.GetMethodInfo((Entity e) => e.Mixin<MixinEntity>()).GetGenericMethodDefinition();

        protected override Expression VisitMember(MemberExpression node)
        {
            LambdaExpression? lambda = ExpressionCleaner.GetFieldExpansion(node.Expression?.Type, node.Member);

            if (lambda != null)
            {
                var replace = ExpressionReplacer.Replace(Expression.Invoke(lambda, node.Expression!));
                return this.Visit(replace);
            }

            if (node.Expression == param)
            {
                var field = table.GetField(node.Member);
                var column = GetColumn(field);
                columns.Add(column);
                return node;
            }

            if (node.Expression is MethodCallExpression me && me.Method.IsInstantiationOf(miMixin))
            {
                var type = me.Method.GetGenericArguments()[0];
                var mixin = table.Mixins!.GetOrThrow(type);
                var field = mixin.GetField(node.Member);
                var column = GetColumn(field);
                columns.Add(column);
                return node;
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var obj = base.Visit(node.Object);
            var args = base.Visit(node.Arguments);

            LambdaExpression? lambda = ExpressionCleaner.GetFieldExpansion(obj?.Type, node.Method);

            if (lambda != null)
            {
                var replace = ExpressionReplacer.Replace(Expression.Invoke(lambda, obj == null ? args : args.PreAnd(obj)));

                return this.Visit(replace);
            }

            if (node.Object == param && node.Method.Name == nameof(node.ToString))
            {
                columns.Add(this.table.ToStrColumn!);
                return node;
            }

            return base.VisitMethodCall(node);
        }

        private IColumn GetColumn(Field field)
        {
            if (field is FieldPrimaryKey or FieldValue or FieldTicks or FieldReference)
                return (IColumn)field;

            throw new InvalidOperationException("{0} not supported when caching the ToString for a Lite of a transacional entity ({1})".FormatWith(field.GetType().TypeName(), this.table.Type.TypeName()));
        }
    }

    internal override bool Contains(PrimaryKey primaryKey)
    {
        return this.rows.Value.ContainsKey(primaryKey);
    }
}
