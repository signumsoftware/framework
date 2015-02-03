using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Templating
{
    public interface ITemplateValueProvider
    {
        object GetValue(TemplateParameters parameters);

        void Foreach(TemplateParameters parameters, Action<TemplateParameters> foreEachElement);
    }

    public class TemplateParameters
    {
        public IEntity Entity;
        public CultureInfo Culture;
        public Dictionary<QueryToken, ResultColumn> Columns;
    }


    public class TokenValueProvider : ITemplateValueProvider
    {
        public ParsedToken ParsedToken;
  

        public object GetValue(TemplateParameters parameters)
        {
            throw new NotImplementedException();
        }

        public void Foreach(TemplateParameters parameters, Action<TemplateParameters> foreEachElement)
        {
            throw new NotImplementedException();
        }
    }

    public class TranslateInstanceValueProvider : ITemplateValueProvider
    {
        public readonly QueryToken EntityToken;
        public readonly PropertyRoute Route;
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
                    error = "Variable '{0}' is not defined at this scope".FormatWith(v);
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

        internal string ToString(ScopedDictionary<string, ParsedToken> variables, string afterToken)
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb, variables, afterToken);
            return sb.ToString();
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
