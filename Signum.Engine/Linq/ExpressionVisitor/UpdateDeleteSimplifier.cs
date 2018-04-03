using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Engine.Maps;

namespace Signum.Engine.Linq
{
    class CommandSimplifier: DbExpressionVisitor
    {
        bool removeSelectRowCount;
        AliasGenerator aliasGenerator;


        public static CommandExpression Simplify(CommandExpression ce, bool removeSelectRowCount, AliasGenerator aliasGenerator)
        {
            return (CommandExpression)new CommandSimplifier { removeSelectRowCount = removeSelectRowCount, aliasGenerator = aliasGenerator }.Visit(ce);
        }

        protected internal override Expression VisitSelectRowCount(SelectRowCountExpression src)
        {
            if (removeSelectRowCount)
                return null;

            return base.VisitSelectRowCount(src);
        }

        protected internal override Expression VisitDelete(DeleteExpression delete)
        {
            var select = delete.Source as SelectExpression;

            TableExpression table = select.From as TableExpression;
            
            if (table == null || delete.Table != table.Table)
                return delete;

            if (!TrivialWhere(delete, select))
                return delete;

            return new DeleteExpression(delete.Table, delete.UseHistoryTable, table, select.Where);
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

            var ce1 = RemoveConvert(b.Left) as ColumnExpression;
            var ce2 = RemoveConvert(b.Right) as ColumnExpression;

            if (ce1 == null || ce2 == null)
                return false;

            ce1 = ResolveColumn(ce1, select);
            ce2 = ResolveColumn(ce2, select);

            return ce1.Name == ce2.Name && ce1.Alias.Equals(ce2.Alias);
        }

        private Expression RemoveConvert(Expression expression)
        {
            if (expression is PrimaryKeyExpression)
                return RemoveConvert(((PrimaryKeyExpression)expression).Value);

            if (expression.NodeType == ExpressionType.Convert)
                return RemoveConvert(((UnaryExpression)expression).Operand);

            return expression;
        }

        private ColumnExpression ResolveColumn(ColumnExpression ce, SelectExpression select)
        {
            if (ce.Alias == select.Alias)
            {
                var cd = select.Columns.SingleEx(a => a.Name == ce.Name);

                var result = cd.Expression as ColumnExpression;

                if(result == null)
                    return ce;

                TableExpression table = (TableExpression)select.From;

                if (table.Alias == result.Alias)
                {
                    return new ColumnExpression(result.Type, aliasGenerator.Table(table.Name), result.Name);
                }

                return result;
            }

            return ce;
        }
    }
}
