using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Entities.Properties;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class FloorToken : QueryToken
    {
        internal FloorToken(QueryToken parent)
            : base(parent)
        {
        }

        public override string ToString()
        {
            return "Floor";
        }

        public override string NiceName()
        {
            return "Floor" + Resources.Of + Parent.ToString();
        }

        public override string Format
        {
            get { return Parent.Format; }
        }

        public override string Unit
        {
            get { return Parent.Unit; }
        }

        public override Type Type
        {
            get { return typeof(int?); }
        }

        public override string Key
        {
            get { return "Floor"; }
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            return new List<QueryToken>();
        }

        static MethodInfo miFloorDouble = ReflectionTools.GetMethodInfo(() => Math.Floor(0.0));
        static MethodInfo miFloorDecimal= ReflectionTools.GetMethodInfo(() => Math.Floor(0.0m));

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            if (exp.Type.UnNullify() == typeof(decimal))
                return Expression.Convert(Expression.Call(miFloorDecimal, exp.UnNullify()), typeof(int?));
            else
                return Expression.Convert(Expression.Call(miFloorDecimal, Expression.Convert(exp, typeof(double))), typeof(int?));
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {  
            return null;
        }

        public override string IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new FloorToken(Parent.Clone());
        }
    }

    [Serializable]
    public class CeilToken : QueryToken
    {
        internal CeilToken(QueryToken parent)
            : base(parent)
        {
        }

        public override string ToString()
        {
            return "Ceil";
        }

        public override string NiceName()
        {
            return "Ceil" + Resources.Of + Parent.ToString();
        }

        public override string Format
        {
            get { return Parent.Format; }
        }

        public override string Unit
        {
            get { return Parent.Unit; }
        }

        public override Type Type
        {
            get { return typeof(int?); }
        }

        public override string Key
        {
            get { return "Ceil"; }
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            return new List<QueryToken>();
        }

        static MethodInfo miFloorDouble = ReflectionTools.GetMethodInfo(() => Math.Floor(0.0));
        static MethodInfo miFloorDecimal = ReflectionTools.GetMethodInfo(() => Math.Floor(0.0m));

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            if (exp.Type.UnNullify() == typeof(decimal))
                return Expression.Convert(Expression.Call(miFloorDecimal, exp.UnNullify()), typeof(int?));
            else
                return Expression.Convert(Expression.Call(miFloorDecimal, Expression.Convert(exp, typeof(double))), typeof(int?));
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new CeilToken(Parent.Clone());
        }
    }
}
