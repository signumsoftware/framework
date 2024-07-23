using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240702_IsolatedDeclarations : CodeUpgradeBase
{
    public override string Description => "";

    public override void Execute(UpgradeContext uctx)
    {
        var regexDefault = new Regex(@"export (?<def>default )?function *(?<name>[A-Z]\w+) *\((?<props>[^)]*)\) *{");
        var regexStart = new Regex(@"export function start *\((?<props>[^)]*)\) *{");
        uctx.ForeachCodeFile(@"*.tsx", file =>
        {
            file.Replace(regexDefault, a =>
            {
                return $"export {a.Groups["def"]}function {a.Groups["name"].Value}({a.Groups["props"].Value}): React.JSX.Element {{";
            });

            file.Replace(regexStart, a =>
            {
                return $"export function start({a.Groups["props"].Value}): void {{";
            });
        });

        uctx.ForeachCodeFile(@"tsconfig.json", file =>
        {
            if (uctx.AbsolutePathSouthwind("Southwind\\tsconfig.json") == file.FilePath)
                file.InsertAfterFirstLine(a => a.Contains(@"""target"":"), @"""isolatedDeclarations"": true,");
            else
                file.InsertAfterFirstLine(a => a.Contains(@"""target"":"), @"//""isolatedDeclarations"": true,");
        });

        uctx.ForeachCodeFile(@"Changelog.ts", file =>
        {
            file.Replace("satisfies ChangeLogDic", "as ChangeLogDic");
        });

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Selenium.WebDriver.ChromeDriver" Version="126.0.6478.12600" />
                <PackageReference Include="Selenium.WebDriver" Version="4.22.0" />
                <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.5.3" />
                <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.21.0" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
                <PackageReference Include="Signum.TSGenerator" Version="8.5.1" />
                """);
        });

        uctx.ChangeCodeFile(@"Southwind.Server/package.json", file =>
        {
        file.UpdateNpmPackages("""
            "css-loader": "7.1.2",
            "rimraf": "5.0.7",
            "sass-loader": "14.2.1",
            "style-loader": "3.3.4",
            "ts-loader": "9.5.1",
            "webpack": "5.92.1",
            "webpack-bundle-analyzer": "4.10.2",
            "typescript": "5.5.3",
            """);
        });
    }
}



