namespace Signum.DynamicQuery.Tokens;

/// <summary>
/// Manual container token for QuickLinks
/// </summary>
public class QuickLinksToken : ManualContainerToken
{

    internal override string GetTokenKey() => "[QuickLinks]";

    public QuickLinksToken(QueryToken parent) : base(parent) { }

    public override QueryToken Clone()
    {
        return new QuickLinksToken(this.parent.Clone());
    }
}
