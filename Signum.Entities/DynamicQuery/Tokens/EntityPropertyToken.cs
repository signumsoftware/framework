using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Properties;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class EntityPropertyToken : QueryToken
    {
        public PropertyInfo PropertyInfo { get; private set; }
       
        public static QueryToken IdProperty(QueryToken parent)
        {
            return new EntityPropertyToken(parent, ReflectionTools.GetPropertyInfo((IdentifiableEntity e) => e.Id));
        }

        internal EntityPropertyToken(QueryToken parent, PropertyInfo pi)
            : base(parent)
        {
            if (pi == null)
                throw new ArgumentNullException("pi");

            this.PropertyInfo = pi;
        }

        public override Type Type
        {
            get { return BuildLite(PropertyInfo.PropertyType).Nullify(); }
        }

        public override string ToString()
        {
            return PropertyInfo.NiceName();
        }

        public override string Key
        {
            get { return PropertyInfo.Name; }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var baseExpression = Parent.BuildExpression(context);

            if (PropertyInfo.Is((IdentifiableEntity ident) => ident.Id) ||
                PropertyInfo.Is((IdentifiableEntity ident) => ident.ToStringProperty))
            {
                baseExpression = ExtractEntity(baseExpression, true);

                return Expression.Property(baseExpression, PropertyInfo.Name).Nullify(); // Late binding over Lite or Identifiable
            }

            baseExpression = ExtractEntity(baseExpression, false);

            Expression result = Expression.Property(baseExpression, PropertyInfo);
            
            return BuildLite(result).Nullify();
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            if (PropertyInfo.PropertyType.UnNullify() == typeof(DateTime))
            {
                PropertyRoute route = this.GetPropertyRoute();

                if (route != null)
                {
                    var att = Validator.GetOrCreatePropertyPack(route.Parent.Type, route.PropertyInfo.Name).TryCC(pp =>
                        pp.Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefaultEx());
                    if (att != null)
                    {
                        return DateTimeProperties(this, att.Precision);
                    }
                }
            }

            return SubTokensBase(PropertyInfo.PropertyType, Implementations());
        }

        public override Implementations Implementations()
        {
            return GetPropertyRoute().GetImplementations();
        }

        public override string Format
        {
            get { return Reflector.FormatString(this.GetPropertyRoute()); }
        }

        public override string Unit
        {
            get { return PropertyInfo.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName); }
        }

        public override bool IsAllowed()
        {
            PropertyRoute route = GetPropertyRoute();

            return Parent.IsAllowed() && (route == null || route.IsAllowed());
        }

        public override PropertyRoute GetPropertyRoute()
        {
            PropertyRoute parent = Parent.GetPropertyRoute();
            if (parent == null)
            {
                Type type = Reflector.ExtractLite(Parent.Type); //Because Parent.Type is always a lite
                if (type != null)
                    return PropertyRoute.Root(type).Add(PropertyInfo);

                return null;
            }
            else
            {
                Type type = Reflector.ExtractLite(parent.Type); //Because Add doesn't work with lites
                if (type != null)
                    return PropertyRoute.Root(type).Add(PropertyInfo);

                return parent.Add(PropertyInfo);
            }
        }

        public override string NiceName()
        {
            return PropertyInfo.NiceName() + Resources.Of + Parent.ToString();
        }

        public override QueryToken Clone()
        {
            return new EntityPropertyToken(Parent.Clone(), PropertyInfo);
        }
    }
}
