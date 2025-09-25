using System.Xml.Linq;
using Signum.UserAssets;
using Signum.UserAssets.Queries;
using Signum.UserAssets.QueryTokens;

namespace Signum.Chart;

public class ChartColumnEmbedded : EmbeddedEntity
{
    [Ignore]
    ChartScriptColumn scriptColumn;
    [HiddenProperty]
    public ChartScriptColumn ScriptColumn
    {
        get { return scriptColumn; }
        set { scriptColumn = value; Notify(() => ScriptColumn); }
    }

    public ChartColumnEmbedded()
    {
    }

    public void TokenChanged()
    {
        this.parentChart?.FixParameters(this);

        if (token != null)
        {
            DisplayName = null;
            Format = null;
        }
    }

    QueryTokenEmbedded? token;
    public QueryTokenEmbedded? Token
    {
        get { return token; }
        set
        {
            if (Set(ref token, value))
                TokenChanged();
        }
    }

    public string? DisplayName { get; set; }

    public string? Format { get; set; }

    [NumberIsValidator(ComparisonType.GreaterThan, 0)]
    public int? OrderByIndex { get; set; }

    public OrderType? OrderByType { get; set; }

    [Ignore]
    internal IChartBase parentChart;

    [HiddenProperty]
    public IChartBase ParentChart { get { return parentChart; } }
    
    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Token))
        {
            if (Token == null)
                return !scriptColumn.IsOptional ? ChartMessage._0IsNotOptional.NiceToString().FormatWith(scriptColumn.GetDisplayName()) : null;

            if (Token.TryToken != null && !ChartUtils.IsChartColumnType(Token.Token, ScriptColumn.ColumnType))
                return ChartMessage._0IsNot1.NiceToString().FormatWith(DisplayName, ScriptColumn.ColumnType);
        }

        return base.PropertyValidation(pi);
    }

    public string GetTitle()
    {
        var unit = Token?.Token.Unit;

        return DisplayName + (unit.HasText() ? " ({0})".FormatWith(unit) : null);
    }

    public void ParseData(ModifiableEntity context, QueryDescription description, SubTokensOptions options)
    {
        if (token != null)
            token.ParseData(context, description, options & ~SubTokensOptions.CanAnyAll);
    }

    internal Column CreateColumn()
    {
        return new Column(Token!.Token, DisplayName);
    }

    internal XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Column",
          Token == null ? null! : new XAttribute("Token", this.Token.Token.FullKey()),
          !DisplayName.HasText() ? null! : new XAttribute("DisplayName", this.DisplayName),
          !Format.HasText() ? null! : new XAttribute("Format", this.Format),
          OrderByIndex == null! ? null! : new XAttribute("OrderByIndex", this.OrderByIndex),
          OrderByType == null! ? null! : new XAttribute("OrderByType", this.OrderByType)
        );
    }

    internal void FromXml(XElement element, IFromXmlContext ctx)
    {
        Token = element.Attribute("Token")?.Let(a => new QueryTokenEmbedded(a.Value));
        DisplayName = element.Attribute("DisplayName")?.Value;
        Format = element.Attribute("Format")?.Value;
        OrderByIndex = element.Attribute("OrderByIndex")?.Value.Let(int.Parse);
        OrderByType = element.Attribute("OrderByType")?.Value.Let(EnumExtensions.ToEnum<OrderType>);
    }

    public override string ToString()
    {
        return token?.ToString() ?? "";
    }
}
