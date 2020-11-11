using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20201110_DotNet5 : CodeUpgradeBase
    {
        public override string Description => "Upgrade to .Net 5";

        public override string SouthwindCommitHash => "db5736eff63bd240d78f27a6db71ab693c5903f8 5a03b1f37f7aba99013a6d3f9292bd6d631c306b";

        protected override void ExecuteInternal(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile("*.cs", uctx.EntitiesDirectory, file =>
            {
                file.Replace(
                    searchFor: @"protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)",
                    replaceBy: @"protected override void ChildCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)");
            });

            uctx.ForeachCodeFile("*.csproj", file =>
            {
                file.Replace(
                    searchFor: @"<TargetFramework>netcoreapp3.1</TargetFramework>",
                    replaceBy: @"<TargetFramework>net5</TargetFramework>");

                file.UpdateNugetReference(
                    packageName: @"Signum.MSBuildTask",
                    version: @"5.0.0-preview2");
            });


            uctx.ChangeCodeFile($@"Southwind.React\Startup.cs", file =>
            {
                file.Replace("AddNewtonsoftJson", "AddJsonOptions");
                file.Replace(
                    "public bool Match(HttpContext httpContext, IRouter route, ", 
                    "public bool Match(HttpContext? httpContext, IRouter? route, ");
            });

        }
    }
}
