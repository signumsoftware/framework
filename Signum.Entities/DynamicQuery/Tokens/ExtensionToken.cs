using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class ExtensionToken : QueryToken
    {
        QueryToken parent;
        public override QueryToken? Parent => parent;


        public ExtensionToken(QueryToken parent, string key, Type type, bool isProjection,
            string? unit, string? format, Implementations? implementations,
            string? isAllowed, PropertyRoute? propertyRoute, string displayName)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));

            var shouldHaveImplementations = typeof(IEntity).IsAssignableFrom((isProjection ? type.ElementType()! : type).CleanType());

            if (shouldHaveImplementations && implementations == null)
                throw new ArgumentException(@"Impossible to determine automatically the implementations for extension token '{0}' (of type {1}) registered on type {2}.
Consider using QueryLogic.Expressions.Register(({2} e) => e.{0}).ForceImplementations = Implementations.By(typeof({1}));".FormatWith(key, type.TypeName(), parent.Type.CleanType().TypeName()));

            this.key= key;
            this.type = type;
            this.isProjection = isProjection;
            this.unit = unit;
            this.format = format;
            this.implementations = implementations;
            this.isAllowed = isAllowed;
            this.propertyRoute = propertyRoute;
            this.DisplayName = displayName;
        }

        public string DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }

        public override string NiceName()
        {
            return DisplayName;
        }

        Type type;
        public override Type Type { get { return type.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.GetPropertyRoute()! }); } }

        string key;
        public override string Key { get { return key; } }

        bool isProjection;
        public bool IsProjection { get { return isProjection; } }

        string? format;
        public override string? Format { get { return isProjection ? null : format; } }
        public string? ElementFormat { get { return isProjection? format: null; } }

        string? unit;
        public override string? Unit { get { return isProjection ? null: unit; } }
        public string? ElementUnit { get { return isProjection ? unit : null; } }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return base.SubTokensBase(type, options, implementations);
        }

        public static Func<Type, string, Expression, Expression>? BuildExtension;

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            if (BuildExtension == null)
                throw new InvalidOperationException("ExtensionToken.BuildExtension not set");

            var parentExpression = parent.BuildExpression(context);

            var result = BuildExtension(parent.Type, Key, parentExpression);

            return result.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.propertyRoute! });
        }

        public PropertyRoute? propertyRoute;
        public override PropertyRoute? GetPropertyRoute()
        {
            return isProjection ? null : propertyRoute;
        }

        public PropertyRoute? GetElementPropertyRoute()
        {
            return isProjection ? propertyRoute : null;
        }

        public Implementations? implementations;
        public override Implementations? GetImplementations()
        {
            return isProjection ? null : implementations;
        }

        protected internal override Implementations? GetElementImplementations()
        {
            return isProjection ? implementations : null;
        }

        string? isAllowed;
        public override string? IsAllowed()
        {
            string? parentAllowed = this.parent.IsAllowed();

            if (isAllowed.HasText() && parentAllowed.HasText())
                return QueryTokenMessage.And.NiceToString().Combine(isAllowed!, parentAllowed!);

            return isAllowed ?? parentAllowed;
        }

        public override QueryToken Clone()
        {
            return new ExtensionToken(this.parent.Clone(), key, type, isProjection, unit, format, implementations, isAllowed, propertyRoute, DisplayName);
        }
    }
}
