using Signum.UserAssets;
using Signum.UserAssets.QueryTokens;
using System.Xml.Linq;

namespace Signum.UserAssets.Queries;

public class QueryOrderEmbedded : EmbeddedEntity
{

    public QueryTokenEmbedded Token { get; set; }

    public OrderType OrderType { get; set; }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Orden",
            new XAttribute("Token", Token.Token.FullKey()),
            new XAttribute("OrderType", OrderType));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Token = new QueryTokenEmbedded(element.Attribute("Token")!.Value);
        OrderType = element.Attribute("OrderType")!.Value.ToEnum<OrderType>();
    }

    public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
    {
        Token.ParseData(context, description, options & ~SubTokensOptions.CanAnyAll);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Token) && Token != null && Token.ParseException == null)
        {
            return QueryUtils.CanOrder(Token.Token);
        }

        return base.PropertyValidation(pi);
    }

    public override string ToString()
    {
        return "{0} {1}".FormatWith(Token, OrderType);
    }

    public QueryOrderEmbedded Clone()
    {
        return new QueryOrderEmbedded()
        {
            OrderType = this.OrderType,
            Token = this.Token.Clone(),
        };
    }
}
