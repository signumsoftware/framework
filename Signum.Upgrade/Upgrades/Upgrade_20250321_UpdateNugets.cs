using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250321_UpdateNugets : CodeUpgradeBase
{
    public override string Description => "";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.18.4" />
                <PackageReference Include="Azure.Storage.Blobs" Version="12.24.0" />
                <PackageReference Include="DeepL.net" Version="1.14.0" />
                <PackageReference Include="DocumentFormat.OpenXml" Version="3.3.0" />
                <PackageReference Include="HtmlAgilityPack" Version="1.12.0" />
                <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.13.0" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.3" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.3" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.3" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.3" />
                <PackageReference Include="Microsoft.Graph" Version="5.74.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.8.1">
                <PackageReference Include="Npgsql" Version="9.0.3" />
                <PackageReference Include="Selenium.Support" Version="4.29.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.29.0" />
                <PackageReference Include="SixLabors.ImageSharp" Version="2.1.10" />
                <PackageReference Include="System.DirectoryServices" Version="9.0.3" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="9.0.3" />
                <PackageReference Include="System.Drawing.Common" Version="9.0.3" />
                <PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.3" />
                <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2">
                <PackageReference Include="OpenTelemetry" Version="1.11.2" />
                <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.11.2" />
                <PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.11.2" />
                <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.11.2" />
                <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.11.1" />
                <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.11.1" />
                """);

            file.ReplaceLine(a => a.Contains(@"Include=""xunit"""), @"<PackageReference Include=""xunit.v3"" Version=""2.0.0"" />");
        });

    }

 
}



