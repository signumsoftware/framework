using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Engine.Properties;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// QueryFormatter is a visitor that converts an bound expression tree into SQL query text
    /// </summary>
    internal class QueryFormatter : DbExpressionVisitor
    {
        StringBuilder sb = new StringBuilder();
        int indent = 2;
        int depth;

        ImmutableStack<string> prevAliases; 

        ParameterExpression row = Expression.Parameter(typeof(IProjectionRow), "row");
        static MethodInfo miGetValue = typeof(IProjectionRow).GetMethod("GetValue");

        List<Expression> parameterExpressions = new List<Expression>(); 

        int parameter = 0; 

        public string GetNextParamAlias()
        {
            return "@p" + (parameter++);
        }

        ConstructorInfo cons = typeof(SqlParameter).GetConstructor(new[] { typeof(string), typeof(object) });

        public Expression CreateParameter(string name, Expression value)
        {
            return Expression.New(cons, Expression.Constant(name), Expression.Convert(value, typeof(object)));
        }

        private QueryFormatter() { }

        static internal string Format(Expression expression)
        {
            QueryFormatter qf = new QueryFormatter() { prevAliases = ImmutableStack<string>.Empty };
            qf.Visit(expression);

            return qf.sb.ToString();
        }

        static internal string Format(Expression expression, ImmutableStack<string> prevAliases, out Expression<Func<IProjectionRow, SqlParameter[]>> getParameters)
        {
            QueryFormatter qf = new QueryFormatter() { prevAliases = prevAliases};
            qf.Visit(expression);

            getParameters = Expression.Lambda<Func<IProjectionRow, SqlParameter[]>>(
                Expression.NewArrayInit(typeof(SqlParameter), qf.parameterExpressions.ToArray()), qf.row);

            return qf.sb.ToString();
        }

        protected enum Indentation
        {
            Same,
            Inner,
            Outer
        }

        internal int IndentationWidth
        {
            get { return this.indent; }
            set { this.indent = value; }
        }

        private void AppendNewLine(Indentation style)
        {
            sb.AppendLine();
            this.Indent(style);
            for (int i = 0, n = this.depth * this.indent; i < n; i++)
            {
                sb.Append(" ");
            }
        }

        private void Indent(Indentation style)
        {
            if (style == Indentation.Inner)
            {
                this.depth++;
            }
            else if (style == Indentation.Outer)
            {
                this.depth--;
                System.Diagnostics.Debug.Assert(this.depth >= 0);
            }
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
                    throw new NotSupportedException(string.Format(Resources.TheUnaryOperator0IsNotSupported, u.NodeType));
            }
            return u;
        }

        protected override Expression VisitSetOperation(SetOperationExpression setOperationExp)
        {
            sb.Append("(");
            AppendNewLine(Indentation.Inner);

            Visit(setOperationExp.Left);

            AppendNewLine(Indentation.Outer);
            sb.Append(new Switch<SetOperation, string>(setOperationExp.SetOperation)
                .Case(SetOperation.Union, "UNION")
                .Case(SetOperation.Concat, "UNION ALL")
                .Case(SetOperation.Intersect, "INTERSECT")
                .Case(SetOperation.Except, "EXCEPT").Default(""));
            AppendNewLine(Indentation.Inner);

            Visit(setOperationExp.Right);

            AppendNewLine(Indentation.Outer);
            sb.Append(")");

            return setOperationExp;
        }

        bool IsNull(Expression exp)
        {
            return exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null; 
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.Coalesce)
            {
                sb.Append("IsNull(");
                Visit(b.Left);
                sb.Append(",");
                Visit(b.Right);
                sb.Append(")"); 
            }
            else if (b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual)
            {
                bool isEqual = b.NodeType == ExpressionType.Equal;

                sb.Append("(");
                if(IsNull(b.Left))
                {
                    Visit(b.Right);
                    sb.Append(isEqual?" IS NULL " : " IS NOT NULL");
                }
                else if(IsNull(b.Right))
                {
                    Visit(b.Left);
                    sb.Append(isEqual?" IS NULL " : " IS NOT NULL");
                }
                else
                {
                    Visit(b.Left);
                    sb.Append(isEqual?" = " : " <> ");
                    Visit(b.Right);
                }
                sb.Append(")");
            }
            else
            {
                sb.Append("(");
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
                        throw new NotSupportedException(string.Format(Resources.TheBinaryOperator0IsNotSupported, b.NodeType));
                }
                this.Visit(b.Right);
                sb.Append(")");
            }
            return b;
        }


        protected override Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            sb.Append("ROW_NUMBER() OVER(ORDER BY ");
            for (int i = 0, n = rowNumber.OrderBy.Count; i < n; i++)
            {
                OrderExpression exp = rowNumber.OrderBy[i];
                if (i > 0)
                    sb.Append(", ");
                this.Visit(exp.Expression);
                if (exp.OrderType != OrderType.Ascending)
                    sb.Append(" DESC");
            }
            sb.Append(")"); 
            return rowNumber;
        }

        protected override Expression VisitCase(CaseExpression cex)
        {
            AppendNewLine(Indentation.Inner);
            sb.Append("CASE");
            AppendNewLine(Indentation.Inner);
            for (int i = 0, n = cex.Whens.Count; i < n; i++)
            {
                When when = cex.Whens[i]; 
                sb.Append("WHEN ");
                Visit(when.Condition);
                sb.Append(" THEN ");
                Visit(when.Value);
                AppendNewLine(Indentation.Same); 
            }
            sb.Append("ELSE ");
            Visit(cex.DefaultValue);
            AppendNewLine(Indentation.Outer);
            sb.Append("END");
            AppendNewLine(Indentation.Outer);

            return cex; 
        }

        protected override Expression VisitLike(LikeExpression like)
        {
            Visit(like.Expression);
            sb.Append(" LIKE ");
            Visit(like.Pattern);
            return like;
        }

        protected override Expression VisitSqlEnum(SqlEnumExpression sqlEnum)
        {
            sb.Append(sqlEnum.Value);
            return sqlEnum;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value == null)
            {
                sb.Append("NULL");
            }
            else
            {
                if(Type.GetTypeCode(c.Value.GetType()) == TypeCode.Object)
                    throw new NotSupportedException(string.Format(Resources.TheConstantFor0IsNotSupported, c.Value));

                string paramName = GetNextParamAlias();

                parameterExpressions.Add(this.CreateParameter(paramName, c));

                sb.Append(paramName); 
            }
            return c;
        }

    
        protected override Expression VisitColumn(ColumnExpression column)
        {
            if (prevAliases.Contains(column.Alias))
            {
                string paramName = GetNextParamAlias();
                parameterExpressions.Add(CreateParameter(paramName,
                    Expression.Call(this.row, 
                      miGetValue.MakeGenericMethod(column.Type),
                      Expression.Constant(column.Alias),
                      Expression.Constant(column.Name))));

                sb.Append(paramName);
            }
            else
            {
                if (column.Alias.HasText())
                {
                    sb.Append(column.Alias.SqlScape());
                    sb.Append(".");
                }
                sb.Append(column.Name.SqlScape());
            }
            return column;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool isFirst = sb.Length == 0;
            if (!isFirst)
            {
                AppendNewLine(Indentation.Inner);
                sb.Append("(");
            }

            sb.Append("SELECT ");
            if (select.Distinct)
                sb.Append("DISTINCT ");

            if (select.Top != null)
            {
                sb.Append("TOP (");
                Visit(select.Top);
                sb.Append(") "); 
            }

            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                ColumnDeclaration column = select.Columns[i];
                if (i > 0)
                {
                    sb.Append(", ");
                }
                
                AppendColumn(column);
            }
            if (select.From != null)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append("FROM ");
                this.VisitSource(select.From);
            }
            if (select.Where != null)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append("WHERE ");
                this.Visit(select.Where);
            }
            if (select.GroupBy != null && select.GroupBy.Count > 0)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append("GROUP BY ");
                for (int i = 0, n = select.GroupBy.Count; i < n; i++)
                {
                    Expression exp = select.GroupBy[i];
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    this.Visit(exp);
                }
            }
            if (select.OrderBy != null && select.OrderBy.Count > 0)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append("ORDER BY ");
                for (int i = 0, n = select.OrderBy.Count; i < n; i++)
                {
                    OrderExpression exp = select.OrderBy[i];
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    this.Visit(exp.Expression);
                    if (exp.OrderType != OrderType.Ascending)
                    {
                        sb.Append(" DESC");
                    }
                }
            }

            if (!isFirst)
            {
                sb.Append(")");
                AppendNewLine(Indentation.Outer);
            }

            return select;
        }

        Dictionary<AggregateFunction, string> dic = new Dictionary<AggregateFunction, string>
        {
            {AggregateFunction.Average, "AVG"},
            {AggregateFunction.Count, "COUNT"},
            {AggregateFunction.Max, "MAX"},
            {AggregateFunction.Min, "MIN"},
            {AggregateFunction.Sum, "SUM"}
        };

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            sb.Append(dic[aggregate.AggregateFunction]);
            sb.Append("(");
            if (aggregate.Source == null)
                sb.Append("*");
            else
                Visit(aggregate.Source);
            sb.Append(")");

            return aggregate; 
        }

        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
        {
            sb.Append(sqlFunction.SqlFunction);
            sb.Append("(");
            for (int i = 0, n = sqlFunction.Arguments.Count; i < n; i++)
            {
                Expression exp = sqlFunction.Arguments[i];
                if (i > 0)
                    sb.Append(", ");
                this.Visit(exp);
            }
            sb.Append(")");

            return sqlFunction;
        }

        private void AppendColumn(ColumnDeclaration column)
        {
            ColumnExpression c = this.Visit(column.Expression) as ColumnExpression;

            if (c == null || c.Name != column.Name)
            {
                sb.Append(" AS ");
                sb.Append(column.Name.SqlScape());
            }
        }

        protected override Expression VisitTable(TableExpression table)
        {
            sb.Append(table.Name.SqlScape());
 
            return table;
        }

        protected override Expression VisitSource(Expression source)
        {
            if (source is SourceExpression)
            {
                if (source is TableExpression)
                    Visit(source);
                else
                {
                    sb.Append("(");
                    Visit(source);
                    sb.Append(")");
                }

                sb.Append(" AS ");
                sb.Append(((SourceExpression)source).Alias);
            }
            else
                this.VisitJoin((JoinExpression)source);

            return source;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            this.VisitSource(join.Left);
            this.AppendNewLine(Indentation.Same);
            switch (join.JoinType)
            {
                case JoinType.CrossJoin:
                    sb.Append("CROSS JOIN ");
                    break;
                case JoinType.InnerJoin:
                    sb.Append("INNER JOIN ");
                    break;
                case JoinType.LeftOuterJoin:
                    sb.Append("LEFT OUTER JOIN ");
                    break;
                case JoinType.RightOuterJoin:
                    sb.Append("RIGHT OUTER JOIN ");
                    break;
                case JoinType.FullOuterJoin:
                    sb.Append("FULL OUTER JOIN ");
                    break;
                case JoinType.CrossApply:
                    sb.Append("CROSS APPLY ");
                    break;
                case JoinType.OuterApply:
                    sb.Append("OUTER APPLY ");
                    break;
            }
            this.VisitSource(join.Right);
            if (join.Condition != null)
            {
                this.AppendNewLine(Indentation.Inner);
                sb.Append("ON ");
                this.Visit(join.Condition);
                this.Indent(Indentation.Outer);
            }
            return join;
        }
    }
}
