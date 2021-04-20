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
    class Upgrade_20210325_FixDllPlugin : CodeUpgradeBase
    {
        public override string Description => "Set entryOnly: false in webpack.DllPlugin";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile(@"Southwind.React\webpack.config.dll.js", file =>
            {
                file.ReplaceLine(
                    a => a.Contains(@"context: path.resolve(__dirname, ""App"")"),
@"context: path.resolve(__dirname, ""App""),
entryOnly: false,"
                );

                file.InsertAfterFirstLine(
                    a => a.Contains(@"require('assets-webpack-plugin');"),
                    "//const BundleAnalyzerPlugin = require('webpack-bundle-analyzer').BundleAnalyzerPlugin;"
                );

                file.InsertBeforeFirstLine(
                    a => a.Contains(@"new webpack.DllPlugin({"),
                    "//new BundleAnalyzerPlugin(),"
                );
            });

            uctx.ChangeCodeFile(@"Southwind.React\webpack.config.js", file =>
            {
                file.InsertAfterFirstLine(
                    a => a.Contains(@"require('webpack-notifier')"),
                    "//const BundleAnalyzerPlugin = require('webpack-bundle-analyzer').BundleAnalyzerPlugin;"
                );

                file.InsertBeforeFirstLine(
                    a => a.Contains(@"new webpack.DllReferencePlugin({"),
                    "//new BundleAnalyzerPlugin(),"
                );
            });

            uctx.ChangeCodeFile(@"Southwind.React\package.json", file =>
            {
                file.InsertAfterFirstLine(
                    a => a.Contains(@"""webpack"""),
                    @"""webpack-bundle-analyzer"": ""4.4.0"","
                );
            });


        }
    }
}
