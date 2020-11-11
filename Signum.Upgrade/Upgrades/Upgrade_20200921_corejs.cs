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
    class Upgrade_20200921_corejs : CodeUpgradeBase
    {
        public override string Description => "Unify polyfills in core-js";

        public override string SouthwindCommitHash => "905e77fbd6c22a29206ff57ab4d3f034ed4b317b";

        protected override void ExecuteInternal(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile($@"Southwind.React/App/polyfills.js", file =>
            {
                file.RemoveAllLines(
                    l => l.Contains("url-search-params-polyfill")
                    );

                file.ReplaceLine(
                  l => l.Contains("es6-promise/auto"),
                  "require('core-js/stable');"
                );
            });

            uctx.ChangeCodeFile($@"Southwind.React/App/polyfills.js", file =>
            {
                file.RemoveAllLines(
                    l => l.Contains("url-search-params-polyfill") ||
                    l.Contains("es6-object-assign") ||
                    l.Contains("es6-promise")
                    );

                file.InsertAfterFirstLine(
                    l => l.Contains("abortcontroller-polyfill"),
@"""core-js"": ""3.6.5"","
                    );
            });
        }
    }
}
