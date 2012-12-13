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
using Signum.Entities.Alerts;

namespace Signum.Test.Extensions
{
    public static class MusicExtensionsLoader
    {
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

                using (AuthLogic.UnsafeUserSession("su"))
                {
                    MusicLoader.Load();

                    new AlertTypeDN { Name = "test alert" }.Execute(AlertTypeOperation.Save);
                }


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
}
