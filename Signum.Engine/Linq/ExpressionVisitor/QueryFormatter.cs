using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// QueryFormatter is a visitor that converts an bound expression tree into SQL query text
    /// </summary>
    internal class QueryFormatter : DbExpressionVisitor
    {
        public static readonly ThreadVariable<Func<SqlPreCommandSimple, SqlPreCommandSimple>> PostFormatter = Statics.ThreadVariable<Func<SqlPreCommandSimple, SqlPreCommandSimple>>("QueryFormatterPostFormatter");


        StringBuilder sb = new StringBuilder();
        int indent = 2;
        int depth;

        ParameterExpression row = Expression.Parameter(typeof(IProjectionRow), "row");
        static PropertyInfo miReader = ReflectionTools.GetPropertyInfo((IProjectionRow row) => row.Reader);

        class DbParameterPair
        {
            internal DbParameter Parameter;
            internal string Name;
        }


        Dictionary<Expression, DbParameterPair> parameterExpressions = new Dictionary<Expression, DbParameterPair>();

        int parameter = 0;

        public string GetNextParamAlias()
        {
            return "@p" + (parameter++);
        }

        MethodInfo miCreateParameter = ReflectionTools.GetMethodInfo((ParameterBuilder s) => s.CreateParameter(null, SqlDbType.BigInt, null, false, null));

        DbParameterPair CreateParameter(ConstantExpression value)
        {
            string name = GetNextParamAlias();

            bool nullable = value.Type.IsClass || value.Type.IsNullable();
            Type clrType = value.Type.UnNullify();
            if (clrType.IsEnum)
                clrType = typeof(int);

            var typePair = Schema.Current.Settings.GetSqlDbTypePair(clrType);

            var pb = Connector.Current.ParameterBuilder;

            return new DbParameterPair
            {
                Parameter = pb.CreateParameter(name, typePair.SqlDbType, typePair.UserDefinedTypeName, nullable, value.Value ?? DBNull.Value),
                Name = name
            };
        }

        ObjectNameOptions objectNameOptions;

        private QueryFormatter()
        {
            objectNameOptions = ObjectName.CurrentOptions;
        }

        static internal SqlPreCommandSimple Format(Expression expression)
        {
            QueryFormatter qf = new QueryFormatter();
            qf.Visit(expression);

            var parameters = qf.parameterExpressions.Values.Select(pi => pi.Parameter).ToList();

            var sqlpc = new SqlPreCommandSimple(qf.sb.ToString(), parameters);

            return PostFormatter.Value == null ? sqlpc : PostFormatter.Value.Invoke(sqlpc);
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


        protected internal override Expression VisitRowNumber(RowNumberExpression rowNumber)
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

        protected internal override Expression VisitCase(CaseExpression cex)
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
            if (cex.DefaultValue != null)
            {
                sb.Append("ELSE ");
                Visit(cex.DefaultValue);
                AppendNewLine(Indentation.Outer);
            }
            sb.Append("END");
            AppendNewLine(Indentation.Outer);

            return cex;
        }

        protected internal override Expression VisitLike(LikeExpression like)
        {
            Visit(like.Expression);
            sb.Append(" LIKE ");
            Visit(like.Pattern);
            return like;
        }

        protected internal override Expression VisitExists(ExistsExpression exists)
        {
            sb.Append("EXISTS(");
            this.Visit(exists.Select);
            sb.Append(")");
            return exists;
        }

        protected internal override Expression VisitScalar(ScalarExpression exists)
        {
            sb.Append("(");
            this.Visit(exists.Select);
            sb.Append(")");
            return exists;
        }

        protected internal override Expression VisitIsNull(IsNullExpression isNull)
        {
            sb.Append("(");
            this.Visit(isNull.Expression);
            sb.Append(") IS NULL");
            return isNull;
        }

        protected internal override Expression VisitIsNotNull(IsNotNullExpression isNotNull)
        {
            sb.Append("(");
            this.Visit(isNotNull.Expression);
            sb.Append(") IS NOT NULL");
            return isNotNull;
        }

        protected internal override Expression VisitIn(InExpression inExpression)
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

        protected internal override Expression VisitSqlEnum(SqlEnumExpression sqlEnum)
        {
            sb.Append(sqlEnum.Value);
            return sqlEnum;
        }

        protected internal override Expression VisitSqlCast(SqlCastExpression castExpr)
        {
            sb.Append("CAST(");
            Visit(castExpr.Expression);
            sb.Append(" as ");
            sb.Append(castExpr.SqlDbType.ToString().ToUpperInvariant());
            if (castExpr.SqlDbType == SqlDbType.NVarChar || castExpr.SqlDbType == SqlDbType.VarChar)
                sb.Append("(MAX)");
            sb.Append(")");
            return castExpr;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value == null)
                sb.Append("NULL");
            else
            {
                if (!Schema.Current.Settings.IsDbType(c.Value.GetType().UnNullify()))
                    throw new NotSupportedException(string.Format("The constant for {0} is not supported", c.Value));

                var pi = parameterExpressions.GetOrCreate(c, () => this.CreateParameter(c));

                sb.Append(pi.Name);
            }
            return c;
        }

        protected internal override Expression VisitSqlConstant(SqlConstantExpression c)
        {
            if (c.Value == null)
                sb.Append("NULL");
            else
            {
                if (!Schema.Current.Settings.IsDbType(c.Value.GetType().UnNullify()))
                    throw new NotSupportedException(string.Format("The constant for {0} is not supported", c.Value));

                if (c.Value.Equals(true))
                    sb.Append("1");
                else if (c.Value.Equals(false))
                    sb.Append("0");
                else if (c.Value is string s)
                    sb.Append(s == "" ? "''" : ("'" + s + "'"));
                else
                    sb.Append(c.ToString());
            }

            return c;
        }

        protected internal override Expression VisitSqlVariable(SqlVariableExpression sve)
        {
            sb.Append(sve.VariableName);
            return sve;
        }


        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            sb.Append(column.Alias.ToString());
            sb.Append(".");
            sb.Append(column.Name.SqlEscape());

            return column;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            bool isFirst = sb.Length == 0;
            if (!isFirst)
            {
                AppendNewLine(Indentation.Inner);
                sb.Append("(");
            }

            sb.Append("SELECT ");
            if (select.IsDistinct)
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
            {
                this.AppendNewLine(Indentation.Inner);
                for (int i = 0, n = select.Columns.Count; i < n; i++)
                {
                    ColumnDeclaration column = select.Columns[i];
                    AppendColumn(column);
                    if (i < (n - 1))
                    {
                        sb.Append(", ");
                        this.AppendNewLine(Indentation.Same);
                    }
                    else
                    {
                        this.Indent(Indentation.Outer);
                    }
                }
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
            if (select.GroupBy.Count > 0)
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
            if (select.OrderBy.Count > 0)
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

            if (select.IsForXmlPathEmpty)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append("FOR XML PATH('')");
            }

            if (!isFirst)
            {
                sb.Append(")");
                AppendNewLine(Indentation.Outer);
            }

            return select;
        }

        Dictionary<AggregateSqlFunction, string> dic = new Dictionary<AggregateSqlFunction, string>
        {
            {AggregateSqlFunction.Average, "AVG"},
            {AggregateSqlFunction.StdDev, "STDEV"},
            {AggregateSqlFunction.StdDevP, "STDEVP"},
            {AggregateSqlFunction.Count, "COUNT"},
            {AggregateSqlFunction.Max, "MAX"},
            {AggregateSqlFunction.Min, "MIN"},
            {AggregateSqlFunction.Sum, "SUM"}
        };

        protected internal override Expression VisitAggregate(AggregateExpression aggregate)
        {
            sb.Append(dic[aggregate.AggregateFunction]);
            sb.Append("(");
            if (aggregate.Distinct)
                sb.Append("DISTINCT ");

            if (aggregate.Expression == null)
                sb.Append("*");
            else
                Visit(aggregate.Expression);
            sb.Append(")");

            return aggregate;
        }

        protected internal override Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
        {
            if (sqlFunction.Object != null)
            {
                Visit(sqlFunction.Object);
                sb.Append(".");
            }
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

        protected internal override Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
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
            ColumnExpression c = column.Expression as ColumnExpression;

            if (column.Name.HasText() && (c == null || c.Name != column.Name))
            {

                sb.Append(column.Name.SqlEscape());
                sb.Append(" = ");
                this.Visit(column.Expression);
            }
            else
            {
                this.Visit(column.Expression);
            }
        }

        protected internal override Expression VisitTable(TableExpression table)
        {
            sb.Append(table.Name.ToString());

            if (table.SystemTime != null && !(table.SystemTime is SystemTime.HistoryTable))
            {
                sb.Append(" ");
                WriteSystemTime(table.SystemTime);
            }
            return table;
        }

        private void WriteSystemTime(SystemTime st)
        {
            sb.Append("FOR SYSTEM_TIME ");

            if (st is SystemTime.AsOf asOf)
            {
                sb.Append("AS OF ");
                this.VisitSystemTimeConstant(asOf.DateTime);
            }
            else if (st is SystemTime.FromTo fromTo)
            {
                sb.Append("FROM ");
                this.VisitSystemTimeConstant(fromTo.StartDateTime);

                sb.Append(" TO ");
                this.VisitSystemTimeConstant(fromTo.EndtDateTime);
            }
            else if (st is SystemTime.Between between)
            {
                sb.Append("BETWEEN ");
                this.VisitSystemTimeConstant(between.StartDateTime);

                sb.Append(" AND ");
                this.VisitSystemTimeConstant(between.EndtDateTime);
            }
            else if (st is SystemTime.ContainedIn contained)
            {
                sb.Append("CONTAINED IN (");
                this.VisitSystemTimeConstant(contained.StartDateTime);

                sb.Append(", ");
                this.VisitSystemTimeConstant(contained.EndtDateTime);
                sb.Append(")");
            }
            else if (st is SystemTime.All)
            {
                sb.Append("ALL");
            }
            else
                throw new InvalidOperationException("Unexpected");

        }

        Dictionary<DateTime, ConstantExpression> systemTimeConstants = new Dictionary<DateTime, ConstantExpression>();
        void VisitSystemTimeConstant(DateTime datetime)
        {
            var c = systemTimeConstants.GetOrCreate(datetime, dt => Expression.Constant(dt));

            VisitConstant(c);
        }

        protected internal override SourceExpression VisitSource(SourceExpression source)
        {
            if (source is SourceWithAliasExpression)
            {
                if (source is TableExpression || source is SqlTableValuedFunctionExpression)
                    Visit(source);
                else
                {
                    sb.Append("(");
                    Visit(source);
                    sb.Append(")");
                }

                sb.Append(" AS ");
                sb.Append(((SourceWithAliasExpression)source).Alias.ToString());

                if (source is TableExpression ta && ta.WithHint != null)
                {
                    sb.Append(" WITH(" + ta.WithHint + ")");
                }
            }
            else
                this.VisitJoin((JoinExpression)source);

            return source;
        }

        protected internal override Expression VisitJoin(JoinExpression join)
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

        protected internal override Expression VisitSetOperator(SetOperatorExpression set)
        {
            VisitSetPart(set.Left);

            switch (set.Operator)
            {
                case SetOperator.Union: sb.Append("UNION"); break;
                case SetOperator.UnionAll: sb.Append("UNION ALL"); break;
                case SetOperator.Intersect: sb.Append("INTERSECT"); break;
                case SetOperator.Except: sb.Append("EXCEPT"); break;
                default:
                    throw new InvalidOperationException("Unexpected SetOperator {0}".FormatWith(set.Operator));
            }

            VisitSetPart(set.Right);

            return set;
        }

        void VisitSetPart(SourceWithAliasExpression source)
        {
            if (source is SelectExpression)
            {
                this.Indent(Indentation.Inner);
                VisitSelect((SelectExpression)source);
                this.Indent(Indentation.Outer);
            }
            else if (source is SetOperatorExpression)
            {
                VisitSetOperator((SetOperatorExpression)source);
            }
            else
                throw new InvalidOperationException("{0} not expected in SetOperatorExpression".FormatWith(source.ToString()));
        }

        protected internal override Expression VisitDelete(DeleteExpression delete)
        {
            sb.Append("DELETE ");
            sb.Append(delete.Name.ToString());
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

        protected internal override Expression VisitUpdate(UpdateExpression update)
        {
            sb.Append("UPDATE ");
            sb.Append(update.Name.ToString());
            sb.Append(" SET");
            this.AppendNewLine(Indentation.Inner);

            for (int i = 0, n = update.Assigments.Count; i < n; i++)
            {
                ColumnAssignment assignment = update.Assigments[i];
                if (i > 0)
                {
                    sb.Append(",");
                    this.AppendNewLine(Indentation.Same);
                }
                sb.Append(assignment.Column.SqlEscape());
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

        protected internal override Expression VisitInsertSelect(InsertSelectExpression insertSelect)
        {
            sb.Append("INSERT INTO ");
            sb.Append(insertSelect.Name.ToString());
            sb.Append("(");
            for (int i = 0, n = insertSelect.Assigments.Count; i < n; i++)
            {
                ColumnAssignment assignment = insertSelect.Assigments[i];
                if (i > 0)
                {
                    sb.Append(", ");
                    if (i % 4 == 0)
                        this.AppendNewLine(Indentation.Same);
                }
                sb.Append(assignment.Column.SqlEscape());
            }
            sb.Append(")");
            this.AppendNewLine(Indentation.Same);
            sb.Append("SELECT ");
            for (int i = 0, n = insertSelect.Assigments.Count; i < n; i++)
            {
                ColumnAssignment assignment = insertSelect.Assigments[i];
                if (i > 0)
                {
                    sb.Append(", ");
                    if (i % 4 == 0)
                        this.AppendNewLine(Indentation.Same);
                }
                this.Visit(assignment.Expression);
            }
            sb.Append(" FROM ");
            VisitSource(insertSelect.Source);
            return insertSelect;

        }

        protected internal override Expression VisitSelectRowCount(SelectRowCountExpression src)
        {
            sb.Append("SELECT @@rowcount");
            return src;
        }

        protected internal override Expression VisitCommandAggregate(CommandAggregateExpression cea)
        {
            for (int i = 0, n = cea.Commands.Count; i < n; i++)
            {
                CommandExpression command = cea.Commands[i];
                if (i > 0)
                {
                    sb.Append(";");
                    this.AppendNewLine(Indentation.Same);
                }
                this.Visit(command);
            }
            return cea;
        }


        protected internal override Expression VisitAggregateRequest(AggregateRequestsExpression aggregate)
        {
            throw InvalidSqlExpression(aggregate);
        }

        protected internal override Expression VisitChildProjection(ChildProjectionExpression child)
        {
            throw InvalidSqlExpression(child);
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            throw InvalidSqlExpression(c);
        }

        protected internal override Expression VisitEmbeddedEntity(EmbeddedEntityExpression eee)
        {
            throw InvalidSqlExpression(eee);
        }

        protected internal override Expression VisitImplementedBy(ImplementedByExpression reference)
        {
            throw InvalidSqlExpression(reference);
        }

        protected internal override Expression VisitImplementedByAll(ImplementedByAllExpression reference)
        {
            throw InvalidSqlExpression(reference);
        }

        protected internal override Expression VisitEntity(EntityExpression ee)
        {
            throw InvalidSqlExpression(ee);
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            throw InvalidSqlExpression(lambda);
        }

        protected override Expression VisitListInit(ListInitExpression init)
        {
            throw InvalidSqlExpression(init);
        }

        protected internal override Expression VisitLiteValue(LiteValueExpression lite)
        {
            throw InvalidSqlExpression(lite);
        }

        protected internal override Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            return base.VisitLiteReference(lite);
        }

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            throw InvalidSqlExpression(iv);
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            throw InvalidSqlExpression(m);
        }

        protected override Expression VisitMemberInit(MemberInitExpression init)
        {
            throw InvalidSqlExpression(init);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            throw InvalidSqlExpression(m);
        }

        protected internal override Expression VisitMList(MListExpression ml)
        {
            throw InvalidSqlExpression(ml);
        }

        protected internal override Expression VisitMListElement(MListElementExpression mle)
        {
            throw InvalidSqlExpression(mle);
        }

        protected override Expression VisitNew(NewExpression nex)
        {
            throw InvalidSqlExpression(nex);
        }



        protected override Expression VisitNewArray(NewArrayExpression na)
        {
            throw InvalidSqlExpression(na);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            throw InvalidSqlExpression(p);
        }

        protected internal override Expression VisitTypeEntity(TypeEntityExpression typeFie)
        {
            throw InvalidSqlExpression(typeFie);
        }

        protected internal override Expression VisitProjection(ProjectionExpression proj)
        {
            throw InvalidSqlExpression(proj);
        }

        protected internal override Expression VisitTypeImplementedBy(TypeImplementedByExpression typeIb)
        {
            throw InvalidSqlExpression(typeIb);
        }

        protected internal override Expression VisitTypeImplementedByAll(TypeImplementedByAllExpression typeIba)
        {
            throw InvalidSqlExpression(typeIba);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression b)
        {
            throw InvalidSqlExpression(b);
        }

        private InvalidOperationException InvalidSqlExpression(Expression expression)
        {
            return new InvalidOperationException("Unexepected expression on sql {0}".FormatWith(expression.ToString()));
        }

    }




    public class QueryPostFormatter : IDisposable
    {
        Func<SqlPreCommandSimple, SqlPreCommandSimple> prePostFormatter = null;

        public QueryPostFormatter(Func<SqlPreCommandSimple, SqlPreCommandSimple> postFormatter)
        {
            prePostFormatter = QueryFormatter.PostFormatter.Value;

            QueryFormatter.PostFormatter.Value = postFormatter;
        }

        public void Dispose()
        {
            QueryFormatter.PostFormatter.Value = prePostFormatter;
        }
    }

}
