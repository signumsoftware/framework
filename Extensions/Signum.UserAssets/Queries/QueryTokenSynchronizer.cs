using Signum.DynamicQuery.Tokens;
using Signum.Engine.Sync;
using Signum.UserAssets;
using Signum.UserAssets.Queries;

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

public static class QueryTokenSynchronizer
{
    static void Remember(Replacements replacements, string oldTokenString, QueryToken newToken, QueryDescription qd, SubTokensOptions options)
    {
        List<QueryToken> tokenList = newToken.Follow(a => a.Parent).Reverse().ToList();

        string[] oldParts = QueryUtils.SplitRegex.Split(oldTokenString);
        string[] newParts = QueryUtils.SplitRegex.Split(newToken.FullKey());

        List<string> oldPartsList = oldParts.ToList();
        List<string> newPartsList = newParts.ToList();

        Func<string, string?> rep = str =>
        {
            if (Replacements.GlobalAutoReplacement == null)
                return null;

            Replacements.Selection? sel = Replacements.GlobalAutoReplacement(new Replacements.AutoReplacementContext("QueryToken", oldValue: str, newValues: null));

            if (sel == null || sel.Value.NewValue == null)
                return null;

            return sel.Value.NewValue;
        };

        int pos = -1;
        while (oldPartsList.Count > 0 && newPartsList.Count > 0 &&
            (oldPartsList[0] == newPartsList[0] ||
             rep(oldPartsList[0]) == newPartsList[0]) 
             )
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



        string key = pos == -1 ? QueryKey(tokenList[0].QueryName) : TypeKey(tokenList[pos].Type);

        replacements.GetOrCreate(key)[oldPartsList.ToString(".")] = newPartsList.ToString(".");
    }

    static bool TryParseRemember(Replacements replacements, string tokenString, QueryDescription qd, SubTokensOptions options, out QueryToken? result)
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
            }
            else
            {
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
                    Replacements.Selection? sel = Replacements.GlobalAutoReplacement(new Replacements.AutoReplacementContext(
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

                string key = result == null ? QueryKey(qd.QueryName) : TypeKey(result.Type);

                Dictionary<string, string>? dic = replacements.TryGetC(key);

                if (dic == null)
                    return false;

                string remainging = parts.Skip(i).ToString(".");

                string? old = dic.Keys.OrderByDescending(a => a.Length).FirstOrDefault(s => remainging.StartsWith(s));

                if (old == null)
                    return false;

                var subParts = dic[old].Let(s => s.HasText() ? QueryUtils.SplitRegex.Split(s) : new string[0]);

                for (int j = 0; j < subParts.Length; j++)
                {
                    string subPart = subParts[j];

                    QueryToken? subNewResult = QueryUtils.SubToken(result, qd, options, subPart);

                    if (subNewResult == null)
                        return false;

                    result = subNewResult;
                }

                i += (old == "" ? 0 : QueryUtils.SplitRegex.Split(old).Length) - 1;
            }
        }

        return true;
    }

    static string QueryKey(object tokenList)
    {
        return "tokens-Query-" + QueryUtils.GetKey(tokenList);
    }

    static string TypeKey(Type type)
    {
        return "tokens-Type-" + type.CleanType().FullName;
    }

    public static FixTokenResult FixValue(Replacements replacements, Type targetType, ref string? valueString, bool allowRemoveToken, bool isList, bool fixInstead, Type? currentEntityType)
    {
        var res = FilterValueConverter.IsValidExpression(valueString, targetType, isList, currentEntityType);

        if (res is Result<Type>.Success)
            return FixTokenResult.Nothing;

        DelayedConsole.Flush();

        if (isList && valueString!.Contains('|'))
        {
            List<string?> changes = new List<string?>();
            foreach (var str in valueString.Split('|'))
            {
                string? s = str;
                var result = FixValue(replacements, targetType, ref s, allowRemoveToken, isList: false, fixInstead, currentEntityType);

                if (result == FixTokenResult.DeleteEntity || 
                    result == FixTokenResult.SkipEntity || 
                    result == FixTokenResult.RemoveToken || 
                    result == FixTokenResult.FixTokenInstead || 
                    result == FixTokenResult.FixOperationInstead)
                    return result;

                changes.Add(s);
            }

            valueString = changes.ToString("|");
            return FixTokenResult.Fix;
        }

        if (targetType.IsLite())
        {
            var m = Lite.ParseRegex.Match(valueString!);
            if (m.Success)
            {
                var typeString = m.Groups["type"].Value;

                if (!TypeLogic.NameToType.ContainsKey(typeString))
                {
                    string? newTypeString = AskTypeReplacement(replacements, typeString);

                    if (newTypeString.HasText())
                    {
                        valueString = valueString!.Replace(typeString, newTypeString);
                        return FixTokenResult.Fix;
                    }
                }
            }
        }

        if (Replacements.GlobalAutoReplacement != null)
        {
            Replacements.Selection? sel = Replacements.GlobalAutoReplacement(new Replacements.AutoReplacementContext(replacementKey: "FixValue", oldValue: valueString!, newValues: null));

            if (sel != null && sel.Value.NewValue != null)
            {
                valueString = sel.Value.NewValue;
                return FixTokenResult.Fix;
            }
        }

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

        if (a == "s")
            return FixTokenResult.SkipEntity;

        if (allowRemoveToken && a == "r")
            return FixTokenResult.RemoveToken;


        if (fixInstead)
        {
            if (a == "t")
                return FixTokenResult.FixTokenInstead;

            if (a == "o")
                return FixTokenResult.FixOperationInstead;
        }

        if (a == "d")
            return FixTokenResult.DeleteEntity;

        valueString = answer;
        return FixTokenResult.Fix;

    }

    static string? AskTypeReplacement(Replacements replacements, string type)
    {
        return replacements.GetOrCreate("cleanNames").GetOrCreate(type, () =>
        {
            if (Replacements.GlobalAutoReplacement != null)
            {
                Replacements.Selection? sel = Replacements.GlobalAutoReplacement(new Replacements.AutoReplacementContext(replacementKey: "FixValue.Type", oldValue: type, newValues: null));

                if (sel != null && sel.Value.NewValue != null)
                    return sel.Value.NewValue;
            }

            Console.WriteLine("Type {0} has been renamed?".FormatWith(type));

            int startingIndex = 0;
            StringDistance sd = new StringDistance();
            var list = TypeLogic.NameToType.Keys.OrderBy(t => sd.LevenshteinDistance(t, type)).ToList();
        retry:
            int maxElements = Console.LargestWindowHeight - 11;

            list.Skip(startingIndex).Take(maxElements)
                       .Select((s, i) => "- {1,2}: {2} ".FormatWith(i + startingIndex == 0 ? ">" : " ", i + startingIndex, s)).ToConsole();
            Console.WriteLine();
            SafeConsole.WriteLineColor(ConsoleColor.White, "- n: None");

            int remaining = list.Count - startingIndex - maxElements;
            if (remaining > 0)
                SafeConsole.WriteLineColor(ConsoleColor.White, "- +: Show more values ({0} remaining)", remaining);

            while (true)
            {
                string answer = Console.ReadLine()!;

                if (answer == null)
                    throw new InvalidOperationException("Impossible to synchronize interactively without Console");

                answer = answer.ToLower();

                if (answer == "+" && remaining > 0)
                {
                    startingIndex += maxElements;
                    goto retry;
                }

                if (answer == "n")
                    return null!;

                if (int.TryParse(answer, out int option))
                {
                    return list[option];
                }

                Console.WriteLine("Error");
            }
        });
    }

    public static FixTokenResult FixToken(Replacements replacements, ref QueryTokenEmbedded token, QueryDescription qd, SubTokensOptions options, string? remainingText, bool allowRemoveToken, bool allowReCreate, bool forceChange = false)
    {
        var t = token;
        using (DelayedConsole.Delay(() => { SafeConsole.WriteColor(t.ParseException == null ? ConsoleColor.Gray : ConsoleColor.Red, "  " + t.TokenString); Console.WriteLine(" " + remainingText); }))
        {
            if (token.ParseException == null && !forceChange)
                return FixTokenResult.Nothing;

            DelayedConsole.Flush();
            FixTokenResult result = FixToken(replacements, token.TokenString, out QueryToken? resultToken, qd, options, remainingText, allowRemoveToken, allowReCreate, forceChange);

            if (result == FixTokenResult.Fix)
                token = new QueryTokenEmbedded(resultToken!);

            return result;
        }
    }

    public static FixTokenResult FixToken(Replacements replacements, string original, out QueryToken? token, QueryDescription qd, SubTokensOptions options, string? remainingText, bool allowRemoveToken, bool allowReGenerate, bool forceChange = false)
    {
        if (TryParseRemember(replacements, original, qd, options, out QueryToken? current))
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
                    Remember(replacements, original, current, qd, options);
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

                if (answer == "s")
                    return UserAssetTokenAction.SkipEntity;

                if (answer == "r" && allowRemoveToken)
                    return UserAssetTokenAction.RemoveToken;

                if (answer == "d")
                    return UserAssetTokenAction.DeleteEntity;

                if (answer == "g")
                    return UserAssetTokenAction.ReGenerateEntity;

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

                var tryToken = QueryUtils.TryParse(rawAnswer, qd, options);

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
