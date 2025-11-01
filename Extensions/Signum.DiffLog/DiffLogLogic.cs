using Signum.API;
using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.DiffLog;

namespace Signum.DiffLog;

public static class DiffLogLogic
{
    public static Polymorphic<Func<IEntity, IOperation, bool>> ShouldLog = new Polymorphic<Func<IEntity, IOperation, bool>>(minimumType: typeof(Entity));

    public static void Start(SchemaBuilder sb, bool registerAll)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        MixinDeclarations.AssertDeclared(typeof(OperationLogEntity), typeof(DiffLogMixin));

        TypeConditionLogic.RegisterWhenAlreadyFilteringBy(OperationLogTypeCondition.FilteringByTarget, 
            property: (OperationLogEntity ol) => ol.Target,
            isConstantAuthorized: e => e != null && TypeAuthLogic.IsAllowedFor(e, TypeAllowedBasic.Read, true, FilterQueryArgs.FromLite(e)),
            useInDBForInMemoryCondition: false);

        OperationLogic.SurroundOperation += OperationLogic_SurroundOperation;

        if (registerAll)
            RegisterShouldLog<Entity>((entity, oper) => true);

        if (sb.WebServerBuilder != null)
            DiffLogServer.Start(sb.WebServerBuilder);

    }

    public static void RegisterShouldLog<T>(Func<IEntity, IOperation, bool> func) where T : Entity
    {
        ShouldLog.SetDefinition(typeof(T), func);
    }

    static IDisposable OperationLogic_SurroundOperation(IOperation operation, OperationLogEntity log, Entity? entity, object?[]? args)
    {
        if (entity != null && ShouldLog.Invoke(entity, operation))
        {
            if (operation.OperationType == OperationType.Execute && !entity.IsNew && ((IEntityOperation)operation).CanBeModified && GraphExplorer.IsGraphModifiedVirtual(entity))
                entity = RetrieveFresh(entity);

            using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
            {
                log.Mixin<DiffLogMixin>().InitialState = new BigStringEmbedded(ObjectDumper.Dump(entity));
            }
        }
        else
        {
            log.Mixin<DiffLogMixin>().InitialState = new BigStringEmbedded();
        }

        return new Disposable(() =>
        {
            var target = log.GetTemporalTarget();

            if (target != null && ShouldLog.Invoke(target, operation) && operation.OperationType != OperationType.Delete)
            {
                using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
                {
                    log.Mixin<DiffLogMixin>().FinalState = new BigStringEmbedded(ObjectDumper.Dump(target));
                }
            }
            else
            {
                log.Mixin<DiffLogMixin>().FinalState = new BigStringEmbedded();
            }
        });
    }

    private static Entity RetrieveFresh(Entity entity)
    {
        using (new EntityCache(EntityCacheType.ForceNew))
            return entity.ToLite().RetrieveAndRemember();
    }
}
