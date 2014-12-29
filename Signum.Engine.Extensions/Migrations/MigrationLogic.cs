using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Migrations;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Engine.Migrations
{
    public static class MigrationLogic
    {
        public const string Genesis = "Genesis";

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<MigrationEntity>();

                dqm.RegisterQuery(typeof(MigrationEntity), () =>
                    from e in Database.Query<MigrationEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.VersionNumber,
                        e.ExecutionDate,
                    });
            }
        }

        public static void Migrations()
        {
            if (Directory.Exists(@"..\..\Migrations"))
                Migrations(@"..\..\Migrations");
            else
                Migrations("Migrations");
        }

        public static void Migrations(string migrationDirectory)
        {
            while (true)
            {
                Dictionary<string, MigrationInfo> dictionary = ReadMigrationsDirectory(migrationDirectory);

                DirectedGraph<MigrationInfo> graph = CreateMigrationChain(dictionary);

                if (!Prompt(graph, migrationDirectory))
                    return;
        }
        }

        private static DirectedGraph<MigrationInfo> CreateMigrationChain(Dictionary<string, MigrationInfo> dictionary)
        {
            var graph = DirectedGraph<MigrationInfo>.Generate(dictionary.Values, mi => mi.From == MigrationLogic.Genesis ?
                new MigrationInfo[0] : new[] { dictionary.TryGetC(mi.From) });

            var parents = graph.Where(a => graph.RelatedTo(a).IsEmpty()).ToList();

            if (parents.Count > 1)
                throw new InvalidOperationException("There are holes in the Migration chain, the top-most parents are: \r\n" + parents.ToString(a => "\t" + a.Version, "\r\n"));

            var feedback = graph.FeedbackEdgeSet();
            if (feedback.Edges.Any())
                throw new InvalidOperationException("There are cyclic dependencies! Checkout the edges: " + feedback.Edges.ToString(" "));

            var list = Database.Query<MigrationEntity>().OrderBy(m => m.ExecutionDate).Select(m => new { m.VersionNumber, m.ExecutionDate }).ToList();

            foreach (var v in list.SkipWhile(ver => !dictionary.ContainsKey(ver.VersionNumber)))
            {
                var info = dictionary.TryGetC(v.VersionNumber);
                if (info == null)
                    throw new InvalidOperationException("Migration {0} was executed but no script found in Migrations directory".FormatWith(v.VersionNumber));

                info.Executed = v.ExecutionDate;
            }

            var parent = parents.SingleOrDefault();
            if (parent == null)
            {
                if (list.Any())
                    throw new InvalidOperationException("There are no migration files but some migrations have been executed!. Last executed migration is '{0}'".FormatWith(list.Last().VersionNumber));
            }
            else if (parent.Executed == null)
            {
                var set = list.Select(a => a.VersionNumber).ToHashSet();

                if (parent.From == Genesis && set.Any())
                    throw new InvalidOperationException("The first migration in the folder ({0}) has not been executed but there are executed migrations!".FormatWith(parent.Version));

                if (parent.From != Genesis && !set.Contains(parent.From))
                    throw new InvalidOperationException("Neither the first-known migration ({0}) nor his parent ({1}) have been excuted".FormatWith(parent.Version, parent.From));
            }

            return graph;
        }

        private static Dictionary<string, MigrationInfo> ReadMigrationsDirectory(string migrationDirectory)
        {
            Console.WriteLine("Reading migrations from: " + migrationDirectory);

            Regex regex = new Regex("(?<version>[^_-]+)(-(?<from>[^_]+))(_(?<comment>.+))?.sql");

            var matches = Directory.EnumerateFiles(migrationDirectory, "*.sql").Select(fileName => new { fileName, match = regex.Match(Path.GetFileName(fileName)) }).ToList();

            var errors = matches.Where(a => !a.match.Success);

            if (errors.Any())
                throw new InvalidOperationException("Some scripts in the migrations directory have an invalid format (Version_PreviousVersion[_Comment].sql): " +
                    errors.ToString(a => Path.GetFileName(a.fileName), "\r\n"));

            var dictionary = matches.Select(a => new MigrationInfo
            {
                FileName = a.fileName,
                Version = a.match.Groups["version"].Value,
                From = a.match.Groups["from"].Value,
                Comment = a.match.Groups["comment"].Value,
            }).ToDictionary(a => a.Version, "Migrations with the same version");
            return dictionary;
        }

        public const string DatabaseNameReplacement = "$DatabaseName$";

        private static bool Prompt(DirectedGraph<MigrationInfo> graph, string migrationDirectory)
        {
            var inverse = graph.Inverse();

            List<MigrationInfo> childrens = graph.Where(a => !inverse.RelatedTo(a).Any()).OrderByDescending(a => a.Executed).ThenByDescending(a => a.Version).ToList();

            for (int i = 0; i < childrens.Count; i++)
            {
                var c = childrens[i];

                foreach (var mi in c.Follow(a => graph.RelatedTo(a).SingleOrDefaultEx()))
                {
                    if (mi.TrackNumber.HasValue)
                        break;

                    mi.TrackNumber = i;

                    var parent = graph.RelatedTo(mi).SingleOrDefaultEx();
                    if (parent == null || parent.TrackNumber.HasValue)
                        mi.IsLast = true;
                }
            }

            var comparer = Comparer<MigrationInfo>.Create((a, b) =>
                graph.IndirectlyRelatedTo(a).Contains(b) ? 1 :
                graph.IndirectlyRelatedTo(b).Contains(a) ? -1 : 0);

            List<MigrationInfo> migrationsInOrder = graph.OrderBy(a => a, comparer).ThenBy(a => a.Version).ToList();

            var multiChildren = Draw(migrationsInOrder, childrens, null);

            Console.WriteLine();

            if (multiChildren)
            {
                Console.Write("A Git merge has produced ");
                SafeConsole.WriteColor(ConsoleColor.Magenta, childrens.Count.ToString());
                Console.WriteLine(" parallel migration chains.");
                Console.WriteLine();
                Console.WriteLine("Fix the conflicts by:");
                Console.WriteLine("- Manually remove the conflicting migration chain and re-create new migrations");
                Console.WriteLine("- Manually rebase on chain on top of the other (if the scripts are independent!)");

                return SafeConsole.Ask("Try again?");
            }

            var child = childrens.SingleOrDefaultEx();

            if (child == null || child.Executed.HasValue)
            {
                if (!SafeConsole.Ask("Create new migration?"))
                    return false;

                var script = Schema.Current.SynchronizationScript(interactive: true, replaceDatabaseName: DatabaseNameReplacement);

                if (script == null)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Green, "No changes found!");
                    return false;
                }
                else
                {
                    string fileName = GetFileName(child);

                    File.WriteAllText(Path.Combine(migrationDirectory, fileName), script.ToString());

                    string csproj = Directory.EnumerateFiles(Path.Combine(migrationDirectory, ".."), "*Load.csproj").SingleOrDefaultEx();

                    if (csproj != null)
                        AddCsprojReference(fileName, csproj);
                }

                return true;
            }
            else
            {
                if (!SafeConsole.Ask("Run migrations ({0})?".FormatWith(migrationsInOrder.Count(a => !a.Executed.HasValue))))
                    return false;

                try
                {
                    foreach (var item in migrationsInOrder.AsEnumerable().SkipWhile(a => a.Executed.HasValue))
                    {
                        Draw(migrationsInOrder, childrens, item);

                        Execute(item);

                    }

                    return true;

                }
                catch (MigrationException e)
                {
                    return true;
                }
            }

        }

        private static void Execute(MigrationInfo mi)
        {
            string text = null;

            string title = mi.Version + (mi.Comment.HasText() ? "({0})".FormatWith(mi.Comment) : null);

            string databaseName = Connector.Current.DatabaseName();

            using (Transaction tr = new Transaction())
            {
                text = File.ReadAllText(mi.FileName);

                text = text.Replace(DatabaseNameReplacement, databaseName);

                try
                {
                    SafeConsole.WaitExecute("Executing {0}".FormatWith(title), () => Executor.ExecuteNonQuery(text));
                }
                catch (SqlException e)
                {
                    Console.WriteLine();
                    Console.WriteLine();

                    var list = text.Lines();

                    var lineNumer = e.LineNumber - 1;

                    SafeConsole.WriteLineColor(ConsoleColor.Red, "ERROR:");

                    var min = Math.Max(0, lineNumer - 20);
                    var max = Math.Min(list.Length - 1, lineNumer + 20);

                    if (min > 0)
                        Console.WriteLine("...");

                    for (int i = min; i <= max; i++)
                    {
                        Console.Write(i + ": ");
                        SafeConsole.WriteLineColor(i == lineNumer ? ConsoleColor.Red: ConsoleColor.DarkRed, list[i]);
                    }


                    if (max < list.Length - 1)
                        Console.WriteLine("...");

                    Console.WriteLine();
                    SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.GetType().Name + (e is SqlException ? " (Number {0}): ".FormatWith(((SqlException)e).Number) : ": "));
                    SafeConsole.WriteLineColor(ConsoleColor.Red, e.Message);

                    Console.WriteLine();

                    throw new MigrationException();
                }

                MigrationEntity m = new MigrationEntity
                {
                    ExecutionDate = TimeZoneManager.Now,
                    VersionNumber = mi.Version,
                }.Save();

                mi.Executed = m.ExecutionDate;

                tr.Commit();
            }
        }

        private static bool Draw(List<MigrationInfo> migrationsInOrder, List<MigrationInfo> childrens, MigrationInfo current)
        {
            bool[] declared = new bool[childrens.Count];

            var multiChildren = childrens.Count > 1;

            foreach (var mi in migrationsInOrder)
            {
                ConsoleColor color = mi.Executed.HasValue ? ConsoleColor.DarkGreen :
                                     current == mi ? ConsoleColor.Green :
                                     ConsoleColor.White;

                for (int i = 0; i < childrens.Count; i++)
                {
                    if (mi.TrackNumber == i)
                        SafeConsole.WriteColor(color, "*");
                    else
                        Console.Write(declared[i] ? "|" : " ");


                    Console.Write(i < mi.TrackNumber && mi.IsLast ? "-" : " ");
                }

                if (multiChildren)
                    SafeConsole.WriteColor(ConsoleColor.Magenta, childrens.Contains(mi) ? "[" + childrens.IndexOf(mi) + "]" : "   ");
                SafeConsole.WriteColor(color, mi.Version);
                Console.WriteLine(" " + mi.Comment);

                declared[mi.TrackNumber.Value] = !mi.IsLast;
            }
            return multiChildren;
        }

        private static void AddCsprojReference(string fileName, string csproj)
        {
            var doc = XDocument.Load(csproj);
            var xmlns = (XNamespace)doc.Document.Root.Attribute("xmlns").Value;

            var element = new XElement(xmlns + "Content",
               new XAttribute("Include", @"Migrations\" + fileName),
               new XElement(xmlns + "CopyToOutputDirectory", "PreserveNewest"));

            var itemGroups = doc.Document.Root.Elements(xmlns + "ItemGroup");

            var lastContent = itemGroups
                .SelectMany(e => e.Elements(xmlns + "Content").Where(a => a.Attribute("Include").Value.StartsWith(@"Migrations\")))
                .LastOrDefault();

            if (lastContent != null)
                lastContent.AddAfterSelf();
            else
                itemGroups.Last().Add(element);

            doc.Save(csproj);
        }

        private static string GetFileName(MigrationInfo child)
        {
            var parent = child != null ? child.Version : Genesis;

            string fileName = "{0}-{1}".FormatWith(DateTime.Now.ToString("yyyy.MM.dd.HH.mm"), parent);

            string comment = SafeConsole.AskString("Comment for the new Migration?", stringValidator: s => null);

            if (comment.HasText())
                fileName += "_" + FileNameValidatorAttribute.RemoveInvalidCharts(comment);

            fileName += ".sql";
            return fileName;
        }
    }

    public class MigrationInfo
    {
        public string FileName;
        public string Version;
        public string From;
        public string Comment;
        public DateTime? Executed;
        public int? TrackNumber;
        public char[] Charts;

        public bool IsLast;

        public override string ToString()
        {
            return Version;
        }

    }

    [Serializable]
    class MigrationException : Exception
    {
        public MigrationException() { }
    }
}
