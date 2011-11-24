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
using Signum.Entities.UserQueries;

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
        public ChartTokenDN()
        {

        }

        public override void TokenChanged()
        {
            NotifyChange(true);

            if (Token != null)
            {
                if (Token is IntervalQueryToken)
                    ((IntervalQueryToken)Token).PropertyChanged += (s, e) => NotifyChange(true);

                Format = Token.Format;
                Unit = Token.Unit;
                DisplayName = Token.NiceName();
            }
            else
            {
                Format = null;
                Unit = null;
                DisplayName = null;
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


        [field: NonSerialized, Ignore]
        internal event Func<ChartTokenDN, bool> GroupByVisibleEvent;
        [AvoidLocalization]
        public bool GroupByVisible { get { return GroupByVisibleEvent(this); } }

        [field: NonSerialized, Ignore]
        internal event Func<ChartTokenDN, bool> GroupingEvent;
        [AvoidLocalization]
        public bool Grouping { get { return GroupingEvent(this); } }

        [field: NonSerialized, Ignore]
        internal event Func<ChartTokenDN, bool> ShouldAggregateEvent;
        [AvoidLocalization]
        public bool ShouldAggregate { get { return ShouldAggregateEvent(this); } }

        [field: NonSerialized, Ignore]
        internal event Func<ChartTokenDN, string> PropertyLabeleEvent;
        [AvoidLocalization]
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
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if(pi.Is(()=>Token) && Token is IDataErrorInfo)
            {
                return ((IDataErrorInfo)Token).Error;
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

        public override void ParseData(QueryDescription queryDescription)
        {
            Token = QueryUtils.Parse(tokenString, token => SubTokensChart(token, queryDescription.Columns));

            CleanSelfModified();
        }

        public List<QueryToken> SubTokensChart(QueryToken token, IEnumerable<ColumnDescription> columnDescriptions)
        {
            var result = QueryUtils.SubTokens(token, columnDescriptions);

            if (this.Grouping)
            {
                if (this.ShouldAggregate)
                {
                    if (token == null)
                    {
                        result.Add(new AggregateToken(null, AggregateFunction.Count));
                    }
                    else
                    {
                        FilterType? ft = QueryUtils.TryGetFilterType(token.Type);

                        if (ft == FilterType.Number || ft == FilterType.DecimalNumber || ft == FilterType.Boolean)
                        {
                            result.Add(new AggregateToken(token, AggregateFunction.Average));
                            result.Add(new AggregateToken(token, AggregateFunction.Sum));

                            result.Add(new AggregateToken(token, AggregateFunction.Min));
                            result.Add(new AggregateToken(token, AggregateFunction.Max));
                        }
                        else if (ft == FilterType.DateTime) /*ft == FilterType.String || */
                        {
                            result.Add(new AggregateToken(token, AggregateFunction.Min));
                            result.Add(new AggregateToken(token, AggregateFunction.Max));
                        }
                    }
                }
                else if (token != null)
                {
                    FilterType? ft = QueryUtils.TryGetFilterType(token.Type);

                    if (ft == FilterType.Number || ft == FilterType.DecimalNumber)
                    {
                        result.Add(new IntervalQueryToken(token));
                    }
                }
            }

            return result;
        }

        internal Column CreateColumn()
        {
            return new Column(Token, DisplayName); 
        }
    }
}
