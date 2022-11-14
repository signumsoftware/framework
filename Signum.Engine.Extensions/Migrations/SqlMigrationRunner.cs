using Signum.Engine.Cache;
using Signum.Entities.Migrations;
using System.IO;
using System.Text.RegularExpressions;

namespace Signum.Engine.Migrations;

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

            if (!autoRun && !Connector.Current.HasTables() && list.Count == 0)
            {
                if (!SafeConsole.Ask("Create initial migration?"))
                    return;

                CreateInitialMigration();
            }
            else
            {
                SetExecuted(list);

                if (!Prompt(list, autoRun) || autoRun)
                    return;
            }
        }
    }

    public static void CreateInitialMigration()
    {
        var script = Schema.Current.GenerationScipt(databaseNameReplacement: DatabaseNameReplacement)!;

        string version = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");

        string comment = InitialMigrationComment;

        string fileName = version + "_" + FileNameValidatorAttribute.RemoveInvalidCharts(comment) + ".sql";

        File.WriteAllText(Path.Combine(MigrationsDirectory, fileName), script.ToString(), Encoding.UTF8);
    }

    private static void SetExecuted(List<MigrationInfo> migrations)
    {
        if (!Connector.Current.HasTables())
            return;

        MigrationLogic.EnsureMigrationTable<SqlMigrationEntity>();

        var first = migrations.FirstOrDefault();

        var executedMigrations = Database.Query<SqlMigrationEntity>().Select(m => new { m.VersionNumber, m.Comment })
            .OrderBy(a => a.VersionNumber)
            .ToList()
            .Where(d => first == null || first.Version.CompareTo(d.VersionNumber) <= 0)
            .ToList();

        var dic = migrations.ToDictionaryEx(a => a.Version, "Migrations in folder");

        foreach (var migration in executedMigrations)
        {
            var m = dic.TryGetC(migration.VersionNumber!);
            if (m != null)
                m.IsExecuted = true;
            else
                migrations.Add(new MigrationInfo
                {
                    FileName = null,
                    Comment = ">> In Database Only << " + migration.Comment,
                    IsExecuted = true,
                    Version = migration.VersionNumber!
                });

        }

        migrations.Sort(a => a.Version);
    }

    public static bool MigrationDirectoryIsEmpty()
    {
        return !Directory.Exists(MigrationsDirectory) || Directory.EnumerateFiles(MigrationsDirectory).IsEmpty();
    }

    public static List<MigrationInfo> ReadMigrationsDirectory(bool silent = false)
    {
        if (silent)
        {
            if (!Directory.Exists(MigrationsDirectory))
                return new List<MigrationInfo>();
        }
        else
        {
            if (!Directory.Exists(MigrationsDirectory))
            {
                Directory.CreateDirectory(MigrationsDirectory);
                SafeConsole.WriteLineColor(ConsoleColor.White, "Directory " + MigrationsDirectory + " auto-generated...");
            }
            else
            {
                Console.WriteLine();
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "Reading migrations from: " + MigrationsDirectory);
            }
        }

        Regex regex = new Regex(@"(?<version>\d{4}\.\d{2}\.\d{2}\-\d{2}\.\d{2}\.\d{2})(_(?<comment>.+))?\.sql");

        var matches =  Directory.EnumerateFiles(MigrationsDirectory, "*.sql")
            .Select(fileName => new { fileName, match = regex.Match(Path.GetFileName(fileName)) }).ToList();

        var errors = matches.Where(a => !a.match!.Success);

        if (errors.Any())
            throw new InvalidOperationException("Some scripts in the migrations directory have an invalid format (yyyy.MM.dd-HH.mm.ss_OptionalComment.sql) " +
                errors.ToString(a => Path.GetFileName(a.fileName), "\r\n"));

        var list = matches.Select(a => new MigrationInfo
        {
            FileName = a.fileName,
            Version = a.match!.Groups["version"].Value,
            Comment = a.match!.Groups["comment"].Value,
        }).OrderBy(a => a.Version).ToList();
        
        return list;
    }

    public const string DatabaseNameReplacement = "#DatabaseName#";
    public const string InitialMigrationComment = "Initial Migration";

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
                DateTime start = Clock.Now;

                foreach (var mi in migrations.AsEnumerable().Where(a => !a.IsExecuted))
                {
                    Draw(migrations, mi);

                    Execute(mi);
                }

                ResetCache();

                Console.WriteLine("Elapsed time: {0}".FormatWith(Clock.Now.Subtract(start).ToString(@"hh\:mm\:ss")));

                return true;
            }
            catch (ExecuteSqlScriptException)
            {
                if (autoRun)
                    throw;

                return true;
            }
        }

    }

    private static void ResetCache()
    {
        CacheLogic.ForceReset();
        GlobalLazy.ResetAll();
        Schema.Current.InvalidateMetadata();
    }

    private static void Execute(MigrationInfo mi)
    {
        string? title = mi.Version + (mi.Comment.HasText() ? " ({0})".FormatWith(mi.Comment) : null);
        string text = File.ReadAllText(mi.FileName!, Encoding.UTF8);
        
        using (var tr = Transaction.ForceNew(System.Data.IsolationLevel.Unspecified))
        {
            string databaseName = Connector.Current.DatabaseName();

            var script = text.Replace(DatabaseNameReplacement, databaseName);

            SqlPreCommandExtensions.ExecuteScript(title, script);

            MigrationLogic.EnsureMigrationTable<SqlMigrationEntity>();

            new SqlMigrationEntity
            {
                VersionNumber = mi.Version,
                Comment = mi.Comment,
            }.Save();

            mi.IsExecuted = true;

            tr.Commit();
        }
    }


    private static void Draw(List<MigrationInfo> migrationsInOrder, MigrationInfo? current)
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


    public static void SquashMigrationHistory()
    {
        Console.WriteLine();

        Console.WriteLine("Squash Migration History will reset all the SQL Migration history into one Initial Migration");
        Console.WriteLine();

        Console.WriteLine("This operation doesn't change your database schema, but will delete your migration history.");
        Console.WriteLine();

        Console.WriteLine("First step is to check that there are no differences between the current database and the application");
        Console.WriteLine();

        Console.WriteLine("Press [ENTER] to start the synchronization");

        Console.ReadLine();

        while (Administrator.TotalSynchronizeScript() is SqlPreCommand cmd)
        {
            if(cmd != null)
            {
                cmd.OpenSqlFileRetry();
            }
        }

        Console.WriteLine();

        SafeConsole.WriteLineColor(ConsoleColor.Green, "Perfectly Synchronized!");
        
        Console.WriteLine();

        var files = Directory.EnumerateFiles(MigrationsDirectory);
        SafeConsole.WriteLineColor(files.Count() == 0 ? ConsoleColor.Green : ConsoleColor.Yellow, $"{files.Count()} files found in the migration directry ({MigrationsDirectory})");

        MigrationLogic.EnsureMigrationTable<SqlMigrationEntity>();

        var executedMigrations = Database.Query<SqlMigrationEntity>().Count();

        SafeConsole.WriteLineColor(executedMigrations == 0 ? ConsoleColor.Green : ConsoleColor.Yellow, $"{executedMigrations} executed migrations in the database ({Schema.Current.Table(typeof(SqlMigrationEntity)).Name})");

        if (files.Count() > 0 || executedMigrations > 0)
        {
            Console.WriteLine();
            if (SafeConsole.Ask("Confirm that do you want to remove all the migrations by writing 'squash'", "squash") == "squash")
            {
                files.ToList().ForEach(f =>
                {
                    File.Delete(f);
                    SafeConsole.WriteLineColor(ConsoleColor.Red, "File deleted: " + f);
                });

                Database.Query<SqlMigrationEntity>().UnsafeDelete(message: "wait");
            }
        }

        Console.WriteLine("Generating Initial Migration file...");

        var script = Schema.Current.GenerationScipt(databaseNameReplacement: DatabaseNameReplacement)!;

        string version = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");

        string comment = InitialMigrationComment;

        string fileName = version + "_" + FileNameValidatorAttribute.RemoveInvalidCharts(comment) + ".sql";


        File.WriteAllText(Path.Combine(MigrationsDirectory, fileName), script.ToString(), Encoding.UTF8);

        new SqlMigrationEntity
        {
            Comment = comment,
            VersionNumber = version,
        }.Save();

        SafeConsole.WriteLineColor(ConsoleColor.Green, "Initial Migration saved and marked as executed");
    }

    public class MigrationInfo
    {
        public required string? FileName;
        public required string Version;
        public required string Comment;

        public bool IsExecuted;

        public override string ToString()
        {
            return Version;
        }
    }
}
