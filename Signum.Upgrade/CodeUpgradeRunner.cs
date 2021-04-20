using LibGit2Sharp;
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
                SetExecuted(upgradeFile);

                if (!Prompt(uctx, upgradeFile))
                    return;
            }
        }

        void SetExecuted(string upgradeFile)
        {
            Console.WriteLine();
            if (!File.Exists(upgradeFile))
            {
                SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"File {upgradeFile} not found... let's create one!");
                Console.WriteLine();
                var result = Upgrades.Select(a=>a.Key).And("<< Mark ALL upgrades as executed >>").ChooseConsole(a => a.ToString(), "What do you think is the next upgrade that you should run? (the previous ones will be marked as executed)");

                File.WriteAllLines(upgradeFile, result == null ? Array.Empty<string>() : Upgrades.TakeWhile(a => a.Key != result).Select(a => a.Key).ToArray());
                Console.WriteLine();
                SafeConsole.WriteLineColor(ConsoleColor.Green, $"File {upgradeFile} created!");
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, $"(this file contains the Upgrades that have been run, and should be commited to git)");
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
                SafeConsole.WriteLineColor(ConsoleColor.Green, "All Upgrades are executed!");

                return false;
            }
            else
            {
                var first = Upgrades.FirstEx(a => !a.IsExecuted);

                if (!SafeConsole.Ask("Run next Upgrade ({0})?".FormatWith(first.Key)))
                    return false;


                return ExecuteUpgrade(first, uctx, signumUpgradeFile);
            }

        }

        static bool IsDirtyExceptSubmodules(string folder)
        {
            using (Repository rep = new Repository(folder))
            {
                var subModules = rep.Submodules.Select(a => a.Name);
                var status = rep.RetrieveStatus();
                return status.Any(a => a.State != FileStatus.Ignored && !subModules.Contains(a.FilePath));
            }
        }

        static bool ExecuteUpgrade(CodeUpgradeBase upgrade, UpgradeContext uctx, string signumUpgradeFile)
        {
            while (IsDirtyExceptSubmodules(uctx.RootFolder))
            {
                Console.WriteLine();
                Console.WriteLine("There are changes in the git repo. Commit or reset the changes and press [Enter]");
                Console.ReadLine();
            }

            Console.WriteLine();

            try
            {
                uctx.HasWarnings = WarningLevel.None;
                upgrade.Execute(uctx);
            }
            catch (Exception ex)
            {
                SafeConsole.WriteLineColor(ConsoleColor.Red, ex.Message);
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, ex.Message);

                if (!SafeConsole.Ask("Do you want to skip {0} and mark it as executed?".FormatWith(upgrade.Key)))
                    return false;
            }

            File.AppendAllLines(signumUpgradeFile, new[] { upgrade.Key });

            Console.WriteLine();

            switch (uctx.HasWarnings)
            {
                case WarningLevel.None: SafeConsole.WriteColor(ConsoleColor.Green, "Upgrade finished sucessfully!"); break;
                case WarningLevel.Warning: SafeConsole.WriteColor(ConsoleColor.Yellow, "Upgrade finished with Warnings..."); break;
                case WarningLevel.Error:SafeConsole.WriteColor(ConsoleColor.Red, "Upgrade finished with Errors..."); break;
                default: throw new UnexpectedValueException(uctx.HasWarnings);
            }

            Console.WriteLine(" Please review the changes...");

            Console.WriteLine();

            switch (SafeConsole.Ask("What should we do next?", "commit", "retry", "exit"))
            {
                case "commit":
                    using (Repository rep = new Repository(uctx.RootFolder))
                    {
                        if (rep.RetrieveStatus().IsDirty)
                        {
                            Commands.Stage(rep, "*");
                            var sign = rep.Config.BuildSignature(DateTimeOffset.Now);
                            rep.Commit(upgrade.Key, sign, sign);
                            SafeConsole.WriteLineColor(ConsoleColor.White, "A commit with text message '{0}' has been created".FormatWith(upgrade.Key));
                        }
                        else
                        {
                            Console.WriteLine("Nothing to commit");
                        }
                    }
                    return true;
                case "retry":
                    while (IsDirtyExceptSubmodules(uctx.RootFolder))
                    {
                        Console.WriteLine("Revert all the changes in git and press [Enter]");
                        Console.ReadLine();
                    }
                    return true;
                case "exit":
                case null:
                    return false;
            }
        
            Console.ReadLine();

            return true;
        }

        private void Draw()
        {
            Console.WriteLine();
            var next = this.Upgrades.FirstOrDefault(a => !a.IsExecuted);

            SafeConsole.WriteLineColor(ConsoleColor.Cyan, "Available Upgrades:");
            Console.WriteLine();

            foreach (var upg in this.Upgrades)
            {
                ConsoleColor color = upg.IsExecuted ? ConsoleColor.DarkGreen :
                                     upg == next ? ConsoleColor.Blue :
                                     ConsoleColor.White;

                SafeConsole.WriteColor(color,
                    upg.IsExecuted ? "- " :
                    upg == next ? "->" :
                              "  ");

                SafeConsole.WriteColor(color, " " + upg.Key);
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, " " + upg.Description);

            }

            Console.WriteLine();
        }
    }
}
