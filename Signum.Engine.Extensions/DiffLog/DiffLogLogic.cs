using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DiffLog;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Engine.DiffLog
{
    public static class DiffLogLogic
    {
        public static Polymorphic<Func<IEntity, IOperation, bool>> Types = new Polymorphic<Func<IEntity, IOperation, bool>>(minimumType: typeof(Entity));

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool registerAll)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                MixinDeclarations.AssertDeclared(typeof(OperationLogEntity), typeof(DiffLogMixin));

                OperationLogic.SurroundOperation += OperationLogic_SurroundOperation;

                if (registerAll)
                    RegisterGraph<Entity>((entity, oper) => true);
            }
        }

        public static void RegisterGraph<T>(Func<IEntity, IOperation, bool> func) where T : Entity
        {
            Types.SetDefinition(typeof(T), func);
        }

        static IDisposable OperationLogic_SurroundOperation(IOperation operation, OperationLogEntity log, Entity entity, object[] args)
        {
            if(entity==null)
                return null;

            var type = entity.GetType();
            bool strategy = Types.ContainsKey(type) ? Types.TryGetValue(type)(entity, operation) : false;

            if (strategy == false)
                 strategy = Types.ContainsKey(typeof(Entity)) ? Types.TryGetValue(typeof(Entity))(entity, operation) : false;


            if (strategy == false)
                return null;

            using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
            {
                if (operation.OperationType == OperationType.Delete)
                    log.Mixin<DiffLogMixin>().InitialState = entity.Dump();
                else if (operation.OperationType == OperationType.Execute && !entity.IsNew)
                    log.Mixin<DiffLogMixin>().InitialState = ((IEntityOperation)operation).Lite ? entity.Dump() : RetrieveFresh(entity).Dump();
            }

            return new Disposable(() =>
            {
                if (log != null)
                {
                    var target = log.GetTarget();

                    if (target != null && operation.OperationType != OperationType.Delete && !target.IsNew)
                    {
                        using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
                        {
                            if (Types.GetValue(type)(entity, operation))
                                log.Mixin<DiffLogMixin>().FinalState = entity.Dump();
                        }
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

        public static void DiffLogic_DeleteLogs(DeleteLogParametersEntity parameters)
        {
            Database.Query<OperationLogEntity>().Where(o => o.Start < parameters.DateLimit && o.Exception != null).UnsafeDeleteChunks(parameters.ChunkSize, parameters.MaxChunks);
            Database.Query<OperationLogEntity>().Where(o => o.Start < parameters.DateLimit && !o.Mixin<DiffLogMixin>().Cleaned).UnsafeUpdate()
                .Set(a => a.Mixin<DiffLogMixin>().InitialState, a => null)
                .Set(a => a.Mixin<DiffLogMixin>().FinalState, a => null)
                .Set(a => a.Mixin<DiffLogMixin>().Cleaned, a => true)
                .ExecuteChunks(parameters.ChunkSize, parameters.MaxChunks);
        }
    }
}
