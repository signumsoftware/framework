using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    class CommandSimplifier : DbExpressionVisitor
    {
        AliasGenerator aliasGenerator;

        public CommandSimplifier(AliasGenerator aliasGenerator)
        {
            this.aliasGenerator = aliasGenerator;
        }

        public static CommandExpression Simplify(CommandExpression ce, bool removeSelectRowCount, AliasGenerator aliasGenerator)
        {
            if (removeSelectRowCount)
                ce = (CommandExpression)new SelectRowRemover().Visit(ce);

            return (CommandExpression)new CommandSimplifier(aliasGenerator).Visit(ce);
        }

        protected internal override Expression VisitDelete(DeleteExpression delete)
        {
            var select = (SelectExpression)delete.Source;

            if (select.From is not TableExpression table || delete.Table != table.Table)
                return delete;

            if (!TrivialWhere(delete, select))
                return delete;

            return new DeleteExpression(delete.Table, delete.UseHistoryTable, table, select.Where, delete.ReturnRowCount);
        }

        private bool TrivialWhere(DeleteExpression delete, SelectExpression select)
        {
            if (select.SelectRoles != 0)
                return false;

            if (delete.Where == null)
                return false;

            if (delete.Where.NodeType != ExpressionType.Equal)
                return false;

            var b = (BinaryExpression)delete.Where;


            if (RemoveConvert(b.Left) is not ColumnExpression ce1 || 
                RemoveConvert(b.Right) is not ColumnExpression ce2)
                return false;

            ce1 = ResolveColumn(ce1, select);
            ce2 = ResolveColumn(ce2, select);

            return ce1.Name == ce2.Name && ce1.Alias.Equals(ce2.Alias);
        }

        private Expression RemoveConvert(Expression expression)
        {
            if (expression is PrimaryKeyExpression pe)
                return RemoveConvert(pe.Value);

            if (expression.NodeType == ExpressionType.Convert)
                return RemoveConvert(((UnaryExpression)expression).Operand);

            return expression;
        }

        private ColumnExpression ResolveColumn(ColumnExpression ce, SelectExpression select)
        {
            if (ce.Alias == select.Alias)
            {
                var cd = select.Columns.SingleEx(a => a.Name == ce.Name);

                if (cd.Expression is not ColumnExpression result)
                    return ce;

                TableExpression table = (TableExpression)select.From!;

                if (table.Alias == result.Alias)
                {
                    return new ColumnExpression(result.Type, aliasGenerator.Table(table.Name), result.Name);
                }

                return result;
            }

            return ce;
        }
    }

    class SelectRowRemover : DbExpressionVisitor
    {   
        protected internal override Expression VisitUpdate(UpdateExpression update)
        {
            if (update.ReturnRowCount == false)
                return update;

            return new UpdateExpression(update.Table, update.UseHistoryTable, update.Source, update.Where, update.Assigments, returnRowCount: false);
        }

        protected internal override Expression VisitInsertSelect(InsertSelectExpression insertSelect)
        {
            if (insertSelect.ReturnRowCount == false)
                return insertSelect;

            return new InsertSelectExpression(insertSelect.Table, insertSelect.UseHistoryTable, insertSelect.Source, insertSelect.Assigments, returnRowCount: false);
        }

        protected internal override Expression VisitDelete(DeleteExpression delete)
        {
            if (delete.ReturnRowCount == false)
                return delete;

            return new DeleteExpression(delete.Table, delete.UseHistoryTable, delete.Source, delete.Where, returnRowCount: false);
        }
    }
}
