using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Signum.Entities.Omnibox
{
    public static class OmniboxParser
    {
        public static List<OmniboxProvider> Providers;

        static readonly Regex tokenizer = new Regex(
@"^(
(?<space>\s+)|
(?<ident>[a-zA-Z_][a-zA-Z0-9_]*)|
(?<number>[+-]?\d+(\.\d+)?)|
(?<string>("".*?""|\'.*?\'))|
(?<dot>\.)|
(?<comparer>(==?|<=|>=|<|>|\^=|\$=|%=|\!=|\!\^=|\!\$=|\!%=))
)*$", RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);


        public List<OmniboxResult> Results(string omniboxQuery)
        {
            List<OmniboxResult> result = new List<OmniboxResult>();

            var m = tokenizer.Match(omniboxQuery);

                List<OmniboxToken> tokens = new List<OmniboxToken>();

                if (m.Success)
                {
                    AddTokens(tokens, m, "ident", OmniboxTokenType.Identifier);
                    AddTokens(tokens, m, "dot", OmniboxTokenType.Dot);
                    AddTokens(tokens, m, "comparer", OmniboxTokenType.Comparer);
                    AddTokens(tokens, m, "number", OmniboxTokenType.Number);
                    AddTokens(tokens, m, "string", OmniboxTokenType.String);
                    AddTokens(tokens, m, "date", OmniboxTokenType.String);
                    
                    tokens.Sort((a, b) => a.Index.CompareTo(b.Index));
                }

              foreach (var provider in Providers)
	          {
                  provider.AddResults(result, omniboxQuery, tokens);
              }

            return result;
        }

        void AddTokens(List<OmniboxToken> tokens, Match m, string groupName, OmniboxTokenType type)
        {
            var group = m.Groups["groupName"];

            if (group.Success)
            {
                foreach (Capture c in group.Captures)
                {
                    tokens.Add(new OmniboxToken(OmniboxTokenType.Identifier, c.Index, c.Value));
                }
            }
        }
    }

    public abstract class OmniboxResult
    {
        public int Priority; 

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
    }

    public enum OmniboxTokenType
    {
        Identifier,
        Dot,
        Comparer,
        Number,
        String,
        Date,
    }
}
