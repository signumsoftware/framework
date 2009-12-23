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

namespace Signum.Test.Extensions
{
    public static class Starter
    {
        static bool started = false;
        public static void Start(string connectionString, DynamicQueryManager dqm)
        { 
            if (!started)
            {
                started = true;

                SchemaBuilder sb = new SchemaBuilder();

                Signum.Test.Starter.Start(sb, dqm);

                sb.Settings.OverrideTypeAttributes<IEmployeeDN>(new ImplementedByAttribute());
            
                OperationLogic.Start(sb, dqm);
            
                new AlbumGraph().Register();

                ConnectionScope.Default = new Connection(connectionString, sb.Schema);
            }
        }

        public static void StartAndLoad(string connectionString, DynamicQueryManager dqm)
        { 
            if (!started)
            {
                started = true;

                SchemaBuilder sb = new SchemaBuilder();

                Signum.Test.Starter.Start(sb, Queries.Web);

                sb.Settings.OverrideTypeAttributes<IEmployeeDN>(new ImplementedByAttribute());
            
                OperationLogic.Start(sb, dqm);
            
                new AlbumGraph().Register();

                ConnectionScope.Default = new Connection(connectionString, sb.Schema);

                Administrator.TotalGeneration();

                Schema.Current.Initialize();

                Signum.Test.Starter.Load();

                new UserDN
                {
                    UserName = "test",
                    PasswordHash = Security.EncodePassword("test"),
                    Role = new RoleDN { Name = "ExternalUser" }
                }.Save();
            }
        }
    }
}
