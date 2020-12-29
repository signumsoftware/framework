using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20201118_FixSwagger : CodeUpgradeBase
    {
        public override string Description => "Fix Swagger";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile($@"Southwind.React\Startup.cs", file =>
            {
                file.WarningLevel = WarningLevel.Warning;
                file.ReplaceBetweenIncluded(
                    a => a.Contains("var scheme = new OpenApiSecurityScheme"),
                    a => a.Contains("c.AddSecurityRequirement(new OpenApiSecurityRequirement"),
@"string headerName = RestApiKeyLogic.ApiKeyHeader;

c.AddSecurityDefinition(headerName, new OpenApiSecurityScheme
{
    Description = $""Api key needed to access the endpoints. { headerName}: My_API_Key"",
    In = ParameterLocation.Header,
    Name = headerName,
    Type = SecuritySchemeType.ApiKey
});

c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Name = headerName,
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = headerName
            },
            },
            new string[] {}
        }
}); ");
            });
        }
    }
}
