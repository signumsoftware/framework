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
using Signum.Entities.UserQueries;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.UserQueries
{
    public static class QueryTokenSynchronizer
    {
        static void Remember(Replacements replacements, string tokenString, QueryToken token)
        {
            List<QueryToken> tokenList = token.FollowC(a => a.Parent).Reverse().ToList();

            string[] oldParts = tokenString.Split('.');
            string[] newParts = token.FullKey().Split('.');

            List<string> oldPartsList = oldParts.ToList();
            List<string> newPartsList = newParts.ToList();

            Func<string, string> rep = str =>
            {
                if (Replacements.AutoRepacement == null)
                    return null;

                Replacements.Selection? sel = Replacements.AutoRepacement(str, null);

                if (sel == null || sel.Value.NewValue == null)
                    return null;

                return sel.Value.NewValue;
            };

            int pos = -1;
            while (oldPartsList.Count > 0 && newPartsList.Count > 0 &&
                (oldPartsList[0] == newPartsList[0] ||
                 rep(oldPartsList[0]) == newPartsList[0]))
            {
                pos++;
                oldPartsList.RemoveAt(0);
                newPartsList.RemoveAt(0);
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

        static bool TryParseRemember(Replacements replacements, string tokenString, QueryDescription qd, bool canAggregate, out QueryToken result)
        {
            string[] parts = tokenString.Split('.');

            result = null;
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                QueryToken newResult = QueryUtils.SubToken(result, qd, canAggregate, part);

                if (newResult != null)
                {
                    result = newResult;
                }
                else
                {
                    if (Replacements.AutoRepacement != null)
                    {
                        Replacements.Selection? sel = Replacements.AutoRepacement(part, result.SubTokens(qd, canAggregate).Select(a => a.Key).ToList());

                        if (sel != null && sel.Value.NewValue != null)
                        {
                            newResult = QueryUtils.SubToken(result, qd, canAggregate, sel.Value.NewValue);

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
                        string subPart = subParts[0];

                        QueryToken subNewResult = QueryUtils.SubToken(result, qd, canAggregate, subPart);

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

        public static FixTokenResult FixValue(Replacements replacements, Type type, ref string valueString, bool allowRemoveToken)
        {
            object val;
            string error = FilterValueConverter.TryParse(valueString, type, out val);

            if (error == null)
                return FixTokenResult.Nothing;

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

            if (Replacements.AutoRepacement != null)
            {
                Replacements.Selection? sel = Replacements.AutoRepacement(valueString, null);

                if (sel != null && sel.Value.NewValue != null)
                {
                    valueString = sel.Value.NewValue;
                    return FixTokenResult.Fix;
                }
            }

            SafeConsole.WriteLineColor(ConsoleColor.White, "Value '{0}' not convertible to {1}.".Formato(valueString, type.TypeName()));
            SafeConsole.WriteLineColor(ConsoleColor.Yellow, "- s: Skip entity");
            if (allowRemoveToken)
                SafeConsole.WriteLineColor(ConsoleColor.DarkRed, "- r: Remove token");
            SafeConsole.WriteLineColor(ConsoleColor.Red, "- d: Delete entity");
            SafeConsole.WriteLineColor(ConsoleColor.Green, "- freeText: New value");

            string answer = Console.ReadLine();

            if (answer == null)
                throw new InvalidOperationException("Impossible to Syncrhonize interactively without Console");

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
                if (Replacements.AutoRepacement != null)
                {
                    Replacements.Selection? sel = Replacements.AutoRepacement(type, null);

                    if (sel != null && sel.Value.NewValue != null)
                        return sel.Value.NewValue;
                }

                Console.WriteLine("Type {0} has been renamed?".Formato(type));

                int startingIndex = 0;
                StringDistance sd = new StringDistance();
                var list = TypeLogic.NameToType.Keys.OrderBy(t => sd.LevenshteinDistance(t, type)).ToList();
            retry:
                int maxElements = Console.LargestWindowHeight - 11;

                list.Skip(startingIndex).Take(maxElements)
                           .Select((s, i) => "- {1,2}: {2} ".Formato(i + startingIndex == 0 ? ">" : " ", i + startingIndex, s)).ToConsole();
                Console.WriteLine();
                SafeConsole.WriteLineColor(ConsoleColor.White, "- n: None");

                int remaining = list.Count - startingIndex - maxElements;
                if (remaining > 0)
                    SafeConsole.WriteLineColor(ConsoleColor.White, "- +: Show more values ({0} remaining)", remaining);

                while (true)
                {
                    string answer = Console.ReadLine();

                    if (answer == null)
                        throw new InvalidOperationException("Impossible to Syncrhonize interactively without Console");

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

        public static FixTokenResult FixToken(Replacements replacements, ref QueryTokenDN token, QueryDescription qd, bool canAggregate, string remainingText, bool allowRemoveToken = true)
        {
            var original = token.TokenString;

            SafeConsole.WriteColor(token.ParseException == null ? ConsoleColor.Gray : ConsoleColor.Red, "  " + original);
            Console.WriteLine(" " + remainingText);

            if (token.ParseException == null)
                return FixTokenResult.Nothing;

            string[] parts = token.TokenString.Split('.');

            QueryToken current;
            if (TryParseRemember(replacements, original, qd, canAggregate, out current))
            {
                SafeConsole.WriteColor(ConsoleColor.DarkRed, "  " + original);
                Console.Write(" -> ");
                SafeConsole.WriteColor(ConsoleColor.DarkGreen, current.FullKey());
                Console.WriteLine(remainingText);
                token = new QueryTokenDN(current);
                return FixTokenResult.Fix;
            }

            while (true)
            {
                var result = SelectInteractive(ref current, qd, canAggregate, allowRemoveToken);
                switch (result)
                {
                    case UserAssetTokenAction.DeleteEntity:
                        SafeConsole.WriteLineColor(ConsoleColor.Red, "Entity deleted");
                        return FixTokenResult.DeleteEntity;
                    case UserAssetTokenAction.RemoveToken:
                        if (!allowRemoveToken)
                            throw new InvalidOperationException("Unexpected RemoveToken");

                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        SafeConsole.WriteColor(ConsoleColor.DarkRed, "  " + original);
                        SafeConsole.WriteColor(ConsoleColor.DarkRed, " (token removed)");
                        Console.WriteLine(remainingText);
                        return FixTokenResult.RemoveToken;
                    case UserAssetTokenAction.SkipEntity:
                        SafeConsole.WriteLineColor(ConsoleColor.DarkYellow, "Entity skipped");
                        return FixTokenResult.SkipEntity;
                    case UserAssetTokenAction.Confirm:
                        Remember(replacements, original, current);
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        SafeConsole.WriteColor(ConsoleColor.DarkRed, "  " + original);
                        Console.Write(" -> ");
                        SafeConsole.WriteColor(ConsoleColor.DarkGreen, current.FullKey());
                        Console.WriteLine(remainingText);
                        token = new QueryTokenDN(current);
                        return FixTokenResult.Fix;
                }
            }
        }

        static UserAssetTokenAction? SelectInteractive(ref QueryToken token, QueryDescription qd, bool canAggegate, bool allowRemoveToken)
        {
            var top = Console.CursorTop;

            try
            {
                if (Console.Out == null)
                    throw new InvalidOperationException("Impossible to synchronize without interactive Console");

                var subTokens = token.SubTokens(qd, canAggegate).ToList();

                int startingIndex = 0;

                SafeConsole.WriteLineColor(ConsoleColor.Cyan, "  " + token.TryCC(a => a.FullKey()));

                bool isRoot = token == null;

            retry:
                int maxElements = Console.LargestWindowHeight - 11;

                subTokens.Skip(startingIndex).Take(maxElements)
                    .Select((s, i) => "- {1,2}: {2} ".Formato(i + " ", i + startingIndex, ((isRoot && !(s is ColumnToken)) ? "-" : "") + s.Key)).ToConsole();
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

                SafeConsole.WriteLineColor(ConsoleColor.Red, "- d: Delete entity");

                while (true)
                {
                    string answer = Console.ReadLine();

                    if (answer == null)
                        throw new InvalidOperationException("Impossible to Syncrhonize interactively without Console");

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
        }
    }

    public enum FixTokenResult
    {
        Nothing,
        Fix,
        RemoveToken,
        DeleteEntity,
        SkipEntity,
    }
}
