using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DiffLog;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Engine.Basics;
using System.Threading;
using Signum.Engine.Authorization;

namespace Signum.Engine.DiffLog
{
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

                ExceptionLogic.DeleteLogs += DiffLogic_CleanLogs;
            }
        }

        public static void RegisterShouldLog<T>(Func<IEntity, IOperation, bool> func) where T : Entity
        {
            ShouldLog.SetDefinition(typeof(T), func);
        }

        static IDisposable OperationLogic_SurroundOperation(IOperation operation, OperationLogEntity log, Entity entity, object[] args)
        {
            if (entity != null && ShouldLog.Invoke(entity, operation))
            {
                if (operation.OperationType == OperationType.Execute && !entity.IsNew && ((IEntityOperation)operation).CanBeModified)
                    entity = RetrieveFresh(entity);

                using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
                {
                    log.Mixin<DiffLogMixin>().InitialState = entity.Dump();
                }
            }

            return new Disposable(() =>
            {
                var target = log.GetTarget();

                if (target != null && ShouldLog.Invoke(target, operation) && operation.OperationType != OperationType.Delete)
                {
                    using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
                    {
                        log.Mixin<DiffLogMixin>().FinalState = target.Dump();
                    }
                }
            });
        }

        private static Entity RetrieveFresh(Entity entity)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
                return entity.ToLite().Retrieve();
        }

        public static MinMax<OperationLogEntity> OperationLogNextPrev(OperationLogEntity log)
        {
            var logs = Database.Query<OperationLogEntity>().Where(a => a.Exception == null && a.Target == log.Target);

            return new MinMax<OperationLogEntity>(
                 log.Mixin<DiffLogMixin>().InitialState == null ? null : logs.Where(a => a.End < log.Start).OrderByDescending(a => a.End).FirstOrDefault(),
                 log.Mixin<DiffLogMixin>().FinalState == null ? null : logs.Where(a => a.Start > log.End).OrderBy(a => a.Start).FirstOrDefault());
        }

        public static void DiffLogic_CleanLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
        {
            var dateLimit = parameters.GetDateLimitClean(typeof(OperationLogEntity).ToTypeEntity());

            Database.Query<OperationLogEntity>().Where(o => o.Start < dateLimit && o.Exception != null).UnsafeDeleteChunksLog(parameters, sb, token);
            Database.Query<OperationLogEntity>().Where(o => o.Start < dateLimit && !o.Mixin<DiffLogMixin>().Cleaned).UnsafeUpdate()
                .Set(a => a.Mixin<DiffLogMixin>().InitialState, a => null)
                .Set(a => a.Mixin<DiffLogMixin>().FinalState, a => null)
                .Set(a => a.Mixin<DiffLogMixin>().Cleaned, a => true)
                .ExecuteChunksLog(parameters, sb, token);
        }
    }
}
