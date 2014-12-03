using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Signum.Engine.Templating
{
    public static class TemplateUtils
    {
        public static readonly Regex KeywordsRegex = new Regex(@"\@(((?<keyword>(foreach|if|raw|global|model|modelraw|any|declare|))\[(?<token>[^\]]+)\](\s+as\s+(?<dec>\$\w*))?)|(?<keyword>endforeach|else|endif|notany|endany))");

        public static readonly Regex TokenFormatRegex = new Regex(@"(?<token>[^\]\:]+)(\:(?<format>.*))?");
        public static readonly Regex TokenOperationValueRegex = new Regex(@"(?<token>[^\]]+)(?<comparer>(" + FilterValueConverter.OperationRegex + @"))(?<value>[^\]\:]+)");

        public static bool ToBool(object obj)
        {
            if (obj == null || obj is bool && ((bool)obj) == false)
                return false;

            return true;
        }


        public static object DistinctSingle(this IEnumerable<ResultRow> rows, ResultColumn column)
        {
            return rows.Select(r => r[column]).Distinct(SemiStructuralEqualityComparer.Comparer).SingleEx(
                () => "No values for column {0}".FormatWith(column.Column.Token.FullKey()),
                () => "Multiple values for column {0}".FormatWith(column.Column.Token.FullKey()));
        }

        class SemiStructuralEqualityComparer : IEqualityComparer<object>
        {
            public static readonly SemiStructuralEqualityComparer Comparer = new SemiStructuralEqualityComparer();

            ConcurrentDictionary<Type, List<Func<object, object>>> Cache = new ConcurrentDictionary<Type, List<Func<object, object>>>();

            public List<Func<object, object>> GetFieldGetters(Type type)
            {
                return Cache.GetOrAdd(type, t =>
                    t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => !f.HasAttribute<IgnoreAttribute>())
                    .Select(fi => Signum.Utilities.Reflection.ReflectionTools.CreateGetterUntyped(t, fi)).ToList());
            }

            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                if (x == null || y == null)
                    return x == null && y == null;

                Type t = x.GetType();

                if (IsSimple(t))
                    return x.Equals(y);

                var fields = GetFieldGetters(t);
                for (int i = 0; i < fields.Count; i++)
                {
                    var f = fields[i];
                    if (!Equals(f(x), f(y)))
                        return false;
                }


                return true;
            }

            public int GetHashCode(object obj)
            {
                if (obj == null)
                    return 0;

                Type t = obj.GetType();

                if (IsSimple(t))
                    return obj.GetHashCode();

                int result = 1;

                var fields = GetFieldGetters(t);
                for (int i = 0; i < fields.Count; i++)
                    result ^= GetHashCode(fields[i](obj)) << (i % 8);

                return result;
            }

            static bool IsSimple(Type t)
            {
                return t == typeof(string) || Type.GetTypeCode(t) >= TypeCode.Boolean ||
                    typeof(IEntity).IsAssignableFrom(t) || typeof(Lite<IEntity>).IsAssignableFrom(t) ||
                    typeof(IEquatable<>).MakeGenericType(t).IsAssignableFrom(t);
            }
        }

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
