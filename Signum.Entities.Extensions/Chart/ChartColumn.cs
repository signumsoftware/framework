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
using Signum.Entities.UserQueries;
using Signum.Entities.Reflection;
using System.Xml.Linq;

namespace Signum.Entities.Chart
{
    [Serializable]
    public class ChartColumnDN : EmbeddedEntity
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


        public void TokenChanged()
        {
            NotifyChange(true);

            SetDefaultParameters();

            if (token != null)
            {
                DisplayName = null;
            }
        }

        public void SetDefaultParameters()
        {
            var t = token.TryCC(tk => tk.Token);
            Parameter1 = scriptColumn.Parameter1.TryCC(a => a.DefaultValue(t));
            Parameter2 = scriptColumn.Parameter2.TryCC(a => a.DefaultValue(t));
            Parameter3 = scriptColumn.Parameter3.TryCC(a => a.DefaultValue(t));
        }

        QueryTokenDN token;
        public QueryTokenDN Token
        {
            get { return token; }
            set
            {
                if (Set(ref token, value, () => Token))
                    TokenChanged();
            }
        }

        string displayName;
        public string DisplayName
        {
            get { return displayName ?? Token.TryCC(t => t.Token.TryCC(tt => tt.NiceName())); }
            set
            {
                var name = value == Token.TryCC(t => t.Token.TryCC(tt => tt.NiceName())) ? null : value;
                Set(ref displayName, name, () => DisplayName);
            }
        }

        [SqlDbType(Size = 50)]
        string parameter1;
        [StringLengthValidator(AllowNulls=true, Max = 50)]
        public string Parameter1
        {
            get { return parameter1; }
            set { if (Set(ref parameter1, value, () => Parameter1))NotifyChange(false); }
        }

        [SqlDbType(Size = 50)]
        string parameter2;
        [StringLengthValidator(AllowNulls = true, Max = 50)]
        public string Parameter2
        {
            get { return parameter2; }
            set { if (Set(ref parameter2, value, () => Parameter2))NotifyChange(false); }
        }

        [SqlDbType(Size = 50)]
        string parameter3;
        [StringLengthValidator(AllowNulls = true, Max = 50)]
        public string Parameter3
        {
            get { return parameter3; }
            set { if (Set(ref parameter3, value, () => Parameter3))NotifyChange(false); }
        }

        int index;
        public int Index
        {
            get { return index; }
            set { Set(ref index, value, () => Index); }
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
                    return !scriptColumn.IsOptional ? "{0} is not optional".Formato(scriptColumn.DisplayName) : null;

                if (parentChart.GroupResults)
                {
                    if (scriptColumn.IsGroupKey)
                    {
                        if (Token.Token is AggregateToken)
                            return "{0} is key, but {1} is an aggregate".Formato(scriptColumn.DisplayName, DisplayName);
                    }
                    else
                    {
                        if (!(Token.Token is AggregateToken))
                            return "{0} should be an aggregate".Formato(scriptColumn.DisplayName, DisplayName);
                    }
                }
                else
                {
                    if (Token.Token is AggregateToken)
                        return "{1} is an aggregate, but the chart is not grouping".Formato(scriptColumn.DisplayName, DisplayName);
                }

                if (!ChartUtils.IsChartColumnType(token.Token, ScriptColumn.ColumnType))
                    return "{0} is not {1}".Formato(DisplayName, ScriptColumn.ColumnType);
            }

            if (pi.Is(() => Parameter1) && token != null)
                return ValidateParameter(pi, Parameter1, scriptColumn.Parameter1);

            if (pi.Is(() => Parameter2) && token != null)
                return ValidateParameter(pi, Parameter2, scriptColumn.Parameter2);

            if (pi.Is(() => Parameter3) && token != null)
                return ValidateParameter(pi, Parameter3, scriptColumn.Parameter3);

            return base.PropertyValidation(pi);
        }

        string ValidateParameter(PropertyInfo pi, string parameter, ChartScriptParameterDN description)
        {
            if (description != null)
            {
                if (parameter == null)
                    return "{0} should be set".Formato(description.Name);

                return description.Valdidate(parameter, token.Token);
            }

            if (parameter.HasText())
                return "{0} should be null".Formato(pi.NiceName());

            return null;
        }


        public string GetTitle()
        {
            var unit = Token.TryCC(a=>a.Token.Unit);

            return DisplayName + (unit.HasText() ? " ({0})".Formato(unit) : null);
        }

        protected override void PreSaving(ref bool graphModified)
        {
            DisplayName = displayName;
        }

        public void ParseData(IdentifiableEntity context, QueryDescription description, bool canAggregate)
        {
            if (token != null)
                token.ParseData(context, description, canAggregate);
        }

        internal Column CreateColumn()
        {
            return new Column(Token.Token, DisplayName); 
        }

        internal XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Column",
              Token ==  null ? null : new XAttribute("Token", this.Token.Token.FullKey()),
              DisplayName == null ? null : new XAttribute("DisplayName", this.DisplayName),
              Parameter1 == null ? null : new XAttribute("Parameter1", this.Parameter1),
              Parameter2 == null ? null : new XAttribute("Parameter2", this.Parameter2),
              Parameter3 == null ? null : new XAttribute("Parameter3", this.Parameter3));
        }

        internal void FromXml(XElement element, IFromXmlContext ctx)
        {
            Token = element.Attribute("Token").TryCC(a => new QueryTokenDN(a.Value));
            DisplayName = element.Attribute("DisplayName").TryCC(a => a.Value);
            Parameter1 = element.Attribute("Parameter1").TryCC(a => a.Value);
            Parameter2 = element.Attribute("Parameter2").TryCC(a => a.Value);
            Parameter3 = element.Attribute("Parameter3").TryCC(a => a.Value);
        }

    }
}
