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
using System.Data;
using Signum.Engine.Maps;
using Signum.Entities.DynamicQuery;

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

        ParameterExpression row = Expression.Parameter(typeof(IProjectionRow), "row");
        static PropertyInfo miReader = ReflectionTools.GetPropertyInfo((IProjectionRow row) => row.Reader);

        class ParameterInfo
        {
            internal MethodCallExpression Expression;
            internal string Name; 
        }

        Dictionary<Expression, ParameterInfo> parameterExpressions = new Dictionary<Expression, ParameterInfo>(); 

        int parameter = 0; 

        public string GetNextParamAlias()
        {
            return "@p" + (parameter++);
        }

        MethodInfo miUnsafeCreateParameter = ReflectionTools.GetMethodInfo(() => SqlParameterBuilder.UnsafeCreateParameter(null, SqlDbType.BigInt, false, null));

        ParameterInfo CreateParameter(Expression value)
        {
            string name = GetNextParamAlias();

            bool nullable = value.Type.IsClass || value.Type.IsNullable();
            Type clrType = value.Type.UnNullify();
            if (clrType.IsEnum)
                clrType = typeof(int);

            SqlDbType sqlDbType = Schema.Current.Settings.TypeValues.TryGetS(clrType) ?? SqlDbType.Variant;

            Expression valExpression = value.Type.IsNullable() ? 
                Expression.Coalesce(Expression.Convert(value, typeof(object)), Expression.Constant(DBNull.Value)) :
                (Expression)Expression.Convert(value, typeof(object));

            return new ParameterInfo
            {
                Expression = Expression.Call(null, miUnsafeCreateParameter,
                   Expression.Constant(name),
                   Expression.Constant(sqlDbType),
                   Expression.Constant(nullable),
                   valExpression),
                Name = name
            };
                
                
               
        }

        private QueryFormatter() { }


        static internal string Format(Expression expression, out Expression<Func<SqlParameter[]>> getParameters)
        {
            QueryFormatter qf = new QueryFormatter();
            qf.Visit(expression);

            getParameters = Expression.Lambda<Func<SqlParameter[]>>(
                Expression.NewArrayInit(typeof(SqlParameter), qf.parameterExpressions.Values.Select(pi=>pi.Expression).ToArray()));

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
                    throw new NotSupportedException(string.Format("The unary perator {0} is not supported", u.NodeType));
            }
            return u;
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
                sb.Append("(");

                Visit(b.Left);
                sb.Append(b.NodeType == ExpressionType.Equal ? " = " : " <> ");
                Visit(b.Right);

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
                        throw new NotSupportedException(string.Format("The binary operator {0} is not supported", b.NodeType));
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

        protected override Expression VisitExists(ExistsExpression exists)
        {
            sb.Append("EXISTS(");
            this.Visit(exists.Select);
            sb.Append(")");
            return exists;
        }

        protected override Expression VisitScalar(ScalarExpression exists)
        {
            sb.Append("(");
            this.Visit(exists.Select);
            sb.Append(")");
            return exists;
        }

        protected override Expression VisitIsNull(IsNullExpression isNull)
        {
            sb.Append("(");
            this.Visit(isNull.Expression);
            sb.Append(") IS NULL");
            return isNull;
        }

        protected override Expression VisitIsNotNull(IsNotNullExpression isNotNull)
        {
            sb.Append("(");
            this.Visit(isNotNull.Expression);
            sb.Append(") IS NOT NULL");
            return isNotNull;
        }

        protected override Expression VisitIn(InExpression inExpression)
        {
            Visit(inExpression.Expression);
            sb.Append(" IN (");
            if (inExpression.Select == null)
            {
                bool any = false;
                foreach (var obj in inExpression.Values)
                {
                    VisitConstant(Expression.Constant(obj));
                    sb.Append(",");
                    any = true;
                }
                if (any)
                    sb.Remove(sb.Length - 1, 1);
            }
            else
            {
                Visit(inExpression.Select);
            }
            sb.Append(" )");
            return inExpression;
        }

        protected override Expression VisitSqlEnum(SqlEnumExpression sqlEnum)
        {
            sb.Append(sqlEnum.Value);
            return sqlEnum;
        }

        protected override Expression VisitSqlCast(SqlCastExpression castExpr)
        {
            sb.Append("CAST(");
            Visit(castExpr.Expression);
            sb.Append(" as ");
            sb.Append(castExpr.SqlDbType.ToString().ToUpperInvariant());
            sb.Append(")");
            return castExpr;
        }

        internal static bool IsSupported(Type type)
        {
            return type == typeof(Guid) || Type.GetTypeCode(type) != TypeCode.Object;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value == null)
                sb.Append("NULL");
            else
            {
                if (!IsSupported(c.Value.GetType()))
                    throw new NotSupportedException(string.Format("The constant for {0} is not supported", c.Value));

                var pi = parameterExpressions.GetOrCreate(c, ()=> this.CreateParameter(c));

                sb.Append(pi.Name);
            }
            return c;
        }

        protected override Expression VisitSqlConstant(SqlConstantExpression c)
        {
            if (c.Value == null)
                sb.Append("NULL");
            else
            {
                if (!IsSupported(c.Value.GetType()))
                    throw new NotSupportedException(string.Format("The constant for {0} is not supported", c.Value));

                if (c.Value.Equals(true))
                    sb.Append("1");
                else if (c.Value.Equals(false))
                    sb.Append("0");
                else
                    sb.Append(c.ToString());
            }

            return c;
        }


        protected override Expression VisitColumn(ColumnExpression column)
        {
            if (column.Alias.HasText())
            {
                sb.Append(column.Alias.SqlScape());
                sb.Append(".");
            }
            sb.Append(column.Name.SqlScape());

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

            if (select.Columns.Count == 0)
                sb.Append("0 as Dummy"); 
            else
                for (int i = 0, n = select.Columns.Count; i < n; i++)
                {
                    ColumnDeclaration column = select.Columns[i];
                    if (i > 0)
                        sb.Append(", ");

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

            if (column.Name.HasText() && (c == null || c.Name != column.Name))
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

        protected override SourceExpression VisitSource(SourceExpression source)
        {
            if (source is SourceWithAliasExpression)
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
                sb.Append(((SourceWithAliasExpression)source).Alias.SqlScape());
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
                case JoinType.SingleRowLeftOuterJoin:
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

            bool needsMoreParenthesis = (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply) && join.Right is JoinExpression;

            if (needsMoreParenthesis)
                sb.Append("(");

            this.VisitSource(join.Right);

            if (needsMoreParenthesis)
                sb.Append(")");

            if (join.Condition != null)
            {
                this.AppendNewLine(Indentation.Inner);
                sb.Append("ON ");
                this.Visit(join.Condition);
                this.Indent(Indentation.Outer);
            }
            return join;
        }

        protected override Expression VisitDelete(DeleteExpression delete)
        {
            sb.Append("DELETE ");
            sb.Append(delete.Table.Name);
            this.AppendNewLine(Indentation.Same);
            sb.Append("FROM ");
            VisitSource(delete.Source);
            if (delete.Where != null)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append("WHERE ");
                Visit(delete.Where);
            }
            return delete;
        }

        protected override Expression VisitUpdate(UpdateExpression update)
        {
            sb.Append("UPDATE ");
            sb.Append(update.Table.Name);
            sb.Append(" SET");
            this.AppendNewLine(Indentation.Inner);
           
            for (int i = 0, n = update.Assigments.Count; i < n; i++)
            {
                ColumnAssignment assignment= update.Assigments[i];
                if (i > 0)
                {
                    sb.Append(",");
                    this.AppendNewLine(Indentation.Same);
                }
                sb.Append(assignment.Column);
                sb.Append(" = ");
                this.Visit(assignment.Expression);
            }
            this.AppendNewLine(Indentation.Outer);
            sb.Append("FROM ");
            VisitSource(update.Source);
            if (update.Where != null)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append("WHERE ");
                Visit(update.Where);
            }
            return update; 

        }

        protected override Expression VisitSelectRowCount(SelectRowCountExpression src)
        {
            sb.Append("SELECT @@rowcount");
            return src; 
        }

        

        protected override Expression VisitCommandAggregate(CommandAggregateExpression cea)
        {
            for (int i = 0, n = cea.Commands.Count; i < n; i++)
            {
                CommandExpression command = cea.Commands[i];
                if (i > 0)
                {
                    sb.Append(";"); 
                    this.AppendNewLine(Indentation.Same);
                    this.AppendNewLine(Indentation.Same);
                }
                this.Visit(command);
            }
            return cea;
        }

        
    }
}
