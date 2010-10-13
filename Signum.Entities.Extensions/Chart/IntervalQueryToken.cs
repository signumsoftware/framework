using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Globalization;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Utilities.Properties;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Chart
{
    [Serializable]
    public class IntervalQueryToken : QueryToken, INotifyPropertyChanged, IDataErrorInfo
    {
        bool isDecimal;

        public IntervalQueryToken(QueryToken parent)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            var ft = QueryUtils.GetFilterType(parent.Type);

            if (ft != FilterType.Number && ft != FilterType.DecimalNumber)
                throw new ArgumentException("parent should be a Number");

            isDecimal = ft == FilterType.DecimalNumber;

            Type t = parent.Type.UnNullify();

            Intervals = (ft == FilterType.DecimalNumber ? "... {0:0.0} {1:0.0} ..." : "... {0} {1} ...").Formato(0, 10);
        }

        //0,5,6.5,8
        string intervals;
        public string Intervals
        {
            get { return intervals; }
            set
            {
                intervals = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Intervals"));
            }
        }

        public static readonly decimal RepeatMin = Decimal.MinValue + 1;
        public static readonly decimal RepeatMax = Decimal.MaxValue - 1;


        class IntervalDefinition
        {
            public bool RepeatInitial;
            public decimal[] Splits;
            public bool RepeatFinal;

            public decimal First { get { return Splits[0]; } }
            public decimal Last { get { return Splits[Splits.Length - 1]; } }

            public decimal FirstStep { get { return Splits[1] - Splits[0]; } }
            public decimal LastStep { get { return Splits[Splits.Length - 1] - Splits[Splits.Length - 2]; } }
        }

        //... num1 num2 num3 ...
        static IntervalDefinition ParseInterval(string interval, bool isDecimal, out string error)
        {
            interval = interval.Trim();

            error = null;

            if (string.IsNullOrEmpty(interval))
                return new IntervalDefinition { Splits = new decimal[0] };

            IntervalDefinition result = new IntervalDefinition();

            if (interval.StartsWith("..."))
            {
                result.RepeatInitial = true;
                interval = interval.RemoveLeft(3);
            }

            if(interval.EndsWith("..."))
            {
                result.RepeatFinal = true;
                interval = interval.RemoveRight(3);
            }

            interval = interval.Trim();

            string[] parts = interval.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            decimal[] nums = new decimal[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                string capture = parts[i];
                int val;
                if (int.TryParse(capture, isDecimal ? NumberStyles.Float : NumberStyles.Integer, CultureInfo.CurrentCulture, out val))
                    nums[i] = val;
                else
                {
                    error = "'{0}' is not a valid number".Formato(capture);
                    return null;
                }
            }

            error = nums.BiSelect((min, max) => min >= max ? "Interval sequence not in order" : null).NotNull().FirstOrDefault();
            if (error != null)
                return null;


            if ((result.RepeatInitial || result.RepeatFinal) && nums.Length <= 1)
            {
                error = "In order to use ... write at least two numbers";
                return null;
            }

            result.Splits = nums;

            return result;
        }

        public string Error
        {
            get { return this["Intervals"]; }
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == "Intervals")
                {
                    string error;
                    var result = ParseInterval(intervals, isDecimal, out error);

                    if (result == null)
                        return error;
                }

                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return "Interval";
        }

        public override string NiceName()
        {
            return "{0} in Intervals like {1}".Formato(Parent.NiceName(), Intervals);
        }

        public override string Format
        {
            get { return Parent.Format; }
        }

        public override string Unit
        {
            get { return Parent.Unit; }
        }

        public override Type Type
        {
            get { return typeof(NullableInterval<>).MakeGenericType(Parent.Type.UnNullify()); }
        }

        public override string Key
        {
            get { return "Interval({1})".Formato(Parent.Key, Intervals.Replace('.', ',')); }
        }

        static Regex reg = new Regex(@"^Interval\((?<pattern>.*?)\)$");

        public override QueryToken MatchPart(string key)
        {
            Match m = reg.Match(key);

            if (!m.Success)
                return null;

            return new IntervalQueryToken(this.Parent) { Intervals = m.Groups["pattern"].Value.Replace(',', '.') };
        }

        protected override QueryToken[] SubTokensInternal()
        {
            return null;
        }

        public override Expression BuildExpression(Expression expression)
        {
            string error;
            IntervalDefinition intervals = ParseInterval(Intervals, isDecimal, out error);
            if (intervals == null)
                throw new InvalidOperationException(error);

            Expression exp = Parent.BuildExpression(expression);

            Type t = Parent.Type.UnNullify();
            Expression sqlExpression = CreateSqlExpression(intervals, exp, t);
            LambdaExpression projector = GetProjector(intervals, t);

            Delegate projDelegate = projector.Compile();

            return Expression.Invoke(Expression.Constant(projDelegate), Expression.Call(miInSql.MakeGenericMethod(sqlExpression.Type), sqlExpression));
        }

        private static LambdaExpression GetProjector(IntervalDefinition intervals, Type t)
        {
            // C# Projector:
            // i < 0? Interval( 2 * i + 0, 2 * (i  + 1) + 0)
            // i == 0 ? Interval(0, 2)
            // i == 1?  Interval(2, 4)
            // i == 2?  Interval(4, 10)
            //          Interval(6 * (i - 3) + 10, 6 * (i - 2) + 10)


            ParameterExpression index = Expression.Parameter(typeof(int), "index");
            ConstructorInfo ci = typeof(NullableInterval<>).MakeGenericType(t).GetConstructor(new[] { t, t });

            if (intervals.Splits.Length == 0)
                return Expression.Lambda(Expression.New(ci, Expression.Constant(null, t.Nullify()), Expression.Constant(null, t.Nullify())), index);

            Expression init = !intervals.RepeatInitial ? Expression.New(ci, Expression.Constant(null, t.Nullify()), Expression.Constant(Convert.ChangeType(intervals.First, t)).Nullify()) :
                Expression.New(ci,
                    Expression.Add(
                        Expression.Multiply(
                            Expression.Constant(Convert.ChangeType(intervals.FirstStep, t)),
                            Cast(index, t)),
                        Expression.Constant(Convert.ChangeType(intervals.First, t))).Nullify(),
                    Expression.Add(
                        Expression.Multiply(
                            Expression.Constant(Convert.ChangeType(intervals.FirstStep, t)),
                            Cast(Expression.Add(index, Expression.Constant(1)), t)),
                        Expression.Constant(Convert.ChangeType(intervals.First, t))).Nullify());

            Expression end = !intervals.RepeatFinal ? Expression.New(ci, Expression.Constant(Convert.ChangeType(intervals.Last, t)).Nullify(), Expression.Constant(null, t.Nullify())) :
                Expression.New(ci,
                    Expression.Add(
                        Expression.Multiply(
                            Expression.Constant(Convert.ChangeType(intervals.LastStep, t)),
                            Cast(Expression.Subtract(index, Expression.Constant(intervals.Splits.Length - 1)), t)),
                        Expression.Constant(Convert.ChangeType(intervals.Last, t))).Nullify(),
                    Expression.Add(
                        Expression.Multiply(
                            Expression.Constant(Convert.ChangeType(intervals.LastStep, t)),
                            Cast(Expression.Subtract(index, Expression.Constant(intervals.Splits.Length - 2)), t)),
                        Expression.Constant(Convert.ChangeType(intervals.Last, t))).Nullify());

            Expression bodyProjector = end;

            for (int i = intervals.Splits.Length - 2; i >= 0; i--)
            {
                Expression test = Expression.Equal(index, Expression.Constant(i));
                Expression val = Expression.New(ci,
                        Expression.Constant(Convert.ChangeType(intervals.Splits[i], t)).Nullify(),
                        Expression.Constant(Convert.ChangeType(intervals.Splits[i + 1], t)).Nullify());
                bodyProjector = Expression.Condition(test, val, bodyProjector);
            }

            bodyProjector = Expression.Condition(Expression.LessThan(index, Expression.Constant(0)), init, bodyProjector);

            return Expression.Lambda(bodyProjector, index);
        }

        MethodInfo miInSql = ReflectionTools.GetMethodInfo(() => ExpressionNominatorExtensions.InSql(0)).GetGenericMethodDefinition();

        private Expression CreateSqlExpression(IntervalDefinition intervals, Expression exp, Type t)
        {
            // Input: 
            // ...0 2 4 10...
            // SqlExpression:
            // e < 0 ? Floor((e-0)/2)-1
            // e < 2? 0
            // e < 4? 1
            // e < 10? 2
            // 3 + Floor((e-10)/6)

            if (intervals.Splits.Length == 0)
                return Expression.Condition(Expression.LessThan(exp, Expression.Constant(Convert.ChangeType(0, t))), Expression.Constant(0), Expression.Constant(0)); //SQL is so blut 


            Expression sqlExpression;
            Expression init = !intervals.RepeatInitial ? (Expression)Expression.Constant(-1) :
                    Floor(Expression.Divide(
                            Expression.Subtract(Cast(exp, typeof(decimal)), Expression.Constant(intervals.First)),
                            Expression.Constant(intervals.FirstStep)));

            Expression end = !intervals.RepeatFinal ? (Expression)Expression.Constant(intervals.Splits.Length - 1) :
                Expression.Add(
                    Expression.Constant(intervals.Splits.Length - 1),
                    Floor(Expression.Divide(
                            Expression.Subtract(Cast(exp, typeof(decimal)), Expression.Constant(intervals.Last)),
                            Expression.Constant(intervals.LastStep))));

            sqlExpression = end;

            for (int i = intervals.Splits.Length - 1; i >= 0; i--)
            {
                Expression test = Expression.LessThan(exp, Expression.Constant(Convert.ChangeType(intervals.Splits[i], t)));
                Expression val = i == 0 ? init : Expression.Constant(i - 1);
                sqlExpression = Expression.Condition(test, val, sqlExpression);
            }
            return sqlExpression;
        }

        MethodInfo miFloorDecimal = ReflectionTools.GetMethodInfo(() => Math.Floor(0.0m));
        MethodInfo miFloorDouble = ReflectionTools.GetMethodInfo(() => Math.Floor(0.0));

        Expression Floor(Expression expr)
        {
            if (expr.Type == typeof(double))
                return Expression.Convert(Expression.Call(miFloorDouble, expr), typeof(int));
            else if (expr.Type == typeof(decimal))
                return Expression.Convert(Expression.Call(miFloorDecimal, expr), typeof(int));
            else
                return Expression.Convert(Expression.Call(miFloorDouble, Expression.Convert(expr, typeof(double))), typeof(int));
        }

        static Expression Cast(Expression expression, Type type)
        {
            if (expression.Type == type)
                return expression;
            return Expression.Convert(expression, type);
        }


        public override PropertyRoute GetPropertyRoute()
        {
            return Parent.GetPropertyRoute(); 
        }

        public override Implementations Implementations()
        {
            return null;
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed(); 
        }

        public override QueryToken Clone()
        {
            return new IntervalQueryToken(Parent.Clone()) { Intervals = Intervals };
        }
    }

    [Serializable]
    public class CountAllToken: QueryToken
    {
        public CountAllToken() : base(null) { }

        public override string ToString()
        {
            return "All"; 
        }

        public override string NiceName()
        {
            return "All";
        }

        public override string Format
        {
            get { return null; }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override Type Type
        {
            get { return typeof(int); }
        }

        public override string Key
        {
            get { return "All"; }
        }

        protected override QueryToken[] SubTokensInternal()
        {
            return null;
        }

        public override Expression BuildExpression(Expression expression)
        {
            return Expression.Constant(1); 
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return null;
        }

        public override Implementations Implementations()
        {
            return null;
        }

        public override bool IsAllowed()
        {
            return true; 
        }

        public override QueryToken Clone()
        {
            return new CountAllToken(); 
        }
    }
}
