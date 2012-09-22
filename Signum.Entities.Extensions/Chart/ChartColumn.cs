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

            Scale = DefaultScale(Token);

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

        ColumnScale scale;
        public ColumnScale Scale
        {
            get { return scale; }
            set { if (Set(ref scale, value, () => Scale)) NotifyChange(false); }
        }

        [Ignore]
        internal IChartBase parentChart;

        [AvoidLocalization]
        public IChartBase ParentChart { get { return parentChart; } }

        [AvoidLocalization]
        public bool? IsGroupKey { get { return (!parentChart.GroupResults) ? (bool?)null: ScriptColumn.IsGroupKey; } }

        [AvoidLocalization]
        public bool GroupByVisible { get { return parentChart.ChartScript.GroupBy != GroupByChart.Never && ScriptColumn.IsGroupKey; } }

        [AvoidLocalization]
        public bool GroupByEnabled { get { return parentChart.ChartScript.GroupBy != GroupByChart.Always; } }

        [AvoidLocalization]
        public bool GroupByChecked
        {
            get { return parentChart.GroupResults; }
            set { parentChart.GroupResults = value; }
        }

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
            parentChart.InvalidateResults(needNewQuery);
        }

        [field: NonSerialized, Ignore]
        public event Action Notified; 

        internal void NotifyAll()
        {
            Notify(() => Token);
            Notify(() => IsGroupKey);
            Notify(() => GroupByEnabled);
            Notify(() => GroupByChecked);
            Notify(() => GroupByVisible);
            Notify(() => PropertyLabel);

            if (Notified != null)
                Notified();
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
                            return "{0} is key, but {1} is an aggregate".Formato(scriptColumn.DisplayName, DisplayName);
                    }
                    else
                    {
                        if (!(Token is AggregateToken))
                            return "{0} should be an aggregate".Formato(scriptColumn.DisplayName, DisplayName);
                    }
                }
                else
                {
                    if (Token is AggregateToken)
                        return "{1} is an aggregate, but the chart is not grouping".Formato(scriptColumn.DisplayName, DisplayName);
                }


                if (!ChartUtils.IsChartColumnType(token, ScriptColumn.ColumnType))
                    return "{0} is not {1}".Formato(DisplayName, ScriptColumn.ColumnType);
            }

            if (pi.Is(() => Scale) && Token != null)
            {
                if (!IsScaleCompatible(Token, Scale))
                    return "The scale {0} is not compatible with {1}".Formato(Scale.NiceToString(), this.DisplayName);
            }

            return base.PropertyValidation(pi);
        }

        public ColumnScale[] CompatibleScales()
        {
            return EnumExtensions.GetValues<ColumnScale>().Where(cs => IsScaleCompatible(Token, cs)).ToArray();
        }

        static bool IsScaleCompatible(QueryToken token, ColumnScale scale)
        {
            switch (scale)
            {
                case ColumnScale.Elements: return token == null || ChartUtils.IsChartColumnType(token, ChartColumnType.Groupable);
                case ColumnScale.MinMax: return ChartUtils.IsChartColumnType(token, ChartColumnType.Positionable);
                case ColumnScale.ZeroMax: return ChartUtils.IsChartColumnType(token, ChartColumnType.Magnitude);
                case ColumnScale.Logarithmic: return ChartUtils.IsChartColumnType(token, ChartColumnType.Magnitude);
                default: return false;
            }
        }

        static ColumnScale DefaultScale(QueryToken Token)
        {
            if (Token == null)
                return ColumnScale.Elements;

            if (ChartUtils.IsChartColumnType(Token, ChartColumnType.Magnitude))
                return ColumnScale.ZeroMax;

            if (ChartUtils.IsChartColumnType(Token, ChartColumnType.Positionable))
                return ColumnScale.MinMax;

            return ColumnScale.Elements;
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

    public enum ColumnScale
    {
        Elements,
        MinMax,
        ZeroMax,
        Logarithmic,
    }
}
