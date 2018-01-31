using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Signum.Entities.Chart
{
    [Serializable]
    public class ChartParameterEmbedded : EmbeddedEntity
    {
        [Ignore]
        internal IChartBase parentChart;

        [HiddenProperty]
        public IChartBase ParentChart { get { return parentChart; } }

        [Ignore]
        ChartScriptParameterEmbedded scriptParameter;
        [InTypeScript(false)]
        public ChartScriptParameterEmbedded ScriptParameter
        {
            get { return scriptParameter; }
            set { scriptParameter = value; Notify(() => ScriptParameter); }
        }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        string value;
        [StringLengthValidator(AllowNulls = true, Max = 50)]
        public string Value
        {
            get { return value; }
            set
            {
                if (Set(ref this.value, value) && ParentChart != null)
                    ParentChart.InvalidateResults(needNewQuery: false);
            }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Name) && Name != scriptParameter.Name)
                return ValidationMessage._0ShouldBe12.NiceToString(pi.NiceName(), ComparisonType.EqualTo.NiceToString(), scriptParameter.Name);

            if (pi.Name == nameof(Value))
                return ScriptParameter.Valdidate(this.Value, this.ScriptParameter.GetToken(this.ParentChart));

            return base.PropertyValidation(pi);
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Parameter",
                new XAttribute("Name", this.Name),
                new XAttribute("Value", this.Value));
        }

        internal void FromXml(XElement x, IFromXmlContext ctx)
        {
            Name = x.Attribute("Name").Value;
            Value = x.Attribute("Value").Value;
        }

        public override string ToString()
        {
            return Name + ": " + Value;
        }
    }
}
