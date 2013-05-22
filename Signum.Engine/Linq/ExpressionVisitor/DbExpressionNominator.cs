using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Collections.ObjectModel;
using Signum.Engine.Maps;
using System.Data;
using Signum.Entities.Reflection;
using Microsoft.SqlServer.Types;
using Microsoft.SqlServer.Server;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// Nominator is a class that walks an expression tree bottom up, determining the set of 
    /// candidate expressions that are possible columns of a select expression
    /// </summary>
    internal class DbExpressionNominator : DbExpressionVisitor
    {
        Alias[] existingAliases;

        // existingAliases is null when used in QueryBinder, not ColumnProjector
        // this allows to make function changes in where clausules but keeping the full expression (not compressing it in one column
        bool tempFullNominate = false;

        bool IsFullNominate { get { return tempFullNominate || existingAliases == null; } }

        bool IsFullNominateOrAggresive { get { return IsFullNominate || isGroupKey; } }

        bool isGroupKey = false;

        bool innerProjection = false; 

        HashSet<Expression> candidates = new HashSet<Expression>();

        T Add<T>(T expression) where T : Expression
        {
            this.candidates.Add(expression);
            return expression;
        }

        bool Has(Expression expression)
        {
            return this.candidates.Contains(expression);
        }

        private DbExpressionNominator() { }

        static internal HashSet<Expression> Nominate(Expression expression, Alias[] existingAliases, out Expression newExpression, bool isGroupKey = false)
        {
            DbExpressionNominator n = new DbExpressionNominator { existingAliases = existingAliases, isGroupKey = isGroupKey };
            newExpression = n.Visit(expression);
            return n.candidates;
        }
        
        static internal Expression FullNominate(Expression expression)
        {
            DbExpressionNominator n = new DbExpressionNominator { existingAliases = null };
            Expression result = n.Visit(expression);

            return result;
        }

        protected override Expression Visit(Expression exp)
        {
            Expression result = base.Visit(exp);
            if (IsFullNominate && result != null && !Has(result) && !IsExcluded(exp.NodeType))
                throw new InvalidOperationException("The expression can not be translated to SQL: " + result.NiceToString());

            return result;
        }

        private bool IsExcluded(ExpressionType expressionType)
        {
            switch ((DbExpressionType)expressionType)
            {
                case DbExpressionType.Table:
                case DbExpressionType.Select:
                case DbExpressionType.Projection:
                case DbExpressionType.Join:
                case DbExpressionType.AggregateSubquery: //Not sure :S 
                case DbExpressionType.Update:
                case DbExpressionType.Delete:
                case DbExpressionType.CommandAggregate:
                case DbExpressionType.SelectRowCount:
                    return true;
            }
            return false; 
        }

        //Dictionary<ColumnExpression, ScalarExpression> replacements; 

        protected override Expression VisitColumn(ColumnExpression column)
        {
            //ScalarExpression scalar;
            //if (replacements != null && replacements.TryGetValue(column, out scalar))
            //    return Visit(scalar);

            if ((IsFullNominateOrAggresive && !innerProjection) ||
                existingAliases.Contains(column.Alias))
                return Add(column);

            return column;
        }

        protected override NewExpression VisitNew(NewExpression nex)
        {
            if (existingAliases == null)
            {
                ReadOnlyCollection<Expression> args = this.VisitExpressionList(nex.Arguments);
                if (args != nex.Arguments)
                {
                    if (nex.Members != null)
                        // parece que para los tipos anonimos hace falt exact type matching
                        nex = Expression.New(nex.Constructor, args, nex.Members);
                    else
                        nex = Expression.New(nex.Constructor, args);
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
            if (!innerProjection && IsFullNominateOrAggresive && ( Schema.Current.Settings.IsDbType(c.Type.UnNullify()) || c.Type == typeof(object) && c.IsNull()))
            {
                return Add(c);
            }
            return c;
        }


        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
        {
            //We can not assume allways true because neasted projections
            Expression obj = Visit(sqlFunction.Object);
            ReadOnlyCollection<Expression> args = sqlFunction.Arguments.NewIfChange(a => Visit(a));
            if (args != sqlFunction.Arguments || obj != sqlFunction.Object)
                sqlFunction = new SqlFunctionExpression(sqlFunction.Type, obj, sqlFunction.SqlFunction, args); ;

            if (args.All(Has) && (obj == null || Has(obj)))
                return Add(sqlFunction);

            return sqlFunction;
        }

        protected override Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
        {
            ReadOnlyCollection<Expression> args = sqlFunction.Arguments.NewIfChange(a => Visit(a));
            if (args != sqlFunction.Arguments)
                sqlFunction = new SqlTableValuedFunctionExpression(sqlFunction.SqlFunction, sqlFunction.Table, sqlFunction.Alias, args); ;

            if (args.All(Has))
                return Add(sqlFunction);

            return sqlFunction;
        }

        protected override Expression VisitSqlCast(SqlCastExpression castExpr)
        {
            var expression = Visit(castExpr.Expression);
            if (expression != castExpr.Expression)
                castExpr = new SqlCastExpression(castExpr.Type, expression, castExpr.SqlDbType);
            return Add(castExpr);
        }

        protected override Expression VisitSqlConstant(SqlConstantExpression sqlConstant)
        {
            if (!innerProjection)
                return Add(sqlConstant);
            return sqlConstant;
        }

        protected override Expression VisitCase(CaseExpression cex)
        {
            var newWhens = cex.Whens.NewIfChange(w => VisitWhen(w));
            var newDefault = Visit(cex.DefaultValue);

            if (newWhens != cex.Whens || newDefault != cex.DefaultValue)
                cex = new CaseExpression(newWhens, newDefault);

            if (newWhens.All(w => Has(w.Condition) && Has(w.Value)) && (newDefault == null || Has(newDefault)))
                return Add(cex);

            return cex;
        }

        protected Expression TrySqlToString(Type type, Expression expression)
        {
            var newExp = Visit(expression);
            if (Has(newExp) && IsFullNominateOrAggresive)
            {
                var cast = new SqlCastExpression(type, newExp);
                return Add(cast);
            }
            return null;
        }

        protected Expression TrySqlFunction(Expression obj, SqlFunction sqlFunction, Type type, params Expression[] expression)
        {
            return TrySqlFunction(obj, sqlFunction.ToString(), type, expression); 
        }

        protected Expression TrySqlFunction(Expression obj, string sqlFunction, Type type, params Expression[] expression)
        {
            if (innerProjection)
                return null;

            Expression newObj = null;
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

            return Add(new SqlFunctionExpression(type, newObj, sqlFunction.ToString(), newExpressions));
        }

        private SqlFunctionExpression TrySqlDifference(SqlEnums sqlEnums, Type type, Expression expression)
        {
            if (innerProjection)
                return null;

            expression = expression.RemoveUnNullify();

            BinaryExpression be = expression as BinaryExpression;

            if (be == null || be.NodeType != ExpressionType.Subtract)
                return null;

            Expression left = Visit(be.Left);
            if (!Has(left.RemoveNullify()))
                return null;

            Expression right = Visit(be.Right);
            if (!Has(right.RemoveNullify()))
                return null;

            SqlFunctionExpression result = new SqlFunctionExpression(type, null, SqlFunction.DATEDIFF.ToString(), new Expression[]{
                new SqlEnumExpression(sqlEnums), right, left});

            return Add(result);
        }

        private Expression TrySqlDate(Expression expression)
        {
            Expression expr = Visit(expression);
            if (innerProjection || !Has(expr))
                return null;

            if (Schema.Current.Settings.DBMS >= DBMS.SqlServer2008)
                return Add(new SqlFunctionExpression(typeof(DateTime), null, SqlFunction.CONVERT.ToString(), new[]
                {
                    new SqlConstantExpression(SqlDbType.Date),
                    expr,
                    new SqlConstantExpression(101)
                }));

            return Add(new SqlCastExpression(typeof(DateTime),
                   new SqlFunctionExpression(typeof(double), null, SqlFunction.FLOOR.ToString(),
                       new[] { new SqlCastExpression(typeof(double), expr) }
                   )));
        }

        private Expression TrySqlTime(Expression expression)
        {
            Expression expr = Visit(expression);
            if (innerProjection || !Has(expr))
                return null;

            if (Schema.Current.Settings.DBMS >= DBMS.SqlServer2008)
                return Add(new SqlFunctionExpression(typeof(TimeSpan), null, SqlFunction.CONVERT.ToString(), new[]
                {
                    new SqlConstantExpression(SqlDbType.Time),
                    expr,
                }));

            throw new InvalidOperationException("{0} not supported on SQL Server 2005");
        }

        private Expression TrySqlDayOftheWeek(Expression expression)
        {
            Expression expr = Visit(expression);
            if (innerProjection || !Has(expr))
                return null;

            var number = Expression.Subtract(
                    TrySqlFunction(null, SqlFunction.DATEPART, typeof(int), new SqlEnumExpression(SqlEnums.weekday), expr),
                    new SqlConstantExpression(1)); 

            Add(number);

            Expression result = Expression.Convert(number, typeof(DayOfWeek));
            if (IsFullNominate)
                Add(result);

            return result;
        }

        private Expression TrySqlMonthStart(Expression expression)
        {
            Expression expr = Visit(expression);
            if (innerProjection || !Has(expr))
                return null;

            Expression result =
                TrySqlFunction(null, SqlFunction.DATEADD, expression.Type, new SqlEnumExpression(SqlEnums.month),
                      TrySqlFunction(null, SqlFunction.DATEDIFF, typeof(int), new SqlEnumExpression(SqlEnums.month), new SqlConstantExpression(0), expression),
                    new SqlConstantExpression(0));

            return Add(result);
        }


        private Expression TryAddSubstractDateTime(Expression date, Expression time, bool add)
        {
            Expression exprDate = Visit(date);
            Expression exprTime = Visit(time);
            if (innerProjection || !Has(exprDate) || !Has(exprTime))
                return null;

            var castDate = new SqlCastExpression(typeof(DateTime), exprDate, SqlDbType.DateTime); //Just in case is a Date
            var castTime = new SqlCastExpression(typeof(TimeSpan), exprTime, SqlDbType.DateTime); //Just in case is a Date

            var result = add ? Expression.Add(castDate, castTime) :
                Expression.Subtract(castDate, castTime);

            return Add(result); 
        }
        

        private Expression TrySqlTrim(Expression expression)
        {
            Expression expr = Visit(expression);
            if (innerProjection || !Has(expr))
                return null;

            Expression result =
                TrySqlFunction(null, SqlFunction.LTRIM, expression.Type,
                      TrySqlFunction(null, SqlFunction.RTRIM, expression.Type, expression));

            return Add(result);
        }

        private Expression DateAdd(SqlEnums part, Expression dateExpression, Expression intExpression)
        {
            return new SqlFunctionExpression(typeof(DateTime), null, SqlFunction.DATEADD.ToString(), new Expression[] { new SqlEnumExpression(part), dateExpression, intExpression });
        }

        private Expression MinusDatePart(SqlEnums part, Expression dateExpression)
        {
            return Expression.Negate(new SqlFunctionExpression(typeof(int), null, SqlFunction.DATEPART.ToString(), new Expression[] { new SqlEnumExpression(part), dateExpression }));
        }


        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual)
            {
                var expression = SmartEqualizer.PolymorphicEqual(b.Left, b.Right);

                if (expression.NodeType == ExpressionType.Equal) //simple comparison
                {
                    BinaryExpression newB = expression as BinaryExpression;
                    var left = Visit(newB.Left);
                    var right = Visit(newB.Right);

                    newB = Expression.MakeBinary(b.NodeType, left, right);
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
                Expression left = this.Visit(b.Left);
                Expression right = this.Visit(b.Right);
                Expression conversion = this.Visit(b.Conversion);
                if (left != b.Left || right != b.Right || conversion != b.Conversion)
                {
                    if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                        b = Expression.Coalesce(left, right, conversion as LambdaExpression);
                    else
                        b = Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
                }

                Expression result = b;
                if (candidates.Contains(left) && candidates.Contains(right) && IsFullNominateOrAggresive)
                {
                    if ((b.NodeType == ExpressionType.Add || b.NodeType == ExpressionType.Subtract) && b.Left.Type.UnNullify() == typeof(DateTime) && b.Right.Type.UnNullify() == typeof(TimeSpan))
                    {
                        result = TryAddSubstractDateTime(b.Left, b.Right, b.NodeType == ExpressionType.Add) ?? result;
                    }
                    else if (b.NodeType == ExpressionType.Add)
                    {
                        result = ConvertToSqlAddition(b);
                    }
                    else if (b.NodeType == ExpressionType.Coalesce)
                    {
                        result = ConvertToSqlCoallesce(b);
                    }

                    return Add(result);
                }

                return result;
            }
        }

        public Expression SimpleNot(Expression e)
        {
            if (e.NodeType == ExpressionType.Not)
                return ((UnaryExpression)e).Operand;

            if (e.NodeType == (ExpressionType)DbExpressionType.IsNull)
                return new IsNotNullExpression(((IsNullExpression)e).Expression);

            if (e.NodeType == (ExpressionType)DbExpressionType.IsNotNull)
                return new IsNullExpression(((IsNotNullExpression)e).Expression);

            return null;
        }

        private Expression ConvertToSqlCoallesce(BinaryExpression b)
        {
            Expression left = b.Left;
            Expression right = b.Right;

            List<Expression> expressions = new List<Expression>();
            SqlFunctionExpression fLeft = left as SqlFunctionExpression;
            if (fLeft != null && fLeft.SqlFunction == SqlFunction.COALESCE.ToString())
                expressions.AddRange(fLeft.Arguments);
            else
                expressions.Add(left);

            SqlFunctionExpression fRight = right as SqlFunctionExpression;
            if (fRight != null && fRight.SqlFunction == SqlFunction.COALESCE.ToString())
                expressions.AddRange(fRight.Arguments);
            else
                expressions.Add(right);

            return Add(new SqlFunctionExpression(b.Type, null, SqlFunction.COALESCE.ToString(), expressions));
        }

        private BinaryExpression ConvertToSqlAddition(BinaryExpression b)
        {
            Expression left = b.Left;
            Expression right = b.Right;

            if (left.Type == typeof(string) || right.Type == typeof(string))
            {
                b = Expression.Add(
                    left.Type == typeof(string) ? NullToStringEmpty(left) : new SqlCastExpression(typeof(string), left),
                    right.Type == typeof(string) ? NullToStringEmpty(right) : new SqlCastExpression(typeof(string), right),
                    miSimpleConcat);
            }
            return b;
        }

        private Expression NullToStringEmpty(Expression exp)
        {
            if (exp is ConstantExpression)
            {
                if (((ConstantExpression)exp).Value == null)
                    return Expression.Constant("", typeof(string));
                else
                    return exp;
            }

            if (exp is SqlConstantExpression)
            {
                if (((SqlConstantExpression)exp).Value == null)
                    return new SqlConstantExpression("", typeof(string));
                else
                    return exp;
            }

            if (AlwaysHasValue(exp))
            {
                return exp;
            }

            return new SqlFunctionExpression(typeof(string), null, SqlFunction.ISNULL.ToString(), new[] { exp, new SqlConstantExpression("") });
        }

        private static bool AlwaysHasValue(Expression exp)
        {
            if (exp is SqlConstantExpression)
                return ((SqlConstantExpression)exp).Value != null;

            if (exp is ConstantExpression)
                return ((ConstantExpression)exp).Value != null;

            if (exp is BinaryExpression)
                return AlwaysHasValue(((BinaryExpression)exp).Left) && AlwaysHasValue(((BinaryExpression)exp).Right);

            if (exp is ConditionalExpression)
                return AlwaysHasValue(((ConditionalExpression)exp).IfTrue) && AlwaysHasValue(((ConditionalExpression)exp).IfFalse);

            return false;
        }

        private Expression ConvertToSqlComparison(BinaryExpression b)
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

        private Expression ConvertAvoidNominate(BinaryExpression b)
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

        static MethodInfo miSimpleConcat = ReflectionTools.GetMethodInfo(() => string.Concat("a", "b"));


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
                if (ifFalse.NodeType == (ExpressionType)DbExpressionType.Case)
                {
                    var oldC = (CaseExpression)ifFalse;
                    candidates.Remove(ifFalse); // just to save some memory
                    result = new CaseExpression(oldC.Whens.PreAnd(new When(test, ifTrue)), oldC.DefaultValue);
                }
                else
                {
                    Expression @default = ifFalse.IsNull() ? null : ifFalse;

                    result = new CaseExpression(new[] { new When(test, ifTrue) }, @default);
                }

                return Add(result);
            }
            return result;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Convert && u.Type.IsNullable() && u.Type.UnNullify().IsEnum)
            {
                if (u.Operand.NodeType == ExpressionType.Convert && u.Operand.Type.IsEnum)
                    u = Expression.Convert(Expression.Convert(((UnaryExpression)u.Operand).Operand, typeof(int?)), u.Type); //Expand nullability
                else if (u.Operand.Type == typeof(int))
                    u = Expression.Convert(Expression.Convert(u.Operand, typeof(int?)), u.Type); //Expand nullability
            }

            Expression operand = this.Visit(u.Operand);

            Expression result = operand == u.Operand ? u : Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);

            if (Has(operand))
            {
                if (u.NodeType == ExpressionType.Not)
                    return Add(SimpleNot(operand) ?? result);
                
                if(u.NodeType == ExpressionType.Negate)
                    return Add(result);

                if (u.NodeType == ExpressionType.Convert)
                {
                    var untu = u.Type.UnNullify();
                    var optu = operand.Type.UnNullify();

                    if ((optu == typeof(bool) || optu == typeof(int) || optu == typeof(long)) &&
                        (untu == typeof(double) || untu == typeof(float) || untu == typeof(decimal)))
                        return Add(new SqlCastExpression(u.Type, operand));

                    if (optu == typeof(bool) &&
                       (untu == typeof(int) || untu == typeof(long)))
                        return Add(new SqlCastExpression(u.Type, operand));

                    if (IsFullNominate || isGroupKey && optu == untu)
                        return Add(result);

                    if ("Sql" + untu.Name == optu.Name)
                        return Add(result);
                }
            }

            return result;
        }



        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            bool oldInnerProjection = this.innerProjection;
            innerProjection = true;
            var result = base.VisitProjection(proj);
            innerProjection = oldInnerProjection;
            return result;
        }

        protected override Expression VisitIn(InExpression inExp)
        {
            Expression exp = this.Visit(inExp.Expression);
            SelectExpression select = (SelectExpression)this.Visit(inExp.Select);
            Expression result = inExp;
            if (exp != inExp.Expression)
                result = select == null ? InExpression.FromValues(exp, inExp.Values) :
                                         new InExpression(exp, select);

            if (!innerProjection && Has(exp))
                return Add(result);

            return result;
        }

        protected override Expression VisitExists(ExistsExpression exists)
        {
            SelectExpression select = (SelectExpression)this.Visit(exists.Select);
            if (select != exists.Select)
                exists = new ExistsExpression(select);

            if(!innerProjection)
                return Add(exists);

            return exists;
        }

        protected override Expression VisitScalar(ScalarExpression scalar)
        {
            SelectExpression select = (SelectExpression)this.Visit(scalar.Select);
            if (select != scalar.Select)
                scalar = new ScalarExpression(scalar.Type, select);

            if (!innerProjection)
                return Add(scalar);

            return scalar;
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            Expression source = Visit(aggregate.Source);
            if (source != aggregate.Source)
                aggregate = new AggregateExpression(aggregate.Type, source, aggregate.AggregateFunction);

            if (!innerProjection)
                return Add(aggregate);

            return aggregate;
        }

        protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
        {
            var subquery = (ScalarExpression)this.Visit(aggregate.Subquery);
            if (subquery != aggregate.Subquery)
                aggregate = new AggregateSubqueryExpression(aggregate.GroupByAlias, aggregate.Aggregate, subquery);

            if (!innerProjection)
                return Add(aggregate);

            return aggregate;
        }

        protected override Expression VisitIsNotNull(IsNotNullExpression isNotNull)
        {
            Expression exp= this.Visit(isNotNull.Expression);
            if (exp != isNotNull.Expression)
                isNotNull = new IsNotNullExpression(exp);

            if (Has(exp) && IsFullNominateOrAggresive)
                return Add(isNotNull);

            return isNotNull;
        }

        protected override Expression VisitIsNull(IsNullExpression isNull)
        {
            Expression exp = this.Visit(isNull.Expression);
            if (exp != isNull.Expression)
                isNull = new IsNullExpression(exp);

            if (Has(exp) && IsFullNominateOrAggresive)
                return Add(isNull);

            return isNull;
        }


        protected override Expression VisitSqlEnum(SqlEnumExpression sqlEnum)
        {
            if (!innerProjection)
                return Add(sqlEnum);
            return sqlEnum;
        }

        protected override Expression VisitLike(LikeExpression like)
        {
            Expression exp = Visit(like.Expression);
            Expression pattern = Visit(like.Pattern);
            if (exp != like.Expression || pattern != like.Pattern)
                like = new LikeExpression(exp, pattern);

            if (Has(exp) && Has(pattern))
                return Add(like);

            return like;
        }


        private Expression TryLike(Expression expression, Expression pattern)
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

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            if (m.Expression.Type.IsNullable() && (m.Member.Name == "Value" || m.Member.Name == "HasValue"))
            {
                Expression expression = this.Visit(m.Expression);
                Expression nullable;
                if (m.Member.Name == "Value")
                    nullable = Expression.Convert(expression, m.Expression.Type.UnNullify());
                else
                    nullable = new IsNotNullExpression(expression);

                if (Has(expression))
                    return Add(nullable);

                return nullable;
            }

            Expression hardResult = HardCodedMembers(m);
            if (hardResult != null)
                return hardResult; 

            return base.VisitMemberAccess(m);
        }

        public Expression HardCodedMembers(MemberExpression m)
        {
            switch (m.Member.DeclaringType.TypeName() + "." + m.Member.Name)
            {
                case "string.Length": return TrySqlFunction(null, SqlFunction.LEN, m.Type, m.Expression);
                case "Math.PI": return TrySqlFunction(null, SqlFunction.PI, m.Type);
                case "DateTime.Year": return TrySqlFunction(null, SqlFunction.YEAR, m.Type, m.Expression);
                case "DateTime.Month": return TrySqlFunction(null, SqlFunction.MONTH, m.Type, m.Expression);
                case "DateTime.Day": return TrySqlFunction(null, SqlFunction.DAY, m.Type, m.Expression);
                case "DateTime.DayOfYear": return TrySqlFunction(null, SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.dayofyear), m.Expression);
                case "DateTime.Hour": return TrySqlFunction(null, SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.hour), m.Expression);
                case "DateTime.Minute": return TrySqlFunction(null, SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.minute), m.Expression);
                case "DateTime.Second": return TrySqlFunction(null, SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.second), m.Expression);
                case "DateTime.Millisecond": return TrySqlFunction(null, SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.millisecond), m.Expression);
                case "DateTime.Date": return TrySqlDate(m.Expression);
                case "DateTime.TimeOfDay": return TrySqlTime(m.Expression);
                case "DateTime.DayOfWeek": return TrySqlDayOftheWeek(m.Expression);

                case "TimeSpan.Hours": return TrySqlFunction(null, SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.hour), m.Expression);
                case "TimeSpan.Minutes": return TrySqlFunction(null, SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.minute), m.Expression);
                case "TimeSpan.Seconds": return TrySqlFunction(null, SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.second), m.Expression);
                case "TimeSpan.Milliseconds": return TrySqlFunction(null, SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.millisecond), m.Expression);
               
                case "TimeSpan.TotalDays": return TrySqlDifference(SqlEnums.day, m.Type, m.Expression);
                case "TimeSpan.TotalHours": return TrySqlDifference(SqlEnums.hour, m.Type, m.Expression);
                case "TimeSpan.TotalMilliseconds": return TrySqlDifference(SqlEnums.millisecond, m.Type, m.Expression);
                case "TimeSpan.TotalSeconds": return TrySqlDifference(SqlEnums.second, m.Type, m.Expression);
                case "TimeSpan.TotalMinutes": return TrySqlDifference(SqlEnums.minute, m.Type, m.Expression);
                default: return null;
            }
        }

        static MethodInfo c = ReflectionTools.GetMethodInfo(()=>string.Concat("",""));

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            Expression result = HardCodedMethods(m);
            if (result != null)
                return result;

            SqlMethodAttribute sma = m.Method.SingleAttribute<SqlMethodAttribute>();
            if (sma != null)
                using (ForceFullNominate())
                    return TrySqlFunction(m.Object, sma.Name ?? m.Method.Name, m.Type, m.Arguments.ToArray());

            return base.VisitMethodCall(m);
        }

        private Expression HardCodedMethods(MethodCallExpression m)
        {
            if(m.Method.Name == "ToString")
                return TrySqlToString(typeof(string), m.Object); 

            switch (m.Method.DeclaringType.TypeName() + "." + m.Method.Name)
            {
                case "string.IndexOf":
                    {
                        Expression startIndex = m.TryGetArgument("startIndex").TryCC(e => Expression.Add(e, new SqlConstantExpression(1)));

                        Expression charIndex = TrySqlFunction(null, SqlFunction.CHARINDEX, m.Type, m.GetArgument("value"), m.Object, startIndex);
                        if (charIndex == null)
                            return null;
                        Expression result = Expression.Subtract(charIndex, new SqlConstantExpression(1));
                        if (Has(charIndex))
                            return Add(result);
                        return result;

                    }
                case "string.ToLower": return TrySqlFunction(null, SqlFunction.LOWER, m.Type, m.Object);
                case "string.ToUpper": return TrySqlFunction(null, SqlFunction.UPPER, m.Type, m.Object);
                case "string.TrimStart": return TrySqlFunction(null, SqlFunction.LTRIM, m.Type, m.Object);
                case "string.TrimEnd": return TrySqlFunction(null, SqlFunction.RTRIM, m.Type, m.Object);
                case "string.Trim": return TrySqlTrim(m.Object);
                case "string.Replace": return TrySqlFunction(null, SqlFunction.REPLACE, m.Type, m.Object, m.GetArgument("oldValue"), m.GetArgument("newValue"));
                case "string.Substring": return TrySqlFunction(null, SqlFunction.SUBSTRING, m.Type, m.Object, Expression.Add(m.GetArgument("startIndex"), new SqlConstantExpression(1)), m.TryGetArgument("length") ?? new SqlConstantExpression(int.MaxValue));
                case "string.Contains": return TryLike(m.Object, Expression.Add(Expression.Add(new SqlConstantExpression("%"), m.GetArgument("value"), c), new SqlConstantExpression("%"), c));
                case "string.StartsWith": return TryLike(m.Object, Expression.Add(m.GetArgument("value"), new SqlConstantExpression("%"), c));
                case "string.EndsWith": return TryLike(m.Object, Expression.Add(new SqlConstantExpression("%"), m.GetArgument("value"), c));

                case "StringExtensions.Start": return TrySqlFunction(null, SqlFunction.LEFT, m.Type, m.GetArgument("str"), m.GetArgument("numChars"));
                case "StringExtensions.End": return TrySqlFunction(null, SqlFunction.RIGHT, m.Type, m.GetArgument("str"), m.GetArgument("numChars"));
                case "StringExtensions.Replicate": return TrySqlFunction(null, SqlFunction.REPLICATE, m.Type, m.GetArgument("str"), m.GetArgument("times"));
                case "StringExtensions.Reverse": return TrySqlFunction(null, SqlFunction.REVERSE, m.Type, m.GetArgument("str"));
                case "StringExtensions.Like": return TryLike(m.GetArgument("str"), m.GetArgument("pattern"));

                case "DateTime.Add":
                case "DateTime.Substract":
                    {
                        var val = m.GetArgument("value");
                        if (val.Type.UnNullify() != typeof(TimeSpan))
                            return null;

                        return TryAddSubstractDateTime(m.Object, val, m.Method.Name == "Add");
                    }
                case "DateTime.AddDays": return TrySqlFunction(null, SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.day), m.GetArgument("value"), m.Object);
                case "DateTime.AddHours": return TrySqlFunction(null, SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.hour), m.GetArgument("value"), m.Object);
                case "DateTime.AddMilliseconds": return TrySqlFunction(null, SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.millisecond), m.GetArgument("value"), m.Object);
                case "DateTime.AddMinutes": return TrySqlFunction(null, SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.minute), m.GetArgument("value"), m.Object);
                case "DateTime.AddMonths": return TrySqlFunction(null, SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.month), m.GetArgument("months"), m.Object);
                case "DateTime.AddSeconds": return TrySqlFunction(null, SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.second), m.GetArgument("value"), m.Object);
                case "DateTime.AddYears": return TrySqlFunction(null, SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.year), m.GetArgument("value"), m.Object);
                
                    //dateadd(month, datediff(month, 0, SomeDate),0);
                case "DateTimeExtensions.MonthStart": return TrySqlMonthStart(m.GetArgument("dateTime"));
                case "DateTimeExtensions.YearsTo": return TrySqlFunction(null, SqlFunction.DATEDIFF, m.Type, new SqlEnumExpression(SqlEnums.year), m.GetArgument("min"), m.GetArgument("max"));
                case "DateTimeExtensions.MonthsTo": return TrySqlFunction(null, SqlFunction.DATEDIFF, m.Type, new SqlEnumExpression(SqlEnums.month), m.GetArgument("min"), m.GetArgument("max"));

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
                case "Math.Round": return TrySqlFunction(null, SqlFunction.ROUND, m.Type,
                    m.TryGetArgument("a") ?? m.TryGetArgument("d") ?? m.GetArgument("value"),
                    m.TryGetArgument("decimals") ?? m.TryGetArgument("digits") ?? new SqlConstantExpression(0));
                case "Math.Truncate": return TrySqlFunction(null, SqlFunction.ROUND, m.Type, m.GetArgument("d"), new SqlConstantExpression(0), new SqlConstantExpression(1));
                case "LinqHints.InSql":
                    using (ForceFullNominate())
                    {
                        return Visit(m.GetArgument("value"));
                    }
                case "StringExtensions.Etc": return TryEtc(m.GetArgument("str"), m.GetArgument("max"), m.TryGetArgument("etcString"));
                default: return null; 
            }
        }

        private Expression TryEtc(Expression str, Expression max, Expression etcString)
        {
            var newStr = Visit(str);
            if (!Has(newStr))
                return null;

            if (this.IsFullNominateOrAggresive)
                return newStr;

            var subString = Add(new SqlFunctionExpression(typeof(string), null, SqlFunction.SUBSTRING.ToString(), new Expression[]
            {
                newStr,
                new SqlConstantExpression(1, typeof(int)),
                Expression.Add(max, new SqlConstantExpression(1, typeof(int))),
            }));

            return etcString == null ? Expression.Call(miEtc2, subString, max) : Expression.Call(miEtc3, subString, max, etcString); 
        }

        static MethodInfo miEtc2 = ReflectionTools.GetMethodInfo(() => "".Etc(2));
        static MethodInfo miEtc3 = ReflectionTools.GetMethodInfo(() => "".Etc(2, "..."));

        IDisposable ForceFullNominate()
        {
            bool oldTemp = tempFullNominate;
            tempFullNominate = true;
            return new Disposable(() => tempFullNominate = oldTemp); 
        }
    }      
}
