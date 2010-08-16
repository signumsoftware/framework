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
        public static void Start(string connectionString)
        { 
            if (!started)
            {
                started = true;
                
                AuthLogic.SystemUserName = "System";

                SchemaBuilder sb = new SchemaBuilder();

                DynamicQueryManager dqm = new DynamicQueryManager();

                ConnectionScope.Default = new Connection(connectionString, sb.Schema, dqm);

                sb.Schema.Initializing(InitLevel.Level1SimpleEntities, Schema_InitializingApplication);

                sb.Settings.OverrideTypeAttributes<IUserRelatedDN>(new ImplementedByAttribute());

                Signum.Test.Starter.InternalStart(sb, dqm);

                OperationLogic.Start(sb, dqm);

                QueryLogic.Start(sb);
                UserQueryLogic.Start(sb, dqm);

                ReportsLogic.Start(sb, dqm, true, false);

                new AlbumGraph().Register();
            }
        }

        public static void StartAndLoad(string connectionString)
        { 
            if (!started)
            {
                Start(connectionString);

                Administrator.TotalGeneration();

                RoleDN superUser = new RoleDN
                {
                    Name = "SuperUser"
                }.Save();

                // crear los usuarios base
                new UserDN
                {
                    UserName = AuthLogic.SystemUserName,
                    PasswordHash = Security.EncodePassword(Guid.NewGuid().ToString()),
                    Role = superUser
                }.Save();

                new UserDN
                {
                    UserName = "test",
                    PasswordHash = Security.EncodePassword("test"),
                    Role = new RoleDN { Name = "ExternalUser" }
                }.Save();

                Schema.Current.Initialize();

                Signum.Test.Starter.Load();

            }
        }

        static void Schema_InitializingApplication(Schema sender)
        {
            AuthLogic.SystemUser = Database.Query<UserDN>().Single(u => u.UserName == AuthLogic.SystemUserName);
        }
    }
}
