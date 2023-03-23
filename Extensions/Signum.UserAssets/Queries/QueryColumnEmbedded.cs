using Signum.DynamicQuery.Tokens;
using Signum.Entities.UserAssets;
using Signum.UserAssets.QueryTokens;
using System.Xml.Linq;

namespace Signum.UserAssets.Queries;

public class QueryColumnEmbedded : EmbeddedEntity
{
    public QueryTokenEmbedded Token { get; set; }

    string? displayName;
    public string? DisplayName
    {
        get { return displayName.DefaultToNull(); }
        set { Set(ref displayName, value); }
    }

    public QueryTokenEmbedded? SummaryToken { get; set; }

    public bool HiddenColumn { get; set; }

    public CombineRows? CombineRows { get; set; }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Column",
            new XAttribute("Token", Token.Token.FullKey()),
            SummaryToken != null ? new XAttribute("SummaryToken", SummaryToken.Token.FullKey()) : null!,
            DisplayName.HasText() ? new XAttribute("DisplayName", DisplayName) : null!,
            HiddenColumn ? new XAttribute("HiddenColumn", HiddenColumn) : null!,
            CombineRows != null ? new XAttribute("CombineRows", CombineRows.Value.ToString()) : null!);
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Token = new QueryTokenEmbedded(element.Attribute("Token")!.Value);
        SummaryToken = element.Attribute("SummaryToken")?.Value.Let(val => new QueryTokenEmbedded(val));
        DisplayName = element.Attribute("DisplayName")?.Value;
        HiddenColumn = element.Attribute("HiddenColumn")?.Value.ToBool() ?? false;
        CombineRows = element.Attribute("CombineRows")?.Value.ToEnum<CombineRows>();
    }

    public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
    {
        Token.ParseData(context, description, options);
        SummaryToken?.ParseData(context, description, options | SubTokensOptions.CanAggregate);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Token) && Token != null && Token.ParseException == null)
        {
            return QueryUtils.CanColumn(Token.Token);
        }

        if (pi.Name == nameof(SummaryToken) && SummaryToken != null && SummaryToken.ParseException == null)
        {
            return QueryUtils.CanColumn(SummaryToken.Token) ??
                (SummaryToken.Token is not AggregateToken ? SearchMessage.SummaryHeaderMustBeAnAggregate.NiceToString() : null);
        }

        return base.PropertyValidation(pi);
    }

    public override string ToString()
    {
        return "{0} {1}".FormatWith(Token, displayName);
    }
}
