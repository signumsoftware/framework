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
                SelectExpression left = join.Left as SelectExpression;
                SelectExpression right = join.Right as SelectExpression;

                bool leftRemovable = left != null && left.SelectRoles == 0;
                bool rightRemovable = right != null && right.SelectRoles == 0;

                if (leftRemovable || rightRemovable)
                {
                    SelectExpression newSelect = (SelectExpression)SubqueryRemover.Remove(select, new[] { 
                            leftRemovable ? left : null, 
                            rightRemovable ? right: null }.NotNull());
                    return newSelect;
                }
            }

            return select; 
        }
    }   
}