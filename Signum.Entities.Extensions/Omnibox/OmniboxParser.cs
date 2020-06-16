using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using System.Threading;
using System.Collections.Concurrent;
using System.Globalization;
using Signum.Entities.Authorization;
using Signum.Entities.UserAssets;
using Newtonsoft.Json;

namespace Signum.Entities.Omnibox
{
    public static class OmniboxParser
    {
        static OmniboxManager manager;

        public static OmniboxManager Manager
        {
            get
            {
                if (manager == null)
                    throw new InvalidOperationException("OmniboxParse.Manager is not set");
                return manager;
            }

            set { manager = value; }
        }

        public static List<IOmniboxResultGenerator> Generators = new List<IOmniboxResultGenerator>();

        static string ident = @"[_\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}][\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]*";

        static string guid = @"[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}";

        static string symbol = @"[\.\,;!?@#$%&/\\\(\)\^\*\[\]\{\}\-+]";

        static readonly Regex tokenizer = new Regex(
$@"(?<entity>{ident};(\d+|{guid}))|
(?<space>\s+)|
(?<guid>{guid})|
(?<ident>{ident})|
(?<ident>\[{ident}\])|
(?<number>[+-]?\d+(\.\d+)?)|
(?<string>("".*?(""|$)|\'.*?(\'|$)))|
(?<comparer>({ FilterValueConverter.OperationRegex}))|
(?<symbol>{symbol})",
  RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

        public static int MaxResults = 20;

        public static List<OmniboxResult> Results(string omniboxQuery, CancellationToken ct)
        {
            List<OmniboxResult> result = new List<OmniboxResult>();

            if (omniboxQuery == "")
            {
                result.Add(new HelpOmniboxResult { Text = OmniboxMessage.Omnibox_OmniboxSyntaxGuide.NiceToString() });

                foreach (var generator in Generators)
                {
                    if (ct.IsCancellationRequested)
                        return result;

                    result.AddRange(generator.GetHelp());
                }

                result.Add(new HelpOmniboxResult { Text = OmniboxMessage.Omnibox_MatchingOptions.NiceToString() });
                result.Add(new HelpOmniboxResult { Text = OmniboxMessage.Omnibox_DatabaseAccess.NiceToString() });
                result.Add(new HelpOmniboxResult { Text = OmniboxMessage.Omnibox_Disambiguate.NiceToString() });

                return result.ToList();
            }
            else
            {
                List<OmniboxToken> tokens = new List<OmniboxToken>();

                foreach (Match m in tokenizer.Matches(omniboxQuery).Cast<Match>())
                {
                    if (ct.IsCancellationRequested)
                        return result;

                    AddTokens(tokens, m, "ident", OmniboxTokenType.Identifier);
                    AddTokens(tokens, m, "symbol", OmniboxTokenType.Symbol);
                    AddTokens(tokens, m, "comparer", OmniboxTokenType.Comparer);
                    AddTokens(tokens, m, "number", OmniboxTokenType.Number);
                    AddTokens(tokens, m, "guid", OmniboxTokenType.Guid);
                    AddTokens(tokens, m, "string", OmniboxTokenType.String);
                    AddTokens(tokens, m, "entity", OmniboxTokenType.Entity);
                }

                tokens.Sort(a => a.Index);

                var tokenPattern = new string(tokens.Select(t => t.Char()).ToArray());

                foreach (var generator in Generators)
                {
                    if (ct.IsCancellationRequested)
                        return result;

                    result.AddRange(generator.GetResults(omniboxQuery, tokens, tokenPattern).Take(MaxResults));
                }

                return result.OrderBy(a => a.Distance).Take(MaxResults).ToList();
            }
        }

        static void AddTokens(List<OmniboxToken> tokens, Match m, string groupName, OmniboxTokenType type)
        {
            var group = m.Groups[groupName];

            if (group.Success)
            {
                tokens.Add(new OmniboxToken(type, group.Index, group.Value));
            }
        }

        public static string ToOmniboxPascal(this string text)
        {
            var simple = Regex.Replace(text, OmniboxMessage.ComplementWordsRegex.NiceToString(), m => "", RegexOptions.IgnoreCase);

            var result = simple.ToPascal();

            if (text.StartsWith("[") && text.EndsWith("]"))
                return "[" + result + "]";

            return result;
        }

        public static Dictionary<string, V> ToOmniboxPascalDictionary<T, V>(this IEnumerable<T> collection, Func<T, string> getKey, Func<T, V> getValue)
        {
            Dictionary<string, V> result = new Dictionary<string, V>();
            foreach (var item in collection)
            {
                var key = getKey(item).ToOmniboxPascal();
                if (result.ContainsKey(key))
                {
                    for (int i = 1; ; i++)
                    {
                        var newKey = key + $"(Duplicated{(i == 1 ? "" : (" "  + i.ToString()))}!)";
                        if (!result.ContainsKey(newKey))
                        {
                            key = newKey;
                            break;
                        }
                    }
                }
                result.Add(key, getValue(item));
            }
            return result;
        }
    }

    public abstract class OmniboxManager
    {
        public abstract bool AllowedType(Type type);
        public abstract bool AllowedPermission(PermissionSymbol permission);
        public abstract bool AllowedQuery(object queryName);

        public abstract QueryDescription GetDescription(object queryName);

        public abstract Lite<Entity>? RetrieveLite(Type type, PrimaryKey id);

        public List<Lite<Entity>> Autocomplete(Type type, string subString, int count)
        {
            return Autocomplete(Implementations.By(type), subString, count);
        }

        public abstract List<Lite<Entity>> Autocomplete(Implementations implementations, string subString, int count);


        protected abstract IEnumerable<object> GetAllQueryNames();

        static ConcurrentDictionary<CultureInfo, Dictionary<string, object>> queries = new ConcurrentDictionary<CultureInfo, Dictionary<string, object>>();

        public Dictionary<string, object> GetQueries()
        {
            return queries.GetOrAdd(CultureInfo.CurrentCulture, ci =>
                 GetAllQueryNames().ToOmniboxPascalDictionary(qn => QueryUtils.GetNiceName(qn), qn => qn));
        }

        protected abstract IEnumerable<Type> GetAllTypes();

        static ConcurrentDictionary<CultureInfo, Dictionary<string, Type>> types = new ConcurrentDictionary<CultureInfo, Dictionary<string, Type>>();

        public Dictionary<string, Type> Types()
        {
            return types.GetOrAdd(CultureInfo.CurrentUICulture, ci =>
               GetAllTypes().Where(t => !t.IsEnumEntityOrSymbol()).ToOmniboxPascalDictionary(t => t.NicePluralName(), t => t));
        }
    }

    public abstract class OmniboxResult
    {
        public float Distance;

        public string ResultTypeName => GetType().Name;
    }

    public class HelpOmniboxResult : OmniboxResult
    {
        public string Text { get; set; }

        [JsonIgnore]
        public Type ReferencedType { get; set; }

        public string? ReferencedTypeName => this.ReferencedType?.Name;

        public override string ToString()
        {
            return "";
        }
    }

    public interface IOmniboxResultGenerator
    {
        IEnumerable<OmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern);

        List<HelpOmniboxResult> GetHelp();
    }

    public abstract class OmniboxResultGenerator<T> : IOmniboxResultGenerator where T : OmniboxResult
    {
        public abstract IEnumerable<T> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern);

        IEnumerable<OmniboxResult> IOmniboxResultGenerator.GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            return GetResults(rawQuery, tokens, tokenPattern);
        }

        public abstract List<HelpOmniboxResult> GetHelp();
    }

    public struct OmniboxToken
    {
        public OmniboxToken(OmniboxTokenType type, int index, string value)
        {
            this.Type = type;
            this.Index = index;
            this.Value = value;
        }

        public readonly OmniboxTokenType Type;
        public readonly int Index;
        public readonly string Value;

        public bool IsNull()
        {
            if (Type == OmniboxTokenType.Identifier)
                return Value == "null" || Value == "none";

            if (Type == OmniboxTokenType.String)
                return Value == "\"\"";

            return false;
        }

        internal char? Next(string rawQuery)
        {
            int last = Index + Value.Length;

            if (last < rawQuery.Length)
                return rawQuery[last];

            return null;
        }

        internal char Char()
        {
            switch (Type)
            {
                case OmniboxTokenType.Identifier: return 'I';
                case OmniboxTokenType.Symbol: return Value.Single();
                case OmniboxTokenType.Comparer: return '=';
                case OmniboxTokenType.Number: return 'N';
                case OmniboxTokenType.String: return 'S';
                case OmniboxTokenType.Entity: return 'E';
                case OmniboxTokenType.Guid: return 'G';
                default: return '?';
            }
        }
    }

    public enum OmniboxTokenType
    {
        Identifier,
        Symbol,
        Comparer,
        Number,
        String,
        Entity,
        Guid,
    }
}
