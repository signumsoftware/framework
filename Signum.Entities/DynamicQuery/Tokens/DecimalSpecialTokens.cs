using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Reflection;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class StepToken : QueryToken
    {
        public decimal StepSize;

        internal StepToken(QueryToken parent, decimal stepSize)
            : base(parent)
        {
            this.StepSize = stepSize;
        }

        public override string ToString()
        {
            return QueryTokenMessage.Step0.NiceToString(StepSize);
        }

        public override string NiceName()
        {
            return QueryTokenMessage._0Steps1.NiceToString(Parent.ToString(), StepSize);
        }

        public override string Format
        {
            get { return null; }
        }

        public override Type Type
        {
            get { return Parent.Type; }
        }

        public override string Key
        {
            get { return "Step" + StepSize; }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>
            {
                new StepMultiplierToken(this, 1),
                new StepMultiplierToken(this, 2),
                new StepMultiplierToken(this, 5),
            };
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            return RoundingExpressionGenerator.RoundExpression(exp, this.StepSize, RoundingType.RoundMiddle);
        }

        public override string Unit
        {
            get { return this.Parent.Unit; }
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return this.Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {
            return this.Parent.GetImplementations();
        }

        public override string IsAllowed()
        {
            return this.Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new StepToken(this.Parent.Clone(), this.StepSize);
        }
    }

    public class StepMultiplierToken : QueryToken
    {
        public int Multiplier;
    
        public StepMultiplierToken(StepToken parent, int multiplier) : base(parent)
        {

        }


        public override string ToString()
        {
            return  "x" + Multiplier;
        }

        public override string NiceName()
        {
            return QueryTokenMessage._0Steps1.NiceToString(Parent.ToString(), StepSize());
        }

        internal decimal StepSize()
        {
            return ((StepToken)this.Parent).StepSize * Multiplier;
        }

        public override string Format
        {
            get { return null; }
        }

        public override Type Type
        {
            get { return Parent.Type; }
        }

        public override string Key
        {
            get { return "x" + Multiplier; }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            return RoundingExpressionGenerator.RoundExpression(exp, this.StepSize(), RoundingType.RoundMiddle);
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>
            {
                new StepRoundingToken(this, RoundingType.Ceil),
                new StepRoundingToken(this, RoundingType.Floor),
                new StepRoundingToken(this, RoundingType.Round),
                new StepRoundingToken(this, RoundingType.RoundMiddle),
            };
        }

        public override QueryToken Clone()
        {
            return new StepMultiplierToken((StepToken)this.Parent.Clone(), this.Multiplier);
        }

        public override string Unit
        {
            get { return null; }
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return this.Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {
            return this.Parent.GetImplementations();
        }

        public override string IsAllowed()
        {
            return this.Parent.IsAllowed();
        }

    }

    public class StepRoundingToken : QueryToken
    {
        public RoundingType Rounding;

        public StepRoundingToken(StepMultiplierToken parent, RoundingType rounding)
            : base(parent)
        {
            this.Rounding = rounding;
        }

        public override string ToString()
        {
            return Rounding.NiceToString();
        }

        public override string NiceName()
        {
            var num = ((StepMultiplierToken)this.Parent).StepSize();

            string str = Rounding == RoundingType.Ceil ? "⌈{0}⌉" :
                Rounding == RoundingType.Floor ? "⌊{0}⌋" :
                Rounding == RoundingType.Round ? "[{0}]" :
                Rounding == RoundingType.RoundMiddle ? "|{0}|" :
                new InvalidOperationException().Throw<string>();

            return QueryTokenMessage._0Steps1.NiceToString(Parent.ToString(), str);
        }

        public override string Format
        {
            get { return null; }
        }

        public override Type Type
        {
            get { return Parent.Type; }
        }

        public override string Key
        {
            get { return Rounding.ToString(); }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>();
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            return RoundingExpressionGenerator.RoundExpression(exp, ((StepMultiplierToken)this.Parent).StepSize(), this.Rounding);
        }

        public override string Unit
        {
            get { return null; }
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return this.Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {
            return this.Parent.GetImplementations();
        }

        public override string IsAllowed()
        {
            return this.Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new StepRoundingToken((StepMultiplierToken)this.Parent.Clone(), this.Rounding);
        }
    }

    internal static class RoundingExpressionGenerator
    {

        static MethodInfo miFloorDouble = ReflectionTools.GetMethodInfo(() => Math.Floor(0.0));
        static MethodInfo miFloorDecimal = ReflectionTools.GetMethodInfo(() => Math.Floor(0.0m));

        static MethodInfo miCeilingDouble = ReflectionTools.GetMethodInfo(() => Math.Ceiling(0.0));
        static MethodInfo miCeilingDecimal = ReflectionTools.GetMethodInfo(() => Math.Ceiling(0.0m));

        static MethodInfo miRoundDouble = ReflectionTools.GetMethodInfo(() => Math.Round(0.0));
        static MethodInfo miRoundDecimal = ReflectionTools.GetMethodInfo(() => Math.Round(0.0m));

        public static Expression RoundExpression(Expression value, decimal multiplier, RoundingType rounding)
        {
            var result = value;

            result = result.Type.UnNullify() == typeof(decimal) ?
                result.UnNullify() :
                Expression.Convert(result, typeof(double));

            if (rounding == RoundingType.RoundMiddle)
                result = Expression.Subtract(result, Constant(multiplier / 2, result.Type));

            if (multiplier != 1)
                result = Expression.Divide(result, Constant(multiplier, result.Type));

            if (rounding == RoundingType.Ceil)
                result = result.Type.UnNullify() == typeof(decimal) ?
                    Expression.Call(miCeilingDecimal, result.UnNullify()) :
                    Expression.Call(miCeilingDouble, result.TryConvert(typeof(double)));
            else if (rounding == RoundingType.Floor)
                result = result.Type.UnNullify() == typeof(decimal) ?
                    Expression.Call(miFloorDecimal, result.UnNullify()) :
                    Expression.Call(miFloorDouble, result.TryConvert(typeof(double)));
            else if (rounding == RoundingType.Round || rounding == RoundingType.RoundMiddle)
                result = result.Type.UnNullify() == typeof(decimal) ?
                    Expression.Call(miRoundDouble, result.UnNullify()) :
                    Expression.Call(miRoundDecimal, result.TryConvert(typeof(double)));

            if (multiplier != 1)
                result = Expression.Multiply(result, Constant(multiplier, result.Type));

            if (rounding == RoundingType.RoundMiddle)
                result = Expression.Add(result, Constant(multiplier / 2, result.Type));

            return result.TryConvert(value.Type);
        }

        private static ConstantExpression Constant(decimal multiplier, Type type)
        {
            return Expression.Constant(ReflectionTools.ChangeType(multiplier, type), type);
        }
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum RoundingType
    {
        Floor,
        Ceil, 
        Round,
        RoundMiddle,
    }


 
}
