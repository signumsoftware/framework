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
        public object Value { get; private set; }
        public FilterOperation? FilterOperation { get; private set; }
        public bool Distinct { get; private set; }


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
        
        public AggregateToken(AggregateFunction function, QueryToken parent, FilterOperation? filterOperation = null, object value = null, bool distinct = false)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            this.AggregateFunction = function;

            if (function == AggregateFunction.Count)
            {
                if (distinct == false && filterOperation == null)
                    throw new ArgumentException("Either distinct or filterOperation should be set");

                else if (distinct == true && this.FilterOperation.HasValue)
                    throw new ArgumentException("distinct and filterOperation are incompatibles");

                this.Value = value;
                this.FilterOperation = filterOperation;
                this.Distinct = distinct;

            }
            else
            {
                if (distinct == true || this.FilterOperation.HasValue)
                    throw new ArgumentException("distinct and filterOperation are incompatibles");
            }
        }

        public override string ToString()
        {
            string suffix = GetNiceOperation();

            return " ".CombineIfNotEmpty(AggregateFunction.NiceToString(), this.GeNiceDistinct(), this.GetNiceOperation(), this.GetNiceValue());
        }

        public override string NiceName()
        {
            if (AggregateFunction == AggregateFunction.Count && Parent == null)
                return AggregateFunction.NiceToString();

            return " ".CombineIfNotEmpty(AggregateFunction.NiceToString(), this.GeNiceDistinct(), this.GetNiceOperation(), this.GetNiceValue(), "of", Parent);
        }

        string GetNiceOperation()
        {
            return this.FilterOperation == null || this.FilterOperation == DynamicQuery.FilterOperation.EqualTo ? null :
                this.FilterOperation == DynamicQuery.FilterOperation.DistinctTo ? QueryTokenMessage.Not.NiceToString() :
                this.FilterOperation.NiceToString();
        }

        string GetNiceValue()
        {
            return this.FilterOperation == null ? null :
               Value == null ? QueryTokenMessage.Null.NiceToString() :
               Value is Enum e ? e.NiceToString() :
               Value.ToString();
        }

        string GeNiceDistinct()
        {
            return this.Distinct ? QueryTokenMessage.Distinct.NiceToString() : null;
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
                var distinct = this.Distinct ? "Distinct" : null;

                var op =
                    this.FilterOperation == null ? null :
                    this.FilterOperation == DynamicQuery.FilterOperation.EqualTo ? "" :
                    this.FilterOperation == DynamicQuery.FilterOperation.DistinctTo ? "Not" :
                    this.FilterOperation.Value.ToString();

                var value =
                    this.FilterOperation == null ? null :
                    this.Value == null ? "Null" :
                    this.Value.ToString();

                return AggregateFunction.ToString() + distinct + op + value;
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
                return new AggregateToken(AggregateFunction, Parent.Clone(), this.FilterOperation, this.Value, this.Distinct);
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
