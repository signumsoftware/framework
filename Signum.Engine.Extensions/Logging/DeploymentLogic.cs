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
using Signum.Utilities;

namespace Signum.Engine.Logging
{
    public static class DeploymentLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
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
        }

        public static void Log(string description)
        {
            new DeploymentLogDN
            {
                CreationDate = TimeZoneManager.Now,
                Version = Schema.Current.MainAssembly.TryCC(a => a.GetName().Version.ToString()),
                Description = description,
                DataSourceName = ConnectionScope.Current.DataSourceName(),
                DatabaseName = ConnectionScope.Current.DatabaseName(),
                MachineName = Environment.MachineName,
            }.Save();
        }
    }
}
