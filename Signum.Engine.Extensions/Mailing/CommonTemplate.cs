using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Signum.Engine.Mailing
{
    public static class TemplateRegex
    {
        public static readonly Regex KeywordsRegex = new Regex(@"\@(((?<keyword>(foreach|if|raw|global|model|modelraw|any|declare|))\[(?<token>[^\]]+)\](\s+as\s+(?<dec>\$\w*))?)|(?<keyword>endforeach|else|endif|notany|endany))");

        public static readonly Regex TokenFormatRegex = new Regex(@"(?<token>[^\]\:]+)(\:(?<format>.*))?");
        public static readonly Regex TokenOperationValueRegex = new Regex(@"(?<token>[^\]]+)(?<comparer>(" + FilterValueConverter.OperationRegex + @"))(?<value>[^\]\:]+)");
    }

    public class ParsedToken
    {
        public string String;
        public QueryToken QueryToken;
        public string Variable;

        public static ParsedToken TryParseToken(string tokenString, string variable, SubTokensOptions options, QueryDescription qd, ScopedDictionary<string, ParsedToken> variables, out string error)
        {
            error = null;
            ParsedToken result = new ParsedToken { String = tokenString, Variable = variable };

            if (tokenString.StartsWith("$"))
            {
                string v = tokenString.TryBefore('.') ?? tokenString;

                ParsedToken token;

                if (!variables.TryGetValue(v, out token))
                {
                    error = "Variable '{0}' is not defined at this scope".Formato(v);
                    return result;
                }

                var after = tokenString.TryAfter('.');

                tokenString = token.QueryToken.FullKey() + (after == null ? null : ("." + after));
            }

            try
            {
                result.QueryToken = QueryUtils.Parse(tokenString, qd, options);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return result;
        }

        internal void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables, string afterToken)
        {
            sb.Append("[");
            sb.Append(QueryToken == null ? String : SimplifyToken(variables, QueryToken.FullKey()));

            if (afterToken.HasItems())
                sb.Append(afterToken);

            sb.Append("]");

            if (Variable.HasItems())
                sb.Append(" as " + Variable);
        }

        string SimplifyToken(ScopedDictionary<string, ParsedToken> variables, string token)
        {
            var variable = (from kvp in variables
                            let t = kvp.Value.QueryToken.FullKey()
                            where token == t || token.StartsWith(t + ".")
                            orderby t.Length descending
                            select kvp).FirstOrDefault();

            if (variable.Key.HasText())
            {
                var fullKey = variable.Value.QueryToken.FullKey();

                return variable.Key + token.RemoveStart(fullKey.Length);
            }

            return token;
        }

        internal void Declare(ScopedDictionary<string, ParsedToken> newVars)
        {
            if (Variable.HasText())
                newVars.Add(Variable, this);
        }
    }
}
