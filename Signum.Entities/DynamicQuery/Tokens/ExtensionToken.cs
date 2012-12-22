using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Properties;
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
            if (typeof(IIdentifiable).IsAssignableFrom(type.CleanType()) && implementations == null)
                throw new ArgumentException("Extension {0} of type {1} has no implementations".Formato(key, type.Name));

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
            return DisplayName + Resources.Of + Parent.ToString();
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

        protected override List<QueryToken> SubTokensInternal()
        {
            return base.SubTokensBase(type, implementations);  
        }

        public static Func<Type, string, Expression, Expression> BuildExtension;

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
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

        public Implementations? GetElementImplementations()
        {
            return isProjection ? implementations : null; 
        }

        string isAllowed; 
        public override string IsAllowed()
        {
            string parent = Parent.IsAllowed();

            if (isAllowed.HasText() && parent.HasText())
                return Resources.And.Combine(isAllowed, parent);

            return isAllowed ?? parent;
        }

        public override QueryToken Clone()
        {
            return new ExtensionToken(this.Parent.Clone(), key, type, isProjection, unit, format, implementations, isAllowed, propertyRoute); 
        }
    }
}
