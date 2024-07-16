
namespace Signum.Engine.Linq;

internal class AsOfExpressionVisitor : DbExpressionVisitor
{
    AliasGenerator ag;
    static internal Expression Rewrite(Expression expression, AliasGenerator ag)
    {
        return new AsOfExpressionVisitor { ag = ag }.Visit(expression);
    }

    protected internal override Expression VisitTable(TableExpression table)
    {
        if(table.SystemTime is SystemTime.AsOfExpression a)
        {
            var interval = table.Table.SystemVersioned!.IntervalExpression(table.Alias);

            return new SelectExpression(ag.NextSelectAlias(), 
                distinct: false, 
                top: null,
                columns: null, 
                from: new TableExpression(table.Alias, table.Table, new SystemTime.All(JoinBehaviour.Current), table.WithHint),
                where: interval?.Contains(a.Expression), 
                orderBy: null,
                groupBy: null,
                options: 0);

        }

        return base.VisitTable(table);
    }

}
