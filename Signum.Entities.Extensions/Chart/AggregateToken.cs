using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Chart;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Chart
{
    [Serializable]
    public class AggregateToken : QueryToken
    {
        AggregateFunction function;

        public AggregateToken(QueryToken parent, AggregateFunction function)
            : base(parent)
        {
            if (function == AggregateFunction.Count)
            {
                if (parent != null)
                    throw new ArgumentException("parent should be null for Count function"); 
            }
            else
            {

                if (parent != null)
                    throw new ArgumentNullException("parent");
            }

            this.function = function;
        }

        public override string ToString()
        {
            return function.NiceToString();
        }

        public override string NiceName()
        {
            if(function == AggregateFunction.Count)
                return function.NiceToString();

            return "{0} of {1}".Formato(function.NiceToString(), Parent.ToString());  
        }

        public override string Format
        {
            get
            {
                if (function == AggregateFunction.Count)
                    return null;
                return Parent.Format;
            }
        }

        public override string Unit
        {
            get
            {
                if (function == AggregateFunction.Count)
                    return null;
                return Parent.Unit;
            }
        }

        public override Type Type
        {
            get
            {
                if (function == AggregateFunction.Count)
                    return typeof(int);

                var pType = Parent.Type;

                if (function == AggregateFunction.Average && 
                    (pType.UnNullify() == typeof(int) || 
                     pType.UnNullify() == typeof(long) ||
                     pType.UnNullify() == typeof(bool)))
                {
                    return pType.IsNullable() ? typeof(double) : typeof(double?);
                }

                return pType;
            }
        }

        public override string Key
        {
            get { return function.ToString(); }
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            return new List<QueryToken>();
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            throw new NotImplementedException();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            if (function == AggregateFunction.Count)
                return null;

            return Parent.GetPropertyRoute(); 
        }

        public override Implementations Implementations()
        {
            return null;
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            if (function == AggregateFunction.Count)
                return new AggregateToken(null, AggregateFunction.Count);
            else
                return new AggregateToken(Parent.Clone(), AggregateFunction.Count);
        }
    }
}
