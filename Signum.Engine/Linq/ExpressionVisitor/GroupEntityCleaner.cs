using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    internal class GroupEntityCleaner : DbExpressionVisitor
    {
        public static Expression Clean(Expression source)
        {
            GroupEntityCleaner pc = new GroupEntityCleaner();
            return pc.Visit(source);
        }

        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
                return null;

            if (exp.Type == typeof(Type))
                return VisitType(exp);
            else
                return base.Visit(exp);
        }

        protected override Expression VisitTypeFieldInit(TypeEntityExpression typeFie)
        {
            return base.VisitTypeFieldInit(typeFie);
        }

        private Expression VisitType(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Constant)
                return exp;

            return new TypeImplementedByAllExpression(QueryBinder.ExtractTypeId(exp));
        }

        protected override Expression VisitEntity(EntityExpression fieldInit)
        {
            Expression newID = Visit(fieldInit.ExternalId);

            return new EntityExpression(fieldInit.Type, newID, null, null, null, fieldInit.AvoidExpandOnRetrieving); // remove bindings
        }

        protected override Expression VisitImplementedBy(ImplementedByExpression reference)
        {
            var implementations = reference.Implementations
                .NewIfChange(ri => (EntityExpression)VisitEntity(ri));

            return new ImplementedByExpression(reference.Type, implementations);
        }

        protected override Expression VisitImplementedByAll(ImplementedByAllExpression reference)
        {
            var id = (ColumnExpression)Visit(reference.Id);
            var typeId = (TypeImplementedByAllExpression)Visit(reference.TypeId);

            return new ImplementedByAllExpression(reference.Type, id, typeId);
        }
    }
}
