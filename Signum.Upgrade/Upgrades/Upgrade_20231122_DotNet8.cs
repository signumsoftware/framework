using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20231122_DotNet8 : CodeUpgradeBase
{
    public override string Description => "Migrate to .Net 8";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.Replace("<TargetFramework>net7.0</TargetFramework>", "<TargetFramework>net8.0</TargetFramework>");
            file.UpdateNugetReference("Signum.TSGenerator", "8.0.0");
            file.UpdateNugetReference("Signum.MSBuildTask", "8.0.0");
        });

        uctx.ForeachCodeFile(@"deploy*.ps1", file =>
        {
            file.Replace("net7.0", "net8.0");
        });

        uctx.ChangeCodeFile(@"Southwind\Dockerfile", file =>
        {
            file.Replace("bullseye-slim", "bookworm-slim");
            file.Replace("dotnet/aspnet:7.", "dotnet/aspnet:8.");
            file.Replace("dotnet/sdk:7.", "dotnet/sdk:8.");
            file.WarningLevel = WarningLevel.None;
            file.Replace("dotnet/aspnet:7.0", "dotnet/aspnet:8.0.0");
        });
    }
}



