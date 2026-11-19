using System.Text.RegularExpressions;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20261119_Dotnet11 : CodeUpgradeBase
{
    public override string Description => "Update to .Net 11";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.csproj", file =>
        {
            file.Replace(
                "<TargetFramework>net10.0</TargetFramework>",
                "<TargetFramework>net11.0</TargetFramework>");
        });

        uctx.ChangeCodeFile(@"Southwind.Server/Dockerfile", file =>
        {
            file.Replace("aspnet:10.0", "aspnet:11.0");
            file.Replace("sdk:10.0", "sdk:11.0");
        });

        uctx.ForeachCodeFile(@"deploy*.ps1", file =>
        {
            file.Replace("net10.0", "net11.0");
        });

        uctx.ChangeCodeFile(@"Southwind.Server/web.config", file =>
        {
            file.Replace("net10.0", "net11.0");
        }, WarningLevel.None);

        uctx.ChangeCodeFile(@".vscode/launch.json", file =>
        {
            file.Replace("net10.0", "net11.0");
        }, WarningLevel.None);

        uctx.ForeachCodeFile("*.pubxml", file =>
        {
            file.Replace(
                new Regex(@"<TargetFramework>net(coreapp)?\d+\.\d+</TargetFramework>"),
                "<TargetFramework>net11.0</TargetFramework>");
        });
    }
}
