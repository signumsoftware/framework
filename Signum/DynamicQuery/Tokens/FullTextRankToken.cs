using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.DynamicQuery.Tokens;

internal class FullTextRankToken : QueryToken
{
    public FullTextRankToken(QueryToken parent)
    {
        if (!(parent.GetPropertyRoute() is PropertyRoute pr && EntityPropertyToken.HasFullTextIndex(pr)))
            throw new InvalidOperationException("invalid parent for FullTextRankToken");

        Parent = parent;
    }

    public override string? Format => null;

    public override string? Unit => null;

    public override Type Type => typeof(int?);

    public override string Key => "Rank";

    public override QueryToken? Parent { get; }

    public override QueryToken Clone() => new FullTextRankToken(this.Parent!);

    public override Implementations? GetImplementations() => null;

    public override PropertyRoute? GetPropertyRoute() => null;

    public override string? IsAllowed() => this.Parent!.IsAllowed();

    public override string NiceName() => QueryTokenMessage.MatchRankFor0.NiceToString(this.Parent!.NiceName());

    public override string ToString() => QueryTokenMessage.MatchRank.NiceToString();

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        return context.Replacements.TryGetS(this)?.RawExpression ?? Expression.Constant(0);
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options) => new List<QueryToken>();
}
