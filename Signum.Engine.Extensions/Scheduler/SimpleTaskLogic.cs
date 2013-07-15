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
        static Dictionary<Enum, Func<Lite<IIdentifiable>>> tasks = new Dictionary<Enum, Func<Lite<IIdentifiable>>>();

        internal static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                MultiEnumLogic<SimpleTaskDN>.Start(sb, () => tasks.Keys.ToHashSet());

                SchedulerLogic.ExecuteTask.Register((SimpleTaskDN ct) =>
                {
                    Enum enumValue = MultiEnumLogic<SimpleTaskDN>.ToEnum(ct.Key);
                    Func<Lite<IIdentifiable>> func = tasks.GetOrThrow(enumValue);
                    return func();
                });


                dqm.RegisterQuery(typeof(SimpleTaskDN), ()=>
                      from ct in Database.Query<SimpleTaskDN>()
                       select new
                       {
                           Entity = ct,
                           ct.Id,
                           ct.Key,
                       });
            }
        }

        public static void Register(Enum key, Func<Lite<IIdentifiable>> action)
        {
            if (key == null)
                throw new ArgumentNullException("taskKey");

            if (action == null)
                throw new ArgumentNullException("actionKey");

            tasks.Add(key, action); 
        }      
    }
}
