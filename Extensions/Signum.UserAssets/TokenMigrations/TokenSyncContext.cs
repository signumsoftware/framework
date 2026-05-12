namespace Signum.UserAssets.TokenMigrations;

public enum TokenSyncMode
{
    /// <summary>
    /// Subscribers walk entities consulting <see cref="TokenSyncContext.History"/> for resolution
    /// and append any new decisions into <see cref="TokenSyncContext.Recording"/>. Entities are NOT
    /// saved. At end of run the recording is serialized to a .tokens.json file.
    /// </summary>
    Record,

    /// <summary>
    /// Pre-recorded decisions in <see cref="TokenSyncContext.History"/> are replayed against entities.
    /// FixToken never prompts. Entities are saved per-entity in their own transaction.
    /// </summary>
    Apply,
}

/// <summary>
/// Per-event context handed to <see cref="TokenMigrationLogic.TokenSynchronizing"/> subscribers.
/// Carries the ordered list of historical migration files to walk for resolution and (in Record mode)
/// the in-progress file new decisions get appended into.
/// </summary>
public class TokenSyncContext
{
    public TokenSyncMode Mode { get; }

    /// <summary>
    /// Ordered, read-only history. In Record mode these are all committed .tokens.json files; in Apply
    /// mode they are the pending (not-yet-applied) files. FixToken walks them in order so a chain
    /// V1: Name→Nombre + V2: Nombre→FullName resolves correctly even when both apply in one batch.
    /// </summary>
    public TokenMigrationFile[] History { get; }

    /// <summary>
    /// Mutable in-progress file. Non-null in Record mode (FixToken appends user decisions here);
    /// null in Apply mode (no prompting, so no new decisions to capture).
    /// </summary>
    public TokenMigrationFile? Recording { get; }

    /// <summary>
    /// Per-entity log of which entities were touched and what changed. Used by the runner to print
    /// an end-of-run summary in the same style as the existing sync log.
    /// </summary>
    public List<TokenSyncEntityReport> Reports { get; } = new();

    public TokenSyncContext(TokenSyncMode mode, TokenMigrationFile[] history, TokenMigrationFile? recording)
    {
        Mode = mode;
        History = history;
        Recording = recording;
    }

    /// <summary>
    /// Pre-recorded actions to honor in Apply mode (Skip/Delete/Regenerate decisions captured in
    /// earlier sessions). Computed lazily by collapsing all history files.
    /// </summary>
    public bool IsKnownAction(IUserAssetEntity entity, out UserAssetEntityActionType action)
    {
        var typeName = entity.GetType().Name;
        foreach (var file in History)
        {
            var match = file.UserAssetActions.FirstOrDefault(a => a.EntityType == typeName && a.Guid == entity.Guid);
            if (match != null)
            {
                action = match.Action;
                return true;
            }
        }

        action = default;
        return false;
    }

    /// <summary>
    /// Appends a Skip/Delete/Regenerate decision into the current recording. Only valid in Record mode.
    /// </summary>
    public void AddUserAssetAction(IUserAssetEntity entity, UserAssetEntityActionType action)
    {
        if (Recording == null)
            throw new InvalidOperationException("AddUserAssetAction is only valid in Record mode.");

        Recording.UserAssetActions.Add(new UserAssetEntityAction
        {
            EntityType = entity.GetType().Name,
            Guid = entity.Guid,
            Action = action,
        });
    }

    /// <summary>
    /// Free-form change log entry (token rename, FileName rewrite, message rendered…). Rendered in
    /// neutral gray by the runner.
    /// </summary>
    public void LogEntityChange(Entity entity, params string[] changes)
        => Reports.Add(new TokenSyncEntityReport(entity, changes.ToList(), action: null, error: null));

    /// <summary>
    /// Action-typed log entry (Skip / Delete / Regenerate). Rendered with a colour matching the
    /// action so an operator scanning the log can immediately spot destructive operations.
    /// </summary>
    public void LogEntityChange(Entity entity, UserAssetEntityActionType action)
        => Reports.Add(new TokenSyncEntityReport(entity, new List<string>(), action, error: null));

    public void LogEntityError(Entity entity, Exception error)
        => Reports.Add(new TokenSyncEntityReport(entity, new List<string>(), action: null, error));

    /// <summary>
    /// Walks <see cref="History"/> file by file, chaining renames at the given bucket+subKey.
    /// V1: A→B + V2: B→C resolves to C in a single pass without precomputing a flat lookup.
    /// When <paramref name="subKey"/> is non-null, each file's lookup uses the subKey's name at
    /// that file's era (derived from later files' <see cref="TokenMigrationFile.Types"/>); this is
    /// what lets V1.<c>Members["Foo.OldType"]</c> still match when the live type is now
    /// <c>"Foo.NewType"</c> renamed via a later V2.<c>Types</c>.
    /// Returns the chained result if it ends up in <paramref name="newValues"/>; otherwise falls
    /// through to <see cref="Signum.Engine.Sync.Replacements.GlobalAutoReplacement"/>, then the
    /// interactive prompt (Record mode only). Any new decision is persisted into
    /// <see cref="Recording"/>'s matching bucket. Apply mode (Recording == null) throws on miss.
    /// </summary>
    public string? AskRename(RenameBucket bucket, string? subKey, string oldValue, ICollection<string> newValues, Signum.Utilities.StringDistance sd)
    {
        if (newValues.Contains(oldValue))
            return oldValue;

        var eraSubKeys = subKey == null ? null : ComputeEraSubKeys(subKey);

        // Per-file chained walk: at each file, if the current working value has a rename, advance.
        string current = oldValue;
        for (int fi = 0; fi < History.Length; fi++)
        {
            var file = History[fi];
            var effectiveSubKey = eraSubKeys?[fi] ?? subKey;
            var d = file.TryGetDictionary(bucket, effectiveSubKey);
            if (d != null && d.TryGetValue(current, out var v))
                current = v;
        }

        if (current != oldValue && newValues.Contains(current))
            return current;

        // In-session memo from Recording (decisions made earlier in this same run).
        if (Recording != null)
        {
            var rd = Recording.TryGetDictionary(bucket, subKey);
            if (rd != null && rd.TryGetValue(oldValue, out var rv) && newValues.Contains(rv))
                return rv;
        }

        string replacementsKey = bucket + (subKey == null ? "" : ":" + subKey);

        if (Signum.Engine.Sync.Replacements.GlobalAutoReplacement != null)
        {
            var sel = Signum.Engine.Sync.Replacements.GlobalAutoReplacement(new Signum.Engine.Sync.Replacements.AutoReplacementContext(replacementsKey, oldValue, newValues.ToList()));
            if (sel != null && sel.Value.NewValue != null)
            {
                if (Recording != null)
                    Recording.GetOrCreateDictionary(bucket, subKey)[oldValue] = sel.Value.NewValue;
                return sel.Value.NewValue;
            }
        }

        if (Recording == null)
            throw new InvalidOperationException($"'{oldValue}' in '{replacementsKey}' has no recorded rename and Apply mode cannot prompt.");

        Console.WriteLine();
        Signum.Utilities.SafeConsole.WriteLineColor(ConsoleColor.White, "   '{0}' has been renamed in {1}?".FormatWith(oldValue, replacementsKey));

        var ordered = newValues.OrderBy(n => sd.LevenshteinDistance(oldValue, n)).ToList();
        int startingIndex = 0;

    retry:
        int max = Console.LargestWindowHeight - 11;
        ordered.Skip(startingIndex).Take(max)
            .Select((s, i) => "{0,2}: {1}".FormatWith(i + startingIndex, s)).ToConsole();
        Console.WriteLine();
        Signum.Utilities.SafeConsole.WriteLineColor(ConsoleColor.White, "- n: None");
        int rem = ordered.Count - startingIndex - max;
        if (rem > 0) Signum.Utilities.SafeConsole.WriteLineColor(ConsoleColor.White, "- +: Show more ({0} remaining)", rem);

        while (true)
        {
            string answer = Console.ReadLine()!;
            if (answer == null) throw new InvalidOperationException("Impossible to synchronize interactively without Console");
            answer = answer.ToLower();

            if (answer == "+" && rem > 0) { startingIndex += max; goto retry; }
            if (answer == "n") return null;
            if (int.TryParse(answer, out int idx))
            {
                var picked = ordered[idx];
                Recording.GetOrCreateDictionary(bucket, subKey)[oldValue] = picked;
                return picked;
            }
            Console.WriteLine("Error");
        }
    }

    /// <summary>
    /// For each file in <see cref="History"/>, computes what <paramref name="liveSubKey"/> was
    /// called at that file's era. Walks history newest→oldest, unwinding the subKey through each
    /// file's <see cref="TokenMigrationFile.Types"/> renames so older files can be looked up under
    /// their then-current name. Returns an array aligned with <see cref="History"/>.
    /// </summary>
    public string[] ComputeEraSubKeys(string liveSubKey)
    {
        var eras = new string[History.Length];
        string running = liveSubKey;
        for (int fi = History.Length - 1; fi >= 0; fi--)
        {
            eras[fi] = running;
            // Unwind: if this file's Types maps X → running, then files prior to this used X.
            foreach (var (oldT, newT) in History[fi].Types)
            {
                if (newT == running)
                {
                    running = oldT;
                    break;
                }
            }
        }
        return eras;
    }
}

public class TokenSyncEntityReport
{
    public Entity Entity { get; }
    public List<string> Changes { get; }
    public UserAssetEntityActionType? Action { get; }
    public Exception? Error { get; }

    public TokenSyncEntityReport(Entity entity, List<string> changes, UserAssetEntityActionType? action, Exception? error)
    {
        Entity = entity;
        Changes = changes;
        Action = action;
        Error = error;
    }
}
