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
        public static Polymorphic<Tuple<DiffLogStrategy>> Types = new Polymorphic<Tuple<DiffLogStrategy>>(minimumType: typeof(IdentifiableEntity)); 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                MixinDeclarations.AssertDeclared(typeof(OperationLogDN), typeof(DiffLogMixin));

                OperationLogic.SurroundOperation += OperationLogic_SurroundOperation;
            }
        }

        public static void RegisterGraph<T>(DiffLogStrategy value) where T : IdentifiableEntity
        {
            Types.SetDefinition(typeof(T), Tuple.Create(value));
        }

        static IDisposable OperationLogic_SurroundOperation(IOperation operation, OperationLogDN log, IdentifiableEntity entity, object[] args)
        {
            var type =
                operation.OperationType == OperationType.Constructor ? operation.ReturnType :
                operation.OperationType == OperationType.ConstructorFrom ? operation.ReturnType :
                operation.OperationType == OperationType.ConstructorFromMany ? operation.ReturnType :
                operation.OperationType == OperationType.Execute ? entity.GetType() :
                operation.OperationType == OperationType.Delete ? entity.GetType() :
                new InvalidOperationException("Unexpected OperationType {0}".Formato(operation.OperationType)).Throw<Type>();

            DiffLogStrategy strategy = Types.GetValue(type).Try(a => a.Item1) ?? 0;

            var required = GetStrategy(operation);

            if ((strategy & required) == 0)
                return null;

            if (operation.OperationType == OperationType.Delete)
                log.Mixin<DiffLogMixin>().InitialState = entity.Dump();
            else if (operation.OperationType == OperationType.Execute && !entity.IsNew)
                log.Mixin<DiffLogMixin>().InitialState = ((IEntityOperation)operation).Lite ? entity.Dump() : RetrieveFresh(entity).Dump();

            return new Disposable(() =>
            {
                if (log != null)
                {
                    var target = log.GetTarget();

                    if (target != null && operation.OperationType != OperationType.Delete)
                        log.Mixin<DiffLogMixin>().FinalState = entity.Dump();
                }
            });
        }

        private static IdentifiableEntity RetrieveFresh(IdentifiableEntity entity)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
                return entity.ToLite().Retrieve();
        }

        private static DiffLogStrategy GetStrategy(IOperation operation)
        {
            switch (operation.OperationType)
            {
                case OperationType.Execute: return ((IEntityOperation)operation).Lite ? DiffLogStrategy.ExecuteLite : DiffLogStrategy.ExecuteNoLite;
                case OperationType.Delete: return DiffLogStrategy.Delete;
                case OperationType.Constructor: return DiffLogStrategy.Construct;
                case OperationType.ConstructorFrom: return DiffLogStrategy.ConstructFrom;
                case OperationType.ConstructorFromMany: return DiffLogStrategy.ConstructFromMany;
                default: throw new InvalidOperationException("Unexpected OperationType " + operation.OperationType);
            }
        }

        public static MinMax<OperationLogDN> OperationLogNextPrev(OperationLogDN log)
        {
            var logs = Database.Query<OperationLogDN>().Where(a => a.Exception == null && a.Target == log.Target);

            return new MinMax<OperationLogDN>(
                 log.Mixin<DiffLogMixin>().InitialState == null ? null : logs.Where(a => a.End < log.Start).OrderByDescending(a => a.End).FirstOrDefault(),
                 log.Mixin<DiffLogMixin>().FinalState == null ? null : logs.Where(a => a.Start > log.End).OrderBy(a => a.Start).FirstOrDefault());
        }
    }

    public enum DiffLogStrategy
    {
        /// <summary>
        /// Save
        /// </summary>
        ExecuteNoLite = 1,
        ExecuteLite = 2,
        Construct = 4, 
        ConstructFrom = 8, 
        ConstructFromMany = 16, 
        Delete = 32,

        All = ExecuteNoLite | ExecuteLite | Construct | ConstructFrom | ConstructFromMany | Delete
    }
}
