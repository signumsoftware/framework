using System.Collections.ObjectModel;

namespace Signum.Engine.Linq;

/// <summary>
/// Replaces complex orderings (typically correlated sub-queries) by a reference to a column of the sub-select
/// they come from, reusing an equivalent column if there is already one.
///
/// The OrderByRewriter moves the orderings to the SelectExpression that needs them (the one with TOP / ROW_NUMBER,
/// and the outer-most one) and the QueryRebinder re-correlates them to the closest alias. As a side effect the
/// whole expression gets duplicated once per nesting level, so ordering a paginated query by something like
/// <c>e.Notifications.Count()</c> ends up with the same sub-query repeated 3 or 4 times.
/// </summary>
internal class OrderByColumnPromoter : DbExpressionVisitor
{
    private OrderByColumnPromoter() { }

    public static Expression Promote(Expression expression)
    {
        return new OrderByColumnPromoter().Visit(expression);
    }

    protected internal override Expression VisitSelect(SelectExpression select)
    {
        select = ReuseOwnColumns((SelectExpression)base.VisitSelect(select));

        if (select.From is not SelectExpression from || !CanAddColumns(from))
            return select;

        var newFrom = from;

        var columns = RowNumberPromoter.Promote(select.Columns, ref newFrom);
        var orderBy = PromoteOrderings(select.OrderBy, ref newFrom);

        if (newFrom != from)
            newFrom = ReuseOwnColumns(newFrom); //It could have got the column that its own ORDER BY is repeating

        if (newFrom == from && columns == select.Columns && orderBy == select.OrderBy)
            return select;

        return new SelectExpression(select.Alias, select.IsDistinct, select.Top, columns, newFrom, select.Where,
            orderBy, select.GroupBy, select.SelectOptions);
    }

    /// <summary>
    /// A select can order by an expression that it is already returning as a column, referring to it by the bare
    /// column alias (SELECT expr as c0 ... ORDER BY c0). No column is added, only repetitions are removed.
    /// </summary>
    static SelectExpression ReuseOwnColumns(SelectExpression select)
    {
        if (!select.OrderBy.Any(o => IsComplex(o.Expression)))
            return select;

        var orderBy = select.OrderBy.Select(o =>
        {
            if (!IsComplex(o.Expression))
                return o;

            var cd = FindColumn(select, RemoveNullify(o.Expression));

            return cd == null ? o : new OrderExpression(o.OrderType, new ColumnExpression(cd.Expression.Type, select.Alias, cd.Name!));
        }).ToReadOnly();

        if (orderBy.ZipStrict(select.OrderBy).All(p => p.first == p.second))
            return select;

        return new SelectExpression(select.Alias, select.IsDistinct, select.Top, select.Columns, select.From,
            select.Where, orderBy, select.GroupBy, select.SelectOptions);
    }

    /// <param name="orderings">Expressed in the scope of the select that has <paramref name="from"/> in the FROM</param>
    static ReadOnlyCollection<OrderExpression> PromoteOrderings(ReadOnlyCollection<OrderExpression> orderings, ref SelectExpression from)
    {
        var current = from;
        if (!orderings.Any(o => ShouldPromote(o.Expression, current)))
            return orderings;

        var result = new List<OrderExpression>();

        foreach (var o in orderings)
        {
            if (!ShouldPromote(o.Expression, from))
            {
                result.Add(o);
            }
            else
            {
                var cd = PromoteColumn(ref from, SubqueryRemover.Remove(o.Expression, new[] { from }));

                result.Add(new OrderExpression(o.OrderType, new ColumnExpression(cd.Expression.Type, from.Alias, cd.Name!)));
            }
        }

        return result.ToReadOnly();
    }

    /// <param name="expression">Expressed in the scope of <paramref name="from"/> (it refers to the aliases of from.From)</param>
    /// <returns>The column of <paramref name="from"/> that now contains the expression</returns>
    static ColumnDeclaration PromoteColumn(ref SelectExpression from, Expression expression)
    {
        //An order made from a QueryToken is nullified, while the same token used as a column is not. Nullifying
        //does not change the order, so look through it to be able to reuse the column that is already there.
        var innerExpression = RemoveNullify(expression);

        var equivalent = FindColumn(from, innerExpression);
        if (equivalent != null)
            return equivalent;

        //If the sub-select is just a wrapper keep going down, so the expression is evaluated only once and each
        //intermediate select just forwards the column.
        if (IsComplex(innerExpression) && from.From is SelectExpression deeper && CanAddColumns(deeper) &&
            ExternalAliases(innerExpression).All(a => deeper.KnownAliases.Contains(a)))
        {
            var newDeeper = deeper;
            var deeperColumn = PromoteColumn(ref newDeeper, SubqueryRemover.Remove(innerExpression, new[] { deeper }));

            var forward = new ColumnExpression(deeperColumn.Expression.Type, deeper.Alias, deeperColumn.Name!);

            var generator = new ColumnGenerator(from.Columns);
            var declaration = FindColumn(from, forward) ?? generator.NewColumn(forward);

            from = new SelectExpression(from.Alias, from.IsDistinct, from.Top, generator.Columns.NotNull(), newDeeper,
                from.Where, from.OrderBy, from.GroupBy, from.SelectOptions);

            return declaration;
        }

        var cg = new ColumnGenerator(from.Columns);
        var newColumn = cg.NewColumn(innerExpression);

        from = new SelectExpression(from.Alias, from.IsDistinct, from.Top, cg.Columns.NotNull(), from.From, from.Where,
            from.OrderBy, from.GroupBy, from.SelectOptions);

        return newColumn;
    }

    static ColumnDeclaration? FindColumn(SelectExpression select, Expression expression)
    {
        return select.Columns.FirstOrDefault(cd => DbExpressionComparer.AreEqual(RemoveNullify(cd.Expression), expression));
    }

    static bool ShouldPromote(Expression expression, SelectExpression from)
    {
        return IsComplex(expression) && ExternalAliases(expression).All(a => from.KnownAliases.Contains(a));
    }

    //Aliases the expression refers to but does not declare itself (a correlated sub-query declares its own)
    static IEnumerable<Alias> ExternalAliases(Expression expression)
    {
        return UsedAliasGatherer.Externals(expression).Except(DeclaredAliasGatherer.GatherDeclared(expression));
    }

    static bool IsComplex(Expression expression)
    {
        return RemoveNullify(expression) is not (ColumnExpression or ConstantExpression or SqlConstantExpression);
    }

    //Only T <-> T?, any other conversion could change the order
    static Expression RemoveNullify(Expression expression)
    {
        while (expression.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked &&
            ((UnaryExpression)expression).Operand.Type.UnNullify() == expression.Type.UnNullify())
            expression = ((UnaryExpression)expression).Operand;

        return expression;
    }

    //Adding a column to the sub-select should not change the rows it returns
    static bool CanAddColumns(SelectExpression select)
    {
        return !select.IsDistinct &&
            select.GroupBy.Count == 0 &&
            !select.IsAllAggregates &&
            !select.IsForXmlPathEmpty &&
            select.From != null;
    }

    /// <summary>
    /// The ORDER BY of a ROW_NUMBER (used to Skip) lives inside a ColumnDeclaration, but is evaluated in the same
    /// scope as the ORDER BY of the select, so it can share the promoted columns.
    /// </summary>
    class RowNumberPromoter : DbExpressionVisitor
    {
        SelectExpression from = null!;

        public static ReadOnlyCollection<ColumnDeclaration> Promote(ReadOnlyCollection<ColumnDeclaration> columns, ref SelectExpression from)
        {
            var visitor = new RowNumberPromoter { from = from };

            var result = Visit(columns, visitor.VisitColumnDeclaration);

            from = visitor.from;

            return result;
        }

        protected internal override Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            if (rowNumber.OrderBy == null)
                return rowNumber;

            var newOrderBy = PromoteOrderings(rowNumber.OrderBy, ref from);

            return newOrderBy == rowNumber.OrderBy ? rowNumber : new RowNumberExpression(newOrderBy);
        }

        //Sub-queries have their own scope, and are already visited by the OrderByColumnPromoter itself
        protected internal override Expression VisitSelect(SelectExpression select) => select;
    }
}
