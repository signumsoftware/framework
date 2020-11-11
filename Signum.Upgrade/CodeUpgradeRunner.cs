using Signum.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade
{
    public class CodeUpgradeRunner: IEnumerable<CodeUpgradeBase>
    {
        public List<CodeUpgradeBase> Upgrades = new List<CodeUpgradeBase>();

        public IEnumerator<CodeUpgradeBase> GetEnumerator() => Upgrades.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Upgrades.GetEnumerator();

        public void Add(CodeUpgradeBase upgrade)
        {
            Upgrades.Add(upgrade);
        }

        public void Run(UpgradeContext uctx)
        {
            var upgradeFile = Path.Combine(uctx.RootFolder, "SignumUpgrade.txt");
            while (true)
            {
                SetExecuted(uctx, upgradeFile);

                if (!Prompt(uctx, upgradeFile))
                    return;
            }
        }

        void SetExecuted(UpgradeContext uctx, string upgradeFile)
        {
            if (!File.Exists(upgradeFile))
            {
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, $"Creating empty {upgradeFile}...");
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, $"(this file contains the Upgrades that have been run, and should be commited to git)");
                File.Create(upgradeFile);
            }
            else
            {
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, $"Reading {upgradeFile}...");
            }

            var list = File.ReadAllLines(upgradeFile);
          
            foreach (var v in this.Upgrades)
            {
                v.IsExecuted = list.Contains(v.GetType().Name);
            }
        }

        bool Prompt(UpgradeContext uctx, string signumUpgradeFile)
        {
            Draw();

            if (Upgrades.All(a => a.IsExecuted))
            {
                SafeConsole.WriteLineColor(ConsoleColor.Green, "All migrations are executed!");

                return false;
            }
            else
            {
                var first = Upgrades.FirstEx(a => !a.IsExecuted);

                SafeConsole.WriteLineColor(ConsoleColor.White, first.Key);
                SafeConsole.WriteLineColor(ConsoleColor.Gray, first.Description);

                if (!SafeConsole.Ask("Run next Upgrade ({0})?".FormatWith(first.Key)))
                    return false;

                try
                {
                    first.Execute(uctx);

                    return true;

                }
                catch (Exception ex)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Red, ex.Message);
                    SafeConsole.WriteLineColor(ConsoleColor.DarkGray, ex.Message);

                    if (!SafeConsole.Ask("Do you want to skip {0} and mark it as executed?".FormatWith(first.Key)))
                    {
                        File.AppendAllLines(signumUpgradeFile, new[] { first.Key });
                    }
                        
                    return true;
                }
            }

        }

        private void Draw()
        {
            Console.WriteLine();
            var next = this.Upgrades.FirstOrDefault(a => !a.IsExecuted);

            foreach (var upg in this.Upgrades)
            {
                ConsoleColor color = upg.IsExecuted ? ConsoleColor.DarkGreen :
                                     upg == next ? ConsoleColor.Green :
                                     ConsoleColor.White;

                SafeConsole.WriteColor(color,
                    upg.IsExecuted ? "- " :
                    upg == next ? "->" :
                              "  ");

            }

            Console.WriteLine();
        }
    }
}
