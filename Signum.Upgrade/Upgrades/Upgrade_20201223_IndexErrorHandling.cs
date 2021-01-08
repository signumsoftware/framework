using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20201223_IndexErrorHandling : CodeUpgradeBase
    {
        public override string Description => "add Index.cshtml error handling (for iPhone)";


        public override void Execute(UpgradeContext uctx)
        {


            uctx.ChangeCodeFile(@"Southwind.React\Views\Home\Index.cshtml", file =>
            {
                file.InsertAfterFirstLine(a => a.Contains(@"})(window.navigator.userAgent.toLowerCase());"),
    @"/**
    * @@param {...(a: HTMLElement) => void } complete - Blabla
    */
function newElement(tagName, complete) {
    var result = document.createElement(tagName);
    complete(result);
    return result;
}

window.onerror = function (message, filename, lineno, colno, error) {
    var spinner = document.querySelector(""#reactDiv .loading-spinner"");
    spinner.parentElement.removeChild(spinner);

    var title = document.querySelector(""#reactDiv h3"").replaceWith(
        newElement(""div"", function (div) {
            div.style.fontFamily = ""'Segoe UI', 'Source Sans Pro' , Calibri, Candara, Arial, sans-serif"";
            div.style.fontSize = ""1.3rem"";
            div.appendChild(newElement(""h3"", function (h3) {
                h3.appendChild(newElement(""span"", function (span) {
                    span.style.color = ""red"";
                    span.innerText = error.name;
                }));
                h3.appendChild(newElement(""span"", function (span) {
                    span.style.color = ""darkread"";
                    span.style.marginLeft = ""10px"";
                    span.innerText = error.message;
                }));
            }));
            div.appendChild(newElement(""pre"", function (pre) {
                pre.innerText = error.stack;
            }));
        }));
};");

                file.ReplaceLine(a => a.Contains("const scriptToLoad = ["),
@"(function () {
    var scriptToLoad = [");

                file.ReplaceLine(a => a.Contains("loadNextScript();"),
@"  loadNextScript();
})();");

                SafeConsole.WriteLineColor(ConsoleColor.Red, "Please 'Format Document' '" + file.FilePath + "' in Visual Studio");
            });
        }
    }
}
