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
        public MemberInfo MemberInfo { get; private set; }
        public Func<string> DisplayName { get; private set; }

        internal NetPropertyToken(QueryToken parent, Expression<Func<object>> pi, Func<string> displayName) :
            this(parent, ReflectionTools.GetPropertyInfo(pi), displayName)
        {

        }

        internal NetPropertyToken(QueryToken parent, MemberInfo pi, Func<string> displayName)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            if (pi == null)
                throw new ArgumentNullException("pi");

            if (displayName == null)
                throw new ArgumentNullException("displayName");

            this.DisplayName = displayName;
            this.MemberInfo = pi;
        }

        public override Type Type
        {
            get
            {
                return
                    MemberInfo is PropertyInfo pi ? pi.PropertyType.Nullify() :
                    MemberInfo is MethodInfo mi ? mi.ReturnType.Nullify() :
                    throw new UnexpectedValueException(MemberInfo);
            }
        }

        public override string ToString()
        {
            return DisplayName();
        }

        public override string Key
        {
            get { return MemberInfo.Name; }
        }

        public static MethodInfo miInSql = ReflectionTools.GetMethodInfo(() => (1).InSql()).GetGenericMethodDefinition();

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {   
            var result = Parent.BuildExpression(context);

            var prop =
                MemberInfo is PropertyInfo pi ? (Expression)Expression.Property(result.UnNullify(), pi) :
                MemberInfo is MethodInfo mi ? (mi.IsStatic ? Expression.Call(null, mi, result.UnNullify()) : Expression.Call(result.UnNullify(), mi)) :
                throw new UnexpectedValueException(MemberInfo);

            return Expression.Call(miInSql.MakeGenericMethod(prop.Type), prop).Nullify();
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return SubTokensBase(this.Type, options, GetImplementations());
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
            return new NetPropertyToken(Parent.Clone(), MemberInfo, DisplayName);
        }
    }

}
