using Signum.Entities.UserAssets;
using System.Xml.Linq;

namespace Signum.Chart;


public class ChartParameterEmbedded : EmbeddedEntity
{
    [Ignore]
    internal IChartBase parentChart;

    [HiddenProperty]
    public IChartBase ParentChart { get { return parentChart; } }

    [Ignore]
    ChartScriptParameter scriptParameter;
    [InTypeScript(false)]
    public ChartScriptParameter ScriptParameter
    {
        get { return scriptParameter; }
        set { scriptParameter = value; Notify(() => ScriptParameter); }
    }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    [StringLengthValidator(Max = 500)]
    public string? Value { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Name) && Name != scriptParameter.Name)
            return ValidationMessage._0ShouldBe12.NiceToString(pi.NiceName(), ComparisonType.EqualTo.NiceToString(), scriptParameter.Name);

        if (pi.Name == nameof(Value))
            return ScriptParameter.Validate(this.Value, this.ScriptParameter.GetToken(this.ParentChart));

        return base.PropertyValidation(pi);
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Parameter",
            new XAttribute("Name", this.Name),
            this.Value == null ? null! : new XAttribute("Value", this.Value));
    }

    internal void FromXml(XElement x, IFromXmlContext ctx)
    {
        Name = x.Attribute("Name")!.Value;
        Value = x.Attribute("Value")?.Value;
    }

    public override string ToString()
    {
        return Name + ": " + Value;
    }
}
