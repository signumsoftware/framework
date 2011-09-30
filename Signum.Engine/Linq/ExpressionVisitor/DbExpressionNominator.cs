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
using Signum.Engine.Properties;
using System.Collections.ObjectModel;
using Signum.Engine.Maps;
using System.Data;
using Signum.Entities.Reflection;

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

        bool IsFullNominateOrAggresive { get { return IsFullNominate || isAggresive; } }

        bool isAggresive = false;

        bool innerProjection = false; 

        HashSet<Expression> candidates = new HashSet<Expression>();

        private DbExpressionNominator() { }

        static internal HashSet<Expression> Nominate(Expression expression, Alias[] existingAliases, out Expression newExpression)
        {
            DbExpressionNominator n = new DbExpressionNominator { existingAliases = existingAliases };
            newExpression = n.Visit(expression);
            return n.candidates;
        }

        static internal HashSet<Expression> NominateGroupBy(Expression expression, Alias[] existingAliases, out Expression newExpression)
        {
            DbExpressionNominator n = new DbExpressionNominator { existingAliases = existingAliases, isAggresive = true };
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
            if(IsFullNominate && result != null && !candidates.Contains(result) && !IsExcluded(exp.NodeType))
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

        Dictionary<ColumnExpression, ScalarExpression> replacements; 

        protected override Expression VisitColumn(ColumnExpression column)
        {
            ScalarExpression scalar;
            if (replacements != null && replacements.TryGetValue(column, out scalar))
                return Visit(scalar);

            if ((IsFullNominateOrAggresive && !innerProjection) ||
                existingAliases.Contains(column.Alias))
                candidates.Add(column);

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

                if (args.All(a => candidates.Contains(a)))
                    candidates.Add(nex);

                return nex;
            }
            else
                return base.VisitNew(nex);
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (!innerProjection && IsFullNominateOrAggresive && (IsSimpleType(c.Type) || c.Type == typeof(object) && c.IsNull()))
            {
                candidates.Add(c);
            }
            return c;
        }

        private bool IsSimpleType(Type type)
        {
            type = type.UnNullify();

            return type.IsEnum || Schema.Current.Settings.TypeValues.ContainsKey(type);
        }

        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
        {
            //We can not assume allways true because neasted projections
            ReadOnlyCollection<Expression> args = sqlFunction.Arguments.NewIfChange(a => Visit(a));
            if (args != sqlFunction.Arguments)
                sqlFunction = new SqlFunctionExpression(sqlFunction.Type, sqlFunction.SqlFunction, args); ;

            if (args.All(a => candidates.Contains(a)))
                candidates.Add(sqlFunction);

            return sqlFunction;
        }

        protected override Expression VisitSqlConstant(SqlConstantExpression sqlConstant)
        {
            if (!innerProjection)
                candidates.Add(sqlConstant);
            return sqlConstant;
        }

        protected override Expression VisitCase(CaseExpression cex)
        {
            var newWhens = cex.Whens.NewIfChange(w => VisitWhen(w));
            var newDefault = Visit(cex.DefaultValue);

            if (newWhens != cex.Whens || newDefault != cex.DefaultValue)
                cex = new CaseExpression(newWhens, newDefault);

            if (newWhens.All(w => candidates.Contains(w.Condition) && candidates.Contains(w.Value)) && (newDefault == null || candidates.Contains(newDefault)))
                candidates.Add(cex);

            return cex;
        }

        protected Expression TrySqlToString(Type type, Expression expression)
        {
            var newExp = Visit(expression);
            if (candidates.Contains(newExp) && IsFullNominateOrAggresive)
            {
                var cast = new SqlCastExpression(type, newExp);
                candidates.Add(cast);
                return cast;
            }
            return null;
        }

        protected Expression TrySqlFunction(SqlFunction sqlFunction, Type type, params Expression[] expression)
        {
            return TrySqlFunction(sqlFunction.ToString(), type, expression); 
        }

        protected Expression TrySqlFunction(string sqlFunction, Type type, params Expression[] expression)
        {
            if (innerProjection)
                return null;

            expression = expression.NotNull().ToArray();
            Expression[] newExpressions = new Expression[expression.Length];

            for (int i = 0; i < expression.Length; i++)
            {
                newExpressions[i] = Visit(expression[i]);
                if (!candidates.Contains(newExpressions[i]))
                    return null;
            }

            var result = new SqlFunctionExpression(type, sqlFunction.ToString(), newExpressions);
            candidates.Add(result);
            return result;
        }

        private SqlFunctionExpression TrySqlDifference(SqlEnums sqlEnums, Type type, Expression expression)
        {
            if (innerProjection)
                return null;

            BinaryExpression be = expression as BinaryExpression;

            if (be == null || be.NodeType != ExpressionType.Subtract)
                return null;

            Expression left = Visit(be.Left);
            if (!candidates.Contains(left))
                return null;

            Expression right = Visit(be.Right);
            if (!candidates.Contains(right))
                return null;

            SqlFunctionExpression result = new SqlFunctionExpression(type, SqlFunction.DATEDIFF.ToString(), new Expression[]{
                new SqlEnumExpression(sqlEnums), right, left});

            candidates.Add(result);

            return result;
        }

        private Expression TrySqlDate(Expression expression)
        {
            Expression expr = Visit(expression);
            if (innerProjection || !candidates.Contains(expr))
                return null;

            Expression result =
                  new SqlCastExpression(typeof(DateTime),
                    new SqlFunctionExpression(typeof(double), SqlFunction.FLOOR.ToString(),
                        new[] { new SqlCastExpression(typeof(double), expr) }
                    ));

            candidates.Add(result);
            return result; 
        }

        private Expression TrySqlDayOftheWeek(Expression expression)
        {
            if (!IsFullNominateOrAggresive)
                return null;

            Expression expr = Visit(expression);
            if (innerProjection || !candidates.Contains(expr))
                return null;

            Expression result = Expression.Convert(Expression.Subtract(
                    TrySqlFunction(SqlFunction.DATEPART, typeof(int), new SqlEnumExpression(SqlEnums.weekday), expr),
                    Expression.Constant(1)), typeof(DayOfWeek));

            candidates.Add(result);

            return result;
        }

        private Expression TrySqlMonthStart(Expression expression)
        {
            Expression expr = Visit(expression);
            if (innerProjection || !candidates.Contains(expr))
                return null;

            Expression result =
                TrySqlFunction(SqlFunction.DATEADD, expression.Type, new SqlEnumExpression(SqlEnums.month),
                      TrySqlFunction(SqlFunction.DATEDIFF, typeof(int), new SqlEnumExpression(SqlEnums.month), new SqlConstantExpression(0), expression),
                    new SqlConstantExpression(0));

            candidates.Add(result);
            return result;
        }

        private Expression TrySqlTrim(Expression expression)
        {
            Expression expr = Visit(expression);
            if (innerProjection || !candidates.Contains(expr))
                return null;

            Expression result =
                TrySqlFunction(SqlFunction.LTRIM, expression.Type,
                      TrySqlFunction(SqlFunction.RTRIM, expression.Type, expression));

            candidates.Add(result);
            return result;
        }

        Expression Convert(Expression exp, Type type)
        {
            return new SqlFunctionExpression(type, SqlFunction.CONVERT.ToString(), new[]{exp, 
                new SqlConstantExpression(Schema.Current.Settings.DefaultSqlType(type).ToString(), typeof(string))});
        }

        private Expression DateAdd(SqlEnums part, Expression dateExpression, Expression intExpression)
        {
            return new SqlFunctionExpression(typeof(DateTime), SqlFunction.DATEADD.ToString(), new Expression[] { new SqlEnumExpression(part), dateExpression, intExpression });
        }

        private Expression MinusDatePart(SqlEnums part, Expression dateExpression)
        {
            return Expression.Negate(new SqlFunctionExpression(typeof(int), SqlFunction.DATEPART.ToString(), new Expression[] { new SqlEnumExpression(part), dateExpression }));
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

                    newB = Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
                    if (candidates.Contains(left) && candidates.Contains(right))
                        candidates.Add(newB);

                    if (candidates.Contains(newB) && IsFullNominateOrAggresive)
                    {
                        Expression result = ConvertToSqlComparison(newB);
                        candidates.Add(result);
                        return result;
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

                    bool nominated = candidates.Contains(result);
                    result = Expression.Not(result);
                    if (nominated)
                        candidates.Add(result);
                    return result;
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
                    if (b.NodeType == ExpressionType.Add)
                    {
                        result = ConvertToSqlAddition(b);
                    }
                    else if (b.NodeType == ExpressionType.Coalesce)
                    {
                        result = ConvertToSqlCoallesce(b);
                    }

                    candidates.Add(result);
                }

                return result;
            }
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

            var result = new SqlFunctionExpression(b.Type, SqlFunction.COALESCE.ToString(), expressions);
            candidates.Add(result);
            return result;
        }

        private BinaryExpression ConvertToSqlAddition(BinaryExpression b)
        {
            Expression left = b.Left;
            Expression right = b.Right;

            if ((left.Type == typeof(string)) != (right.Type == typeof(string)))
            {
                b = Expression.Add(
                    left.Type == typeof(string) ? left : new SqlCastExpression(typeof(string), left),
                    right.Type == typeof(string) ? right : new SqlCastExpression(typeof(string), right), miSimpleConcat);
            }
            return b;
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

            if (candidates.Contains(test) && candidates.Contains(ifTrue) && candidates.Contains(ifFalse))
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

                candidates.Add(result);
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

            UnaryExpression result = operand == u.Operand ? u : Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);

            if (candidates.Contains(operand) &&
                (u.NodeType == ExpressionType.Not ||
                 u.NodeType == ExpressionType.Negate ||
                 u.NodeType == ExpressionType.Convert && (IsFullNominate || isAggresive && u.Operand.Type.UnNullify() == u.Type.UnNullify()))) //Expand nullability
                candidates.Add(result);

            return result;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            if (proj.IsOneCell)
            {
                var column = proj.Source.Columns.SingleEx();

                var select = (SelectExpression)base.Visit(proj.Source);
                var scalar = new ScalarExpression(column.Expression.Type, select);

                var reference = column.GetReference(proj.Source.Alias);
                
                if (replacements == null)
                    replacements = new Dictionary<ColumnExpression, ScalarExpression>(); 

                replacements.Add(reference, scalar);
                var result = Visit(proj.Projector);
                replacements.Remove(reference);

                return result;
            }
            else
            {
                bool oldInnerProjection = this.innerProjection;
                innerProjection = true;
                var result = base.VisitProjection(proj);
                innerProjection = oldInnerProjection;
                return result;
            }
        }

        protected override Expression VisitIn(InExpression inExp)
        {
            Expression exp = this.Visit(inExp.Expression);
            SelectExpression select = (SelectExpression)this.Visit(inExp.Select);
            Expression result = inExp;
            if (exp != inExp.Expression)
                result = select == null ? InExpression.FromValues(exp, inExp.Values) :
                                         new InExpression(exp, select);

            if (!innerProjection && candidates.Contains(exp))
                candidates.Add(result);

            return result;
        }

        protected override Expression VisitExists(ExistsExpression exists)
        {
            SelectExpression select = (SelectExpression)this.Visit(exists.Select);
            if (select != exists.Select)
                exists = new ExistsExpression(select);

            if(!innerProjection)
                candidates.Add(exists);

            return exists;
        }

        protected override Expression VisitScalar(ScalarExpression scalar)
        {
            SelectExpression select = (SelectExpression)this.Visit(scalar.Select);
            if (select != scalar.Select)
                scalar = new ScalarExpression(scalar.Type, select);

            if (!innerProjection)
                candidates.Add(scalar);

            return scalar;
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            Expression source = Visit(aggregate.Source);
            if (source != aggregate.Source)
                aggregate = new AggregateExpression(aggregate.Type, source, aggregate.AggregateFunction);

            if (!innerProjection)
                candidates.Add(aggregate);

            return aggregate;
        }

        protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
        {
            var subquery = (ScalarExpression)this.Visit(aggregate.AggregateAsSubquery);
            if (subquery != aggregate.AggregateAsSubquery)
                aggregate = new AggregateSubqueryExpression(aggregate.GroupByAlias, aggregate.AggregateInGroupSelect, subquery);

            if (!innerProjection) 
                candidates.Add(aggregate);

            return aggregate;
        }

        protected override Expression VisitIsNotNull(IsNotNullExpression isNotNull)
        {
            Expression exp= this.Visit(isNotNull.Expression);
            if (exp != isNotNull.Expression)
                isNotNull = new IsNotNullExpression(exp);

            if (candidates.Contains(exp))
                candidates.Add(isNotNull);

            return isNotNull;
        }

        protected override Expression VisitIsNull(IsNullExpression isNull)
        {
            Expression exp = this.Visit(isNull.Expression);
            if (exp != isNull.Expression)
                isNull = new IsNullExpression(exp);

            if (candidates.Contains(exp))
                candidates.Add(isNull);

            return isNull;
        }


        protected override Expression VisitSqlEnum(SqlEnumExpression sqlEnum)
        {
            if (!innerProjection)
                candidates.Add(sqlEnum);
            return sqlEnum;
        }

        protected override Expression VisitLike(LikeExpression like)
        {
            Expression exp = Visit(like.Expression);
            Expression pattern = Visit(like.Pattern);
            if (exp != like.Expression || pattern != like.Pattern)
                like = new LikeExpression(exp, pattern);

            if (candidates.Contains(exp) && candidates.Contains(pattern))
                candidates.Add(like);

            return like;
        }
        

         private LikeExpression TryLike(Expression expression, Expression pattern)
        {
             //pattern = ExpressionEvaluator.PartialEval(pattern);
             Expression newPattern = Visit(pattern); 
             Expression newExpression = Visit(expression);

             LikeExpression result = new LikeExpression(newExpression,newPattern); 

             if(candidates.Contains(newPattern) && candidates.Contains(newExpression))
             {
                 candidates.Add(result); 
                 return result;
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

                if (candidates.Contains(expression))
                    candidates.Add(nullable);

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
                case "string.Length": return TrySqlFunction(SqlFunction.LEN, m.Type, m.Expression);
                case "Math.PI": return TrySqlFunction(SqlFunction.PI, m.Type);
                case "DateTime.Year": return TrySqlFunction(SqlFunction.YEAR, m.Type, m.Expression);
                case "DateTime.Month": return TrySqlFunction(SqlFunction.MONTH, m.Type, m.Expression);
                case "DateTime.Day": return TrySqlFunction(SqlFunction.DAY, m.Type, m.Expression);
                case "DateTime.DayOfYear": return TrySqlFunction(SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.dayofyear), m.Expression);
                case "DateTime.Hour": return TrySqlFunction(SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.hour), m.Expression);
                case "DateTime.Minute": return TrySqlFunction(SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.minute), m.Expression);
                case "DateTime.Second": return TrySqlFunction(SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.second), m.Expression);
                case "DateTime.Millisecond": return TrySqlFunction(SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.millisecond), m.Expression);
                case "DateTime.Date": return TrySqlDate(m.Expression);
                case "DateTime.DayOfWeek": return TrySqlDayOftheWeek(m.Expression);
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
            SqlMethodAttribute sma = m.Method.SingleAttribute<SqlMethodAttribute>();
            if (sma != null)
                using (ForceFullNominate())
                    return TrySqlFunction(sma.Name ?? m.Method.Name, m.Type, m.Arguments.ToArray());

            Expression result = HardCodedMethods(m);
            if (result != null)
                return result;

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

                        Expression charIndex = TrySqlFunction(SqlFunction.CHARINDEX, m.Type, m.GetArgument("value"), m.Object, startIndex);
                        if (charIndex == null)
                            return null;
                        Expression result = Expression.Subtract(charIndex, Expression.Constant(1));
                        if (candidates.Contains(charIndex))
                            candidates.Add(result);
                        return result;
                    }
                case "string.ToLower": return TrySqlFunction(SqlFunction.LOWER, m.Type, m.Object);
                case "string.ToUpper": return TrySqlFunction(SqlFunction.UPPER, m.Type, m.Object);
                case "string.TrimStart": return TrySqlFunction(SqlFunction.LTRIM, m.Type, m.Object);
                case "string.TrimEnd": return TrySqlFunction(SqlFunction.RTRIM, m.Type, m.Object);
                case "string.Trim": return TrySqlTrim(m.Object);
                case "string.Replace": return TrySqlFunction(SqlFunction.REPLACE, m.Type, m.Object, m.GetArgument("oldValue"), m.GetArgument("newValue"));
                case "string.Substring": return TrySqlFunction(SqlFunction.SUBSTRING, m.Type, m.Object, Expression.Add(m.GetArgument("startIndex"), new SqlConstantExpression(1)), m.TryGetArgument("length") ?? new SqlConstantExpression(int.MaxValue));
                case "string.Contains": return TryLike(m.Object, Expression.Add(Expression.Add(Expression.Constant("%"), m.GetArgument("value"), c), Expression.Constant("%"), c));
                case "string.StartsWith": return TryLike(m.Object, Expression.Add(m.GetArgument("value"), Expression.Constant("%"), c));
                case "string.EndsWith": return TryLike(m.Object, Expression.Add(Expression.Constant("%"), m.GetArgument("value"), c));

                case "StringExtensions.Left": return TrySqlFunction(SqlFunction.LEFT, m.Type, m.GetArgument("str"), m.GetArgument("numChars"));
                case "StringExtensions.Right": return TrySqlFunction(SqlFunction.RIGHT, m.Type, m.GetArgument("str"), m.GetArgument("numChars"));
                case "StringExtensions.Replicate": return TrySqlFunction(SqlFunction.REPLICATE, m.Type, m.GetArgument("str"), m.GetArgument("times"));
                case "StringExtensions.Reverse": return TrySqlFunction(SqlFunction.REVERSE, m.Type, m.GetArgument("str"));
                case "StringExtensions.Like": return TryLike(m.GetArgument("str"), m.GetArgument("pattern"));

                case "DateTime.AddDays": return TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.day), m.GetArgument("value"), m.Object);
                case "DateTime.AddHours": return TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.hour), m.GetArgument("value"), m.Object);
                case "DateTime.AddMilliseconds": return TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.millisecond), m.GetArgument("value"), m.Object);
                case "DateTime.AddMinutes": return TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.minute), m.GetArgument("value"), m.Object);
                case "DateTime.AddMonths": return TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.month), m.GetArgument("months"), m.Object);
                case "DateTime.AddSeconds": return TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.second), m.GetArgument("value"), m.Object);
                case "DateTime.AddYears": return TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.year), m.GetArgument("value"), m.Object);
                
                    //dateadd(month, datediff(month, 0, SomeDate),0);
                case "DateTimeExtensions.MonthStart": return TrySqlMonthStart(m.GetArgument("dateTime"));
                case "DateTimeExtensions.YearsTo": return TrySqlFunction(SqlFunction.DATEDIFF, m.Type, new SqlEnumExpression(SqlEnums.year), m.GetArgument("min"), m.GetArgument("max"));
                case "DateTimeExtensions.MonthsTo": return TrySqlFunction(SqlFunction.DATEDIFF, m.Type, new SqlEnumExpression(SqlEnums.month), m.GetArgument("min"), m.GetArgument("max"));

                case "Math.Sign": return TrySqlFunction(SqlFunction.SIGN, m.Type, m.GetArgument("value"));
                case "Math.Abs": return TrySqlFunction(SqlFunction.ABS, m.Type, m.GetArgument("value"));
                case "Math.Sin": return TrySqlFunction(SqlFunction.SIN, m.Type, m.GetArgument("a"));
                case "Math.Asin": return TrySqlFunction(SqlFunction.ASIN, m.Type, m.GetArgument("d"));
                case "Math.Cos": return TrySqlFunction(SqlFunction.COS, m.Type, m.GetArgument("d"));
                case "Math.Acos": return TrySqlFunction(SqlFunction.ACOS, m.Type, m.GetArgument("d"));
                case "Math.Tan": return TrySqlFunction(SqlFunction.TAN, m.Type, m.GetArgument("a"));
                case "Math.Atan": return TrySqlFunction(SqlFunction.ATAN, m.Type, m.GetArgument("d"));
                case "Math.Atan2": return TrySqlFunction(SqlFunction.ATN2, m.Type, m.GetArgument("y"), m.GetArgument("x"));
                case "Math.Pow": return TrySqlFunction(SqlFunction.POWER, m.Type, m.GetArgument("x"), m.GetArgument("y"));
                case "Math.Sqrt": return TrySqlFunction(SqlFunction.SQRT, m.Type, m.GetArgument("d"));
                case "Math.Exp": return TrySqlFunction(SqlFunction.EXP, m.Type, m.GetArgument("d"));
                case "Math.Floor": return TrySqlFunction(SqlFunction.FLOOR, m.Type, m.GetArgument("d"));
                case "Math.Log10": return TrySqlFunction(SqlFunction.LOG10, m.Type, m.GetArgument("d"));
                case "Math.Log": return m.Arguments.Count != 1? null: TrySqlFunction(SqlFunction.LOG, m.Type, m.GetArgument("d"));
                case "Math.Ceiling": return TrySqlFunction(SqlFunction.CEILING, m.Type, m.TryGetArgument("d") ?? m.GetArgument("a"));
                case "Math.Round": return TrySqlFunction(SqlFunction.ROUND, m.Type,
                    m.TryGetArgument("a") ?? m.TryGetArgument("d") ?? m.GetArgument("value"),
                    m.TryGetArgument("decimals") ?? m.TryGetArgument("digits") ?? new SqlConstantExpression(0));
                case "Math.Truncate": return TrySqlFunction(SqlFunction.ROUND, m.Type, m.GetArgument("d"), new SqlConstantExpression(0), new SqlConstantExpression(1));
                case "ExpressionNominatorExtensions.InSql":
                    using (ForceFullNominate())
                    {
                        return Visit(m.GetArgument("value"));
                    }

                default: return null; 
            }
        }

        IDisposable ForceFullNominate()
        {
            bool oldTemp = tempFullNominate;
            tempFullNominate = true;
            return new Disposable(() => tempFullNominate = oldTemp); 
        }
    }      
}
