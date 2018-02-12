using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class DatePartStartToken : QueryToken
    {
        public QueryTokenMessage Name { get; private set; }

        internal DatePartStartToken(QueryToken parent, QueryTokenMessage name)
            : base(parent)
        {
            this.Name = name;
        }

        private static MethodInfo GetMethodInfo(QueryTokenMessage name)
        {
            return 
                name == QueryTokenMessage.MonthStart ? miMonthStart :
                name == QueryTokenMessage.WeekStart ? miWeekStart :
                name == QueryTokenMessage.HourStart ? miHourStart :
                name == QueryTokenMessage.MinuteStart ? miMinuteStart :
                name == QueryTokenMessage.SecondStart ? miSecondStart :
                throw new InvalidOperationException("Unexpected name");
        }

        public override string ToString()
        {
            return this.Name.NiceToString();
        }

        public override string NiceName()
        {
            return this.Name.NiceToString() + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
        }

        public override string Format
        {
            get
            {
                return
                    Name == QueryTokenMessage.MonthStart ? "Y" :
                    Name == QueryTokenMessage.WeekStart ? "d" :
                    Name == QueryTokenMessage.HourStart ? "g" :
                    Name == QueryTokenMessage.MinuteStart ? "g":
                    Name == QueryTokenMessage.SecondStart ? "G" :
                    throw new InvalidOperationException("Unexpected name");
            }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override Type Type
        {
            get { return typeof(DateTime?); }
        }

        public override string Key
        {
            get { return this.Name.ToString(); }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>();
        }

        public static MethodInfo miMonthStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MonthStart(DateTime.MinValue));
        public static MethodInfo miWeekStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.WeekStart(DateTime.MinValue));
        public static MethodInfo miHourStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.HourStart(DateTime.MinValue));
        public static MethodInfo miMinuteStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MinuteStart(DateTime.MinValue));
        public static MethodInfo miSecondStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.SecondStart(DateTime.MinValue));

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);
            var mi = GetMethodInfo(this.Name);
            return Expression.Call(mi, exp.UnNullify()).Nullify();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {  
            return null;
        }

        public override string IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new DatePartStartToken(Parent.Clone(), this.Name);
        }

        public override bool IsGroupable
        {
            get { return true; }
        }
    }

    [Serializable]
    public class DayOfYearToken : QueryToken
    {
        internal DayOfYearToken(QueryToken parent)
            : base(parent)
        {
        }

        public override string ToString()
        {
            return QueryTokenMessage.DayOfYear.NiceToString();
        }

        public override string NiceName()
        {
            return QueryTokenMessage.DayOfYear.NiceToString() + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
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
            get { return typeof(int?); }
        }

        public override string Key
        {
            get { return "DayOfYear"; }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>();
        }

        static PropertyInfo piDayOfYear = ReflectionTools.GetPropertyInfo(() => DateTime.MinValue.DayOfYear);

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            return Expression.Property(exp.UnNullify(), piDayOfYear).Nullify();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new DayOfYearToken(Parent.Clone());
        }
    }



    [Serializable]
    public class DayOfWeekToken : QueryToken
    {
        internal DayOfWeekToken(QueryToken parent)
            : base(parent)
        {
        }

        public override string ToString()
        {

            return QueryTokenMessage.DayOfWeek.NiceToString();
        }

        public override string NiceName()
        {
            return QueryTokenMessage.DayOfWeek.NiceToString() + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
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
            get { return typeof(DayOfWeek?); }
        }

        public override string Key
        {
            get { return "DayOfWeek"; }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>();
        }

        static PropertyInfo piDayOfWeek = ReflectionTools.GetPropertyInfo(() => DateTime.MinValue.DayOfWeek);

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            return Expression.Property(exp.UnNullify(), piDayOfWeek).Nullify();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new DayOfWeekToken(Parent.Clone());
        }
    }


    [Serializable]
    public class WeekNumberToken : QueryToken
    {
        internal WeekNumberToken(QueryToken parent)
            : base(parent)
        {
        }

        public override string ToString()
        {
            return QueryTokenMessage.WeekNumber.NiceToString();
        }

        public override string NiceName()
        {
            return QueryTokenMessage.WeekNumber.NiceToString() + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
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
            get { return typeof(int?); }
        }

        public override string Key
        {
            get { return "WeekNumber"; }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>();
        }

        static MethodInfo miWeekNumber = ReflectionTools.GetMethodInfo(() => DateTime.MinValue.WeekNumber());

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            return Expression.Call(miWeekNumber, exp.UnNullify()).Nullify();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new WeekNumberToken(Parent.Clone());
        }
    }   
}
