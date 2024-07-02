using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20240702_IsolatedDeclarations : CodeUpgradeBase
{
    public override string Description => "";

    public override void Execute(UpgradeContext uctx)
    {
        var regexDefault = new Regex(@"export default function *(?<name>\w+) *\((?<props>[^)]*)\) *{");
        var regexStart = new Regex(@"export function start *\((?<props>[^)]*)\) *{");
        uctx.ForeachCodeFile(@"*.tsx", file =>
        {
            file.Replace(regexDefault, a =>
            {
                return $"export default function {a.Groups["name"].Value}({a.Groups["props"].Value}): React.JSX.Element {{";
            });

            file.Replace(regexStart, a =>
            {
                return $"export function start({a.Groups["props"].Value}): void {{";
            });
        });


        uctx.ForeachCodeFile(@"tsconfig.json", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains(@"""target"":"), @"""isolatedDeclarations"": true,");
        });

        uctx.ForeachCodeFile(@"Changelog.ts", file =>
        {
            file.Replace("satisfies ChangeLogDic", "as ChangeLogDic");
        });
    }
}



