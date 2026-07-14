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


    public static Func<string> MigrationsDirectory = ()=> SqlMigrationRunner.MigrationsDirectory;

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

        SqlMigrationRunner.AfterMigrationsCompleted += TokenMigrationRunner.TokenMigrations;
        SqlMigrationRunner.AfterCreatingMigration += AfterMigrationCreated;
        Administrator.AfterSynchronize += TokenMigrationRunner.AfterSynchronize;

        IsStarted = true;
    }

    /// <summary>
    /// Drains <see cref="PendingQueryRenames"/> into a versioned .query.json (a
    /// <see cref="TokenMigrationFile"/> populated with <see cref="TokenMigrationFile.Types"/> only).
    /// Returns true if anything was written.
    /// </summary>
    public static void AfterMigrationCreated(string fullFileName, Replacements rep)
    {
        var file = new TokenMigrationFile();
        file.LoadTypes(rep);

        if (file.IsEmpty)
            return;

        var fullPath = Path.GetFileNameWithoutExtension(fullFileName) + QueryFileExtension;
        file.Save(fullPath);
        return;
    }

    internal static void FireTokenSynchronizing(TokenSyncContext ctx)
    {
        foreach (var e in TokenSynchronizing.GetInvocationListTyped())
        {
            SafeConsole.WriteColor(ConsoleColor.White, e.Method.DeclaringType!.TypeName());
            Console.Write(".");
            SafeConsole.WriteColor(ConsoleColor.DarkGray, e.Method.MethodName());
            e(ctx);
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Lists every migration file in the directory (both .tokens.json and .query.json), sorted by
    /// version. <see cref="MigrationInfo.Kind"/> distinguishes them.
    /// </summary>
    public static List<MigrationInfo> ReadMigrationsDirectory(bool silent = false)
    {
        var migationDirectory = MigrationsDirectory();

        if (!Directory.Exists(migationDirectory))
        {
            if (!silent)
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "Migrations directory does not exist: " + migationDirectory);

            return new List<MigrationInfo>();
        }

        var infos = new List<MigrationInfo>();
        foreach (var f in Directory.EnumerateFiles(migationDirectory))
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

        public override string ToString() => Version + " (" + Kind + ")";
    }
}
