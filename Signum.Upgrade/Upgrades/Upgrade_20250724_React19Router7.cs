using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250724_React19Router7 : CodeUpgradeBase
{
    public override string Description => "React 19.1 and React Router 7.7";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind\tsconfig.json", file =>
        {
            file.ReplaceLine(a => a.Contains("target"), """
                "target": "esnext", 
                """);

            file.ReplaceLine(a => a.Contains("sourceMap"), """
                "sourceMap": "true", 
                """);

            file.ReplaceLine(a => a.Contains("moduleResolution"), """
                "moduleResolution": "bundler", 
                """);

            file.ReplaceLine(a => a.Contains("jsx"), """
                "jsx": "react-jsx",
                """);

            file.ReplaceBetween(
                new ReplaceBetweenOption(a => a.Contains(@"""lib"": ["), 1),
                new ReplaceBetweenOption(a => a.Contains(@"]"), -1), """
                "ESNext",
                "dom"
                """
                );
        });

        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Remember to:");
        SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, "Yarn install");
        SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, "Delete bin, obj and ts_out inf Framework and your projects");
        SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, "Compile again");
    }
}


