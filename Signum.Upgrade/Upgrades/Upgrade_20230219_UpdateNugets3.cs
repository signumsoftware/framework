using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230219_UpdateNugets3 : CodeUpgradeBase
{
    public override string Description => "Updates Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                    <PackageReference Include="Selenium.Support" Version="4.8.1" />
                    <PackageReference Include="Selenium.WebDriver" Version="4.8.1" />
                    <PackageReference Include="Microsoft.Graph" Version="4.54.0" />
                    <PackageReference Include="TensorFlow.Keras" Version="0.10.2" />
                    <PackageReference Include="Microsoft.IdentityModel.Protocols.OpenIdConnect" Version="6.27.0" />
                    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="6.27.0" />
                    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="6.27.0" />
                    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
                    <PackageReference Include="SciSharp.TensorFlow.Redist" Version="2.11.0" />
                    """);
        });
    }
}



