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
using System.Globalization;

namespace Signum.Entities.DynamicQuery
{
    public class StepToken : QueryToken
    {
        public decimal StepSize;

        internal StepToken(QueryToken parent, decimal stepSize)
            : base(parent)
        {
            this.StepSize = stepSize;
            this.Priority = 1; 
        }

        public override string ToString()
        {
            return QueryTokenMessage.Step0.NiceToString(StepSize);
        }

        public override string NiceName()
        {
            return QueryTokenMessage._0Steps1.NiceToString(Parent.NiceName(), StepSize);
        }

        public override string Format
        {
            get { return Parent.Format; }
        }

        public override Type Type
        {
            get { return Parent.Type.Nullify(); }
        }

        public override string Key
        {
            get { return "Step" + StepSize.ToString(CultureInfo.InvariantCulture).Replace(".", "_"); }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>
            {
                new StepMultiplierToken(this, 1),
                new StepMultiplierToken(this, 1.2m),
                new StepMultiplierToken(this, 1.5m),
                new StepMultiplierToken(this, 2),
                new StepMultiplierToken(this, 2.5m),
                new StepMultiplierToken(this, 3),
                new StepMultiplierToken(this, 4),
                new StepMultiplierToken(this, 5),
                new StepMultiplierToken(this, 6),
                new StepMultiplierToken(this, 8),
            };
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            return RoundingExpressionGenerator.RoundExpression(exp, this.StepSize, RoundingType.Ceil);
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

        public override bool IsGroupable
        {
            get { return true; }
        }
    }

    public class StepMultiplierToken : QueryToken
    {
        public decimal Multiplier;
    
        public StepMultiplierToken(StepToken parent, decimal multiplier) : base(parent)
        {
            this.Multiplier = multiplier;
        }


        public override string ToString()
        {
            return "x" + Multiplier;
        }

        public override string NiceName()
        {
            return QueryTokenMessage._0Steps1.NiceToString(Parent.Parent.NiceName(), StepSize());
        }

        internal decimal StepSize()
        {
            return ((StepToken)this.Parent).StepSize * Multiplier;
        }

        public override string Format
        {
            get { return Parent.Format; }
        }

        public override Type Type
        {
            get { return Parent.Type.Nullify(); }
        }

        public override string Key
        {
            get { return "x" + Multiplier.ToString().Replace(".", "_"); }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.Parent.BuildExpression(context);

            return RoundingExpressionGenerator.RoundExpression(exp, this.StepSize(), RoundingType.Ceil);
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

        public override bool IsGroupable
        {
            get { return true; }
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
                throw new InvalidOperationException();

            return QueryTokenMessage._0Steps1.NiceToString(Parent.Parent.Parent.NiceName(), str.FormatWith(num));
        }

        public override string Format
        {
            get { return Parent.Format; }
        }

        public override Type Type
        {
            get { return Parent.Type.Nullify(); }
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
            var exp = Parent.Parent.Parent.BuildExpression(context);

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

        public override bool IsGroupable
        {
            get { return true; }
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
                    Expression.Call(miRoundDecimal, result.UnNullify()) :
                    Expression.Call(miRoundDouble, result.TryConvert(typeof(double)));

            if (multiplier != 1)
                result = Expression.Multiply(result, Constant(multiplier, result.Type));

            if (rounding == RoundingType.RoundMiddle)
                result = Expression.Add(result, Constant(multiplier / 2, result.Type));

            return result.Nullify().TryConvert(value.Type.Nullify());
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

    public class ModuloToken : QueryToken
    {
        public int Divisor;

        internal ModuloToken(QueryToken parent, int divisor)
            : base(parent)
        {
            this.Divisor = divisor;
        }

        public override string ToString()
        {
            return QueryTokenMessage.Modulo0.NiceToString(Divisor);
        }

        public override string NiceName()
        {
            return QueryTokenMessage._0Mod1.NiceToString(Parent.NiceName(), Divisor);
        }

        public override string Format
        {
            get { return null; }
        }

        public override Type Type
        {
            get { return typeof(int).Nullify(); }
        }

        public override string Key
        {
            get { return "Mod" + Divisor.ToString(CultureInfo.InvariantCulture).Replace(".", "_"); }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>
            {
            };
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);
            return Expression.Modulo(Expression.Convert(exp, typeof(int)), Expression.Constant(Divisor)).Nullify();
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
            return new ModuloToken(this.Parent.Clone(), this.Divisor);
        }

        public override bool IsGroupable
        {
            get { return true; }
        }
    }
}
