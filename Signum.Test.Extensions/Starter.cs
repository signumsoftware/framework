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

                    Load();

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

                AuthLogic.Start(sb, dqm, "System", "Anonymous");
                UserTicketLogic.Start(sb, dqm);
                OperationLogic.Start(sb, dqm);
                
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

                Signum.Test.Starter.StartMusic(sb, dqm);
                
                CacheLogic.CacheTable<LabelDN>(sb);

                AlbumGraph.Register();
                OperationLogic.Register(new BasicExecute<ArtistDN>(ArtistOperation.AssignPersonalAward)
                {
                    Lite = true,
                    AllowsNew = false,
                    CanExecute = a => a.LastAward != null ? "Artist already has an award" : null,
                    Execute = (a, para) => a.LastAward = new PersonalAwardDN() { Category = "Best Artist", Year = DateTime.Now.Year, Result = AwardResult.Won }
                });

                OperationLogic.Register(new BasicDelete<AlbumDN>(AlbumOperation.Delete)
                {
                    Delete = (album, _) => album.Delete()
                });

                TypeConditionLogic.Register<LabelDN>(MusicGroups.JapanEntities, l => l.Country.Name.StartsWith(Signum.Test.Starter.Japan) || l.Owner != null && l.Owner.Entity.Country.Name.StartsWith(Signum.Test.Starter.Japan));
                TypeConditionLogic.Register<AlbumDN>(MusicGroups.JapanEntities, a => a.Label.InCondition(MusicGroups.JapanEntities));

                started = true;

                sb.ExecuteWhenIncluded();
            }
        }

        public static void Load()
        {
            Administrator.TotalGeneration();

            Administrator.SetSnapshotIsolation(true);
            Administrator.MakeSnapshotIsolationDefault(true); 

            using (AuthLogic.Disable())
            {
                RoleDN anonymousUserRole = null;
                RoleDN superUserRole = null;
                RoleDN internalUserRole = null;
                RoleDN externalUserRole = null;
                using (OperationLogic.AllowSave<RoleDN>())
                {
                    anonymousUserRole = new RoleDN { Name = "Anonymous" }.Save();
                    superUserRole = new RoleDN { Name = "SuperUser" }.Save();
                    internalUserRole = new RoleDN { Name = "InternalUser" }.Save();
                    externalUserRole = new RoleDN { Name = "ExternalUser" }.Save();
                }

                using (OperationLogic.AllowSave<UserDN>())
                {
                    new UserDN
                    {
                        State = UserState.Created,
                        UserName = AuthLogic.SystemUserName,
                        PasswordHash = Security.EncodePassword(Guid.NewGuid().ToString()),
                        Role = superUserRole
                    }.Save();

                    new UserDN
                    {
                        State = UserState.Created,
                        UserName = AuthLogic.AnonymousUserName,
                        PasswordHash = Security.EncodePassword(Guid.NewGuid().ToString()),
                        Role = anonymousUserRole
                    }.Save();

                    new UserDN
                    {
                        State = UserState.Created,
                        UserName = "su",
                        PasswordHash = Security.EncodePassword("su"),
                        Role = superUserRole
                    }.Save();

                    new UserDN
                    {
                        State = UserState.Created,
                        UserName = "internal",
                        PasswordHash = Security.EncodePassword("internal"),
                        Role = internalUserRole
                    }.Save();

                    new UserDN
                    {
                        State = UserState.Created,
                        UserName = "external",
                        PasswordHash = Security.EncodePassword("external"),
                        Role = externalUserRole
                    }.Save();
                }

                Schema.Current.InitializeUntil(InitLevel.Level3MainEntities);
                using (OperationLogic.AllowSave<AlbumDN>())
                    Signum.Test.Starter.Load();

                TypeConditionUsersRoles(externalUserRole.ToLite());
                
                TypeAuthLogic.Manual.SetAllowed(externalUserRole.ToLite(), typeof(LabelDN), 
                    new TypeAllowedAndConditions(TypeAllowed.None, 
                            new TypeConditionRule(MusicGroups.JapanEntities, TypeAllowed.Create)));

                TypeAuthLogic.Manual.SetAllowed(externalUserRole.ToLite(), typeof(AlbumDN), 
                    new TypeAllowedAndConditions(TypeAllowed.None,
                            new TypeConditionRule(MusicGroups.JapanEntities, TypeAllowed.Create)));

                TypeConditionUsersRoles(internalUserRole.ToLite());

                ChartScriptLogic.ImportAllScripts(@"d:\Signum\Extensions\Signum.Engine.Extensions\Chart\ChartScripts");
            }
        }

        private static void TypeConditionUsersRoles(Lite<RoleDN> role)
        {
            TypeAuthLogic.Manual.SetAllowed(role, typeof(UserQueryDN),
                new TypeAllowedAndConditions(TypeAllowed.None,
                        new TypeConditionRule(MusicGroups.RoleEntities, TypeAllowed.Read),
                        new TypeConditionRule(MusicGroups.UserEntities, TypeAllowed.Create)));

            TypeAuthLogic.Manual.SetAllowed(role, typeof(ControlPanelDN),
                new TypeAllowedAndConditions(TypeAllowed.None,
                        new TypeConditionRule(MusicGroups.RoleEntities, TypeAllowed.Read),
                        new TypeConditionRule(MusicGroups.UserEntities, TypeAllowed.Create)));

            TypeAuthLogic.Manual.SetAllowed(role, typeof(UserChartDN),
                new TypeAllowedAndConditions(TypeAllowed.None,
                        new TypeConditionRule(MusicGroups.RoleEntities, TypeAllowed.Read),
                        new TypeConditionRule(MusicGroups.UserEntities, TypeAllowed.Create)));

            TypeAuthLogic.Manual.SetAllowed(role, typeof(LinkListPartDN),
              new TypeAllowedAndConditions(TypeAllowed.None,
                      new TypeConditionRule(MusicGroups.RoleEntities, TypeAllowed.Read),
                      new TypeConditionRule(MusicGroups.UserEntities, TypeAllowed.Create)));
        }
    }

    public enum MusicGroups
    {
        JapanEntities,
        RoleEntities,
        UserEntities
    }
}
