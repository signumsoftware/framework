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
using System.Text.RegularExpressions;
using Signum.Engine.UserAssets;

namespace Signum.Engine.Templating
{
    public static class TemplateUtils
    {
        public static readonly Regex KeywordsRegex = new Regex(@"\@(((?<keyword>(foreach|if|raw|global|model|modelraw|any|declare|))\[(?<expr>[^\]\}]+)\](\s+as\s+(?<dec>\$\w*))?)|(?<keyword>endforeach|else|endif|notany|endany))");

        public static readonly Regex TokenOperationValueRegex = new Regex(@"(?<token>((?<type>[\w]):)?.+?)(?<operation>(" + FilterValueConverter.OperationRegex + @"))(?<value>[^\]\:]+)");

        public static readonly Regex TokenFormatRegex = new Regex(@"(?<token>((?<type>[\w]):)?(\\\]|\\\:|[^\:])+)(\:(?<format>.*))?");
        
        public struct SplittedToken
        {
            public string Token;
            public string? Format; 
        }

        public static SplittedToken? SplitToken(string formattedToken)
        {
            var tok = TemplateUtils.TokenFormatRegex.Match(formattedToken);

            if (tok == null)
                return null;

            return new SplittedToken
            {
                Token = tok.Groups["token"].Value.Replace(@"\:", ":"),
                Format = tok.Groups["format"].Value.DefaultText("").Replace(@"\:", ":").DefaultToNull()
            };
        }

        public static ConditionBase ParseCondition(string expr, string variable, ITemplateParser parser)
        {
            expr = expr.Trim();

            var left = expr.TryBefore("||");
            if (left != null)
            {
                return new ConditionOr(
                    ParseCondition(left, variable, parser),
                    ParseCondition(expr.After("||"), variable, parser));
            }

            left = expr.TryBefore(" OR ");
            if (left != null)
            {
                return new ConditionOr(
                    ParseCondition(left, variable, parser),
                    ParseCondition(expr.After(" OR "), variable, parser));
            }

            left = expr.TryBefore("&&");
            if (left != null)
            {
                return new ConditionAnd(
                    ParseCondition(left, variable, parser),
                    ParseCondition(expr.After("&&"), variable, parser));
            }

            left = expr.TryBefore(" AND ");
            if (left != null)
            {
                return new ConditionAnd(
                    ParseCondition(left, variable, parser),
                    ParseCondition(expr.After(" AND "), variable, parser));
            }

            var filter = TemplateUtils.TokenOperationValueRegex.Match(expr);
            if (!filter.Success)
            {
                return new ConditionCompare(ValueProviderBase.TryParse(expr, variable, parser)!);
            }
            else
            {
                var vpb = ValueProviderBase.TryParse(filter.Groups["token"].Value, variable, parser);
                var operation = filter.Groups["operation"].Value;
                var value = filter.Groups["value"].Value;
                return new ConditionCompare(vpb, operation, value, parser.AddError);
            }
        }

       

        public static string ScapeColon(string tokenOrFormat)
        {
            return tokenOrFormat.Replace(":", @"\:");
        }

        public static object? DistinctSingle(this IEnumerable<ResultRow> rows, ResultColumn column)
        {
            return rows.Select(r => r[column]).Distinct(SemiStructuralEqualityComparer.Comparer).SingleEx(
                () => "No values for column {0}".FormatWith(column.Column.Token.FullKey()),
                () => "Multiple values for column {0}".FormatWith(column.Column.Token.FullKey()));
        }

        public static IEnumerable<IEnumerable<ResultRow>> GroupByColumn(this IEnumerable<ResultRow> rows, ResultColumn keyColumn)
        {
            var groups = rows.GroupBy(r => r[keyColumn], TemplateUtils.SemiStructuralEqualityComparer.Comparer).ToList();
            if (groups.Count == 1 && groups[0].Key == null)
                return Enumerable.Empty<IEnumerable<ResultRow>>();

            return groups;
        }

        internal class SemiStructuralEqualityComparer : IEqualityComparer<object?>
        {
            public static readonly SemiStructuralEqualityComparer Comparer = new SemiStructuralEqualityComparer();

            ConcurrentDictionary<Type, List<Func<object, object?>>> Cache = new ConcurrentDictionary<Type, List<Func<object, object?>>>();

            public List<Func<object, object?>> GetFieldGetters(Type type)
            {
                return Cache.GetOrAdd(type, t =>
                    t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => !f.HasAttribute<IgnoreAttribute>())
                    .Select(fi => Signum.Utilities.Reflection.ReflectionTools.CreateGetterUntyped(t, fi)!).ToList());
            }

            bool IEqualityComparer<object?>.Equals(object? x, object? y)
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

            public int GetHashCode(object? obj)
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

    public struct TemplateError
    {
        public TemplateError(bool isFatal, string message)
        {
            this.Message = message;
            this.IsFatal = isFatal;
        }

        public readonly string Message;
        public readonly bool IsFatal;

        public override string ToString()
        {
            return (IsFatal ? "FATAL: " : "ERROR: ") + Message;
        }
    }


    public static class ParsedModel
    {
        public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static List<MemberInfo>? GetMembers(Type modelType, string? fieldOrPropertyChain, Action<bool, string> addError)
        {
            var members = new List<MemberInfo>();
            var type = modelType;
            foreach (var field in (fieldOrPropertyChain ?? "").Trim().Split('.'))
            {
                var info = (MemberInfo?)type.GetField(field, Flags) ??
                           (MemberInfo?)type.GetProperty(field, Flags) ??
                           (MemberInfo?)type.GetMethod(field, Flags, null, new[] { typeof(TemplateParameters) }, null);

                if (info == null)
                {
                    addError(false, "Type {0} does not have a property/field with name {1}, or a method that takes a TemplateParameters as an argument".FormatWith(type.Name, field));
                    return null;
                }

                members.Add(info);

                type = info.ReturningType();
            }

            return members;
        }
    }

    public class SynchronizationContext
    {
        public ScopedDictionary<string, ValueProviderBase> Variables;
        public Type? ModelType;
        public Replacements Replacements;
        public StringDistance StringDistance;
        public QueryDescription QueryDescription;

        public bool HasChanges;

        public SynchronizationContext(Replacements replacements, StringDistance stringDistance, QueryDescription queryDescription, Type? modelType)
        {
            Variables = new ScopedDictionary<string, ValueProviderBase>(null);
            ModelType = modelType;
            Replacements = replacements;
            StringDistance = stringDistance;
            QueryDescription = queryDescription;
            HasChanges = false;
        }

        internal void SynchronizeToken(ParsedToken parsedToken, string remainingText)
        {
            if (parsedToken.QueryToken == null)
            {
                string tokenString = parsedToken.String;

                if (tokenString.StartsWith("$"))
                {
                    string v = tokenString.TryBefore('.') ?? tokenString;

                    if (!Variables.TryGetValue(v, out ValueProviderBase prov))
                        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Variable '{0}' not found!".FormatWith(v));

                    var provToken = prov as TokenValueProvider;
                    if (!(provToken is TokenValueProvider))
                        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Variable '{0}' is not a Query Token");

                    var part = provToken?.ParsedToken; 

                    if (part != null && part.QueryToken == null)
                        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Variable '{0}' is not fixed yet! currently: '{1}'".FormatWith(v, part.String));

                    var after = tokenString.TryAfter('.');

                    tokenString =
                        (part == null ? "Unknown" :
                        part.QueryToken == null ? part.String :
                        part.QueryToken.FullKey()) + (after == null ? null : ("." + after));
                }

                SafeConsole.WriteColor(ConsoleColor.Red, "  " + tokenString);
                Console.WriteLine(" " + remainingText);

                FixTokenResult result = QueryTokenSynchronizer.FixToken(Replacements, tokenString, out QueryToken? token, QueryDescription, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll /*not always*/, remainingText, allowRemoveToken: false, allowReGenerate: ModelType != null);
                switch (result)
                {
                    case FixTokenResult.Nothing:
                    case FixTokenResult.Fix:
                        this.HasChanges = true;
                        parsedToken.QueryToken = token;
                        parsedToken.String = token!.FullKey();
                        break;
                    case FixTokenResult.SkipEntity:
                    case FixTokenResult.RemoveToken:
                     case FixTokenResult.ReGenerateEntity:
                        throw new TemplateSyncException(result);
                }
            }
        }

        public void SynchronizeValue(Type type, ref string? value, bool isList)
        {
            string? val = value;
            FixTokenResult result = QueryTokenSynchronizer.FixValue(Replacements, type, ref val, allowRemoveToken: false, isList: isList);
            switch (result)
            {
                case FixTokenResult.Fix:
                case FixTokenResult.Nothing:
                    value = val;
                    break;
                case FixTokenResult.SkipEntity:
                case FixTokenResult.RemoveToken:
                    throw new TemplateSyncException(result);
            }
        }


        internal List<MemberInfo>? GetMembers(string fieldOrPropertyChain, Type initialType)
        {
            List<MemberInfo> fields = new List<MemberInfo>();

            Type type = initialType;
            foreach (var field in fieldOrPropertyChain.Split('.'))
            {
                var allMembers = type.GetFields(ParsedModel.Flags).Cast<MemberInfo>().Concat(type.GetProperties(ParsedModel.Flags)).ToDictionary(a => a.Name);

                string? s = this.Replacements.SelectInteractive(field, allMembers.Keys, "Members {0}".FormatWith(type.FullName), this.StringDistance);

                if (s == null)
                    return null;

                var member = allMembers.GetOrThrow(s);

                fields.Add(member);

                type = member.ReturningType();
            }

            return fields;
        }

        public IDisposable NewScope()
        {
            Variables = new ScopedDictionary<string, ValueProviderBase>(Variables);

            return new Disposable(() => Variables = Variables.Previous!);
        }
    }

    public class TemplateSyncException : Exception
    {
        public FixTokenResult Result;

        public TemplateSyncException(FixTokenResult result)
        {
            this.Result = result;
        }
    }

}
