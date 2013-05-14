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

        object queryName; 
        public override object QueryName
        {
            get { return AggregateFunction == AggregateFunction.Count ? queryName : base.QueryName; }
        }

        public AggregateToken(AggregateFunction function, object queryName)
            : base(null)
        {
            if (function != AggregateFunction.Count)
                throw new ArgumentException("function should be Count for this overload");

            if (queryName == null)
                throw new ArgumentNullException("queryName");

            this.queryName = queryName;
            this.AggregateFunction = function;
        }


        public AggregateToken(AggregateFunction function, QueryToken parent)
            : base(parent)
        {
            if (function == AggregateFunction.Count)
                throw new ArgumentException("function should not different than Count for this overload");

            if (parent == null)
                throw new ArgumentNullException("parent");

            this.AggregateFunction = function;
        }

        public override string ToString()
        {
            return AggregateFunction.NiceToString();
        }

        public override string NiceName()
        {
            if (AggregateFunction == AggregateFunction.Count)
                return AggregateFunction.NiceToString();

            return "{0} of {1}".Formato(AggregateFunction.NiceToString(), Parent.ToString());
        }

        public override string Format
        {
            get
            {
                if (AggregateFunction == AggregateFunction.Count || AggregateFunction == AggregateFunction.Average)
                    return null;
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

                var pType = Parent.Type;
                var pTypeUn = Parent.Type.UnNullify();

                if (AggregateFunction == AggregateFunction.Average &&
                    (pTypeUn == typeof(int) || pTypeUn == typeof(long) || pTypeUn == typeof(bool)))
                {
                    return pType.IsNullable() ? typeof(double?) : typeof(double);
                }

                if (pTypeUn == typeof(bool))
                {
                    return pType.IsNullable() ? typeof(int?) : typeof(int);
                }

                return pType;
            }
        }

        public override string Key
        {
            get { return AggregateFunction.ToString(); }
        }

        protected override List<QueryToken> SubTokensOverride()
        {
            return new List<QueryToken>();
        }

        public override Expression BuildExpression(BuildExpressionContext context)
        {
            throw new InvalidOperationException("AggregateToken does not support this method");
        }

        public override PropertyRoute GetPropertyRoute()
        {
            if (AggregateFunction == AggregateFunction.Count)
                return null;

            return Parent.GetPropertyRoute();
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string IsAllowed()
        {
            if (AggregateFunction == AggregateFunction.Count)
                return null;

            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            if (AggregateFunction == AggregateFunction.Count)
                return new AggregateToken(AggregateFunction.Count, this.queryName);
            else
                return new AggregateToken(AggregateFunction, Parent.Clone());
        }

        internal Type ConvertTo()
        {
            if (AggregateFunction == AggregateFunction.Count)
                return null;

            var pu = Parent.Type.UnNullify();

            if (AggregateFunction == AggregateFunction.Average && (pu == typeof(int) || pu == typeof(long) || pu == typeof(bool)))
                return Parent.Type.IsNullable() ? typeof(double?) : typeof(double);

            if (pu == typeof(bool))
                return Parent.Type.IsNullable() ? typeof(int?) : typeof(int);

            return null;
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
