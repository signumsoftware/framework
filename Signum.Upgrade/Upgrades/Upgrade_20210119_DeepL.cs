using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20210119_DeepL : CodeUpgradeBase
    {
        public override string Description => "Add DeepL support and parallel translations";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile(@"Southwind.Entities/ApplicationConfiguration.cs ", file =>
            {
                file.InsertBeforeFirstLine(a => a.Contains("[AutoInit]"),
@"
[Serializable]
public class TranslationConfigurationEmbedded : EmbeddedEntity
{
    [Description(""Azure Cognitive Service API Key"")]
    [StringLengthValidator(Max = 300), FileNameValidator]
    public string? AzureCognitiveServicesAPIKey { get; set; }

    [Description(""Azure Cognitive Service Region"")]
    [StringLengthValidator(Max = 300), FileNameValidator]
    public string? AzureCognitiveServicesRegion { get; set; }

    [Description(""DeepL API Key"")]
    [StringLengthValidator(Max = 300), FileNameValidator]
    public string? DeepLAPIKey { get; set; }
}

");

                file.InsertAfterFirstLine(a => a.Contains("public FoldersConfigurationEmbedded Folders { get; set; }"),
@"
public TranslationConfigurationEmbedded Translation { get; set; }");
            });

            uctx.ChangeCodeFile(@"Southwind.React/App/Southwind/Templates/ApplicationConfiguration.tsx", file =>
            {
                file.InsertAfterLastLine(a => a.Contains("</Tab>"),
@"<Tab eventKey=""translation"" title={ctx.niceName(a => a.translation)}>
    <RenderEntity ctx={ctx.subCtx(a => a.translation)} />
</Tab>");

            });

            uctx.ChangeCodeFile(@"Southwind.React/Startup.cs", file =>
            {
                file.ReplaceLine(a => a.Contains("TranslationServer.Start(app, new AlreadyTranslatedTranslator"),
@"TranslationServer.Start(app,
    new AlreadyTranslatedTranslator(),
    new AzureTranslator(
        () => Starter.Configuration.Value.Translation.AzureCognitiveServicesAPIKey,
        () => Starter.Configuration.Value.Translation.AzureCognitiveServicesRegion),
    new DeepLTranslator(() => Starter.Configuration.Value.Translation.DeepLAPIKey)
);");

            });

            var azureKey = SafeConsole.AskString("Azure API Key?");
            var deeplKey = SafeConsole.AskString("DeepL API Key?");

            uctx.ChangeCodeFile(@"Southwind.Terminal\SouthwindMigrations.cs", file =>
            {
                file.InsertBeforeFirstLine(a => a.Contains("Folders = new FoldersConfigurationEmbedded"),
@$"Translation = new TranslationConfigurationEmbedded
{{
    AzureCognitiveServicesAPIKey = ""{azureKey}"",
    DeepLAPIKey = ""{deeplKey}"",
}},");
            });

            uctx.ChangeCodeFile(@"Southwind.Test.Environment/SouthwindEnvironment.cs", file =>
                {
                    file.InsertBeforeFirstLine(a => a.Contains("Folders = new FoldersConfigurationEmbedded"),
    @$"Translation = new TranslationConfigurationEmbedded
{{
    AzureCognitiveServicesAPIKey = ""{azureKey}"",
    DeepLAPIKey = ""{deeplKey}"",
}},");

                });
        }
    }
}
