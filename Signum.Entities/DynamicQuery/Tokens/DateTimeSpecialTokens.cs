using System;
using System.Collections.Generic;
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
        QueryToken parent;
        public override QueryToken? Parent => parent;
        
        internal DatePartStartToken(QueryToken parent, QueryTokenMessage name)
        {
            this.Name = name;
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        private static MethodInfo GetMethodInfoDateTime(QueryTokenMessage name)
        {
            return
                name == QueryTokenMessage.MonthStart ? miMonthStart :
                name == QueryTokenMessage.QuarterStart ? miQuarterStart :
                name == QueryTokenMessage.WeekStart ? miWeekStart :
                name == QueryTokenMessage.HourStart ? miHourStart :
                name == QueryTokenMessage.MinuteStart ? miMinuteStart :
                name == QueryTokenMessage.SecondStart ? miSecondStart :
                throw new InvalidOperationException("Unexpected name");
        }

        private static MethodInfo GetMethodInfoDate(QueryTokenMessage name)
        {
            return
                name == QueryTokenMessage.MonthStart ? miDMonthStart :
                name == QueryTokenMessage.QuarterStart ? miDQuarterStart :
                name == QueryTokenMessage.WeekStart ? miDWeekStart :
                throw new InvalidOperationException("Unexpected name");
        }

        public override string ToString()
        {
            return this.Name.NiceToString();
        }

        public override string NiceName()
        {
            return this.Name.NiceToString() + QueryTokenMessage.Of.NiceToString() + parent.ToString();
        }

        public override string? Format
        {
            get
            {
                return
                    Name == QueryTokenMessage.MonthStart ? "Y" :
                    Name == QueryTokenMessage.QuarterStart ? "d" :
                    Name == QueryTokenMessage.WeekStart ? "d" :
                    Name == QueryTokenMessage.HourStart ? "g" :
                    Name == QueryTokenMessage.MinuteStart ? "g" :
                    Name == QueryTokenMessage.SecondStart ? "G" :
                    throw new InvalidOperationException("Unexpected name");
            }
        }

        public override string? Unit
        {
            get { return null; }
        }

        public override Type Type
        {
            get { return Parent!.Type.Nullify(); }
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
        public static MethodInfo miQuarterStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.QuarterStart(DateTime.MinValue));
        public static MethodInfo miWeekStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.WeekStart(DateTime.MinValue));
        public static MethodInfo miHourStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.HourStart(DateTime.MinValue));
        public static MethodInfo miMinuteStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MinuteStart(DateTime.MinValue));
        public static MethodInfo miSecondStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.SecondStart(DateTime.MinValue));

        public static MethodInfo miDMonthStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MonthStart(Date.MinValue));
        public static MethodInfo miDQuarterStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.QuarterStart(Date.MinValue));
        public static MethodInfo miDWeekStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.WeekStart(Date.MinValue));

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = parent.BuildExpression(context);
            var mi = parent.Type.UnNullify() == typeof(Date) ? 
                    GetMethodInfoDate(this.Name) :
                    GetMethodInfoDateTime(this.Name);

            return Expression.Call(mi, exp.UnNullify()).Nullify();
        }

        public override PropertyRoute? GetPropertyRoute()
        {
            return parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string? IsAllowed()
        {
            return parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new DatePartStartToken(parent.Clone(), this.Name);
        }

        public override bool IsGroupable
        {
            get { return true; }
        }
    }
}
