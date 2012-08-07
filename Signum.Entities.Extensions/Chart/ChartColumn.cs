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
using Signum.Entities.Reflection;

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
    public class ChartColumnDN : QueryTokenDN
    {
        [Ignore]
        ChartScriptColumnDN scriptColumn;
        public ChartScriptColumnDN ScriptColumn
        {
            get { return scriptColumn; }
            set { scriptColumn = value; }
        }
        
        public ChartColumnDN()
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
        public bool GroupByVisible { get { return parentChart.ChartScript.GroupBy != GroupByChart.Never && ScriptColumn.IsGroupKey; } }

        [AvoidLocalization]
        public bool ShouldAggregate { get { return parentChart.GroupResults && !ScriptColumn.IsGroupKey; } }

        [AvoidLocalization]
        public string PropertyLabel { get { return ScriptColumn.DisplayName; } }

        int index;
        public int Index
        {
            get { return index; }
            set { Set(ref index, value, () => Index); }
        }

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
            if(pi.Is(()=>Token))
            {
                if (Token is IDataErrorInfo)
                {
                    var err = ((IDataErrorInfo)Token).Error;
                    if (err != null)
                        return err;
                }

                if (!ChartUtils.IsChartColumnType(token, ScriptColumn.ColumnType))
                    return "An {0} is not {1}".Formato(token, ScriptColumn.ColumnType);
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

            if (this.parentChart.GroupResults && ScriptColumn.IsGroupKey && token != null)
            {
                FilterType? ft = QueryUtils.TryGetFilterType(token.Type);

                if (ft == FilterType.Integer || ft == FilterType.Decimal)
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
