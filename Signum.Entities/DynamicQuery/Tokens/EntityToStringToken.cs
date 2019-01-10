using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Utilities;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class EntityToStringToken : QueryToken
    {
        QueryToken parent;
        public override QueryToken? Parent => parent;

        internal EntityToStringToken(QueryToken parent)
        {
            Priority = 9;
            this.parent = parent;
        }

        public override Type Type
        {
            get { return typeof(string); }
        }

        public override string ToString()
        {
            return "[" + LiteMessage.ToStr.NiceToString() + "]";
        }

        public override string Key
        {
            get { return "ToString"; }
        }

        static MethodInfo miToString = ReflectionTools.GetMethodInfo((object o) => o.ToString());
        static PropertyInfo miToStringProperty = ReflectionTools.GetPropertyInfo((Entity o) => o.ToStringProperty);

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var baseExpression = parent.BuildExpression(context);

            return Expression.Call(baseExpression, miToString);
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return SubTokensBase(typeof(string), options, GetImplementations());
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string? Format
        {
            get { return null; }
        }

        public override string? Unit
        {
            get { return null; }
        }

        public override string? IsAllowed()
        {
            return parent.IsAllowed();
        }

        public override PropertyRoute? GetPropertyRoute()
        {
            PropertyRoute? pr = parent.GetPropertyRoute();
            if (pr == null)
            {
                Type? type = Lite.Extract(pr.Type); //Because Parent.Type is always a lite
                if (type != null)
                    return PropertyRoute.Root(type).Add(miToStringProperty);
            }
            else
            {
                Type? type = Lite.Extract(pr.Type); //Because Add doesn't work with lites
                if (type != null)
                    return PropertyRoute.Root(type).Add(miToStringProperty);
            }

            return null;
        }

        public override string NiceName()
        {
            return LiteMessage.ToStr.NiceToString() + QueryTokenMessage.Of.NiceToString() + parent.ToString();
        }

        public override QueryToken Clone()
        {
            return new EntityToStringToken(parent.Clone());
        }
    }
}
