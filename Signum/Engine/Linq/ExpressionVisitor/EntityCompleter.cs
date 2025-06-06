using Signum.Engine.Maps;
using System.Collections.ObjectModel;
using System.Collections.Immutable;

namespace Signum.Engine.Linq;

internal class EntityCompleter : DbExpressionVisitor
{
    readonly QueryBinder binder;
    ImmutableStack<Type> previousTypes = ImmutableStack<Type>.Empty;

    public EntityCompleter(QueryBinder binder)
    {
        this.binder = binder;
    }

    public static Expression Complete(Expression source, QueryBinder binder)
    {
        EntityCompleter pc = new EntityCompleter(binder);

        var result = pc.Visit(source);

        var expandedResul = QueryJoinExpander.ExpandJoins(result, binder, cleanRequests: true, binder.systemTime);

        return expandedResul;
    }

    protected internal override Expression VisitLiteReference(LiteReferenceExpression lite)
    {
        if (lite.EagerEntity)
            return base.VisitLiteReference(lite);

        var id = binder.GetId(lite.Reference);

        var typeId = binder.GetEntityType(lite.Reference);

        var partitionIds = GetPartitionIds(lite);

        if (lite.CustomModelExpression != null)
            return new LiteValueExpression(lite.Type, typeId, id, lite.CustomModelExpression, null, partitionIds?.ToReadOnly());

        var models = GetModels(lite);

        //var model2 = Visit(model); //AdditionalBinding in embedded requires it, but makes problems in many other lites in Nominator

        return new LiteValueExpression(lite.Type, typeId, id, null, models?.ToReadOnly(), partitionIds?.ToReadOnly());
    }

    private Dictionary<Type, ExpressionOrType>? GetModels(LiteReferenceExpression lite)
    {
        if (lite.Reference is ImplementedByAllExpression iba)
        {
            if (lite.CustomModelTypes != null)
                return lite.CustomModelTypes.ToDictionary(a => a.Key, a => new ExpressionOrType(a.Value));

            return null;
        }

        if (lite.Reference is EntityExpression entityExp)
        {
            var modelType = lite.CustomModelTypes?.TryGetC(entityExp.Type) ?? Lite.DefaultModelType(entityExp.Type);

            return new Dictionary<Type, ExpressionOrType>
            {
                { entityExp.Type, lite.LazyModel  || entityExp.AvoidExpandOnRetrieving ? new ExpressionOrType(modelType) : new ExpressionOrType(GetModel(entityExp, modelType)) }
            };
        }

        if (lite.Reference is ImplementedByExpression ibe)
        {
            return ibe.Implementations.Values.ToDictionary(imp => imp.Type,
                imp =>
                {
                    var modelType = lite.CustomModelTypes?.TryGetC(imp.Type) ?? Lite.DefaultModelType(imp.Type);
                    return lite.LazyModel || imp.AvoidExpandOnRetrieving ? new ExpressionOrType(modelType) : new ExpressionOrType(GetModel(imp, modelType));
                });
        }

        return new Dictionary<Type, ExpressionOrType>(); //Could be more accurate to preserve model in liteA ?? liteB  or condition ? liteA : liteB 
    }

    private Dictionary<Type, Expression>? GetPartitionIds(LiteReferenceExpression lite)
    {
        if (lite.LazyModel)
            return null;

        if (lite.Reference is ImplementedByAllExpression iba)
        {
            return null;
        }

        if (lite.Reference is EntityExpression entityExp)
        {
            var modelType = lite.CustomModelTypes?.TryGetC(entityExp.Type) ?? Lite.DefaultModelType(entityExp.Type);

            if (entityExp.AvoidExpandOnRetrieving)
                return null;

            var partition = entityExp.GetPartitionId();

            if (partition != null)
                return new Dictionary<Type, Expression>
                {
                    { entityExp.Type,  partition}
                };

            return null;
        }

        if (lite.Reference is ImplementedByExpression ibe)
        {
            if (lite.LazyModel)
                return null;

            return (from imp in ibe.Implementations.Values
                    where !imp.AvoidExpandOnRetrieving
                    let pid = imp.GetPartitionId()
                    where pid != null
                    select KeyValuePair.Create(imp.Type, pid)).ToDictionary();
        }

        return new Dictionary<Type, Expression>(); //Could be more accurate to preserve model in liteA ?? liteB  or condition ? liteA : liteB 
    }

    private Expression GetModel(EntityExpression entityExp, Type modelType)
    {
        //if (modelType == typeof(string))
        //    return binder.BindMethodCall(Expression.Call(entityExp, EntityExpression.ToStringMethod));

        var mce = Lite.GetModelConstructorExpression(entityExp.Type, modelType);

        var bound = binder.Visit(Expression.Invoke(mce, entityExp));

        return Visit(bound);
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

        var bindings = VisitBindings(ee.Bindings!);

        var mixins = Visit(ee.Mixins!, VisitMixinEntity);

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
        var mixins = eee.Mixins == null ? null : Visit(eee.Mixins, VisitMixinEntity);
        var hasValue = Visit(eee.HasValue);

        if (eee.Bindings != bindings || eee.HasValue != hasValue || eee.EntityContext != null)
        {
            return new EmbeddedEntityExpression(eee.Type, hasValue, bindings, mixins, eee.FieldEmbedded, eee.ViewTable, null);
        }
        return eee;
    }

    protected internal override MixinEntityExpression VisitMixinEntity(MixinEntityExpression me)
    {
        var bindings = VisitBindings(me.Bindings);

        if (me.Bindings != bindings || me.EntityContext != null)
        {
            return new MixinEntityExpression(me.Type, bindings, me.MainEntityAlias, me.FieldMixin, null);
        }
        return me;
    }

    private static bool IsCached(Type type)
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

        var newEx = this.Visit(exp)!;

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
