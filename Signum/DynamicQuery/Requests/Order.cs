using Signum.DynamicQuery.Tokens;
using Signum.Engine.Maps;

namespace Signum.DynamicQuery;

public class Order : IEquatable<Order>
{
    public QueryToken Token { get; }
    public OrderType OrderType { get; }

    public Order(QueryToken token, OrderType orderType)
    {
        this.Token = token;
        this.OrderType = orderType;
    }

    public override string ToString()
    {
        return "{0} {1}".FormatWith(Token.FullKey(), OrderType);
    }

    public override int GetHashCode() => Token.GetHashCode();
    public override bool Equals(object? obj) => obj is Order order && Equals(order);
    public bool Equals(Order? other) => other is Order o && o.Token.Equals(Token) && o.OrderType.Equals(OrderType);

    internal Order ToFullText()
    {
        if(this.Token is StringSnippetToken s)
        {
            if (s.Parent is EntityPropertyToken ep && Schema.Current.HasFullTextIndex(ep.PropertyRoute))
                return new Order(new FullTextRankToken(ep), this.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending);

            return new Order(s.Parent!, this.OrderType);
        }

        return this;
    }
}

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public enum OrderType
{
    Ascending,
    Descending
}
