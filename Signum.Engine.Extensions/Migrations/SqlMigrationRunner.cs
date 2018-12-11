using Signum.Engine.Maps;
using Signum.Engine.SchemaInfoTables;
using Signum.Entities;
using Signum.Entities.Migrations;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Signum.Engine.Migrations
{
    public class SqlMigrationRunner
    {
        public static string MigrationsDirectory = @"..\..\..\Migrations";

        public static void SqlMigrations()
        {
            SqlMigrations(autoRun: false);
        }

        public static void SqlMigrations(bool autoRun)
        {
            while (true)
            {
                List<MigrationInfo> list = ReadMigrationsDirectory();

                SetExecuted(list);

                if (!Prompt(list, autoRun) || autoRun)
                    return;
            }
        }

        private static void SetExecuted(List<MigrationInfo> migrations)
        {
            MigrationLogic.EnsureMigrationTable<SqlMigrationEntity>();
            AddCommentColumnIfNecessary();

            var first = migrations.FirstOrDefault();

            var executedMigrations = Database.Query<SqlMigrationEntity>().Select(m => new { m.VersionNumber, m.Comment })
                .OrderBy(a => a.VersionNumber)
                .ToList()
                .Where(d => first == null || first.Version.CompareTo(d.VersionNumber) <= 0)
                .ToList();

            var dic = migrations.ToDictionaryEx(a => a.Version, "Migrations in folder");

            foreach (var migration in executedMigrations)
            {
                var m = dic.TryGetC(migration.VersionNumber);
                if (m != null)
                    m.IsExecuted = true;
                else
                    migrations.Add(new MigrationInfo
                    {
                        FileName = null,
                        Comment = ">> In Database Only << " + migration.Comment,
                        IsExecuted = true,
                        Version = migration.VersionNumber
                    });

            }

            migrations.Sort(a => a.Version);
        }

        private static void AddCommentColumnIfNecessary()
        {
            var table = Schema.Current.Table<SqlMigrationEntity>();
            var col = table.Columns[nameof(SqlMigrationEntity.Comment)];

            var hasComment = Database.View<SysTables>()
                .Where(a => a.Schema().name == table.Name.Schema.Name && a.name == table.Name.Name)
                .SelectMany(t => t.Columns())
                .Any(c => c.name == col.Name);
                
            if (!hasComment)
            {
                SafeConsole.WriteLineColor(ConsoleColor.White, "Column " + col.Name + " created in " + table.Name + "...");
                Executor.ExecuteNonQuery($"ALTER TABLE {table.Name} ADD {col.Name} NVARCHAR({col.Size}) NULL");
            }
        }

        public static List<MigrationInfo> ReadMigrationsDirectory()
        {
            Console.WriteLine();
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "Reading migrations from: " + MigrationsDirectory);

            if (!Directory.Exists(MigrationsDirectory))
            {
                Directory.CreateDirectory(MigrationsDirectory);
                SafeConsole.WriteLineColor(ConsoleColor.White, "Directory " + MigrationsDirectory + " auto-generated...");
            }

            Regex regex = new Regex(@"(?<version>\d{4}\.\d{2}\.\d{2}\-\d{2}\.\d{2}\.\d{2})(_(?<comment>.+))?\.sql");

            var matches =  Directory.EnumerateFiles(MigrationsDirectory, "*.sql")
                .Select(fileName => new { fileName, match = regex.Match(Path.GetFileName(fileName)) }).ToList();

            var errors = matches.Where(a => !a.match.Success);

            if (errors.Any())
                throw new InvalidOperationException("Some scripts in the migrations directory have an invalid format (yyyy.MM.dd-HH.mm.ss_OptionalComment.sql) " +
                    errors.ToString(a => Path.GetFileName(a.fileName), "\r\n"));

            var list = matches.Select(a => new MigrationInfo
            {
                FileName = a.fileName,
                Version = a.match.Groups["version"].Value,
                Comment = a.match.Groups["comment"].Value,
            }).OrderBy(a => a.Version).ToList();
            
            return list;
        }

        public const string DatabaseNameReplacement = "#DatabaseName#";

        private static bool Prompt(List<MigrationInfo> migrations, bool autoRun)
        {
            Draw(migrations, null);

            if (migrations.Any(a => a.IsExecuted && a.FileName == null))
            {
                var str = "There are fresh executed migrations that are not in the folder. Get latest version?";
                if (autoRun)
                    throw new InvalidOperationException(str);

                SafeConsole.WriteLineColor(ConsoleColor.Red, str);
                return false;
            }


            if (migrations.SkipWhile(a => a.IsExecuted).Any(a => a.IsExecuted))
            {
                var str = "Possible merge conflict. There are old migrations in the folder that have not been executed!. You need to manually discard one migration branch.";
                if (autoRun)
                    throw new InvalidOperationException(str);

                SafeConsole.WriteLineColor(ConsoleColor.Red, str);
                Console.WriteLine();
                Console.Write("Write '");
                SafeConsole.WriteColor(ConsoleColor.White, "force");
                Console.WriteLine("' to execute them anyway");

                if (Console.ReadLine() != "force")
                    return false;
            }

            var last = migrations.LastOrDefault() ?? null;
            if (migrations.All(a=>a.IsExecuted))
            {
                if (autoRun || !SafeConsole.Ask("Create new migration?"))
                    return false;

                var script = Schema.Current.SynchronizationScript(interactive: true, replaceDatabaseName: DatabaseNameReplacement);

                if (script == null)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Green, "No changes found!");
                    return false;
                }
                else
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Yellow, "Some changes found, here is the script:");
                    Console.WriteLine();

                    SafeConsole.WriteLineColor(ConsoleColor.DarkGray, script.ToString().Indent(4));

                    Console.WriteLine();

                    string version = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");

                    string comment = SafeConsole.AskString("Comment for the new Migration? ", stringValidator: s => null).Trim();

                    string fileName = version + (comment.HasText() ? "_" + FileNameValidatorAttribute.RemoveInvalidCharts(comment): null) + ".sql";

                    File.WriteAllText(Path.Combine(MigrationsDirectory, fileName), script.ToString(), Encoding.UTF8);
                }

                return true;
            }
            else
            {
                if (!autoRun && !SafeConsole.Ask("Run {0} migration(s)?".FormatWith(migrations.Count(a => !a.IsExecuted))))
                    return false;

                try
                {
                    DateTime start = TimeZoneManager.Now;

                    foreach (var mi in migrations.AsEnumerable().Where(a => !a.IsExecuted))
                    {
                        Draw(migrations, mi);

                        Execute(mi);
                    }

                    Console.WriteLine("Elapsed time: {0}".FormatWith(TimeZoneManager.Now.Subtract(start).ToString(@"hh\:mm\:ss")));

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

        public static int Timeout = 20 * 60; 

        private static void Execute(MigrationInfo mi)
        {
            string title = mi.Version + (mi.Comment.HasText() ? " ({0})".FormatWith(mi.Comment) : null);
            string text = File.ReadAllText(mi.FileName, Encoding.UTF8);
            
            using (Transaction tr = Transaction.ForceNew(System.Data.IsolationLevel.Unspecified))
            {
                ExecuteScript(title, text);

                SqlMigrationEntity m = new SqlMigrationEntity
                {
                    VersionNumber = mi.Version,
                    Comment = mi.Comment,
                }.Save();

                mi.IsExecuted = true;

                tr.Commit();
            }
        }

        public static void ExecuteScript(string title, string script)
        {
            using (Connector.CommandTimeoutScope(Timeout))
            {
                string databaseName = Connector.Current.DatabaseName();

                script = script.Replace(DatabaseNameReplacement, databaseName);

                var parts = Regex.Split(script, " *GO *(\r?\n|$)", RegexOptions.IgnoreCase).Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();

                int pos = 0;

                try
                {
                    for (pos = 0; pos < parts.Length; pos++)
                    {
                        SafeConsole.WaitExecute("Executing {0} [{1}/{2}]".FormatWith(title, pos + 1, parts.Length), () => Executor.ExecuteNonQuery(parts[pos]));
                    }
                }
                catch (Exception ex)
                {
                    var e = ex as SqlException ?? ex.InnerException as SqlException;
                    if (e == null)
                        throw;

                    Console.WriteLine();
                    Console.WriteLine();

                    var list = script.Lines();

                    var lineNumer = (e.LineNumber - 1) + pos + parts.Take(pos).Sum(a => a.Lines().Length);

                    SafeConsole.WriteLineColor(ConsoleColor.Red, "ERROR:");

                    var min = Math.Max(0, lineNumer - 20);
                    var max = Math.Min(list.Length - 1, lineNumer + 20);

                    if (min > 0)
                        Console.WriteLine("...");

                    for (int i = min; i <= max; i++)
                    {
                        Console.Write(i + ": ");
                        SafeConsole.WriteLineColor(i == lineNumer ? ConsoleColor.Red : ConsoleColor.DarkRed, list[i]);
                    }

                    if (max < list.Length - 1)
                        Console.WriteLine("...");

                    Console.WriteLine();
                    SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.GetType().Name + (e is SqlException ? " (Number {0}): ".FormatWith(((SqlException)e).Number) : ": "));
                    SafeConsole.WriteLineColor(ConsoleColor.Red, e.Message);

                    Console.WriteLine();

                    throw new MigrationException(ex.Message, ex);
                }
            }
        }

        private static void Draw(List<MigrationInfo> migrationsInOrder, MigrationInfo current)
        {
            Console.WriteLine();

            foreach (var mi in migrationsInOrder)
            {
                ConsoleColor color = current == mi ? ConsoleColor.Green :
                    mi.FileName != null && mi.IsExecuted ? ConsoleColor.DarkGreen :
                    mi.FileName == null && mi.IsExecuted ? ConsoleColor.Red :
                    mi.FileName != null && !mi.IsExecuted ? ConsoleColor.White :
                    throw new InvalidOperationException();


                SafeConsole.WriteColor(color,  
                    mi.IsExecuted?  "- " : 
                    current == mi ? "->" : 
                                    "  ");
                
                SafeConsole.WriteColor(color, mi.Version);
                SafeConsole.WriteLineColor(mi.FileName == null ? ConsoleColor.Red: ConsoleColor.Gray, " " + mi.Comment);
            }

            Console.WriteLine();
        }

        public class MigrationInfo
        {
            public string FileName;
            public string Version;
            public string Comment;

            public bool IsExecuted;

            public override string ToString()
            {
                return Version;
            }

        }
    }
}
