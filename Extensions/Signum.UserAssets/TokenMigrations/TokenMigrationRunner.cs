using Signum.Engine.Sync;
using System.IO;

namespace Signum.UserAssets.TokenMigrations;

public static class TokenMigrationRunner
{
    /// <summary>
    /// Subscribed to <c>SqlMigrationRunner.AfterMigrationsCompleted</c> from app-level wiring. Fires
    /// whether or not the SQL runner actually applied anything — so token/query migrations get a
    /// chance to run even when the schema was already up to date.
    /// </summary>
    public static void AutoApplyAfterSqlMigrations(bool autoRun)
    {
        var infos = TokenMigrationLogic.ReadMigrationsDirectory(silent: true);
        SetExecuted(infos);

        var pending = infos.Where(a => !a.IsExecuted && a.FileName != null).ToList();
        if (pending.Count == 0)
            return;

        Draw(pending);

        if (!autoRun && !SafeConsole.Ask("Apply {0} token/query migration(s)?".FormatWith(pending.Count)))
            return;

        ApplyPending(pending);
    }

    /// <summary>
    /// Subscribed to <c>Administrator.AfterSynchronize</c>. Drives Record mode so devs see the
    /// token-rename prompts in the same console session as their schema sync.
    /// </summary>
    public static void AutoRecordAfterSynchronize()
    {
        if (!SafeConsole.Ask("Record token migrations now?"))
            return;

        RecordNew();
    }

    public static void TokenMigrations()
        => TokenMigrations(autoRun: false);

    public static void TokenMigrations(bool autoRun)
    {
        while (true)
        {
            var infos = TokenMigrationLogic.ReadMigrationsDirectory();
            SetExecuted(infos);

            Draw(infos);

            var pending = infos.Where(a => !a.IsExecuted && a.FileName != null).ToList();
            if (pending.Any())
            {
                if (!autoRun && !SafeConsole.Ask("Apply {0} token migration(s)?".FormatWith(pending.Count)))
                    return;

                ApplyPending(pending);

                if (autoRun)
                    return;
            }
            else
            {
                if (autoRun)
                    return;

                if (!SafeConsole.Ask("All token migrations are applied. Record a new one?"))
                    return;

                RecordNew();
            }
        }
    }

    static void SetExecuted(List<TokenMigrationLogic.MigrationInfo> infos)
    {
        if (!Connector.Current.HasTables())
            return;

        TokenMigrationLogic.EnsureTokenMigrationTable();

        var executed = Database.Query<TokenMigrationEntity>()
            .Select(m => new { m.VersionNumber, m.Comment })
            .OrderBy(a => a.VersionNumber)
            .ToList();

        var byVersion = infos.ToDictionary(a => a.Version);

        foreach (var e in executed)
        {
            var info = byVersion.TryGetC(e.VersionNumber!);
            if (info != null)
                info.IsExecuted = true;
            else
                infos.Add(new TokenMigrationLogic.MigrationInfo
                {
                    FileName = null,
                    Comment = ">> In Database Only << " + e.Comment,
                    IsExecuted = true,
                    Version = e.VersionNumber!,
                    Kind = TokenMigrationLogic.MigrationKind.Tokens,
                });
        }

        infos.Sort(a => a.Version);
    }

    /// <summary>
    /// Applies all pending migration files in version order, batched into a single
    /// <c>TokenSynchronizing</c> fire. Both .tokens.json (full rename data + entity actions) and
    /// .query.json (Types only) contribute to the History pool. The chained walk inside FixToken
    /// / FixValue / AskRename traverses them per-file and consults each file's era-name (computed
    /// from later files' Types) when looking up TokensColumn / TokensType / FilterValues — so
    /// V1.Tokens recorded under the pre-rename query key still matches when the live key is the
    /// post-rename name from V2.Types, without ever mutating the loaded files.
    /// </summary>
    static void ApplyPending(List<TokenMigrationLogic.MigrationInfo> pending)
    {
        var loaded = pending.Select(p => TokenMigrationFile.Load(p.FileName!)).ToArray();

        var ctx = new TokenSyncContext(TokenSyncMode.Apply, loaded, recording: null);

        try
        {
            QueryLogic.AssertLoaded();
            TypeLogic.AssertLoaded();

            TokenMigrationLogic.FireTokenSynchronizing(ctx);
        }
        finally
        {
            PrintReport(ctx);
        }

        TokenMigrationLogic.EnsureTokenMigrationTable();
        using (var tr = Transaction.ForceNew())
        {
            foreach (var p in pending)
            {
                new TokenMigrationEntity
                {
                    VersionNumber = p.Version,
                    Comment = p.Comment,
                }.Save();
                p.IsExecuted = true;
            }
            tr.Commit();
        }
    }

    static void RecordNew()
    {
        // History = every committed migration file (both .tokens.json and .query.json), regardless
        // of whether applied to the dev DB. Files are loaded as-is; era-aware subKey resolution
        // inside FixToken / FixValue / AskRename takes care of any earlier files whose outer keys
        // pre-date later Types renames.
        var allCommitted = TokenMigrationLogic.ReadMigrationsDirectory(silent: true);
        var loaded = allCommitted
            .Where(p => p.FileName != null)
            .Select(p => TokenMigrationFile.Load(p.FileName!))
            .ToArray();

        var recording = new TokenMigrationFile();

        var ctx = new TokenSyncContext(TokenSyncMode.Record, loaded, recording);

        QueryLogic.AssertLoaded();
        TypeLogic.AssertLoaded();

        TokenMigrationLogic.FireTokenSynchronizing(ctx);

        PrintReport(ctx);

        var version = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");
        var comment = SafeConsole.AskString("Comment for the new token migration? ", stringValidator: s => null).Trim();

        if (!recording.IsEmpty)
        {
            var fileName = version + (comment.HasText() ? "_" + FileNameValidatorAttribute.RemoveInvalidCharts(comment) : null) + TokenMigrationLogic.TokensFileExtension;

            if (!Directory.Exists(TokenMigrationLogic.MigrationsDirectory))
                Directory.CreateDirectory(TokenMigrationLogic.MigrationsDirectory);

            var fullPath = Path.Combine(TokenMigrationLogic.MigrationsDirectory, fileName);
            recording.Save(fullPath);

            var tokenRenameCount = recording.TokensColumn.Sum(kvp => kvp.Value.Count) + recording.TokensType.Sum(kvp => kvp.Value.Count);
            var filterValueCount = recording.FilterValues.Sum(kvp => kvp.Value.Count);
            var memberCount = recording.Members.Sum(kvp => kvp.Value.Count);

            SafeConsole.WriteLineColor(ConsoleColor.Green, "Token migration written: " + fullPath);
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  Token renames: " + tokenRenameCount);
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  Filter value renames: " + filterValueCount);
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  Type renames: " + recording.Types.Count);
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  Member renames: " + memberCount);
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  Global renames: " + recording.Globals.Count);
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "  Entity actions: " + recording.UserAssetActions.Count);
        }
        else
        {
            SafeConsole.WriteLineColor(ConsoleColor.Green, "No new token decisions to record.");
        }

        // Always flush pending query renames — having only a .query.json with no accompanying
        // .tokens.json is a valid outcome (useful for FixValue Lite type resolution later).
        TokenMigrationLogic.FlushPendingQueryRenames(version, comment);
    }

    static void PrintReport(TokenSyncContext ctx)
    {
        if (ctx.Reports.Count == 0)
            return;

        Console.WriteLine();
        foreach (var r in ctx.Reports)
        {
            if (r.Error != null)
            {
                SafeConsole.WriteLineColor(ConsoleColor.Red, $"{r.Entity.GetType().Name} {r.Entity}:");
                SafeConsole.WriteLineColor(ConsoleColor.DarkRed, "  " + r.Error.Message);
            }
            else if (r.Action != null)
            {
                var color = r.Action switch
                {
                    UserAssetEntityActionType.Skip => ConsoleColor.DarkYellow,
                    UserAssetEntityActionType.Delete => ConsoleColor.Red,
                    UserAssetEntityActionType.Regenerate => ConsoleColor.Magenta,
                    _ => ConsoleColor.Gray,
                };
                SafeConsole.WriteLineColor(color, $"{r.Entity.GetType().Name} {r.Entity}: {r.Action.ToString()!.ToLower()}");
            }
            else
            {
                SafeConsole.WriteLineColor(ConsoleColor.White, $"{r.Entity.GetType().Name} {r.Entity}:");
                foreach (var c in r.Changes)
                    SafeConsole.WriteLineColor(ConsoleColor.Gray, "  " + c);
            }
        }
        Console.WriteLine();
    }

    static void Draw(List<TokenMigrationLogic.MigrationInfo> infos)
    {
        Console.WriteLine();

        if (infos.Count == 0)
        {
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "No token/query migrations found.");
        }
        else
        {
            foreach (var mi in infos)
            {
                ConsoleColor color =
                    mi.FileName != null && mi.IsExecuted ? ConsoleColor.DarkGreen :
                    mi.FileName == null && mi.IsExecuted ? ConsoleColor.Red :
                    mi.FileName != null && !mi.IsExecuted ? ConsoleColor.White :
                    ConsoleColor.Gray;

                SafeConsole.WriteColor(color, mi.IsExecuted ? "- " : "  ");
                SafeConsole.WriteColor(color, mi.Version);
                SafeConsole.WriteColor(color, " [" + mi.Kind + "]");
                SafeConsole.WriteLineColor(mi.FileName == null ? ConsoleColor.Red : ConsoleColor.Gray, " " + mi.Comment);
            }
        }

        Console.WriteLine();
    }
}
