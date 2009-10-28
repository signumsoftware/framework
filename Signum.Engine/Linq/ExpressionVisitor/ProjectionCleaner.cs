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
    internal class ProjectionCleaner : DbExpressionVisitor
    {
        public static Expression Clean(Expression source)
        {
            ProjectionCleaner pc = new ProjectionCleaner();
            return pc.Visit(source);
        }

        protected override Expression VisitLiteReference(LiteReferenceExpression lazy)
        {
            var newToStr = Visit(lazy.ToStr);
            var newId = Visit(lazy.Id);
            var newTypeId = Visit(lazy.TypeId);
            return new LiteReferenceExpression(lazy.Type, null, newId, newToStr, newTypeId);
        }

        protected override Expression VisitFieldInit(FieldInitExpression fieldInit)
        {
            Expression newID = Visit(fieldInit.ExternalId);
            if (newID != fieldInit.ExternalId)
            {
                return new FieldInitExpression(fieldInit.Type, fieldInit.TableAlias, newID, null); // eliminamos los bindings
            }

            fieldInit.Bindings = null;
            fieldInit.PropertyBindings = null;
            return fieldInit;
        }

        protected override Expression VisitEmbeddedFieldInit(EmbeddedFieldInitExpression efie)
        {
            var bindings = efie.Bindings.NewIfChange(fb => Visit(fb.Binding).Map(r => r == fb.Binding ? fb : new FieldBinding(fb.FieldInfo, r)));

            if (efie.Bindings != bindings)
            {
                return new EmbeddedFieldInitExpression(efie.Type, bindings);
            }

            efie.PropertyBindings = null;
            return efie;
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
                return new ImplementedByAllExpression(reference.Type, id, typeId);
            }
            
            reference.Implementations = null;
            return reference;
        }
    }
}
