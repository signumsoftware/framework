using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221108_DotNet7 : CodeUpgradeBase
{
    public override string Description => "Updates to .Net 7";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.Replace("<TargetFramework>net6.0</TargetFramework>", "<TargetFramework>net7.0</TargetFramework>");
        });

        uctx.ForeachCodeFile(@"deploy*.ps1", file =>
        {
            file.Replace("net6.0", "net7.0");
        });

        uctx.ChangeCodeFile(@"Southwind.React\Dockerfile", file =>
        {
            file.Replace("dotnet/aspnet:6.0.0", "dotnet/aspnet:7.0.0");
            file.Replace("dotnet/sdk:6.0.100", "dotnet/sdk:7.0.100");
            file.WarningLevel = WarningLevel.None;
            file.Replace("dotnet/aspnet:6.0", "dotnet/aspnet:7.0.0");
        });
    }
}



