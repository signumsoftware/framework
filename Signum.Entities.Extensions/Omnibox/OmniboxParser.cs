using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Utilities;

namespace Signum.Entities.Omnibox
{
    public static class OmniboxParser
    {
        public static List<OmniboxProvider> Providers = new List<OmniboxProvider>();

        static readonly Regex tokenizer = new Regex(
@"(?<space>\s+)|
(?<ident>[a-zA-Z_][a-zA-Z0-9_]*)|
(?<number>[+-]?\d+(\.\d+)?)|
(?<string>("".*?""|\'.*?\'))|
(?<dot>\.)|
(?<semicolon>;)|
(?<comparer>(==?|<=|>=|<|>|\^=|\$=|%=|\*=|\!=|\!\^=|\!\$=|\!%=|\!\*=))", 
  RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);


        public static List<OmniboxResult> Results(string omniboxQuery)
        {
            List<OmniboxToken> tokens = new List<OmniboxToken>();

            foreach (Match m in tokenizer.Matches(omniboxQuery))
            {
                AddTokens(tokens, m, "ident", OmniboxTokenType.Identifier);
                AddTokens(tokens, m, "dot", OmniboxTokenType.Dot);
                AddTokens(tokens, m, "semicolon", OmniboxTokenType.Semicolon);
                AddTokens(tokens, m, "comparer", OmniboxTokenType.Comparer);
                AddTokens(tokens, m, "number", OmniboxTokenType.Number);
                AddTokens(tokens, m, "string", OmniboxTokenType.String);
                AddTokens(tokens, m, "date", OmniboxTokenType.String);
            }

            tokens.Sort(a => a.Index);

            List<OmniboxResult> result = new List<OmniboxResult>();
            foreach (var provider in Providers)
            {
                provider.AddResults(result, omniboxQuery, tokens);
            }

            result.Sort(a => a.Distance);

            return result;
        }

        static void AddTokens(List<OmniboxToken> tokens, Match m, string groupName, OmniboxTokenType type)
        {
            var group = m.Groups[groupName];

            if (group.Success)
            {
                tokens.Add(new OmniboxToken(type, group.Index, group.Value));
            }
        }
    }

    public abstract class OmniboxResult
    {
        public float Distance;

    }

    public abstract class OmniboxProvider
    {
        public abstract void AddResults(List<OmniboxResult> results, string rawQuery, List<OmniboxToken> tokens); 
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
    }

    public enum OmniboxTokenType
    {
        Identifier,
        Dot,
        Semicolon,
        Comparer,
        Number,
        String,
    }
}
