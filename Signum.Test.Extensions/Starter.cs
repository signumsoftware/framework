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

                    Schema.Current.Initialize(InitLevel.Level0SyncEntities);

                    Load();

                    Schema.Current.Initialize();
                }

                started = true;
            }
        }

        internal static void Dirty()
        {
            started = false;
        }


        public static void Start(string connectionString)
        {
            AuthLogic.SystemUserName = "System";

            SchemaBuilder sb = new SchemaBuilder();
            DynamicQueryManager dqm = new DynamicQueryManager();
            ConnectionScope.Default = new Connection(connectionString, sb.Schema, dqm);

            sb.Settings.OverrideTypeAttributes<IUserRelatedDN>(new ImplementedByAttribute());

            AuthLogic.Start(sb, dqm, AuthLogic.SystemUserName, null);
            UserTicketLogic.Start(sb, dqm);
            OperationLogic.Start(sb, dqm);

            TypeAuthLogic.Start(sb);
            PropertyAuthLogic.Start(sb, true);
            QueryAuthLogic.Start(sb, dqm);
            OperationAuthLogic.Start(sb);
            PermissionAuthLogic.Start(sb);
            EntityGroupAuthLogic.Start(sb);
            FacadeMethodAuthLogic.Start(sb, typeof(IServerSample));

            QueryLogic.Start(sb);
            UserQueryLogic.Start(sb, dqm);

            ReportsLogic.Start(sb, dqm, true, false);

            Signum.Test.Starter.StartMusic(sb, dqm);

            new AlbumGraph().Register();

            EntityGroupLogic.Register<LabelDN>(MusicGroups.JapanEntities, l => l.Country.Name == Signum.Test.Starter.Japan || l.Owner != null && l.Owner.SmartRetrieve().Country.Name == Signum.Test.Starter.Japan);

            EntityGroupLogic.Register<AlbumDN>(MusicGroups.JapanEntities, a => a.Label.IsInGroup(MusicGroups.JapanEntities));
        }

        public static void Load()
        {
            Administrator.TotalGeneration();

            using (AuthLogic.Disable())
            {
                Schema.Current.Initialize(InitLevel.Level3MainEntities);

                RoleDN superUser = new RoleDN { Name = "SuperUser" }.Save();
                RoleDN internalUser = new RoleDN { Name = "InternalUser" }.Save();
                RoleDN externalUser = new RoleDN { Name = "ExternalUser" }.Save();

                // crear los usuarios base
                new UserDN
                {
                    UserName = AuthLogic.SystemUserName,
                    PasswordHash = Security.EncodePassword(Guid.NewGuid().ToString()),
                    Role = superUser
                }.Save();

                new UserDN
                {
                    UserName = "su",
                    PasswordHash = Security.EncodePassword("su"),
                    Role = superUser
                }.Save();

                new UserDN
                {
                    UserName = "internal",
                    PasswordHash = Security.EncodePassword("internal"),
                    Role = internalUser
                }.Save();

                new UserDN
                {
                    UserName = "external",
                    PasswordHash = Security.EncodePassword("external"),
                    Role = externalUser
                }.Save();

                Signum.Test.Starter.Load();

                EntityGroupAuthLogic.SetEntityGroupAllowed(externalUser.ToLite(), MusicGroups.JapanEntities,
                    new EntityGroupAllowedDN(TypeAllowed.DBCreateUICreate, TypeAllowed.DBNoneUINone));
            }
        }
    }

    public enum MusicGroups
    {
        JapanEntities
    }
}
