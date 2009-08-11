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
        Join,
        Aggregate,
        SqlFunction,
        SqlEnum,
        Case, 
        RowNumber,
        SetOperation,
        Like,
        In,
        FieldInit,
        ImplementedBy,
        ImplementedByAll,
        NullEntity,
        LazyReference,
        LazyLiteral, 
        MList,
    }

    internal static class DbExpressionExtensions
    {
        internal static bool IsDbExpression(this ExpressionType et)
        {
            return ((int)et) >= 1000;
        }
    }

    internal abstract class SourceExpression: Expression
    {
        public readonly string Alias;

        public SourceExpression(ExpressionType nodeType, Type type, string alias)
            : base(nodeType, type)
        {
            this.Alias = alias;
        }
    }


    /// <summary>
    /// A custom expression node that represents a table reference in a SQL query
    /// </summary>
    internal class TableExpression : SourceExpression
    {
        public readonly string Name;

        internal TableExpression(Type type, string alias, string name)
            : base((ExpressionType)DbExpressionType.Table, type, alias)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return "{0} as {1}".Formato(Name, Alias);
        }

    }

    /// <summary>
    /// A custom expression node that represents a reference to a column in a SQL query
    /// </summary>
    internal class ColumnExpression : Expression
    {
        public readonly string Alias;
        public readonly string Name;
        //public readonly int Ordinal;
        internal ColumnExpression(Type type, string alias, string name/*, int ordinal*/)
            : base((ExpressionType)DbExpressionType.Column, type)
        {
            this.Alias = alias;
            this.Name = name;
            //this.Ordinal = ordinal;
        }

        public override string ToString()
        {
            return "[{0}].[{1}]".Formato(Alias, Name);
        }
    }

    /// <summary>
    /// A declaration of a column in a SQL SELECT expression
    /// </summary>
    internal class ColumnDeclaration
    {
        public readonly string Name;
        public readonly Expression Expression;
        internal ColumnDeclaration(string name, Expression expression)
        {
            this.Name = name;
            this.Expression = expression;
        }

        public override string ToString()
        {
            return "{0} AS {1}".Formato(Expression, Name); 
        }
    }

    public enum AggregateFunction
    {
        Average,
        Count,
        Min,
        Max,
        Sum,
    }


    internal class AggregateExpression : Expression
    {
        public readonly Expression Source;
        public readonly AggregateFunction AggregateFunction;
        public AggregateExpression(Type type, Expression source, AggregateFunction aggregateFunction)
            : base((ExpressionType)DbExpressionType.Aggregate, type)
        {
            this.Source = source;
            this.AggregateFunction = aggregateFunction;
        }

        public override string ToString()
        {
            return "{0}({1})".Formato(AggregateFunction, Source.TryCC(s => s.ToString()) ?? "*");
        }
    }

    /// <summary>
    /// An SQL OrderBy order type 
    /// </summary>
    internal enum OrderType
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// A pairing of an expression and an order type for use in a SQL Order By clause
    /// </summary>
    internal class OrderExpression
    {
        public readonly OrderType OrderType;
        public readonly Expression Expression;

        internal OrderExpression(OrderType orderType, Expression expression)
        {
            this.OrderType = orderType;
            this.Expression = expression;
        }

        public override string ToString()
        {
            return "OrderType: {0} Expression: {1}".Formato(OrderType, Expression);
        }
    }

    [Flags]
    internal enum SelectRoles
    {
        Where = 1,
        GroupBy = 2,
        OrderBy = 4, 
        Select = 8, 
        Distinct = 16,
        Top = 32,
    }

    /// <summary>
    /// A custom expression node used to represent a SQL SELECT expression
    /// </summary>
    internal class SelectExpression : SourceExpression
    {
        public readonly ReadOnlyCollection<ColumnDeclaration> Columns;
        public readonly Expression From;
        public readonly Expression Where;
        public readonly ReadOnlyCollection<OrderExpression> OrderBy;
        public readonly ReadOnlyCollection<Expression> GroupBy;
        public readonly string GroupOf;
        public readonly Expression Top;
        public readonly bool Distinct; 

        internal SelectExpression(
            Type type, string alias, bool distinct, Expression top, IEnumerable<ColumnDeclaration> columns,
            Expression from, Expression where, IEnumerable<OrderExpression> orderBy, IEnumerable<Expression> groupBy, string groupOf)
            : base((ExpressionType)DbExpressionType.Select, type, alias)
        {
            this.Distinct = distinct;
            this.Top = top; 
            this.Columns = columns.ToReadOnly();
            this.From = from;
            this.Where = where;
            this.OrderBy = orderBy.ToReadOnly();
            this.GroupBy = groupBy.ToReadOnly();
            this.GroupOf = groupOf; 
        }

        internal SelectRoles SelectRoles
        {
            get 
            {
                SelectRoles roles = (SelectRoles)0;
                if (!Columns.All(cd => cd.Expression is ColumnExpression))
                    roles |= SelectRoles.Select;
                if (Where != null)
                    roles |= SelectRoles.Where;
                if (GroupBy != null && GroupBy.Count > 0)
                    roles |= SelectRoles.GroupBy;
                if (OrderBy != null && OrderBy.Count > 0)
                    roles |= SelectRoles.OrderBy;
                if(Distinct)
                    roles |= SelectRoles.Distinct;
                if (Top != null)
                    roles |= SelectRoles.Top;

                return roles;
            }
        }

        public override string ToString()
        {
            return "SELECT {0}{1}{2}\r\nFROM {3}\r\n{4}{5}{6}AS {7}".Formato(
                Distinct ? "DISTINCT " : "", 
                Top.TryCC(t => "TOP {0} ".Formato(t)), 
                Columns.TryCC(c => c.ToString(", ")),
                From.ToString().Map(a => a.Contains("\r\n") ? "\r\n" + a.Indent(4) : a),
                Where.TryCC(a => "WHERE " + a.ToString() + "\r\n"),
                OrderBy.TryCC(c => "ORDER BY " + c.ToString(" ,") + "\r\n"),
                GroupBy.TryCC(g => "GROUP BY " + g.ToString(" ,") + "\r\n"),
                Alias);
        }
    }

    /// <summary>
    /// A kind of SQL join
    /// </summary>
    internal enum JoinType
    {
        CrossJoin,
        InnerJoin,
        CrossApply,
        OuterApply,
        LeftOuterJoin,
        RightOuterJoin,
        FullOuterJoin, 
    }

    /// <summary>
    /// A custom expression node representing a SQL join clause
    /// </summary>
    internal class JoinExpression : Expression
    {
        public readonly JoinType JoinType;
        public readonly Expression Left;
        public readonly Expression Right;
        public readonly bool IsSingleRow; 
        public new readonly Expression Condition;
        internal JoinExpression(Type type, JoinType joinType, Expression left, Expression right, Expression condition, bool isSingleRow)
            : base((ExpressionType)DbExpressionType.Join, type)
        {
            Debug.Assert(!IsSingleRow || IsSingleRow && joinType == JoinType.LeftOuterJoin); 
            this.JoinType = joinType;
            this.Left = left;
            this.Right = right;
            this.Condition = condition;
            this.IsSingleRow = isSingleRow; 
        }
   
        public override string ToString()
        {
            return "{0}\r\n{1}\r\n{2}\r\nON {3}".Formato(Left.ToString().Indent(4), JoinType, Right.ToString().Indent(4), Condition);
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
        ACOS,
        POWER,
        SQRT,
        ASIN,
        EXP,
        SQUARE,
        CEILING, 
        FLOOR,
        TAN,
        COS,
        Log10, 
        ROUND,
        SIGN,
        DAY,
        MONTH,
        YEAR,
        GETDATE,
        DATEPART, 
        DATEDIFF,
        DATEADD, 
    }

    internal enum SqlEnums
    {
        year,
        month,
        day,
        week,
        hour,
        minute,
        second,
        millisecond,
        dayofyear,
    }



    internal class SqlEnumExpression: Expression
    {
        public readonly SqlEnums Value; 
        public SqlEnumExpression(SqlEnums value)
            :base((ExpressionType)DbExpressionType.SqlEnum, typeof(object))
        {
            this.Value = value; 
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    internal class SqlFunctionExpression : Expression
    {
        public readonly string SqlFunction;
        public readonly ReadOnlyCollection<Expression> Arguments;

        public SqlFunctionExpression(Type type, string sqlFunction, IEnumerable<Expression> arguments)
            :base((ExpressionType)DbExpressionType.SqlFunction, type )
        {
            this.SqlFunction = sqlFunction;
            this.Arguments = arguments.ToReadOnly(); 
        }

        public override string ToString()
        {
            return "{0}({1})".Formato(SqlFunction, Arguments.ToString(","));
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

            if (condition.Type != typeof(bool))
                throw new ArgumentException("condition");

            this.Condition = condition;
            this.Value = value; 
        }

        public override string ToString()
        {
            return "  WHEN {0} THEN {1}".Formato(Condition, Value);
        }
    }

    internal class CaseExpression : Expression
    {
        public readonly ReadOnlyCollection<When> Whens;
        public readonly Expression DefaultValue;

        public CaseExpression(IEnumerable<When> whens, Expression defaultValue)
            :base((ExpressionType)DbExpressionType.Case, defaultValue.Type)
        {
            if (whens.Any(w => w.Value.Type != defaultValue.Type))
                throw new ArgumentException("whens");

            this.Whens = whens.ToReadOnly();
            this.DefaultValue = defaultValue;
        }

        public override string ToString()
        {
            return "CASE\r\n{0}\r\n  ELSE {1}\r\nEND".Formato(Whens.ToString("\r\n"), DefaultValue);
        }
    }

    internal class LikeExpression : Expression
    {
        public readonly Expression Expression;
        public readonly Expression Pattern;

        public LikeExpression(Expression expression, Expression pattern)
            :base((ExpressionType)DbExpressionType.Like, typeof(bool))
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
            return "{0} LIKE {1}".Formato(Expression, Pattern);
        }
    }

    internal class InExpression : Expression
    {
        public readonly Expression Expression;
        public readonly object[] Values;

        public InExpression(Expression expression, object[] values)
            : base((ExpressionType)DbExpressionType.In, typeof(bool))
        {
            if (expression == null)
                throw new ArgumentException("expression");

            if (values == null)
                throw new ArgumentException("values"); 
       
            this.Expression = expression;
            this.Values = values;
        }

        public override string ToString()
        {
            return "{0} IN ({1})".Formato(Expression, Values.ToString(", "));
        }
    }


    internal class RowNumberExpression : Expression
    {
        public readonly ReadOnlyCollection<OrderExpression> OrderBy;

        public RowNumberExpression(IEnumerable<OrderExpression> orderBy)
            :base((ExpressionType)DbExpressionType.RowNumber, typeof(int))
        {
            this.OrderBy = orderBy.ToReadOnly(); 
        }

        public override string ToString()
        {
            return "ROW_NUMBER()";
        }
    }

    internal enum SetOperation
    {
        Union,
        Except,
        Intersect,
        Concat,
    }

    internal class SetOperationExpression : SourceExpression
    {
        public readonly SelectExpression Left; // not necessary a select, could be a table or a set operation, but it is de facto
        public readonly SelectExpression Right;
        public readonly SetOperation SetOperation;

        public SetOperationExpression(Type type, string alias, SetOperation setOperation, SelectExpression left, SelectExpression right)
            :base((ExpressionType)DbExpressionType.SetOperation, type, alias)
        {
            this.SetOperation = setOperation; 
            this.Left = left;
            this.Right = right;
        }

        public override string ToString()
        {
            return "{0}\r\n{1}\r\n{2}".Formato(Left.ToString().Indent(4), SetOperation, Right.ToString().Indent(4));
        }
    }

    public enum UniqueFunction
    {
        First, 
        FirstOrDefault,
        Single,
        SingleOrDefault,
        SingleGreaterThanZero,
        SingleIsZero,
    }

    /// <summary>
    /// A custom expression representing the construction of one or more result objects from a 
    /// SQL select expression
    /// </summary>
    internal class ProjectionExpression : Expression
    {   
        public readonly SelectExpression Source;
        public readonly Expression Projector;
        public readonly UniqueFunction?  UniqueFunction;

        internal ProjectionExpression(SelectExpression source, Expression projector, UniqueFunction? uniqueFunction)
            : this(source.Type, source, projector, uniqueFunction)
        {  
        }

        internal ProjectionExpression(Type type, SelectExpression source, Expression projector, UniqueFunction? uniqueFunction)
            : base((ExpressionType)DbExpressionType.Projection, type)
        {
            this.Source = source;
            this.Projector = projector;
            this.UniqueFunction = uniqueFunction;
        }
    
        internal bool IsOneCell
        {
            get { return this.UniqueFunction.HasValue && Source.Columns.Count == 1; }
        }

        public override string ToString()
        {
            return "SOURCE\r\n{0}\r\nPROJECTION\r\n{1}".Formato(Source.ToString().Indent(4), Projector.ToString().Indent(4)); 
        }
    }
}
