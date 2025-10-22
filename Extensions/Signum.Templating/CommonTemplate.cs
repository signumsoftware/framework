using Signum.Utilities.DataStructures;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Signum.Utilities.Reflection;
using Signum.UserAssets.QueryTokens;
using Signum.DynamicQuery.Tokens;
using Signum.Engine.Sync;
using Signum.UserAssets;

namespace Signum.Templating;

public static class TemplateUtils
{
    public static readonly Regex KeywordsRegex = new Regex(@"\@(((?<keyword>(foreach|if|raw|global|model|modelraw|any|declare|))\[(?<expr>(?>[^\[\]]+|\[(?<open>)|\](?<-open>))*(?(open)(?!))?)\](\s+as\s+(?<dec>\$\w*))?)|(?<keyword>endforeach|else|endif|notany|endany))");

    public static readonly Regex TokenOperationValueRegex = new Regex(@"(?<token>((?<type>[\w]):)?.+?)(?<operation>(" + FilterValueConverter.OperationRegex + @"))(?<value>[^\]]+)");

    public static readonly Regex TokenFormatRegex = new Regex(@"(?<token>((?<type>[\w]):)?((\[[^\[\]]+\])|([^\[\]\:]+))+)(\:(?<format>.*))?");
    
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
            Token = tok.Groups["token"].Value,
            Format = tok.Groups["format"].Value.DefaultText("").Replace(@"\:", ":").Replace(@"\]", "]").DefaultToNull()
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
            () => "No values for column {0}".FormatWith(column.Token.FullKey()),
            () => "Multiple values for column {0}".FormatWith(column.Token.FullKey()));
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
                .Select(fi => Signum.Utilities.Reflection.ReflectionTools.CreateGetter<object, object?>(fi)!).ToList());
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

public class MemberWithArguments
{
    public MemberWithArguments(MemberInfo member, ValueProviderBase[]? arguments = null)
    {
        Member = member;
        Arguments = arguments;
    }

    public MemberInfo Member { get; }
    public ValueProviderBase[]? Arguments { get; }

    public override string ToString() => ToString(new ScopedDictionary<string, ValueProviderBase>(null));

    public string ToString(ScopedDictionary<string, ValueProviderBase> variables)
    {
        return Member.Name + (Arguments == null ? null : "(" + Arguments.ToString(a => a.ToStringWithoutBrackets(variables), ", ") + ")");
    }
}

public static class ParsedModel
{
    public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    static readonly Regex Parenthesis = new Regex(@"(\([^)]*\))");

    public static List<MemberWithArguments>? GetMembers(Type modelType, string? fieldOrPropertyChain, ITemplateParser tp)
    {
        var members = new List<MemberWithArguments>();
        var parensDic = new List<string>();
        var replacedFieldOrPropertyChain = Parenthesis.Replace((fieldOrPropertyChain ?? "").Trim(), a =>
        {
            parensDic.Add(a.Value);
            return "($$" + (parensDic.Count - 1) + "$$)";
        });

        var type = modelType;
        foreach (var part in replacedFieldOrPropertyChain.SplitNoEmpty('.'))
        {
            if (part.EndsWith("$$)"))
            {
                var argumentsIndex = int.Parse(part.Between("($$", "$$)"));

                var parameterString = parensDic[argumentsIndex];

                var arguments = parameterString.TrimStart('(').TrimEnd(')').SplitNoEmpty(",").Select(arg => ValueProviderBase.TryParse(arg.Trim(), null, tp)).ToList();

                var methodName = part.Before("($$");

                var method = type.GetMethod(methodName.Trim(), Flags);

                if (method == null)
                {
                    tp.AddError(false, $"Type {type.Name} does not have a method with name {methodName}");
                    return null;
                }
                var miParameters = method.GetParameters();

                if(miParameters.Length == 0 || !typeof(TemplateParameters).IsAssignableFrom(miParameters.Last().ParameterType))
                {
                    tp.AddError(false, $"The method {methodName} in {type.Name} should have a {nameof(TemplateParameters)} as last argument".FormatWith(type.Name, part));
                    return null;
                }

                var errors = miParameters.Take(miParameters.Length - 1).ZipOrDefault(arguments, (p, a) => 
                    a == null ? $"The parameter {p.Name} ({p.ParameterType.TypeName()}) is not set for method {method.MethodSignature()}":
                    p == null ? $"Extra argument {a} in method {method.MethodSignature()}" :
                    !p.ParameterType.IsAssignableFrom(a.Type) ? $"Unable to assign the expression {a} ({a.Type!.TypeName()}) to the parameter {p.Name} ({p.ParameterType.TypeName()}) in {methodName}": 
                    null)
                    .NotNull().ToString("\n");

                if (errors.HasText())
                {
                    tp.AddError(false, errors);
                    return null;
                }

                members.Add(new MemberWithArguments(method, arguments.NotNull().ToArray()));

                type = method.ReturningType();
            }
            else
            {
                var info =
                    (type.GetField(part, Flags) is { } fi ? new MemberWithArguments(fi) : null) ??
                    (type.GetProperty(part, Flags) is { } pi ? new MemberWithArguments(pi) : null) ??
                    (type.GetMethod(part, Flags) is { } mi ? new MemberWithArguments(mi) : null) ??
                    (type.IsModifiableEntity() && MixinDeclarations.GetMixinDeclarations(type).Any(a => a.Name == part) ? new MemberWithArguments(MixinDeclarations.GetMixinDeclarations(type).SingleEx(a => a.Name == part)) : null);


                if (info == null)
                {
                    tp.AddError(false, "Type {0} does not have a property/field with name {1}, or a method that takes a TemplateParameters as an argument".FormatWith(type.Name, part));
                    return null;
                }
                
                if(info.Member is MethodInfo method)
                {
                    var miParameters = method.GetParameters();

                    if (miParameters.Length == 0 || !typeof(TemplateParameters).IsAssignableFrom(miParameters.Last().ParameterType))
                    {
                        tp.AddError(false, $"The method {method.Name} in {type.Name} should have a {nameof(TemplateParameters)} as last argument".FormatWith(type.Name, part));
                        return null;
                    }
                }

                members.Add(info);

                type = info.Member as Type ?? info.Member.ReturningType();
            }

        }

        return members;
    }
}

public class TemplateSynchronizationContext
{
    public ScopedDictionary<string, ValueProviderBase> Variables;
    public Type? ModelType;
    public Replacements Replacements;
    public StringDistance StringDistance;
    public QueryDescription? QueryDescription;

    public bool HasChanges;

    public TemplateSynchronizationContext(Replacements replacements, StringDistance stringDistance, QueryDescription? queryDescription, Type? modelType)
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
            if (this.QueryDescription == null)
                throw new InvalidOperationException("Unable to Sync token without QueryDescription: " + parsedToken);

            string tokenString = parsedToken.String;

            if (tokenString.StartsWith("$"))
            {
                string v = tokenString.TryBefore('.') ?? tokenString;

                if (!Variables.TryGetValue(v, out ValueProviderBase? prov))
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
                case FixTokenResult.RegenerateEntity:
                    throw new TemplateSyncException(result);
            }
        }
    }

    public void SynchronizeValue(Type targetType, ref string? value, bool isList, Type? currentEntityType)
    {
        string? val = value;
        FixTokenResult result = QueryTokenSynchronizer.FixValue(Replacements, targetType, ref val, allowRemoveToken: false, isList: isList, currentEntityType);
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


    internal List<MemberWithArguments>? GetMembers(string fieldOrPropertyChain, Type initialType, ref bool hasChanges)
    {
        hasChanges = false;
        List<MemberWithArguments> fields = new List<MemberWithArguments>();

        Type type = initialType;
        foreach (var field in fieldOrPropertyChain.Split('.'))
        {
            var allMembers = type.GetFields(ParsedModel.Flags).Where(f => !f.IsBackingField()).Cast<MemberInfo>()
                .Concat(type.GetProperties(ParsedModel.Flags))
                .Concat(type.IsModifiableEntity() && !type.IsAbstract ? MixinDeclarations.GetMixinDeclarations(type) : new HashSet<Type>())
                .ToDictionary(a => a.Name);

            string? s = this.Replacements.SelectInteractive(field, allMembers.Keys, "Members {0}".FormatWith(type.FullName), this.StringDistance);

            if (s == null)
            {
                hasChanges = true;
                return null;
            }

            if (s != field)
                hasChanges = true;

            var member = allMembers.GetOrThrow(s);

            fields.Add(new MemberWithArguments(member));

            type = member as Type ?? member.ReturningType();
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

