using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public Func<string> DisplayName { get; private set; }

        internal NetPropertyToken(QueryToken parent, Expression<Func<object>> pi, Func<string> displayName) :
            this(parent, ReflectionTools.GetPropertyInfo(pi), displayName)
        {

        }

        internal NetPropertyToken(QueryToken parent, PropertyInfo pi, Func<string> displayName)
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
            return DisplayName();
        }

        public override string Key
        {
            get { return PropertyInfo.Name; }
        }

        public static MethodInfo miInSql = ReflectionTools.GetMethodInfo(() => (1).InSql()).GetGenericMethodDefinition();

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {   
            var result = Parent.BuildExpression(context);

            var prop = Expression.Property(result.UnNullify(), PropertyInfo);

            return Expression.Call(miInSql.MakeGenericMethod(prop.Type), prop).Nullify();
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
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
            return DisplayName() + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
        }

        public override QueryToken Clone()
        {
            return new NetPropertyToken(Parent.Clone(), PropertyInfo, DisplayName);
        }
    }

}
