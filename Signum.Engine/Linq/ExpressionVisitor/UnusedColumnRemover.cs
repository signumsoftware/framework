using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Linq
{
    internal class UnusedColumnRemover : DbExpressionVisitor
    {
        Dictionary<string, HashSet<string>> allColumnsUsed = new Dictionary<string, HashSet<string>>();

        private UnusedColumnRemover() { }

        static internal Expression Remove(Expression expression)
        {
            return new UnusedColumnRemover().Visit(expression);
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            allColumnsUsed.GetOrCreate(column.Alias).Add(column.Name);
            return column;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {   
           // visit column projection first
           HashSet<string> columnsUsed =  allColumnsUsed.GetOrCreate(select.Alias); // a veces no se usa

           ReadOnlyCollection<ColumnDeclaration> columns = select.Columns.NewIfChange(
               c => !columnsUsed.Contains(c.Name)  && !select.Distinct ? null : Visit(c.Expression).Map(ex => ex == c.Expression ? c : new ColumnDeclaration(c.Name, ex))); 

            ReadOnlyCollection<OrderExpression> orderbys = this.VisitOrderBy(select.OrderBy);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<Expression> groupbys = this.VisitGroupBy(select.GroupBy); 
            Expression from = this.Visit(select.From);
            
            if (columns != select.Columns || orderbys != select.OrderBy || where != select.Where || from != select.From|| groupbys != select.GroupBy)
            {
                return new SelectExpression(select.Type, select.Alias, select.Distinct, select.Top, columns, from, where, orderbys, groupbys, null);
            }

            return select;
        }

        protected override Expression VisitLazyReference(LazyReferenceExpression lazy)
        {   
            return base.VisitLazyReference(lazy);
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            // visit mapping in reverse order
            Expression projector = this.Visit(projection.Projector);
            SelectExpression source = (SelectExpression)this.Visit(projection.Source);
            if (projector != projection.Projector || source != projection.Source)
            {
                return new ProjectionExpression(projection.Type, source, projector, projection.UniqueFunction);
            }
            return projection;
        }

        protected override Expression VisitSetOperation(SetOperationExpression setOperationExp)
        {
            if (setOperationExp.SetOperation == SetOperation.Concat)
            {
                // no es una operacion de conjuntos, y por tanto se pueden recortar columnas
                var hashSet = this.allColumnsUsed.TryGetC(setOperationExp.Alias);
                if (hashSet != null)
                {
                    allColumnsUsed.Add(setOperationExp.Left.Alias, hashSet.ToHashSet());
                    allColumnsUsed.Add(setOperationExp.Right.Alias, hashSet.ToHashSet());
                }
            }
            else
            {
                allColumnsUsed.Add(setOperationExp.Left.Alias, setOperationExp.Left.Columns.Select(c=>c.Name).ToHashSet());
                allColumnsUsed.Add(setOperationExp.Right.Alias, setOperationExp.Right.Columns.Select(c => c.Name).ToHashSet());
            }

            SelectExpression left = (SelectExpression)Visit(setOperationExp.Left);
            SelectExpression right = (SelectExpression)Visit(setOperationExp.Right);
            if (setOperationExp.Left != left || setOperationExp.Right != right)
                return new SetOperationExpression(setOperationExp.Type, setOperationExp.Alias, setOperationExp.SetOperation, left, right);

            return setOperationExp;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            if (join.IsSingleRow)
            {
                var table = (TableExpression)join.Right;

                var hs = allColumnsUsed.TryGetC(table.Alias);

                if (hs == null || hs.Count == 0)
                    return Visit(join.Left);
            }


            // visit join in reverse order
            Expression condition = this.Visit(join.Condition);
            Expression right = this.VisitSource(join.Right);
            Expression left = this.VisitSource(join.Left);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.Type, join.JoinType, left, right, condition, join.IsSingleRow);
            }
            return join;

        }
    }
}