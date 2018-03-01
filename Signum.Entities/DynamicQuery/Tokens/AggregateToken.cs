using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Signum.Entities.Reflection;
using Signum.Utilities;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class AggregateToken : QueryToken
    {
        public AggregateFunction AggregateFunction { get; private set; }
        public object CountIsValue { get; private set; }
        object queryName; 
        public override object QueryName
        {
            get { return  queryName ?? base.QueryName; }
        }

        public AggregateToken(AggregateFunction function, object queryName)
            : base(null)
        {
            if (function != AggregateFunction.Count)
                throw new ArgumentException("function should be Count for this overload");

            this.queryName = queryName ?? throw new ArgumentNullException("queryName");
            this.AggregateFunction = function;
        }

        public static readonly object AnyValue = new object();

        public AggregateToken(AggregateFunction function, QueryToken parent, object countIsValue = null)
            : base(parent)
        {
            if (countIsValue != null && function != AggregateFunction.Count)
                throw new ArgumentException("CountIsValue should only be set for Count");

            if (parent == null)
                throw new ArgumentNullException("parent");

            this.CountIsValue = countIsValue;
            this.AggregateFunction = function;
        }

        public override string ToString()
        {
            string suffix = GetNiceSuffix();

            return AggregateFunction.NiceToString() + (suffix == null ? null : " " + suffix);
        }

        public override string NiceName()
        {
            if (AggregateFunction == AggregateFunction.Count && Parent == null)
                return AggregateFunction.NiceToString();
            
            string suffix = GetNiceSuffix();

            return $"{AggregateFunction.NiceToString()}{(suffix == null ? null : " " + suffix)} of {Parent}";
        }

        private string GetNiceSuffix()
        {
            if (this.AggregateFunction != AggregateFunction.Count)
                return null;

            if (this.Parent == null)
                return null;

            return CountIsValue == AnyValue ? null :
               CountIsValue == null ? QueryTokenMessage.Null.NiceToString() :
               CountIsValue is Enum e ? e.NiceToString() : 
               CountIsValue.ToString();
        }

        public override string Format
        {
            get
            {
                if (AggregateFunction == AggregateFunction.Count)
                    return null;

                if (AggregateFunction == AggregateFunction.Average && Parent.Format == "D")
                    return "N2";

                return Parent.Format;
            }
        }

        public override string Unit
        {
            get
            {
                if (AggregateFunction == AggregateFunction.Count)
                    return null;

                return Parent.Unit;
            }
        }

        public override Type Type
        {
            get
            {
                if (AggregateFunction == AggregateFunction.Count)
                    return typeof(int);

                var pu = Parent.Type.UnNullify();

                if (AggregateFunction == AggregateFunction.Average && (pu != typeof(float) || pu != typeof(double) || pu == typeof(decimal)))
                    return Parent.Type.IsNullable() ? typeof(double?) : typeof(double);

                if (pu == typeof(bool) ||
                    pu == typeof(byte) || pu == typeof(sbyte) ||
                    pu == typeof(short) || pu == typeof(ushort) ||
                    pu == typeof(uint) ||
                    pu == typeof(ulong))
                    return Parent.Type.IsNullable() ? typeof(int?) : typeof(int);

                return Parent.Type;
            }
        }

        public override string Key
        {
            get
            {
                return AggregateFunction.ToString() +
                  (
                  this.AggregateFunction != AggregateFunction.Count || this.Parent == null ? null :
                  this.CountIsValue == null ? "Null" :
                  this.CountIsValue.ToString()
                  );
            }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return new List<QueryToken>();
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            throw new InvalidOperationException("AggregateToken should have a replacement at this stage");
        }

        public override PropertyRoute GetPropertyRoute()
        {
            if (Parent == null)
                return null;

            return Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string IsAllowed()
        {
            if (Parent == null)
                return null;

            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            if (Parent == null)
                return new AggregateToken(AggregateFunction, this.queryName);
            else
                return new AggregateToken(AggregateFunction, Parent.Clone(), this.CountIsValue);
        }
        

        public override string TypeColor
        {
            get { return "#0000FF"; }
        }
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum AggregateFunction
    {
        Count,
        Average,
        Sum,
        Min,
        Max,
    }
}
