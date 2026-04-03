namespace Signum.Upgrade.Upgrades;

class Upgrade_20260326_ClaudeGitignore : CodeUpgradeBase
{
    public override string Description => "Add .claude worktrees and settings.local.json to root .gitignore";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(".gitignore", file =>
        {
            file.InsertAfterLastLine(a => a.Trim().Length > 0,
                """

                .claude/worktrees/
                .claude/settings.local.json
                """);
        });
    }
}
