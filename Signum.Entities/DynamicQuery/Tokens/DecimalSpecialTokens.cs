using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Utilities;
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
            return "Floor" + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
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

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>();
        }

        static MethodInfo miFloorDouble = ReflectionTools.GetMethodInfo(() => Math.Floor(0.0));
        static MethodInfo miFloorDecimal= ReflectionTools.GetMethodInfo(() => Math.Floor(0.0m));

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            var call = exp.Type.UnNullify() == typeof(decimal) ?
                Expression.Call(miFloorDecimal, exp.UnNullify()) :
                Expression.Call(miFloorDouble, Expression.Convert(exp, typeof(double)));

            return Expression.Convert(call.Nullify(), typeof(int?));
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
            return "Ceil" + QueryTokenMessage.Of.NiceToString() + Parent.ToString();
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

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>();
        }

        static MethodInfo miCeilingDouble = ReflectionTools.GetMethodInfo(() => Math.Ceiling(0.0));
        static MethodInfo miCeilingDecimal = ReflectionTools.GetMethodInfo(() => Math.Ceiling(0.0m));

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            var call = exp.Type.UnNullify() == typeof(decimal) ? 
                Expression.Call(miCeilingDecimal, exp.UnNullify()) :
                Expression.Call(miCeilingDouble, Expression.Convert(exp, typeof(double))); 

            return Expression.Convert(call.Nullify(), typeof(int?));
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
