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
using Signum.Engine.Extensions.Chart;
using Signum.Entities.Chart;
using Signum.Engine.UserQueries;
using Signum.Entities.UserQueries;
using Signum.Entities.Basics;

namespace Signum.Test.Extensions
{
    public static class Starter
    {
        static bool started = false;
        public static void StartAndLoad(string connectionString)
        {
            if (!started)
            {
                Start(connectionString);

                Administrator.TotalGeneration();

                using (AuthLogic.Disable())
                {
                    Schema.Current.InitializeUntil(InitLevel.Level0SyncEntities);

                    Load();

                    Schema.Current.Initialize();
                }

                started = true;
            }
        }

        public static void Dirty()
        {
            started = false;
        }


        public static void Start(string connectionString)
        {
            SchemaBuilder sb = new SchemaBuilder();
            DynamicQueryManager dqm = new DynamicQueryManager();
            ConnectionScope.Default = new Connection(connectionString, sb.Schema, dqm);

            sb.Settings.OverrideAttributes((UserDN u) => u.Related, new ImplementedByAttribute());
            sb.Settings.OverrideAttributes((ControlPanelDN cp) => cp.Related, new ImplementedByAttribute(typeof(UserDN), typeof(RoleDN)));
            sb.Settings.OverrideAttributes((UserQueryDN uq) => uq.Related, new ImplementedByAttribute(typeof(UserDN), typeof(RoleDN)));
            sb.Settings.OverrideAttributes((UserChartDN uq) => uq.Related, new ImplementedByAttribute(typeof(UserDN), typeof(RoleDN)));

            AuthLogic.Start(sb, dqm, "System", "Anonymous");
            UserTicketLogic.Start(sb, dqm);
            OperationLogic.Start(sb, dqm);

            AuthLogic.StartAllModules(sb, dqm, typeof(IServerSample));
            
            QueryLogic.Start(sb);
            UserQueryLogic.Start(sb, dqm);
            UserQueryLogic.RegisterUserEntityGroup(sb, MusicGroups.UserEntities);
            UserQueryLogic.RegisterRoleEntityGroup(sb, MusicGroups.RoleEntities);
            ControlPanelLogic.Start(sb, dqm);
            ControlPanelLogic.RegisterUserEntityGroup(sb, MusicGroups.UserEntities);
            ControlPanelLogic.RegisterRoleEntityGroup(sb, MusicGroups.RoleEntities);
            ChartLogic.Start(sb, dqm);
            ChartLogic.RegisterUserEntityGroup(sb, MusicGroups.UserEntities);
            ChartLogic.RegisterRoleEntityGroup(sb, MusicGroups.RoleEntities);

            ReportsLogic.Start(sb, dqm, true);

            Signum.Test.Starter.StartMusic(sb, dqm);

            AlbumGraph.Register();
            OperationLogic.Register(new BasicExecute<ArtistDN>(ArtistOperation.AssignPersonalAward)
            {
                Lite = true,
                AllowsNew = false,
                CanExecute = a => a.LastAward != null ? "Artist cannot have already an award" : null,
                Execute = (a,para) => a.LastAward = new PersonalAwardDN() { Category = "Best Artist", Year = DateTime.Now.Year, Result = AwardResult.Won }
            });

            OperationLogic.Register(new BasicDelete<AlbumDN>(AlbumOperation.Delete)
            {
                Delete = (album, _) => album.Delete()
            });

            TypeConditionLogic.Register<LabelDN>(MusicGroups.JapanEntities, l => l.Country.Name.StartsWith(Signum.Test.Starter.Japan) || l.Owner != null && l.Owner.Entity.Country.Name.StartsWith(Signum.Test.Starter.Japan));
            TypeConditionLogic.Register<AlbumDN>(MusicGroups.JapanEntities, a => a.Label.InCondition(MusicGroups.JapanEntities));
        }

        public static void Load()
        {
            Administrator.TotalGeneration();

            Administrator.SetSnapshotIsolation(true);
            Administrator.MakeSnapshotIsolationDefault(true); 

            using (AuthLogic.Disable())
            {
                //Schema.Current.Initialize(InitLevel.Level1SimpleEntities);

                //RoleDN systemUser = new RoleDN { Name = "System" }.Save();
                RoleDN anonymousUser = new RoleDN { Name = "Anonymous" }.Save();
                
                RoleDN superUser = new RoleDN { Name = "SuperUser" }.Save();
                RoleDN internalUser = new RoleDN { Name = "InternalUser" }.Save();
                RoleDN externalUser = new RoleDN { Name = "ExternalUser" }.Save();

                // crear los usuarios base
                new UserDN
                {
                    State=UserState.Created,
                    UserName = AuthLogic.SystemUserName,
                    PasswordHash = Security.EncodePassword(Guid.NewGuid().ToString()),
                    Role = superUser
                }.Save();

                new UserDN
                {
                    State = UserState.Created,
                    UserName = AuthLogic.AnonymousUserName,
                    PasswordHash = Security.EncodePassword(Guid.NewGuid().ToString()),
                    Role = anonymousUser
                }.Save();

                new UserDN
                {
                    State = UserState.Created,
                    UserName = "su",
                    PasswordHash = Security.EncodePassword("su"),
                    Role = superUser
                }.Save();

                new UserDN
                {
                    State = UserState.Created,
                    UserName = "internal",
                    PasswordHash = Security.EncodePassword("internal"),
                    Role = internalUser
                }.Save();

                new UserDN
                {
                    State = UserState.Created,
                    UserName = "external",
                    PasswordHash = Security.EncodePassword("external"),
                    Role = externalUser
                }.Save();

                Schema.Current.InitializeUntil(InitLevel.Level3MainEntities);
                Signum.Test.Starter.Load();

                TypeConditionUsersRoles(externalUser.ToLite());
                
                TypeAuthLogic.Manual.SetAllowed(externalUser.ToLite(), typeof(LabelDN), 
                    new TypeAllowedAndConditions(TypeAllowed.None, 
                            new TypeConditionRule(MusicGroups.JapanEntities, TypeAllowed.Create)));

                TypeAuthLogic.Manual.SetAllowed(externalUser.ToLite(), typeof(LabelDN), 
                    new TypeAllowedAndConditions(TypeAllowed.None,
                            new TypeConditionRule(MusicGroups.JapanEntities, TypeAllowed.Create)));

                TypeConditionUsersRoles(internalUser.ToLite());

                AuthLogic.InvalidateCache(); 
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
