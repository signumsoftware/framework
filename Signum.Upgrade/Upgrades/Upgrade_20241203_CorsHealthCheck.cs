using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20241203_CorsHealthCheck : CodeUpgradeBase
{
    public override string Description => "Enable CORS policy HealthCheck";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.Server\\Program.cs", file =>
        {
            if (SafeConsole.Ask(@"Enable CORS policy ""HealtchCheck""?"))
            {
                file.InsertAfterFirstLine(a => a.Contains("AddSignumValidation"), """

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("HealthCheck", builder =>
                    {
                        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyHeader();
                    });
                });
                """);

                file.InsertAfterFirstLine(a => a.Contains("app.UseRouting();"), """
                app.UseCors("HealthCheck");
                """);
            }
        });
    }
}



