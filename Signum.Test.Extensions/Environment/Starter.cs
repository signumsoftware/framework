using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Authorization;
using Signum.Engine.Operations;
using Signum.Engine.Maps;
using Signum.Engine;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Services;
using Signum.Engine.Basics;
using Signum.Engine.Reports;
using Signum.Engine.ControlPanel;
using Signum.Entities.ControlPanel;
using Signum.Entities.Reports;
using Signum.Entities.Chart;
using Signum.Engine.UserQueries;
using Signum.Entities.UserQueries;
using Signum.Entities.Basics;
using Signum.Engine.Chart;
using Signum.Engine.Cache;
using Signum.Engine.Files;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Engine.Alerts;
using Signum.Engine.Notes;

namespace Signum.Test.Extensions
{
    public static class Starter
    {
        static bool hasData = false;
        public static void StartAndLoad(string connectionString)
        {
            Start(connectionString);

            if (!hasData)
            {
                Administrator.TotalGeneration();

                using (AuthLogic.Disable())
                {
                    Schema.Current.InitializeUntil(InitLevel.Level0SyncEntities);

                    MusicExtensionsLoader.Load();

                    Schema.Current.Initialize();
                }

                hasData = true;
            }
        }

        public static void Dirty()
        {
            hasData = false;
        }

        static bool started = false;
        public static void Start(string connectionString)
        {
            if (!started)
            {
                SchemaBuilder sb = new SchemaBuilder(DBMS.SqlServer2008);
                DynamicQueryManager dqm = new DynamicQueryManager();
                Connector.Default = new SqlConnector(connectionString, sb.Schema, dqm);
                sb.Schema.Version = typeof(Starter).Assembly.GetName().Version; 

                sb.Settings.OverrideAttributes((UserDN u) => u.Related, new ImplementedByAttribute());
                sb.Settings.OverrideAttributes((ControlPanelDN cp) => cp.Related, new ImplementedByAttribute(typeof(UserDN), typeof(RoleDN)));
                sb.Settings.OverrideAttributes((UserQueryDN uq) => uq.Related, new ImplementedByAttribute(typeof(UserDN), typeof(RoleDN)));
                sb.Settings.OverrideAttributes((UserChartDN uq) => uq.Related, new ImplementedByAttribute(typeof(UserDN), typeof(RoleDN)));

                sb.Schema.Settings.OverrideAttributes((ProcessExecutionDN cp) => cp.ProcessData, new ImplementedByAttribute(typeof(PackageDN), typeof(PackageOperationDN)));
                sb.Schema.Settings.OverrideAttributes((PackageLineDN cp) => cp.Package, new ImplementedByAttribute(typeof(PackageDN), typeof(PackageOperationDN)));

                sb.Schema.Settings.OverrideAttributes((OperationLogDN ol) => ol.User, new ImplementedByAttribute(typeof(UserDN)));
                sb.Schema.Settings.OverrideAttributes((ExceptionDN e) => e.User, new ImplementedByAttribute(typeof(UserDN)));

                OperationLogic.Start(sb, dqm);
                ExceptionLogic.Start(sb, dqm);
                AuthLogic.Start(sb, dqm, "System", "Anonymous");
                UserTicketLogic.Start(sb, dqm);

                ProcessLogic.Start(sb, dqm, 1, userProcessSession: true);
                PackageLogic.Start(sb, dqm, true, true);
                ProcessLogic.CreateDefaultProcessSession = UserProcessSessionDN.CreateCurrent;
                CacheLogic.Start(sb);

                AuthLogic.StartAllModules(sb, dqm, typeof(IServerSample));

                QueryLogic.Start(sb);
                UserQueryLogic.Start(sb, dqm);
                UserQueryLogic.RegisterUserTypeCondition(sb, MusicGroups.UserEntities);
                UserQueryLogic.RegisterRoleTypeCondition(sb, MusicGroups.RoleEntities);
                ControlPanelLogic.Start(sb, dqm);
                ControlPanelLogic.RegisterUserTypeCondition(sb, MusicGroups.UserEntities);
                ControlPanelLogic.RegisterRoleTypeCondition(sb, MusicGroups.RoleEntities);
                
                ChartLogic.Start(sb, dqm);
                UserChartLogic.RegisterUserTypeCondition(sb, MusicGroups.UserEntities);
                UserChartLogic.RegisterRoleTypeCondition(sb, MusicGroups.RoleEntities);

                AlertLogic.Start(sb, dqm);
                NoteLogic.Start(sb, dqm);

                FilePathLogic.Start(sb, dqm);
                ReportsLogic.Start(sb, dqm, true);

                MusicLogic.Start(sb, dqm);
                
                CacheLogic.CacheTable<LabelDN>(sb);

                TypeConditionLogic.Register<LabelDN>(MusicGroups.JapanEntities, l => l.Country.Name.StartsWith(MusicLoader.Japan) || l.Owner != null && l.Owner.Entity.Country.Name.StartsWith(MusicLoader.Japan));
                TypeConditionLogic.Register<AlbumDN>(MusicGroups.JapanEntities, a => a.Label.InCondition(MusicGroups.JapanEntities));

                started = true;

                sb.ExecuteWhenIncluded();
            }
        }
    }

    public enum MusicGroups
    {
        JapanEntities,
        RoleEntities,
        UserEntities
    }
}
