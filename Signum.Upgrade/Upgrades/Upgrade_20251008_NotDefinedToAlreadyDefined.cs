using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251008_NotDefinedToAlreadyDefined : CodeUpgradeBase
{
    public override string Description => "Convert sb.NotDefined to sb.AlreadyDefined early return.";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*Logic.cs", file =>
        {
            file.ReplaceBlock(
                line => line.Contains("sb.NotDefined("),
                line => line.Trim() == "}", (start, body, end) =>
                {
                    return $"""
                    if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
                        return;

                    {body.Unindent(4)}
                    """;
                }
            );
        });
    }
}
