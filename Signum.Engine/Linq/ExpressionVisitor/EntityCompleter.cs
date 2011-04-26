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
using Signum.Utilities.DataStructures;

namespace Signum.Engine.Linq
{
    internal class EntityCompleter : DbExpressionVisitor
    {
        BinderTools tools;
        ImmutableStack<Type> previousTypes = ImmutableStack<Type>.Empty; 

        public static Expression Clean(Expression source, BinderTools tools)
        {
            EntityCompleter pc = new EntityCompleter(){ tools = tools};
            return pc.Visit(source);
        }

        protected override Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            var newToStr = Visit(lite.ToStr);
            var newId = Visit(lite.Id);
            var newTypeId = Visit(lite.TypeId);
            return new LiteReferenceExpression(lite.Type, null, newId, newToStr, newTypeId);
        }

        protected override Expression VisitFieldInit(FieldInitExpression fie)
        {
            fie = new FieldInitExpression(fie.Type, fie.TableAlias, fie.ExternalId, null, fie.Token) { Bindings = fie.Bindings.ToList() };

            if (previousTypes.Contains(fie.Type))
                fie.Bindings.Clear();
            else
                fie.Complete(tools);

            previousTypes = previousTypes.Push(fie.Type);

            var bindings = fie.Bindings.NewIfChange(fb => Visit(fb.Binding).Map(r => r == fb.Binding ? fb : new FieldBinding(fb.FieldInfo, r)));

            var id = Visit(fie.ExternalId);

            var token = VisitProjectionToken(fie.Token);

            var result = new FieldInitExpression(fie.Type, fie.TableAlias, id, null, token) { Bindings = bindings };

            previousTypes = previousTypes.Pop();

            return result;
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

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            var oldList = previousTypes;

            previousTypes = ImmutableStack<Type>.Empty;

            Expression projector = this.Visit(proj.Projector);

            var result = new ProjectionExpression(proj.Source, projector, proj.UniqueFunction, proj.Token, proj.Type);

            var expanded = tools.ApplyExpansions(result);

            previousTypes = oldList; 

            return expanded;
        }
    }
}
