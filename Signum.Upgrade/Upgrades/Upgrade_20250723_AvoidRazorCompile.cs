using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250723_AvoidRazorCompile : CodeUpgradeBase
{
    public override string Description => "RazorCompileOnBuild false";

    public override void Execute(UpgradeContext uctx)
    {

        uctx.ChangeCodeFile(@"Directory.Build.props", file =>
        {
            file.ReplaceLine(a => a.Contains("PreserveCompilationContext"), """
                <EnableRazorComponentCompile>false</EnableRazorComponentCompile>
                <RazorCompileOnBuild>false</RazorCompileOnBuild>
                <RazorCompileOnPublish>false</RazorCompileOnPublish>
                """);
        });

        uctx.ChangeCodeFile(@"Southwind/Southwind.csproj", file =>
        {
            file.InsertAfterLastLine(a => a.Contains("NoWarn"), """
                <EnableRazorComponentCompile>true</EnableRazorComponentCompile>
                <RazorCompileOnBuild>true</RazorCompileOnBuild>
                <RazorCompileOnPublish>true</RazorCompileOnPublish>
                """);
        });
    }
}


