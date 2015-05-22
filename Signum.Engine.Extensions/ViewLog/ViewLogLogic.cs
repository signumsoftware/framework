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
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;

namespace Signum.Engine.ViewLog
{
    public static class ViewLogLogic
    {
        static Func<ViewLogConfigurationEntity> getConfiguration;
        public static ViewLogConfigurationEntity Configuration
        {
            get { return getConfiguration(); }
        }

        public static Func<DynamicQueryManager.ExecuteType, object, BaseQueryRequest, IDisposable> QueryExecutedLog;

        static Expression<Func<Entity, IQueryable<ViewLogEntity>>> ViewLogsExpression =
            a => Database.Query<ViewLogEntity>().Where(log => log.Target.RefersTo(a));
        public static IQueryable<ViewLogEntity> ViewLogs(this Entity a)
        {
            return ViewLogsExpression.Evaluate(a);
        }

        public static HashSet<Type> Types = new HashSet<Type>();


        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm,HashSet<Type> types, Func<DynamicQueryManager.ExecuteType, object, BaseQueryRequest, IDisposable> QueryExecutedLog, Func<ViewLogConfigurationEntity> configuration)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
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

                getConfiguration = configuration;

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;

                Types = types;


                var exp = Signum.Utilities.ExpressionTrees.Linq.Expr((Entity entity) => entity.ViewLogs());

                foreach (var t in Types)
                {
                    dqm.RegisterExpression(new ExtensionInfo(t, exp, exp.Body.Type, "ViewLogs", () => typeof(ViewLogEntity).NicePluralName()));
                }

                if (Types.Contains(typeof(QueryEntity)) && QueryExecutedLog != null)
                {
                    DynamicQueryManager.Current.QueryExecuted += QueryExecutedLog;
                }
            }

            
        }

  

        public static Func<DynamicQueryManager.ExecuteType, object, BaseQueryRequest, IDisposable> QueryExecutedDefaultLog = (type, queryName, baseQueryRequest) =>
        {
            if (type == DynamicQueryManager.ExecuteType.ExecuteQuery ||
                type == DynamicQueryManager.ExecuteType.ExecuteGroupQuery)
            {
                baseQueryRequest.QueryTextLog = Configuration.QueryTextLog;
                baseQueryRequest.QueryUrlLog = Configuration.QueryUrlLog;

                return LogView(QueryLogic.GetQueryEntity(queryName).ToLite(), "Query", () =>
                       baseQueryRequest.QueryTextLog || baseQueryRequest.QueryUrlLog ? "{0} \r\n {1}".FormatWith(baseQueryRequest.QueryUrl, baseQueryRequest.QueryText) : null);
            }

            return null;
        };

        static void ExceptionLogic_DeleteLogs(DeleteLogParametersEntity parameters)
        {
            Database.Query<ViewLogEntity>().Where(view => view.StartDate < parameters.DateLimit).UnsafeDeleteChunks(parameters.ChunkSize, parameters.MaxChunks);
        }

        public static IDisposable LogView(Lite<IEntity> entity, string viewAction)
        {
            return LogView(entity, viewAction, () => null);
        }

        public static IDisposable LogView(Lite<IEntity> entity, string viewAction, Func<string> dataString)
        {
            if (!Configuration.Active || !Configuration.Types.Contains(entity.EntityType))
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
                viewLog.Data = dataString();
                using (ExecutionMode.Global())
                    viewLog.Save();
            });
        }
    }

}
