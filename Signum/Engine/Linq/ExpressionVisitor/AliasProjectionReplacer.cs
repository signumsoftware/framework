
namespace Signum.Engine.Linq;

internal class AliasProjectionReplacer : DbExpressionVisitor
{
    ProjectionExpression? root;
    AliasGenerator aliasGenerator;

    public AliasProjectionReplacer(ProjectionExpression? root, AliasGenerator aliasGenerator)
    {
        this.root = root;
        this.aliasGenerator = aliasGenerator;
    }

    public static Expression Replace(Expression proj, AliasGenerator aliasGenerator)
    {
        AliasProjectionReplacer apr = new AliasProjectionReplacer(
            root:  proj as ProjectionExpression,
            aliasGenerator : aliasGenerator
        );
        return apr.Visit(proj);
    }

    protected internal override Expression VisitProjection(ProjectionExpression proj)
    {
        if (proj != root)
            return AliasReplacer.Replace(base.VisitProjection(proj), aliasGenerator);
        else
            return (ProjectionExpression)base.VisitProjection(proj);
    }
}

