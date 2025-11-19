using Microsoft.SqlServer.Types;
using NpgsqlTypes;
using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Collections.ObjectModel;
using System.Data;


namespace Signum.Engine.Linq;


/// <summary>
/// Extended node types for custom expressions
/// </summary>
internal enum DbExpressionType
{
    Table = 1000, // make sure these don't overlap with ExpressionType
    Column,
    Select,
    Projection,
    ChildProjection,
    Join,
    SetOperator,
    Aggregate,
    AggregateRequest,
    SqlFunction,
    SqlTableValuedFunction,
    SqlConstant,
    SqlVariable,
    SqlLiteral,
    SqlColumnList,
    SqlCast,
    Case,
    RowNumber,
    Like,
    In,
    Exists,
    Scalar,
    IsNull,
    IsNotNull,
    Update,
    Delete,
    InsertSelect,
    CommandAggregate,
    Entity = 2000,
    EmbeddedInit,
    MixinInit,
    ImplementedBy,
    ImplementedByAll,
    LiteReference,
    LiteValue,
    TypeEntity,
    TypeImplementedBy,
    TypeImplementedByAll,
    MList,
    MListProjection,
    MListElement,
    AdditionalField,
    PrimaryKey,
    ToDayOfWeek,
    Interval,
    IsDescendatOf
}


internal abstract class DbExpression : Expression
{
    readonly Type type;
    public override Type Type
    {
        get { return type; }
    }

    readonly DbExpressionType dbNodeType;
    public DbExpressionType DbNodeType
    {
        get { return dbNodeType; }
    }

    public override ExpressionType NodeType
    {
        get { return ExpressionType.Extension; }
    }

    protected DbExpression(DbExpressionType nodeType, Type type)
    {
        this.type = type;
        this.dbNodeType = nodeType;
    }

    public abstract override string ToString();

    protected abstract Expression Accept(DbExpressionVisitor visitor);

    protected override Expression Accept(ExpressionVisitor visitor)
    {
        if (visitor is DbExpressionVisitor dbVisitor)
            return Accept(dbVisitor);

        return base.Accept(visitor);
    }
}

internal abstract class SourceExpression : DbExpression
{
    public abstract Alias[] KnownAliases { get; }

    public SourceExpression(DbExpressionType nodeType)
        : base(nodeType, typeof(void))
    {
    }
}

internal abstract class SourceWithAliasExpression : SourceExpression
{
    public readonly Alias Alias;

    public SourceWithAliasExpression(DbExpressionType nodeType, Alias alias)
        : base(nodeType)
    {
        this.Alias = alias;
    }
}


internal class SqlTableValuedFunctionExpression : SourceWithAliasExpression
{
    public readonly Table? ViewTable;
    public readonly Type? SingleColumnType; 
    public readonly ReadOnlyCollection<Expression> Arguments;
    public readonly string FunctionName; 

    public override Alias[] KnownAliases
    {
        get { return new[] { Alias }; }
    }

    public SqlTableValuedFunctionExpression(string functionName, Table? viewTable, Type? singleColumnType, Alias alias, IEnumerable<Expression> arguments)
        : base(DbExpressionType.SqlTableValuedFunction, alias)
    {
        if ((viewTable == null) == (singleColumnType == null))
            throw new ArgumentException("Either viewTable or singleColumn should be set");

        this.FunctionName = functionName;
        this.ViewTable = viewTable;
        this.SingleColumnType = singleColumnType;
        this.Arguments = arguments.ToReadOnly();
    }

    public override string ToString()
    {
        string result = "{0}({1}) as {2}".FormatWith(FunctionName, Arguments.ToString(a => a.ToString(), ","), Alias);

        return result;
    }

    internal ColumnExpression GetIdExpression()
    {
        if (ViewTable != null)
        {

            var expression = ((ITablePrivate)ViewTable).GetPrimaryOrder(Alias);

            if (expression == null)
                throw new InvalidOperationException("Impossible to determine Primary Key for {0}".FormatWith(ViewTable.Name));

            return expression;
        }
        else
        {
            return new ColumnExpression(this.SingleColumnType!, Alias, null);
        }
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitSqlTableValuedFunction(this);
    }
}

internal class TableExpression : SourceWithAliasExpression
{
    public readonly ITable Table;

    public ObjectName Name { get { return SystemTime is SystemTime.HistoryTable ? Table.SystemVersioned!.TableName : Table.Name; } }

    public SystemTime? SystemTime { get; private set; }

    public readonly string? WithHint;

    public override Alias[] KnownAliases
    {
        get { return new[] { Alias }; }
    }

    internal TableExpression(Alias alias, ITable table, SystemTime? systemTime, string? withHint)
        : base(DbExpressionType.Table, alias)
    {
        this.Table = table;
        this.SystemTime = systemTime;
        this.WithHint = withHint;
    }

    public override string ToString()
    {
        var st = SystemTime != null && !(SystemTime is SystemTime.HistoryTable) ? " FOR SYSTEM_TIME " + SystemTime.ToString() : null;

        return $"{Name}{st} as {Alias}";
    }

    internal ColumnExpression GetIdExpression()
    {
        var expression = ((ITablePrivate)Table).GetPrimaryOrder(Alias);

        if (expression == null)
            throw new InvalidOperationException("Impossible to determine Primary Key for {0}".FormatWith(Name));

        return expression;
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitTable(this);
    }
}

internal class ColumnExpression : DbExpression, IEquatable<ColumnExpression>
{
    public readonly Alias Alias;
    public readonly string? Name;

    internal ColumnExpression(Type type, Alias alias, string? name)
        : base(DbExpressionType.Column, type)
    {
        if (type.UnNullify() == typeof(PrimaryKey))
            throw new ArgumentException("type should not be PrimaryKey");

        this.Alias = alias ?? throw new ArgumentNullException(nameof(alias));
        this.Name = name ?? (Schema.Current.Settings.IsPostgres ? (string?)null : throw new ArgumentNullException(nameof(name)));
    }

    public override string ToString()
    {
        return "{0}.{1}".FormatWith(Alias, Name);
    }

    public override bool Equals(object? obj) => obj is ColumnExpression ce && Equals(ce);
    public bool Equals(ColumnExpression? other)
    {
        return other != null && other.Alias == Alias && other.Name == Name;
    }

    public override int GetHashCode()
    {
        return Alias.GetHashCode() ^ (Name?.GetHashCode() ?? -1);
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitColumn(this);
    }
}

internal class ColumnDeclaration
{
    public readonly string Name;
    public readonly Expression Expression;
    internal ColumnDeclaration(string name, Expression expression)
    {
        this.Name = name;
        this.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public override string ToString()
    {
        if (Name.HasText())
            return "{0} = {1}".FormatWith(Name, Expression.ToString());

        return Expression.ToString();
    }

    public ColumnExpression GetReference(Alias alias)
    {
        return new ColumnExpression(Expression.Type, alias, Name);
    }
}

internal enum AggregateSqlFunction
{
    Average,
    StdDev,
    StdDevP,
    Count,
    CountDistinct,
    Min,
    Max,
    Sum,

    string_agg,
}

static class AggregateSqlFunctionExtensions
{
    public static bool OrderMatters(this AggregateSqlFunction aggregateFunction)
    {
        return aggregateFunction switch
        {
            AggregateSqlFunction.Average or 
            AggregateSqlFunction.StdDev or 
            AggregateSqlFunction.StdDevP or 
            AggregateSqlFunction.Count or 
            AggregateSqlFunction.CountDistinct or
            AggregateSqlFunction.Min or 
            AggregateSqlFunction.Max or 
            AggregateSqlFunction.Sum => false,

            AggregateSqlFunction.string_agg => true,
            _ => throw new UnexpectedValueException(aggregateFunction),
        };
    }
}

internal class AggregateExpression : DbExpression
{
    public readonly AggregateSqlFunction AggregateFunction;
    public readonly ReadOnlyCollection<Expression> Arguments;
    public readonly ReadOnlyCollection<OrderExpression>? OrderBy;
    public AggregateExpression(Type type, AggregateSqlFunction aggregateFunction, IEnumerable<Expression> arguments, IEnumerable<OrderExpression>? orderBy)
        : base(DbExpressionType.Aggregate, type)
    {
        if (arguments == null)
            throw new ArgumentNullException(nameof(arguments));
        
        this.AggregateFunction = aggregateFunction;
        this.Arguments = arguments.ToReadOnly();
        this.OrderBy = orderBy?.ToReadOnly();
    }

    public override string ToString()
    {
        var result =  $"{AggregateFunction}({(AggregateFunction == AggregateSqlFunction.CountDistinct ? "Distinct " : "")}{Arguments.ToString(", ") ?? "*"})";

        if (OrderBy == null)
            return result;

        return result + "WITHIN GROUP (" + OrderBy.ToString(", ") + ")";
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitAggregate(this);
    }
}

internal class OrderExpression
{
    public readonly OrderType OrderType;
    public readonly Expression Expression;

    internal OrderExpression(OrderType orderType, Expression expression)
    {
        this.OrderType = orderType;
        this.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public override string ToString()
    {
        return "{0} {1}".FormatWith(Expression.ToString(), OrderType == OrderType.Ascending ? "ASC" : "DESC");
    }
}

[Flags]
internal enum SelectRoles
{
    Where = 1,
    Aggregate = 2,
    GroupBy = 4,
    OrderBy = 8,
    Select = 16,
    Distinct = 32,
    Top = 64
}

internal enum SelectOptions
{
    Reverse = 1,
    ForXmlPathEmpty = 2,
    OrderAlsoByKeys = 4,
    HasIndex = 8,
}

internal class SelectExpression : SourceWithAliasExpression
{
    public readonly ReadOnlyCollection<ColumnDeclaration> Columns;
    public readonly SourceExpression? From;
    public readonly Expression? Where;
    public readonly ReadOnlyCollection<OrderExpression> OrderBy;
    public readonly ReadOnlyCollection<Expression> GroupBy;
    public readonly Expression? Top;
    public readonly bool IsDistinct;
    public readonly SelectOptions SelectOptions;

    public bool IsReverse
    {
        get { return (SelectOptions & Linq.SelectOptions.Reverse) != 0; }
    }

    public bool IsForXmlPathEmpty
    {
        get { return (SelectOptions & Linq.SelectOptions.ForXmlPathEmpty) != 0; }
    }

    public bool IsOrderAlsoByKeys
    {
        get { return (SelectOptions & Linq.SelectOptions.OrderAlsoByKeys) != 0; }
    }

    public bool HasIndex
    {
        get { return (SelectOptions & Linq.SelectOptions.HasIndex) != 0; }
    }


    readonly Alias[] knownAliases;
    public override Alias[] KnownAliases
    {
        get { return knownAliases; }
    }

    internal SelectExpression(Alias alias, bool distinct, Expression? top, IEnumerable<ColumnDeclaration>? columns, SourceExpression? from, Expression? where, IEnumerable<OrderExpression>? orderBy, IEnumerable<Expression>? groupBy, SelectOptions options)
        : base(DbExpressionType.Select, alias)
    {
        this.IsDistinct = distinct;
        this.SelectOptions = options;
        this.Top = top;
        this.Columns = columns.ToReadOnly();
        this.From = from;
        this.Where = where;
        this.OrderBy = orderBy.ToReadOnly();
        this.GroupBy = groupBy.ToReadOnly();
        this.knownAliases = from == null ? new[] { alias } : from.KnownAliases.And(alias).ToArray();
    }

    internal SelectRoles SelectRoles
    {
        get
        {
            SelectRoles roles = (SelectRoles)0;

            if (Where != null)
                roles |= SelectRoles.Where;

            if (GroupBy.Count > 0)
                roles |= SelectRoles.GroupBy;
            else if (AggregateFinder.GetAggregates(Columns) != null)
                roles |= SelectRoles.Aggregate;

            if (OrderBy.Count > 0)
                roles |= SelectRoles.OrderBy;

            if (!Columns.All(cd => cd.Expression is ColumnExpression))
                roles |= SelectRoles.Select;

            if (IsDistinct)
                roles |= SelectRoles.Distinct;

            if (Top != null)
                roles |= SelectRoles.Top;

            return roles;
        }
    }

    public bool IsAllAggregates  => Columns.Any() && Columns.All(a => a.Expression is AggregateExpression ag && !ag.AggregateFunction.OrderMatters());

    public override string ToString()
    {
        return "SELECT {0}{1}\n{2}\nFROM {3}\n{4}{5}{6}{7} AS {8}".FormatWith(
            IsDistinct ? "DISTINCT " : "",
            Top?.Let(t => "TOP {0} ".FormatWith(t.ToString())),
            Columns.ToString(c => c.ToString().Indent(4), ",\n"),
            From?.Let(f => f.ToString().Let(a => a.Contains("\n") ? "\n" + a.Indent(4) : a)),
            Where?.Let(a => "WHERE " + a.ToString() + "\n"),
            OrderBy.Any() ? ("ORDER BY " + OrderBy.ToString(" ,") + "\n") : null,
            GroupBy.Any() ? ("GROUP BY " + GroupBy.ToString(g => g.ToString(), " ,") + "\n") : null,
            SelectOptions == 0 ? "" : SelectOptions.ToString() + "\n",
            Alias);
    }

    internal bool IsOneRow()
    {

        if (Top is ConstantExpression ce && ((int)ce.Value!) == 1)
            return true;

        return false;
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitSelect(this);
    }
}

internal enum JoinType
{
    CrossJoin,
    InnerJoin,
    CrossApply,
    OuterApply,
    LeftOuterJoin,
    SingleRowLeftOuterJoin,
    RightOuterJoin,
    FullOuterJoin,
}

internal class JoinExpression : SourceExpression
{
    public readonly JoinType JoinType;
    public readonly SourceExpression Left;
    public readonly SourceExpression Right;
    public new readonly Expression? Condition;

    public override Alias[] KnownAliases
    {
        get { return Left.KnownAliases.Concat(Right.KnownAliases).ToArray(); }
    }

    internal JoinExpression(JoinType joinType, SourceExpression left, SourceExpression right, Expression? condition)
        : base(DbExpressionType.Join)
    {
        if (condition == null && joinType != JoinType.CrossApply && joinType != JoinType.OuterApply && joinType != JoinType.CrossJoin)
            throw new ArgumentNullException(nameof(condition));

        this.JoinType = joinType;
        this.Left = left ?? throw new ArgumentNullException(nameof(left));
        this.Right = right ?? throw new ArgumentNullException(nameof(right));
        this.Condition = condition;
    }

    public override string ToString()
    {
        return "{0}\n{1}\n{2}\nON {3}".FormatWith(Left.ToString().Indent(4), JoinType, Right.ToString().Indent(4), Condition?.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitJoin(this);
    }
}

internal enum SetOperator
{
    Union,
    UnionAll,
    Intersect,
    Except
}

internal class SetOperatorExpression : SourceWithAliasExpression
{
    public readonly SetOperator Operator;
    public readonly SourceWithAliasExpression Left;
    public readonly SourceWithAliasExpression Right;

    public override Alias[] KnownAliases
    {
        get { return Left.KnownAliases.Concat(Right.KnownAliases).PreAnd(Alias).ToArray(); }
    }

    internal SetOperatorExpression(SetOperator @operator, SourceWithAliasExpression left, SourceWithAliasExpression right, Alias alias)
        : base(DbExpressionType.SetOperator, alias)
    {
        this.Operator = @operator;
        this.Left = Validate(left, nameof(left));
        this.Right = Validate(right, nameof(right));
    }

    static SourceWithAliasExpression Validate(SourceWithAliasExpression exp, string name)
    {
        if (exp == null)
            throw new ArgumentNullException(name);

        if (exp is TableExpression || exp is SqlTableValuedFunctionExpression)
            throw new ArgumentException($"{name} should not be a {exp.GetType().Name}");

        return exp;
    }

    public override string ToString()
    {
        return "{0}\n{1}\n{2}\n as {3}".FormatWith(Left.ToString().Indent(4), Operator, Right.ToString().Indent(4), Alias);
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitSetOperator(this);
    }
}

internal enum SqlFunction
{
    CHARINDEX,
    LEFT,
    LEN,
    LOWER,
    LTRIM,
    REPLACE,
    REPLICATE,
    REVERSE,
    RIGHT,
    RTRIM,
    SUBSTRING,
    UPPER,

    ABS,
    PI,
    SIN,
    ASIN,
    COS,
    ACOS,
    TAN,
    ATAN,
    ATN2,
    POWER,
    SQRT,

    EXP,
    SQUARE,
    LOG10,
    LOG,

    FLOOR,
    CEILING,
    ROUND,
    SIGN,

    DAY,
    MONTH,
    YEAR,
    DATEPART,
    DATEDIFF,
    DATEDIFF_BIG,
    DATEADD,

    COALESCE,
    CONVERT,
    STUFF,
    COLLATE,
    CONCAT,
    SwitchOffset,

    CONTAINS,
    CONTAINSTABLE,
    FREETEXT,
    FREETEXTTABLE,
    DATETRUNC,
    AtTimeZone,
}

internal enum PostgresFunction
{
    strpos,
    starts_with,
    length,
    EXTRACT,
    trunc,
    substr,
    repeat,
    date_trunc,
    age,
    tstzrange,
    upper,
    lower,
    subpath,
    nlevel,
    to_tsquery,
    plainto_tsquery,
    phraseto_tsquery,
    websearch_to_tsquery,

    ts_rank,
    ts_rank_cd,
}

public static class PostgressOperator
{
    public static string Overlap = "&&";
    public static string Contains = "@>";
    public static string IsContained = "<@";
    public static string Matches = "@@";
    public static string Minus = "-";

    public static string[] All = new[] { Overlap, Contains, IsContained, Matches, Minus };
}

internal enum SqlEnums
{
    year,
    month,
    quarter,
    day,
    week,
    weekday, //Sql Server
    dow,     //Postgres
    hour,
    minute,
    second,
    millisecond,
    dayofyear, //SQL Server
    doy,       //Postgres
    epoch
}

internal class SqlLiteralExpression : DbExpression
{
    public readonly string Value;
    public SqlLiteralExpression(SqlEnums value) : this(typeof(object), value.ToString()) { }
    public SqlLiteralExpression(Type type, string value)
        : base(DbExpressionType.SqlLiteral, type)
    {
        this.Value = value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitSqlLiteral(this);
    }
}

internal class SqlColumnListExpression : DbExpression
{
    public readonly ReadOnlyCollection<ColumnExpression> Columns;
    public SqlColumnListExpression(IEnumerable<ColumnExpression> column)
        : base(DbExpressionType.SqlLiteral, typeof(void))
    {
        this.Columns = column.ToReadOnly();
    }

    public override string ToString()
    {
        var only = Columns.Only();
        if (only != null)
            return only.ToString();

        return "(" + Columns.ToString(", ") + ")";
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitSqlColumnList(this);
    }
}

internal class ToDayOfWeekExpression : DbExpression
{
    public readonly Expression Expression;
    public ToDayOfWeekExpression(Expression expression)
        : base(DbExpressionType.ToDayOfWeek, typeof(DayOfWeek?))
    {
        if (expression.Type != typeof(int?))
            throw new InvalidOperationException("int? expected");

        this.Expression = expression;
    }
    public override string ToString()
    {
        return "ToDayOfWeek(" + Expression.ToString() + ")";
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitToDayOfWeek(this);
    }

    internal static MethodInfo miToDayOfWeekPostgres = ReflectionTools.GetMethodInfo(() => ToDayOfWeekPostgres(1));
    public static DayOfWeek? ToDayOfWeekPostgres(int? postgressWeekDay)
    {
        if (postgressWeekDay == null)
            return null;

        return (DayOfWeek)(postgressWeekDay);
    }


    internal static MethodInfo miToDayOfWeekSql = ReflectionTools.GetMethodInfo(() => ToDayOfWeekSql(1, 1));
    public static DayOfWeek? ToDayOfWeekSql(int? sqlServerWeekDay, byte dateFirst)
    {
        if (sqlServerWeekDay == null)
            return null;

        return (DayOfWeek)((dateFirst + sqlServerWeekDay.Value - 1) % 7);
    }

    public static int ToSqlWeekDay(DayOfWeek dayOfWeek, byte dateFirst /*keep parameter here to evaluate now*/)
    {
        return (((int)dayOfWeek - dateFirst + 7) % 7) + 1;
    }
}

internal class SqlCastExpression : DbExpression
{
    public readonly AbstractDbType DbType;
    public readonly Expression Expression;

    public SqlCastExpression(Type type, Expression expression)
        : this(type, expression, Schema.Current.Settings.DefaultSqlType(type.UnNullify()))
    {
    }

    public SqlCastExpression(Type type, Expression expression, AbstractDbType dbType)
        : base(DbExpressionType.SqlCast, type)
    {
        this.Expression = expression;
        this.DbType = dbType;
    }

    public override string ToString()
    {
        return "Cast({0} as {1})".FormatWith(Expression.ToString(), DbType.ToString(Schema.Current.Settings.IsPostgres));
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitSqlCast(this);
    }
}

internal class SqlFunctionExpression : DbExpression
{
    public readonly Expression? Object;
    public readonly string SqlFunction;
    public readonly ReadOnlyCollection<Expression> Arguments;

    public SqlFunctionExpression(Type type, Expression? obj, string sqlFunction, IEnumerable<Expression> arguments)
        : base(DbExpressionType.SqlFunction, type)
    {
        this.SqlFunction = sqlFunction;
        this.Object = obj;
        this.Arguments = arguments.ToReadOnly();
    }

    public override string ToString()
    {
        string result = "{0}({1})".FormatWith(SqlFunction, Arguments.ToString(a => a.ToString(), ","));
        if (Object == null)
            return result;
        return Object.ToString() + "." + result;
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitSqlFunction(this);
    }
}

internal class SqlConstantExpression : DbExpression
{
    public readonly object? Value;

    public SqlConstantExpression(object value)
        : this(value, value.GetType())
    {
    }

    public SqlConstantExpression(object? value, Type type)
        : base(DbExpressionType.SqlConstant, type)
    {
        this.Value = value;
    }

    public override string ToString()
    {
        return Value?.ToString() ?? "NULL";
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitSqlConstant(this);
    }
}

internal class SqlVariableExpression : DbExpression
{
    public readonly string VariableName;

    public SqlVariableExpression(string variableName, Type type)
        : base(DbExpressionType.SqlConstant, type)
    {
        this.VariableName = variableName;
    }

    public override string ToString()
    {
        return VariableName;
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitSqlVariable(this);
    }
}

internal class When
{
    public readonly Expression Condition;
    public readonly Expression Value;

    public When(Expression condition, Expression value)
    {
        if (condition == null)
            throw new ArgumentNullException(nameof(condition));

        if (condition.Type.UnNullify() != typeof(bool))
            throw new ArgumentException("condition should be boolean");

        this.Condition = condition;
        this.Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override string ToString()
    {
        return "  WHEN {0} THEN {1}".FormatWith(Condition.ToString(), Value.ToString());
    }
}

internal static class ExpressionTools
{
    public static Expression ToCondition(this IEnumerable<When> whens, Type returnType)
    {
        var @default = returnType.IsClass || returnType.IsNullable() ?
                            Expression.Constant(null, returnType) :
                            Convert(Expression.Constant(null, returnType.Nullify()), returnType);

        var result = whens.Reverse().Aggregate(
            @default, (acum, when) => Expression.Condition(when.Condition, Convert(when.Value, returnType), acum));

        return result;
    }

    public static Expression Convert(Expression expression, Type returnType)
    {
        if (expression.Type == returnType)
            return expression;

        if (expression.Type.Nullify() == returnType)
            return expression.Nullify();

        if (expression.Type.UnNullify() == returnType)
            return expression.UnNullify();

        if (returnType.IsAssignableFrom(expression.Type) || expression.Type.IsAssignableFrom(returnType))
            return Expression.Convert(expression, returnType);

        throw new InvalidOperationException("Imposible to convert to {0} the expression: \n{1}"
            .FormatWith(returnType.TypeName(), expression.ToString()));
    }

    public static Expression NotEqualsNulll(this Expression exp)
    {
        return Expression.NotEqual(exp.Nullify(), Expression.Constant(null, typeof(int?)));
    }
}

internal class CaseExpression : DbExpression
{
    public readonly ReadOnlyCollection<When> Whens;
    public readonly Expression? DefaultValue;
    
    public CaseExpression(IEnumerable<When> whens, Expression? defaultValue)
        : base(DbExpressionType.Case, GetType(whens, defaultValue))
    {
        if (whens.IsEmpty())
            throw new ArgumentNullException(nameof(whens));

        Type refType = this.Type.UnNullify();

        if (whens.Any(w => w.Value.Type.UnNullify() != refType))
            throw new ArgumentException("inconsistent whens");

        this.Whens = whens.ToReadOnly();
        this.DefaultValue = defaultValue;
    }

    static Type GetType(IEnumerable<When> whens, Expression? defaultValue)
    {
        var types = whens.Select(w => w.Value.Type).ToList();
        if (defaultValue != null)
            types.Add(defaultValue.Type);

        if (types.Any(a => a.IsNullable()))
            types = types.Select(ReflectionExtensions.Nullify).ToList();

        return types.Distinct().SingleEx();
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("CASE");
        foreach (var w in Whens)
            sb.AppendLine(w.ToString());

        if (DefaultValue != null)
            sb.AppendLine(DefaultValue.ToString());

        sb.AppendLine("END");
        return sb.ToString();
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitCase(this);
    }
}

internal class IntervalExpression : DbExpression
{
    public readonly Expression? Min;
    public readonly Expression? Max;
    public readonly Expression? PostgresRange;

    public readonly Type ElementType;

    public IntervalExpression(Type type, Expression? min, Expression? max, Expression? postgresRange)
        :base(DbExpressionType.Interval, type)
    {
#pragma warning disable IDE0075 // Simplify conditional expression
        var isNullable =
             type.IsInstantiationOf(typeof(NullableInterval<>)) ? true :
             type.IsInstantiationOf(typeof(Interval<>)) ? false :
             throw new UnexpectedValueException(type);
#pragma warning restore IDE0075 // Simplify conditional expression

        this.ElementType = isNullable ?  type.GetGenericArguments()[0].Nullify() : type.GetGenericArguments()[0];
        
        if (postgresRange == null)
        {
            this.Min = min == null ? throw new ArgumentNullException(nameof(min)) :
                min.Type != ElementType ? throw new ArgumentException($"{nameof(min)} should be a {ElementType.TypeName()}"): 
                min;


            this.Max = max == null ? throw new ArgumentNullException(nameof(max)) :
                max.Type != ElementType ? throw new ArgumentException($"{nameof(max)} should be a {ElementType.TypeName()}") :
                max;
        }
        else
        {
            var rangeType = typeof(NpgsqlRange<>).MakeGenericType(type.GetGenericArguments()[0]);

            if (min != null || max != null)
                throw new InvalidOperationException($"{nameof(min)} and {nameof(max)} should be null if {nameof(postgresRange)} is used");

            this.PostgresRange = postgresRange.Type != rangeType ? throw new ArgumentException($"{nameof(postgresRange)} should be a {rangeType.TypeName()}") :
                postgresRange;
        }
    }

    public override string ToString()
    {
        var type = this.Type.GetGenericArguments()[0].TypeName();

        if (PostgresRange != null)
            return $"new Interval<{type}>({this.PostgresRange})";
        else
            return $"new Interval<{type}>({this.Min}, {this.Max})";
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitInterval(this);
    }
}

public static class SystemTimeExpressions
{
    internal static Expression? Overlaps(this IntervalExpression? interval1, IntervalExpression? interval2)
    {
        if (interval1 == null)
            return null;

        if (interval2 == null)
            return null;

        if(interval1.PostgresRange != null)
        {
            return new SqlFunctionExpression(typeof(bool), null, "&&", new Expression[] { interval1.PostgresRange!, interval2.PostgresRange! });
        }

        var min1 = interval1.Min!;
        var max1 = interval1.Max!;
        var min2 = interval2.Min!;
        var max2 = interval2.Max!;

        return Expression.And(
             Expression.GreaterThan(max1, min2),
             Expression.GreaterThan(max2, min1)
             );
    }

    internal static Expression? Contains(this IntervalExpression interval, Expression expression)
    {
        if (interval.PostgresRange != null)
        {
            return new SqlFunctionExpression(typeof(bool), null, "@>", new Expression[] { interval.PostgresRange!, expression! });
        }

        return Expression.And(
             Expression.LessThanOrEqual(interval.Min!, expression.Nullify()),
             Expression.LessThan(expression.Nullify(), interval.Max!)
             );
    }

    public static Expression And(this Expression expression, Expression? other)
    {
        if (other == null)
            return expression;

        return Expression.And(expression, other);
    }
}

internal class LikeExpression : DbExpression
{
    public readonly Expression Expression;
    public readonly Expression Pattern;

    public LikeExpression(Expression expression, Expression pattern)
        : base(DbExpressionType.Like, typeof(bool))
    {
        if (expression == null || expression.Type != typeof(string))
            throw new ArgumentException("expression is wrong");

        if (pattern == null || pattern.Type != typeof(string))
            throw new ArgumentException("pattern is wrong");
        this.Expression = expression;
        this.Pattern = pattern;
    }

    public override string ToString()
    {
        return "{0} LIKE {1}".FormatWith(Expression.ToString(), Pattern.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitLike(this);
    }
}

internal class IsDesendantOfExpression : DbExpression
{
    public readonly Expression Child;
    public readonly Expression Parent;

    public IsDesendantOfExpression(Expression child, Expression parent) : base(DbExpressionType.IsDescendatOf, typeof(bool))
    {
        if (child == null || child.Type != typeof(SqlHierarchyId))
            throw new ArgumentException("expression is wrong");

        if (parent == null || parent.Type != typeof(SqlHierarchyId))
            throw new ArgumentException("pattern is wrong");
        this.Child = child;
        this.Parent = parent;
    }

    public override string ToString()
    {
        return $"{Child} <@ {Parent}";
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitIsDescendatOf(this);
    }
}

internal abstract class SubqueryExpression : DbExpression
{
    public readonly SelectExpression? Select;
    protected SubqueryExpression(DbExpressionType nodeType, Type type, SelectExpression? select)
        : base(nodeType, type)
    {
        System.Diagnostics.Debug.Assert(nodeType == DbExpressionType.Scalar || nodeType == DbExpressionType.Exists || nodeType == DbExpressionType.In);
        this.Select = select;
    }
}

internal class ScalarExpression : SubqueryExpression
{
    public ScalarExpression(Type type, SelectExpression select)
        : base(DbExpressionType.Scalar, type, select)
    {
    }

    public override string ToString()
    {
        return "SCALAR({0})".FormatWith(Select!.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitScalar(this);
    }
}

internal class IsNullExpression : DbExpression
{
    public readonly Expression Expression;

    public IsNullExpression(Expression expression)
        : base(DbExpressionType.IsNull, typeof(bool))
    {
        this.Expression = expression;
    }

    public override string ToString()
    {
        return "{0} IS NULL".FormatWith(Expression.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitIsNull(this);
    }
}

internal class IsNotNullExpression : DbExpression
{
    public readonly Expression Expression;

    public IsNotNullExpression(Expression expression)
        : base(DbExpressionType.IsNotNull, typeof(bool))
    {
        this.Expression = expression;
    }

    public override string ToString()
    {
        return "{0} IS NOT NULL".FormatWith(Expression.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitIsNotNull(this);
    }
}

internal class ExistsExpression : SubqueryExpression
{
    public ExistsExpression(SelectExpression select)
        : base(DbExpressionType.Exists, typeof(bool), select)
    {
    }

    public override string ToString()
    {
        return "EXIST({0})".FormatWith(Select!.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitExists(this);
    }
}

internal class InExpression : SubqueryExpression
{
    public readonly Expression Expression;
    public readonly object[]? Values;

    public InExpression(Expression expression, SelectExpression select)
        : base(DbExpressionType.In, typeof(bool), select)
    {
        if (select == null) throw new ArgumentNullException(nameof(@select));

        this.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public static Expression FromValues(Expression expression, object[] values)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        if (values == null) throw new ArgumentNullException(nameof(values));

        if (values.Length == 0)
            return Expression.Constant(false);

        return new InExpression(expression, values);
    }

    InExpression(Expression expression, object[] values)
        : base(DbExpressionType.In, typeof(bool), null)
    {
        this.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        this.Values = values ?? throw new ArgumentNullException(nameof(values));
    }

    public override string ToString()
    {
        if (Values == null)
            return "{0} IN ({1})".FormatWith(Expression.ToString(), Select!.ToString());
        else
            return "{0} IN ({1})".FormatWith(Expression.ToString(), Values.ToString(", "));
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitIn(this);
    }
}

internal class AggregateRequestsExpression : DbExpression
{
    public readonly Alias GroupByAlias;
    public readonly AggregateExpression Aggregate;
    public AggregateRequestsExpression(Alias groupByAlias, AggregateExpression aggregate)
        : base(DbExpressionType.AggregateRequest, aggregate.Type)
    {
        this.Aggregate = aggregate;
        this.GroupByAlias = groupByAlias;
    }

    public override string ToString()
    {
        return "AggregateRequest OF {0}({1})".FormatWith(GroupByAlias, Aggregate);
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitAggregateRequest(this);
    }
}

internal class RowNumberExpression : DbExpression
{
    public readonly ReadOnlyCollection<OrderExpression> OrderBy;

    public RowNumberExpression(IEnumerable<OrderExpression>? orderBy)
        : base(DbExpressionType.RowNumber, typeof(int))
    {
        this.OrderBy = orderBy.ToReadOnly();
    }

    public override string ToString()
    {
        return "ROW_NUMBER()";
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitRowNumber(this);
    }
}

internal enum UniqueFunction
{
    First,
    FirstOrDefault,
    Single,
    SingleOrDefault,
}

/// <summary>
/// A custom expression representing the construction of one or more result objects from a
/// SQL select expression
/// </summary>
internal class ProjectionExpression : DbExpression
{
    public readonly SelectExpression Select;
    public readonly Expression Projector;
    public readonly UniqueFunction? UniqueFunction;

    internal ProjectionExpression(SelectExpression select, Expression projector, UniqueFunction? uniqueFunction, Type resultType)
        : base(DbExpressionType.Projection, resultType)
    {
        if (projector == null)
            throw new ArgumentNullException(nameof(projector));

        var elementType = uniqueFunction == null ? resultType.ElementType()! : resultType;
        if (!elementType.IsAssignableFrom(projector.Type))
            throw new InvalidOperationException("Projector ({0}) does not fit in the projection ({1})".FormatWith(
                projector.Type.TypeName(),
                elementType.TypeName()));

        this.Select = select ?? throw new ArgumentNullException(nameof(@select));
        this.Projector = projector;
        this.UniqueFunction = uniqueFunction;
    }

    public override string ToString()
    {
        return "(SOURCE\n{0}\nPROJECTOR\n{1})".FormatWith(Select.ToString().Indent(4), Projector.ToString().Indent(4));
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitProjection(this);
    }
}

internal class ChildProjectionExpression : DbExpression
{
    public readonly ProjectionExpression Projection;
    public readonly Expression OuterKey;
    public readonly bool IsLazyMList;
    public readonly LookupToken Token;

    internal ChildProjectionExpression(ProjectionExpression projection, Expression outerKey, bool isLazyMList, Type type, LookupToken token)
        : base(DbExpressionType.ChildProjection, type)
    {
        this.Projection = projection;
        this.OuterKey = outerKey;
        this.IsLazyMList = isLazyMList;
        this.Token = token;
    }

    public override string ToString()
    {
        return "{0}.InLookup({1})".FormatWith(Projection.ToString(), OuterKey.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitChildProjection(this);
    }
}

internal abstract class CommandExpression : DbExpression
{
    public CommandExpression(DbExpressionType nodeType)
        : base(nodeType, typeof(void))
    {
    }
}

internal class DeleteExpression : CommandExpression
{
    public readonly ITable Table;
    public readonly bool UseHistoryTable;
    public ObjectName Name { get { return UseHistoryTable ? Table.SystemVersioned!.TableName : Table.Name; } }

    public readonly Alias? Alias;
    public readonly SourceWithAliasExpression Source;
    public readonly Expression? Where;
    public readonly bool ReturnRowCount;

    public DeleteExpression(ITable table, bool useHistoryTable, SourceWithAliasExpression source, Expression? where, bool returnRowCount, Alias? alias)
        : base(DbExpressionType.Delete)
    {
        this.Table = table;
        this.UseHistoryTable = useHistoryTable;
        this.Source = source;
        this.Where = where;
        this.ReturnRowCount = returnRowCount;
        this.Alias = alias;
    }

    public override string ToString()
    {
        return "DELETE FROM {0}\nFROM {1}\n{2}".FormatWith(
            (object?)Alias ?? Name,
            Source.ToString(),
            Where?.Let(w => "WHERE " + w.ToString())) + 
            (ReturnRowCount ? "\nSELECT @@rowcount" : "");
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitDelete(this);
    }
}

internal class UpdateExpression : CommandExpression
{
    public readonly ITable Table;
    public readonly bool UseHistoryTable;
    public ObjectName Name { get { return UseHistoryTable ? Table.SystemVersioned!.TableName : Table.Name; } }

    public readonly ReadOnlyCollection<ColumnAssignment> Assigments;
    public readonly SourceWithAliasExpression Source;
    public readonly Expression Where;
    public readonly bool ReturnRowCount;

    public UpdateExpression(ITable table, bool useHistoryTable, SourceWithAliasExpression source, Expression where, IEnumerable<ColumnAssignment> assigments, bool returnRowCount)
        : base(DbExpressionType.Update)
    {
        this.Table = table;
        this.UseHistoryTable = useHistoryTable;
        this.Assigments = assigments.ToReadOnly();
        this.Source = source;
        this.Where = where;
        this.ReturnRowCount = returnRowCount;
    }

    public override string ToString()
    {
        return "UPDATE {0}\nSET {1}\nFROM {2}\n{3}".FormatWith(
            Table.Name,
            Assigments.ToString("\n"),
            Source.ToString(),
            Where?.Let(w => "WHERE " + w.ToString()));
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitUpdate(this);
    }
}

internal class InsertSelectExpression : CommandExpression
{
    public readonly ITable Table;
    public readonly bool UseHistoryTable;
    public ObjectName Name { get { return UseHistoryTable ? Table.SystemVersioned!.TableName : Table.Name; } }
    public readonly ReadOnlyCollection<ColumnAssignment> Assigments;
    public readonly SourceWithAliasExpression Source;
    public readonly bool ReturnRowCount;

    public InsertSelectExpression(ITable table, bool useHistoryTable, SourceWithAliasExpression source, IEnumerable<ColumnAssignment> assigments, bool returnRowCount)
        : base(DbExpressionType.InsertSelect)
    {
        this.Table = table;
        this.UseHistoryTable = useHistoryTable;
        this.Assigments = assigments.ToReadOnly();
        this.Source = source;
        this.ReturnRowCount = returnRowCount;
    }

    public override string ToString()
    {
        return "INSERT INTO {0}({1})\nSELECT {2}\nFROM {3}".FormatWith(
            Table.Name,
            Assigments.ToString(a => a.Column, ",\n"),
            Assigments.ToString(a => a.Expression.ToString(), ",\n"),
            Source.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitInsertSelect(this);
    }
}

internal class ColumnAssignment
{
    public readonly string Column;
    public readonly Expression Expression;

    public ColumnAssignment(string column, Expression expression)
    {
        this.Column = column;
        this.Expression = expression;
    }

    public override string ToString()
    {
        return "{0} = {1}".FormatWith(Column, Expression);
    }
}

internal class CommandAggregateExpression : CommandExpression
{
    public readonly ReadOnlyCollection<CommandExpression> Commands;

    public CommandAggregateExpression(IEnumerable<CommandExpression> commands)
        : base(DbExpressionType.CommandAggregate)
    {
        Commands = commands.ToReadOnly();
    }

    public override string ToString()
    {
        return Commands.ToString(a => a.ToString(), "\n\n");
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitCommandAggregate(this);
    }
}
