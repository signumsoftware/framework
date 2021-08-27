using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.Scheduler;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using System.Reflection;

namespace Signum.Engine.Scheduler
{
    public static class SimpleTaskLogic
    {
        static Dictionary<SimpleTaskSymbol, Func<ScheduledTaskContext, Lite<IEntity>?>> tasks = new Dictionary<SimpleTaskSymbol, Func<ScheduledTaskContext, Lite<IEntity>?>>();

        internal static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SymbolLogic<SimpleTaskSymbol>.Start(sb, () => tasks.Keys.ToHashSet());

                SchedulerLogic.ExecuteTask.Register((SimpleTaskSymbol st, ScheduledTaskContext ctx) =>
                {
                    Func<ScheduledTaskContext, Lite<IEntity>?> func = tasks.GetOrThrow(st);
                    return func(ctx);
                });

                sb.Include<SimpleTaskSymbol>()
                    .WithQuery(() => ct => new
                    {
                        Entity = ct,
                        ct.Id,
                        ct.Key,
                    });
            }
        }

        public static void Register(SimpleTaskSymbol simpleTaskSymbol, Func<ScheduledTaskContext, Lite<IEntity>?> action)
        {
            if (simpleTaskSymbol == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(SimpleTaskSymbol), nameof(simpleTaskSymbol));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            tasks.Add(simpleTaskSymbol, action); 
        }      
    }
}
