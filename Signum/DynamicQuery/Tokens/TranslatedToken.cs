namespace Signum.DynamicQuery.Tokens;

public class TranslatedToken : QueryToken
{
    QueryToken parent;
    public override QueryToken? Parent => parent;


    internal TranslatedToken(QueryToken parent)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }


    public override Type Type
    {
        get { return typeof(string); }
    }

    public override string ToString()
    {
        return PropertyRouteMessage.Translated.NiceToString();
    }

    public override string Key
    {
        get { return "Translated"; }
    }


    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var pr = this.Parent!.GetPropertyRoute();

        var mlistItemRoute = pr!.GetMListItemsRoute();

        if(mlistItemRoute != null)
        {
            var rootType = pr.RootType;

            QueryToken rootEntityToken = GetRootEntityToken(context, rootType);

            var lambda = pr.GetLambdaExpression(mlistItemRoute.Type, typeof(string), safeNullAccess: false, skipBefore: mlistItemRoute);

            var itemmToken = ((QueryToken)this).Follow(a => a.Parent).FirstOrDefault(qt => qt is CollectionElementToken or CollectionNestedToken or CollectionAnyAllToken or CollectionToArrayToken);

            if(itemmToken == null)
                throw new InvalidOperationException("Unable to find the MList element token for " + this.Parent);

            var rootEntityExp = rootEntityToken.BuildExpression(context);
            var itemExpr = itemmToken.BuildExpression(context);

            var box = context.Replacements.GetOrThrow(itemmToken);

            var rowId = Expression.Property(box.RawExpression, "RowId").Nullify();

            return Expression.Condition(
                Expression.Equal(rowId, Expression.Constant(null, typeof(PrimaryKey?))),
                Expression.Constant(null, typeof(string)),
                Expression.Call(PropertyRouteTranslationLogic.miTranslatedField, rootEntityExp, Expression.Constant(pr), rowId, Expression.Invoke(lambda, itemExpr))
                );
        }
        else
        {
            var rootType = pr.RootType;
            var lambda = pr.GetLambdaExpression(rootType, typeof(string), safeNullAccess: false);

            QueryToken rootEntityToken = GetRootEntityToken(context, rootType);

            var rootEntityExp = rootEntityToken.BuildExpression(context);

            return Expression.Call(PropertyRouteTranslationLogic.miTranslatedField, rootEntityExp, Expression.Constant(pr), Expression.Constant(null, typeof(PrimaryKey?)), Expression.Invoke(lambda, rootEntityExp.ExtractEntity(false)));
        }
    }

    private QueryToken GetRootEntityToken(BuildExpressionContext context, Type rootType)
    {
        var rootEntityToken = ((QueryToken)this).Follow(a => a.Parent).FirstOrDefault(qt => qt.Type.CleanType().IsEntity());

        if (rootEntityToken == null)
        {
            var entity = context.Replacements.Keys.SingleOrDefault(a => a.FullKey() == "Entity");

            if (entity != null && entity.Type.CleanType() == rootType)
                rootEntityToken = entity;
        }

        if (rootEntityToken == null)
            throw new InvalidOperationException("Unable to find root entity token for " + this.Parent);

        return rootEntityToken;
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return SubTokensBase(this.Type, options, GetImplementations());
    }

    public override Implementations? GetImplementations() => null;
    public override string? Format => null;
    public override string? Unit => null;

    public override string? IsAllowed()
    {
        string? parent = this.parent.IsAllowed();

        string? route = GetPropertyRoute()?.IsAllowed();

        if (parent.HasText() && route.HasText())
            return QueryTokenMessage.And.NiceToString().Combine(parent!, route!);

        return parent ?? route;
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        return this.parent.GetPropertyRoute();
    }

    public override string NiceName()
    {
        return $"{this.parent.NiceName()} ({PropertyRouteMessage.Translated.NiceToString()})";
    }

    public override QueryToken Clone()
    {
        return new TranslatedToken(parent.Clone());
    }
}
