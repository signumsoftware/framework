using System.Collections.ObjectModel;
using Signum.Engine.Maps;

namespace Signum.Engine.Linq;

class RedundantSubqueryRemover : DbExpressionVisitor
{
    private RedundantSubqueryRemover()
    {
    }

    public static Expression Remove(Expression expression)
    {
        var removed = new RedundantSubqueryRemover().Visit(expression);
        var merged = SubqueryMerger.Merge(removed);
        var simplified = JoinSimplifier.Simplify(merged);
        return simplified;
    }

    protected internal override Expression VisitSelect(SelectExpression select)
    {
        select = (SelectExpression)base.VisitSelect(select);

        // first remove all purely redundant subqueries
        List<SelectExpression>? redundant = RedundantSubqueryGatherer.Gather(select.From!);
        if (redundant != null)
        {
            select = (SelectExpression)SubqueryRemover.Remove(select, redundant);
        }

        return select;
    }

    protected internal override Expression VisitProjection(ProjectionExpression proj)
    {
        proj = (ProjectionExpression)base.VisitProjection(proj);
        if (proj.Select.From is SelectExpression)
        {
            List<SelectExpression>? redundant = RedundantSubqueryGatherer.Gather(proj.Select);
            if (redundant != null)
            {
                proj = (ProjectionExpression)SubqueryRemover.Remove(proj, redundant);
            }
        }
        return proj;
    }

    internal static bool IsSimpleProjection(SelectExpression select)
    {
        foreach (ColumnDeclaration decl in select.Columns)
        {
            if (decl.Expression is not ColumnExpression col || decl.Name != col.Name)
            {
                return false;
            }
        }
        return true;
    }

    internal static bool IsNameMapProjection(SelectExpression select)
    {
        if (select.From is TableExpression) 
            return false;
        
        if (select.From is not SelectExpression fromSelect || select.Columns.Count != fromSelect.Columns.Count)
            return false;
        
        ReadOnlyCollection<ColumnDeclaration> fromColumns = fromSelect.Columns;
        // test that all columns in 'select' are refering to columns in the same position
        // in from.
        for (int i = 0, n = select.Columns.Count; i < n; i++)
        {
            if (select.Columns[i].Expression is not ColumnExpression col || !(col.Name == fromColumns[i].Name))
                return false;
        }
        return true;
    }

    class RedundantSubqueryGatherer : DbExpressionVisitor
    {
        List<SelectExpression>? redundant;
        
        internal static List<SelectExpression>? Gather(Expression source)
        {
            RedundantSubqueryGatherer gatherer = new RedundantSubqueryGatherer();
            gatherer.Visit(source);
            return gatherer.redundant;
        }

        private static bool IsRedudantSubquery(SelectExpression select)
        {
            return (IsSimpleProjection(select) || IsNameMapProjection(select))
                && !select.IsDistinct
                && !select.IsReverse
                && select.Top == null
                //&& select.Skip == null
                && select.Where == null
                && (select.OrderBy.Count == 0)
                && (select.GroupBy.Count == 0);
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            if (IsRedudantSubquery(select))
            {
                if (this.redundant == null)
                {
                    this.redundant = new List<SelectExpression>();
                }
                this.redundant.Add(select);
            }
            return select;
        }

        // don't gather inside scalar & exists
        protected internal override Expression VisitIn(InExpression @in)
        {
            return @in;
        }
        // don't gather inside scalar & exists
        protected internal override Expression VisitScalar(ScalarExpression scalar)
        {
            return scalar;
        }
        // don't gather inside scalar & exists
        protected internal override Expression VisitExists(ExistsExpression exists)
        {
            return exists;
        }

        protected internal override Expression VisitJoin(JoinExpression join)
        {
            var result = (JoinExpression)base.VisitJoin(join);
            if (result.JoinType == JoinType.CrossApply || 
                result.JoinType == JoinType.OuterApply)
            {
                if (Schema.Current.Settings.IsPostgres && this.redundant != null && result.Right is SelectExpression s && this.redundant.Contains(s))
                {
                    if (HasJoins(s))
                        this.redundant.Remove(s);
                }
            }

            return result;
        }

        protected internal override Expression VisitSetOperator(SetOperatorExpression set)
        {
            var result = (SetOperatorExpression)base.VisitSetOperator(set);

            if(this.redundant != null)
            {
                if (result.Left is SelectExpression l)
                    this.redundant.Remove(l);

                if (result.Right is SelectExpression r)
                    this.redundant.Remove(r);
            }

            return result;
        }

        static bool HasJoins(SelectExpression s)
        {
            return s.From is JoinExpression || s.From is SelectExpression s2 && HasJoins(s2);
        }
    }

    

    class SubqueryMerger : DbExpressionVisitor
    {
        private SubqueryMerger()
        {
        }

        internal static Expression Merge(Expression expression)
        {
            return new SubqueryMerger().Visit(expression);
        }

        bool isTopLevel = true;

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            bool wasTopLevel = isTopLevel;
            isTopLevel = false;

            select = (SelectExpression)base.VisitSelect(select);

            // next attempt to merge subqueries that would have been removed by the above
            // logic except for the existence of a where clause
            while (CanMergeWithFrom(select, wasTopLevel))
            {
                SelectExpression fromSelect = GetLeftMostSelect(select.From!)!;

                // remove the redundant subquery
                select = (SelectExpression)SubqueryRemover.Remove(select, new[] { fromSelect });

                // merge where expressions
                Expression? where = select.Where;
                if (fromSelect.Where != null)
                {
                    if (where != null)
                    {
                        where = Expression.And(fromSelect.Where, where.UnNullify());
                    }
                    else
                    {
                        where = fromSelect.Where;
                    }
                }
                var orderBy = select.OrderBy.Count > 0 ? select.OrderBy : fromSelect.OrderBy;
                var groupBy = select.GroupBy.Count > 0 ? select.GroupBy : fromSelect.GroupBy;
                //Expression skip = select.Skip != null ? select.Skip : fromSelect.Skip;
                Expression? top = select.Top ?? fromSelect.Top;
                bool isDistinct = select.IsDistinct | fromSelect.IsDistinct;

                if (where != select.Where
                    || orderBy != select.OrderBy
                    || groupBy != select.GroupBy
                    || isDistinct != select.IsDistinct
                    //|| skip != select.Skip
                    || top != select.Top)
                {
                    select = new SelectExpression(select.Alias, isDistinct, top, select.Columns, select.From, where, orderBy, groupBy, select.SelectOptions);
                }
            }

            return select;
        }

        static bool IsColumnProjection(SelectExpression select)
        {
            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                var cd = select.Columns[i];
                if (!(cd.Expression is ColumnExpression) &&
                    cd.Expression.NodeType != ExpressionType.Constant)
                    return false;
            }
            return true;
        }

        static bool CanMergeWithFrom(SelectExpression select, bool isTopLevel)
        {
            SelectExpression? fromSelect = GetLeftMostSelect(select.From!);
            if (fromSelect == null)
                return false;

            if (!IsColumnProjection(fromSelect))
                return false;

            bool selHasOrderBy = select.OrderBy.Count > 0;
            bool selHasGroupBy = select.GroupBy.Count > 0;

            bool frmHasOrderBy = fromSelect.OrderBy.Count > 0;
            bool frmHasGroupBy = fromSelect.GroupBy.Count > 0;
            // both cannot have orderby
            if (selHasOrderBy && frmHasOrderBy)
                return false;
            // both cannot have groupby
            if (selHasGroupBy && frmHasGroupBy)
                return false;
            // this are distinct operations
            if (select.IsReverse || fromSelect.IsReverse)
                return false;

            // cannot move forward order-by if outer has group-by
            if (frmHasOrderBy && (selHasGroupBy || select.IsDistinct || AggregateChecker.HasAggregates(select)))
                return false;
            // cannot move forward group-by if outer has where clause
            if (frmHasGroupBy /*&& (select.Where != null)*/) // need to assert projection is the same in order to move group-by forward
                return false;

            // cannot move forward a take if outer has take or skip or distinct
            if (fromSelect.Top != null && (select.Top != null || /*select.Skip != null ||*/ select.IsDistinct || selHasGroupBy || HasApplyJoin(select.From!) || select.Where != null))
                return false;
            // cannot move forward a skip if outer has skip or distinct
            //if (fromSelect.Skip != null && (select.Skip != null || select.Distinct || selHasAggregates || selHasGroupBy))
            //    return false;
            // cannot move forward a distinct if outer has take, skip, groupby or a different projection
            if (fromSelect.IsDistinct && (select.Top != null || /*select.Skip != null ||*/ !IsNameMapProjection(select) || selHasGroupBy || (selHasOrderBy && !isTopLevel) || AggregateChecker.HasAggregates(select)))
                return false;
            return true;
        }

        static SelectExpression? GetLeftMostSelect(Expression source)
        {
            if (source is SelectExpression select)
                return select;

            if (source is JoinExpression join && join.JoinType is not (JoinType.RightOuterJoin or JoinType.FullOuterJoin))
                return GetLeftMostSelect(join.Left);

            return null;
        }

        static bool HasApplyJoin(SourceExpression source)
        {
            if (source is not JoinExpression join)
                return false;

            return join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply || HasApplyJoin(join.Left) || HasApplyJoin(join.Right);
        }
    }

    class AggregateChecker : DbExpressionVisitor
    {
        bool hasAggregate = false;
        private AggregateChecker()
        {
        }

        internal static bool HasAggregates(SelectExpression expression)
        {
            AggregateChecker checker = new AggregateChecker();
            checker.Visit(expression);
            return checker.hasAggregate;
        }

        protected internal override Expression VisitAggregate(AggregateExpression aggregate)
        {
            this.hasAggregate = true;
            return aggregate;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            // only consider aggregates in these locations
            this.Visit(select.Where);

            Visit(select.OrderBy, VisitOrderBy);
            Visit(select.Columns, VisitColumnDeclaration);
            return select;
        }

        // don't count aggregates in subqueries
        protected internal override Expression VisitIn(InExpression @in)
        {
            return base.VisitIn(@in);
        }

        // don't count aggregates in subqueries
        protected internal override Expression VisitExists(ExistsExpression exists)
        {
            return base.VisitExists(exists);
        }

        // don't count aggregates in subqueries
        protected internal override Expression VisitScalar(ScalarExpression scalar)
        {
            return base.VisitScalar(scalar);
        }
    }
}

class JoinSimplifier : DbExpressionVisitor
{
    internal static Expression Simplify(Expression expression)
    {
        return new JoinSimplifier().Visit(expression);
    }

    protected internal override Expression VisitJoin(JoinExpression join)
    {
        SourceExpression left = this.VisitSource(join.Left);
        SourceExpression right = this.VisitSource(join.Right);
        Expression? condition = this.Visit(join.Condition);

        if(join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
        {
            if (right is TableExpression)
            {
                return new JoinExpression(join.JoinType == JoinType.OuterApply ? JoinType.LeftOuterJoin : JoinType.InnerJoin, left, right,
                    Schema.Current.Settings.IsPostgres ? (Expression)new SqlConstantExpression(true) :
                    Expression.Equal(new SqlConstantExpression(1), new SqlConstantExpression(1)));
            }
        }

        if (left != join.Left || right != join.Right || condition != join.Condition)
        {
            return new JoinExpression(join.JoinType, left, right, condition);
        }
        return join;
    }
}
