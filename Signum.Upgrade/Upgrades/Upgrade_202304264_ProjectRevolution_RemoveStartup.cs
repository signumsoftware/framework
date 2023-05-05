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

        bool hasSwagger = false;
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

            hasSwagger = true;
        }

        uctx.ChangeCodeFile($"{uctx.ApplicationName}/Program.cs", program =>
        {
            var secret = startup.Content.Between("() => Starter.Configuration.Value.AuthTokens, \"", "\");");
            var culture = startup.Content.Between("DefaultCulture = CultureInfo.GetCultureInfo(\"", "\");");

            var programContent = """
                    var builder = WebApplication.CreateBuilder(args);
            """ +
                startup.GetMethodBody(l => l.Contains("ConfigureServices(IServiceCollection services)"))
                .Replace("services", "builder.Services") +

                "\n" +
                (hasSwagger ? "        SwaggerConfig.ConfigureSwaggerService(builder); \n\n" : null) +
                "        var app = builder.Build(); \n\n" +
                startup.GetMethodBody(a => a.Contains("Configure(IApplicationBuilder app,"))
                        .Replace("Configuration.", "app.Configuration.")
                        .Replace("lifetime", "app.Lifetime")
                        .Replace("DynamicCode.CodeGenDirectory = env.", "DynamicLogic.CodeGenDirectory = app.Environment.")
                        .Replace("detectSqlVersion: false", $$"""
                        new WebServerBuilder
                        {
                            WebApplication = app,
                            AuthTokenEncryptionKey = "{{secret}}",
                            MachineName = app.Configuration.GetValue<string?>("ServerName"),
                            DefaultCulture = CultureInfo.GetCultureInfo("{{culture}}")
                        }
                        """) +
                "\n" +
                "        app.Run(); \n" +
                "    }\n" +
                startup.GetLinesBetween(
                    new(l => l.Contains("class NoAPIContraint : IRouteConstraint"), 0),
                    new(l => l.Contains("}"), 0) { SameIdentation = true}) + "\n";

            program.ReplaceBetween(
                new(l => l.Contains("public class Program"), 4),
                new(l => l == "}", -1), programContent);

            program.ReplaceBetween(
                new(l => l.Contains(".ConfigureApplicationPartManager(apm =>"), 0),
                new(l => l.Contains("});"), 0), "");

            program.ReplaceBetween(
                new(l => l.Contains(@"log.Switch(""WebStart"");"), 0),
                new(l => l.Contains(@"WebStart(app, env, lifetime"), 0), "");

            program.ReplaceBetween(
                new(a => a.Contains("app.UseEndpoints(endpoints =>")),
                new(a => a.Contains(" });")) { SameIdentation = true },
                a => a.Lines().Skip(2).SkipLast(1).Select(a => a.After("    ").Replace("endpoints", "app")).ToString("\r\n")
                );
        });

        uctx.ChangeCodeFile($"{uctx.ApplicationName}/Starter.cs", starter =>
        {

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
            }

            """);

            starter.ReplaceLine(l => l.Contains("DynamicLogic.Start("), "EvalLogic.Start(sb);");

            starter.InsertBeforeFirstLine(l => l.Contains(@"// Extensions modules"), "PermissionLogic.Start(sb);");

            starter.ReplaceLine(l => l.Contains("AuthLogic.StartAllModules("),
                @"AuthLogic.StartAllModules(sb, () => Starter.Configuration.Value.AuthTokens);");

            starter.ReplaceLine(l => l.Contains("TranslationLogic.Start"),
                "TranslationLogic.Start(sb, countLocalizationHits: false,\n" +
                startup.GetLinesBetween(
                    new(l => l.Contains("TranslationServer.Start("), 1),
                    new(l => l.Contains(");"), 0))
                .Replace("Starter.Configuration", "Configuration") + "\n");

            starter.ReplaceLine(l => l.Contains("WorkflowLogicStarter.Start("),
                "WorkflowLogicStarter.Start(sb, () => Configuration.Value.Workflow);");

            starter.InsertAfterFirstLine(l => l.Contains(@"}//3"), """
            
            if (wsb != null)
                ReflectionServer.RegisterLike(typeof(RegisterUserModel), () => true);
            """);

            starter.ReplaceBetween(
                new(l => l.Contains("public override ObjectName GenerateTableName("), 0),
                new(l => l.Contains("public Type[] InLogDatabase = new Type[]"), -1), "");

            starter.ReplaceLine(l => l.Contains("GetDatabaseName("),
                "public override DatabaseName? GetDatabase(Type type)");

            starter.ReplaceBetween(
                new(l => l.Contains("GetSchemaNameName("), 0),
                new(l => l.Contains("Impossible to determine SchemaName"), 1), "");

            starter.InsertAfterLastLine(l => l.Contains("sb.Schema.Settings.FieldAttributes"),
                """
            
            sb.Schema.Settings.FieldAttributes((ToolbarEntity tb) => tb.Elements.First().Content).Replace(new ImplementedByAttribute(typeof(ToolbarMenuEntity), typeof(ToolbarEntity), typeof(QueryEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(DashboardEntity), typeof(PermissionSymbol)));
            sb.Schema.Settings.FieldAttributes((ToolbarMenuEntity tbm) => tbm.Elements.First().Content).Replace(new ImplementedByAttribute(typeof(ToolbarMenuEntity), typeof(ToolbarEntity), typeof(QueryEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(DashboardEntity), typeof(PermissionSymbol)));

            """);

            starter.InsertAfterFirstLine(l => l.Contains("sb.Schema.Settings.FieldAttributes((DashboardEntity cp) => cp.Owner)"),
                """

            sb.Schema.Settings.FieldAttributes((DashboardEntity a) => a.Parts.First().Content).Replace(new ImplementedByAttribute(typeof(UserChartPartEntity), typeof(CombinedUserChartPartEntity), typeof(UserQueryPartEntity), typeof(ValueUserQueryListPartEntity), typeof(LinkListPartEntity)));
            sb.Schema.Settings.FieldAttributes((CachedQueryEntity a) => a.UserAssets.First()).Replace(new ImplementedByAttribute(typeof(UserQueryEntity), typeof(UserChartEntity)));

            """);

            starter.ReplaceLine(l => l.Contains("sb.Schema.Settings.FieldAttributes((DashboardEntity a) => a.Parts[0].Content)"), "");
        });

        uctx.DeleteFile("Southwind/Startup.cs");

        uctx.ChangeCodeFile("Southwind/Layout.tsx", layout =>
        {
            layout.Replace("@extensions/Signum.Translation/CultureDropdown", "@framework/Basics/CultureDropdown");
            layout.RemoveAllLines(a => a.Contains("import OmniboxAutoComplete"));

            layout.InsertAfterFirstLine(a => a.Contains("const ToolbarRenderer"),
                "const OmniboxAutocomplete = React.lazy(() => import('@extensions/Signum.Omnibox/OmniboxAutocomplete'));");

        });

        uctx.ChangeCodeFile("Southwind/MainAdmin.tsx", main =>
        {
            main.Replace(
                "@extensions/Signum.Authorization/ActiveDirectoryClient",
                "@extensions/Signum.Authorization.ActiveDirectory/ActiveDirectoryClient");

            main.Replace(
                "Signum.Cache/CacheClient",
                "Signum.Caching/CacheClient");

            main.RemoveAllLines(a => a.Contains("import") && a.Contains("ToolbarConfig"));
            main.ReplaceBetweenIncluded(a => a.Contains("ToolbarClient.start("), a => a.Contains(");"), 
                "ToolbarClient.start({ routes });");

            main.RemoveAllLines(a => a.Contains("import") && a.Contains("OmniboxProvider"));
            main.ReplaceBetweenIncluded(a => a.Contains("OmniboxClient.start("), a => a.Contains(");"),
                "OmniboxClient.start();");

            main.ReplaceLine(a => a.Contains("import * as DynamicClient"),
                "import * as EvalClient from \"@extensions/Signum.Eval/EvalClient\"");

            main.ReplaceLine(a => a.Contains("DynamicClient.start({"), "EvalClient.start({ routes });");
        });

        uctx.ChangeCodeFile("Southwind/MainPublic.tsx", main =>
        {
            main.Replace("@extensions/Signum.Translation/CultureClient", "@framework/Basics/CultureClient");
        });
    }
}
