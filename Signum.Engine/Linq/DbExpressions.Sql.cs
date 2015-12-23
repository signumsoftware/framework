using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using Signum.Engine;
using Signum.Entities;
using System.Diagnostics;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Maps;
using Signum.Entities.DynamicQuery;
using System.Data;


namespace Signum.Engine.Linq
{

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
        SqlEnum,
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
        SelectRowCount,
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
        PrimaryKey,
        PrimaryKeyString,
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
            DbExpressionVisitor dbVisitor = visitor as DbExpressionVisitor;
            if (dbVisitor != null)
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

    internal abstract class SourceWithAliasExpression: SourceExpression
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
        public readonly Table Table;
        public readonly ReadOnlyCollection<Expression> Arguments;
        public readonly string SqlFunction;

        public override Alias[] KnownAliases
        {
            get { return new[] { Alias }; }
        }

        public SqlTableValuedFunctionExpression(string sqlFunction, Table table, Alias alias, IEnumerable<Expression> arguments)
            :base(DbExpressionType.SqlTableValuedFunction, alias)
        {
            this.SqlFunction = sqlFunction;
            this.Table = table;
            this.Arguments = arguments.ToReadOnly(); 
        }

        public override string ToString()
        {
            string result = "{0}({1}) as {2}".FormatWith(SqlFunction, Arguments.ToString(a => a.ToString(), ","), Alias);

            return result;
        }
      
        internal ColumnExpression GetIdExpression()
        {
            var expression = ((ITablePrivate)Table).GetPrimaryOrder(Alias);

            if (expression == null)
                throw new InvalidOperationException("Impossible to determine Primary Key for {0}".FormatWith(Table.Name));

            return expression;
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitSqlTableValuedFunction(this);
        }
    }

    internal class TableExpression : SourceWithAliasExpression
    {
        public readonly ITable Table;

        public ObjectName Name { get { return Table.Name; } }

        public readonly string WithHint;

        public override Alias[] KnownAliases
        {
            get { return new[] { Alias }; }
        }

        internal TableExpression(Alias alias, ITable table, string withHint)
            : base(DbExpressionType.Table, alias)
        {
            this.Table = table;
            this.WithHint = withHint;
        }

        public override string ToString()
        {
            return "{0} as {1}".FormatWith(Name, Alias);
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
        public readonly string Name;

        internal ColumnExpression(Type type, Alias alias, string name)
            : base(DbExpressionType.Column, type)
        {
            if (alias == null)
                throw new ArgumentNullException("alias");

            if (name == null)
                throw new ArgumentNullException("name");

            if (type.UnNullify() == typeof(PrimaryKey))
                throw new ArgumentException("type should not be PrimaryKey");

            this.Alias = alias;
            this.Name = name;
        }

        public override string ToString()
        {
            return "{0}.{1}".FormatWith(Alias, Name);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ColumnExpression);
        }

        public bool Equals(ColumnExpression other)
        {
            return other != null && other.Alias == Alias && other.Name == Name; 
        }

        public override int GetHashCode()
        {
            return Alias.GetHashCode() ^ Name.GetHashCode();
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
            if (expression == null) throw new ArgumentNullException("expression");

            this.Name = name;
            this.Expression = expression;
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

    internal enum AggregateFunction
    {
        Average,
        Count,
        Min,
        Max,
        Sum,
    }

    internal class AggregateExpression : DbExpression
    {
        public readonly Expression Expression;
        public readonly AggregateFunction AggregateFunction;
        public AggregateExpression(Type type, Expression expression, AggregateFunction aggregateFunction)
            : base(DbExpressionType.Aggregate, type)
        {
            if (expression == null && aggregateFunction != AggregateFunction.Count) 
                throw new ArgumentNullException("expression");

            this.Expression = expression;
            this.AggregateFunction = aggregateFunction;
        }

        public override string ToString()
        {
            return "{0}({1})".FormatWith(AggregateFunction, Expression?.ToString() ?? "*");
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
            if (expression == null) throw new ArgumentNullException("expression");

            this.OrderType = orderType;
            this.Expression = expression;
        }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(Expression.ToString(), OrderType == OrderType.Ascending ? "ASC": "DESC");
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
        public readonly SourceExpression From;
        public readonly Expression Where;
        public readonly ReadOnlyCollection<OrderExpression> OrderBy;
        public readonly ReadOnlyCollection<Expression> GroupBy;
        public readonly Expression Top;
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

        internal SelectExpression(Alias alias, bool distinct, Expression top, IEnumerable<ColumnDeclaration> columns, SourceExpression from, Expression where, IEnumerable<OrderExpression> orderBy, IEnumerable<Expression> groupBy, SelectOptions options)
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
                else if (Columns.Any(cd => AggregateFinder.HasAggregates(cd.Expression)))
                    roles |= SelectRoles.Aggregate;

                if (OrderBy.Count > 0)
                    roles |= SelectRoles.OrderBy;

                if (!Columns.All(cd => cd.Expression is ColumnExpression))
                    roles |= SelectRoles.Select;

                if(IsDistinct)
                    roles |= SelectRoles.Distinct;
                
                if (Top != null)
                    roles |= SelectRoles.Top;

                return roles;
            }
        }

        public override string ToString()
        {
            return "SELECT {0}{1}\r\n{2}\r\nFROM {3}\r\n{4}{5}{6}{7} AS {8}".FormatWith(
                IsDistinct ? "DISTINCT " : "",
                Top?.Let(t => "TOP {0} ".FormatWith(t.ToString())),
                Columns.ToString(c => c.ToString().Indent(4) ,",\r\n"),
                From?.Let(f => f.ToString().Let(a => a.Contains("\r\n") ? "\r\n" + a.Indent(4) : a)),
                Where?.Let(a => "WHERE " + a.ToString() + "\r\n"),
                OrderBy.Any() ? ("ORDER BY " + OrderBy.ToString(" ,") + "\r\n") : null,
                GroupBy.Any() ? ("GROUP BY " + GroupBy.ToString(g => g.ToString(), " ,") + "\r\n") : null,
                SelectOptions == 0 ? "" : SelectOptions.ToString() + "\r\n",
                Alias);
        }

        internal bool IsOneRow()
        {
            ConstantExpression ce = Top as ConstantExpression;

            if (ce != null && ((int)ce.Value) == 1)
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
        public new readonly Expression Condition;

        public override Alias[] KnownAliases
        {
            get { return Left.KnownAliases.Concat(Right.KnownAliases).ToArray(); }
        }

        internal JoinExpression(JoinType joinType, SourceExpression left, SourceExpression right, Expression condition)
            : base(DbExpressionType.Join)
        {
            if (left == null) 
                throw new ArgumentNullException("left");

            if (right == null)
                throw new ArgumentNullException("right");

            if (condition == null && joinType != JoinType.CrossApply && joinType != JoinType.OuterApply && joinType != JoinType.CrossJoin)
                throw new ArgumentNullException("condition");

            this.JoinType = joinType;
            this.Left = left;
            this.Right = right;
            this.Condition = condition;
        }
   
        public override string ToString()
        {
            return "{0}\r\n{1}\r\n{2}\r\nON {3}".FormatWith(Left.ToString().Indent(4), JoinType, Right.ToString().Indent(4), Condition?.ToString());
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
            if (left == null)
                throw new ArgumentNullException("left");

            if (right == null)
                throw new ArgumentNullException("right");

            this.Operator = @operator;
            this.Left = left;
            this.Right = right;
        }

        public override string ToString()
        {
            return "{0}\r\n{1}\r\n{2}\r\n as {3}".FormatWith(Left.ToString().Indent(4), Operator, Right.ToString().Indent(4), Alias);
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
        DATEADD,

        COALESCE,
        CONVERT,
        ISNULL,
        STUFF,
    }

    internal enum SqlEnums
    {
        year,
        month,
        day,
        week,
        weekday,
        hour,
        minute,
        second,
        millisecond,
        dayofyear,
        iso_week
    }



    internal class SqlEnumExpression : DbExpression
    {
        public readonly SqlEnums Value; 
        public SqlEnumExpression(SqlEnums value)
            :base(DbExpressionType.SqlEnum, typeof(object))
        {
            this.Value = value; 
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitSqlEnum(this);
        }
    }

    internal class SqlCastExpression : DbExpression
    {
        public readonly SqlDbType SqlDbType;
        public readonly Expression Expression;

        public SqlCastExpression(Type type, Expression expression)
            : this(type, expression, Schema.Current.Settings.DefaultSqlType(type.UnNullify()))
        {
        }

        public SqlCastExpression(Type type, Expression expression, SqlDbType sqlDbType)
            :base(DbExpressionType.SqlCast, type)
        {
            this.Expression = expression;
            this.SqlDbType = sqlDbType; 
        }

        public override string ToString()
        {
            return "Cast({0} as {1})".FormatWith(Expression.ToString(), SqlDbType.ToString().ToUpper());
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitSqlCast(this);
        }
    }

    internal class SqlFunctionExpression : DbExpression
    {
        public readonly Expression Object; 
        public readonly string SqlFunction;
        public readonly ReadOnlyCollection<Expression> Arguments;

        public SqlFunctionExpression(Type type, Expression obj, string sqlFunction, IEnumerable<Expression> arguments)
            :base(DbExpressionType.SqlFunction, type )
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
        public readonly object Value;

        public SqlConstantExpression(object value)
            : this(value, value.GetType())
        {
        }

        public SqlConstantExpression(object value, Type type)
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

    internal class When
    {
        public readonly Expression Condition;
        public readonly Expression Value;

        public When(Expression condition, Expression value)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            if (value == null)
                throw new ArgumentNullException("value");

            if (condition.Type.UnNullify() != typeof(bool))
                throw new ArgumentException("condition");

            this.Condition = condition;
            this.Value = value; 
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

            throw new InvalidOperationException("Imposible to convert to {0} the expression: \r\n{1}"
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
        public readonly Expression DefaultValue;

        
        public CaseExpression(IEnumerable<When> whens, Expression defaultValue)
            :base(DbExpressionType.Case, GetType(whens, defaultValue))
        {
            if (whens.IsEmpty())
                throw new ArgumentNullException("whens");

            Type refType = this.Type.UnNullify();

            if (whens.Any(w => w.Value.Type.UnNullify() != refType))
                throw new ArgumentException("whens");

            this.Whens = whens.ToReadOnly();
            this.DefaultValue = defaultValue;
        }

        static Type GetType(IEnumerable<When> whens, Expression defaultValue)
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

            if(DefaultValue != null)
                sb.AppendLine(DefaultValue.ToString()); 

            sb.AppendLine("END");
            return sb.ToString();
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitCase(this);
        }
    }

    internal class LikeExpression : DbExpression
    {
        public readonly Expression Expression;
        public readonly Expression Pattern;

        public LikeExpression(Expression expression, Expression pattern)
            :base(DbExpressionType.Like, typeof(bool))
        {
            if (expression == null || expression.Type != typeof(string))
                throw new ArgumentException("expression");

            if (pattern == null || pattern.Type != typeof(string))
                throw new ArgumentException("pattern");
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

    internal abstract class SubqueryExpression : DbExpression
    {
        public readonly SelectExpression Select;
        protected SubqueryExpression(DbExpressionType nodeType, Type type, SelectExpression select)
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
            return "SCALAR({0})".FormatWith(Select.ToString());
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
            return "EXIST({0})".FormatWith(Select.ToString());
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitExists(this);
        }
    }

    internal class InExpression : SubqueryExpression
    {
        public readonly Expression Expression;
        public readonly object[] Values;

        public InExpression(Expression expression, SelectExpression select)
            : base(DbExpressionType.In, typeof(bool), select)
        {
            if (expression == null)throw new ArgumentNullException("expression");
            if (select == null) throw new ArgumentNullException("select"); 
       
            this.Expression = expression;
        }

        public static Expression FromValues(Expression expression, object[] values)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (values == null) throw new ArgumentNullException("values");

            if (values.Length == 0)
                return Expression.Constant(false);

            return new InExpression(expression, values);
        }

        InExpression(Expression expression, object[] values)
            : base(DbExpressionType.In, typeof(bool), null)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (values == null) throw new ArgumentNullException("values");

            this.Expression = expression;
            this.Values = values;
        }

        public override string ToString()
        {
            if (Values == null)
                return "{0} IN ({1})".FormatWith(Expression.ToString(), Select.ToString());
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

        public RowNumberExpression(IEnumerable<OrderExpression> orderBy)
            :base(DbExpressionType.RowNumber, typeof(int))
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

        internal ProjectionExpression(SelectExpression source, Expression projector, UniqueFunction? uniqueFunction, Type resultType)
            : base(DbExpressionType.Projection, resultType)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (projector == null)
                throw new ArgumentNullException("projector");

            var elementType = uniqueFunction == null ? resultType.ElementType() : resultType;
            if (!elementType.IsAssignableFrom(projector.Type))
                throw new InvalidOperationException("Projector ({0}) does not fit in the projection ({1})".FormatWith(
                    projector.Type.TypeName(),
                    elementType.TypeName()));

            this.Select = source;
            this.Projector = projector;
            this.UniqueFunction = uniqueFunction;
        }

        public override string ToString()
        {
            return "(SOURCE\r\n{0}\r\nPROJECTION\r\n{1})".FormatWith(Select.ToString().Indent(4), Projector.ToString().Indent(4)); 
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
        public readonly SourceWithAliasExpression Source;
        public readonly Expression Where;

        public DeleteExpression(ITable table, SourceWithAliasExpression source, Expression where)
            :base(DbExpressionType.Delete)
        {
            this.Table = table;
            this.Source = source;
            this.Where = where; 
        }

        public override string ToString()
        {
            return "DELETE {0}\r\nFROM {1}\r\n{2}".FormatWith(
                Table.Name, 
                Source.ToString(), 
                Where?.Let(w => "WHERE " + w.ToString())); 
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitDelete(this);
        }
    }

    internal class UpdateExpression : CommandExpression
    {
        public readonly ITable Table;
        public readonly ReadOnlyCollection<ColumnAssignment> Assigments;
        public readonly SourceWithAliasExpression Source;
        public readonly Expression Where;

        public UpdateExpression(ITable table, SourceWithAliasExpression source, Expression where, IEnumerable<ColumnAssignment> assigments)
            :base(DbExpressionType.Update)
        {
            this.Table = table;
            this.Assigments = assigments.ToReadOnly();
            this.Source = source;
            this.Where = where;
        }

        public override string ToString()
        {
            return "UPDATE {0}\r\nSET {1}\r\nFROM {2}\r\n{3}".FormatWith(
                Table.Name,
                Assigments.ToString("\r\n"),
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
        public readonly ReadOnlyCollection<ColumnAssignment> Assigments;
        public readonly SourceWithAliasExpression Source;

        public InsertSelectExpression(ITable table, SourceWithAliasExpression source, IEnumerable<ColumnAssignment> assigments)
            : base(DbExpressionType.InsertSelect)
        {
            this.Table = table;
            this.Assigments = assigments.ToReadOnly();
            this.Source = source;
        }

        public override string ToString()
        {
            return "INSERT INTO {0}({1})\r\nSELECT {2}\r\nFROM {3}".FormatWith(
                Table.Name,
                Assigments.ToString(a => a.Column, ",\r\n"),
                Assigments.ToString(a => a.Expression.ToString(), ",\r\n"),
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
            return Commands.ToString(a => a.ToString(), "\r\n\r\n");
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitCommandAggregate(this);
        }
    }

    internal class SelectRowCountExpression : CommandExpression
    {
        public SelectRowCountExpression()
            : base(DbExpressionType.SelectRowCount)
        {
        }

        public override string ToString()
        {
            return "SELECT @@rowcount";
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitSelectRowCount(this);
        }
    }
}
