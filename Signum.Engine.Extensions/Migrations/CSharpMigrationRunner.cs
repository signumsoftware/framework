using Signum.Entities;
using Signum.Entities.Migrations;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Signum.Engine.Migrations
{
    public class CSharpMigrationRunner: IEnumerable<CSharpMigrationRunner.MigrationInfo>
    {
        public List<MigrationInfo> Migrations = new List<MigrationInfo>();

        public void Add(Action action, string? uniqueName = null)
        {
            Migrations.Add(new MigrationInfo(action, uniqueName ?? GetUniqueName(action)));
        }

        private string GetUniqueName(Action action)
        {
            return action.Method.Name;
        }

        public void Run(bool autoRun)
        {
            while (true)
            {
                SetExecuted();

                if (!Prompt(autoRun) || autoRun)
                    return;
            }
        }

        void SetExecuted()
        {
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "Reading C# migrations...");

            MigrationLogic.EnsureMigrationTable<CSharpMigrationEntity>();

            var database = Database.Query<CSharpMigrationEntity>().Select(m => m.UniqueName).OrderBy().ToHashSet();

            foreach (var v in this.Migrations)
            {
                v.IsExecuted = database.Contains(v.UniqueName);
            }
        }

        bool Prompt(bool autoRun)
        {
            Draw(null);


            if (Migrations.All(a=>a.IsExecuted))
            {
                SafeConsole.WriteLineColor(ConsoleColor.Green, "All migrations are executed!");

                return false;
            }
            else
            {
                if (!autoRun && !SafeConsole.Ask("Run migrations ({0})?".FormatWith(Migrations.Count(a => !a.IsExecuted))))
                    return false;

                try
                {
                    foreach (var item in this.Migrations.AsEnumerable().Where(a => !a.IsExecuted))
                    {
                        Draw(item);

                        Execute(item);
                    }

                    return true;

                }
                catch (MigrationException)
                {
                    if (autoRun)
                        throw;

                    return true;
                }
            }

        }

        private void Execute(MigrationInfo mi)
        {
            try
            {
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, mi.UniqueName + " executing ...");
                mi.Action();
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, mi.UniqueName + " finished!");
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine();
                SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.GetType().Name + (e is SqlException ? " (Number {0}): ".FormatWith(((SqlException)e).Number) : ": "));
                SafeConsole.WriteLineColor(ConsoleColor.Red, e.Message);
                SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.StackTrace);

                Console.WriteLine();

                throw new MigrationException(e.Message, e);
            }

            CSharpMigrationEntity m = new CSharpMigrationEntity
            {
                ExecutionDate = TimeZoneManager.Now,
                UniqueName = mi.UniqueName,
            }.Save();

            mi.IsExecuted = true;
        }

        private void Draw(MigrationInfo? current)
        {
            Console.WriteLine();

            foreach (var mi in this.Migrations)
            {
                ConsoleColor color = mi.IsExecuted ? ConsoleColor.DarkGreen :
                                     current == mi ? ConsoleColor.Green :
                                     ConsoleColor.White;

                SafeConsole.WriteColor(color,  
                    mi.IsExecuted?  "- " : 
                    current == mi ? "->" : 
                                    "  ");

                SafeConsole.WriteLineColor(color, mi.UniqueName);
            }

            Console.WriteLine();
        }

        public class MigrationInfo
        {
            public Action Action;
            public string UniqueName;
            public bool IsExecuted;

            public MigrationInfo(Action action, string uniqueName)
            {
                Action = action;
                UniqueName = uniqueName;
            }

            public override string ToString()
            {
                return UniqueName;
            }

        }


        public IEnumerator<CSharpMigrationRunner.MigrationInfo> GetEnumerator()
        {
            return this.Migrations.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.Migrations.GetEnumerator();
        }
    }
}
