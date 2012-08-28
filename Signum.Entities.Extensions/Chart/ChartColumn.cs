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
        internal IChartBase parentChart;

        [AvoidLocalization]
        public IChartBase ParentChart { get { return parentChart; } }

        [AvoidLocalization]
        public bool? IsGroupKey { get { return !parentChart.GroupResults ? (bool?)null: ScriptColumn.IsGroupKey; } }

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
            Notify(() => IsGroupKey);
            Notify(() => PropertyLabel);
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

                if (parentChart.GroupResults)
                {
                    if (scriptColumn.IsGroupKey)
                    {
                        if (Token is AggregateToken)
                            return "{0} is key, but {1} is an aggregate".Formato(scriptColumn.DisplayName, token.NiceName());
                    }
                    else
                    {
                        if (!(Token is AggregateToken))
                            return "{0} should be an aggregate".Formato(scriptColumn.DisplayName, token.NiceName());
                    }
                }
                else
                {
                    if (Token is AggregateToken)
                        return "{1} is an aggregate, but the chart is not grouping".Formato(scriptColumn.DisplayName, token.NiceName());
                }


                if (!ChartUtils.IsChartColumnType(token, ScriptColumn.ColumnType))
                    return "{0} is not {1}".Formato(token, ScriptColumn.ColumnType);
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

        internal new void ParseData(QueryDescription description)
        {
            ParseData(t => SubTokensChart(t, description.Columns));
        }

        public override void ParseData(Func<QueryToken, List<QueryToken>> subTokens)
        {
            Token = QueryUtils.Parse(tokenString, subTokens);

            CleanSelfModified();
        }

        public List<QueryToken> SubTokensChart(QueryToken token, IEnumerable<ColumnDescription> columnDescriptions)
        {
            var result = token.SubTokensChart(columnDescriptions, this.IsGroupKey == false);

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

     
    }
}
