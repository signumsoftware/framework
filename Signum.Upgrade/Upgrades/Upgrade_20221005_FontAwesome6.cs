using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20221005_FontAwesome6 : CodeUpgradeBase
{
    public override string Description => "upgrade FontAwesome 6";

    public static Regex IconDefRegex = new Regex(@"icon *[:=] *\{?(?<iconDef>[^{};\n]+)\}?");
    public static Regex IconNameRegex = new Regex(@"""(?<iconName>[^""]+)""");

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            file.Replace(IconDefRegex, m =>
            {
                return m.ToString().Replace(m.Groups["iconDef"].Value, Regex.Replace(m.Groups["iconDef"].Value, @"""(?<iconName>[^""]+)""", s => {
                    var oldName = s.Groups["iconName"].Value;

                    var newName = FontAwesomeV6Upgrade.iconNamesDic.TryGetC(oldName);

                    if (newName == null)
                        return s.ToString();
                    return s.ToString().Replace(oldName, newName);
                }));
            });
        });

        uctx.ForeachCodeFile("package.json", file =>
        {
            file.UpdateNpmPackage("@fortawesome/fontawesome-svg-core", "6.2.0");
            file.UpdateNpmPackage("@fortawesome/free-brands-svg-icons", "6.2.0");
            file.UpdateNpmPackage("@fortawesome/free-regular-svg-icons", "6.2.0");
            file.UpdateNpmPackage("@fortawesome/free-solid-svg-icons", "6.2.0");
            file.UpdateNpmPackage("@fortawesome/react-fontawesome", "0.2.0");
        });

        uctx.ChangeCodeFile(@"Southwind.Terminal/SouthwindMigrations.cs ", file =>
        {
            file.InsertBeforeFirstLine(a => a.Contains("namespace"),
@"using Signum.Engine.Toolbar;
using Signum.Engine.Dashboard;

");

            file.InsertBeforeFirstLine(a => a.Contains("Run(autoRun);"),
@"
    ToolbarLogic.UpdateToolbarIconNameInDB,
    DashboardLogic.UpdateDashboardIconNameInDB,
");
        });
    }
}



