using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using System.Data;
using System.Data.Common;
using System.Globalization;
using Signum.Engine.Sync;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Types;
using NpgsqlTypes;

namespace Signum.Engine.Linq;

/// <summary>
/// QueryFormatter is a visitor that converts an bound expression tree into SQL query text
/// </summary>
internal class QueryFormatter : DbExpressionVisitor
{
    public static readonly AsyncThreadVariable<Func<SqlPreCommandSimple, SqlPreCommandSimple>?> PostFormatter = Statics.ThreadVariable<Func<SqlPreCommandSimple, SqlPreCommandSimple>?>("QueryFormatterPostFormatter");

    Schema schema = Schema.Current;
    bool isPostgres = Schema.Current.Settings.IsPostgres;

    StringBuilder sb = new StringBuilder();
    int indent = 2;
    int depth;

    class DbParameterPair
    {
        internal DbParameter Parameter;
        internal string Name;

        public DbParameterPair(DbParameter parameter, string name)
        {
            Parameter = parameter;
            Name = name;
        }
    }


    Dictionary<Expression, DbParameterPair> parameterExpressions = new Dictionary<Expression, DbParameterPair>();

    int parameter = 0;

    public string GetNextParamAlias()
    {
        return "@p" + (parameter++);
    }

    DbParameterPair CreateParameter(ConstantExpression value)
    {
        string name = GetNextParamAlias();

        bool nullable = value.Type.IsClass || value.Type.IsNullable();
        object? val = value.Value;
        Type clrType = value.Type.UnNullify();
        if (clrType.IsEnum)
        {
            clrType = typeof(int);
            val = val == null ? (int?)null : Convert.ToInt32(val);
        }

        var typePair = isPostgres && clrType == typeof(DateTime) && value.Value is DateTime dt && dt.Kind == DateTimeKind.Utc ? new DbTypePair(new AbstractDbType(NpgsqlDbType.TimestampTz), null) :
            Schema.Current.Settings.GetSqlDbTypePair(clrType);

        var pb = Connector.Current.ParameterBuilder;

        var param = pb.CreateParameter(name, typePair.DbType, typePair.UserDefinedTypeName, nullable, value.GetMetadata()?.DateTimeKind ?? DateTimeKind.Unspecified, val ?? DBNull.Value);

        return new DbParameterPair(param, name);
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
            sb.Append(' ');
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
            sb.Append("COALESCE(");
            Visit(b.Left);
            sb.Append(',');
            Visit(b.Right);
            sb.Append(')');
        }
        else if (b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual)
        {
            sb.Append('(');
            Visit(b.Left);
            sb.Append(b.NodeType == ExpressionType.Equal ? " = " : " <> ");
            Visit(b.Right);
            sb.Append(')');
        }
        else if (b.NodeType == ExpressionType.ArrayIndex)
        {
            Visit(b.Left);
            sb.Append('[');
            Visit(b.Right);
            sb.Append(']');
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
                    if (this.isPostgres &&
                        (b.Left.Type == typeof(string) || b.Right.Type == typeof(string) ||
                        b.Left.Type == typeof(SqlHierarchyId) || b.Right.Type == typeof(SqlHierarchyId)))
                        sb.Append(" || ");
                    else
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


    protected internal override Expression VisitRowNumber(RowNumberExpression rowNumber)
    {
        sb.Append("ROW_NUMBER() OVER(ORDER BY ");
        for (int i = 0, n = rowNumber.OrderBy.Count; i < n; i++)
        {
            OrderExpression exp = rowNumber.OrderBy[i];
            if (i > 0)
                sb.Append(", ");
            this.Visit(exp.Expression);

            if (exp.OrderType == OrderType.Ascending)
                sb.Append(isPostgres ? " NULLS FIRST" : "");
            else
                sb.Append(isPostgres ? " DESC NULLS LAST" : " DESC");
        }
        sb.Append(')');
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
        sb.Append(')');
        return exists;
    }

    protected internal override Expression VisitScalar(ScalarExpression exists)
    {
        sb.Append('(');
        this.Visit(exists.Select);
        sb.Append(')');
        return exists;
    }

    protected internal override Expression VisitIsNull(IsNullExpression isNull)
    {
        sb.Append('(');
        this.Visit(isNull.Expression);
        sb.Append(") IS NULL");
        return isNull;
    }

    protected internal override Expression VisitIsNotNull(IsNotNullExpression isNotNull)
    {
        sb.Append('(');
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
            foreach (var obj in inExpression.Values!)
            {
                VisitConstant(Expression.Constant(obj));
                sb.Append(',');
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

    protected internal override Expression VisitSqlLiteral(SqlLiteralExpression sqlEnum)
    {
        sb.Append(sqlEnum.Value);
        return sqlEnum;
    }

    protected internal override Expression VisitSqlCast(SqlCastExpression castExpr)
    {
        sb.Append("CAST(");
        Visit(castExpr.Expression);
        sb.Append(" as ");
        sb.Append(castExpr.DbType.ToString(schema.Settings.IsPostgres));
        
        if (!schema.Settings.IsPostgres && (castExpr.DbType.SqlServer == SqlDbType.NVarChar || castExpr.DbType.SqlServer == SqlDbType.VarChar))
            sb.Append("(MAX)");

        sb.Append(')');
        return castExpr;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        if (c.Value == null)
            sb.Append("NULL");
        else
        {
            if (!schema.Settings.IsDbType(c.Value.GetType().UnNullify()))
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
            if (!schema.Settings.IsDbType(c.Value.GetType().UnNullify()))
                throw new NotSupportedException(string.Format("The constant for {0} is not supported", c.Value));

            if (!isPostgres && c.Value.Equals(true))
                sb.Append('1');
            else if (!isPostgres && c.Value.Equals(false))
                sb.Append('0');
            else if (c.Value is string s)
                sb.Append(s == "" ? "''" : ("'" + s + "'"));
            else if (c.Value is TimeSpan ts)
                sb.Append(@$"CONVERT(time, '{ts}')");
            else if (ReflectionTools.IsDecimalNumber(c.Value.GetType()))
                sb.Append(((IFormattable)c.Value).ToString("0.00####", CultureInfo.InvariantCulture));
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
        if (column.Name != null) //Is null for PostgressFunctions.unnest and friends (IQueryable<int> table-valued function)
        {
            sb.Append('.');
            sb.Append(column.Name.SqlEscape(isPostgres));
        }
        return column;
    }

    protected internal override Expression VisitSqlColumnList(SqlColumnListExpression sqlColumnList)
    {
        if (sqlColumnList.Columns.Count == 0)
            sb.Append("*");
        else
        {
            if (sqlColumnList.Columns.Count > 1)
                sb.Append("(");

            foreach (var col in sqlColumnList.Columns)
            {
                sb.Append(col.Alias.ToString());
                sb.Append('.');
                sb.Append(col.Name == "*" ? "*" : col.Name!.SqlEscape(isPostgres));
            }

            if (sqlColumnList.Columns.Count > 1)
                sb.Append(")");
        }

        return sqlColumnList;
    }

    protected internal override Expression VisitSelect(SelectExpression select)
    {
        bool isFirst = sb.Length == 0;
        if (!isFirst)
        {
            AppendNewLine(Indentation.Inner);
            sb.Append('(');
        }

        sb.Append("SELECT ");
        if (select.IsDistinct)
            sb.Append("DISTINCT ");

        if (select.Top != null && !this.isPostgres)
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
            VisitOrderBys(select.OrderBy);
        }

        if (select.Top != null && this.isPostgres)
        {
            this.AppendNewLine(Indentation.Same);
            sb.Append("LIMIT ");
            Visit(select.Top);
        }

        if (select.IsForXmlPathEmpty)
        {
            this.AppendNewLine(Indentation.Same);
            sb.Append("FOR XML PATH('')");
        }

        if (!isFirst)
        {
            sb.Append(')');
            AppendNewLine(Indentation.Outer);
        }

        return select;
    }

    private void VisitOrderBys(ReadOnlyCollection<OrderExpression> orderBys)
    {
        for (int i = 0, n = orderBys.Count; i < n; i++)
        {
            OrderExpression exp = orderBys[i];
            if (i > 0)
            {
                sb.Append(", ");
            }
            this.Visit(exp.Expression);
            if (exp.OrderType == OrderType.Ascending)
                sb.Append(isPostgres ? " NULLS FIRST" : "");
            else
                sb.Append(isPostgres ? " DESC NULLS LAST" : " DESC");
        }
    }

    string GetAggregateFunction(AggregateSqlFunction agg)
    {
        return agg switch
        {
            AggregateSqlFunction.Average => "AVG",
            AggregateSqlFunction.StdDev => !isPostgres ? "STDEV" : "stddev_samp",
            AggregateSqlFunction.StdDevP => !isPostgres? "STDEVP" : "stddev_pop",
            AggregateSqlFunction.Count => "COUNT",
            AggregateSqlFunction.CountDistinct => "COUNT",
            AggregateSqlFunction.Max => "MAX",
            AggregateSqlFunction.Min => "MIN",
            AggregateSqlFunction.Sum => "SUM",
            AggregateSqlFunction.string_agg => "string_agg",
            _ => throw new UnexpectedValueException(agg)
        };
    }

    protected internal override Expression VisitAggregate(AggregateExpression aggregate)
    {
        sb.Append(GetAggregateFunction(aggregate.AggregateFunction));
        sb.Append('(');
        if (aggregate.AggregateFunction == AggregateSqlFunction.CountDistinct)
            sb.Append("DISTINCT ");

        if (aggregate.Arguments.Count == 1 && aggregate.Arguments[0] == null && aggregate.AggregateFunction == AggregateSqlFunction.Count)
        {
            sb.Append('*');
        }
        else
        {
            for (int i = 0, n = aggregate.Arguments.Count; i < n; i++)
            {
                Expression exp = aggregate.Arguments[i];
                if (i > 0)
                    sb.Append(", ");
                this.Visit(exp);
            }

            if(aggregate.OrderBy != null && aggregate.OrderBy.Count > 0 && isPostgres)
            {
                sb.Append(" ORDER BY ");
                VisitOrderBys(aggregate.OrderBy!);
            }
        }
        sb.Append(')');

        if (aggregate.OrderBy != null && aggregate.OrderBy.Count > 0 && !isPostgres)
        {
            sb.Append(" WITHIN GROUP (ORDER BY ");
            VisitOrderBys(aggregate.OrderBy!);
            sb.Append(")");

        }



        return aggregate;
    }

    protected internal override Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
    {
        if (isPostgres && sqlFunction.SqlFunction == PostgresFunction.EXTRACT.ToString())
        {
            sb.Append(sqlFunction.SqlFunction);
            sb.Append('(');
            this.Visit(sqlFunction.Arguments[0]);
            sb.Append(" from ");
            this.Visit(sqlFunction.Arguments[1]);
            sb.Append(')');
        }
        else if(isPostgres && PostgressOperator.All.Contains(sqlFunction.SqlFunction))
        {
            sb.Append('(');
            this.Visit(sqlFunction.Arguments[0]);
            sb.Append(" " + sqlFunction.SqlFunction + " ");
            this.Visit(sqlFunction.Arguments[1]);
            sb.Append(')');
        }
        else if (sqlFunction.SqlFunction == SqlFunction.COLLATE.ToString())
        {
            this.Visit(sqlFunction.Arguments[0]);
            sb.Append(" COLLATE ");
            if (sqlFunction.Arguments[1] is SqlConstantExpression ce)
                sb.Append((string)ce.Value!);
        }
        else if (sqlFunction.SqlFunction == (isPostgres ? SqlFunction.AtTimeZone.ToString().ToLower() : SqlFunction.AtTimeZone.ToString()))
        {
            this.Visit(sqlFunction.Object);
            sb.Append(" AT TIME ZONE ");
            this.Visit(sqlFunction.Arguments.SingleEx());
        }
        else
		{
            if (sqlFunction.Object != null)
            {
                Visit(sqlFunction.Object);
                sb.Append('.');
            }
            sb.Append(sqlFunction.SqlFunction);
            sb.Append('(');
            for (int i = 0, n = sqlFunction.Arguments.Count; i < n; i++)
            {
                Expression exp = sqlFunction.Arguments[i];
                if (i > 0)
                    sb.Append(", ");
                this.Visit(exp);
            }
            sb.Append(')');
        }
        return sqlFunction;
    }

    protected internal override Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
    {
        sb.Append(sqlFunction.FunctionName);
        sb.Append('(');
        for (int i = 0, n = sqlFunction.Arguments.Count; i < n; i++)
        {
            Expression exp = sqlFunction.Arguments[i];
            if (i > 0)
                sb.Append(", ");
            this.Visit(exp);
        }
        sb.Append(')');

        return sqlFunction;
    }

    private void AppendColumn(ColumnDeclaration column)
    {
        ColumnExpression? c = column.Expression as ColumnExpression;

        if (column.Name.HasText() && (c == null || c.Name != column.Name))
        {
            this.Visit(column.Expression);
            sb.Append(" as ");
            sb.Append(column.Name.SqlEscape(isPostgres));
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
            sb.Append(' ');
            WriteSystemTime(table.SystemTime);
        }
        return table;
    }

    private void WriteSystemTime(SystemTime st)
    {
        sb.Append("FOR SYSTEM_TIME ");

        switch (st)
        {
            case SystemTime.AsOf asOf:
                {
                    sb.Append("AS OF ");
                    this.VisitSystemTimeConstant(asOf.DateTime);

                    break;
                }
            case SystemTime.Between between:
                {
                    sb.Append("BETWEEN ");
                    this.VisitSystemTimeConstant(between.StartDateTime);

                    sb.Append(" AND ");
                    this.VisitSystemTimeConstant(between.EndtDateTime);

                    break;
                }
            case SystemTime.ContainedIn contained:
                {
                    sb.Append("CONTAINED IN (");
                    this.VisitSystemTimeConstant(contained.StartDateTime);

                    sb.Append(", ");
                    this.VisitSystemTimeConstant(contained.EndtDateTime);
                    sb.Append(')');
                    break;
                }
            case SystemTime.All:
                {
                    sb.Append("ALL");
                    break;
                }
            default:
                throw new UnexpectedValueException(st);
        }
    }

    Dictionary<DateTimeOffset, ConstantExpression> systemTimeConstants = new Dictionary<DateTimeOffset, ConstantExpression>();
    void VisitSystemTimeConstant(DateTimeOffset datetime)
    {
        var c = systemTimeConstants.GetOrCreate(datetime, dt => Expression.Constant(dt));

        VisitConstant(c);
    }

    protected internal override SourceExpression VisitSource(SourceExpression source)
    {
        if (source is SourceWithAliasExpression swae)
        {
            if (source is TableExpression || source is SqlTableValuedFunctionExpression)
                Visit(source);
            else
            {
                sb.Append('(');
                Visit(source);
                sb.Append(')');
            }

            sb.Append(" AS ");
            sb.Append(swae.Alias.ToString());

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
                sb.Append(isPostgres ? "JOIN LATERAL " : "CROSS APPLY ");
                break;
            case JoinType.OuterApply:
                sb.Append(isPostgres ? "LEFT JOIN LATERAL " : "OUTER APPLY ");
                break;
        }

        bool needsMoreParenthesis = (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply) && join.Right is JoinExpression;

        if (needsMoreParenthesis)
            sb.Append('(');

        this.VisitSource(join.Right);

        if (needsMoreParenthesis)
            sb.Append(')');

        if (join.Condition != null)
        {
            this.AppendNewLine(Indentation.Inner);
            sb.Append("ON ");
            this.Visit(join.Condition);
            this.Indent(Indentation.Outer);
        }
        else if (isPostgres && join.JoinType != JoinType.CrossJoin)
        {
            this.AppendNewLine(Indentation.Inner);
            sb.Append("ON true");
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
        if (source is SelectExpression se)
        {
            this.Indent(Indentation.Inner);
            VisitSelect(se);
            this.Indent(Indentation.Outer);
        }
        else if (source is SetOperatorExpression soe)
        {
            VisitSetOperator(soe);
        }
        else
            throw new InvalidOperationException("{0} not expected in SetOperatorExpression".FormatWith(source.ToString()));
    }

    protected internal override Expression VisitDelete(DeleteExpression delete)
    {
        using (this.PrintSelectRowCount(delete.ReturnRowCount))
        {
            sb.Append("DELETE FROM ");
            if (delete.Alias != null)
                sb.Append(delete.Alias.ToString());
            else
                sb.Append(delete.Name.ToString());
            this.AppendNewLine(Indentation.Same);

            if (isPostgres)
                sb.Append("USING ");
            else
                sb.Append("FROM ");

            VisitSource(delete.Source);
            if (delete.Where != null)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append("WHERE ");
                Visit(delete.Where);
            }
            sb.Append(";");
            return delete;
        }
    }

    protected internal override Expression VisitUpdate(UpdateExpression update)
    {
        using (this.PrintSelectRowCount(update.ReturnRowCount))
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
                    sb.Append(',');
                    this.AppendNewLine(Indentation.Same);
                }
                sb.Append(assignment.Column.SqlEscape(isPostgres));
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
            sb.Append(";");
            return update;
        }
    }

    protected internal override Expression VisitInsertSelect(InsertSelectExpression insertSelect)
    {
        using (this.PrintSelectRowCount(insertSelect.ReturnRowCount))
        {
            sb.Append("INSERT INTO ");
            sb.Append(insertSelect.Name.ToString());
            sb.Append('(');
            for (int i = 0, n = insertSelect.Assigments.Count; i < n; i++)
            {
                ColumnAssignment assignment = insertSelect.Assigments[i];
                if (i > 0)
                {
                    sb.Append(", ");
                    if (i % 4 == 0)
                        this.AppendNewLine(Indentation.Same);
                }
                sb.Append(assignment.Column.SqlEscape(isPostgres));
            }
            sb.Append(')');
            this.AppendNewLine(Indentation.Same);
            if(this.isPostgres && Administrator.IsIdentityBehaviourDisabled(insertSelect.Table))
            {
                sb.Append("OVERRIDING SYSTEM VALUE");
                this.AppendNewLine(Indentation.Same);
            }
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

            sb.Append(";");
            return insertSelect;
        }
    }

    protected internal IDisposable? PrintSelectRowCount(bool returnRowCount)
    {
        if (returnRowCount == false)
            return null;

        if (!this.isPostgres)
        {
            return new Disposable(() =>
            {
                sb.AppendLine();
                sb.AppendLine("SELECT @@rowcount");
            });
        }
        else
        {
            sb.Append("WITH rows AS (");
            this.AppendNewLine(Indentation.Inner);

            return new Disposable(() =>
            {
                if (sb[sb.Length - 1] == ';')
                    sb.Remove(sb.Length - 1, 1);

                this.AppendNewLine(Indentation.Same);
                sb.Append("RETURNING 1");
                this.AppendNewLine(Indentation.Outer);
                sb.Append(')');
                this.AppendNewLine(Indentation.Same);
                sb.Append("SELECT CAST(COUNT(*) AS INTEGER) FROM rows");
            });
        }
    }

    protected internal override Expression VisitCommandAggregate(CommandAggregateExpression cea)
    {
        for (int i = 0, n = cea.Commands.Count; i < n; i++)
        {
            CommandExpression command = cea.Commands[i];
            if (i > 0)
            {
                //sb.Append(';');
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
        throw InvalidSqlExpression(lite);
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

    private static InvalidOperationException InvalidSqlExpression(Expression expression)
    {
        return new InvalidOperationException("Unexepected expression on sql {0}".FormatWith(expression.ToString()));
    }

}


public class QueryPostFormatter : IDisposable
{
    Func<SqlPreCommandSimple, SqlPreCommandSimple>? prePostFormatter = null;

    public QueryPostFormatter(Func<SqlPreCommandSimple, SqlPreCommandSimple> postFormatter)
    {
        prePostFormatter = QueryFormatter.PostFormatter.Value;

        QueryFormatter.PostFormatter.Value = postFormatter;
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public void Dispose()
    {
        QueryFormatter.PostFormatter.Value = prePostFormatter;
    }
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
}
