using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class ExtensionDictionaryToken<T, K, V> : QueryToken
    {
        public ExtensionDictionaryToken(QueryToken parent, K key,
            string unit, string format, 
            Implementations? implementations,
            PropertyRoute propertyRoute)
            : base(parent)
        {
            this.keyValue= key;
            this.unit = unit;
            this.format = format;
            this.implementations = implementations;
            this.propertyRoute = propertyRoute;
            this.Priority = -10;
        }

        public string DisplayName { get; set; }

        public override string ToString()
        {
            return "[" + (((object)keyValue) is Enum e ? e.NiceToString() : keyValue.ToString()) + "]";
        }

        public override string NiceName()
        {
            return ((object)keyValue) is Enum e ? e.NiceToString() : keyValue.ToString();
        }
        
        public override Type Type { get { return typeof(V).BuildLiteNullifyUnwrapPrimaryKey(new[] { this.GetPropertyRoute() }); } }

        K keyValue;
        public override string Key => "[" + keyValue.ToString() + "]";

        string format;
        public override string Format => format;

        string unit;
        public override string Unit => unit;

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return base.SubTokensBase(typeof(V), options, implementations);  
        }

        public Expression<Func<T, V>> Lambda;

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var liteParent = Parent.Follow(a => a.Parent).FirstEx(p => p.Type.IsLite());

            var parentExpression = liteParent.BuildExpression(context).ExtractEntity(false).UnNullify();

            var result = Expression.Invoke(Lambda, parentExpression);

            return result.BuildLiteNulifyUnwrapPrimaryKey(new[] { this.propertyRoute });
        }

        public PropertyRoute propertyRoute;
        public override PropertyRoute GetPropertyRoute() => this.propertyRoute;

        public Implementations? implementations;
        public override Implementations? GetImplementations() => this.implementations;

        public override string IsAllowed()
        {
            PropertyRoute pr = GetPropertyRoute();

            string parent = Parent.IsAllowed();

            string route = pr?.IsAllowed();

            if (parent.HasText() && route.HasText())
                return QueryTokenMessage.And.NiceToString().Combine(parent, route);

            return parent ?? route;
        }

        public override QueryToken Clone()
        {
            return new ExtensionDictionaryToken<T, K, V>(this.Parent.Clone(), keyValue, unit, format, implementations, propertyRoute)
            {
                Lambda = Lambda
            }; 
        }
    }
}
