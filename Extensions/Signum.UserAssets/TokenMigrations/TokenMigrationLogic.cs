using Signum.Engine.Sync;
using System.IO;
using System.Text.RegularExpressions;
using Signum.Engine;
using Signum.Migrations;

namespace Signum.UserAssets.TokenMigrations;

public static class TokenMigrationLogic
{
    /// <summary>
    /// Subscribers walk their entities, consult ctx (history + recording) via FixToken / FixValue /
    /// AskRename, and either capture new decisions (Record mode, no Save) or replay them
    /// (Apply mode, per-entity transaction + Save). The runner fires this exactly once per session.
    /// </summary>
    public static event Action<TokenSyncContext>? TokenSynchronizing;

    /// <summary>
    /// Directory where versioned migration files live (.sql / .tokens.json / .query.json). Both
    /// token-migration extensions live alongside the .sql files so a developer browsing the folder
    /// sees the full migration history in chronological order.
    /// </summary>
    public static string MigrationsDirectory = Path.Combine("..", "..", "..", "Migrations");

    public const string TokensFileExtension = ".tokens.json";
    public const string QueryFileExtension = ".query.json";

    /// <summary>
    /// Single regex matching both .tokens.json and .query.json. The <c>kind</c> capture distinguishes
    /// them. <c>.tokens.json</c> files normally carry token/value/member/global renames and entity
    /// actions; <c>.query.json</c> files normally carry only <see cref="TokenMigrationFile.Types"/>
    /// entries (query renames captured during schema sync — since queryKey is essentially a type
    /// clean name, the Types bucket serves both purposes).
    /// </summary>
    static readonly Regex MigrationFileRegex = new(@"(?<version>\d{4}\.\d{2}\.\d{2}\-\d{2}\.\d{2}\.\d{2})(_(?<comment>.+))?\.(?<kind>tokens|query)\.json");

    /// <summary>
    /// Buffer of query renames captured live from <see cref="Signum.Basics.QueryLogic.QueryRenamed"/>
    /// during a schema sync. Drained when the dev promotes the sync to a versioned migration
    /// (via <see cref="OnSqlMigrationCreated"/>) or when a token migration is recorded without an
    /// accompanying .sql (via <see cref="TokenMigrationRunner.RecordNew"/>). Becomes the
    /// <see cref="TokenMigrationFile.Types"/> dict of the flushed .query.json file.
    /// </summary>
    static readonly Dictionary<string, string> PendingQueryRenames = new();

    public static bool IsStarted { get; private set; }

    public static void AssertStarted()
    {
        if (!IsStarted)
            throw new InvalidOperationException("TokenMigrationLogic is not started. Please call TokenMigrationLogic.Start in your application startup.");
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        PermissionLogic.RegisterPermissions(UserAssetPermission.UserAssetsToXML);

        sb.Include<TokenMigrationEntity>()
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.VersionNumber,
                e.Comment,
            });

        QueryLogic.QueryRenamed += OnQueryRenamedDuringSync;
        SqlMigrationRunner.AfterMigrationsCompleted += TokenMigrationRunner.AutoApplyAfterSqlMigrations;
        SqlMigrationRunner.AfterCreatingMigration += FlushPendingQueryRenames;
        Administrator.AfterSynchronize += TokenMigrationRunner.AutoRecordAfterSynchronize;

        IsStarted = true;
    }

    static void OnQueryRenamedDuringSync(string oldKey, string newKey)
    {
        // Chain into any existing buffered rename whose target equals oldKey (A→oldKey becomes A→newKey)
        foreach (var existingOld in PendingQueryRenames.Keys.ToList())
        {
            if (PendingQueryRenames[existingOld] == oldKey)
                PendingQueryRenames[existingOld] = newKey;
        }

        if (oldKey != newKey)
            PendingQueryRenames[oldKey] = newKey;
    }

    /// <summary>
    /// Drains <see cref="PendingQueryRenames"/> into a versioned .query.json (a
    /// <see cref="TokenMigrationFile"/> populated with <see cref="TokenMigrationFile.Types"/> only).
    /// Returns true if anything was written.
    /// </summary>
    public static void FlushPendingQueryRenames(string version, string comment)
    {
        if (PendingQueryRenames.Count == 0)
            return;

        var file = new TokenMigrationFile { Types = new Dictionary<string, string>(PendingQueryRenames) };
        PendingQueryRenames.Clear();

        var fileName = version + (comment.HasText() ? "_" + comment : null) + QueryFileExtension;

        if (!Directory.Exists(MigrationsDirectory))
            Directory.CreateDirectory(MigrationsDirectory);

        var fullPath = Path.Combine(MigrationsDirectory, fileName);
        file.Save(fullPath);

        SafeConsole.WriteLineColor(ConsoleColor.Green, "Query rename migration written: " + fullPath);
        SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  Type renames: " + file.Types.Count);
        return;
    }

    internal static void FireTokenSynchronizing(TokenSyncContext ctx)
    {
        TokenSynchronizing?.Invoke(ctx);
    }

    /// <summary>
    /// Lists every migration file in the directory (both .tokens.json and .query.json), sorted by
    /// version. <see cref="MigrationInfo.Kind"/> distinguishes them.
    /// </summary>
    public static List<MigrationInfo> ReadMigrationsDirectory(bool silent = false)
    {
        if (!Directory.Exists(MigrationsDirectory))
        {
            if (!silent)
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "Token migrations directory does not exist: " + MigrationsDirectory);
            return new List<MigrationInfo>();
        }

        var infos = new List<MigrationInfo>();
        foreach (var f in Directory.EnumerateFiles(MigrationsDirectory))
        {
            var name = Path.GetFileName(f);
            var match = MigrationFileRegex.Match(name);
            if (!match.Success) continue;

            infos.Add(new MigrationInfo
            {
                FileName = f,
                Version = match.Groups["version"].Value,
                Comment = match.Groups["comment"].Value,
                Kind = match.Groups["kind"].Value == "tokens" ? MigrationKind.Tokens : MigrationKind.Query,
            });
        }

        infos.Sort(a => a.Version);
        return infos;
    }

    public enum MigrationKind
    {
        Tokens,
        Query,
    }

    public class MigrationInfo
    {
        public required string? FileName;
        public required string Version;
        public required string Comment;
        public required MigrationKind Kind;
        public bool IsExecuted;

        public string FileExtension => Kind == MigrationKind.Tokens ? TokensFileExtension : QueryFileExtension;

        public override string ToString() => Version + " (" + Kind + ")";
    }

    /// <summary>
    /// Creates the TokenMigrationEntity table on demand if missing. Mirrors
    /// MigrationLogic.EnsureMigrationTable so this module stays decoupled from Signum.Migrations.
    /// </summary>
    internal static void EnsureTokenMigrationTable()
    {
        using var tr = new Transaction();

        if (!Administrator.ExistsTable<TokenMigrationEntity>())
        {
            var table = Schema.Current.Table<TokenMigrationEntity>();
            var sqlBuilder = Connector.Current.SqlBuilder;

            if (!table.Name.Schema.IsDefault() && !Administrator.ExistSchema(table.Name.Schema))
                sqlBuilder.CreateSchema(table.Name.Schema).ExecuteLeaves();

            sqlBuilder.CreateTableSql(table).ExecuteLeaves();

            foreach (var i in table.AllIndexes().Where(i => !i.PrimaryKey))
                sqlBuilder.CreateIndex(i, checkUnique: null).ExecuteLeaves();

            SafeConsole.WriteLineColor(ConsoleColor.White, "Table " + table.Name + " auto-generated...");
        }

        tr.Commit();
    }
}
