using Signum.Entities.Migrations;
using Signum.Engine.SchemaInfoTables;
using Signum.Entities.Basics;

namespace Signum.Engine.Migrations;

public static class MigrationLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Include<SqlMigrationEntity>()
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.VersionNumber,
                });

            sb.Include<CSharpMigrationEntity>()
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.UniqueName,
                    e.ExecutionDate,
                });

            sb.Include<LoadMethodLogEntity>()
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Start,
                    e.Duration,
                    e.ClassName,
                    e.MethodName,
                    e.Description,
                });

            ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;

            Administrator.AvoidSimpleSynchronize = () =>
            {
                if (Administrator.ExistsTable<SqlMigrationEntity>())
                {
                    var count = Database.Query<SqlMigrationEntity>().Count();
                    if (count > 0)
                    {
                        Console.Write("The database ");
                        SafeConsole.WriteColor(ConsoleColor.Yellow, Connector.Current.DatabaseName());
                        Console.Write(" contains ");
                        SafeConsole.WriteColor(ConsoleColor.Yellow, count.ToString());
                        Console.WriteLine(" Sql Migrations!");

                        if (SafeConsole.Ask("Do you want to create a new SQL Migration instead?", "continue", "migrations") == "migrations")
                        {
                            SqlMigrationRunner.SqlMigrations();
                            return true;
                        }
                    }
                }

                return false;
            };

            Administrator.AvoidSimpleGenerate = () =>
            {
                if(SqlMigrationRunner.MigrationDirectoryIsEmpty())
                {
                    Console.WriteLine("Your SQL Migrations Directory is empty.");

                    if (SafeConsole.Ask("Do you want to create the INITIAL SQL Migration instead?"))
                    {
                        SqlMigrationRunner.CreateInitialMigration();
                        SqlMigrationRunner.SqlMigrations();
                        return true;
                    }
                }
                else
                {
                    var hasInitial = SqlMigrationRunner.ReadMigrationsDirectory(silent: true).MinBy(a => a.Version)?.Comment.Contains(SqlMigrationRunner.InitialMigrationComment);

                    Console.WriteLine("You have an Initial SQL Migration.");

                    if (SafeConsole.Ask("Do you want to run the SQL Migrations instead?", "continue", "migrations") == "migrations")
                    {
                        SqlMigrationRunner.SqlMigrations();
                        return true;
                    }
                }

                return false;
            };
        }
    }

    public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        void Remove(DateTime? dateLimit, bool withExceptions)
        {
            if (dateLimit == null)
                return;

            var query = Database.Query<LoadMethodLogEntity>().Where(o => o.Start < dateLimit);

            if (withExceptions)
                query = query.Where(a => a.Exception != null);

            query.UnsafeDeleteChunksLog(parameters, sb, token);
        }

        Remove(parameters.GetDateLimitDelete(typeof(LoadMethodLogEntity).ToTypeEntity()), withExceptions: false);
        Remove(parameters.GetDateLimitDeleteWithExceptions(typeof(LoadMethodLogEntity).ToTypeEntity()), withExceptions: true);
    }

    public static void EnsureMigrationTable<T>() where T : Entity
    {
        using (var tr = new Transaction())
        {
            if (!Administrator.ExistsTable<T>())
            {
                var table = Schema.Current.Table<T>();
                var sqlBuilder = Connector.Current.SqlBuilder;

                if (!table.Name.Schema.IsDefault() && !Administrator.ExistSchema(table.Name.Schema))
                    sqlBuilder.CreateSchema(table.Name.Schema).ExecuteLeaves();

                sqlBuilder.CreateTableSql(table).ExecuteLeaves();

                foreach (var i in table.GeneratAllIndexes().Where(i => !(i is PrimaryKeyIndex)))
                {
                    sqlBuilder.CreateIndex(i, checkUnique: null).ExecuteLeaves();
                }

                SafeConsole.WriteLineColor(ConsoleColor.White, "Table " + table.Name + " auto-generated...");
            }

            tr.Commit();
        }
    }

    public static Exception? ExecuteLoadProcess(Action action, string description)
    {
        string showDescription = description ?? action.Method.Name.SpacePascal(true);
        Console.WriteLine("------- Executing {0} ".FormatWith(showDescription).PadRight(Console.WindowWidth - 2, '-'));

        var log = !Schema.Current.Tables.ContainsKey(typeof(LoadMethodLogEntity)) ? null : new LoadMethodLogEntity
        {
            Start = Clock.Now,
            ClassName = action.Method.DeclaringType!.FullName,
            MethodName = action.Method.Name,
            Description = description,
        }.Save();

        try
        {
            action();
            if (log != null)
            {
                log.End = Clock.Now;
                log.Save();
                Console.WriteLine("------- Executed {0} (took {1})".FormatWith(showDescription, (log.End.Value - log.Start).NiceToString()).PadRight(Console.WindowWidth - 2, '-'));
            }

            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine();

            SafeConsole.WriteColor(ConsoleColor.Red, e.GetType() + ": ");
            SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.Message);
            SafeConsole.WriteSameLineColor(ConsoleColor.DarkRed, e.StackTrace);
            if (log != null)
            {
                var exLog = e.LogException();
                log.Exception = exLog.ToLite();
                log.End = Clock.Now;
                log.Save();
            }

            return e;
        }
    }
}
