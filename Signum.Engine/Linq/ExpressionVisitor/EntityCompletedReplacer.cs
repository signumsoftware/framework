using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    class EntityCompletedReplacer : DbExpressionVisitor
    {
        Dictionary<EntityExpression, EntityExpression> entityReplacements;
        Dictionary<LiteExpression, LiteExpression> liteReplacements;

        HashSet<EntityExpression> entityToRemove = new HashSet<EntityExpression>();
        HashSet<LiteExpression> liteToRemove = new HashSet<LiteExpression>();

        public static Expression Replace(Expression expression,
            Dictionary<EntityExpression, EntityExpression> entityReplacements,
            Dictionary<LiteExpression, LiteExpression> liteReplacements)
        {
            var replacer = new EntityCompletedReplacer
            {
                entityReplacements = entityReplacements,
                liteReplacements = liteReplacements
            };

            var result = replacer.Visit(expression);

            entityReplacements.RemoveRange(replacer.entityToRemove);
            liteReplacements.RemoveRange(replacer.liteToRemove);

            return result;
        }

        protected override Expression VisitEntity(EntityExpression ee)
        {
            var entity = entityReplacements.TryGetC(ee);

            if (entity != null)
                entityToRemove.Remove(ee);
            else
                entity = ee; 

            return base.VisitEntity(ee);
        }

        protected override Expression VisitLite(LiteExpression le)
        {
            var lite = liteReplacements.TryGetC(le);

            if (lite != null)
                liteToRemove.Remove(le);
            else
                lite = le; 

            return base.VisitLite(le);
        }
    }
}
