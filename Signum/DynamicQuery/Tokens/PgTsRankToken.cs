using Microsoft.IdentityModel.Abstractions;
using Signum.Entities.TsVector;

namespace Signum.DynamicQuery.Tokens;

internal class PgTsRankToken : QueryToken
{
    public PgTsRankToken(QueryToken parent)
    {
        if (parent is not PgTsVectorColumnToken)
            throw new InvalidOperationException("invalid parent for PgTsRankToken");

        Parent = parent;
    }

    public override string? Format => null;

    public override string? Unit => null;

    public override Type Type => typeof(int?);

    public override string Key => "Rank";

    public override QueryToken? Parent { get; }

    public override QueryToken Clone() => new PgTsRankToken(this.Parent!);

    public override Implementations? GetImplementations() => null;

    public override PropertyRoute? GetPropertyRoute() => null;

    public override string? IsAllowed() => this.Parent!.IsAllowed();

    public override string NiceName() => QueryTokenMessage.MatchRankFor0.NiceToString(this.Parent!.NiceName());

    public override string ToString() => QueryTokenMessage.MatchRank.NiceToString();

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var queryExpression = GetCombinedTsQuery(context.Filters ?? new List<Filter>(), (PgTsVectorColumnToken)this.Parent!, FilterGroupOperation.And);

        if (queryExpression == null)
            return Expression.Constant(0);

        var vectorColumn = this.Parent!.BuildExpression(context);

        return Expression.Call(TsVectorExtensions.miRank, vectorColumn, queryExpression);
    }

    private Expression? GetCombinedTsQuery(List<Filter> filters, PgTsVectorColumnToken vector, FilterGroupOperation groupOperation)
    {
        var mi = TsVectorExtensions.GetTsQueryGroupOperator(groupOperation);

        var list = filters.Select(f => GetCombinedTsQuery(f, vector)).NotNull();
        if (list.IsEmpty())
            return null;

        return list.Aggregate((a, b) => Expression.Call(mi, a, b));
    }

    private Expression? GetCombinedTsQuery(Filter filter, PgTsVectorColumnToken vector)
    {
        if (filter is FilterCondition fc)
        {
            if (fc.Token != null && fc.Token.Equals(vector) && fc.Value is string s && s.Length > 0)
            {
                var mi = TsVectorExtensions.GetTsQueryMethodInfo(fc.Operation);

                return Expression.Call(mi, Expression.Constant(s));
            }

            return null;
        }
        else if (filter is FilterGroup fg)
        {
            return GetCombinedTsQuery(fg.Filters, vector, fg.GroupOperation);
        }
        else
            throw new UnexpectedValueException(filter);
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options) => new List<QueryToken>();
}
