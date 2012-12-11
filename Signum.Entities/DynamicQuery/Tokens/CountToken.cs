using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Properties;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class CountToken : QueryToken
    {
        internal CountToken(QueryToken parent)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

        }

        public override Type Type
        {
            get { return typeof(int?); }
        }

        public override string ToString()
        {
            return Resources.Count;
        }

        public override string Key
        {
            get { return "Count"; }
        }

        static MethodInfo miCount = ReflectionTools.GetMethodInfo((IEnumerable<string> q) => q.Count()).GetGenericMethodDefinition();

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var parentResult = Parent.BuildExpression(context);

            var result = Expression.Call(miCount.MakeGenericMethod(parentResult.Type.ElementType()), parentResult);

            return result.Nullify().BuildLite();
        }

        protected override List<QueryToken> SubTokensInternal()
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
            return null;
        }

        public override string NiceName()
        {
            return ToString() + Resources.Of + Parent.ToString();
        }

        public override QueryToken Clone()
        {
            return new CountToken(Parent.Clone());
        }
    }
}
