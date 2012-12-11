using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Entities.Properties;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class DateToken : QueryToken
    {
        internal DateToken(QueryToken parent)
            : base(parent)
        {
        }

        public override string ToString()
        {
            return Resources.Date;
        }

        public override string NiceName()
        {
            return Resources.Date + Resources.Of + Parent.ToString();
        }

        public override string Format
        {
            get { return "d"; }
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
            get { return "Date"; }
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            return new List<QueryToken>();
        }

        static PropertyInfo miDate = ReflectionTools.GetPropertyInfo((DateTime d) => d.Date);

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);
            
            return Expression.Property(exp.UnNullify(), miDate).Nullify();
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
            return new DateToken(Parent.Clone());
        }
    }
}
