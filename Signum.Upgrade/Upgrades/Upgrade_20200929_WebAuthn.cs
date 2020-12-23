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
    class Upgrade_20200929_WebAuthn : CodeUpgradeBase
    {
        public override string Description => "Add Support to WebAuthn";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile($@"Southwind.Entities\ApplicationConfiguration.cs", file =>
            {
                file.InsertAfterFirstLine(
                    l => l.Contains("public AuthTokenConfigurationEmbedded AuthTokens { get; set; }"),
                    "public WebAuthnConfigurationEmbedded WebAuthn { get; set; }");

            });


            uctx.ChangeCodeFile($@"Southwind.Logic\Starter.cs", file =>
            {
                file.InsertAfterLastLine(
                    l => 
                    l.Contains("SessionLogLogic.Start(sb);") || 
                    l.Contains("UserTicketLogic.Start(sb);") || 
                    l.Contains("AuthLogic.StartAllModules(sb);"),
                    "WebAuthnLogic.Start(sb, ()=> Configuration.Value.WebAuthn);");
            });

            uctx.ChangeCodeFile($@"Southwind.React\App\Layout.tsx", file =>
            {
                file.InsertAfterLastLine(
                    l => l.Contains("import *"),
                    "import * as WebAuthnClient from '@extensions/Authorization/WebAuthn/WebAuthnClient'");

                file.Replace(
                    "<LoginDropdown />",
                    "<LoginDropdown extraButons={user => <WebAuthnClient.WebAuthnRegisterMenuItem />} />");
            });

            uctx.ChangeCodeFile($@"Southwind.React\App\MainPublic.tsx", file =>
            {
                file.InsertAfterFirstLine(
                    l => l.Contains("import * as AuthClient"),
                    "import * as WebAuthnClient from '@extensions/Authorization/WebAuthn/WebAuthnClient'");

                file.InsertAfterFirstLine(
                    l => l.Contains("import NotFound from './NotFound'"),
                    "import LoginPage from '@extensions/Authorization/Login/LoginPage'");

                file.InsertBeforeFirstLine(
              l => l.Contains("Services.SessionSharing.setAppNameAndRequestSessionStorage"),
              @"LoginPage.customLoginButtons = ctx => <WebAuthnClient.WebAuthnLoginButton ctx={ctx} />;
");
            });

            uctx.ChangeCodeFile($@"Southwind.React/App/Southwind/Templates/ApplicationConfiguration.tsx", file =>
            {
                file.InsertAfterLastLine(
                    l => l.Contains("</Tab>"),
@"<Tab eventKey=""webauthn"" title={ctx.niceName(a => a.webAuthn)}>
  <RenderEntity ctx={ctx.subCtx(a => a.webAuthn)} />
 </Tab>");   
            });

            uctx.ChangeCodeFile($@"Southwind.Terminal\SouthwindMigrations.cs", file =>
            {
                file.InsertBeforeFirstLine(
                    l => l.Contains("}, //Auth"),
@"},
WebAuthn = new WebAuthnConfigurationEmbedded
{
    ServerName = ""Southwind""");
            });

            uctx.ChangeCodeFile($@"Southwind.Test.Environment/SouthwindEnvironment.cs", file =>
            {
                file.InsertBeforeFirstLine(
                    l => l.Contains("}, //Auth"),
@"},
WebAuthn = new WebAuthnConfigurationEmbedded
{
    ServerName = ""Southwind""");
            });
        }
    }
}
