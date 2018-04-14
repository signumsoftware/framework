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

        public override Expression Visit(Expression exp)
        {
            if (exp == null)
                return null;

            if (exp.Type == typeof(Type))
                return VisitType(exp);
            else
                return base.Visit(exp);
        }

        protected internal override Expression VisitTypeEntity(TypeEntityExpression typeFie)
        {
            return base.VisitTypeEntity(typeFie);
        }

        private Expression VisitType(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Constant)
                return exp;

            return new TypeImplementedByAllExpression(QueryBinder.ExtractTypeId(exp));
        }

        protected internal override Expression VisitEntity(EntityExpression entity)
        {
            var newID = (PrimaryKeyExpression)Visit(entity.ExternalId);

            return new EntityExpression(entity.Type, newID, null, null, null, null, null, entity.AvoidExpandOnRetrieving); // remove bindings
        }
    }
}
