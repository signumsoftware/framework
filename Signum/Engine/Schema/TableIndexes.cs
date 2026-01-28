using System.Diagnostics.CodeAnalysis;
using Microsoft.Identity.Client;
using NpgsqlTypes;
using Signum.Engine.Sync;
using Signum.Entities.Reflection;
using Signum.Utilities.DataStructures;

namespace Signum.Engine.Maps;

public class TableIndex
{
    public ITable Table { get; private set; }
    public IColumn[] Columns { get; private set; }
    public IColumn[]? IncludeColumns { get; set; }

    public string? Where { get; set; }

    public static IColumn[] GetColumnsFromField(Field field)
    {
        if (field is FieldEmbedded or FieldMixin)
            throw new InvalidOperationException("Embedded fields not supported for indexes");

        return field.Columns().ToArray();
    }

    public TableIndex(ITable table, params IColumn[] columns)
    {
        if (table == null)
            throw new ArgumentNullException(nameof(table));

        if (columns == null || columns.IsEmpty())
            throw new ArgumentNullException(nameof(columns));

        this.Table = table;
        this.Columns = columns;
    }

    public bool Clustered { get; set; }
    public bool Unique { get; set; }
    public bool PrimaryKey { get; set; }
    public bool Partitioned { get; set; }

    public string? PartitionColumnName => Partitioned ? Table.Columns.Values.OfType<FieldPartitionId>().SingleEx().Name : null;
    public string? PartitionSchemeName => Partitioned ? Table.PartitionScheme!.Name : null;

    public virtual string GetIndexName(ObjectName tableName)
    {
        if (PrimaryKey)
            return GetPrimaryKeyName(tableName);

        int maxLength = MaxNameLength();

        var isPostgres = tableName.IsPostgres;

        var prefix = Unique ? "UIX" : Clustered ? "CIX" : "IX";

        if (isPostgres)
            prefix = prefix.ToLower();

        return StringHashEncoder.ChopHash($"{prefix}_{tableName.Name}_{ColumnSignature()}", maxLength, isPostgres) + WhereSignature();
    }

    internal static string GetPrimaryKeyName(ObjectName tableName)
    {
        var prefix = tableName.IsPostgres ? "pk" : "PK";

        return StringHashEncoder.ChopHash($"{prefix}_{tableName.Schema.Name}_{tableName.Name}", MaxNameLength(), tableName.IsPostgres);
    }

    protected static int MaxNameLength()
    {
        return Connector.Current.MaxNameLength - StringHashEncoder.HashSize - 2;
    }

    public string IndexName => GetIndexName(Table.Name);

    protected string ColumnSignature()
    {
        return Columns.ToString(c => c.Name, "_");
    }

    public string? WhereSignature()
    {
        var includeColumns = IncludeColumns.HasItems() ? IncludeColumns.ToString(c => c.Name, "_") : null;

        if (string.IsNullOrEmpty(Where) && includeColumns == null)
            return null;

        return "__" + StringHashEncoder.Codify(Where + includeColumns, this.Table.Name.IsPostgres);
    }

    public string? ViewName
    {
        get
        {
            if (!Unique)
                return null;

            if (!Where.HasText())
                return null;

            if (Connector.Current.AllowsIndexWithWhere(Where))
                return null;

            var maxSize = MaxNameLength();

            var prefix = this.Table.Name.IsPostgres ? "vix" : "VIX";

            return StringHashEncoder.ChopHash($"{prefix}_{Table.Name.Name}_{ColumnSignature()}", maxSize, this.Table.Name.IsPostgres) + WhereSignature();
        }
    }

    public bool AvoidAttachToUniqueIndexes { get; set; }

    public override string ToString()
    {
        return IndexName;
    }

    public string HintText()
    {
        return $"INDEX([{this.IndexName}])";
    }
}

public class FullTextTableIndex : TableIndex
{
    public class SqlServerOptions
    {
        public string CatallogName = "DefaultFullTextCatallog";

        public FullTextIndexChangeTracking? ChangeTraking;
        public string? StoplistName;
        public string? PropertyListName;

        public static readonly string FULL_TEXT = "FULL_TEXT_INDEX";
    }

    public class PostgresOptions
    {
        public string TsVectorColumnName = PostgresTsVectorColumn.DefaultTsVectorColumn;
        public string Configuration = DefaultLanguage();

        public static string DefaultLanguage()
        {
            return Schema.Current.ForceCultureInfo?.EnglishName.ToLower().Try(a => a.TryBefore(" ") ?? a) ?? "english";
        }

        public Dictionary<string, NpgsqlTsVector.Lexeme.Weight> Weights = new Dictionary<string, NpgsqlTsVector.Lexeme.Weight>(); //If not set it will use the order A, B, C, D, D, D...
        internal void DefaultWeights(IColumn[] columns)
        {
            var defaultWeight = NpgsqlTsVector.Lexeme.Weight.A;
            foreach (var c in columns)
            {
                if (!Weights.ContainsKey(c.Name))
                {
                    Weights.Add(c.Name, defaultWeight);
                }

                if (defaultWeight > NpgsqlTsVector.Lexeme.Weight.D)
                    defaultWeight--;
            }
        }
    }

    public SqlServerOptions SqlServer = new SqlServerOptions();
    public PostgresOptions Postgres = new PostgresOptions();

    public override string GetIndexName(ObjectName tableName)
    {
        if (!tableName.IsPostgres)
            return SqlServerOptions.FULL_TEXT;

        return StringHashEncoder.ChopHash("ix_{0}_{1}".FormatWith(tableName.Name, ColumnSignature()), MaxNameLength(), tableName.IsPostgres);
    }

    public FullTextTableIndex(ITable table, IColumn[] columns) : base(table, columns)
    {
    }

    ComputedColumn GetComputedColumn()
    {
        var pg = this.Postgres;
        pg.DefaultWeights(Columns);

        var exp = Columns.ToString(a => $"setweight(to_tsvector('{pg.Configuration}'::regconfig, (COALESCE({a.Name.SqlEscape(true)}, ''::character varying))::text), '{pg.Weights.GetOrThrow(a.Name)}'::\"char\")", " || ");

        return new ComputedColumn(exp, persisted: true);
    }

    protected internal IEnumerable<IColumn> GenerateColumns()
    {
        if (this.Table.Name.IsPostgres)
        {
          yield return new PostgresTsVectorColumn(this.Postgres.TsVectorColumnName, this.Columns) { ComputedColumn = this.GetComputedColumn() };
        }
    }

    internal static string GetSqlServerChangeTracking(FullTextIndexChangeTracking changeTraking) => changeTraking switch
    {
        FullTextIndexChangeTracking.Manual => "MANUAL",
        FullTextIndexChangeTracking.Auto => "AUTO",
        FullTextIndexChangeTracking.Off => "OFF",
        FullTextIndexChangeTracking.Off_NoPopulation => "OFF, NO POPULATION",
        _ => throw new UnexpectedValueException(changeTraking)
    };
}

public class PostgresTsVectorColumn : IColumn
{
    public static string DefaultTsVectorColumn = "tsvector";

    public IColumn[] Columns { get; set; }

    public PostgresTsVectorColumn(string name, IColumn[] columns)
    {
        this.Name = name;
        this.Columns = columns;
    }

    public string Name { get; private set; }

    public IsNullable Nullable => IsNullable.Yes;
    public AbstractDbType DbType => new AbstractDbType(NpgsqlDbType.TsVector);
    public Type Type => typeof(NpgsqlTsVector);
    public string? UserDefinedTypeName => null;
    public bool PrimaryKey => false;
    public bool IdentityBehaviour => false;
    public bool Identity => false;
    public string? Default => null;
    public ComputedColumn? ComputedColumn { get; set; }
    public string? Check => null;
    public int? Size => null;
    public byte? Precision => null;
    public byte? Scale => null;
    public string? Collation => null;
    public Table? ReferenceTable => null;
    public bool AvoidForeignKey => false;
    public DateTimeKind DateTimeKind => DateTimeKind.Utc;

    public override string ToString()
    {
        return Name;
    }
}

public enum FullTextIndexChangeTracking
{
    Manual,
    Auto,
    Off,
    Off_NoPopulation
}

public class VectorTableIndex : TableIndex
{
    public class SqlServerOptions
    {
        public SqlServerDistanceMetric Metric { get; set; } = SqlServerDistanceMetric.Cosine;
        public SqlServerVectorIndexType IndexType { get; set; } = SqlServerVectorIndexType.DiskANN;
        public int? MaxDegreeOfParallelism { get; set; }
    }

    public class PostgresOptions
    {
        public PGVectorIndexType? IndexType { get; set; }
        public PGVectorDistanceMetric Metric { get; set; } = PGVectorDistanceMetric.Cosine;
        public int? Lists { get; set; }
    }

    public SqlServerOptions SqlServer { get; set; } = new SqlServerOptions();
    public PostgresOptions Postgres { get; set; } = new PostgresOptions();

    public VectorTableIndex(ITable table, IColumn column) : base(table, column)
    {
    }

    public override string GetIndexName(ObjectName tableName)
    {
        var prefix = tableName.IsPostgres ? "vec_ix" : "VEC_IX";
        return StringHashEncoder.ChopHash($"{prefix}_{tableName.Name}_{ColumnSignature()}", MaxNameLength(), tableName.IsPostgres);
    }

    protected internal IEnumerable<IColumn> GenerateColumns()
    {
        // Vector columns are typically defined by the user with [DbType] attribute
        // No additional columns need to be generated
        yield break;
    }

    public static string GetSqlserverString(FullTextIndexChangeTracking changeTraking) => changeTraking switch
    {
        FullTextIndexChangeTracking.Manual => "MANUAL",
        FullTextIndexChangeTracking.Auto => "AUTO",
        FullTextIndexChangeTracking.Off => "OFF",
        FullTextIndexChangeTracking.Off_NoPopulation => "OFF, NO POPULATION",
        _ => throw new UnexpectedValueException(changeTraking)
    };

    public static string GetSqlServerVectorMetric(SqlServerDistanceMetric metric) => metric switch
    {
        SqlServerDistanceMetric.Cosine => "cosine",
        SqlServerDistanceMetric.Euclidean => "euclidean",
        SqlServerDistanceMetric.DotProduct => "dot",
        _ => throw new UnexpectedValueException(metric)
    };

    internal static string GetPGVectorIndex(PGVectorIndexType indexType) => indexType switch
    {
        PGVectorIndexType.IVFFlat => "ivfflat",
        PGVectorIndexType.HNSW => "hnsw",
        _ => throw new UnexpectedValueException(indexType)
    };

    internal static object GetPGVectorDistanceMetric(PGVectorDistanceMetric metric) => metric switch
    {
        PGVectorDistanceMetric.Cosine => "vector_cosine_ops",
        PGVectorDistanceMetric.L2 => "vector_l2_ops",
        PGVectorDistanceMetric.InnerProduct => "vector_ip_ops",
        _ => throw new UnexpectedValueException(metric)
    };
}

public enum SqlServerVectorIndexType
{
    DiskANN
}

public enum SqlServerDistanceMetric
{
    Cosine,
    Euclidean,
    DotProduct
}

public enum PGVectorIndexType
{
    IVFFlat,
    HNSW
}

public enum PGVectorDistanceMetric
{
    Cosine,
    L2, // Euclidean
    InnerProduct
}

public class IndexKeyColumns
{
    public static (Field? field, IColumn[] columns)[] Split(IFieldFinder finder, LambdaExpression columns)
    {
        if (columns == null)
            throw new ArgumentNullException(nameof(columns));

        if (columns.Body.NodeType == ExpressionType.New)
        {
            var resultColumns = (from a in ((NewExpression)columns.Body).Arguments
                                 select GetColumns(finder, Expression.Lambda(Expression.Convert(a, typeof(object)), columns.Parameters)));

            return resultColumns.ToArray();
        }

        return [GetColumns(finder, columns)];
    }

    static string[] ignoreMembers = new string[] { "ToLite", "ToLiteFat" };

    static (Field? field, IColumn[] columns) GetColumns(IFieldFinder finder, LambdaExpression field)
    {
        var body = field.Body;

        Type? type = RemoveCasting(ref body);

        body = IndexWhereExpressionVisitor.RemoveLiteEntity(body);

        var members = Reflector.GetMemberListBase(body);
        if (members.Any(a => ignoreMembers.Contains(a.Name)))
            members = members.Where(a => !ignoreMembers.Contains(a.Name)).ToArray();

        if(members.FirstEx() is MethodInfo mi && mi.Name == nameof(SystemTimeExtensions.SystemPeriod))
        {
            var table = (ITable)finder;
            var sv = table.SystemVersioned;

            if (sv == null)
                throw new InvalidOperationException($"Table {table.Name} is not system versioned");

            if (members.Length == 1)
            {
                if (sv.PostgresSysPeriodColumnName != null)
                    return (null, [table.Columns[sv.PostgresSysPeriodColumnName!]]);
                else return (null, [
                    table.Columns[sv.StartColumnName!],
                    table.Columns[sv.EndColumnName!]
                ]);
            }else
            {
                var columnName = members[1].Name switch
                {
                    nameof(NullableInterval<DateTime>.Min) => sv.StartColumnName!,
                    nameof(NullableInterval<DateTime>.Max) => sv.EndColumnName!,
                    string other => throw new UnexpectedValueException(other)
                };

                return (null, [table.Columns[columnName]]);
            }
        }

        Field f = Schema.FindField(finder, members);

        if (type != null)
        {
            var ib = f as FieldImplementedBy;
            if (ib == null)
                throw new InvalidOperationException("Casting only supported for {0}".FormatWith(typeof(FieldImplementedBy).Name));

            var columns = (from ic in ib.ImplementationColumns
                           where type.IsAssignableFrom(ic.Key)
                           select (IColumn)ic.Value).ToArray();

            return (ib, columns);
        }

        return (f, TableIndex.GetColumnsFromField(f));
    }

    static Type? RemoveCasting(ref Expression body)
    {
        if (body.NodeType == ExpressionType.Convert && body.Type == typeof(object))
            body = ((UnaryExpression)body).Operand;

        Type? type = null;
        if ((body.NodeType == ExpressionType.Convert || body.NodeType == ExpressionType.TypeAs) &&
            body.Type != typeof(object))
        {
            type = body.Type;
            body = ((UnaryExpression)body).Operand;
        }
        
        return type;
    }
}

public class IndexWhereExpressionVisitor : ExpressionVisitor
{
    StringBuilder sb = new StringBuilder();

    IFieldFinder RootFinder;
    bool isPostgres;

    public IndexWhereExpressionVisitor(IFieldFinder rootFinder)
    {
        RootFinder = rootFinder;
        this.isPostgres = Schema.Current.Settings.IsPostgres;
    }

    public static string GetIndexWhere(LambdaExpression lambda, IFieldFinder rootFiender)
    {
        IndexWhereExpressionVisitor visitor = new IndexWhereExpressionVisitor(rootFiender);

        var newLambda = (LambdaExpression)ExpressionEvaluator.PartialEval(lambda);

        visitor.Visit(newLambda.Body);

        return visitor.sb.ToString();
    }

    public Field GetField(Expression exp)
    {
        if (exp.NodeType == ExpressionType.Convert)
            exp = ((UnaryExpression)exp).Operand;

        return Schema.FindField(RootFinder, Reflector.GetMemberListBase(exp));
    }


    [return: NotNullIfNotNull("exp")]
    public override Expression? Visit(Expression? exp)
    {
        switch (exp!.NodeType)
        {
            case ExpressionType.Conditional:
            case ExpressionType.Constant:
            case ExpressionType.Parameter:
            case ExpressionType.Call:
            case ExpressionType.Lambda:
            case ExpressionType.New:
            case ExpressionType.NewArrayInit:
            case ExpressionType.NewArrayBounds:
            case ExpressionType.Invoke:
            case ExpressionType.MemberInit:
            case ExpressionType.ListInit:
                throw new NotSupportedException("Expression of type {0} not supported: {1}".FormatWith(exp.NodeType, exp.ToString()));
            default:
                return base.Visit(exp);
        }
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression b)
    {
        var exp = RemoveLiteEntity(b.Expression);

        var f = GetField(exp);

        if (f is FieldReference fr)
        {
            if (b.TypeOperand.IsAssignableFrom(fr.FieldType))
                sb.Append(fr.Name.SqlEscape(isPostgres) + " IS NOT NULL");
            else
                throw new InvalidOperationException("A {0} will never be {1}".FormatWith(fr.FieldType.TypeName(), b.TypeOperand.TypeName()));

            return b;
        }

        if (f is FieldImplementedBy fib)
        {
            var typeOperant = b.TypeOperand.CleanType();

            var imp = fib.ImplementationColumns.Where(kvp => typeOperant.IsAssignableFrom(kvp.Key));

            if (imp.Any())
                sb.Append(imp.ToString(kvp => kvp.Value.Name.SqlEscape(isPostgres) + " IS NOT NULL", " OR "));
            else
                throw new InvalidOperationException("No implementation ({0}) will never be {1}".FormatWith(fib.ImplementationColumns.Keys.ToString(t => t.TypeName(), ", "), b.TypeOperand.TypeName()));

            return b;
        }

        throw new NotSupportedException("'is' only works with ImplementedBy or Reference fields");
    }

    public static Expression RemoveLiteEntity(Expression exp)
    {
        if (exp is MemberExpression m && m.Member is PropertyInfo pi && m.Expression!.Type.IsInstantiationOf(typeof(Lite<>)) &&
            (pi.Name == nameof(Lite<Entity>.Entity) || pi.Name == nameof(Lite<Entity>.EntityOrNull)))
            return m.Expression;
        return exp;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        var field = GetField(m);

        sb.Append(Equals(field, value: true, equals: true, isPostgres));

        return m;
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Not:
                sb.Append(" NOT ");
                this.Visit(u.Operand);
                break;
            case ExpressionType.Negate:
                sb.Append(" - ");
                this.Visit(u.Operand);
                break;
            case ExpressionType.UnaryPlus:
                sb.Append(" + ");
                this.Visit(u.Operand);
                break;
            case ExpressionType.Convert:
                //Las unicas conversiones explicitas son a Binary y desde datetime a numeros
                this.Visit(u.Operand);
                break;
            default:
                throw new NotSupportedException(string.Format("The unary perator {0} is not supported", u.NodeType));
        }
        return u;
    }


    public static string? IsNull(Field field, bool equals, bool isPostgres)
    {
        string isNull = equals ? "{0} IS NULL" : "{0} IS NOT NULL";

        if (field is IColumn col)
        {
            if (col.Nullable == IsNullable.No)
                return null;

            string result = isNull.FormatWith(col.Name.SqlEscape(isPostgres));

            if (!col.DbType.IsString())
                return result;

            return result + (equals ? " OR " : " AND ") + (col.Name.SqlEscape(isPostgres) + (equals ? " = " : " <> ") + "''");

        }
        else if (field is FieldImplementedBy ib)
        {
            if (ib.ImplementationColumns.Count == 0)
                return equals ? "TRUE" : "FALSE";

            if (ib.ImplementationColumns.Values.Only()?.Nullable == IsNullable.No)
                return null;

            return ib.ImplementationColumns.Values.ToString(ic => isNull.FormatWith(ic.Name.SqlEscape(isPostgres)), equals ? " AND " : " OR ");
        }
        else if (field is FieldImplementedByAll iba)
        {
            if(iba.TypeColumn.Nullable == IsNullable.No) 
                return null;

            return isNull.FormatWith(iba.TypeColumn);
        }
        else if (field is FieldEmbedded fe)
        {
            if (fe.HasValue == null)
                return null;

            return fe.HasValue.Name.SqlEscape(isPostgres) + (equals ? "<> 1" : " = 1");
        }

        throw new NotSupportedException(isNull.FormatWith(field.GetType()));
    }

    static string? Equals(Field field, object value, bool equals, bool isPostgres)
    {
        if (value == null)
        {
            return IsNull(field, equals, isPostgres);
        }
        else
        {
            if (field is IColumn)
            {
                return ((IColumn)field).Name.SqlEscape(isPostgres) +
                    (equals ? " = " : " <> ") + SqlPreCommandSimple.LiteralValue(value);
            }

            throw new NotSupportedException("Impossible to compare {0} to {1}".FormatWith(field, value));
        }
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
        if (b.NodeType == ExpressionType.Coalesce)
        {
            sb.Append("IsNull(");
            Visit(b.Left);
            sb.Append(',');
            Visit(b.Right);
            sb.Append(')');
        }
        else if (b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual)
        {
            if (b.Left is ConstantExpression)
            {
                if (b.Right is ConstantExpression)
                    throw new NotSupportedException("NULL == NULL not supported");

                Field field = GetField(b.Right);

                sb.Append(Equals(field, ((ConstantExpression)b.Left).Value!, b.NodeType == ExpressionType.Equal, isPostgres));
            }
            else if (b.Right is ConstantExpression)
            {
                Field field = GetField(b.Left);

                sb.Append(Equals(field, ((ConstantExpression)b.Right).Value!, b.NodeType == ExpressionType.Equal, isPostgres));
            }
            else
                throw new NotSupportedException("Impossible to translate {0}".FormatWith(b.ToString()));
        }
        else
        {
            sb.Append('(');
            this.Visit(b.Left);
            switch (b.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    sb.Append(b.Type.UnNullify() == typeof(bool) ? " AND " : " & ");
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    sb.Append(b.Type.UnNullify() == typeof(bool) ? " OR " : " | ");
                    break;
                case ExpressionType.ExclusiveOr:
                    sb.Append(" ^ ");
                    break;
                case ExpressionType.LessThan:
                    sb.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    sb.Append(" <= ");
                    break;
                case ExpressionType.GreaterThan:
                    sb.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    sb.Append(" >= ");
                    break;

                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    sb.Append(" + ");
                    break;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    sb.Append(" - ");
                    break;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    sb.Append(" * ");
                    break;
                case ExpressionType.Divide:
                    sb.Append(" / ");
                    break;
                case ExpressionType.Modulo:
                    sb.Append(" % ");
                    break;
                default:
                    throw new NotSupportedException(string.Format("The binary operator {0} is not supported", b.NodeType));
            }
            this.Visit(b.Right);
            sb.Append(')');
        }
        return b;
    }
}
