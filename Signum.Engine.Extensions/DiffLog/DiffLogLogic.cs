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
using System.Text.RegularExpressions;

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
            }
        }

        static Regex LiteImpRegex = new Regex(@"^(?<space> *)(?<prop>\w[\w\d_]+) = new LiteImp<");

        public static string? SimplifyDump(string? text, bool simplify)
        {
            if (text == null)
                return null;

            if (!simplify)
                return text;

            var lines = text.Lines().ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                var current = lines[i];
                if(current.Contains("= new LiteImp<") && !current.EndsWith(","))
                {
                    var match = LiteImpRegex.Match(current);
                    if (match.Success)
                    {
                        var spaces = match.Groups["space"].Value;
                        if (lines[i + 1] == spaces + "{")
                        {
                            var lastIndex = lines.IndexOf(spaces + "},", i + 1);

                            if(lastIndex != -1)
                            {
                                lines.RemoveRange(i + 1, lastIndex - (i + 1) + 1);
                            }

                            lines[i] = current + " { Entity = /* Loaded */ },";
                        }
                    }
                }
            }

            return lines.ToString("\r\n");
        }

        public static void RegisterShouldLog<T>(Func<IEntity, IOperation, bool> func) where T : Entity
        {
            ShouldLog.SetDefinition(typeof(T), func);
        }

        static IDisposable OperationLogic_SurroundOperation(IOperation operation, OperationLogEntity log, Entity? entity, object?[]? args)
        {
            if (entity != null && ShouldLog.Invoke(entity, operation))
            {
                if (operation.OperationType == OperationType.Execute && !entity.IsNew && ((IEntityOperation)operation).CanBeModified)
                    entity = RetrieveFresh(entity);

                using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
                {
                    log.Mixin<DiffLogMixin>().InitialState = new BigStringEmbedded(entity.Dump());
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
                        log.Mixin<DiffLogMixin>().FinalState = new BigStringEmbedded(target.Dump());
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

        public static MinMax<OperationLogEntity?> OperationLogNextPrev(OperationLogEntity log)
        {
            var logs = Database.Query<OperationLogEntity>().Where(a => a.Exception == null && a.Target == log.Target);

            return new MinMax<OperationLogEntity?>(
                 log.Mixin<DiffLogMixin>().InitialState.Text == null ? null : logs.Where(a => a.End < log.Start).OrderByDescending(a => a.End).FirstOrDefault(),
                 log.Mixin<DiffLogMixin>().FinalState.Text == null ? null : logs.Where(a => a.Start > log.End).OrderBy(a => a.Start).FirstOrDefault());
        }
    }
}
