using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251219_FixTaskJson : CodeUpgradeBase
{
    public override string Description => "Fix task.json";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@".vscode\tasks.json", file =>
        {
            file.ReplaceBetweenIncluded(
                a => a.Contains("yarn tsgo"),
                a => a.Contains("problemMatcher"),
                $$"""
                "label": "yarn tsgo",
                "type": "shell",
                "options": {
                    "cwd": "${workspaceFolder}"
                },
                "windows": {
                    "command": "yarn tsgo -b {{uctx.ApplicationName}}.Server/tsconfig.json"
                },
                "group": "build",
                "problemMatcher": "$tsc"
                """);

        });
    }
}
