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
        QueryBinder binder;
        ImmutableStack<Type> previousTypes = ImmutableStack<Type>.Empty; 

        public static Expression Complete(Expression source, QueryBinder binder)
        {
            EntityCompleter pc = new EntityCompleter() { binder = binder };
            return pc.Visit(source);
        }

        protected override Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            var newId = Visit(lite.Id);
            var newTypeId = Visit(lite.TypeId);

            var newToStr = !lite.CustomToString && IsCacheable(newTypeId) ? null : Visit(lite.ToStr);

            return new LiteReferenceExpression(lite.Type, null, newId, newToStr, newTypeId, lite.CustomToString);
        }

        private bool IsCacheable(Expression newTypeId)
        {
            TypeFieldInitExpression tfie= newTypeId as TypeFieldInitExpression;

            if (tfie != null)
                return IsCompletlyCached(tfie.TypeValue);

            TypeImplementedByExpression tibe = newTypeId as TypeImplementedByExpression;

            if (tibe != null)
                return tibe.TypeImplementations.All(t => IsCompletlyCached(t.Type));

            return false;
        }

        protected override Expression VisitFieldInit(FieldInitExpression fie)
        {
            fie = new FieldInitExpression(fie.Type, fie.TableAlias, fie.ExternalId, fie.Token) { Bindings = fie.Bindings.ToList() };

            if (previousTypes.Contains(fie.Type) || IsCompletlyCached(fie.Type))
            {
                fie.Bindings.Clear();
                fie.TableAlias = null;
            }
            else
                fie.Complete(binder);

            previousTypes = previousTypes.Push(fie.Type);

            var bindings = fie.Bindings.NewIfChange(fb => Visit(fb.Binding).Map(r => r == fb.Binding ? fb : new FieldBinding(fb.FieldInfo, r)));

            var id = Visit(fie.ExternalId);

            var token = VisitProjectionToken(fie.Token);

            var result = new FieldInitExpression(fie.Type, fie.TableAlias, id, token) { Bindings = bindings };

            previousTypes = previousTypes.Pop();

            return result;
        }

        private bool IsCompletlyCached(Type type)
        { 
            var cc = Schema.Current.CacheController(type);
            return cc != null && cc.Enabled && cc.IsComplete; /*just to force cache before executing the query*/
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
            var typeId = (TypeImplementedByAllExpression)Visit(reference.TypeId);

            if (id != reference.Id || typeId != reference.TypeId)
            {
                return new ImplementedByAllExpression(reference.Type, id, typeId, null);
            }
            
            reference.Implementations = null;
            return reference;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            Expression projector = this.Visit(proj.Projector);

            var result = new ProjectionExpression(proj.Select, projector, proj.UniqueFunction, proj.Token, proj.Type);

            var expanded = binder.ApplyExpansions(result);

            return expanded;
        }
    }
}
