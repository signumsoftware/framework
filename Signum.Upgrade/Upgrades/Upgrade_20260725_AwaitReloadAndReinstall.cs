using Signum.Utilities;
using System;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260725_AwaitReloadAndReinstall : CodeUpgradeBase
{
    public override string Description => "Await reload on logout, remove UserAzureADMixin registration, switch VS Code task to 'yarn tsc' and force a yarn re-install";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind/MainPublic.tsx", file =>
        {
            file.Replace("async function reload() {", "async function reload() : Promise<boolean> {");
            file.Replace("AuthClient.Options.onLogout = () => {", "AuthClient.Options.onLogout = async () => {");
            file.Replace("  reload();", "  await reload();"); // only the indented call inside onLogout, not the top-level reload();
        });

        uctx.ChangeCodeFile(@"Southwind/Starter.cs", file =>
        {
            file.InsertBeforeFirstLine(a => a.Contains("using Signum.UserQueries;"),
                "using Signum.UserAssets.TokenMigrations;");

            file.RemoveAllLines(a => a.Contains("MixinDeclarations.Register<UserEntity, UserAzureADMixin>()"));
        });

        uctx.ChangeCodeFile(@".vscode/tasks.json", file =>
        {
            file.Replace("yarn tsgo", "yarn tsc"); // updates both the task label and the command
        });

        // Force a clean yarn install so the lockfile is regenerated
        uctx.DeleteFile("yarn.lock");

        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Remember to:");
        SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, "Run 'yarn install' after this upgrade (yarn.lock was deleted to force a re-install)");
    }
}
