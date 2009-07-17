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

namespace Signum.Engine.Linq
{
    /// <summary>
    /// Nominator is a class that walks an expression tree bottom up, determining the set of 
    /// candidate expressions that are possible columns of a select expression
    /// </summary>
    internal class Nominator : DbExpressionVisitor
    {
        public static ConditionsRewriter ConditionsRewriter = new ConditionsRewriter();

        string[] existingAliases;
        HashSet<Expression> candidates = new HashSet<Expression>();

        private Nominator() { }

        static internal HashSet<Expression> Nominate(Expression expression, string[] existingAliases, out Expression newExpression)
        {
            Nominator n = new Nominator { existingAliases = existingAliases };
            newExpression = n.Visit(expression);
            return n.candidates;
        }

        static internal Expression FullNominate(Expression expression, bool isCondition)
        {
            Nominator n = new Nominator { existingAliases = null };
            Expression result = n.Visit(expression);
            if (!n.candidates.Contains(result))
                throw new ApplicationException(Resources.TheExpressionCanTBeTranslatedToSQL + expression.ToString());

            if (isCondition)
                result = ConditionsRewriter.MakeSqlCondition(result);
            else
                result = ConditionsRewriter.MakeSqlValue(result); 

            return result;
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {

            if (existingAliases == null || 
                // existingAliases is null when used in QueryBinder, not ColumnProjector
                // this allows to make function changes in where clausules but keeping the full expression (not compressing it in one column)
                existingAliases.Contains(column.Alias))
                candidates.Add(column);
            return column;
        }

        protected override NewExpression VisitNew(NewExpression nex)
        {
            if (existingAliases == null)
            {
                IEnumerable<Expression> args = this.VisitExpressionList(nex.Arguments);
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
            candidates.Add(c);
            return c;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            if (proj.IsOneCell)
            {
                if (proj.UniqueFunction == UniqueFunction.SingleIsZero)
                {
                    var newProj = new ProjectionExpression(typeof(int), proj.Source, proj.Projector, UniqueFunction.Single);
                    candidates.Add(newProj); 
                    Expression result = Expression.Equal(newProj, Expression.Constant(0));
                    candidates.Add(result);
                    return result;
                }
                else if (proj.UniqueFunction == UniqueFunction.SingleGreaterThanZero)
                {
                    var newProj = new ProjectionExpression(typeof(int), proj.Source, proj.Projector, UniqueFunction.Single);
                    candidates.Add(newProj); 
                    Expression result = Expression.GreaterThan(newProj, Expression.Constant(0));
                    candidates.Add(result);
                    return result;
                }
                else
                {
                    candidates.Add(proj);
                    return proj;
                }
            }
            else
                return base.VisitProjection(proj);
        }

        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
        {
            candidates.Add(sqlFunction);
            return sqlFunction;
        }

        protected Expression TrySqlFunction(SqlFunction sqlFunction, Type type, params Expression[] expression)
        {
            return TrySqlFunction(sqlFunction.ToString(), type, expression); 
        }

        protected Expression TrySqlFunction(string sqlFunction, Type type, params Expression[] expression)
        {
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
            if (!candidates.Contains(expr))
                return null;

            Expression result = DateAdd(SqlEnums.hour, MinusDatePart(SqlEnums.hour, expr),
                                    DateAdd(SqlEnums.minute, MinusDatePart(SqlEnums.minute, expr),
                                        DateAdd(SqlEnums.second, MinusDatePart(SqlEnums.second, expr),
                                            DateAdd(SqlEnums.millisecond, MinusDatePart(SqlEnums.millisecond, expr), expr))));

            candidates.Add(result);
            return result; 
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
            if (existingAliases == null)
            {
                var nuevo = Transform(b);
                if (nuevo != null)
                    return Visit(nuevo);
            }

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

            if (candidates.Contains(left) && candidates.Contains(right))
                candidates.Add(b);

            return b;
        }

        private static Expression Transform(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual)
            {
                var newb = SmartEqualizer.PolymorphicEqual(b.Left, b.Right);

                if (newb.NodeType == b.NodeType && ((BinaryExpression)newb).Map(nb => nb.Left == b.Left && nb.Right == b.Right))
                {
                    return null; 
                }
                else if (b.NodeType == ExpressionType.NotEqual)
                    return Expression.Not(newb);
                else
                    return newb;
            }
            return null;
        }


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
                Expression newTest = ConditionsRewriter.MakeSqlCondition(test); 

                if (ifFalse.NodeType == (ExpressionType)DbExpressionType.Case)
                {
                    var oldC  = (CaseExpression)ifFalse;
                    candidates.Remove(ifFalse); // just to save some memory
                    result = new CaseExpression(oldC.Whens.PreAnd(new When(newTest, ifTrue)), oldC.DefaultValue);
                }
                else
                    result = new CaseExpression(new[] { new When(newTest, ifTrue) }, ifFalse);

                candidates.Add(result);
            }
            return result;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            Expression operand = this.Visit(u.Operand);
            if (operand != u.Operand)
                u = Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);

            if (candidates.Contains(operand))
                candidates.Add(u);

            return u;
        }

        protected override Expression VisitIn(InExpression inExp)
        {
            Expression exp = this.Visit(inExp.Expression);
            if (exp != inExp.Expression)
                inExp = new InExpression(exp, inExp.Values);

            if (candidates.Contains(exp))
                candidates.Add(inExp);

            return inExp;
        }

        protected override Expression VisitSqlEnum(SqlEnumExpression sqlEnum)
        {
            candidates.Add(sqlEnum);
            return sqlEnum;
        }

         private LikeExpression TryLike(Expression expression, Expression pattern)
        {
             pattern = ExpressionEvaluator.PartialEval(pattern);
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
            Expression newM = new Switch<string, Expression>(m.Member.DeclaringType.TypeName() + "." + m.Member.Name)
                            .Case("string.Length", a => TrySqlFunction(SqlFunction.LEN, m.Type, m.Expression))
                            .Case("Math.PI", a => TrySqlFunction(SqlFunction.PI, m.Type))
                            .Case("DateTime.Now", a => TrySqlFunction(SqlFunction.GETDATE, m.Type))
                            .Case("DateTime.Year", a => TrySqlFunction(SqlFunction.YEAR, m.Type, m.Expression))
                            .Case("DateTime.Month", a => TrySqlFunction(SqlFunction.MONTH, m.Type, m.Expression))
                            .Case("DateTime.Day", a => TrySqlFunction(SqlFunction.DAY, m.Type, m.Expression))
                            .Case("DateTime.DayOfYear", a => TrySqlFunction(SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.dayofyear), m.Expression))
                            .Case("DateTime.Hour", a => TrySqlFunction(SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.hour), m.Expression))
                            .Case("DateTime.Minute", a => TrySqlFunction(SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.minute), m.Expression))
                            .Case("DateTime.Second", a => TrySqlFunction(SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.second), m.Expression))
                            .Case("DateTime.Millisecond", a => TrySqlFunction(SqlFunction.DATEPART, m.Type, new SqlEnumExpression(SqlEnums.millisecond), m.Expression))
                            .Case("DateTime.Date", a=>TrySqlDate(m.Expression))
                            .Case("TimeSpan.TotalDays", a=> TrySqlDifference(SqlEnums.day, m.Type, m.Expression))
                            .Case("TimeSpan.TotalHours", a=> TrySqlDifference(SqlEnums.hour, m.Type, m.Expression))
                            .Case("TimeSpan.TotalMilliseconds", a=> TrySqlDifference(SqlEnums.millisecond, m.Type, m.Expression))
                            .Case("TimeSpan.TotalSeconds", a=> TrySqlDifference(SqlEnums.second, m.Type, m.Expression))
                            .Case("TimeSpan.TotalMinutes", a=> TrySqlDifference(SqlEnums.minute, m.Type, m.Expression))
                            .Default((Expression)null);

             if (newM != null)
                return newM;
     
            if (m.Expression.Type.IsNullable() && (m.Member.Name == "Value" || m.Member.Name == "HasValue"))
            {
                Expression expression = this.Visit(m.Expression);

                if (m.Member.Name == "Value")
                    newM = Expression.Convert(expression, m.Expression.Type.UnNullify());
                else
                    newM = Expression.NotEqual(expression, Expression.Constant(null));

                if (candidates.Contains(expression))
                    candidates.Add(newM);

                return newM;
            }

            return base.VisitMemberAccess(m);
        }

       



        static MethodInfo c = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            SqlMethodAttribute sma = m.Method.SingleAttribute<SqlMethodAttribute>();
            if (sma != null)
                return TrySqlFunction(sma.Name?? m.Method.Name, m.Type, m.Arguments.ToArray()); 

            Expression newM = new Switch<string, Expression>(m.Method.DeclaringType.TypeName() + "." + m.Method.MethodName())
                            .Case("string.IndexOf", a => TrySqlFunction(SqlFunction.CHARINDEX, m.Type, m.GetArgument("value"), m.Object, m.TryGetArgument("startIndex").TryCC(e => Expression.Add(e, Expression.Constant(1)))).TryCC(e => Expression.Subtract(e, Expression.Constant(1))))
                            .Case("string.ToLower", a => TrySqlFunction(SqlFunction.LOWER, m.Type, m.Object))
                            .Case("string.ToUpper", a => TrySqlFunction(SqlFunction.UPPER, m.Type, m.Object))
                            .Case("string.TrimStart", a => TrySqlFunction(SqlFunction.LTRIM, m.Type, m.Object))
                            .Case("string.TrimEnd", a => TrySqlFunction(SqlFunction.RTRIM, m.Type, m.Object))
                            .Case("string.Replace", a => TrySqlFunction(SqlFunction.REPLACE, m.Type, m.Object, m.GetArgument("oldValue"), m.GetArgument("newValue")))
                            .Case("string.Substring", a => TrySqlFunction(SqlFunction.SUBSTRING, m.Type, m.Object, Expression.Add(m.GetArgument("startIndex"), Expression.Constant(1)), m.TryGetArgument("length") ?? Expression.Constant(int.MaxValue)))
                            // escapar los patrones es muy complicado en expresiones generales (hacerlo en SQL)
                            .Case("string.Contains", a=>TryLike(m.Object, Expression.Add(Expression.Add( Expression.Constant("%"), m.GetArgument("value"), c), Expression.Constant("%"), c))) 
                            .Case("string.StartsWith", a => TryLike(m.Object, Expression.Add(m.GetArgument("value"), Expression.Constant("%"), c)))
                            .Case("string.EndsWith", a => TryLike(m.Object, Expression.Add(Expression.Constant("%"), m.GetArgument("value"), c)))
                          
                            .Case("StringExtensions.Left", a => TrySqlFunction(SqlFunction.LEFT, m.Type, m.GetArgument("s"), m.GetArgument("numChars")))
                            .Case("StringExtensions.Right", a => TrySqlFunction(SqlFunction.RIGHT, m.Type, m.GetArgument("s"), m.GetArgument("numChars")))
                            .Case("StringExtensions.Replicate", a => TrySqlFunction(SqlFunction.REPLICATE, m.Type, m.GetArgument("s"), m.GetArgument("times")))
                            .Case("StringExtensions.Reverse", a => TrySqlFunction(SqlFunction.REVERSE, m.Type, m.GetArgument("s")))
                            .Case("StringExtensions.Like", a=>TryLike(m.GetArgument("s"), m.GetArgument("pattern"))) 

                            .Case("DateTime.AddDays", a=>TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.day), m.GetArgument("value"), m.Object))
                            .Case("DateTime.AddHours", a=>TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.hour), m.GetArgument("value"), m.Object))
                            .Case("DateTime.AddMilliseconds", a=>TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.millisecond), m.GetArgument("value"), m.Object))
                            .Case("DateTime.AddMinutes", a=>TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.minute), m.GetArgument("value"), m.Object))
                            .Case("DateTime.AddMonths", a=>TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.month), m.GetArgument("value"), m.Object))
                            .Case("DateTime.AddSeconds", a=>TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.second), m.GetArgument("value"), m.Object))
                            .Case("DateTime.AddYears", a=>TrySqlFunction(SqlFunction.DATEADD, m.Type, new SqlEnumExpression(SqlEnums.year), m.GetArgument("value"), m.Object))

                            .Case("Math.Sign", a => TrySqlFunction(SqlFunction.SIGN, m.Type, m.GetArgument("value")))
                            .Case("Math.Abs", a => TrySqlFunction(SqlFunction.ABS, m.Type, m.GetArgument("value")))
                            .Case("Math.Sin", a => TrySqlFunction(SqlFunction.SIN, m.Type, m.GetArgument("a")))
                            .Case("Math.Asin", a => TrySqlFunction(SqlFunction.ASIN, m.Type, m.GetArgument("d")))
                            .Case("Math.Cos", a => TrySqlFunction(SqlFunction.COS, m.Type, m.GetArgument("d")))
                            .Case("Math.Acos", a => TrySqlFunction(SqlFunction.ACOS, m.Type, m.GetArgument("d")))
                            .Case("Math.Tan", a => TrySqlFunction(SqlFunction.TAN, m.Type, m.GetArgument("a")))
                            .Case("Math.Pow", a => TrySqlFunction(SqlFunction.POWER, m.Type, m.GetArgument("x"), m.GetArgument("y")))
                            .Case("Math.Sqrt", a => TrySqlFunction(SqlFunction.SQRT, m.Type, m.GetArgument("d")))
                            .Case("Math.Exp", a => TrySqlFunction(SqlFunction.EXP, m.Type, m.GetArgument("d")))
                            .Case("Math.Floor", a => TrySqlFunction(SqlFunction.FLOOR, m.Type, m.GetArgument("d")))
                            .Case("Math.Log10", a => TrySqlFunction(SqlFunction.Log10, m.Type, m.GetArgument("d")))
                            .Case("Math.Ceiling", a => TrySqlFunction(SqlFunction.CEILING, m.Type, m.TryGetArgument("d") ?? m.GetArgument("a")))
                            .Case("Math.Round", a => TrySqlFunction(SqlFunction.ROUND, m.Type,
                                m.TryGetArgument("a") ?? m.TryGetArgument("d") ?? m.GetArgument("value"),
                                m.TryGetArgument("decimals") ?? m.TryGetArgument("digits") ?? Expression.Constant(0)))
                            .Default((Expression)null);

            return newM ?? base.VisitMethodCall(m);
        }

        protected override Expression VisitImplementedBy(ImplementedByExpression reference)
        {
            if (existingAliases != null)
                return base.VisitImplementedBy(reference); 

            var newImple = reference.Implementations
              .NewIfChange(ri => Visit(ri.Field).Map(r => r == ri.Field ? ri : new ImplementationColumnExpression(ri.Type, (FieldInitExpression)r)));

            if (newImple != reference.Implementations)
                reference = new ImplementedByExpression(reference.Type, newImple);

            if (newImple.All(i => candidates.Contains(i.Field)))
                candidates.Add(reference);

            return reference;
        }

        protected override Expression VisitImplementedByAll(ImplementedByAllExpression reference)
        {
            if (existingAliases != null || reference.Implementations != null && reference.Implementations.Count > 0)
                return base.VisitImplementedByAll(reference); 

            var id = (ColumnExpression)Visit(reference.ID);
            var typeId = (ColumnExpression)Visit(reference.TypeID);

            if (id != reference.ID || typeId != reference.TypeID)
                reference = new ImplementedByAllExpression(reference.Type, id, typeId);

            if (candidates.Contains(id) && candidates.Contains(typeId))
                candidates.Add(reference);

            return reference;
        }

        protected override Expression VisitFieldInit(FieldInitExpression fieldInit)
        {
            if (existingAliases != null || fieldInit.Bindings != null && fieldInit.Bindings.Count > 0)
                return base.VisitFieldInit(fieldInit);

            var id = Visit(fieldInit.ExternalId);
            var alias = VisitFieldInitAlias(fieldInit.Alias);
            if (fieldInit.ExternalId != id)
                fieldInit = new FieldInitExpression(fieldInit.Type, alias, id);

            if (candidates.Contains(id))
                candidates.Add(fieldInit);
            
            return fieldInit;
        }
    }
}
