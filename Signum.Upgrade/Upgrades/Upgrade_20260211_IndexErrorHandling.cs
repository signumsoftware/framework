using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260211_IndexErrorHandling : CodeUpgradeBase
{
    public override string Description => "Refactor Index.cshtml";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.Server/Index.cshtml", file =>
        {
            file.ReplaceBetweenIncluded(
                a => a.Contains("function showError(error) {"),
                a => a.Contains("var title = document.querySelector(\"#reactDiv h3\").replaceWith("),
                """
                function showError(error) {
                    var title = document.querySelector("#reactDiv .center-container").replaceWith(
                """);
        });
    }
}
