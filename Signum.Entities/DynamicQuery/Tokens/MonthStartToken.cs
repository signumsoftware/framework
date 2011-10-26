using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Entities.Properties;
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
            return Resources.MonthStart;
        }

        public override string NiceName()
        {
            return Resources.MonthStart + Resources.Of + Parent.ToString();
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

        protected override List<QueryToken> SubTokensInternal()
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
            return new MonthStartToken(Parent.Clone());
        }
    }   
}
