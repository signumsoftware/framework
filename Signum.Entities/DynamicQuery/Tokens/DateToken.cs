using System;
using System.Collections.Generic;
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
        QueryToken parent;
        public override QueryToken? Parent => parent;
        
        internal DateToken(QueryToken parent)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public override string ToString()
        {
            return QueryTokenMessage.Date.NiceToString();
        }

        public override string NiceName()
        {
            return QueryTokenMessage.Date.NiceToString() + QueryTokenMessage.Of.NiceToString() + parent.ToString();
        }

        public override string? Format
        {
            get { return "d"; }
        }

        public override string? Unit
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
            var exp = parent.BuildExpression(context);

            return Expression.Property(exp.UnNullify(), miDate).Nullify();
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
            return new DateToken(parent.Clone());
        }

        public override bool IsGroupable
        {
            get { return true; }
        }
    }
}
