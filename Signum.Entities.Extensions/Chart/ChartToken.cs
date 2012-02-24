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

                DisplayName = Token.NiceName();
            }
            else
            {
                DisplayName = null;
            }
        }

        string displayName;
        public string DisplayName
        {
            get { return displayName; }
            set { if (Set(ref displayName, value, () => DisplayName)) NotifyChange(false); }
        }

        [Ignore]
        internal ChartBase parentChart;

        [AvoidLocalization]
        public bool GroupByVisible { get { return parentChart.token_GroupByVisible(this); } }

        [AvoidLocalization]
        public bool Grouping { get { return parentChart.GroupResults; } }

        [AvoidLocalization]
        public bool ShouldAggregate { get { return parentChart.token_ShouldAggregate(this); } }

        [AvoidLocalization]
        public string PropertyLabel { get { return parentChart.token_PropertyLabel(this); } }


        public void NotifyChange(bool needNewQuery)
        {
            parentChart.NotifyChange(needNewQuery);
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
            var unit = Token.TryCC(a=>a.Unit);

            return DisplayName + (unit.HasText() ? " ({0})".Formato(unit) : null);
        }

        protected override void PreSaving(ref bool graphModified)
        {
            tokenString = token == null ? null : token.FullKey();
        }

        public override void ParseData(Func<QueryToken, List<QueryToken>> subTokens)
        {
            Token = QueryUtils.Parse(tokenString, subTokens);

            CleanSelfModified();
        }

        public List<QueryToken> SubTokensChart(QueryToken token, IEnumerable<ColumnDescription> columnDescriptions)
        {
            var result = parentChart.SubTokensChart(token, columnDescriptions, this.ShouldAggregate);

            if (this.Grouping && !this.ShouldAggregate && token != null)
            {
                FilterType? ft = QueryUtils.TryGetFilterType(token.Type);

                if (ft == FilterType.Number || ft == FilterType.DecimalNumber)
                {
                    result.Add(new IntervalQueryToken(token));
                }
            }

            return result;
        }

        internal Column CreateColumn()
        {
            return new Column(Token, DisplayName); 
        }

        internal new void ParseData(QueryDescription description)
        {
            ParseData(t => SubTokensChart(t, description.Columns));
        }
    }
}
