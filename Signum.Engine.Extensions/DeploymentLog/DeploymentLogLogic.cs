using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DeploymentLog;
using Signum.Entities;

namespace Signum.Engine.DeploymentLog
{
    public static class DeploymentLogLogic
    {
        public static Func<string> GetCurrentVersion; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DeploymentLogDN>();
                
                dqm[typeof(DeploymentLogDN)] = (from r in Database.Query<DeploymentLogDN>()
                                                select new
                                                {
                                                    Entity = r.ToLite(),
                                                    r.Id,
                                                    r.CreationDate,
                                                    r.Version,
                                                    r.Description,
                                                    DatabaseHost = r.DataSourceName,
                                                    r.DataBaseName,
                                                    r.MachineName,
                                                }).ToDynamic(); 
            }
        }

        public static void Log(string description)
        {
            new DeploymentLogDN
            {
                CreationDate = TimeZoneManager.Now, 
                Version = GetCurrentVersion(),
                Description = description,
                DataSourceName = ConnectionScope.Current.DataSourceName(),
                DataBaseName = ConnectionScope.Current.DatabaseName(),
                MachineName = Environment.MachineName,
            }.Save(); 
        }
    }
}
