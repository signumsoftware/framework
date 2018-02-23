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
    public class ExtensionDictionaryToken<K, V> : QueryToken
    {
        public ExtensionDictionaryToken(QueryToken parent, K key,
            string unit, string format, 
            Implementations? implementations,
            PropertyRoute propertyRoute)
            : base(parent)
        {
            var shouldHaveImplementations = typeof(IEntity).IsAssignableFrom(type.CleanType());

            if (shouldHaveImplementations && implementations == null)
                throw new ArgumentException(@"Impossible to determine automatically the implementations for extension token '{0}' (of type {1}) registered on type {2}.  
Consider using dqm.RegisterExpression(({2} e) => e.{0}).ForceImplementations = Implementations.By(typeof({1}));".FormatWith(key, type.TypeName(), parent.Type.CleanType().TypeName()));

            this.keyValue= key;
            this.unit = unit;
            this.format = format;
            this.implementations = implementations;
            this.propertyRoute = propertyRoute;
        }

        public string DisplayName { get; set; }

        public override string ToString()
        {
            return "[" + (((object)keyValue) is Enum e ? e.NiceToString() : Key.ToString()) + "]";
        }

        public override string NiceName()
        {
            return ((object)keyValue) is Enum e ? e.NiceToString() : Key.ToString();
        }

        Type type;
        public override Type Type { get { return type.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.GetPropertyRoute() }); } }

        K keyValue;
        public override string Key => "[" + keyValue.ToString() + "]";

        string format;
        public override string Format => format;

        string unit;
        public override string Unit => unit;

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return base.SubTokensBase(type, options, implementations);  
        }

        public static Func<Type, string, Expression, Expression> BuildExtension;

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            throw new InvalidOperationException();

            if (BuildExtension == null)
                throw new InvalidOperationException("ExtensionToken.BuildExtension not set");

            var parentExpression = Parent.BuildExpression(context).ExtractEntity(false).UnNullify();

            var result = BuildExtension(Parent.Type.CleanType().UnNullify(), Key, parentExpression);

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
            return new ExtensionDictionaryToken<K, V>(this.Parent.Clone(), keyValue, unit, format, implementations, propertyRoute); 
        }
    }
}
