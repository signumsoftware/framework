using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Signum.Upgrade.Upgrades;

class Upgrade_202304264_ProjectRevolution_RemoveStartup : CodeUpgradeBase
{
    public override string Description => "Project REVOLUTION - remove startup.cs";

    public override void Execute(UpgradeContext uctx)
    {
        var startup = uctx.TryGetCodeFile($"{uctx.ApplicationName}/startup.cs");

        if (startup == null)
            throw new ApplicationException("startup.cs not found!!");

        if (startup.Content.Contains("services.AddSwaggerGen(c =>"))
        {

            var sweagerContent = startup.Content.Before("public class Startup").Replace("Signum.React.Filters.", "") + "\n" +
                """
                internal static class SwaggerConfig
                {
                    internal static void ConfigureSwaggerService(WebApplicationBuilder builder)
                    {
                        //https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-2.1&tabs=visual-studio%2Cvisual-studio-xml            
                        builder.Services.AddSwaggerGen(c =>
                """ +
                startup.Content.After("services.AddSwaggerGen(c =>").Before(@"}); //Swagger Services") +
                @"}); //Swagger Services" + "\n" +
                """
                    }
                }
            
                """;

            uctx.CreateCodeFile($"{uctx.ApplicationName}/SwaggerConfig.cs", sweagerContent);
        }

        var program = uctx.TryGetCodeFile($"{uctx.ApplicationName}/program.cs");
        if (program == null)
            throw new ApplicationException("program.cs not found!!");

        var programContent = """
                    var builder = WebApplication.CreateBuilder(args);
                    builder.Services.AddResponseCompression();
            """ +
            startup.GetLinesBetween(l => l.Contains("services.AddResponseCompression()"), 1,
            l => l.Contains(@"//JSon.Net requires it"), 0).Replace("services", "builder.Services") + "\n" +
            "        SwaggerConfig.ConfigureSwaggerService(builder); \n\n" +
            "        var app = builder.Build(); \n\n" +
            startup.GetLinesBetween(l => l.Contains("public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)"), 2,
                l => l.Contains("class NoAPIContraint : IRouteConstraint"), -3)
                    .Replace("Configuration.", "app.Configuration.")
                    .Replace("LogStartStop(app, lifetime)", "LogStartStop(app, app.Lifetime)")
                    .Replace("DynamicCode.CodeGenDirectory = env.", "DynamicLogic.CodeGenDirectory = app.Environment.")
                    .Replace("detectSqlVersion: false", """
                        {
                            WebApplication = app,
                            AuthTokenEncryptionKey = "IMPORTANT SECRET FROM Southwind. CHANGE THIS STRING!!!",
                            MachineName = app.Configuration.GetValue<string?>("ServerName"),
                            DefaultCulture = CultureInfo.GetCultureInfo("en")
                        });
                        """) + 
            "\n" +
            "        app.Run(); \n" +
            "    }\n" +
            startup.GetLinesBetween(l => l.Contains("class NoAPIContraint : IRouteConstraint"), 0,
            l => l == "    }", 0) + "\n";

        program.ReplaceBetween(l => l.Contains("public class Program"), 4,
            l => l == "}", -1, programContent);

        program.ReplaceBetween(l => l.Contains(".AddJsonOptions(options => options.AddSignumJsonConverters())"), 0,
            l => l.Contains("});"), 0, ".AddJsonOptions(options => options.AddSignumJsonConverters());");

        program.ReplaceBetween(l => l.Contains(@"log.Switch(""WebStart"");"), 0,
            l => l.Contains(@"WebStart(app, env, lifetime"), 0, "");

        program.SaveIfNecessary();


        var starter = uctx.TryGetCodeFile($"{uctx.ApplicationName}/starter.cs");
        if (starter == null)
            throw new ApplicationException("starter.cs not found!!");

        starter.ReplaceLine(l => l.Contains("public static void Start("), 
            @"public static void Start(string connectionString, bool isPostgres, string? azureStorageConnectionString, string? broadcastSecret, string? broadcastUrls, WebServerBuilder? wsb, bool includeDynamic = true)");

        starter.ReplaceLine(l => l.Contains("SchemaBuilder sb = new CustomSchemaBuilder"),
            @"SchemaBuilder sb = new CustomSchemaBuilder { LogDatabaseName = logDatabase, Tracer = initial, WebServerBuilder = wsb };");

        starter.InsertAfterFirstLine(l => l.Contains("MixinDeclarations.Register<OperationLogEntity"),
            @"MixinDeclarations.Register<EmailMessageEntity, EmailMessagePackageMixin>();");

        starter.ReplaceLine(l => l.Contains("SqlServerVersionDetector.Detect(connectionString)"),
            @"var sqlVersion = wsb == null ? SqlServerVersionDetector.Detect(connectionString) : SqlServerVersion.AzureSQL;");

        starter.ReplaceLine(l => l.Contains("PostgresVersionDetector.Detect(connectionString)"),
            @"var postgreeVersion = wsb == null ? PostgresVersionDetector.Detect(connectionString) : null;");

        starter.InsertBeforeFirstLine(l => l.Contains("CacheLogic.Start(sb, serverBroadcast"), """

            if (wsb != null)
            {
                SignumServer.Start(wsb);

                ReflectionServer.RegisterLike(typeof(RegisterUserModel), () => true);
            }

            """);

        starter.ReplaceLine(l => l.Contains("DynamicLogic.Start("), "EvalLogic.Start(sb);");

        starter.InsertBeforeFirstLine(l => l.Contains(@"// Extensions modules"), "PermissionLogic.Start(sb);");

        starter.ReplaceLine(l => l.Contains("AuthLogic.StartAllModules("),
            @"AuthLogic.StartAllModules(sb, () => Starter.Configuration.Value.AuthTokens);");

        starter.ReplaceLine(l => l.Contains("OmniboxLogic.Start("),
            "OmniboxLogic.Start(sb, \n" +
            startup.GetLinesBetween(l => l.Contains("OmniboxServer.Start("), 1, l => l.Contains(");"), 0) + "\n");

        starter.ReplaceLine(l => l.Contains("TranslationLogic.Start"),
            "TranslationLogic.Start(sb, countLocalizationHits: false,\n" +
            startup.GetLinesBetween(l => l.Contains("TranslationServer.Start("), 1, l => l.Contains(");"), 0).Replace("Starter.Configuration", "Configuration") + "\n");

        starter.ReplaceLine(l => l.Contains("WorkflowLogicStarter.Start("),
            "WorkflowLogicStarter.Start(sb, () => Configuration.Value.Workflow);");

        starter.InsertAfterFirstLine(l => l.Contains(@"}//3"), """
            
            if (wsb != null)
                ReflectionServer.RegisterLike(typeof(RegisterUserModel), () => true);
            """);

        starter.ReplaceBetween(l => l.Contains("public override ObjectName GenerateTableName("), 0,
            l => l.Contains("public Type[] InLogDatabase = new Type[]"), -1, "");

        starter.ReplaceLine(l => l.Contains("GetDatabaseName("),
            "public override DatabaseName? GetDatabase(Type type)");

        starter.ReplaceBetween(l => l.Contains("GetSchemaNameName("), 0,
            l => l.Contains("Impossible to determine SchemaName"), 1, "");

        starter.InsertAfterFirstLine(l => l.Contains("sb.Schema.Settings.FieldAttributes((SystemEventLogEntity a)"),
            """
            
            sb.Schema.Settings.FieldAttributes((ToolbarEntity tb) => tb.Elements.First().Content).Replace(new ImplementedByAttribute(typeof(ToolbarMenuEntity), typeof(ToolbarEntity), typeof(QueryEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(DashboardEntity), typeof(PermissionSymbol)));
            sb.Schema.Settings.FieldAttributes((ToolbarMenuEntity tbm) => tbm.Elements.First().Content).Replace(new ImplementedByAttribute(typeof(ToolbarMenuEntity), typeof(ToolbarEntity), typeof(QueryEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(DashboardEntity), typeof(PermissionSymbol)));

            """);

        starter.InsertAfterFirstLine(l => l.Contains("sb.Schema.Settings.FieldAttributes((DashboardEntity cp) => cp.Owner)"),
            """

            sb.Schema.Settings.FieldAttributes((DashboardEntity a) => a.Parts.First().Content).Replace(new ImplementedByAttribute(typeof(UserChartPartEntity), typeof(CombinedUserChartPartEntity), typeof(UserQueryPartEntity), typeof(ValueUserQueryListPartEntity), typeof(LinkListPartEntity)));

            sb.Schema.Settings.FieldAttributes((CachedQueryEntity a) => a.UserAssets.First()).Replace(new ImplementedByAttribute(typeof(UserQueryEntity), typeof(UserChartEntity)));

            """);

        starter.ReplaceLine(l => l.Contains("sb.Schema.Settings.FieldAttributes((DashboardEntity a) => a.Parts[0].Content)"),"");
        
        starter.InsertAfterLastLine(l => l.StartsWith("}"), """
            
            internal class SouthwindAuthorizer : ActiveDirectoryAuthorizer
            {
                public SouthwindAuthorizer(Func<ActiveDirectoryConfigurationEmbedded> getConfig) : base(getConfig)
                {
                }

                public override void UpdateUserInternal(UserEntity user, IAutoCreateUserContext ctx)
                {
                    base.UpdateUserInternal(user, ctx);

                    //user.Mixin<UserADMixin>().FirstName = ctx.FirstName;
                }
            }
            
            """);
        starter.SaveIfNecessary();

        File.Delete(Path.Combine(uctx.RootFolder, uctx.ApplicationName, "startup.cs"));
    }
}
