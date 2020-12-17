using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class ExtensionDictionaryToken<T, K, V> : QueryToken
        where K : notnull
    {
        QueryToken parent;
        public override QueryToken? Parent => parent;

        public ExtensionDictionaryToken(QueryToken parent, K key,
            string? unit, 
            string? format,
            Implementations? implementations,
            PropertyRoute? propertyRoute, 
            Expression<Func<T, V>> lambda)
        {
            this.keyValue= key;
            this.unit = unit;
            this.format = format;
            this.implementations = implementations;
            this.propertyRoute = propertyRoute;
            this.Priority = -10;
            this.Lambda = lambda;
            this.parent = parent;
        }
        
        public override string ToString()
        {
            return "[" + (keyValue is Enum e ? e.NiceToString() : keyValue.ToString()) + "]";
        }

        public override string NiceName()
        {
            return keyValue is Enum e ? e.NiceToString() : keyValue.ToString()!;
        }

        public override Type Type { get { return typeof(V).BuildLiteNullifyUnwrapPrimaryKey(new[] { this.GetPropertyRoute()! }); } }

        K keyValue;
        public override string Key => "[" + keyValue.ToString() + "]";

        string? format;
        public override string? Format => format;

        string? unit;
        public override string? Unit => unit;

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return base.SubTokensBase(typeof(V), options, implementations);
        }

        public Expression<Func<T, V>> Lambda;

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var parentExpression = parent.BuildExpression(context).ExtractEntity(false).UnNullify();

            var result = Expression.Invoke(Lambda, parentExpression);

            return result.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.propertyRoute! });
        }

        public PropertyRoute? propertyRoute;
        public override PropertyRoute? GetPropertyRoute() => this.propertyRoute;

        public Implementations? implementations;
        public override Implementations? GetImplementations() => this.implementations;

        public override string? IsAllowed()
        {
            string? parentAllowed = this.parent.IsAllowed();

            string? routeAlllowed = GetPropertyRoute()?.IsAllowed();

            if (parentAllowed.HasText() && routeAlllowed.HasText())
                return QueryTokenMessage.And.NiceToString().Combine(parentAllowed!, routeAlllowed!);

            return parentAllowed ?? routeAlllowed;
        }

        public override QueryToken Clone()
        {
            return new ExtensionDictionaryToken<T, K, V>(this.parent.Clone(), keyValue, unit, format, implementations, propertyRoute, Lambda);
        }
    }
}
