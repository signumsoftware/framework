using Signum.Upgrade.Upgrades;
using Signum.Utilities;
using System;
using System.Runtime.Intrinsics.Arm;

namespace Signum.Upgrade
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("  ..:: Welcome to Signum Upgrade ::..");
            Console.WriteLine();

            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  This application helps you upgrade a Signum Framework application by modifying your source code.");
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  The closer your application resembles Southwind, the better it works.");
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  Review all the changes carefully");
            Console.WriteLine();

            var uctx = UpgradeContext.CreateFromCurrentDirectory();
            Console.Write("  RootFolder = "); SafeConsole.WriteLineColor(ConsoleColor.DarkGray, uctx.RootFolder);
            Console.Write("  ApplicationName = "); SafeConsole.WriteLineColor(ConsoleColor.DarkGray, uctx.ApplicationName);


            new CodeUpgradeRunner
            {
                new Upgrade_20200920_moment_to_luxon(),
                new Upgrade_20200920_remove_numbro(),
                new Upgrade_20200921_corejs(),
                new Upgrade_20200929_WebAuthn(),
                new Upgrade_20201110_DotNet5(),
                new Upgrade_20201118_FixSwagger(),
                new Upgrade_20201119_SplitPolyfills(),
                new Upgrade_20201123_Typescript41(),
                new Upgrade_20201124_CombinedUserChartPart(),
                new Upgrade_20201124_FixImportUserAssets(),
                new Upgrade_20201125_ReactBootstrap14(),
                new Upgrade_20201126_AddWebAppRestart(),
                new Upgrade_20201210_NavigatorView(),
                new Upgrade_20201220_React17(),
                new Upgrade_20201223_IndexErrorHandling(),
                new Upgrade_20201230_TensorFlow(),
                new Upgrade_20201231_AnyCPU(),
                new Upgrade_20210108_RemoveLangVersion(),
                new Upgrade_20210113_TimezoneInDockerfile(),
                new Upgrade_20210119_DeepL(),
                new Upgrade_20210205_ErrorHandling(),
                new Upgrade_20210210_UpgradeNugets(),
                new Upgrade_20210216_RegisterTranslatableRoutes(),
                new Upgrade_20210302_TypeScript42(),
                new Upgrade_20210325_FixDllPlugin(),
                new Upgrade_20210331_ReactWidgets5(),
                new Upgrade_20210414_SimplifyQueryTokenString(),
                new Upgrade_20210415_ReactWidgets503(),
                new Upgrade_20210511_MSAL2(),
                new Upgrade_20210513_UpdateNugets(),    
            }.Run(uctx);
        }
    }
}
