using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities;

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
            return QueryTokenMessage.Date.NiceToString();
        }

        public override string NiceName()
        {
            return QueryTokenMessage.Date.NiceToString() + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
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

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
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

        public override bool IsGroupable
        {
            get { return true; }
        }
    }
}
