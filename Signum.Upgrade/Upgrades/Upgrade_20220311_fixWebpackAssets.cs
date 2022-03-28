using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20220311_fixWebpackAssets : CodeUpgradeBase
{
    public override string Description => "Replaces fetchLitesWithFilters to fetchLites";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.React/webpack.config.js", file =>
        {
            file.Replace(@"use: [{ loader: ""url-loader"", options: { ""mimetype"": ""image/gif"" } }]", @"type: ""asset""");
            file.Replace(@"use: [{ loader: ""url-loader"", options: { ""mimetype"": ""image/png"" } }]", @"type: ""asset""");
            file.Replace(@"use: [{ loader: ""url-loader"", options: { ""mimetype"": ""application/font-woff"" } }]", @"type: ""asset""");
            file.Replace(@"use: [{ loader: ""file-loader"", options: { ""name"": ""[name].[ext]"" } }]", @"type: ""asset""");
        });
    }
}
