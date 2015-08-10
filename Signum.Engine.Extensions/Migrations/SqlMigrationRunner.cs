using Signum.Engine.Cache;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Migrations;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Signum.Engine.Migrations
{
    public class SqlMigrationRunner
    {
        public static string MigrationsDirectory = @"..\..\Migrations";
        public static string MigrationsDirectoryName = "Migrations";

        public static void SqlMigrations()
        {
            SqlMigrations(autoRun: false);
        }

        public static void SqlMigrations(bool autoRun)
        {
            while (true)
            {
                Dictionary<string, MigrationInfo> dictionary = ReadMigrationsDirectory();

                SetExecuted(dictionary);

                if (!Prompt(dictionary, autoRun) || autoRun)
                    return;
            }
        }

        private static void SetExecuted(Dictionary<string, MigrationInfo> dictionary)
        {
            MigrationLogic.EnsureMigrationTable<SqlMigrationEntity>();

            var folder = dictionary.Select(a => a.Value.Version).OrderBy().ToList();
            var database = Database.Query<SqlMigrationEntity>().Select(m => m.VersionNumber).OrderBy().ToList();

            if (database.Any() && folder.Any())
            {
                {
                    var maxDatabase = database.Max();

                    var oldNotExecuted = folder.Except(database).Where(f => f.CompareTo(maxDatabase) < 0).ToList();

                    if (oldNotExecuted.Any())
                        throw new InvalidOperationException("There are old migrations in the folder that have not been executed!:\r\n" + oldNotExecuted.ToString("\r\n").Indent(4));
                }

                {
                    var minFolder = folder.Min();

                    var executedNewMigrations = database.Except(folder).Where(f => minFolder.CompareTo(f) < 0).ToList();

                    if (executedNewMigrations.Any())
                        throw new InvalidOperationException("There are executed migrations newer than anything in the folder!:\r\n" + executedNewMigrations.ToString("\r\n").Indent(4));
                }
            }

            foreach (var v in dictionary.Values)
            {
                v.IsExecuted = database.Contains(v.Version);
            }
        }

        private static Dictionary<string, MigrationInfo> ReadMigrationsDirectory()
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

            var dictionary = matches.Select(a => new MigrationInfo
            {
                FileName = a.fileName,
                Version = a.match.Groups["version"].Value,
                Comment = a.match.Groups["comment"].Value,
            }).ToDictionary(a => a.Version, "Migrations with the same version");

            return dictionary;
        }

        public const string DatabaseNameReplacement = "$DatabaseName$";

        private static bool Prompt(Dictionary<string, MigrationInfo> graph, bool autoRun)
        {
            List<MigrationInfo> migrationsInOrder = graph.Values.OrderBy(a => a.Version).ToList();

            Draw(migrationsInOrder, null);

            var last = migrationsInOrder.LastOrDefault() ?? null;
            if (migrationsInOrder.All(a=>a.IsExecuted))
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
                    string version = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");

                    string comment = SafeConsole.AskString("Comment for the new Migration? ", stringValidator: s => null);

                    string fileName = version + (comment.HasText() ? "_" + FileNameValidatorAttribute.RemoveInvalidCharts(comment): null) + ".sql";

                    File.WriteAllText(Path.Combine(MigrationsDirectory, fileName), script.ToString());

                    AddCsprojReference(fileName);
                }

                return true;
            }
            else
            {
                if (!autoRun && !SafeConsole.Ask("Run {0} migration(s)?".FormatWith(migrationsInOrder.Count(a => !a.IsExecuted))))
                    return false;

                try
                {
                    foreach (var item in migrationsInOrder.AsEnumerable().SkipWhile(a => a.IsExecuted))
                    {
                        Draw(migrationsInOrder, item);

                        Execute(item, autoRun);
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

        public static int Timeout = 5 * 60; 

        private static void Execute(MigrationInfo mi, bool autoRun)
        {
            string title = mi.Version + (mi.Comment.HasText() ? " ({0})".FormatWith(mi.Comment) : null);

            string databaseName = Connector.Current.DatabaseName();

            using (Connector.CommandTimeoutScope(Timeout))
            using (Transaction tr = new Transaction())
            {
                string text = File.ReadAllText(mi.FileName);

                text = text.Replace(DatabaseNameReplacement, databaseName);

                var parts = Regex.Split(text, " *GO *\r?\n", RegexOptions.IgnoreCase).Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();

                int pos = 0;

                try
                {   
                    for (pos = 0; pos < parts.Length; pos++)
			        {
                        if (autoRun)
                            Executor.ExecuteNonQuery(parts[pos]);
                        else
                            SafeConsole.WaitExecute("Executing {0} [{1}/{2}]".FormatWith(title, pos + 1, parts.Length), () => Executor.ExecuteNonQuery(parts[pos]));
			        }
                }
                catch (SqlException e)
                {
                    Console.WriteLine();
                    Console.WriteLine();

                    var list = text.Lines();

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

                    throw new MigrationException();
                }

                SqlMigrationEntity m = new SqlMigrationEntity
                {
                    VersionNumber = mi.Version,
                }.Save();

                mi.IsExecuted = true;

                tr.Commit();
            }
        }

        private static void Draw(List<MigrationInfo> migrationsInOrder, MigrationInfo current)
        {
            Console.WriteLine();

            foreach (var mi in migrationsInOrder)
            {
                ConsoleColor color = mi.IsExecuted ? ConsoleColor.DarkGreen :
                                     current == mi ? ConsoleColor.Green :
                                     ConsoleColor.White;


                SafeConsole.WriteColor(color,  
                    mi.IsExecuted?  "- " : 
                    current == mi ? "->" : 
                                    "  ");
                
                SafeConsole.WriteColor(color, mi.Version);
                Console.WriteLine(" " + mi.Comment);
            }

            Console.WriteLine();
        }

        private static void AddCsprojReference(string fileName)
        {
            string csproj = Directory.EnumerateFiles(Path.Combine(MigrationsDirectory, ".."), "*Load.csproj").SingleEx(() => "Load.csproj");

            var doc = XDocument.Load(csproj);
            var xmlns = (XNamespace)doc.Document.Root.Attribute("xmlns").Value;

            var element = new XElement(xmlns + "Content",
               new XAttribute("Include", @"Migrations\" + fileName));

            var itemGroups = doc.Document.Root.Elements(xmlns + "ItemGroup");

            var lastContent = itemGroups
                .SelectMany(e => e.Elements(xmlns + "Content").Where(a => a.Attribute("Include").Value.StartsWith(@"Migrations\")))
                .LastOrDefault();

            if (lastContent != null)
                lastContent.AddAfterSelf(element);
            else
                itemGroups.Last().Add(element);

            doc.Save(csproj);
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
