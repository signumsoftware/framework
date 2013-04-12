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
    public class MonthStartToken : QueryToken
    {
        internal MonthStartToken(QueryToken parent)
            : base(parent)
        {
        }

        public override string ToString()
        {
            return QueryTokenMessage.MonthStart.NiceToString();
        }

        public override string NiceName()
        {
            return QueryTokenMessage.MonthStart.NiceToString() + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
        }

        public override string Format
        {
            get { return "Y"; }
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
            get { return "MonthStart"; }
        }

        protected override List<QueryToken> SubTokensOverride()
        {
            return new List<QueryToken>();
        }

        static MethodInfo miMonthStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MonthStart(DateTime.MinValue));

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);
            
            return Expression.Call(miMonthStart, exp.UnNullify()).Nullify();
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
            return new MonthStartToken(Parent.Clone());
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

        protected override List<QueryToken> SubTokensOverride()
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

        protected override List<QueryToken> SubTokensOverride()
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

}
