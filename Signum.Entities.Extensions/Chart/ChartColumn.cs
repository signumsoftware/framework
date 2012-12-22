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
            set { Set(ref scriptColumn, value, () => ScriptColumn); }
        }
        
        public ChartColumnDN()
        {

        }

        public override void TokenChanged()
        {
            NotifyChange(true);

            SetDefaultParameters();

            if (token != null)
            {
                DisplayName =token.NiceName();
            }
            else
            {
                DisplayName = null;
            }

            base.TokenChanged();
        }

        public void SetDefaultParameters()
        {
            Parameter1 = scriptColumn.Parameter1.TryCC(a => a.DefaultValue(token));
            Parameter2 = scriptColumn.Parameter2.TryCC(a => a.DefaultValue(token));
            Parameter3 = scriptColumn.Parameter3.TryCC(a => a.DefaultValue(token));
        }

        string displayName;
        public string DisplayName
        {
            get { return displayName; }
            set { if (Set(ref displayName, value, () => DisplayName)) NotifyChange(false); }
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
            if (pi.Is(() => TokenString))
            {
                if (TokenString != null && Token == null)
                    return null;

                if (Token == null)
                    return !scriptColumn.IsOptional ? "{0} is not optional".Formato(scriptColumn.DisplayName) : null;

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

            if (pi.Is(() => Parameter1) && token != null)
                return ValidateParameter(pi, Parameter1, scriptColumn.Parameter1);

            if (pi.Is(() => Parameter2) && token != null)
                return ValidateParameter(pi, Parameter2, scriptColumn.Parameter2);

            if (pi.Is(() => Parameter3) && token != null)
                return ValidateParameter(pi, Parameter3, scriptColumn.Parameter3);

            return base.PropertyValidation(pi);
        }

        private string ValidateParameter(PropertyInfo pi, string parameter, ChartScriptParameterDN description)
        {
            if (description != null)
            {
                if (parameter == null)
                    return "{0} should be set".Formato(description.Name);

                return description.Valdidate(parameter, token);
            }

            if (parameter.HasText())
                return "{0} should be null".Formato(pi.NiceName());

            return null;
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

        public override void ParseData(QueryDescription description, IdentifiableEntity context)
        {
            ParseData(t => SubTokensChart(t, description.Columns), context);
        }

        public override void ParseData(Func<QueryToken, List<QueryToken>> subTokens, IdentifiableEntity context)
        {
            try
            {
                token = string.IsNullOrEmpty(tokenString) ? null : QueryUtils.Parse(tokenString, subTokens);
            }
            catch (Exception e)
            {
                parseException = new FormatException("{0} {1}: {2}\r\n{3}".Formato(context.GetType().Name, context.IdOrNull, context, e.Message));
            }

            CleanSelfModified();
        }

        public List<QueryToken> SubTokensChart(QueryToken token, IEnumerable<ColumnDescription> columnDescriptions)
        {
            var result = token.SubTokensChart(columnDescriptions, this.IsGroupKey == false);

            return result;
        }

        internal Column CreateColumn()
        {
            return new Column(Token, DisplayName); 
        }
    }
}
