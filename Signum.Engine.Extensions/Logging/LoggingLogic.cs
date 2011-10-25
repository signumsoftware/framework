using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Logging;
using Signum.Entities;
using System.Threading;
using Signum.Entities.Authorization;

namespace Signum.Engine.Logging
{
    public static class LoggingLogic
    {
        public static Func<string> GetCurrentVersion; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool deployment,  bool exceptions)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (deployment)
                {
                    sb.Include<DeploymentLogDN>();

                    dqm[typeof(DeploymentLogDN)] =
                        (from r in Database.Query<DeploymentLogDN>()
                         select new
                         {
                             Entity = r.ToLite(),
                             r.Id,
                             r.CreationDate,
                             r.Version,
                             r.Description,
                             DatabaseHost = r.DataSourceName,
                             DataBaseName = r.DatabaseName,
                             r.MachineName,
                         }).ToDynamic();
                }

                if (exceptions)
                {
                    sb.Include<ExceptionLogDN>();

                    dqm[typeof(ExceptionLogDN)] =
                        (from r in Database.Query<ExceptionLogDN>()
                         select new
                         {
                             Entity = r.ToLite(),
                             r.Id,
                             r.CreationDate,
                             r.ExceptionType,
                             ExcepcionMessage = r.ExceptionMessage,
                             r.StackTraceHash,
                         }).ToDynamic();
                }
            }
        }

        public static void LogDeployment(string description)
        {
            new DeploymentLogDN
            {
                CreationDate = TimeZoneManager.Now,
                Version = GetCurrentVersion(),
                Description = description,
                DataSourceName = ConnectionScope.Current.DataSourceName(),
                DatabaseName = ConnectionScope.Current.DatabaseName(),
                MachineName = Environment.MachineName,
            }.Save();
        }

        public static Func<Exception, IdentifiableEntity> GetContext;

        public static bool ThrowLogingErrors = true;

        public static void LogException(Exception ex, string controllerName, string actionName, string userAgent, string requestUrl)
        {
            try
            {
                using (Schema.Current.GlobalMode())
                using (Transaction tr = new Transaction(true))
                {
                    var log = new ExceptionLogDN
                    {
                        ExceptionType = ex.GetType().Name,
                        ExceptionMessage = ex.Message,
                        StackTrace = ex.StackTrace,
                        ThreadId = Thread.CurrentThread.ManagedThreadId,
                        User = UserDN.Current,
                        RequestUrl = requestUrl,
                        UserAgent = userAgent,
                        ControllerName = controllerName,
                        ActionName = actionName,
                        Context = GetContext == null ? null : GetContext(ex)
                    }.Save();

                    tr.Commit();
                }
            }
            catch (Exception)
            {
                if (ThrowLogingErrors)
                    throw;
            }
        }
    }
}
