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
using Signum.Entities.UserQueries;
using Signum.Entities.Reflection;
using System.Xml.Linq;
using Signum.Entities.UserAssets;

namespace Signum.Entities.Chart
{
    [Serializable]
    public class ChartColumnEntity : EmbeddedEntity
    {
        [Ignore]
        ChartScriptColumnEntity scriptColumn;
        public ChartScriptColumnEntity ScriptColumn
        {
            get { return scriptColumn; }
            set { scriptColumn = value; Notify(() => ScriptColumn); } 
        }
        
        public ChartColumnEntity()
        {
        }

        public void TokenChanged()
        {
            NotifyChange(true);

            this.parentChart.FixParameters(this);

            if (token != null)
            {
                DisplayName = null;
            }
        }

        QueryTokenEntity token;
        public QueryTokenEntity Token
        {
            get { return token; }
            set
            {
                if (Set(ref token, value))
                    TokenChanged();
            }
        }

        string displayName;
        public string DisplayName
        {
            get { return displayName ?? Token.Try(t => t.Token.Try(tt => tt.NiceName())); }
            set
            {
                var name = value == Token.Try(t => t.Token.Try(tt => tt.NiceName())) ? null : value;
                Set(ref displayName, name);
            }
        }
      
        [Ignore]
        internal IChartBase parentChart;

        [HiddenProperty]
        public IChartBase ParentChart { get { return parentChart; } }

        [HiddenProperty]
        public bool? IsGroupKey { get { return (!parentChart.GroupResults) ? (bool?)null: ScriptColumn.IsGroupKey; } }

        [HiddenProperty]
        public bool GroupByVisible { get { return parentChart.ChartScript.GroupBy != GroupByChart.Never && ScriptColumn.IsGroupKey; } }

        [HiddenProperty]
        public bool GroupByEnabled { get { return parentChart.ChartScript.GroupBy != GroupByChart.Always; } }

        [HiddenProperty]
        public bool GroupByChecked
        {
            get { return parentChart.GroupResults; }
            set { parentChart.GroupResults = value; }
        }

        [HiddenProperty]
        public string PropertyLabel { get { return ScriptColumn.DisplayName; } }

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
            if (pi.Is(() => Token))
            {
                if (Token == null)
                    return !scriptColumn.IsOptional ? ChartMessage._0IsNotOptional.NiceToString().FormatWith(scriptColumn.DisplayName) : null;

                if (parentChart.GroupResults)
                {
                    if (scriptColumn.IsGroupKey)
                    {
                        if (Token.Token is AggregateToken)
                            return ChartMessage._0IsKeyBut1IsAnAggregate.NiceToString().FormatWith(scriptColumn.DisplayName, DisplayName);
                    }
                    else
                    {
                        if (!(Token.Token is AggregateToken))
                            return ChartMessage._0ShouldBeAnAggregate.NiceToString().FormatWith(scriptColumn.DisplayName, DisplayName);
                    }
                }
                else
                {
                    if (Token.Token is AggregateToken)
                        return ChartMessage._0IsAnAggregateButTheChartIsNotGrouping.NiceToString().FormatWith(DisplayName);
                }

                if (!ChartUtils.IsChartColumnType(token.Token, ScriptColumn.ColumnType))
                    return ChartMessage._0IsNot1.NiceToString().FormatWith(DisplayName, ScriptColumn.ColumnType);
            }

            return base.PropertyValidation(pi);
        }

        public string GetTitle()
        {
            var unit = Token.Try(a=>a.Token.Unit);

            return DisplayName + (unit.HasText() ? " ({0})".FormatWith(unit) : null);
        }

        protected override void PreSaving(ref bool graphModified)
        {
            DisplayName = displayName;
        }

        public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
        {
            if (token != null)
                token.ParseData(context, description, options & ~SubTokensOptions.CanAnyAll);
        }

        internal Column CreateColumn()
        {
            return new Column(Token.Token, DisplayName); 
        }

        internal XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Column",
              Token ==  null ? null : new XAttribute("Token", this.Token.Token.FullKey()),
              DisplayName == null ? null : new XAttribute("DisplayName", this.DisplayName));
        }

        internal void FromXml(XElement element, IFromXmlContext ctx)
        {
            Token = element.Attribute("Token").Try(a => new QueryTokenEntity(a.Value));
            DisplayName = element.Attribute("DisplayName").Try(a => a.Value);
        }

        public override string ToString()
        {
            return token.TryToString();
        }
    }
}
