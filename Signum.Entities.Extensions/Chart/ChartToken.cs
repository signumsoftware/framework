using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.ComponentModel;
using Signum.Entities.Reports;
using Signum.Entities.Extensions.Properties;

namespace Signum.Entities.Chart
{
    public enum AggregateFunction
    {
        Count,
        Average,
        Sum,
        Min,
        Max,
    }

    [Serializable]
    public class ChartTokenDN : QueryTokenDN
    {
        static ChartTokenDN()
        {

        }

        public ChartTokenDN()
        {

        }

        protected override void TokenChanged()
        {
            NotifyChange(true);

            if (Token != null)
            {
                if (Token is IntervalQueryToken)
                    ((IntervalQueryToken)Token).PropertyChanged += (s, e) => NotifyChange(true);

                Format = Token.Format;
                Unit = Token.Unit;
                DisplayName = Aggregate == AggregateFunction.Count ? "Count" : Token.NiceName();
            }
            else
            {
                Format = null;
                Unit = null;
                DisplayName = null;
            }

            Notify(() => Aggregate);   
        }

        AggregateFunction? aggregate;
        public AggregateFunction? Aggregate
        {
            get { return aggregate; }
            set
            {
                if (Set(ref aggregate, value, () => Aggregate))
                {
                    if (aggregate == AggregateFunction.Count)
                        Token = new CountAllToken();
                    else if (Token is CountAllToken)
                        Token = null; 

                    NotifyChange(true);

                    Notify(() => Token);
                }
            }
        }

        public Type Type
        {

            get
            {
                if (Token == null)
                    return null;

                if (aggregate == AggregateFunction.Average && QueryUtils.GetFilterType(Token.Type) == FilterType.Number)
                    return Token.Type.IsNullable() ? typeof(double?) : typeof(double);

                return Token.Type;
            }
        }

        string format;
        public string Format
        {
            get { return format; }
            set { if (Set(ref format, value, () => Format))NotifyChange(false); }
        }

        string unit;
        public string Unit
        {
            get { return unit; }
            set { if (Set(ref unit, value, () => Unit))NotifyChange(false); }
        }

        string displayName;
        public string DisplayName
        {
            get { return displayName; }
            set { if (Set(ref displayName, value, () => DisplayName)) NotifyChange(false); }
        }

        OrderType? orderType;
        public OrderType? OrderType
        {
            get { return orderType; }
            set { if (Set(ref orderType, value, () => OrderType))Notify(() => OrderPriority); }
        }

        int? orderPriority;
        public int? OrderPriority
        {
            get { return orderPriority; }
            set { if (Set(ref orderPriority, value, () => OrderPriority))Notify(() => OrderType); }
        }

        [field: NonSerialized, Ignore]
        internal event Func<ChartTokenDN, bool> GroupByVisibleEvent;
        public bool GroupByVisible { get { return GroupByVisibleEvent(this); } }

        [field: NonSerialized, Ignore]
        internal event Func<ChartTokenDN, bool> ShouldAggregateEvent;
        public bool ShouldAggregate { get { return ShouldAggregateEvent(this); } }

        [field: NonSerialized, Ignore]
        internal event Func<ChartTokenDN, string> PropertyLabeleEvent;
        public string PropertyLabel { get { return PropertyLabeleEvent(this); } }

        [field: NonSerialized, Ignore]
        public event Action<bool> ChartRequestChanged;

        public void NotifyChange(bool needNewQuery)
        {
            if (ChartRequestChanged != null)
                ChartRequestChanged(needNewQuery);
        }

        internal void NotifyExternal<T>(Expression<Func<T>> property)
        {
            Notify(property);
        }

        internal void NotifyAll()
        {
            Notify(() => Token);
            Notify(() => GroupByVisible);
            Notify(() => PropertyLabel);
            Notify(() => ShouldAggregate);
            Notify(() => Aggregate);
        }

        internal void NotifyGroup()
        {
            Notify(() => GroupByVisible);
            Notify(() => ShouldAggregate);
        }

        public override string ToString()
        {
            return " ".Combine(Aggregate, Token);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if(pi.Is(()=>Token) && Token is IDataErrorInfo)
            {
                return ((IDataErrorInfo)Token).Error;
            }

            if (pi.Is(() => OrderPriority) || pi.Is(() => OrderType))
            {
                if ((OrderPriority == null) != (OrderType == null))
                    return "Order properties mismatch";
            }

            return base.PropertyValidation(pi);
        }

        public string GetTitle()
        {
            return DisplayName + (Unit.HasText() ? " ({0})".Formato(Unit) : null);
        }

        protected override void PreSaving(ref bool graphModified)
        {
            tokenString = token == null ? null : token.FullKey();
        }

        public override void PostRetrieving(QueryDescription queryDescription)
        {
            Token = tokenString.HasText() ? QueryUtils.Parse(tokenString, token => SubTokensChart(token, queryDescription.Columns)) : null;
            CleanSelfModified();
        }

        static readonly QueryToken[] Empty = new QueryToken[0];

        public static QueryToken[] SubTokensChart(QueryToken token, IEnumerable<ColumnDescription> columnDescriptions)
        {
            var result = QueryUtils.SubTokens(token, columnDescriptions);

            if (token != null)
            {
                FilterType? ft = QueryUtils.TryGetFilterType(token.Type);

                if (ft == FilterType.Number || ft == FilterType.DecimalNumber)
                {
                    return (result ?? Empty).And(new IntervalQueryToken(token)).ToArray();
                }
            }

            return result;       
        } 

        internal Column CreateSimpleColumn()
        {
            return new Column(Token, DisplayName); 
        }


       

    }
}
