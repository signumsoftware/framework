using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Signum.Upgrade.Upgrades;

class Upgrade_202304263_ProjectRevolution_config : CodeUpgradeBase
{
    public override string Description => "Project REVOLUTION - update config files";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"webpack.config.js", file =>
        {
            var regex = new Regex(@"var[ ]*node_modules[ ]*=");

            file.ReplaceLine(
                l => regex.Match(l).Success,
                @"var node_modules = path.join(__dirname, "".."", ""node_modules"");"
            );

            regex = new Regex(@"main[ ]*:[ ]*\[");

            file.ReplaceLine(
                l => regex.Match(l).Success,
                @"main: [""./MainPublic.tsx""],"
            );

            file.ReplaceLine(
                l => l.Contains(@"../Framework/Signum.React/Scripts"),
                @"'@framework': path.resolve(__dirname, '../Framework/Signum/React'),"
            );

            file.ReplaceLine(
                l => l.Contains(@"../Framework/Signum.React.Extensions"),
                @"'@extensions': path.resolve(__dirname, '../Framework/Extensions')"
            );
        });

        uctx.ForeachCodeFile(@"webpack.config.dll.js", file =>
        {
            var regex = new Regex(@"vendor[ ]*:[ ]*\[");

            file.ReplaceLine(
                l => regex.Match(l).Success,
                @"vendor: [path.join(__dirname, ""vendors.js"")]"
            );
        });


        var project = File.ReadAllText(uctx.AbsolutePath($"{uctx.ApplicationName}\\{uctx.ApplicationName}.csproj"));

        var extRegex = new Regex(@"Framework\\Extensions\\(?<ext>[\w\.]*)");

        var modules = project.Lines().Where(l => l.Contains("<ProjectReference Include=\"..\\Framework\\Extensions")).Select(l => extRegex.Match(l).Groups["ext"].Value).ToList();

        uctx.ForeachCodeFile(@"tsconfig.json", file =>
        {
            file.Replace(new Regex(@"[ ]*\""paths\""[ :]*\{([^}]+)\}"),
                m => """
                    "paths": {
                      "@framework/*": [ "../Framework/Signum/React/*" ],
                      "@extensions/*": [ "../Framework/Extensions/*" ]
                    }
                """);

            file.Replace(new Regex(@"[ ]*\""exclude\""[ :]*\[([^}]+)\],"), m => "");

            var regex = new Regex(@"[ ]*\""path\""[ :]*""..\/Framework\/Signum.React");
            file.RemoveAllLines(l => regex.Match(l).Success);

            regex = new Regex(@"[ ]*\""references\""[ :]*\[");

            string newRefs = @"  { ""path"": ""../Framework/Signum"" }," + "\n" +
                modules.Select(m => $"  {{ \"path\": \"../Framework/Extensions/{m}\" }}").ToString(",\n");
            file.InsertAfterFirstLine(l => regex.Match(l).Success, newRefs);
        });

        uctx.ForeachCodeFile(@"package.json", file =>
        {
            file.RemoveAllLines(l => l.Contains("preinstall"));
        });
    }        
    
}
