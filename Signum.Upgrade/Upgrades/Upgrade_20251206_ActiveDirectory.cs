using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251206_ActiveDirectory : CodeUpgradeBase
{
    public override string Description => "Update from ActiveDirectory to AzureAD or WindowsAD";

    public override void Execute(UpgradeContext uctx)
    {
        var adType = SafeConsole.AskRetry("What is this application using?", "AzureAD", "WindowsAD", "None");


        uctx.ChangeCodeFile("Southwind/Starter.cs", file =>
        {
            file.Replace("ActiveDirectory", adType);
            file.Replace("UserADMixin", adType == "AzureAD" ? "UserAzureADMixin" : "UserWindowsADMixin");

            if (adType == "AzureAD")
                file.Replace(
                        "new SouthwindAuthorizer(() => ".Replace("Southwind", uctx.ApplicationName),
                        "new SouthwindAuthorizer(adVariant => ".Replace("Southwind", uctx.ApplicationName));
        });


        uctx.ChangeCodeFile("Southwind/SouthwindAuthorizer.cs", file =>
        {
            file.Replace("ActiveDirectory", adType);

            file.Replace(
             "public SouthwindAuthorizer(Func<ActiveDirectoryConfigurationEmbedded> getConfig))".Replace("Southwind", uctx.ApplicationName),
             $"public SouthwindAuthorizer(Func<{adType}ConfigurationEmbedded?> getConfig)".Replace("Southwind", uctx.ApplicationName));

            if (adType == "AzureAD")
                file.Replace(
                "public SouthwindAuthorizer(Func<AzureADConfigurationEmbedded?> getConfig)".Replace("Southwind", uctx.ApplicationName),
                "public SouthwindAuthorizer(Func<string?, AzureADConfigurationEmbedded?> getConfig)".Replace("Southwind", uctx.ApplicationName));
        });

        uctx.ChangeCodeFile("Southwind.Terminal/Program.cs", file =>
        {
            file.Replace(
                """{"HL", HelpXml.ImportExportHelp},""",
                """{"HL", HelpExportImport.ImportExportHelpMenu},""");
        });

        if (adType == "AzureAD")
        {
            uctx.ChangeCodeFile("Southwind.Server/Index.cshtml", file =>
            {
                file.Replace(
                    "ActiveDirectory.AzureAD?.ToAzureADConfigTS());",
                    "AzureAD?.ToAzureADConfigTS());");

                file.Replace(
                    "AzureAD?.ToAzureADConfigTS());",
                    "AzureAD?.ToAzureADConfigTS(null));");
            });
        }

        uctx.ChangeCodeFile("Southwind/Globals/ApplicationConfigurationEntity.cs", file =>
        {
            file.Replace(
                "public ActiveDirectoryConfigurationEmbedded ActiveDirectory { get; set; }",
                $$"""public {{adType}}ConfigurationEmbedded? {{adType}} { get; set; }""");

        });

        uctx.ChangeCodeFile("Southwind/Globals/ApplicationConfiguration.tsx", file =>
        {
            file.ReplaceBetweenIncluded(a => a.Contains("<Tab") && a.Contains(".activeDirectory)"),
                a => a.EndsWith("</Tab>"),
                $$"""
                <Tab eventKey="activeDirectory" title={ctx.niceName(a => a.{{adType.FirstLower()}})}>
                  <EntityDetail ctx={ctx.subCtx(a => a.{{adType.FirstLower()}})} />
                </Tab>
                """);
        });

        uctx.ChangeCodeFile("Southwind/tsconfig.json", file =>
        {
            file.Replace("ActiveDirectory", adType);
        });

        uctx.ChangeCodeFile("Southwind/MainAdmin.tsx", file =>
        {
            file.Replace("ActiveDirectory", adType);
        });

        uctx.ChangeCodeFile("Southwind/MainPublic.tsx", file =>
        {
            file.Replace("ActiveDirectory", adType);
            file.Replace(adType + "Client", adType + "Authenticator");
        });

        uctx.ChangeCodeFile("Southwind/Southwind.csproj", file =>
        {
            file.Replace("ActiveDirectory", adType);
        });

    }
}
