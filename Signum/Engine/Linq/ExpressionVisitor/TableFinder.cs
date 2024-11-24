
namespace Signum.Engine.Linq;

internal class TableFinder : DbExpressionVisitor
{
    TableExpression? tableExpression;
    Alias alias;
    private TableFinder() { }

    public static TableExpression? GetTable(Expression source, Alias alias)
    {
        var ap = new TableFinder { alias = alias };
        ap.Visit(source);
        return ap.tableExpression;
    }

    protected internal override Expression VisitTable(TableExpression table)
    {
        if (table.Alias.Equals(this.alias))
            this.tableExpression = table;
        return base.VisitTable(table);
    }

}
