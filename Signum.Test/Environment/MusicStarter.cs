using Signum.Engine.Maps;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Types;
using Signum.Basics;
using Npgsql;
using Npgsql.Internal;

namespace Signum.Test.Environment;

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

            var environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? throw new InvalidOperationException("ASPNETCORE_ENVIRONMENT environment variable must be set to 'SqlServer' or 'Postgres'");
            
            var conf = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = conf.GetConnectionString("SignumTest") 
                ?? throw new InvalidOperationException($"No connection string found in appsettings.{environment}.json");

            Console.WriteLine($"[MusicStarter] Environment: {environment}");
            Console.WriteLine($"[MusicStarter] Connection string: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
            
            Start(connectionString);

            Administrator.TotalGeneration(interactive: false);

            Schema.Current.Initialize();

            (Connector.Current as PostgreSqlConnector)?.ReloadTypes();

            MusicLoader.Load();

            if (Connector.Current is SqlServerConnector)
                Schema.Current.Table<SimplePassageEntity>().AllIndexes().OfType<VectorTableIndex>().SingleEx().CreateVectorIndex();

            startedAndLoaded = true;
        }
    }

    public static void Start(string connectionString)
    {
        SchemaBuilder sb = new SchemaBuilder();

        if (connectionString.Contains("Data Source"))
        {
            var sqlVersion = SqlServerVersionDetector.Detect(connectionString, SqlServerVersion.SqlServer2017);
            Connector.Default = new SqlServerConnector(connectionString, sb.Schema, sqlVersion);
        }
        else
        {
            var postgreeVersion = PostgresVersionDetector.Detect(connectionString, null);
            Connector.Default = new PostgreSqlConnector(connectionString, sb.Schema, postgreeVersion, builder =>
            {
                builder.EnableArrays();
                builder.EnableLTree();
                builder.EnableRanges();
                builder.UseVector();
            });
        }

        sb.Schema.Version = typeof(MusicStarter).Assembly.GetName().Version!;
        sb.Schema.Settings.ImplementedByAllPrimaryKeyTypes.Add(typeof(long));
        sb.Schema.Settings.ImplementedByAllPrimaryKeyTypes.Add(typeof(Guid));
        sb.Schema.Settings.FieldAttributes((OperationLogEntity ol) => ol.User).Add(new ImplementedByAttribute());
        sb.Schema.Settings.FieldAttributes((ExceptionEntity e) => e.User).Add(new ImplementedByAttribute());

        Lite.RegisterLiteModelConstructor((AmericanMusicAwardEntity a) => new AwardLiteModel { Category = a.Category, Year = a.Year, Type = "AMA" });
        Lite.RegisterLiteModelConstructor((GrammyAwardEntity a) => new AwardLiteModel { Category = a.Category, Year = a.Year, Type = "Grammy" }, isDefault: false);

        if (Connector.Current.SupportsTemporalTables)
        {
            sb.Schema.Settings.TypeAttributes<FolderEntity>().Add(new SystemVersionedAttribute());
        }

        if(Connector.Default is SqlServerConnector c && c.Version > SqlServerVersion.SqlServer2008)
        {
            sb.Settings.UdtSqlName.Add(typeof(SqlHierarchyId), "HierarchyId");
        }
        //else
        //{
        //    sb.Settings.FieldAttributes((LabelEntity a) => a.Node).Add(new Signum.Entities.IgnoreAttribute());
        //}

        Validator.PropertyValidator((OperationLogEntity e) => e.User).Validators.Clear();

        TypeLogic.Start(sb);

        OperationLogic.Start(sb);
        ExceptionLogic.Start(sb);

        QueryLogic.Start(sb);
        MusicLogic.Start(sb);

        sb.Schema.OnSchemaCompleted();
    }
}
