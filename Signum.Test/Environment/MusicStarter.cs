using System;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine;
using System.IO;
using Signum.Entities.Basics;
using Signum.Engine.Operations;
using Signum.Engine.Basics;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Types;

namespace Signum.Test.Environment
{
    public static class MusicStarter
    {
        static bool startedAndLoaded = false;
        public static void StartAndLoad()
        {
            if (startedAndLoaded)
                return;

            lock (typeof(MusicStarter))
            {
                if (startedAndLoaded)
                    return;

                var conf = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                    //.AddJsonFile("appsettings.json")
                    .AddUserSecrets(typeof(MusicStarter).Assembly)
                    .Build();

                var connectionString = conf.GetConnectionString("SignumTest") ?? "Data Source=.\\SQLEXPRESS;Initial Catalog=SignumTest;Integrated Security=true";

                Start(connectionString);

                Administrator.TotalGeneration();

                Schema.Current.Initialize();

                MusicLoader.Load();

                startedAndLoaded = true;
            }
        }

        public static void Start(string connectionString)
        {
            SchemaBuilder sb = new SchemaBuilder(true);

            //Connector.Default = new SqlCeConnector(@"Data Source=C:\BaseDatos.sdf", sb.Schema);

            var sqlVersion = SqlServerVersionDetector.Detect(connectionString);

            Connector.Default = new SqlConnector(connectionString, sb.Schema, sqlVersion ?? SqlServerVersion.SqlServer2017);

            sb.Schema.Version = typeof(MusicStarter).Assembly.GetName().Version;

            sb.Schema.Settings.FieldAttributes((OperationLogEntity ol) => ol.User).Add(new ImplementedByAttribute());
            sb.Schema.Settings.FieldAttributes((ExceptionEntity e) => e.User).Add(new ImplementedByAttribute());

            if(Connector.Current.SupportsTemporalTables)
            {
                sb.Schema.Settings.TypeAttributes<FolderEntity>().Add(new SystemVersionedAttribute());
            }

            if (!Schema.Current.Settings.TypeValues.ContainsKey(typeof(TimeSpan)))
            {
                sb.Settings.FieldAttributes((AlbumEntity a) => a.Songs[0].Duration).Add(new Signum.Entities.IgnoreAttribute());
                sb.Settings.FieldAttributes((AlbumEntity a) => a.BonusTrack.Duration).Add(new Signum.Entities.IgnoreAttribute());
            }

            if(sqlVersion > SqlServerVersion.SqlServer2008)
            {
                sb.Settings.UdtSqlName.Add(typeof(SqlHierarchyId), "HierarchyId");
            }
            else
            {
                sb.Settings.FieldAttributes((LabelEntity a) => a.Node).Add(new Signum.Entities.IgnoreAttribute());
            }

            Validator.PropertyValidator((OperationLogEntity e) => e.User).Validators.Clear();

            TypeLogic.Start(sb);

            OperationLogic.Start(sb);
            ExceptionLogic.Start(sb);

            MusicLogic.Start(sb);

            sb.Schema.OnSchemaCompleted();
        }
    }
}
