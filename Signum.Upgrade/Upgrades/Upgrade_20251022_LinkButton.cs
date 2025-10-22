using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251022_LinkButton : CodeUpgradeBase
{
    public override string Description => "replace <a href=\"#\"> by <LinkButton>";

    public override void Execute(UpgradeContext uctx)
    {
        var ahrefPattern = new Regex(@"<a\s+href\s*=\s*""#""\s*(.*?)>(.*?)</a>");

        uctx.ForeachCodeFile("*.tsx", file =>
        {
            file.Replace(ahrefPattern, g =>
            {
                var a = g.Value.Replace("<a", "<LinkButton").Replace("</a>", "</LinkButton>");
                a = a.Replace("href=\"#\"", "");
                a = a.Replace("role=\"button\"", "");
                a = a.Replace("e.preventDefault();", "");
                return a;
            });
        });
    }
}
