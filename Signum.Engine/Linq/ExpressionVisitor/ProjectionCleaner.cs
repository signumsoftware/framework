using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Signum.Entities;
using Signum.Utilities;
using System.Diagnostics;
using Signum.Entities.Reflection;
using Signum.Engine.Maps;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Linq
{
    internal class EntityCleaner : DbExpressionVisitor
    {
        public static Expression Clean(Expression source)
        {
            EntityCleaner pc = new EntityCleaner();
            return pc.Visit(source);
        }

        protected override Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            var newToStr = Visit(lite.ToStr);
            var newId = Visit(lite.Id);
            var newTypeId = Visit(lite.TypeId);
            return new LiteReferenceExpression(lite.Type, null, newId, newToStr, newTypeId);
        }

        protected override Expression VisitFieldInit(FieldInitExpression fieldInit)
        {
            Expression newID = Visit(fieldInit.ExternalId);

            return new FieldInitExpression(fieldInit.Type, fieldInit.TableAlias, newID, null, null, null); // eliminamos los bindings
        }

        protected override Expression VisitImplementedBy(ImplementedByExpression reference)
        {
            var implementations = reference.Implementations
                .NewIfChange(ri => Visit(ri.Field).Map(r => r == ri.Field ? ri : new ImplementationColumnExpression(ri.Type, (FieldInitExpression)r)));

            if (implementations != reference.Implementations)
                return new ImplementedByExpression(reference.Type, implementations);

            reference.PropertyBindings = null;
            return reference;
        }

        protected override Expression VisitImplementedByAll(ImplementedByAllExpression reference)
        {
            var id = (ColumnExpression)Visit(reference.Id);
            var typeId = (ColumnExpression)Visit(reference.TypeId);

            if (id != reference.Id || typeId != reference.TypeId)
            {
                return new ImplementedByAllExpression(reference.Type, id, typeId, null);
            }
            
            reference.Implementations = null;
            return reference;
        }
    }

    internal class GroupEntityCleaner : DbExpressionVisitor
    {
        public static Expression Clean(Expression source)
        {
            GroupEntityCleaner pc = new GroupEntityCleaner();
            return pc.Visit(source);
        }

        protected override Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            var newId = Visit(lite.Id);
            var newTypeId = Visit(lite.TypeId);
            var reference = Visit(lite.Reference);
            return new LiteReferenceExpression(lite.Type, reference, newId, null, newTypeId);
        }

        protected override Expression VisitFieldInit(FieldInitExpression fieldInit)
        {
            Expression newID = Visit(fieldInit.ExternalId);

            return new FieldInitExpression(fieldInit.Type, null, newID, null, null, fieldInit.Token); // eliminamos los bindings
        }

        protected override Expression VisitImplementedBy(ImplementedByExpression reference)
        {
            var implementations = reference.Implementations
                .NewIfChange(ri => Visit(ri.Field).Map(r => r == ri.Field ? ri : new ImplementationColumnExpression(ri.Type, (FieldInitExpression)r)));

            return new ImplementedByExpression(reference.Type, implementations);
        }

        protected override Expression VisitImplementedByAll(ImplementedByAllExpression reference)
        {
            var id = (ColumnExpression)Visit(reference.Id);
            var typeId = (ColumnExpression)Visit(reference.TypeId);

            return new ImplementedByAllExpression(reference.Type, id, typeId, reference.Token);
        }
    }
}
