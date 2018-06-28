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
                name == QueryTokenMessage.QuarterStart ? miQuarterStart :
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
                    Name == QueryTokenMessage.QuarterStart ? "d" :
                    Name == QueryTokenMessage.WeekStart ? "d" :
                    Name == QueryTokenMessage.HourStart ? "g" :
                    Name == QueryTokenMessage.MinuteStart ? "g" :
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
        public static MethodInfo miQuarterStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.QuarterStart(DateTime.MinValue));
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
}
