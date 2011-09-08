using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    internal class RedundantSubqueryRemover : DbExpressionVisitor
    {

        private RedundantSubqueryRemover() { }

        static internal Expression Remove(Expression expression)
        {
            return new RedundantSubqueryRemover().Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression original)
        {
            var select = (SelectExpression)base.VisitSelect(original);

            SelectExpression fromSelect = select.From as SelectExpression;
            if (fromSelect != null)
            {
                SelectRoles selectRole = select.SelectRoles;
                SelectRoles minSelect = (SelectRoles)EnumExtensions.MinFlag((int)selectRole); 

                SelectRoles fromSelectRole = fromSelect.SelectRoles;
                SelectRoles maxSelect = (SelectRoles)EnumExtensions.MaxFlag((int)fromSelectRole);

                if (selectRole == 0 || minSelect > maxSelect || minSelect == maxSelect && minSelect == SelectRoles.Where)
                {
                    SelectExpression newSelect = (SelectExpression)SubqueryRemover.Remove(select, new[] { fromSelect });

                    var distinct = (selectRole & SelectRoles.Distinct) != 0 ? newSelect.Distinct : fromSelect.Distinct;
                    var top = (selectRole & SelectRoles.Top) != 0 ? newSelect.Top : fromSelect.Top;
                    var where = minSelect == maxSelect && minSelect == SelectRoles.Where ? Expression.And(newSelect.Where, fromSelect.Where) :
                                (selectRole & SelectRoles.Where) != 0 ? newSelect.Where : fromSelect.Where;
                    var groupBy = (selectRole & SelectRoles.GroupBy) != 0 ? newSelect.GroupBy : fromSelect.GroupBy;
                    var orderBy = (selectRole & SelectRoles.OrderBy) != 0 ? newSelect.OrderBy : fromSelect.OrderBy;

                    return new SelectExpression(newSelect.Alias, distinct, top, newSelect.Columns, newSelect.From, where, orderBy, groupBy);
                }
            }

            JoinExpression join = select.From as JoinExpression;
            if (join != null)
            {
                List<SelectExpression> toRemove = new List<SelectExpression>();
                
                GatherRedundantSelects(join.Left, toRemove);
                GatherRedundantSelects(join.Right, toRemove);

                if (toRemove.Count > 0)
                {
                    SelectExpression newSelect = (SelectExpression)SubqueryRemover.Remove(select, toRemove);
                    return newSelect;
                }
            }

            return select; 
        }

        private static void GatherRedundantSelects(SourceExpression source, List<SelectExpression> toRemove)
        {
            SelectExpression select = source as SelectExpression;
            if (select != null && select.SelectRoles == 0)
                toRemove.Add(select); 

            JoinExpression join = source as JoinExpression;
            if(join != null)
            {
                GatherRedundantSelects(join.Left, toRemove);
                GatherRedundantSelects(join.Right, toRemove);
            }
        }

        protected override Expression VisitDelete(DeleteExpression original)
        {
            var delete = (DeleteExpression)base.VisitDelete(original);

            SelectExpression select = delete.Source as SelectExpression;
            if (select != null && select.SelectRoles == 0)
            {
                var result =  (DeleteExpression)SubqueryRemover.Remove(delete, new[] { select });
                TableExpression table = result.Source as TableExpression;
                if (table != null && table.Name == result.Table.Name)
                    return new DeleteExpression(result.Table, result.Source, null); //remove where cos SQL is Orks language
            }

            return delete;
        }

        protected override Expression VisitUpdate(UpdateExpression original)
        {
            var update = (UpdateExpression)base.VisitUpdate(original);

            SelectExpression select = update.Source as SelectExpression;
            if (select != null && select.SelectRoles == 0)
            {
                var result = (UpdateExpression)SubqueryRemover.Remove(update, new[] { select });
                TableExpression table = result.Source as TableExpression;
                if (table != null && table.Name == result.Table.Name)
                    return new UpdateExpression(result.Table, result.Source, null, result.Assigments); //remove where cos SQL is Orks language
            }

            return update;
        }
    }   
}