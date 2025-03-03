using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250303_AutoReplacementMList : CodeUpgradeBase
{
    public override string Description => "Add AutoReplacement for the new MList table name _ (and Postgres idiomatic)";

    public override void Execute(UpgradeContext uctx)
    {
       
        uctx.ChangeCodeFile("Southwind.Terminal/Program.cs",  pg =>
        {
            pg.InsertAfterFirstLine(a => a.Contains("Main(string[]"), """
                Replacements.AutoReplacement = c => {

                    var newValue = c.NewValues?.Where(a => a.ToLower().Replace("_", "") == c.OldValue.ToLower().Replace("_", "")).Only();

                    if (newValue != null)
                        return new Replacements.Selection(c.OldValue, newValue);

                    return null;
                };
                """);
        });
    }

 
}



