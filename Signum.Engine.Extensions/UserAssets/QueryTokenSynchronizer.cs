using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.UserAssets
{
    public static class QueryTokenSynchronizer
    {
        static void Remember(Replacements replacements, string tokenString, QueryToken token)
        {
            List<QueryToken> tokenList = token.Follow(a => a.Parent).Reverse().ToList();

            string[] oldParts = tokenString.Split('.');
            string[] newParts = token.FullKey().Split('.');

            List<string> oldPartsList = oldParts.ToList();
            List<string> newPartsList = newParts.ToList();

            Func<string, string> rep = str =>
            {
                if (Replacements.AutoReplacement == null)
                    return null;

                Replacements.Selection? sel = Replacements.AutoReplacement(str, null);

                if (sel == null || sel.Value.NewValue == null)
                    return null;

                return sel.Value.NewValue;
            };

            int pos = -1;
            while (oldPartsList.Count > 0 && newPartsList.Count > 0 &&
                (oldPartsList[0] == newPartsList[0] ||
                 rep(oldPartsList[0]) == newPartsList[0]))
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

        static bool TryParseRemember(Replacements replacements, string tokenString, QueryDescription qd, SubTokensOptions options, out QueryToken result)
        {
            string[] parts = tokenString.Split('.');

            result = null;
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                QueryToken newResult = QueryUtils.SubToken(result, qd, options, part);

                if (newResult != null)
                {
                    result = newResult;
                }
                else
                {
                    if (i == 0)
                    {
                        var entity = QueryUtils.SubToken(result, qd, options, "Entity");
                        QueryToken newSubResult = QueryUtils.SubToken(entity, qd, options, part);

                        if (newSubResult != null)
                        {
                            result = newSubResult;
                            continue;
                        }
                    }


                    if (Replacements.AutoReplacement != null)
                    {
                        Replacements.Selection? sel = Replacements.AutoReplacement(part, result.SubTokens(qd, options).Select(a => a.Key).ToList());

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

                    Dictionary<string, string> dic = replacements.TryGetC(key);

                    if (dic == null)
                        return false;

                    string remainging = parts.Skip(i).ToString(".");

                    string old = dic.Keys.OrderByDescending(a => a.Length).FirstOrDefault(s => remainging.StartsWith(s));

                    if (old == null)
                        return false;

                    var subParts = dic[old].Let(s => s.HasText() ? s.Split('.') : new string[0]);

                    for (int j = 0; j < subParts.Length; j++)
                    {
                        string subPart = subParts[j];

                        QueryToken subNewResult = QueryUtils.SubToken(result, qd, options, subPart);

                        if (subNewResult == null)
                            return false;

                        result = subNewResult;
                    }

                    i += (old == "" ? 0 : old.Split('.').Length) - 1;
                }
            }

            return true;
        }

        static string QueryKey(object tokenList)
        {
            return "tokens-Query-" + QueryUtils.GetCleanName(tokenList);
        }

        static string TypeKey(Type type)
        {
            return "tokens-Type-" + type.CleanType().FullName;
        }

        public static FixTokenResult FixValue(Replacements replacements, Type type, ref string valueString, bool allowRemoveToken, bool isList)
        {
            object val;
            string error = FilterValueConverter.TryParse(valueString, type, out val, isList);

            if (error == null)
                return FixTokenResult.Nothing;

            if (isList && valueString.Contains('|'))
            {
                List<string> changes = new List<string>();
                foreach (var str in valueString.Split('|'))
                {
                    string s = str;
                    var result = FixValue(replacements, type, ref s, allowRemoveToken, false);

                    if (result == FixTokenResult.DeleteEntity || result == FixTokenResult.SkipEntity || result == FixTokenResult.RemoveToken)
                        return result;

                    changes.Add(s);
                }

                valueString = changes.ToString("|");
                return FixTokenResult.Fix;
            }

            if (type.IsLite())
            {
                var m = Lite.ParseRegex.Match(valueString);
                if (m.Success)
                {
                    var typeString = m.Groups["type"].Value;

                    if (!TypeLogic.NameToType.ContainsKey(typeString))
                    {
                        string newTypeString = AskTypeReplacement(replacements, typeString);

                        if (newTypeString.HasText())
                        {
                            valueString = valueString.Replace(typeString, newTypeString);
                            return FixTokenResult.Fix;
                        }
                    }
                }
            }

            if (Replacements.AutoReplacement != null)
            {
                Replacements.Selection? sel = Replacements.AutoReplacement(valueString, null);

                if (sel != null && sel.Value.NewValue != null)
                {
                    valueString = sel.Value.NewValue;
                    return FixTokenResult.Fix;
                }
            }

            SafeConsole.WriteLineColor(ConsoleColor.White, "Value '{0}' not convertible to {1}.".FormatWith(valueString, type.TypeName()));
            SafeConsole.WriteLineColor(ConsoleColor.Yellow, "- s: Skip entity");
            if (allowRemoveToken)
                SafeConsole.WriteLineColor(ConsoleColor.DarkRed, "- r: Remove token");
            SafeConsole.WriteLineColor(ConsoleColor.Red, "- d: Delete entity");
            SafeConsole.WriteLineColor(ConsoleColor.Green, "- freeText: New value");

            string answer = Console.ReadLine();

            if (answer == null)
                throw new InvalidOperationException("Impossible to synchronize interactively without Console");

            string a = answer.ToLower();

            if (a == "s")
                return FixTokenResult.SkipEntity;

            if (allowRemoveToken && a == "r")
                return FixTokenResult.RemoveToken;

            if (a == "d")
                return FixTokenResult.DeleteEntity;

            valueString = answer;
            return FixTokenResult.Fix;

        }

        static string AskTypeReplacement(Replacements replacements, string type)
        {
            return replacements.GetOrCreate("cleanNames").GetOrCreate(type, () =>
            {
                if (Replacements.AutoReplacement != null)
                {
                    Replacements.Selection? sel = Replacements.AutoReplacement(type, null);

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
                    string answer = Console.ReadLine();

                    if (answer == null)
                        throw new InvalidOperationException("Impossible to synchronize interactively without Console");

                    answer = answer.ToLower();

                    if (answer == "+" && remaining > 0)
                    {
                        startingIndex += maxElements;
                        goto retry;
                    }

                    if (answer == "n")
                        return null;

                    int option = 0;
                    if (int.TryParse(answer, out option))
                    {
                        return list[option];
                    }

                    Console.WriteLine("Error");
                }
            });
        }

        public static FixTokenResult FixToken(Replacements replacements, ref QueryTokenEntity token, QueryDescription qd, SubTokensOptions options, string remainingText, bool allowRemoveToken, bool allowReCreate)
        {
            SafeConsole.WriteColor(token.ParseException == null ? ConsoleColor.Gray : ConsoleColor.Red, "  " + token.TokenString);
            Console.WriteLine(" " + remainingText);

            if (token.ParseException == null)
                return FixTokenResult.Nothing;

            QueryToken resultToken;
            FixTokenResult result = FixToken(replacements, token.TokenString, out resultToken, qd, options, remainingText, allowRemoveToken, allowReCreate);

            if (result == FixTokenResult.Fix)
                token = new QueryTokenEntity(resultToken);

            return result;
        }

        public static FixTokenResult FixToken(Replacements replacements, string original, out QueryToken token, QueryDescription qd, SubTokensOptions options, string remainingText, bool allowRemoveToken, bool allowReGenerate)
        {
            string[] parts = original.Split('.');

            QueryToken current;
            if (TryParseRemember(replacements, original, qd, options, out current))
            {
                if (current.FullKey() != original)
                {
                    SafeConsole.WriteColor(ConsoleColor.DarkRed, "  " + original);
                    Console.Write(" -> ");
                    SafeConsole.WriteColor(ConsoleColor.DarkGreen, current.FullKey());
                }
                Console.WriteLine(remainingText);
                token = current;
                return FixTokenResult.Fix;
            }

            while (true)
            {
                var result = SelectInteractive(ref current, qd, options, allowRemoveToken, allowReGenerate);
                switch (result)
                {
                    case UserAssetTokenAction.DeleteEntity:
                        SafeConsole.WriteLineColor(ConsoleColor.Red, "Entity deleted");
                        token = null;
                        return FixTokenResult.DeleteEntity;
                    case UserAssetTokenAction.ReGenerateEntity:
                        if (!allowReGenerate)
                            throw new InvalidOperationException("Unexpected ReGenerate");

                        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Entity Re-Generated");
                        token = null;
                        return FixTokenResult.ReGenerateEntity;
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
                        Remember(replacements, original, current);
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

        static UserAssetTokenAction? SelectInteractive(ref QueryToken token, QueryDescription qd, SubTokensOptions options, bool allowRemoveToken, bool allowReGenerate)
        {
            var top = Console.CursorTop;

            try
            {
                if (Console.Out == null)
                    throw new InvalidOperationException("Impossible to synchronize without interactive Console");

                var subTokens = token.SubTokens(qd, options).OrderBy(a => a.Parent != null).ThenBy(a => a.Key).ToList();

                int startingIndex = 0;

                SafeConsole.WriteLineColor(ConsoleColor.Cyan, "  " + token.Try(a => a.FullKey()));

                bool isRoot = token == null;

            retry:
                int maxElements = Console.LargestWindowHeight - 11;

                subTokens.Skip(startingIndex).Take(maxElements)
                    .Select((s, i) => "- {1,2}: {2} ".FormatWith(i + " ", i + startingIndex, ((isRoot && s.Parent != null) ? "-" : "") + s.Key)).ToConsole();
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

                while (true)
                {
                    string answer = Console.ReadLine();

                    if (answer == null)
                        throw new InvalidOperationException("Impossible to synchronize interactively without Console");

                    answer = answer.ToLower();

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
                            token = token.Parent;
                            return null;
                        }
                    }

                    int option = 0;
                    if (int.TryParse(answer, out option))
                    {
                        token = subTokens[option];
                        return null;
                    }

                    Console.WriteLine("Error");
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
        ReGenerateEntity,
    }
}
