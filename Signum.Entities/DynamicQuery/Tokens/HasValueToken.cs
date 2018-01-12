using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Entities.DynamicQuery
{
    internal static class HasValueTokenExtensions
    {
        internal static List<QueryToken> AndHasValue(this List<QueryToken> list, QueryToken parent)
        {
            list.Add(new HasValueToken(parent));
            return list;
        }

        internal static List<QueryToken> AndModuloTokens(this List<QueryToken> list, QueryToken parent)
        {
            list.AddRange(new List<QueryToken>
            {
                new ModuloToken(parent, 10),
                new ModuloToken(parent, 100),
                new ModuloToken(parent, 1000),
                new ModuloToken(parent, 10000),
            });
            return list;
        }
    }

    [Serializable]
    public class HasValueToken : QueryToken
    {
        internal HasValueToken(QueryToken parent)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            this.Priority = -1;
        }

        public override Type Type
        {
            get { return typeof(bool); }
        }

        public override string ToString()
        {
            return "[" + QueryTokenMessage.HasValue + "]";
        }

        public override string Key
        {
            get { return "HasValue"; }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            Expression baseExpression = Parent.BuildExpression(context);
            
            var result = Expression.NotEqual(baseExpression, Expression.Constant(null, baseExpression.Type.Nullify()));

            if (baseExpression.Type == typeof(string))
                result = Expression.And(result, Expression.NotEqual(baseExpression, Expression.Constant("")));

            return result;
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>();
        }

        public override string Format
        {
            get { return null; }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return null; ;
        }

        public override string NiceName()
        {
            return QueryTokenMessage._0HasValue.NiceToString(Parent.ToString());
        }

        public override QueryToken Clone()
        {
            return new HasValueToken(Parent.Clone());
        }
    }
    
}
