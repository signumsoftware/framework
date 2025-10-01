using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250930_TSC : CodeUpgradeBase
{
    public override string Description => "Prepare for using tsc (via Signum.TSBuild.vsix) instead of Microsoft.TypeScript.MSBuild) ";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.MoveFile(@"Southwind/HomeController.cs", "Southwind.Server/HomeController.cs");
        uctx.MoveFile(@"Southwind/Index.cshtml", "Southwind.Server/Index.cshtml");
        uctx.ChangeCodeFile(@"Southwind.Server/package.json", cf => {
            cf.ReplaceLine(a => a.Contains("\"name\":"), $"\"name\": \"{uctx.ApplicationName.ToLower()}.server\",");

        });


        uctx.CreateCodeFile(@"Southwind.Server/tsconfig.json", $$"""
            {
              "extends": "../Framework/tsconfig.base.json",
              "compilerOptions": {
                "isolatedDeclarations": false,
                "outDir": "./ts_out"
              },
              "references": [
                { "path": "../{{uctx.ApplicationName}}" }
              ]
            }
            """);

        uctx.ChangeCodeFile(@"Southwind/tsconfig.json", cf =>
        {
            cf.InsertBeforeFirstLine(a => a.Contains("compilerOptions"), """
                "extends": "../Framework/tsconfig.base.json",
                """);

            cf.RemoveAllLines(a =>
            a.Contains(@"""target"":") ||
            a.Contains(@"""isolatedDeclarations"":") ||
            a.Contains(@"""sourceMap"":") ||
            a.Contains(@"""module"":") ||
            a.Contains(@"""moduleResolution"":") ||
            a.Contains(@"""allowSyntheticDefaultImports"":") ||
            a.Contains(@"""jsx"":") ||
            a.Contains(@"""incremental"":") ||
            a.Contains(@"""composite"":") ||
            a.Contains(@"""noEmit"":") ||
            a.Contains(@"""strict"":"));

            cf.ReplaceBetweenIncluded(a => a.Contains(@"""lib"""), a => a.Contains("]"), "");
        });

        uctx.CreateCodeFile(@"Southwind/package.json", $$"""
            {
              "name": "{{uctx.ApplicationName.ToLower()}}",
              "version": "1.0.0",
              "description": "Southwind application",
              "repository": "",
              "keywords": [],
              "author": "Signum Software",
              "license": "MIT",
              "resolutions": {
              },
              "dependencies": {
              }
            }
            
            """);

        uctx.ChangeCodeFile(@"Southwind.Server/Southwind.Server.csproj", cf =>
        {
            cf.InsertBeforeFirstLine(a => a.Contains("</PropertyGroup>"), "<TSC_Build>true</TSC_Build>");
            cf.InsertBeforeFirstLine(a => a.Contains("</ItemGroup>"), "<Content Remove=\"tsconfig.json\" />");
            cf.InsertBeforeFirstLine(a => a.Contains("</ItemGroup>"), "<None Include=\"tsconfig.json\" />");
            cf.InsertBeforeFirstLine(a => a.Contains("<ProjectReference"), "<PackageReference Include=\"Signum.TSGenerator\" Version=\"9.2.0\" />");
        });

        uctx.ForeachCodeFile("*.csproj", cf =>
        {
            if (cf.FilePath.EndsWith("Server.csproj"))
                return;

            if (cf.Content.Contains("Microsoft.NET.Sdk.Web") && cf.Content.Contains("Signum.TSGenerator"))
            {
                cf.Replace("Microsoft.NET.Sdk.Web", "Microsoft.NET.Sdk");
                cf.InsertAfterFirstLine(a => a.Contains("<ItemGroup>"), "\t<FrameworkReference Include=\"Microsoft.AspNetCore.App\" />");
                cf.RemoveAllLines(a =>
                    a.Contains("TSC_Build") ||
                    a.Contains("EnableRazorComponentCompile") ||
                    a.Contains("RazorCompileOnBuild") ||
                    a.Contains("RazorCompileOnPublish") ||
                    a.Contains("CodeGen") ||
                    a.Contains("wwwroot") ||
                    a.Contains("node_modules")
                );

                cf.RemoveNugetReference("Microsoft.TypeScript.MSBuild");

                cf.UpdateNugetReference("Signum.TSGenerator", "9.2.0");
            }
        });

        uctx.ChangeCodeFile(@".dockerignore", cf =>
        {
            cf.InsertAfterLastLine(a => a.Contains("**"), "**/ts_out");
        });

        uctx.ChangeCodeFile(@".gitignore", cf =>
        {
            cf.InsertAfterLastLine(a => a.Contains($"{uctx.ApplicationName}.Server"), $"/{uctx.ApplicationName}.Server/ts_out/**");
        });

        uctx.ChangeCodeFile(@"Directory.Build.props", cf =>
        {
            cf.RemoveAllLines(a =>
                a.Contains("EnableRazorComponentCompile") ||
                a.Contains("RazorCompileOnBuild") ||
                a.Contains("RazorCompileOnPublish")
            );
        });

        Console.WriteLine();
        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Install the Signum.TSCBuild VSIX extension!");
        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "https://marketplace.visualstudio.com/items?itemName=SignumSoftware.signumtscbuild");
        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "We're sure you will like it... Please add some starts/comments! :)");
    }
}


