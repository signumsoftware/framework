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

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class BagPropertyToken : QueryToken
    {
        public PropertyInfo PropertyInfo { get; private set; }

        internal BagPropertyToken(QueryToken parent, PropertyInfo pi)
            : base(parent)
        {
            if (pi == null)
                throw new ArgumentNullException("pi");

            this.PropertyInfo = pi;
        }

        public override Type Type
        {
            get { return PropertyInfo.PropertyType.BuildLite().Nullify(); }
        }

        public override string ToString()
        {
            return PropertyInfo.NiceName();
        }

        public override string Key
        {
            get { return PropertyInfo.Name; }
        }

        public override Expression BuildExpression(BuildExpressionContext context)
        {
            var baseExpression = Parent.BuildExpression(context);

            Expression result = Expression.Property(baseExpression, PropertyInfo);

            return result.BuildLite().Nullify();
        }

        protected override List<QueryToken> SubTokensOverride()
        {
            return SubTokensBase(PropertyInfo.PropertyType, GetImplementations());
        }

        public override Implementations? GetImplementations()
        {
            var cleanType = PropertyInfo.PropertyType.CleanType();

            if (!typeof(IIdentifiable).IsAssignableFrom(cleanType))
                return null;

            var fi = Reflector.TryFindFieldInfo(Parent.Type, PropertyInfo);
            if (fi != null)
                return Implementations.FromAttributes(cleanType, fi.GetCustomAttributes(true).Cast<Attribute>().ToArray(), null);

            if (cleanType.IsAbstract)
                throw new InvalidOperationException("Impossible to determine implementations for {0}".Formato(fi.FieldName()));

            return Implementations.By(cleanType);
        }

        public override string Format
        {
            get
            {
                FormatAttribute format = PropertyInfo.SingleAttribute<FormatAttribute>();
                if (format != null)
                    return format.Format;

                return Reflector.FormatString(Type);
            }
        }

        public override string Unit
        {
            get { return PropertyInfo.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName); }
        }

        public override string IsAllowed()
        { 
            return Parent.IsAllowed();
        }
    
        public override string NiceName()
        {
            return PropertyInfo.NiceName() + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
        }

        public override QueryToken Clone()
        {
            return new BagPropertyToken(Parent.Clone(), PropertyInfo);
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return null;
        }
    }

    public interface IQueryTokenBag { }
}
