using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20200920_remove_numbro : CodeUpgradeBase
    {
        public override string Description => "Remove Numbro.js and use Intl directly";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile($@"Southwind.React/App/MainPublic.tsx", file =>
            {
                file.RemoveAllLines(
                    l => l.Contains("import numbro from \"numbro\"")
                    );

                file.Replace("reloadTypes }", "NumberFormatSettings, reloadTypes }");

                file.RemoveAllLines(
                    l => l.Contains("numbro.registerLanguage")
                );

                file.ReplaceLine(
                  l => l.Contains("numbro.setLanguage"),
                  "NumberFormatSettings.defaultNumberFormatLocale = culture;"
                );
            });

            uctx.ForeachCodeFile($@"*.tsx, *.ts", uctx.ReactDirectory, file =>
            {
                file.Replace(@"import numbro from ""numbro""", "import { toNumberFormat } from '@framework/Reflection'");
                file.Replace(@"import numbro from 'numbro'", "import { toNumberFormat } from '@framework/Reflection'");
                file.Replace(new Regex(@"numbro\((?<val>::EXPR::)\)\.format\((?<frmt>::EXPR::)\)").WithMacros(),
                    m => $@"toNumberFormat({m.Groups["frmt"].Value})/*move-to-local-var*/.format({m.Groups["val"].Value})");
            });

            uctx.ChangeCodeFile($@"Southwind.React/App/vendors.js", file =>
            {
                file.RemoveAllLines(a => a.Contains("require(\"numbro\");"));
            });

            uctx.ChangeCodeFile($@"Southwind.React\package.json", file =>
            {
                file.RemoveAllLines(a => a.Contains("\"numbro\""));
            });
        }

        
    }

    public static class RegexExpressions
    {
        public static Regex WithMacros(this Regex regex)
        {
            var pattern = regex.ToString();

            var newPattern = pattern.Replace("::EXPR::", new Regex(@"(?:[^()]|(?<Open>[(])|(?<-Open>[)]))+").ToString()); /*Just for validation and coloring*/

            return new Regex(newPattern, regex.Options);
        }
    }
}
