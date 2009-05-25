using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Utilities;
using System.Diagnostics;
using Signum.Utilities.DataStructures;

namespace Signum.Engine.Linq
{
    internal static class AggregateOptimizer
    {
        public static ProjectionExpression Optimize(ProjectionExpression pe)
        {
            var dic = AggregateProjectorFinder.Find(pe.Projector).ToDictionary(a => a, a => (ColumnExpression)null);
            var newSelect = VisitSelect(pe.Source, dic);
            var newProj =  AggregateProjectionReplacer.Replace(pe.Projector, dic);

            if (pe.Source != newSelect || pe.Projector != newProj)
                return new ProjectionExpression(newSelect, newProj, pe.UniqueFunction);
            return pe;
        }

        static Expression VisitFrom(Expression expression, Dictionary<ProjectionExpression, ColumnExpression> requests)
        {
            return new Switch<Expression, Expression>(expression)
            .Case<SelectExpression>(e => VisitSelect(e, requests))
            .Case<JoinExpression>(e => VisitJoin(e, requests))
            .Case<TableExpression>(e => VisitTable(e, requests))
            .Case<SetOperationExpression>(e => VisitSetOperation(e, requests))
            .Default((Expression)null);
        }

        /// <summary>
        ///                    Req. Anteriores       Req. Mias
        ///  Resuelvo Yo         requests            --------   
        ///  Resuelve Otro      (requests            myRequests) --> request for others
        /// </summary>
        static SelectExpression VisitSelect(SelectExpression select, Dictionary<ProjectionExpression, ColumnExpression> requests)
        {
            Debug.Assert(select.GroupOf == null);

            var myReq = new HashSet<ProjectionExpression>();
            myReq.UnionWith(AggregateProjectorFinder.Find(select.Where));
            myReq.UnionWith(select.OrderBy.TryCC(c => c.SelectMany(g => AggregateProjectorFinder.Find(g.Expression))) ?? new HashSet<ProjectionExpression>());
            myReq.UnionWith(select.GroupBy.TryCC(c => c.SelectMany(g => AggregateProjectorFinder.Find(g))) ?? new HashSet<ProjectionExpression>());
            myReq.UnionWith(select.Columns.SelectMany(cd => AggregateProjectorFinder.Find(cd.Expression)));

            var myRequests = myReq.ToDictionary(a => a, a => (ColumnExpression)null);

            var requestForOthers = requests.Union(myRequests).Extract(p=> ((SelectExpression)p.Source.From).GroupOf != select.Alias);

            Expression newFrom = VisitFrom(select.From, requestForOthers);

            List<ColumnDeclaration> newColumns = new List<ColumnDeclaration>(select.Columns);
            foreach (var key in requests.Keys.ToList())
            {
                ColumnDeclaration cd = requestForOthers.TryGetC(key).TryCC(ce => new ColumnDeclaration(ce.Name, ce)) ??
                                     ((SelectExpression)SubqueryRemover.Remove(key.Source, new[] { (SelectExpression)key.Source.From })).Columns.Single();
                newColumns.Add(cd);
                requests[key]= new ColumnExpression(cd.Expression.Type, select.Alias, cd.Name);
            }
           
            Expression newWhere = AggregateProjectionReplacer.Replace(select.Where, requestForOthers);
            ReadOnlyCollection<OrderExpression> newOrderBys = select.OrderBy.NewIfChange(o => AggregateProjectionReplacer.Replace(o.Expression, requestForOthers).Map(e => e == o.Expression ? o : new OrderExpression(o.OrderType, e)));
            ReadOnlyCollection<Expression> newGroupBys = select.GroupBy.NewIfChange(g => AggregateProjectionReplacer.Replace(g, requestForOthers));

            ReadOnlyCollection<ColumnDeclaration> newNewColums = newColumns.Count == select.Columns.Count ? select.Columns : newColumns.ToReadOnly();
            newNewColums = newNewColums.NewIfChange(c => AggregateProjectionReplacer.Replace(c.Expression, requestForOthers).Map(e => e == c.Expression ? c : new ColumnDeclaration(c.Name, e)));

            if (select.Columns != newNewColums || newFrom != select.From || select.Where != newWhere || select.OrderBy != newOrderBys || select.GroupBy != newGroupBys)
            {
                return new SelectExpression(select.Type, select.Alias, false, null, newNewColums, newFrom, newWhere, newOrderBys, newGroupBys, select.GroupOf);
            }
            return select;
        }

        static JoinExpression VisitJoin(JoinExpression join, Dictionary<ProjectionExpression, ColumnExpression> requests)
        {
            HashSet<string> leftAliases = AliasGatherer.Gather(join.Left); 
            HashSet<string> rightAliases = AliasGatherer.Gather(join.Right);

            var myRequests = AggregateProjectorFinder.Find(join.Condition).ToDictionary(a=>a, a=>(ColumnExpression)null);
            var totalRequests = requests.Union(myRequests);

            var leftRequests = totalRequests.Extract(a => leftAliases.Contains(((SelectExpression)a.Source.From).GroupOf));
            var rightRequests = totalRequests.Extract(a => rightAliases.Contains(((SelectExpression)a.Source.From).GroupOf)); 

            Debug.Assert(totalRequests.Count == 0);

            Expression newLeft = VisitFrom(join.Left, leftRequests);
            var newRight = VisitFrom(join.Right, rightRequests);

            foreach (var key in requests.Keys.ToList())
            {
                requests[key] = leftRequests.TryGetC(key) ?? rightRequests.TryGetC(key);
            }

            totalRequests = leftRequests.Union(rightRequests); 

            var newCondition = AggregateProjectionReplacer.Replace(join.Condition, totalRequests);

            if (newLeft != join.Left || newRight != join.Right || newCondition != join.Condition)
            {
                return new JoinExpression(join.Type, join.JoinType, newLeft, newRight, newCondition, false); 
            }
            return join;
        }

        static SetOperationExpression VisitSetOperation(SetOperationExpression setOperation, Dictionary<ProjectionExpression, ColumnExpression> requests)
        {
            Debug.Assert(requests.Count == 0);
            return setOperation; 
        }

        static TableExpression VisitTable(TableExpression table, Dictionary<ProjectionExpression, ColumnExpression> requests)
        {
            Debug.Assert(requests.Count == 0);
            return table;
        }


    }

    class AggregateProjectorFinder : DbExpressionVisitor
    {
        HashSet<ProjectionExpression> aggregateProjections = new HashSet<ProjectionExpression>();

        internal static HashSet<ProjectionExpression> Find(Expression expression)
        {
            AggregateProjectorFinder apf = new AggregateProjectorFinder();
            apf.Visit(expression);
            return apf.aggregateProjections;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            if (proj.IsOneCell && (proj.Source.Columns.Single().Expression is AggregateExpression) &&
                    ((proj.Source.From as SelectExpression).TryCS(c => c.GroupOf != null) ?? false))
            {
                aggregateProjections.Add(proj);
            }

            return base.VisitProjection(proj);
        }
    }

    class AggregateProjectionReplacer : DbExpressionVisitor
    {
        Dictionary<ProjectionExpression, ColumnExpression> aggregateProjections = new Dictionary<ProjectionExpression, ColumnExpression>();

        internal static Expression Replace(Expression expression, Dictionary<ProjectionExpression, ColumnExpression> projections)
        {
            AggregateProjectionReplacer apf = new AggregateProjectionReplacer() { aggregateProjections = projections };
            return apf.Visit(expression);
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            return aggregateProjections.TryGetC(proj) ?? base.VisitProjection(proj);
        }
    }

}

