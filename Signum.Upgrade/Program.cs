using Signum.Upgrade.Upgrades;
using System;
using System.Runtime.Intrinsics.Arm;

namespace Signum.Upgrade
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Signum Upgrade");

            var uctx = UpgradeContext.CreateFromCurrentDirectory();
            Console.WriteLine(uctx.ToString());


            new CodeUpgradeRunner
            {
                new Upgrade_20201110_DotNet5(),




            }.Run(uctx);
        }
    }
}
