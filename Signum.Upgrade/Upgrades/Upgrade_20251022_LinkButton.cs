using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251022_LinkButton : CodeUpgradeBase
{
    public override string Description => "replace <a href=\"#\"> by <LinkButton>";

    public override void Execute(UpgradeContext uctx)
    {
        var ahrefPattern = new Regex(
            @"<a\s+([^>]*?)href\s*=\s*""#""([^>]*)>(.*?)</a>",
            RegexOptions.Singleline
        );

        uctx.ForeachCodeFile("*.tsx", file =>
        {
            file.Replace(ahrefPattern, g =>
            {
                var a = g.Value.Replace("<a", "<LinkButton").Replace("</a>", "</LinkButton>");
                a = Regex.Replace(a, @"\s+href=""#""", "");
                a = Regex.Replace(a, @"\s+role=""button""", "");
                a = Regex.Replace(a, @"\s+e.preventDefault\(\);?", "");
                return a;
            });
        });
    }
}
