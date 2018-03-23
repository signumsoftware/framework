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
using System.Collections.ObjectModel;

namespace Signum.Engine.Linq
{
    internal class EntityCompleter : DbExpressionVisitor
    {
        QueryBinder binder;
        ImmutableStack<Type> previousTypes = ImmutableStack<Type>.Empty; 

        public static Expression Complete(Expression source, QueryBinder binder)
        {
            EntityCompleter pc = new EntityCompleter() { binder = binder };
            
            var result = pc.Visit(source);

            var expandedResul = QueryJoinExpander.ExpandJoins(result, binder, cleanRequests: true);

            return expandedResul;
        }

        protected internal override Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            if (lite.EagerEntity)
                return base.VisitLiteReference(lite);

            var id = lite.Reference is ImplementedByAllExpression || 
                lite.Reference is ImplementedByExpression && ((ImplementedByExpression)lite.Reference).Implementations.Select(imp=>imp.Value.ExternalId.ValueType.Nullify()).Distinct().Count() > 1 ?
                (Expression)binder.GetIdString(lite.Reference) :
                (Expression)binder.GetId(lite.Reference);

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

            if (lite.LazyToStr)
                return null;

            if (IsCacheable(typeId))
                return null;

            if (lite.Reference is EntityExpression entityExp)
            {
                if (entityExp.AvoidExpandOnRetrieving)
                    return null;

                return binder.BindMethodCall(Expression.Call(entityExp, EntityExpression.ToStringMethod));
            }

            if (lite.Reference is ImplementedByExpression ibe)
            {
                if (ibe.Implementations.Any(imp => imp.Value.AvoidExpandOnRetrieving))
                    return null;

                return ibe.Implementations.Values.Select(ee =>
                    new When(SmartEqualizer.NotEqualNullable(ee.ExternalId, QueryBinder.NullId(ee.ExternalId.ValueType)),
                     binder.BindMethodCall(Expression.Call(ee, EntityExpression.ToStringMethod)))
                     ).ToCondition(typeof(string));
            }

            return binder.BindMethodCall(Expression.Call(lite.Reference, EntityExpression.ToStringMethod));
        }

        private bool IsCacheable(Expression newTypeId)
        {

            if (newTypeId is TypeEntityExpression tfie)
                return IsCached(tfie.TypeValue);


            if (newTypeId is TypeImplementedByExpression tibe)
                return tibe.TypeImplementations.All(t => IsCached(t.Key));

            return false;
        }

        protected internal override Expression VisitEntity(EntityExpression ee)
        {
            if (previousTypes.Contains(ee.Type) || IsCached(ee.Type) || ee.AvoidExpandOnRetrieving)
            {
                ee = new EntityExpression(ee.Type, ee.ExternalId, null, null, null, null, null /*ee.SystemPeriod TODO*/ , ee.AvoidExpandOnRetrieving);
            }
            else
                ee = binder.Completed(ee);

            previousTypes = previousTypes.Push(ee.Type);

            var bindings = VisitBindings(ee.Bindings);

            var mixins = Visit(ee.Mixins, VisitMixinEntity);

            var id = (PrimaryKeyExpression)Visit(ee.ExternalId);

            var result = new EntityExpression(ee.Type, id, ee.ExternalPeriod, ee.TableAlias, bindings, mixins, ee.TablePeriod, ee.AvoidExpandOnRetrieving);

            previousTypes = previousTypes.Pop();

            return result;
        }

        private ReadOnlyCollection<FieldBinding> VisitBindings(ReadOnlyCollection<FieldBinding> bindings)
        {
            return bindings.Select(b =>
            {
                var newB = Visit(b.Binding);

                if (newB != null)
                    return new FieldBinding(b.FieldInfo, newB);

                return null;
            }).NotNull().ToReadOnly();
        }

        protected internal override Expression VisitEmbeddedEntity(EmbeddedEntityExpression eee)
        {
            var bindings = VisitBindings(eee.Bindings);
            var hasValue = Visit(eee.HasValue);

            if (eee.Bindings != bindings || eee.HasValue != hasValue)
            {
                return new EmbeddedEntityExpression(eee.Type, hasValue, bindings, eee.FieldEmbedded, eee.ViewTable);
            }
            return eee;
        }

        protected internal override MixinEntityExpression VisitMixinEntity(MixinEntityExpression me)
        {
            var bindings = VisitBindings(me.Bindings);

            if (me.Bindings != bindings)
            {
                return new MixinEntityExpression(me.Type, bindings, me.MainEntityAlias, me.FieldMixin);
            }
            return me;
        }

        private bool IsCached(Type type)
        { 
            var cc = Schema.Current.CacheController(type);
            if (cc != null && cc.Enabled)
            {
                cc.Load(); /*just to force cache before executing the query*/
                return true;
            }
            return false;
        }

        protected internal override Expression VisitMList(MListExpression ml)
        {
            var proj = binder.MListProjection(ml, withRowId: true);

            var newProj = (ProjectionExpression)this.Visit(proj);

            return new MListProjectionExpression(ml.Type, newProj);
        }

        protected internal override Expression VisitAdditionalField(AdditionalFieldExpression afe)
        {
            var exp = binder.BindAdditionalField(afe, entityCompleter: true);

            var newEx = this.Visit(exp);

            if (newEx is ProjectionExpression newProj && newProj.Projector.Type.IsInstantiationOf(typeof(MList<>.RowIdElement)))
                return new MListProjectionExpression(afe.Type, newProj);

            return newEx;
        }

        protected internal override Expression VisitProjection(ProjectionExpression proj)
        {
            Expression projector;
            SelectExpression select = proj.Select;
            using (binder.SetCurrentSource(proj.Select))
                projector = this.Visit(proj.Projector);

            Alias alias = binder.aliasGenerator.NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projector, alias);
            projector = pc.Projector;

            select = new SelectExpression(alias, false, null, pc.Columns, select, null, null, null, 0);

            if (projector != proj.Projector)
                return new ProjectionExpression(select, projector, proj.UniqueFunction, proj.Type);

            return proj;
        }
    }
}
