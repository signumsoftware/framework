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
    public class ExtensionToken : QueryToken
    {
        public ExtensionToken(QueryToken parent, string key, Type type, bool isProjection,
            string unit, string format, 
            Implementations? implementations,
            string isAllowed, PropertyRoute propertyRoute)
            : base(parent)
        {
            var shouldHaveImplementations = typeof(IIdentifiable).IsAssignableFrom((isProjection ? type.ElementType() : type).CleanType());

            if (shouldHaveImplementations && implementations == null)
                throw new ArgumentException("Extension {0} ({1}) registered on type {2} has no implementations".Formato(key, type.TypeName(), parent.Type.CleanType()));

            this.key= key;
            this.type = type;
            this.isProjection = isProjection;
            this.unit = unit;
            this.format = format;
            this.implementations = implementations;
            this.isAllowed = isAllowed;
            this.propertyRoute = propertyRoute;
        }

        public string DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }

        public override string NiceName()
        {
            return DisplayName + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
        }

        Type type;
        public override Type Type { get { return type.BuildLite().Nullify(); } }

        string key;
        public override string Key { get { return key; } }

        bool isProjection;
        public bool IsProjection { get { return isProjection; } }

        string format;
        public override string Format { get { return isProjection ? null : format; } }
        public string ElementFormat { get { return isProjection? format: null; } }

        string unit;
        public override string Unit { get { return isProjection? null: unit; } }
        public string ElementUnit { get { return isProjection?  unit: null; } }

        protected override List<QueryToken> SubTokensOverride()
        {
            return base.SubTokensBase(type, implementations);  
        }

        public static Func<Type, string, Expression, Expression> BuildExtension;

        public override Expression BuildExpression(BuildExpressionContext context)
        {
            if (BuildExtension == null)
                throw new InvalidOperationException("ExtensionToken.BuildExtension not set");

            var parentExpression = Parent.BuildExpression(context).ExtractEntity(false).UnNullify();

            var result = BuildExtension(Parent.Type.CleanType().UnNullify(), Key, parentExpression);

            return result.BuildLite().Nullify();
        }

        public PropertyRoute propertyRoute;
        public override PropertyRoute GetPropertyRoute()
        {
            return isProjection ? null : propertyRoute;
        }

        public PropertyRoute GetElementPropertyRoute()
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

        string isAllowed; 
        public override string IsAllowed()
        {
            string parent = Parent.IsAllowed();

            if (isAllowed.HasText() && parent.HasText())
                return QueryTokenMessage.And.NiceToString().Combine(isAllowed, parent);

            return isAllowed ?? parent;
        }

        public override QueryToken Clone()
        {
            return new ExtensionToken(this.Parent.Clone(), key, type, isProjection, unit, format, implementations, isAllowed, propertyRoute); 
        }
    }
}
