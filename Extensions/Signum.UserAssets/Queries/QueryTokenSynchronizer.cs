using Signum.DynamicQuery.Tokens;
using Signum.Engine.Sync;
using Signum.UserAssets.Queries;
using Signum.UserAssets.TokenMigrations;

namespace Signum.UserAssets.QueryTokens;

public static class DelayedConsole
{
    static List<Action> ToWrite = new List<Action>();
    public static IDisposable Delay(Action action)
    {
        ToWrite.Add(action);

        return new Disposable(() => ToWrite.Remove(action));
    }

    public static void Flush()
    {
        foreach (var item in ToWrite)
        {
            item();
        }

        ToWrite.Clear();
    }
}

/// <summary>
/// Token-rename resolver. All public entry points take a <see cref="TokenSyncContext"/>; the only
/// reference to <see cref="Replacements"/> in this file is the static
/// <see cref="Replacements.GlobalAutoReplacement"/> delegate (and its nested types), which stays
/// for the auto-fill fallback path.
/// </summary>
public static class QueryTokenSynchronizer
{
    // ---------- Public entry points ----------

    public static FixTokenResult FixToken(TokenSyncContext ctx, ref QueryTokenEmbedded token, QueryDescription qd, SubTokensOptions options, string? remainingText, bool allowRemoveToken, bool allowReCreate, bool forceChange = false)
    {
        var t = token;
        using (DelayedConsole.Delay(() => { SafeConsole.WriteColor(t.ParseException == null ? ConsoleColor.Gray : ConsoleColor.Red, "  " + t.TokenString); Console.WriteLine(" " + remainingText); }))
        {
            if (token.ParseException == null && !forceChange)
                return FixTokenResult.Nothing;

            DelayedConsole.Flush();
            var result = FixToken(ctx, token.TokenString, out QueryToken? resultToken, qd, options, remainingText, allowRemoveToken, allowReCreate, forceChange);

            if (result == FixTokenResult.Fix)
                token = new QueryTokenEmbedded(resultToken!);

            return result;
        }
    }

    public static FixTokenResult FixToken(TokenSyncContext ctx, string original, out QueryToken? token, QueryDescription qd, SubTokensOptions options, string? remainingText, bool allowRemoveToken, bool allowReGenerate, bool forceChange = false)
    {
        if (TryParseRememberToken(ctx, original, qd, options, out QueryToken? current))
        {
            if (current!.FullKey() != original)
            {
                SafeConsole.WriteColor(ConsoleColor.DarkRed, "  " + original);
                Console.Write(" -> ");
                SafeConsole.WriteColor(ConsoleColor.DarkGreen, current.FullKey());
            }
            Console.WriteLine(remainingText);
            token = current;
            if (!forceChange)
                return FixTokenResult.Fix;
        }

        if (ctx.Recording == null)
        {
            // Apply mode: no prompting. The caller (subscriber) will see a SkipEntity-equivalent
            // outcome via the throw and report the failure.
            token = null;
            throw new InvalidOperationException($"Cannot resolve token '{original}' against current schema. Apply mode does not allow interactive prompts.");
        }

        while (true)
        {
            var tempToken = current!;
            var result = SelectInteractive(ref tempToken, qd, options, remainingText, allowRemoveToken, allowReGenerate);
            current = tempToken;
            switch (result)
            {
                case UserAssetTokenAction.DeleteEntity:
                    SafeConsole.WriteLineColor(ConsoleColor.Red, "Entity deleted");
                    token = null;
                    return FixTokenResult.DeleteEntity;
                case UserAssetTokenAction.ReGenerateEntity:
                    if (!allowReGenerate)
                        throw new InvalidOperationException("Unexpected Regenerate");

                    SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Entity Regenerated");
                    token = null;
                    return FixTokenResult.RegenerateEntity;
                case UserAssetTokenAction.RemoveToken:
                    if (!allowRemoveToken)
                        throw new InvalidOperationException("Unexpected RemoveToken");

                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    SafeConsole.WriteColor(ConsoleColor.DarkRed, "  " + original);
                    SafeConsole.WriteColor(ConsoleColor.DarkRed, " (token removed)");
                    Console.WriteLine(remainingText);
                    token = null;
                    return FixTokenResult.RemoveToken;
                case UserAssetTokenAction.SkipEntity:
                    SafeConsole.WriteLineColor(ConsoleColor.DarkYellow, "Entity skipped");
                    token = null;
                    return FixTokenResult.SkipEntity;
                case UserAssetTokenAction.Confirm:
                    Remember(ctx.Recording, original, current, qd, options);
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    SafeConsole.WriteColor(ConsoleColor.DarkRed, "  " + original);
                    Console.Write(" -> ");
                    SafeConsole.WriteColor(ConsoleColor.DarkGreen, current.FullKey());
                    Console.WriteLine(remainingText);
                    token = current;
                    return FixTokenResult.Fix;
            }
        }
    }

    public static FixTokenResult FixValue(TokenSyncContext ctx, string queryKey, string tokenString, Type targetType, ref string? valueString, bool allowRemoveToken, bool isList, bool fixInstead, Type? currentEntityType)
    {
        var res = FilterValueConverter.IsValidExpression(valueString, targetType, isList, currentEntityType);
        if (res is Result<Type>.Success)
            return FixTokenResult.Nothing;

        DelayedConsole.Flush();

        // List values: split on '|', recurse per item, recompose.
        if (isList && valueString!.Contains('|'))
        {
            var changes = new List<string?>();
            foreach (var str in valueString.Split('|'))
            {
                string? s = str;
                var inner = FixValue(ctx, queryKey, tokenString, targetType, ref s, allowRemoveToken, isList: false, fixInstead, currentEntityType);

                if (inner == FixTokenResult.DeleteEntity ||
                    inner == FixTokenResult.SkipEntity ||
                    inner == FixTokenResult.RemoveToken ||
                    inner == FixTokenResult.FixTokenInstead ||
                    inner == FixTokenResult.FixOperationInstead)
                    return inner;

                changes.Add(s);
            }

            valueString = changes.ToString("|");
            return FixTokenResult.Fix;
        }

        // Look up a previously-recorded (queryKey, tokenString, oldValue) → newValue mapping by
        // walking history file by file at the FilterValue bucket, chaining as we go so V1: A→B +
        // V2: B→C lands at C. The queryKey portion of the subKey is unwound to its era name per
        // file (via Types renames in later files), so older FilterValues entries stored under the
        // pre-rename queryKey still match.
        var eraQueryKeys = ctx.ComputeEraSubKeys(queryKey);
        string current = valueString ?? "";
        bool advanced = false;
        for (int fi = 0; fi < ctx.History.Length; fi++)
        {
            var eraSubKey = TokenMigrationFile.FilterValueSubKey(eraQueryKeys[fi], tokenString);
            var d = ctx.History[fi].TryGetDictionary(RenameBucket.FilterValue, eraSubKey);
            if (d != null && d.TryGetValue(current, out var v))
            {
                current = v;
                advanced = true;
            }
        }
        if (advanced)
        {
            valueString = current;
            return FixTokenResult.Fix;
        }

        // Lite: rewrite the type segment ("Foo;42") when the type was renamed.
        if (targetType.IsLite())
        {
            var m = Lite.ParseRegex.Match(valueString!);
            if (m.Success)
            {
                var typeString = m.Groups["type"].Value;

                if (!TypeLogic.NameToType.ContainsKey(typeString))
                {
                    string? newTypeString = AskTypeReplacement(ctx, typeString);

                    if (newTypeString.HasText())
                    {
                        valueString = valueString!.Replace(typeString, newTypeString);
                        return FixTokenResult.Fix;
                    }
                }
            }
        }

        // GlobalAutoReplacement fallback (the only Replacements.* reference allowed here).
        if (Replacements.GlobalAutoReplacement != null)
        {
            var sel = Replacements.GlobalAutoReplacement(new Replacements.AutoReplacementContext(replacementKey: "FixValue", oldValue: valueString!, newValues: null));

            if (sel != null && sel.Value.NewValue != null)
            {
                var oldVal = valueString ?? "";
                valueString = sel.Value.NewValue;
                if (ctx.Recording != null && oldVal != valueString)
                    ctx.Recording.GetOrCreateDictionary(RenameBucket.FilterValue, TokenMigrationFile.FilterValueSubKey(queryKey, tokenString))[oldVal] = valueString ?? "";
                return FixTokenResult.Fix;
            }
        }

        if (ctx.Recording == null)
            throw new InvalidOperationException($"Cannot fix value '{valueString}' for {targetType.TypeName()} in Apply mode (no interactive prompt allowed).");

        SafeConsole.WriteLineColor(ConsoleColor.White, "Value '{0}' not convertible to {1}.".FormatWith(valueString, targetType.TypeName()));
        SafeConsole.WriteLineColor(ConsoleColor.Yellow, "- s: Skip entity");
        if (allowRemoveToken)
            SafeConsole.WriteLineColor(ConsoleColor.DarkRed, "- r: Remove token");
        SafeConsole.WriteLineColor(ConsoleColor.Red, "- d: Delete entity");
        if (fixInstead)
        {
            SafeConsole.WriteLineColor(ConsoleColor.Blue, "- t: Fix Token Instead");
            SafeConsole.WriteLineColor(ConsoleColor.Cyan, "- o: Fix Operation Instead");
        }
        SafeConsole.WriteLineColor(ConsoleColor.Green, "- freeText: New value");

        string answer = Console.ReadLine()!;
        if (answer == null)
            throw new InvalidOperationException("Impossible to synchronize interactively without Console");

        string a = answer.ToLower();
        if (a == "s") return FixTokenResult.SkipEntity;
        if (allowRemoveToken && a == "r") return FixTokenResult.RemoveToken;
        if (fixInstead)
        {
            if (a == "t") return FixTokenResult.FixTokenInstead;
            if (a == "o") return FixTokenResult.FixOperationInstead;
        }
        if (a == "d") return FixTokenResult.DeleteEntity;

        var originalValue = valueString ?? "";
        valueString = answer;
        ctx.Recording.GetOrCreateDictionary(RenameBucket.FilterValue, TokenMigrationFile.FilterValueSubKey(queryKey, tokenString))[originalValue] = valueString;
        return FixTokenResult.Fix;
    }

    // ---------- Internal resolution (token-migration-native) ----------

    /// <summary>
    /// Walks parts of <paramref name="tokenString"/> against the live schema, falling back to the
    /// appropriate rename bucket on miss. Root position (<c>result == null</c>) consults
    /// <see cref="RenameBucket.TokensColumn"/> keyed by query; inner positions consult
    /// <see cref="RenameBucket.TokensType"/> keyed by the current type's FullName. Each lookup is a
    /// fresh chain-composed dict so V1: A→B + V2: B→C resolves to A→C in one hop.
    /// </summary>
    static bool TryParseRememberToken(TokenSyncContext ctx, string tokenString, QueryDescription qd, SubTokensOptions options, out QueryToken? result)
    {
        string[] parts = QueryUtils.SplitRegex.Split(tokenString);

        result = null;
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];

            QueryToken? newResult = QueryUtils.SubToken(result, qd, options, part);
            if (newResult != null)
            {
                result = newResult;
                continue;
            }

            if (i == 0)
            {
                var entity = QueryUtils.SubToken(result, qd, options, "Entity");
                QueryToken? newSubResult = QueryUtils.SubToken(entity, qd, options, part);
                if (newSubResult != null)
                {
                    result = newSubResult;
                    continue;
                }
            }

            if (Replacements.GlobalAutoReplacement != null)
            {
                var sel = Replacements.GlobalAutoReplacement(new Replacements.AutoReplacementContext(
                    replacementKey: "QueryToken",
                    oldValue: part,
                    newValues: result.SubTokens(qd, options).Select(a => a.Key).ToList()));

                if (sel != null && sel.Value.NewValue != null)
                {
                    newResult = QueryUtils.SubToken(result, qd, options, sel.Value.NewValue);
                    if (newResult != null)
                    {
                        result = newResult;
                        continue;
                    }
                }
            }

            // Per-file chained walk: each file gets a chance to rewrite the remaining-path prefix.
            // V1: "Name" → "Nombre" + V2: "Nombre" → "FullName" both apply in sequence so a stale
            // token with "Name" ends up at "FullName" against the live schema. The subKey at each
            // file's era is also tracked (via Types renames in newer files) so an older
            // TokensColumn["Rechnung"] still matches when the live key is "Invoice".
            string remaining = parts.Skip(i).ToString(".");
            string originalRemaining = remaining;
            int consumedOriginalParts = 0;

            string liveSubKey = result == null
                ? QueryUtils.GetKey(qd.QueryName)
                : CleanTypeName(result.Type);
            var eraSubKeys = ctx.ComputeEraSubKeys(liveSubKey);

            for (int fi = 0; fi < ctx.History.Length; fi++)
            {
                var file = ctx.History[fi];
                var dic = result == null
                    ? file.TokensColumn.TryGetC(eraSubKeys[fi])
                    : file.TokensType.TryGetC(eraSubKeys[fi]);

                if (dic == null)
                    continue;

                string? old = dic.Keys.OrderByDescending(a => a.Length).FirstOrDefault(s => remaining == s || remaining.StartsWith(s + "."));
                if (old == null)
                    continue;

                int oldPartsCount = old.Length == 0 ? 0 : QueryUtils.SplitRegex.Split(old).Length;
                // Track how many of the ORIGINAL parts at position i this collectively consumes —
                // only the first file's match counts toward consumption; subsequent files rewrite
                // the prefix in place (B → C operates on the result of A → B, not on extra parts).
                if (consumedOriginalParts == 0)
                    consumedOriginalParts = oldPartsCount;

                var newKey = dic[old];
                if (remaining == old)
                    remaining = newKey;
                else
                    remaining = newKey.HasText()
                        ? newKey + remaining.Substring(old.Length)
                        : remaining.Substring(old.Length + 1);
            }

            if (remaining == originalRemaining)
                return false;

            var subParts = remaining.HasText() ? QueryUtils.SplitRegex.Split(remaining) : Array.Empty<string>();

            for (int j = 0; j < subParts.Length; j++)
            {
                QueryToken? subNewResult = QueryUtils.SubToken(result, qd, options, subParts[j]);
                if (subNewResult == null)
                    return false;
                result = subNewResult;
            }

            i += (consumedOriginalParts == 0 ? 0 : consumedOriginalParts) - 1;
        }

        return true;
    }

    private static string CleanTypeName(Type type)
    {
        var t = type.CleanType();
        return TypeLogic.TypeToEntity.TryGetC(t)?.CleanName ?? t.Name;
    }

    /// <summary>
    /// Persists a user-confirmed rename into <paramref name="recording"/>. Routes to
    /// <see cref="RenameBucket.TokensColumn"/> when the rename starts at the query root, or
    /// <see cref="RenameBucket.TokensType"/> when it's at a sub-path within a type.
    /// </summary>
    static void Remember(TokenMigrationFile recording, string oldTokenString, QueryToken newToken, QueryDescription qd, SubTokensOptions options)
    {
        var tokenList = newToken.Follow(a => a.Parent).Reverse().ToList();

        var oldParts = QueryUtils.SplitRegex.Split(oldTokenString);
        var newParts = QueryUtils.SplitRegex.Split(newToken.FullKey());

        var oldPartsList = oldParts.ToList();
        var newPartsList = newParts.ToList();

        Func<string, string?> rep = str =>
        {
            if (Replacements.GlobalAutoReplacement == null)
                return null;

            var sel = Replacements.GlobalAutoReplacement(new Replacements.AutoReplacementContext("QueryToken", oldValue: str, newValues: null));
            if (sel == null || sel.Value.NewValue == null)
                return null;

            return sel.Value.NewValue;
        };

        int pos = -1;
        while (oldPartsList.Count > 0 && newPartsList.Count > 0 &&
            (oldPartsList[0] == newPartsList[0] || rep(oldPartsList[0]) == newPartsList[0]))
        {
            oldPartsList.RemoveAt(0);
            newPartsList.RemoveAt(0);
            pos++;
        }

        while (oldPartsList.Count > 0 && newPartsList.Count > 0 &&
            (oldPartsList[oldPartsList.Count - 1] == newPartsList[newPartsList.Count - 1] ||
             rep(oldPartsList[oldPartsList.Count - 1]) == newPartsList[newPartsList.Count - 1]))
        {
            oldPartsList.RemoveAt(oldPartsList.Count - 1);
            newPartsList.RemoveAt(newPartsList.Count - 1);
        }

        var recTokenDic = pos == -1 ?
            recording.TokensColumn.GetOrCreate(QueryUtils.GetKey(tokenList[0].QueryName)) :
            recording.TokensType.GetOrCreate(CleanTypeName(tokenList[pos].Type));

        recTokenDic[oldPartsList.ToString(".")] = newPartsList.ToString(".");
    }

    /// <summary>
    /// Resolves a Lite type rename. <see cref="RenameBucket.Types"/> covers both type renames
    /// captured during Record AND query renames captured during schema sync (.query.json files) —
    /// since queryKey is essentially a Type clean name, the same dict serves both, so this is just
    /// a straight <see cref="TokenSyncContext.AskRename"/> call.
    /// </summary>
    static string? AskTypeReplacement(TokenSyncContext ctx, string oldTypeName)
        => ctx.AskRename(RenameBucket.Types, subKey: null, oldTypeName, TypeLogic.NameToType.Keys, new StringDistance());

    // ---------- Interactive token picker (unchanged behaviour) ----------

    static UserAssetTokenAction? SelectInteractive(ref QueryToken token, QueryDescription qd, SubTokensOptions options, string? remainingText, bool allowRemoveToken, bool allowReGenerate)
    {
        var top = Console.CursorTop;

        try
        {
            if (Console.Out == null)
                throw new InvalidOperationException("Unable to ask for renames to synchronize query tokens without interactive Console. Please use your Terminal application.");

            var subTokens = token.SubTokens(qd, options).OrderBy(a => a.Parent != null).ThenByDescending(a => a.Priority).ThenBy(a => a.Key).ToList();

            int startingIndex = 0;

            SafeConsole.WriteColor(ConsoleColor.Cyan, "  " + token?.FullKey());
            if (remainingText.HasText())
                Console.Write(" " + remainingText);
            Console.WriteLine();

            bool isRoot = token == null;

        retry:
            int maxElements = Console.LargestWindowHeight - 11;

            subTokens.Skip(startingIndex).Take(maxElements)
                .Select((s, i) => "- {1,2}: {2} ".FormatWith(i + " ", i + startingIndex, (isRoot && s.Parent != null ? "-" : "") + s.Key)).ToConsole();
            Console.WriteLine();

            int remaining = subTokens.Count - startingIndex - maxElements;
            if (remaining > 0)
                SafeConsole.WriteLineColor(ConsoleColor.White, "- +: Show more values ({0} remaining)", remaining);

            if (token != null)
            {
                SafeConsole.WriteLineColor(ConsoleColor.White, "- b: Back");
                SafeConsole.WriteLineColor(ConsoleColor.Green, "- c: Confirm");
            }

            SafeConsole.WriteLineColor(ConsoleColor.Yellow, "- s: Skip entity");

            if (allowRemoveToken)
                SafeConsole.WriteLineColor(ConsoleColor.DarkRed, "- r: Remove token");

            if (allowReGenerate)
                SafeConsole.WriteLineColor(ConsoleColor.Magenta, "- g: Generate from default template");

            SafeConsole.WriteLineColor(ConsoleColor.Red, "- d: Delete entity");
            SafeConsole.WriteLineColor(ConsoleColor.White, "- freeText: New Full Token");

            while (true)
            {
                string rawAnswer = Console.ReadLine()!;

                if (rawAnswer == null)
                    throw new InvalidOperationException("Impossible to synchronize interactively without Console");

                string answer = rawAnswer.ToLower();

                if (answer == "+" && remaining > 0)
                {
                    startingIndex += maxElements;
                    goto retry;
                }

                if (answer == "s") return UserAssetTokenAction.SkipEntity;
                if (answer == "r" && allowRemoveToken) return UserAssetTokenAction.RemoveToken;
                if (answer == "d") return UserAssetTokenAction.DeleteEntity;
                if (answer == "g") return UserAssetTokenAction.ReGenerateEntity;

                if (token != null)
                {
                    if (answer == "c")
                        return UserAssetTokenAction.Confirm;

                    if (answer == "b")
                    {
                        token = token.Parent!;
                        return null;
                    }
                }

                if (int.TryParse(answer, out int option))
                {
                    token = subTokens[option];
                    return null;
                }

                var tryToken = QueryUtils.TryParse(rawAnswer, qd, options, out var _, out var _);
                if (tryToken != null)
                {
                    token = tryToken;
                    return UserAssetTokenAction.Confirm;
                }

                Console.WriteLine("Input Error");
            }
        }
        finally
        {
            Clean(top, Console.CursorTop);
        }
    }

    static void Clean(int top, int nextTop)
    {
        ConsoleColor colorBefore = Console.BackgroundColor;
        try
        {
            for (int i = nextTop - 1; i >= top; i--)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                string spaces = new string(' ', Console.BufferWidth);
                Console.SetCursorPosition(0, i);
                Console.Write(spaces);
            }

            Console.SetCursorPosition(0, top);
        }
        finally
        {
            Console.BackgroundColor = colorBefore;
        }
    }

    public enum UserAssetTokenAction
    {
        RemoveToken,
        DeleteEntity,
        SkipEntity,
        Confirm,
        ReGenerateEntity,
    }
}

public enum FixTokenResult
{
    Nothing,
    Fix,
    RemoveToken,
    DeleteEntity,
    SkipEntity,
    RegenerateEntity,
    FixTokenInstead,
    FixOperationInstead,
}
