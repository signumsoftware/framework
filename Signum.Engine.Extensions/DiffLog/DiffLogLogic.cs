using Signum.Entities.Basics;
using Signum.Entities.DiffLog;
using Signum.Utilities.DataStructures;
using Signum.Engine.Authorization;
using System.Text.RegularExpressions;
using Signum.Entities.Reflection;

namespace Signum.Engine.DiffLog;

public static class DiffLogLogic
{
    public static Polymorphic<Func<IEntity, IOperation, bool>> ShouldLog = new Polymorphic<Func<IEntity, IOperation, bool>>(minimumType: typeof(Entity));

    public static void Start(SchemaBuilder sb, bool registerAll)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            MixinDeclarations.AssertDeclared(typeof(OperationLogEntity), typeof(DiffLogMixin));

            PermissionAuthLogic.RegisterTypes(typeof(TimeMachinePermission));

            OperationLogic.SurroundOperation += OperationLogic_SurroundOperation;

            if (registerAll)
                RegisterShouldLog<Entity>((entity, oper) => true);
        }
    }

    

   

    public static void RegisterShouldLog<T>(Func<IEntity, IOperation, bool> func) where T : Entity
    {
        ShouldLog.SetDefinition(typeof(T), func);
    }

    static IDisposable OperationLogic_SurroundOperation(IOperation operation, OperationLogEntity log, Entity? entity, object?[]? args)
    {
        if (entity != null && ShouldLog.Invoke(entity, operation))
        {
            if (operation.OperationType == OperationType.Execute && !entity.IsNew && ((IEntityOperation)operation).CanBeModified && GraphExplorer.IsGraphModified(entity))
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
