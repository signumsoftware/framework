using Signum.Utilities;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250210_UpdateActiveDirectory : CodeUpgradeBase
{
    public override string Description => "";

    public override void Execute(UpgradeContext uctx)
    {
        void ProcessAzureAndWindows(CodeFile cf)
        {
            cf.ProcessLines(lines =>
            {
                bool MoveIntoBlock(string property, string embedded, List<string> properties)
                {
                    var pos = lines.FindIndex(a => properties.Any(p => a.Contains(p)));
                    if (pos == -1)
                        return false;

                    var indent = CodeFile.GetIndent(lines[pos]);
                    var extract = lines.Extract(line => properties.Any(p => line.Contains(p)));

                    var main = extract.SingleOrDefault(a => a.Contains(properties.FirstEx()));

                    var text = main == null || main.Contains(" null,") ?
                        $"{property} = null," :
                        $$"""
                        {{property}} = new {{embedded}}
                        {
                        {{extract.ToString(a => "    "+ a.Trim() ,"\r\n")}}
                        },
                        """;

                    lines.InsertRange(pos + 1, text.Indent(indent.Length));

                    return true;
                }


                var result = false; 
                
                result |= MoveIntoBlock("WindowsAD", "WindowsADEmbedded", ["Azure_ApplicationID", "Azure_DirectoryID", "Azure_ClientSecret", "LoginWithAzureAD", "UseDelegatedPermission"]);
                result |= MoveIntoBlock("AzureAD", "AzureADEmbedded", ["DomainName", "LoginWithActiveDirectoryRegistry", "LoginWithWindowsAuthenticator", "DirectoryRegistry_Username", "DirectoryRegistry_Password"]);

                return result;
            });
        }


        uctx.ChangeCodeFile("Southwind.Terminal/SouthwindMigrations.cs", ProcessAzureAndWindows);
        uctx.ChangeCodeFile("Southwind.Test.Environment/SouthwindEnvironment.cs", ProcessAzureAndWindows);

        uctx.ChangeCodeFile("Southwind/Index.cshtml", cf =>
        {
            cf.Replace("var __azureApplicationId", @"var __azureADConfig");
            cf.Replace(".Azure_ApplicationID", ".AzureAD?.ToAzureADConfigTS()");
            cf.RemoveAllLines(a =>
                a.Contains("var __azureTenantId") ||
                a.Contains("var __azureB2CSignInSignUp_UserFlow") ||
                a.Contains("var __azureB2CTenantName")
            );
        });

        uctx.ChangeCodeFile("Southwind/MainPublic.tsx", cf =>
        {
            cf.Replace("if (window.__azureApplicationId)", "if (window.__azureADConfig)");
        });
    }

 
}



