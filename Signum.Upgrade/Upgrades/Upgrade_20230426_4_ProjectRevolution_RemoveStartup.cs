using Signum.Utilities;
using System;

namespace Signum.Upgrade.Upgrades;

class Upgrade_202304264_ProjectRevolution_RemoveStartup : CodeUpgradeBase
{
    public override string Description => "Project REVOLUTION - remove startup.cs";

    public override void Execute(UpgradeContext uctx)
    {
        var startup = uctx.TryGetCodeFile($"{uctx.ApplicationName}/Startup.cs");

        if (startup == null)
            throw new ApplicationException("startup.cs not found!!");

        uctx.ChangeCodeFile($"{uctx.ApplicationName}/Program.cs", program =>
        {
            var secret = startup.Content.Between("() => Starter.Configuration.Value.AuthTokens, \"", "\");");
            var culture = startup.Content.Between("DefaultCulture = CultureInfo.GetCultureInfo(\"", "\");");

            var servicesBody = startup.GetMethodBody(l => l.Contains("ConfigureServices(IServiceCollection services)"));

            if (servicesBody.Contains("services.AddSwaggerGen(c =>"))
            {
                var swagger = startup.Content.Between("services.AddSwaggerGen(c =>", @"}); //Swagger Services");

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

                servicesBody =
                    servicesBody.Before("services.AddSwaggerGen(c =>") +
                    "        SwaggerConfig.ConfigureSwaggerService(builder); \n\n" +
                    servicesBody.After("}); //Swagger Services");
            }


            var configure = startup.GetMethodBody(a => a.Contains("Configure(IApplicationBuilder app,"));


            var programContent = """
            var builder = WebApplication.CreateBuilder(args);

            """ +
                servicesBody.Replace("services", "builder.Services") +
                "\r\n" +
                "        var app = builder.Build(); \r\n\r\n" +
               configure
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
                "    }\n\n" +
                startup.GetLinesBetween(
                    new(l => l.Contains("class NoAPIContraint : IRouteConstraint"), 0),
                    new(l => l.Contains("}"), 0) { SameIdentation = true }) + "\n";


            program.ReplaceBetween(
                new(l => l.Contains("public class Program"), 4),
                new(l => l == "}", -1), programContent);

            program.RemoveAllLines(a => a.Contains("AddApplicationPart"));

            program.ReplaceBetween(
                new(l => l.Contains(".ConfigureApplicationPartManager(apm =>"), 0),
                new(l => l.Contains("});"), 0), "");

            program.Replace("AddSignumJsonConverters())", "AddSignumJsonConverters());");

            program.ReplaceBetween(
                new(l => l.Contains(@"log.Switch(""WebStart"");"), 0),
                new(l => l.Contains(@"WebStart(app, env"), 0), "");

            var endpoints = program.Content.TryBetween(".UseEndpoints(", "=>")?.Trim() ?? "endpoints";

            program.ReplaceBetween(
                new(a => a.Contains("app.UseEndpoints(")),
                new(a => a.Contains(" });")) { SameIdentation = true },
                a => a.Lines().Skip(2).SkipLast(1).Select(a => a.After("                ").Replace(endpoints, "app")).ToString("\r\n")
                );

            using (program.OverrideWarningLevel(WarningLevel.Warning))
            {
                program.RemoveAllLines(a => a.Contains("AlertsServer.MapAlertsHub"));
                program.RemoveAllLines(a => a.Contains("ConcurrentUserServer.MapConcurrentUserHub"));
            }
        });

        var hasDynamic = false;

        uctx.ChangeCodeFile($"{uctx.ApplicationName}/Starter.cs", starter =>
        {
            starter.ProcessLines(lines =>
            {
                var index = lines.IndexOf(a => a.Contains("\"public static void Start(\""));

                if (index == -1)
                {
                    starter.Warning($@"""public static void Start("" not found!");
                    return false;
                }

                lines[index] = lines[index].Replace("bool detectSqlVersion = true", "");
                lines[index] = lines[index].Replace("bool detectSqlVersion = false", "");
                lines[index] = lines[index].Replace("bool detectSqlVersion ", "");

                lines[index] = lines[index].Before(")") + ", WebServerBuilder? wsb)" + lines[index].After(")");

                return true;

            });


            starter.ReplaceLine(l => l.Contains("SchemaBuilder sb = new CustomSchemaBuilder"),
                @"SchemaBuilder sb = new CustomSchemaBuilder { LogDatabaseName = logDatabase, Tracer = initial, WebServerBuilder = wsb };");

            starter.InsertAfterFirstLine(l => l.Contains("MixinDeclarations.Register<OperationLogEntity"),
                @"MixinDeclarations.Register<EmailMessageEntity, EmailMessagePackageMixin>();");

            starter.ReplaceLine(l => l.Contains("SqlServerVersionDetector.Detect(connectionString)"),
                @"var sqlVersion = wsb == null ? SqlServerVersionDetector.Detect(connectionString) : SqlServerVersion.AzureSQL;");

            using (starter.OverrideWarningLevel(WarningLevel.Warning))
                starter.ReplaceLine(l => l.Contains("PostgresVersionDetector.Detect(connectionString)"),
                    @"var postgreeVersion = wsb == null ? PostgresVersionDetector.Detect(connectionString) : null;");

            starter.InsertBeforeFirstLine(l => l.Contains("CacheLogic.Start"), """

            if (wsb != null)
            {
                SignumServer.Start(wsb);
            }

            """);

            //starter.ReplaceLine(l => l.Contains("DynamicLogic.Start("), "EvalLogic.Start(sb);");

            starter.InsertBeforeFirstLine(l => l.Contains(@"AuthLogic.Start"), "PermissionLogic.Start(sb);");

            starter.ReplaceLine(l => l.Contains("AuthLogic.StartAllModules("),
                @"AuthLogic.StartAllModules(sb, () => Starter.Configuration.Value.AuthTokens);");

            starter.ReplaceLine(l => l.Contains("TranslationLogic.Start"),
                "TranslationLogic.Start(sb, countLocalizationHits: false,\n" +
                startup.GetLinesBetween(
                    new(l => l.Contains("TranslationServer.Start("), 1),
                    new(l => l.Contains(");"), 0))
                .Replace("Starter.Configuration", "Configuration") + "\n");

            using (starter.OverrideWarningLevel(WarningLevel.Warning))
                starter.ReplaceLine(l => l.Contains("WorkflowLogicStarter.Start("),
                    "WorkflowLogicStarter.Start(sb, () => Configuration.Value.Workflow);");

            //starter.InsertAfterFirstLine(l => l.Contains(@"}//3"), """

            //if (wsb != null)
            //    ReflectionServer.RegisterLike(typeof(RegisterUserModel), () => true);
            //""");

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

            starter.InsertAfterFirstLine(a => a.Contains("sb.Schema.Settings.FieldAttributes((EmailSenderConfigurationEntity"), """
                sb.Schema.Settings.FieldAttributes((EmailSenderConfigurationEntity em) => em.Service).Replace(new ImplementedByAttribute(typeof(SmtpEmailServiceEntity), typeof(MicrosoftGraphEmailServiceEntity) /*Remove?*/, typeof(ExchangeWebServiceEmailServiceEntity) /*Remove?*/));
                """);

            starter.ReplaceLine(l => l.Contains("sb.Schema.Settings.FieldAttributes((DashboardEntity a) => a.Parts[0].Content)"), "");

            hasDynamic = starter.Content.Contains("DynamicLogic.CompileDynamicCode");
        });

        if (hasDynamic)
        {
            uctx.ChangeCodeFile("Southwind.Terminal/Program.cs", program =>
            {
                program.InsertBeforeFirstLine(a => a.Contains("Starter.Start("), uctx.ReplaceSouthwind(@"DynamicLogic.CodeGenDirectory = ""../../../../Southwind/CodeGen"";"));
            });
        }

        uctx.DeleteFile("Southwind/Startup.cs");

        uctx.ChangeCodeFile("Southwind/Layout.tsx", layout =>
        {
            layout.Replace("@extensions/Signum.Translation/CultureDropdown", "@framework/Basics/CultureDropdown");
            layout.RemoveAllLines(a => a.Contains("Signum.Omnibox/OmniboxAutocomplete"));

            layout.InsertAfterFirstLine(a => a.Contains("const ToolbarRenderer"),
                "const OmniboxAutocomplete = React.lazy(() => import('@extensions/Signum.Omnibox/OmniboxAutocomplete'));");

        });

        uctx.ChangeCodeFile("Southwind/MainAdmin.tsx", main =>
        {
            using (main.OverrideWarningLevel(WarningLevel.Warning))
                main.Replace(
                    "@extensions/Signum.Authorization/ActiveDirectoryClient",
                    "@extensions/Signum.Authorization.ActiveDirectory/ActiveDirectoryClient");

            main.Replace(
                "Signum.Cache/CacheClient",
                "Signum.Caching/CacheClient");

            main.RemoveAllLines(a => a.Contains("import") && (a.Contains("ToolbarConfig") || a.Contains("ToolbarMenuConfig")));
            main.ReplaceBetweenIncluded(a => a.Contains("ToolbarClient.start("), a => a.Contains(");"),
                "ToolbarClient.start({ routes });");

            main.RemoveAllLines(a => a.Contains("import") && a.Contains("OmniboxProvider"));
            main.ReplaceBetweenIncluded(a => a.Contains("OmniboxClient.start("), a => a.Contains(");"),
                "OmniboxClient.start();");


            if (main.Content.Contains("DynamicClient"))
            {
                if (main.Content.Contains("withCodeGen: true"))
                {
                    main.InsertBeforeFirstLine(a => a.Contains("import * as DynamicClient"), "import * as EvalClient from \"@extensions/Signum.Eval/EvalClient\"");
                    main.InsertBeforeFirstLine(a => a.Contains("DynamicClient.start"), "EvalClient.start({ routes });");
                }
                else
                {
                    main.ReplaceLine(a => a.Contains("import * as DynamicClient"), "import * as EvalClient from \"@extensions/Signum.Eval/EvalClient\"");
                    main.ReplaceLine(a => a.Contains("DynamicClient.start"), "EvalClient.start({ routes });");
                }
            }
        });

        uctx.ChangeCodeFile("Southwind/MainPublic.tsx", main =>
        {
            main.Replace("@extensions/Signum.Translation/CultureClient", "@framework/Basics/CultureClient");
        });
    }
}
