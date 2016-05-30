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

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var baseExpression = Parent.BuildExpression(context);

            Expression result = Expression.Property(baseExpression, PropertyInfo);

            return result.BuildLite().Nullify();
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return SubTokensBase(PropertyInfo.PropertyType, options, GetImplementations());
        }

        public override Implementations? GetImplementations()
        {
            var cleanType = PropertyInfo.PropertyType.CleanType();

            if (!typeof(IEntity).IsAssignableFrom(cleanType))
                return null;

            var fi = Reflector.TryFindFieldInfo(Parent.Type, PropertyInfo);
            if (fi != null)
                return Implementations.FromAttributes(cleanType, null, 
                    fi.GetCustomAttribute<ImplementedByAttribute>(), 
                    fi.GetCustomAttribute<ImplementedByAllAttribute>());

            if (cleanType.IsAbstract)
                throw new InvalidOperationException("Impossible to determine implementations for {0}".FormatWith(PropertyInfo.PropertyName()));

            return Implementations.By(cleanType);
        }

        public override string Format
        {
            get
            {
                FormatAttribute format = PropertyInfo.GetCustomAttribute<FormatAttribute>();
                if (format != null)
                    return format.Format;

                return Reflector.FormatString(Type);
            }
        }

        public override string Unit
        {
            get { return PropertyInfo.GetCustomAttribute<UnitAttribute>()?.UnitName; }
        }

        public override string IsAllowed()
        { 
            return Parent.IsAllowed();
        }
    
        public override string NiceName()
        {
            return PropertyInfo.NiceName();
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
