using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Operations;
using Signum.Entities.Scheduler;
using Signum.Engine.Processes;
using Signum.Engine.Maps;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Entities.Processes;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.Authorization;
using System.Linq.Expressions;
using Signum.Engine.Exceptions;

namespace Signum.Engine.Scheduler
{
    public static class SimpleTaskLogic
    {
        static Dictionary<SimpleTaskSymbol, Func<Lite<IIdentifiable>>> tasks = new Dictionary<SimpleTaskSymbol, Func<Lite<IIdentifiable>>>();

        internal static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SymbolLogic<SimpleTaskSymbol>.Start(sb, () => tasks.Keys.ToHashSet());

                SchedulerLogic.ExecuteTask.Register((SimpleTaskSymbol st) =>
                {
                    Func<Lite<IIdentifiable>> func = tasks.GetOrThrow(st);
                    return func();
                });


                dqm.RegisterQuery(typeof(SimpleTaskSymbol), ()=>
                      from ct in Database.Query<SimpleTaskSymbol>()
                       select new
                       {
                           Entity = ct,
                           ct.Id,
                           ct.Key,
                       });
            }
        }

        public static void Register(SimpleTaskSymbol simpleTaskSymbol, Func<Lite<IIdentifiable>> action)
        {
            if (simpleTaskSymbol == null)
                throw new ArgumentNullException("simpleTaskSymbol");

            if (action == null)
                throw new ArgumentNullException("actionKey");

            tasks.Add(simpleTaskSymbol, action); 
        }      
    }
}
