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
            var id = binder.GetId(lite.Reference);
            var typeId = binder.GetEntityType(lite.Reference);
            var toStr = LiteToString(lite, typeId);

            return new LiteValueExpression(lite.Type, typeId, id, toStr);
        }

        private Expression LiteToString(LiteReferenceExpression lite, Expression typeId)
        {
            if (lite.CustomToStr != null)
                return Visit(lite.CustomToStr);

            if (lite.Reference is ImplementedByAllExpression)
                return null;

            if (IsCacheable(typeId))
                return null;

            return binder.BindMethodCall(Expression.Call(lite.Reference, EntityExpression.ToStringMethod));
        }

        private bool IsCacheable(Expression newTypeId)
        {
            TypeEntityExpression tfie= newTypeId as TypeEntityExpression;

            if (tfie != null)
                return IsCached(tfie.TypeValue);

            TypeImplementedByExpression tibe = newTypeId as TypeImplementedByExpression;

            if (tibe != null)
                return tibe.TypeImplementations.All(t => IsCached(t.Type));

            return false;
        }

        protected override Expression VisitEntity(EntityExpression ee)
        {
            if (previousTypes.Contains(ee.Type) || IsCached(ee.Type))
            {
                ee = new EntityExpression(ee.Type, ee.ExternalId, null, null);
            }
            else
                ee = binder.Completed(ee);

            previousTypes = previousTypes.Push(ee.Type);

            var bindings = ee.Bindings.NewIfChange(VisitFieldBinding);

            var id = Visit(ee.ExternalId);

            var result = new EntityExpression(ee.Type, id, ee.TableAlias, bindings);

            previousTypes = previousTypes.Pop();

            return result;
        }

        private bool IsCached(Type type)
        { 
            var cc = Schema.Current.CacheController(type);
            return cc != null && cc.Enabled; /*just to force cache before executing the query*/
        }

        protected override Expression VisitMList(MListExpression ml)
        {
            var proj = binder.MListProjection(ml);

            var newProj = (ProjectionExpression)this.Visit(proj);

            return new MListProjectionExpression(ml.Type, newProj);
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            Expression projector = this.Visit(proj.Projector);

            var result = new ProjectionExpression(proj.Select, projector, proj.UniqueFunction, proj.Type);

            var expanded = binder.ApplyExpansionsProjection(result);

            return expanded;
        }
    }
}
