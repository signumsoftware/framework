using Microsoft.SqlServer.Server;
using NpgsqlTypes;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Signum.Engine.Linq;

/// <summary>
/// Nominator is a class that walks an expression tree bottom up, determining the set of
/// candidate expressions that are possible columns of a select expression
/// </summary>
internal class DbExpressionNominator : DbExpressionVisitor
{

    // existingAliases is null when used in QueryBinder, not ColumnProjector
    // this allows to make function changes in where clausules but keeping the full expression (not compressing it in one column

    bool isFullNominate;

    bool IsFullNominateOrAggresive { get { return isFullNominate || isGroupKey; } }

    bool isGroupKey = false;

    Expression? isNotNullRoot;

    bool innerProjection = false;

    readonly HashSet<Expression> candidates = new HashSet<Expression>();

    T Add<T>(T expression) where T : Expression?
    {
        if (expression == null)
            return null!;

        this.candidates.Add(expression);
        return expression;
    }

    bool Has(Expression expression)
    {
        return this.candidates.Contains(expression);
    }


    bool isPostgres;
    private DbExpressionNominator() 
    {
        this.isPostgres = Schema.Current.Settings.IsPostgres;
    }

    static internal HashSet<Expression> Nominate(Expression? expression, out Expression newExpression, bool isGroupKey = false)
    {
        var n = new DbExpressionNominator { isFullNominate = false, isGroupKey = isGroupKey };
        newExpression = n.Visit(expression)!;
        return n.candidates;
    }

    [return: NotNullIfNotNull("expression")]
    static internal Expression? FullNominate(Expression? expression)
    {
        var n = new DbExpressionNominator { isFullNominate = true };
        Expression? result = n.Visit(expression);

        return result;
    }

    [return: NotNullIfNotNull("expression")]
    static internal Expression? FullNominateNotNullable(Expression? expression)
    {
        var n = new DbExpressionNominator { isFullNominate = true, isNotNullRoot = expression };
        Expression? result = n.Visit(expression);

        return result;
    }

    [return:NotNullIfNotNull("exp")]
    public override Expression? Visit(Expression? exp)
    {
        Expression result = base.Visit(exp)!;
        if (isFullNominate && result != null && !Has(result) && !IsExcluded(exp!) && !ExtractDayOfWeek(result, out var bla))
            throw new InvalidOperationException("The expression can not be translated to SQL: " + result.ToString());


        return result!;
    }

    private static bool IsExcluded(Expression exp)
    {
        if (exp is not DbExpression expDb)
            return false;

        return expDb.DbNodeType switch
        {
            DbExpressionType.Table or 
            DbExpressionType.Select or 
            DbExpressionType.Projection or 
            DbExpressionType.Join or 
            DbExpressionType.AggregateRequest or 
            DbExpressionType.Update or 
            DbExpressionType.Delete or 
            DbExpressionType.CommandAggregate => true,
            _ => false,
        };
    }


    #region Not Null Root
    protected internal override Expression VisitPrimaryKey(PrimaryKeyExpression pk)
    {
        if (pk == isNotNullRoot)
            return Add(pk.Value);

        return base.VisitPrimaryKey(pk);
    }

    protected internal override Expression VisitEmbeddedEntity(EmbeddedEntityExpression eee)
    {
        if (eee == isNotNullRoot)
            return Add(new CaseExpression(new[] {
                new When(Expression.Equal(eee.HasValue, Expression.Constant(false)), new SqlConstantExpression(null, typeof(bool?)))
                },
                eee.HasValue));

        return base.VisitEmbeddedEntity(eee);
    }

    protected internal override Expression VisitLiteReference(LiteReferenceExpression lite)
    {
        if (lite == isNotNullRoot)
        {
            if (lite.Reference is EntityExpression rr)
                return Add(rr.ExternalId.Value);
            if (lite.Reference is ImplementedByExpression ib)
                return Add(GetImplmentedById(ib));
            if (lite.Reference is ImplementedByAllExpression iba)
                return Add(iba.TypeId.TypeColumn.Value);
        }

        return base.VisitLiteReference(lite);
    }


    protected internal override Expression VisitEntity(EntityExpression ee)
    {
        if (ee == isNotNullRoot)
            return Add(ee.ExternalId.Value);

        return base.VisitEntity(ee);
    }

    protected internal override Expression VisitTypeEntity(TypeEntityExpression typeFie)
    {
        if (typeFie == isNotNullRoot)
            return Add(typeFie.ExternalId.Value);

        return base.VisitTypeEntity(typeFie);
    }

    protected internal override Expression VisitImplementedBy(ImplementedByExpression ib)
    {
        if (ib == isNotNullRoot)
            return Add(GetImplmentedById(ib));

        return base.VisitImplementedBy(ib);
    }

    private static Expression GetImplmentedById(ImplementedByExpression ib)
    {
        return ib.Implementations.IsEmpty() ? new SqlConstantExpression(null, typeof(int?)) :
            ib.Implementations.Select(a => a.Value.ExternalId.Value).Aggregate((id1, id2) => Expression.Coalesce(id1, id2));
    }

    protected internal override Expression VisitTypeImplementedBy(TypeImplementedByExpression typeIb)
    {
        if (typeIb == isNotNullRoot)
            return Add(typeIb.TypeImplementations.IsEmpty() ? new SqlConstantExpression(null, typeof(int?)) :
                typeIb.TypeImplementations.Select(a => a.Value.Value).Aggregate((id1, id2) => Expression.Coalesce(id1, id2)));

        return base.VisitTypeImplementedBy(typeIb);
    }

    protected internal override Expression VisitImplementedByAll(ImplementedByAllExpression iba)
    {
        if (iba == isNotNullRoot)
            return Add(iba.TypeId.TypeColumn.Value);

        return base.VisitImplementedByAll(iba);
    }

    protected internal override Expression VisitTypeImplementedByAll(TypeImplementedByAllExpression typeIba)
    {
        if (typeIba == isNotNullRoot)
            return Add(typeIba.TypeColumn);

        return base.VisitTypeImplementedByAll(typeIba);
    }
    #endregion




    protected internal override Expression VisitColumn(ColumnExpression column)
    {
        if (IsFullNominateOrAggresive || !innerProjection)
            return Add(column);

        return column;
    }

    protected override Expression VisitNew(NewExpression nex)
    {
        if (isFullNominate)
        {
            ReadOnlyCollection<Expression> args = this.Visit(nex.Arguments);
            if (args != nex.Arguments)
            {
                if (nex.Members != null)
                    // anonymous types require exaxt type matching
                    nex = Expression.New(nex.Constructor!, args, nex.Members);
                else
                    nex = Expression.New(nex.Constructor!, args);
            }

            if (args.All(a => Has(a)))
                return Add(nex);

            return nex;
        }
        else
            return base.VisitNew(nex);
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        Type ut = c.Type.UnNullify();
        if (ut == typeof(PrimaryKey) && isFullNominate)
        {
            if (c.Value == null)
                return Add(Expression.Constant(null, typeof(object)));
            else
                return Add(Expression.Constant(((PrimaryKey)c.Value).Object));
        }

        if (!innerProjection && IsFullNominateOrAggresive)
        {
            if (ut == typeof(DayOfWeek))
            {
                var dayNumber = c.Value == null ? (int?)null :
                    isPostgres ? (int)(DayOfWeek)c.Value :
                    ToDayOfWeekExpression.ToSqlWeekDay((DayOfWeek)c.Value, ((SqlServerConnector)Connector.Current).DateFirst);

                return new ToDayOfWeekExpression(Add(Expression.Constant(dayNumber, typeof(int?))));
            }

            if (Schema.Current.Settings.IsDbType(ut))
                return Add(c);

            if (c.Type == typeof(object) && (c.IsNull() || (Schema.Current.Settings.IsDbType(c.Value!.GetType()))))
                return Add(c);
        }

        return c;
    }


    protected internal override Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
    {
        //We can not assume allways true because neasted projections
        Expression? obj = Visit(sqlFunction.Object);
        ReadOnlyCollection<Expression> args = Visit(sqlFunction.Arguments);
        if (args != sqlFunction.Arguments || obj != sqlFunction.Object)
            sqlFunction = new SqlFunctionExpression(sqlFunction.Type, obj, sqlFunction.SqlFunction, args); ;

        if (args.All(Has) && (obj == null || Has(obj)))
            return Add(sqlFunction);

        return sqlFunction;
    }

    protected internal override Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
    {
        ReadOnlyCollection<Expression> args = Visit(sqlFunction.Arguments, a => Visit(a));
        if (args != sqlFunction.Arguments)
            sqlFunction = new SqlTableValuedFunctionExpression(sqlFunction.SqlFunction, sqlFunction.ViewTable, sqlFunction.SingleColumnType, sqlFunction.Alias, args); ;

        if (args.All(Has))
            return Add(sqlFunction);

        return sqlFunction;
    }

    protected internal override Expression VisitSqlCast(SqlCastExpression castExpr)
    {
        var expression = Visit(castExpr.Expression);
        if (expression != castExpr.Expression)
            castExpr = new SqlCastExpression(castExpr.Type, expression!, castExpr.DbType);
        return Add(castExpr);
    }

    protected internal override Expression VisitSqlCastLazy(SqlCastLazyExpression castExpr)
    {
        var expression = Visit(castExpr.Expression);
        if(isFullNominate)
            return Add(new SqlCastExpression(castExpr.Type, expression!, castExpr.DbType));

        if (expression != castExpr.Expression)
            castExpr = new SqlCastLazyExpression(castExpr.Type, expression!, castExpr.DbType);
        return castExpr;
    }

    protected internal override Expression VisitSqlConstant(SqlConstantExpression sqlConstant)
    {
        if (!innerProjection)
        {
            if (sqlConstant.Type.UnNullify() == typeof(PrimaryKey))
            {
                if (isFullNominate)
                {
                    if (sqlConstant.Value == null)
                        return Add(new SqlConstantExpression(null, typeof(object)));
                    else
                        return Add(new SqlConstantExpression(((PrimaryKey)sqlConstant.Value).Object));
                }
                else
                {
                    return sqlConstant;
                }
            }

            return Add(sqlConstant);
        }
        return sqlConstant;
    }

    protected internal override Expression VisitSqlVariable(SqlVariableExpression sve)
    {
        return Add(sve);
    }

    protected internal override Expression VisitCase(CaseExpression cex)
    {
        var newWhens = Visit(cex.Whens, w => VisitWhen(w));
        var newDefault = Visit(cex.DefaultValue);

        if (newWhens != cex.Whens || newDefault != cex.DefaultValue)
            cex = new CaseExpression(newWhens, newDefault);

        if (newWhens.All(w => Has(w.Condition) && Has(w.Value)) && (newDefault == null || Has(newDefault)))
            return Add(cex);

        return cex;
    }

    protected Expression? TrySqlToString(MethodCallExpression m)
    {
        var expression = m.Object!;

        if (expression!.Type.UnNullify() == typeof(PrimaryKey))
            expression = SmartEqualizer.UnwrapPrimaryKey(expression);

        if (IsFullNominateOrAggresive && m.Arguments.Any() && (expression.Type.UnNullify() == typeof(DateTime) || ReflectionTools.IsNumber(expression.Type.UnNullify())) && Connector.Current.SupportsFormat)
            return GetFormatToString(m);

        var newExp = Visit(expression);
        if (Has(newExp) && IsFullNominateOrAggresive)
        {
            if (newExp.Type.UnNullify().IsEnum)
                throw new InvalidOperationException($"Impossible to get the ToString of {newExp.Type.Name} because is not in the Schema");

            var cast = new SqlCastExpression(typeof(string), newExp);
            return Add(cast);
        }
        return null;
    }

    protected Expression GetFormatToString(MethodCallExpression m, string? defaultFormat = null)
    {
        var culture = m.TryGetArgument("culture")?.Let(e => (CultureInfo)((ConstantExpression)Visit(e)).Value!) ?? CultureInfo.CurrentCulture;

        string? format = m.TryGetArgument("format")?.Let(e => (string)((ConstantExpression)Visit(e)).Value!) ?? defaultFormat!;

        var obj = Visit(m.Object!);

        if ((!culture.IsReadOnly || isPostgres) && (obj.Type.UnNullify() == typeof(DateTime) || obj.Type.UnNullify() == typeof(DateOnly)))
            format = DateTimeExtensions.ToCustomFormatString(format, culture);

        if (isPostgres)
            return Add(new SqlFunctionExpression(typeof(string), null, "to_char", new[] {
            obj,
            new SqlConstantExpression(ToPostgres(format), typeof(string))
            }));


        return Add(new SqlFunctionExpression(typeof(string), null, "Format", new[] {
            obj,
            new SqlConstantExpression(format, typeof(string)),
            culture.Name.HasText() ? new SqlConstantExpression(culture.Name) : null,
        }.NotNull()));
    }

    //https://database.guide/list-of-the-custom-date-time-format-strings-supported-by-the-format-function-in-sql-server/
    //https://www.postgresql.org/docs/current/functions-formatting.html
    static Dictionary<string, string> postgresReplacement = new Dictionary<string, string>()
    {
        { "d", "DD"},
        { "dd", "DD"},
        { "ddd", "Dy"},
        { "dddd", "Day"},
        { "f", "MS"},
        { "ff", "MS"},
        { "fff", "MS"},
        { "ffff", "US"},
        { "fffff", "US"},
        { "ffffff", "US"},
        { "fffffff", "US"},
        { "ffffffff", "US"},
        { "F", "MS"},
        { "FF", "MS"},
        { "FFF", "MS"},
        { "FFFF", "US"},
        { "FFFFF", "US"},
        { "FFFFFF", "US"},
        { "FFFFFFF", "US"},
        { "FFFFFFFF", "US"},
        { "g", "ad"},
        { "gg", "ad"},
        { "h", "HH12"},
        { "hh", "HH12"},
        { "H", "HH24"},
        { "HH", "HH24"},
        { "K", "OF"},
        { "m", "MI"},
        { "mm", "MI"},
        { "M", "MM"},
        { "MM", "MM"},
        { "MMM", "Mon"},
        { "MMMM", "Month"},
        { "s", "SS"},
        { "ss", "SS"},
        { "t", "AM"},
        { "tt", "AM"},
        { "y", "Y"},
        { "yy", "YY"},
        { "yyy", "YYY"},
        { "yyyy", "YYYY"},
        { "yyyyy", "YYYY"},
        { "z", "TZ"},
        { "zz", "TZ"},
        { "zzz", "TZ"},
    };

    static string? ToPostgres(string? format)
    {
        if (format == null)
            return null;

        var result= Regex.Replace(format, @"\b\w+\b", m => postgresReplacement.TryGetC(m.Value) ?? m.Value);
        return result;
    }

    protected Expression? TrySqlFunction(Expression? obj, PostgresFunction postgresFunction, Type type, params Expression[] expression)
    {
        return TrySqlFunction(obj, postgresFunction.ToString(), type, expression);
    }

    protected Expression? TrySqlFunction(Expression? obj, SqlFunction sqlFunction, Type type, params Expression[] expression)
    {
        return TrySqlFunction(obj, isPostgres? sqlFunction.ToString().ToLower() : sqlFunction.ToString(), type, expression);
    }

    protected Expression? TrySqlFunction(Expression? obj, string sqlFunction, Type type, params Expression[] expression)
    {
        if (innerProjection)
            return null;

        Expression? newObj = null;
        if (obj != null)
        {
            newObj = Visit(obj);
            if (!Has(newObj))
                return null;
        }

        expression = expression.NotNull().ToArray();
        Expression[] newExpressions = new Expression[expression.Length];

        for (int i = 0; i < expression.Length; i++)
        {
            newExpressions[i] = Visit(expression[i]);
            if (!Has(newExpressions[i]))
                return null;
        }

        return Add(new SqlFunctionExpression(type, newObj, sqlFunction, newExpressions));
    }

    private Expression? TrySqlDifference(SqlEnums unit, Type type, Expression expression)
    {
        if (innerProjection)
            return null;

        expression = expression.RemoveUnNullify();

        if (expression is BinaryExpression be && be.NodeType == ExpressionType.Subtract)
            return TrySqlDifference(unit, type, be.Left, be.Right);

        if (expression is MethodCallExpression mc && mc.Method.Name == nameof(DateTime.Subtract))
            return TrySqlDifference(unit, type, mc.Object!, mc.Arguments.SingleEx());

        return null;
    }

    static int DaysBetween(DateOnly a, DateOnly b) => a.DayNumber - b.DayNumber;

    private Expression? TrySqlDifference(SqlEnums unit, Type type, Expression leftSide, Expression rightSide)
    {
        Expression left = Visit(leftSide);
        if (!Has(left.RemoveNullify()))
            return null;

        Expression right = Visit(rightSide);
        if (!Has(right.RemoveNullify()))
            return null;

        if (isPostgres)
        {
            if (unit == SqlEnums.day && left.Type == typeof(DateOnly) && right.Type == typeof(DateOnly))
                return Add(Expression.Convert(Expression.Subtract(left, right, ReflectionTools.GetMethodInfo(()=> DaysBetween(DateOnly.MinValue, DateOnly.MinValue))), typeof(double)));
          
            var secondsDouble = new SqlFunctionExpression(typeof(double), null, PostgresFunction.EXTRACT.ToString(), new Expression[]
            {
                new SqlLiteralExpression(SqlEnums.epoch),
                Expression.Subtract(left, right),
            });

            if (unit == SqlEnums.second)
                return Add(secondsDouble);


            if (unit == SqlEnums.millisecond)
                return Add(Expression.Multiply(secondsDouble, new SqlConstantExpression(1000.0)));

            double scale = unit switch
            {
                SqlEnums.minute => 60,
                SqlEnums.hour => 60 * 60,
                SqlEnums.day => 60 * 60 * 24,
                _ => throw new UnexpectedValueException(unit),
            };

            return Add(Expression.Multiply(secondsDouble, new SqlConstantExpression(1 / scale)));
        }
        else
        {
            SqlFunctionExpression DateDiff(SqlEnums unit)
            {
                var functionName = Connector.Current.SupportsDateDifBig ? SqlFunction.DATEDIFF_BIG.ToString() : SqlFunction.DATEDIFF.ToString();

                return new SqlFunctionExpression(typeof(double), null, functionName, new Expression[]
                {
                    new SqlLiteralExpression(unit),
                    right,
                    left
                });
            }

            return unit switch
            {
                SqlEnums.day => Add(Expression.Multiply(DateDiff(SqlEnums.minute), new SqlConstantExpression(1 / (60 * 24.0)))),
                SqlEnums.hour => Add(Expression.Multiply(DateDiff(SqlEnums.minute), new SqlConstantExpression(1 / (60.0)))),
                SqlEnums.minute => Add(Expression.Multiply(DateDiff(SqlEnums.second), new SqlConstantExpression(1 / (60.0)))),
                SqlEnums.second => Add(Expression.Multiply(DateDiff(SqlEnums.millisecond), new SqlConstantExpression(1 / (1000.0)))),
                SqlEnums.millisecond => Add(DateDiff(SqlEnums.millisecond)),
                _ => throw new UnexpectedValueException(unit),
            };
        }
    }

    private Expression? TrySqlDate(Expression expression)
    {
        Expression expr = Visit(expression);
        if (innerProjection || !Has(expr))
            return null;

        if (isPostgres)
        {
            return Add(new SqlCastExpression(typeof(DateTime), expr, new AbstractDbType(NpgsqlDbType.Date)));
        }
        else
        {
            if (Connector.Current.AllowsConvertToDate)
            {
                return Add(new SqlFunctionExpression(typeof(DateTime), null, SqlFunction.CONVERT.ToString(), new[]
                {
                    new SqlConstantExpression(SqlDbType.Date),
                    expr,
                    new SqlConstantExpression(101)
                }));
            }

            return Add(new SqlCastExpression(typeof(DateTime),
                   new SqlFunctionExpression(typeof(double), null, SqlFunction.FLOOR.ToString(),
                       new[] { new SqlCastExpression(typeof(double), expr) }
                   )));
        }
    }


    private Expression? TrySqlTime(Expression expression)
    {
        Expression expr = Visit(expression);
        if (innerProjection || !Has(expr))
            return null;

        if (isPostgres)
            return Add(new SqlCastExpression(typeof(TimeSpan), expression));

        if (Connector.Current.AllowsConvertToTime)
            return Add(new SqlFunctionExpression(typeof(TimeSpan), null, SqlFunction.CONVERT.ToString(), new[]
            {
                isPostgres ? new SqlConstantExpression(NpgsqlDbType.Time) : new SqlConstantExpression(SqlDbType.Time),
                expr,
            }));

        throw new InvalidOperationException("{0} not supported on SQL Server 2005");
    }

    private Expression? TrySqlDayOftheWeek(Expression expression)
    {
        Expression expr = Visit(expression);
        if (innerProjection || !Has(expr))
            return null;

        var number = TrySqlFunction(null, GetDatePart(), typeof(int?), new SqlLiteralExpression(isPostgres ? SqlEnums.dow : SqlEnums.weekday), expr)!;

        Add(number);

        return new ToDayOfWeekExpression(number).TryConvert(typeof(DayOfWeek));
    }

    private Expression? TrySqlStartOf(Expression expression, SqlEnums part)
    {
        Expression expr = Visit(expression);
        if (innerProjection || !Has(expr))
            return null;

        if (isPostgres)
        {
            Expression? result =
                TrySqlFunction(null, PostgresFunction.date_trunc, expr.Type,
                    new SqlConstantExpression(part.ToString()), expr);

            return Add(result);
        }
        else
        {
            if (part == SqlEnums.second)
            {
                Expression result =
                    TrySqlFunction(null, SqlFunction.DATEADD, expr.Type, new SqlLiteralExpression(SqlEnums.millisecond),
                        Expression.Negate(TrySqlFunction(null, SqlFunction.DATEPART, typeof(int), new SqlLiteralExpression(SqlEnums.millisecond), expr)!), expr)!;

                return Add(result);
            }
            else
            {
                Expression result =
                    TrySqlFunction(null, SqlFunction.DATEADD, expr.Type, new SqlLiteralExpression(part),
                        TrySqlFunction(null, SqlFunction.DATEDIFF, typeof(int), new SqlLiteralExpression(part), new SqlConstantExpression(0), expr)!,
                        new SqlConstantExpression(0))!;

                return Add(result);
            }
        }
    }

    private Expression? TryAddSubtractDateTimeTimeSpan(Expression date, Expression time, bool add)
    {
        Expression exprDate = Visit(date);
        Expression exprTime = Visit(time);
        if (innerProjection || !Has(exprDate) || !Has(exprTime))
            return null;

        //Sql Server DateTime + DateTime
        //Postgres TimeSpan + Time
        var castDate = new SqlCastExpression(typeof(DateTime), exprDate, new AbstractDbType(SqlDbType.DateTime, NpgsqlDbType.Timestamp)); 
        var castTime = new SqlCastExpression(typeof(TimeSpan), exprTime, new AbstractDbType(SqlDbType.DateTime, NpgsqlDbType.Time)); 

        var result = add ? Expression.Add(castDate, castTime) :
            Expression.Subtract(castDate, castTime);

        return Add(result);
    }

    private Expression? TryAddSubtractTimeSpan(Expression timeA, Expression timeB, bool add)
    {
        if (IsTimeSpanFrom(timeA, out var valueA, out SqlEnums unitA))
            return TryDateAdd(typeof(TimeSpan), timeB, add ? valueA : Expression.Negate(valueA), unitA);

        if (IsTimeSpanFrom(timeB, out var valueB, out SqlEnums unitB))
            return TryDateAdd(typeof(TimeSpan), timeA, add ? valueB : Expression.Negate(valueB), unitB);

        return null;
    }

    public static bool IsTimeSpanFrom(Expression exp, out Expression value, out SqlEnums unit)
    {
        if(exp is MethodCallExpression mce && mce.Method.DeclaringType == typeof(TimeSpan))
        {
            switch (mce.Method.Name)
            {
                case nameof(TimeSpan.FromMilliseconds): unit = SqlEnums.millisecond; value = mce.GetArgument("value"); return true;
                case nameof(TimeSpan.FromSeconds): unit = SqlEnums.second; value = mce.GetArgument("value"); return true;
                case nameof(TimeSpan.FromMinutes): unit = SqlEnums.millisecond; value = mce.GetArgument("value"); return true;
                case nameof(TimeSpan.FromHours): unit = SqlEnums.hour; value = mce.GetArgument("value"); return true;
            }
        }

        value = default!;
        unit = default;
        return false;
    }

    private Expression? TryDatePartTo(SqlEnums unit, Expression start, Expression end)
    {
        Expression exprStart = Visit(start);
        Expression exprEnd = Visit(end);
        if (innerProjection || !Has(exprStart) || !Has(exprEnd))
            return null;

        var dateType = new[] { start.Type.UnNullify(), end.Type.UnNullify() }.Distinct().SingleEx(); 

        if (isPostgres)
        {
            var age = new SqlFunctionExpression(dateType, null, PostgresFunction.age.ToString(), new[] { exprStart, exprEnd });

            static SqlFunctionExpression Extract( SqlEnums part, Expression period)
            {
                return new SqlFunctionExpression(typeof(int), null, PostgresFunction.EXTRACT.ToString(), new[] { new SqlLiteralExpression(part), period });
            }

            if (unit == SqlEnums.month)
                return Add(Expression.Add(Extract(SqlEnums.year, age), Expression.Multiply(Extract(SqlEnums.month, age), new SqlConstantExpression(12, typeof(int)))));
            else if (unit == SqlEnums.year)
                return Add(Extract(SqlEnums.year, age));
            else
                throw new UnexpectedValueException(unit);
        }
        else
        {
            var datePart = new SqlLiteralExpression(unit);

            var diff = new SqlFunctionExpression(typeof(int), null, SqlFunction.DATEDIFF.ToString(),
                new[] { datePart, exprStart, exprEnd });

            var add = new SqlFunctionExpression(dateType, null, SqlFunction.DATEADD.ToString(),
                new[] { datePart, diff, exprStart });

            return Add(new CaseExpression(new[]{
            new When(Expression.GreaterThan(add, exprEnd), Expression.Subtract(diff, Expression.Constant(1)))},
                    diff));
        }
    }


    private Expression? TrySqlTrim(Expression expression)
    {
        Expression expr = Visit(expression);
        if (innerProjection || !Has(expr))
            return null;

        Expression result =
            TrySqlFunction(null, SqlFunction.LTRIM, expression.Type,
                  TrySqlFunction(null, SqlFunction.RTRIM, expression.Type, expression)!)!;

        return Add(result);
    }

    static PropertyInfo piDayNumber = ReflectionTools.GetPropertyInfo((DateOnly d) => d.DayNumber);

    protected override Expression VisitBinary(BinaryExpression b)
    {
        if (b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual)
        {
            var expression = SmartEqualizer.PolymorphicEqual(b.Left, b.Right);

            if (expression.NodeType == ExpressionType.Equal) //simple comparison
            {
                BinaryExpression newB = (BinaryExpression)expression;
                var left = Visit(newB.Left)!;
                var right = Visit(newB.Right)!;

                if(ExtractDayOfWeek(left, out var ldow) &&
                   ExtractDayOfWeek(right, out var rdow))
                {
                    left = ldow!;
                    right = rdow!;
                }

                newB = MakeBinaryFlexible(b.NodeType, left, right);
                if (Has(left) && Has(right))
                    candidates.Add(newB);

                if (Has(newB) && IsFullNominateOrAggresive)
                {
                    Expression result = ConvertToSqlComparison(newB);
                    return Add(result);
                }
                else
                {
                    return ConvertAvoidNominate(newB);
                }
            }
            else //tuples, entities, etc...
            {
                Expression result = Visit(expression);
                if (b.NodeType == ExpressionType.Equal)
                    return result;

                var not = SimpleNot(result) ?? Expression.Not(result);

                if (Has(result))
                    return Add(not);
                return not;
            }
        }
        else
        {
            if ((b.NodeType == ExpressionType.Add || b.NodeType == ExpressionType.Subtract) && b.Left.Type.UnNullify() == typeof(TimeSpan) && b.Right.Type.UnNullify() == typeof(TimeSpan))
            {
                return TryAddSubtractTimeSpan(b.Left, b.Right, b.NodeType == ExpressionType.Add) ?? b;
            }

            if (b.NodeType == ExpressionType.Subtract && b.Type == typeof(int) && 
                b.Left is MemberExpression leftSide && leftSide.Member is PropertyInfo piLeft && ReflectionTools.PropertyEquals(piLeft, piDayNumber) &&
                b.Right is MemberExpression rightSide && rightSide.Member is PropertyInfo piRight && ReflectionTools.PropertyEquals(piRight, piDayNumber))
            {
                var diff = TrySqlDifference(SqlEnums.day, b.Type, leftSide.Expression!, rightSide.Expression!);
                if (diff == null)
                    return b;


                return Add(new SqlCastExpression(typeof(int), diff));
            }

            b = SmartEqualizer.UnwrapPrimaryKeyBinary(b);

           
            Expression left = this.Visit(b.Left);
            Expression right = this.Visit(b.Right);
            Expression? conversion = this.Visit(b.Conversion);

            if (left != b.Left || right != b.Right || conversion != b.Conversion)
            {
                if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                    b = Expression.Coalesce(left, right, conversion as LambdaExpression);
                else if (left.Type == b.Left.Type && right.Type == b.Right.Type)
                    b = Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
                else
                    b = MakeBinaryFlexible(b.NodeType, left, right);
            }

            Expression result = b;
            if (candidates.Contains(left) && candidates.Contains(right) && IsFullNominateOrAggresive)
            {
                if ((b.NodeType == ExpressionType.Add || b.NodeType == ExpressionType.Subtract) &&  b.Left.Type.UnNullify() == typeof(DateTime) && b.Right.Type.UnNullify() == typeof(TimeSpan))
                {
                    result = TryAddSubtractDateTimeTimeSpan(b.Left, b.Right, b.NodeType == ExpressionType.Add) ?? result;
                }
               
                else if (b.NodeType == ExpressionType.Add)
                {
                    result = ConvertToSqlAddition(b);
                }
                else if (b.NodeType == ExpressionType.Coalesce)
                {
                    result = ConvertToSqlCoalesce(b);
                }

                return Add(result);
            }

            return result;
        }
    }

    private bool ExtractDayOfWeek(Expression exp, out Expression? result)
    {
        if (exp is ToDayOfWeekExpression tdow)
        {
            result = tdow.Expression;
            return true;
        }

        if (exp.NodeType == ExpressionType.Convert && exp.Type.UnNullify() == typeof(DayOfWeek))
            return ExtractDayOfWeek(((UnaryExpression)exp).Operand, out result);

        result = null;
        return false;
    }

    private static BinaryExpression MakeBinaryFlexible(ExpressionType nodeType, Expression left, Expression right)
    {
        if (left.Type == right.Type)
        {
            return Expression.MakeBinary(nodeType, left, right);
        }
        else if (left.Type.UnNullify() == right.Type.UnNullify())
        {
            return Expression.MakeBinary(nodeType, left.Nullify(), right.Nullify());
        }
        else if (left.IsNull() || right.IsNull())
        {
            var newLeft = left.IsNull() ? ConvertNull(left, right.Type.Nullify()) : left.Nullify();
            var newRight = right.IsNull() ? ConvertNull(right, left.Type.Nullify()) : right.Nullify();

            return Expression.MakeBinary(nodeType, newLeft, newRight);
        }

        throw new InvalidOperationException();
    }

    public static Expression ConvertNull(Expression nullNode, Type type)
    {
        switch (nullNode.NodeType)
        {
            case ExpressionType.Convert: return ConvertNull(((UnaryExpression)nullNode).Operand, type);
            case ExpressionType.Constant: return Expression.Constant(null, type);
            default:
                if (nullNode is SqlConstantExpression)
                    return new SqlConstantExpression(null, type);

                throw new InvalidOperationException("Unexpected NodeType to ConvertNull " + nullNode.NodeType);
        }
    }

    public static Expression? SimpleNot(Expression e)
    {
        if (e.NodeType == ExpressionType.Not)
            return ((UnaryExpression)e).Operand;

        if (e is IsNullExpression isNull)
            return new IsNotNullExpression(isNull.Expression);

        if (e is IsNotNullExpression isNotNull)
            return new IsNullExpression(isNotNull.Expression);

        return null;
    }

    private Expression ConvertToSqlCoalesce(BinaryExpression b)
    {
        Expression left = b.Left;
        Expression right = b.Right;

        List<Expression> expressions = new List<Expression>();
        if (left is SqlFunctionExpression fLeft && fLeft.SqlFunction == SqlFunction.COALESCE.ToString())
            expressions.AddRange(fLeft.Arguments);
        else
            expressions.Add(left);

        if (right is SqlFunctionExpression fRight && fRight.SqlFunction == SqlFunction.COALESCE.ToString())
            expressions.AddRange(fRight.Arguments);
        else
            expressions.Add(right);

        return Add(new SqlFunctionExpression(b.Type, null, SqlFunction.COALESCE.ToString(), expressions));
    }

    private static Expression ConvertToSqlAddition(BinaryExpression b)
    {
        Expression left = b.Left;
        Expression right = b.Right;

        if (left.Type == typeof(string) || right.Type == typeof(string))
        {
            var arguments = new List<Expression>();
            if (left is SqlFunctionExpression sleft && sleft.SqlFunction == SqlFunction.CONCAT.ToString())
                arguments.AddRange(sleft.Arguments);
            else
                arguments.Add(left);

            if (right is SqlFunctionExpression sright && sright.SqlFunction == SqlFunction.CONCAT.ToString())
                arguments.AddRange(sright.Arguments);
            else
                arguments.Add(right);

            return new SqlFunctionExpression(typeof(string), null, SqlFunction.CONCAT.ToString(), arguments);
        }
        return b;
    }

    private static bool AlwaysHasValue(Expression exp)
    {
        if (exp is SqlConstantExpression scons)
            return scons.Value != null;

        if (exp is ConstantExpression cons)
            return cons.Value != null;

        if (exp is BinaryExpression bin)
            return AlwaysHasValue(bin.Left) && AlwaysHasValue(bin.Right);

        if (exp is ConditionalExpression cond)
            return AlwaysHasValue(cond.IfTrue) && AlwaysHasValue(cond.IfFalse);

        return false;
    }

    static Expression ConvertToSqlComparison(BinaryExpression b)
    {
        if (b.NodeType == ExpressionType.Equal)
        {
            if (b.Left.IsNull())
            {
                if (b.Right.IsNull())
                    return Expression.Constant(true);
                else
                    return new IsNullExpression(b.Right);
            }
            else
            {
                if (b.Right.IsNull())
                    return new IsNullExpression(b.Left);

                return b;
            }
        }
        else if (b.NodeType == ExpressionType.NotEqual)
        {
            if (b.Left.IsNull())
            {
                if (b.Right.IsNull())
                    return Expression.Constant(false);
                else
                    return new IsNotNullExpression(b.Right);
            }
            else
            {
                if (b.Right.IsNull())
                    return new IsNotNullExpression(b.Left);

                return b;
            }
        }
        throw new InvalidOperationException();
    }

    private static Expression ConvertAvoidNominate(BinaryExpression b)
    {
        if (b.NodeType == ExpressionType.Equal)
        {
            if (b.Left.IsNull())
            {
                if (b.Right.IsNull())
                    return Expression.Constant(true);
                else
                    return Expression.Equal(Expression.Constant(null, b.Left.Type), b.Right);
            }
            else
            {
                if (b.Right.IsNull())
                    return Expression.Equal(b.Left, Expression.Constant(null, b.Right.Type));

                return b;
            }
        }
        else if (b.NodeType == ExpressionType.NotEqual)
        {
            if (b.Left.IsNull())
            {
                if (b.Right.IsNull())
                    return Expression.Constant(false);
                else
                    return Expression.NotEqual(Expression.Constant(null, b.Left.Type), b.Right);
            }
            else
            {
                if (b.Right.IsNull())
                    return Expression.NotEqual(b.Left, Expression.Constant(null, b.Right.Type));

                return b;
            }
        }
        throw new InvalidOperationException();
    }

    static readonly MethodInfo miSimpleConcat = ReflectionTools.GetMethodInfo(() => string.Concat("a", "b"));


    protected override Expression VisitConditional(ConditionalExpression c)
    {
        Expression result = c;
        Expression test = this.Visit(c.Test);
        Expression ifTrue = this.Visit(c.IfTrue);
        Expression ifFalse = this.Visit(c.IfFalse);
        if (test != c.Test || ifTrue != c.IfTrue || ifFalse != c.IfFalse)
        {
            result = Expression.Condition(test, ifTrue, ifFalse);
        }

        if (Has(test) && Has(ifTrue) && Has(ifFalse))
        {
            if (ifFalse is CaseExpression oldC)
            {
                candidates.Remove(ifFalse); // just to save some memory
                result = new CaseExpression(oldC.Whens.PreAnd(new When(test, ifTrue)), oldC.DefaultValue);
            }
            else
            {
                if (ifTrue.IsNull() && ifFalse.IsNull())
                    return ifTrue; //cond? null: null doesn't work in sql

                result = new CaseExpression(new[] { new When(test, ifTrue) },
                    ifFalse.IsNull() ? null : ifFalse);
            }

            return Add(result);
        }
        return result;
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        if (this.isFullNominate && u.NodeType == ExpressionType.Convert &&
            (u.Type.UnNullify() == typeof(PrimaryKey) || u.Operand.Type.UnNullify() == typeof(PrimaryKey)))
        {
            return base.Visit(u.Operand); //Could make sense to simulate a similar convert (nullify / unnullify)
        }

        if(this.isFullNominate && u.Operand is ToDayOfWeekExpression && (u.Type.UnNullify() == typeof(int) || u.Type.UnNullify() == typeof(DayOfWeek)))
        {
            return base.Visit(u.Operand); //Could make sense to simulate a similar convert (nullify / unnullify)
        }

        if (u.NodeType == ExpressionType.Convert && u.Type.IsNullable() && u.Type.UnNullify() == u.Operand.Type && u.Operand.NodeType == ExpressionType.Conditional)
        {
            ConditionalExpression ce = (ConditionalExpression)u.Operand;

            var newCe = Expression.Condition(ce.Test, Convert(ce.IfTrue, u.Type), Convert(ce.IfFalse, u.Type));

            return Visit(newCe);
        }

        if (u.NodeType == ExpressionType.Convert && u.Type.IsNullable() && u.Type.UnNullify().IsEnum)
        {
            var underlying = Enum.GetUnderlyingType(u.Type.UnNullify());

            if (u.Operand.NodeType == ExpressionType.Convert && u.Operand.Type.IsEnum)
                u = Expression.Convert(Expression.Convert(((UnaryExpression)u.Operand).Operand, underlying.Nullify()), u.Type); //Expand nullability
            else if (u.Operand.Type == underlying)
                u = Expression.Convert(Expression.Convert(u.Operand, underlying.Nullify()), u.Type); //Expand nullability
        }

        Expression operand = this.Visit(u.Operand);

        Expression result = operand == u.Operand ? u : Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);

        if (Has(operand))
        {
            if (u.NodeType == ExpressionType.Not)
                return Add(SimpleNot(operand) ?? result);

            if (u.NodeType == ExpressionType.Negate)
                return Add(result);

            if (u.NodeType == ExpressionType.Convert)
            {
                var untu = u.Type.UnNullify();
                var optu = operand.Type.UnNullify();

                //Only from smaller to bigger, SQL Cast to Decimal could remove decimal places!
                if ((optu == typeof(bool) || optu == typeof(int) || optu == typeof(long)) &&
                    (untu == typeof(double) || untu == typeof(float) || untu == typeof(decimal)))
                    return Add(new SqlCastExpression(u.Type, operand));

                if ((optu == typeof(float)) &&
                    (untu == typeof(double)))
                    return Add(new SqlCastExpression(u.Type, operand));

                if (ReflectionTools.IsIntegerNumber(optu) && ReflectionTools.IsIntegerNumber(untu))
                    return Add(new SqlCastExpression(u.Type, operand));

                if (isFullNominate || isGroupKey && optu == untu)
                    return Add(result);

                if ("Sql" + untu.Name == optu.Name)
                    return Add(result);
            }
        }

        return result;
    }

    private static Expression Convert(Expression expression, Type type)
    {
        if (expression.Type == type)
            return expression;

        if (expression.NodeType == ExpressionType.Convert && ((UnaryExpression)expression).Operand.Type == type)
            return ((UnaryExpression)expression).Operand;

        return Expression.Convert(expression, type);
    }

    protected internal override Expression VisitProjection(ProjectionExpression proj)
    {
        bool oldInnerProjection = this.innerProjection;
        innerProjection = true;
        var result = base.VisitProjection(proj);
        innerProjection = oldInnerProjection;
        return result;
    }

    protected internal override Expression VisitTable(TableExpression table)
    {
        if (!innerProjection)
            return Add(table);

        return table;
    }

    protected internal override Expression VisitSelect(SelectExpression select)
    {
        if (!innerProjection)
            return Add(select);

        return select;
    }

    protected internal override Expression VisitIn(InExpression inExp)
    {
        if (!innerProjection)
            return Add(inExp);

        return inExp;
    }

    protected internal override Expression VisitExists(ExistsExpression exists)
    {
        if (!innerProjection)
            return Add(exists);

        return exists;
    }

    protected internal override Expression VisitScalar(ScalarExpression scalar)
    {
        if (!innerProjection)
            return Add(scalar);

        return scalar;
    }

    protected internal override Expression VisitAggregate(AggregateExpression aggregate)
    {
        if (!innerProjection)
            return Add(aggregate);

        return aggregate;
    }

    protected internal override Expression VisitAggregateRequest(AggregateRequestsExpression aggregate)
    {
        if (!innerProjection)
            return Add(aggregate);

        return aggregate;
    }

    protected internal override Expression VisitIsNotNull(IsNotNullExpression isNotNull)
    {
        if (!innerProjection)
            return Add(isNotNull);

        return isNotNull;
    }

    protected internal override Expression VisitIsNull(IsNullExpression isNull)
    {
        if (!innerProjection)
            return Add(isNull);

        return isNull;
    }

    protected internal override Expression VisitSqlLiteral(SqlLiteralExpression sqlEnum)
    {
        if (!innerProjection)
            return Add(sqlEnum);

        return sqlEnum;
    }

    protected internal override Expression VisitLike(LikeExpression like)
    {
        Expression exp = Visit(like.Expression);
        Expression pattern = Visit(like.Pattern);
        if (exp != like.Expression || pattern != like.Pattern)
            like = new LikeExpression(exp, pattern);

        if (Has(exp) && Has(pattern))
            return Add(like);

        return like;
    }

    private Expression? TryLike(Expression expression, Expression pattern)
    {
        if (expression.IsNull())
            return Add(Expression.Constant(false));

        //pattern = ExpressionEvaluator.PartialEval(pattern);
        Expression newPattern = Visit(pattern);
        Expression newExpression = Visit(expression);

        LikeExpression result = new LikeExpression(newExpression, newPattern);

        if (Has(newPattern) && Has(newExpression))
        {
            return Add(result);
        }
        return null;
    }

    private Expression? TryCharIndex(Expression expression, Expression subExpression, Func<Expression, Expression> compare)
    {
        if (expression.IsNull())
            return Add(Expression.Constant(false));

        Expression newSubExpression = Visit(subExpression);
        Expression newExpression = Visit(expression);

        if (Has(newSubExpression) && Has(newExpression))
        {
            SqlFunctionExpression result = isPostgres ?
                new SqlFunctionExpression(typeof(int), null, PostgresFunction.strpos.ToString(), new[] { newExpression, newSubExpression }):
                new SqlFunctionExpression(typeof(int), null, SqlFunction.CHARINDEX.ToString(), new[] { newSubExpression, newExpression });

            Add(result);

            return Add(compare(result));
        }
        return null;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression!.Type.Namespace == "System.Data.SqlTypes" && (m.Member.Name == "Value" || m.Member.Name == "IsNull"))
        {
            Expression expression = this.Visit(m.Expression);
            Expression nullable;
            if (m.Member.Name == "Value")
                nullable = Expression.Convert(expression, m.Type.UnNullify());
            else
                nullable = new IsNullExpression(expression);

            if (Has(expression))
                return Add(nullable);

            return nullable;
        }


        Expression? hardResult = HardCodedMembers(m);
        if (hardResult != null)
            return hardResult;

        return base.VisitMember(m);
    }

    string GetDatePart() => isPostgres ? PostgresFunction.EXTRACT.ToString() : SqlFunction.DATEPART.ToString();

    public Expression? HardCodedMembers(MemberExpression m)
    {
        

        switch (m.Member.DeclaringType!.TypeName() + "." + m.Member.Name)
        {
            case "string.Length": return TrySqlFunction(null, isPostgres ? PostgresFunction.length.ToString() : SqlFunction.LEN.ToString(), m.Type, m.Expression!);
            case "Math.PI": return TrySqlFunction(null, SqlFunction.PI, m.Type);
            case "DateOnly.Year":
            case "DateTime.Year": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.year), m.Expression!);
            case "DateOnly.Month":
            case "DateTime.Month": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.month), m.Expression!);
            case "DateOnly.Day":
            case "DateTime.Day": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.day), m.Expression!);
            case "DateOnly.DayOfYear":
            case "DateTime.DayOfYear": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(isPostgres? SqlEnums.doy: SqlEnums.dayofyear), m.Expression!);
            case "DateOnly.DayOfWeek":
            case "DateTime.DayOfWeek": return TrySqlDayOftheWeek(m.Expression!);
            case "DateTime.Hour": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.hour), m.Expression!);
            case "DateTime.Minute": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.minute), m.Expression!);
            case "DateTime.Second": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.second), m.Expression!);
            case "DateTime.Millisecond": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.millisecond), m.Expression!);
            case "DateTime.Date": return TrySqlDate(m.Expression!);
            case "DateTime.TimeOfDay": return TrySqlTime(m.Expression!);

            case "TimeSpan.Days":
                {
                    var diff = TrySqlDifference(SqlEnums.day, m.Type, m.Expression!);
                    if (diff == null)
                        return null;

                    return Add(new SqlCastExpression(typeof(int?), TrySqlFunction(null, SqlFunction.FLOOR, typeof(double?), diff)!));
                }
            case "TimeSpan.Hours": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.hour), m.Expression!);
            case "TimeSpan.Minutes": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.minute), m.Expression!);
            case "TimeSpan.Seconds": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.second), m.Expression!);
            case "TimeSpan.Milliseconds": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.millisecond), m.Expression!);
                
            case "TimeSpan.TotalDays": return TrySqlDifference(SqlEnums.day, m.Type, m.Expression!);
            case "TimeSpan.TotalHours": return TrySqlDifference(SqlEnums.hour, m.Type, m.Expression!);
            case "TimeSpan.TotalMilliseconds": return TrySqlDifference(SqlEnums.millisecond, m.Type, m.Expression!);
            case "TimeSpan.TotalSeconds": return TrySqlDifference(SqlEnums.second, m.Type, m.Expression!);
            case "TimeSpan.TotalMinutes": return TrySqlDifference(SqlEnums.minute, m.Type, m.Expression!);
            case "PrimaryKey.Object":
                {
                    var exp = m.Expression!;
                    if (exp is UnaryExpression ue)
                        exp = ue.Operand;

                    var pk = (PrimaryKeyExpression)exp;

                    return Visit(pk.Value);
                }
            default: return null;
        }
    }

    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        Expression? result = HardCodedMethods(m);
        if (result != null)
            return result;

        SqlMethodAttribute? sma = m.Method.GetCustomAttribute<SqlMethodAttribute>();
        if (sma != null)
        {
            using (ForceFullNominate())
            {
                if (m.Method.IsExtensionMethod())
                    using (ForceFullNominate())
                        return TrySqlFunction(m.Arguments[0], m.Method.Name, m.Type, m.Arguments.Skip(1).ToArray())!;

                if (m.Object != null)
                    using (ForceFullNominate())
                        return TrySqlFunction(m.Object, sma.Name ?? m.Method.Name, m.Type, m.Arguments.ToArray())!;

                return TrySqlFunction(m.Object, ObjectName.Parse(sma.Name ?? m.Method.Name, isPostgres).ToString(), m.Type, m.Arguments.ToArray())!;
            }
        }

        return base.VisitMethodCall(m);
    }

    private Expression? GetDateTimeToStringSqlFunction(MethodCallExpression m, string? defaultFormat = null)
    {
        return Connector.Current.SupportsFormat ? GetFormatToString(m, defaultFormat) : TrySqlToString(m);
    }

    private Expression? TryDateAdd(Type returnType, Expression date, Expression value, SqlEnums unit)
    {
        if (this.isPostgres)
        {
            Expression d = Visit(date)!;
            if (!Has(d))
                return null;

            Expression v = Visit(value)!;
            if (!Has(v))
                return null;

            return Add(Expression.Add(date, Expression.Multiply(value, new SqlLiteralExpression(typeof(TimeSpan), $"INTERVAL '1 {unit}'"))));
        }


        return TrySqlFunction(null, SqlFunction.DATEADD, returnType, new SqlLiteralExpression(unit), value, date);
    }

    private Expression? HardCodedMethods(MethodCallExpression m)
    {
        if (m.Method.Name == "ToString" && m.Method.DeclaringType != typeof(EnumerableExtensions))
            return TrySqlToString(m);

        if (m.Method.Name == "Equals")
        {
            var obj = m.Object!;
            var arg = m.Arguments.SingleEx();

            if (obj.Type != arg.Type)
            {
                if (arg.Type == typeof(object))
                {
                    if (arg is ConstantExpression c)
                        arg = Expression.Constant(c.Value);
                    if (arg is UnaryExpression u && (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.Convert))
                        arg = u.Operand;
                }

            }

            return VisitBinary(Expression.Equal(obj, arg));
        }

        switch (m.Method.DeclaringType!.TypeName() + "." + m.Method.Name)
        {
            case "string.IndexOf":
                {
                    Expression? startIndex = m.TryGetArgument("startIndex")?.Let(e => Expression.Add(e, new SqlConstantExpression(1)));

                    Expression? charIndex = isPostgres ?
                        (startIndex != null ?
                        throw new NotImplementedException() :
                        TrySqlFunction(null, PostgresFunction.strpos, m.Type, m.Object!, m.GetArgument("value")))
                        :
                        (startIndex != null ?
                        TrySqlFunction(null, SqlFunction.CHARINDEX, m.Type, m.GetArgument("value"), m.Object!, startIndex) :
                        TrySqlFunction(null, SqlFunction.CHARINDEX, m.Type, m.GetArgument("value"), m.Object!));

                    if (charIndex == null)
                        return null;
                    Expression result = Expression.Subtract(charIndex, new SqlConstantExpression(1));
                    if (Has(charIndex))
                        return Add(result);
                    return result;

                }
            case "string.ToLower":
                return TrySqlFunction(null, SqlFunction.LOWER, m.Type, m.Object!);
            case "string.ToUpper":
                return TrySqlFunction(null, SqlFunction.UPPER, m.Type, m.Object!);
            case "string.TrimStart":
                return m.TryGetArgument("value") == null ? TrySqlFunction(null, SqlFunction.LTRIM, m.Type, m.Object!) : null;
            case "string.TrimEnd":
                return m.TryGetArgument("value") == null ? TrySqlFunction(null, SqlFunction.RTRIM, m.Type, m.Object!) : null;
            case "string.Trim":
                return m.Arguments.Any() ? null : TrySqlTrim(m.Object!);
            case "string.Replace":
                return TrySqlFunction(null, SqlFunction.REPLACE, m.Type, m.Object!, m.GetArgument("oldValue"), m.GetArgument("newValue"));
            case "string.Substring":
                var start = Expression.Add(m.GetArgument("startIndex"), new SqlConstantExpression(1));
                var length = m.TryGetArgument("length");
                if(isPostgres)
                    return length == null ?
                        TrySqlFunction(null, PostgresFunction.substr, m.Type, m.Object!, start) :
                        TrySqlFunction(null, PostgresFunction.substr, m.Type, m.Object!, start, length);
                else
                    return TrySqlFunction(null, SqlFunction.SUBSTRING, m.Type, m.Object!, start, length ?? new SqlConstantExpression(int.MaxValue));
            case "string.Contains":
                return TryCharIndex(m.Object!, m.GetArgument("value"), index => Expression.GreaterThanOrEqual(index, new SqlConstantExpression(1)));
            case "string.StartsWith":
                return TryCharIndex(m.Object!, m.GetArgument("value"), index => Expression.Equal(index, new SqlConstantExpression(1)));
            case "string.EndsWith":
                return TryCharIndex(
                    TrySqlFunction(null, SqlFunction.REVERSE, m.Type, m.Object!)!,
                    TrySqlFunction(null, SqlFunction.REVERSE, m.Type, m.GetArgument("value"))!,
                    index => Expression.Equal(index, new SqlConstantExpression(1)));
            case "string.Format":
            case "StringExtensions.FormatWith":
                return this.IsFullNominateOrAggresive ? TryStringFormat(m) : null;
            case "StringExtensions.Start":
                return TrySqlFunction(null, SqlFunction.LEFT, m.Type, m.GetArgument("str"), m.GetArgument("numChars"));
            case "StringExtensions.End":
                return TrySqlFunction(null, SqlFunction.RIGHT, m.Type, m.GetArgument("str"), m.GetArgument("numChars"));
            case "StringExtensions.Replicate":
                return TrySqlFunction(null, isPostgres ? PostgresFunction.repeat.ToString() : SqlFunction.REPLICATE.ToString(), m.Type, m.GetArgument("str"), m.GetArgument("times")); ;
            case "StringExtensions.Reverse":
                return TrySqlFunction(null, SqlFunction.REVERSE, m.Type, m.GetArgument("str"));
            case "StringExtensions.Like":
                return TryLike(m.GetArgument("str"), m.GetArgument("pattern"));
            case "StringExtensions.Etc":
                return TryEtc(m.GetArgument("str"), m.GetArgument("max"), m.TryGetArgument("etcString"));
            case "LinqHints.Collate":
                return TryCollate(m.GetArgument("str"), m.GetArgument("collation"));

            case "DateTime.Add":
            case "DateTime.Subtract":
                {
                    var val = m.GetArgument("value");
                    if (val.Type.UnNullify() != typeof(TimeSpan))
                        return null;

                    return TryAddSubtractDateTimeTimeSpan(m.Object!, val, m.Method.Name == "Add");
                }
            case "DateTime.AddYears": return TryDateAdd(m.Type, m.Object!, m.GetArgument("value"), SqlEnums.year);
            case "DateTime.AddMonths": return TryDateAdd(m.Type, m.Object!, m.GetArgument("months"), SqlEnums.month);
            case "DateTime.AddDays": return TryDateAdd(m.Type, m.Object!, m.GetArgument("value"), SqlEnums.day);
            case "DateOnly.AddYears": return TryDateAdd(m.Type, m.Object!, m.GetArgument("value"), SqlEnums.year);
            case "DateOnly.AddMonths": return TryDateAdd(m.Type, m.Object!, m.GetArgument("value"), SqlEnums.month);
            case "DateOnly.AddDays": return TryDateAdd(m.Type, m.Object!, m.GetArgument("value"), SqlEnums.day);
            case "DateTime.AddHours": return TryDateAdd(m.Type, m.Object!, m.GetArgument("value"), SqlEnums.hour); 
            case "DateTime.AddMinutes": return TryDateAdd(m.Type, m.Object!, m.GetArgument("value"), SqlEnums.minute); 
            case "DateTime.AddSeconds": return TryDateAdd(m.Type, m.Object!, m.GetArgument("value"), SqlEnums.second); 
            case "DateTime.AddMilliseconds": return TryDateAdd(m.Type, m.Object!, m.GetArgument("value"), SqlEnums.millisecond); 
            case "DateOnly.ToShortDateString": return GetDateTimeToStringSqlFunction(m, "d");
            case "DateTime.ToShortDateString": return GetDateTimeToStringSqlFunction(m, "d");
            case "DateTime.ToShortTimeString": return GetDateTimeToStringSqlFunction(m, "t");
            case "DateOnly.ToLongDateString": return GetDateTimeToStringSqlFunction(m, "D");
            case "DateTime.ToLongDateString": return GetDateTimeToStringSqlFunction(m, "D");
            case "DateTime.ToLongTimeString": return GetDateTimeToStringSqlFunction(m, "T");

            //dateadd(month, datediff(month, 0, SomeDate),0);
            case "DateTimeExtensions.YearStart": return TrySqlStartOf(m.TryGetArgument("dateTime") ?? m.GetArgument("date"), SqlEnums.year);
            case "DateTimeExtensions.MonthStart": return TrySqlStartOf(m.TryGetArgument("dateTime") ?? m.GetArgument("date"), SqlEnums.month);
            case "DateTimeExtensions.QuarterStart": return TrySqlStartOf(m.TryGetArgument("dateTime") ?? m.GetArgument("date"), SqlEnums.quarter);
            case "DateTimeExtensions.WeekStart": return TrySqlStartOf(m.TryGetArgument("dateTime") ?? m.GetArgument("date"), SqlEnums.week);
            case "DateTimeExtensions.HourStart": return TrySqlStartOf(m.GetArgument("dateTime"), SqlEnums.hour);
            case "DateTimeExtensions.MinuteStart": return TrySqlStartOf(m.GetArgument("dateTime"), SqlEnums.minute);
            case "DateTimeExtensions.SecondStart": return TrySqlStartOf(m.GetArgument("dateTime"), SqlEnums.second);
            case "DateTimeExtensions.YearsTo": return TryDatePartTo(SqlEnums.year, m.GetArgument("start"), m.GetArgument("end"));
            case "DateTimeExtensions.MonthsTo": return TryDatePartTo(SqlEnums.month, m.GetArgument("start"), m.GetArgument("end"));
            case "DateTimeExtensions.DaysTo": return TryDatePartTo(SqlEnums.day, m.GetArgument("start"), m.GetArgument("end"));

            case "DateTimeExtensions.Quarter": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.quarter), m.Arguments.Single());
            case "DateTimeExtensions.WeekNumber": return TrySqlFunction(null, GetDatePart(), m.Type, new SqlLiteralExpression(SqlEnums.week), m.Arguments.Single());

            case "DateTimeExtensions.ToDateOnly": return TrySqlCast(m.Type, m.GetArgument("dateTime"));
            case "DateTimeExtensions.ToDateTime": return TrySqlCast(m.Type, m.GetArgument("date"));
            case "DateOnly.FromDateTime":return  TrySqlCast(m.Type, m.GetArgument("dateTime"));

            case "DateOnly.ToDateTime": return TryAddSubtractDateTimeTimeSpan(m.Object!, m.GetArgument("time"), add: true);

            case "TimeSpan.FromHours": return TryTimeSpanFrom(SqlEnums.hour, m.GetArgument("value"));
            case "TimeSpan.FromMinute": return TryTimeSpanFrom(SqlEnums.minute, m.GetArgument("value"));
            case "TimeSpan.FromSeconds": return TryTimeSpanFrom(SqlEnums.second, m.GetArgument("value"));
            case "TimeSpan.FromMilliseconds": return TryTimeSpanFrom(SqlEnums.millisecond, m.GetArgument("value"));

            case "Math.Sign": return TrySqlFunction(null, SqlFunction.SIGN, m.Type, m.GetArgument("value"));
            case "Math.Abs": return TrySqlFunction(null, SqlFunction.ABS, m.Type, m.GetArgument("value"));
            case "Math.Sin": return TrySqlFunction(null, SqlFunction.SIN, m.Type, m.GetArgument("a"));
            case "Math.Asin": return TrySqlFunction(null, SqlFunction.ASIN, m.Type, m.GetArgument("d"));
            case "Math.Cos": return TrySqlFunction(null, SqlFunction.COS, m.Type, m.GetArgument("d"));
            case "Math.Acos": return TrySqlFunction(null, SqlFunction.ACOS, m.Type, m.GetArgument("d"));
            case "Math.Tan": return TrySqlFunction(null, SqlFunction.TAN, m.Type, m.GetArgument("a"));
            case "Math.Atan": return TrySqlFunction(null, SqlFunction.ATAN, m.Type, m.GetArgument("d"));
            case "Math.Atan2": return TrySqlFunction(null, SqlFunction.ATN2, m.Type, m.GetArgument("y"), m.GetArgument("x"));
            case "Math.Pow": return TrySqlFunction(null, SqlFunction.POWER, m.Type, m.GetArgument("x"), m.GetArgument("y"));
            case "Math.Sqrt": return TrySqlFunction(null, SqlFunction.SQRT, m.Type, m.GetArgument("d"));
            case "Math.Exp": return TrySqlFunction(null, SqlFunction.EXP, m.Type, m.GetArgument("d"));
            case "Math.Floor": return TrySqlFunction(null, SqlFunction.FLOOR, m.Type, m.GetArgument("d"));
            case "Math.Log10": return TrySqlFunction(null, SqlFunction.LOG10, m.Type, m.GetArgument("d"));
            case "Math.Log": return m.Arguments.Count != 1 ? null : TrySqlFunction(null, SqlFunction.LOG, m.Type, m.GetArgument("d"));
            case "Math.Ceiling": return TrySqlFunction(null, SqlFunction.CEILING, m.Type, m.TryGetArgument("d") ?? m.GetArgument("a"));
            case "Math.Round":

                var value = m.TryGetArgument("a") ?? m.TryGetArgument("d") ?? m.GetArgument("value");
                var digits = m.TryGetArgument("decimals") ?? m.TryGetArgument("digits");
                if (digits == null && isPostgres)
                    return TrySqlFunction(null, SqlFunction.ROUND, m.Type, value);
                else
                    return TrySqlFunction(null, SqlFunction.ROUND, m.Type, value, digits ?? new SqlConstantExpression(0));

            case "Math.Truncate":
                if(isPostgres)
                    return TrySqlFunction(null, PostgresFunction.trunc, m.Type, m.GetArgument("d"));

                return TrySqlFunction(null, SqlFunction.ROUND, m.Type, m.GetArgument("d"), new SqlConstantExpression(0), new SqlConstantExpression(1));
            case "Math.Max":
            case "Math.Min": return null; /* could be translates to something like 'case when a > b then a
                                           *                                             when a < b then b
                                           *                                             else null end
                                           * but looks too horrible */
            case "LinqHints.InSql":
                using (ForceFullNominate())
                {
                    return Visit(m.GetArgument("value"));
                }


            case "decimal.Parse": return Add(new SqlCastExpression(typeof(decimal), m.GetArgument("s")));
            case "double.Parse": return Add(new SqlCastExpression(typeof(double), m.GetArgument("s")));
            case "float.Parse": return Add(new SqlCastExpression(typeof(float), m.GetArgument("s")));
            case "byte.Parse": return Add(new SqlCastExpression(typeof(byte), m.GetArgument("s")));
            case "short.Parse": return Add(new SqlCastExpression(typeof(short), m.GetArgument("s")));
            case "int.Parse": return Add(new SqlCastExpression(typeof(int), m.GetArgument("s")));
            case "long.Parse": return Add(new SqlCastExpression(typeof(long), m.GetArgument("s")));
            default: return null;
        }
    }

    private Expression? TrySqlCast(Type type, Expression expression)
    {
        var e = Visit(expression);

        if (Has(e))
            return Add(new SqlCastExpression(type, e));

        return null;
    }

    private Expression? TryTimeSpanFrom(SqlEnums unit, Expression expression)
    {
        if (this.isPostgres)
        {
            Expression v = Visit(expression);
            if (!Has(v))
                return null;

            return Add(Expression.Add(new SqlConstantExpression(TimeSpan.Zero), Expression.Multiply(v, new SqlLiteralExpression(typeof(TimeSpan), $"INTERVAL '1 {unit}'"))));
        }

        return TrySqlFunction(null, SqlFunction.DATEADD, typeof(TimeSpan), new SqlLiteralExpression(unit), expression, new SqlConstantExpression(TimeSpan.Zero));
    }

    private Expression? TryStringFormat(MethodCallExpression m)
    {
        var prov = m.TryGetArgument("provider");
        if (prov != null)
            return null;

        var format = (ConstantExpression)(m.Object ?? m.GetArgument("format"));

        var args = m.TryGetArgument("args")?.Let(a => ((NewArrayExpression)a).Expressions) ??
            new[] { m.TryGetArgument("arg0"), m.TryGetArgument("arg1"), m.TryGetArgument("arg2"), m.TryGetArgument("arg3") }.NotNull().ToReadOnly();

        var strFormat = (string)format.Value!;

        var matches = Regex.Matches(strFormat, @"\{(?<index>\d+)(?<format>:[^}]*)?\}").Cast<Match>().ToList();

        if (matches.Count == 0)
            return Add(Expression.Constant(strFormat));

        var firsStr = strFormat.Substring(0, matches.FirstEx().Index);

        var arguments = new List<Expression>();
        if (firsStr.HasText())
            arguments.Add(new SqlConstantExpression(firsStr));

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (match.Groups["format"].Value.HasText())
                throw new InvalidOperationException("formatters not supported in: " + strFormat);

            var index = int.Parse(match.Groups["index"].Value);

            var exp = Visit(args[index]);
            if (!Has(exp))
                return null;

            arguments.Add(exp);

            var nextStr = i == matches.Count - 1 ?
                strFormat[match.EndIndex()..] :
                strFormat[match.EndIndex()..matches[i + 1].Index];

            if (nextStr.HasText())
                arguments.Add(new SqlConstantExpression(nextStr));
        }

        if (arguments.Count == 1)
            return Add(arguments.SingleEx());

        return Add(new SqlFunctionExpression(typeof(string), null, SqlFunction.CONCAT.ToString(), arguments));
    }

    private Expression? TryEtc(Expression str, Expression max, Expression? etcString)
    {
        var newStr = Visit(str);
        if (!Has(newStr))
            return null;

        if (this.IsFullNominateOrAggresive)
            return newStr;

        return etcString == null ?
            Expression.Call(miEtc2, newStr, max) :
            Expression.Call(miEtc3, newStr, max, etcString);
    }

    private Expression? TryCollate(Expression str, Expression collation)
    {
        var newStr = Visit(str);
        if (!Has(newStr))
            return null;

        var colStr = collation is ConstantExpression col ? (string)col.Value! : null;
        if (colStr == null)
            return null;

        return Add(new SqlFunctionExpression(typeof(string), newStr, SqlFunction.COLLATE.ToString(), new[] { newStr, new SqlConstantExpression(colStr) }));
    }

    static readonly MethodInfo miEtc2 = ReflectionTools.GetMethodInfo(() => "".Etc(2));
    static readonly MethodInfo miEtc3 = ReflectionTools.GetMethodInfo(() => "".Etc(2, "..."));

    IDisposable ForceFullNominate()
    {
        bool oldTemp = isFullNominate;
        isFullNominate = true;
        return new Disposable(() => isFullNominate = oldTemp);
    }
}
