using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Engine.Basics;
using Signum.Entities.ViewLog;

namespace Signum.Engine.ViewLog
{
    public static class ViewLogLogic
    {
        static Expression<Func<Entity, IQueryable<ViewLogEntity>>> ViewLogsExpression =
            a => Database.Query<ViewLogEntity>().Where(log => log.Target.RefersTo(a));
        public static IQueryable<ViewLogEntity> ViewLogs(this Entity a)
        {
            return ViewLogsExpression.Evaluate(a);
        }

        public static HashSet<Type> Types = new HashSet<Type>();

        static bool Started;
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, HashSet<Type> types)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Started = true;

                sb.Include<ViewLogEntity>();

                dqm.RegisterQuery(typeof(ViewLogEntity), () =>
                    from e in Database.Query<ViewLogEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Target,
                        e.ViewAction,
                        e.User,
                        e.Duration,
                        e.StartDate,
                        e.EndDate,
                    });

                Types = types;

                var exp = Signum.Utilities.ExpressionTrees.Linq.Expr((Entity entity)=> entity.ViewLogs());

                foreach (var t in types)
                {
                    dqm.RegisterExpression(new ExtensionInfo(t, exp, exp.Body.Type, "ViewLogs", () => typeof(ViewLogEntity).NicePluralName()));
                }

                if (types.Contains(typeof(QueryEntity)))
                {
                    DynamicQueryManager.Current.QueryExecuted += Current_QueryExecuted;
                }

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            }
        }

        static IDisposable Current_QueryExecuted(DynamicQueryManager.ExecuteType type, object queryName)
        {
            if (type == DynamicQueryManager.ExecuteType.ExecuteQuery ||
                type == DynamicQueryManager.ExecuteType.ExecuteGroupQuery)
            {
                return LogView(QueryLogic.GetQueryEntity(queryName).ToLite(), "Query");
            }

            return null;
        }

        static void ExceptionLogic_DeleteLogs(DeleteLogParametersEntity parameters)
        {
            Database.Query<ViewLogEntity>().Where(view => view.StartDate < parameters.DateLimit).UnsafeDeleteChunks(parameters.ChunkSize, parameters.MaxChunks);
        }

        public static IDisposable LogView(Lite<IEntity> entity, string viewAction)
        {
            if (!Started || !Types.Contains(entity.EntityType))
                return null;

            var viewLog = new ViewLogEntity
            {
                Target = (Lite<Entity>)entity,
                User = UserHolder.Current.ToLite(),
                ViewAction = viewAction,
            };

            return new Disposable(() =>
            {
                viewLog.EndDate = TimeZoneManager.Now;
                using (ExecutionMode.Global())
                    viewLog.Save();
            });
        }
    }
   
}
