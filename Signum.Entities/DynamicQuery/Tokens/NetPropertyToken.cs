using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Properties;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class NetPropertyToken : QueryToken
    {
        public PropertyInfo PropertyInfo { get; private set; }
        public string DisplayName { get; private set; }

        internal NetPropertyToken(QueryToken parent, Expression<Func<object>> pi, string displayName) :
            this(parent, ReflectionTools.GetPropertyInfo(pi), displayName)
        {

        }

        internal NetPropertyToken(QueryToken parent, PropertyInfo pi, string displayName)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            if (pi == null)
                throw new ArgumentNullException("pi");

            if (displayName == null)
                throw new ArgumentNullException("displayName");

            this.DisplayName = displayName;
            this.PropertyInfo = pi;
        }

        public override Type Type
        {
            get { return PropertyInfo.PropertyType.Nullify(); }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public override string Key
        {
            get { return PropertyInfo.Name; }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {   
            var result = Parent.BuildExpression(context);

            return Expression.Property(result.UnNullify(), PropertyInfo).Nullify();
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            return new List<QueryToken>();
        }

        public override string Format
        {
            get { return null; }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override Implementations? GetImplementations()
        {
            return null;
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
            return DisplayName + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
        }

        public override QueryToken Clone()
        {
            return new NetPropertyToken(Parent.Clone(), PropertyInfo, DisplayName);
        }
    }

}
