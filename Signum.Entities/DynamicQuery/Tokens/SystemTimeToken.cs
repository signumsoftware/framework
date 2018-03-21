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
    public class SystemTimeToken : QueryToken
    {
        SystemTimeProperty property;
        internal SystemTimeToken(QueryToken parent, SystemTimeProperty property)
            : base(parent)
        {
            Priority = 8;
            this.property = property;
        }

        public override Type Type
        {
            get { return typeof(DateTime); }
        }

        public override string ToString()
        {
            return "[" + this.property.NiceToString() + "]";
        }

        public override string Key
        {
            get { return this.property.ToString(); }
        }
        static MethodInfo miSystemPeriod = ReflectionTools.GetMethodInfo((object o) => SystemTimeExtensions.SystemPeriod(null));

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var result = Parent.BuildExpression(context).ExtractEntity(false);

            var period = Expression.Call(miSystemPeriod, result.UnNullify());

            return Expression.Property(period, property == SystemTimeProperty.SystemValidFrom ? "Min" : "Max");
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return SubTokensBase(typeof(DateTime), options, GetImplementations());
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string Format
        {
            get { return Reflector.FormatString(typeof(DateTime)); }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override string IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return null;
        }

        public override string NiceName()
        {
            return this.property.NiceToString();
        }

        public override QueryToken Clone()
        {
            return new SystemTimeToken(Parent.Clone(), this.property);
        }
    }
}
