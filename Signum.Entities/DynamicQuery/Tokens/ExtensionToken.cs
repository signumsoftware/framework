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
        public ExtensionToken(QueryToken parent, string key, Type type, string unit, string format, Implementations implementations, bool isAllowed, PropertyRoute propertyRoute): base(parent)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(parent.Type.CleanType()))
                throw new InvalidOperationException("Extensions only allowed over {0}".Formato(typeof(IIdentifiable).Name)); 

            this.key= key;
            this.type = type;
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

        string format;
        public override string Format { get { return format; } }

        string unit;
        public override string Unit { get { return unit; } }

        Type type;
        public override Type Type { get { return BuildLite(type).Nullify(); } }

        string key;
        public override string Key { get { return key; } }

        protected override List<QueryToken> SubTokensInternal()
        {
            return base.SubTokensBase(type, implementations);  
        }

        public static Func<Type, string, Expression, Expression> BuildExtension;

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            if (BuildExtension == null)
                throw new InvalidOperationException("ExtensionToken.BuildExtension not set");

            var result = BuildExtension(Parent.Type.CleanType(), Key, ExtractEntity(Parent.BuildExpression(context), false));

            return BuildLite(result).Nullify();
        }

        public PropertyRoute propertyRoute;
        public override PropertyRoute GetPropertyRoute()
        {
            return propertyRoute;
        }

        public Implementations implementations;
        public override Implementations Implementations()
        {
            return implementations;
        }

        bool isAllowed; 
        public override bool IsAllowed()
        {
            return isAllowed && Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new ExtensionToken(this.Parent.Clone(), key, type, unit, format, implementations, isAllowed, propertyRoute); 
        }
    }
}
