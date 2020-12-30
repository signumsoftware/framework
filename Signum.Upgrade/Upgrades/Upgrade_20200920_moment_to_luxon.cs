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
    class Upgrade_20200920_moment_to_luxon : CodeUpgradeBase
    {
        public override string Description => "Replace Moment.js by Luxon.js";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile($@"Southwind.React/App/MainPublic.tsx", file =>
            {
                file.Replace(
                    "import * as moment from \"moment\"",
                    "import * as luxon from \"luxon\"");

                file.RemoveAllLines(
                    l => l.Contains("require('moment/locale")
                    );

                file.RemoveAllLines(
                    l => l.Contains("ConfigureReactWidgets.asumeGlobalUtcMode(moment, false);")
                 );

                file.ReplaceBetweenExcluded(
                    l => l.Contains("const culture = ci.name"),
                    l => l.Contains("});"),
@"  luxon.Settings.defaultLocale = culture;
  numbro.setLanguage(culture);"
                    );

            });

            uctx.ForeachCodeFile($@"*.tsx, *.ts", uctx.ReactDirectory, file =>
            {
                file.Replace("import * as moment from 'moment'", "import { DateTime } from 'luxon'");
                file.Replace("format()", "toISO()");
                file.Replace("moment()", "DateTime.local()");
                file.Replace("moment(", "DateTime.fromISO(");
                file.Replace(".fromNow(true)", ".toRelative()");
            });

            uctx.ChangeCodeFile($@"Southwind.React/App/vendors.js", file =>
            {
                file.Replace(
                    "require(\"moment\");",
                    "require(\"luxon\");");
            });

            uctx.ChangeCodeFile($@"Southwind.React/package.json", file =>
            {
                file.ReplaceLine(a => a.Contains("\"moment\""),
@"""@types/luxon"": ""1.24.4"",
""luxon"": ""1.25.0"","
                    );
            });

            uctx.ChangeCodeFile($@"Southwind.React/webpack.config.js", file =>
            {
                file.RemoveAllLines(a => a.Contains("webpack.ContextReplacementPlugin"));
            });

        }
    }
}
