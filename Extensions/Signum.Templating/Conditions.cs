using Signum.DynamicQuery.Tokens;
using Signum.UserAssets;
using Signum.UserAssets.QueryTokens;
using Signum.Utilities.DataStructures;
using System.Collections;
using System.Text.RegularExpressions;

namespace Signum.Templating;

public abstract class ConditionBase
{
    public abstract ConditionBase Clone();

    public abstract void FillQueryTokens(List<QueryToken> tokens);

    public abstract bool Evaluate(TemplateParameters p);

    public abstract void Synchronize(TemplateSynchronizationContext sc, string remainingText);

    public abstract void Declare(ScopedDictionary<string, ValueProviderBase> variables);
    
    public override string ToString() => ToString(new ScopedDictionary<string, ValueProviderBase>(null));

    public string ToString(ScopedDictionary<string, ValueProviderBase> variables)
    {
        StringBuilder sb = new StringBuilder();
        this.ToStringBrackets(sb, variables);
        return sb.ToString();
    }
    
    public virtual void ToStringBrackets(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        sb.Append("[");
        this.ToStringInternal(sb, variables);
        sb.Append("]");
    }

    public abstract void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables);

}

public enum QuantifierKind
{
    Any,
    All,
}

/// <summary>
/// How the elements of the collection reach the template. A model collection is simply there; a query one has
/// to be asked for, either by joining it in (Element, which multiplies the rows) or as a correlated sub-query
/// (Nested, which keeps one row and a nested table).
/// </summary>
public enum QuantifierStrategy
{
    InMemory,
    Element,
    Nested,
}

/// <summary>
/// A quantifier over a collection, written as a lambda so that its body can speak about the element:
///
///     @if[m:Entity.Lines.Any($l => $l.Discount &gt; 0)]
///     @if[Lines.AnyElement($l => $l.Discount &gt; 0 || $l.Discount &lt; 0)]
///     @if[Lines.AllNested($l => $l.Discount != null)]
///
/// Unlike a comparison it answers about the collection as a whole, so it can be combined with other
/// conditions, which is what the old @any block could not do.
/// </summary>
public class ConditionQuantifier : ConditionBase
{
    public readonly QuantifierKind Kind;
    public readonly QuantifierStrategy Strategy;
    public readonly ValueProviderBase? Collection;
    public readonly ConditionBase? Body;

    public ConditionQuantifier(QuantifierKind kind, QuantifierStrategy strategy, ValueProviderBase? collection, ConditionBase? body)
    {
        Kind = kind;
        Strategy = strategy;
        Collection = collection;
        Body = body;
    }

    public static readonly Regex QuantifierRegex = new Regex(@"^(?<token>.+?)\.(?<kind>Any|All)(?<strategy>Element|Nested)?\s*\(", RegexOptions.Compiled);

    /// <summary>
    /// Parses "Collection.AnyElement($l =&gt; ...)". Returns null when the expression is not a quantifier at
    /// all, so that the caller can go on trying a plain comparison.
    /// </summary>
    public static ConditionQuantifier? TryParse(string expr, ITemplateParser parser)
    {
        var m = QuantifierRegex.Match(expr);
        if (!m.Success)
            return null;

        var open = m.Index + m.Length - 1;
        var close = TemplateUtils.IndexOfClosingParenthesis(expr, open);

        //Only a quantifier that spans the whole condition; anything else is a comparison that happens to
        //contain the word Any, and belongs to the regular path.
        if (close != expr.Length - 1)
            return null;

        var kind = m.Groups["kind"].Value == "All" ? QuantifierKind.All : QuantifierKind.Any;
        var strategy = m.Groups["strategy"].Value switch
        {
            "Element" => QuantifierStrategy.Element,
            "Nested" => QuantifierStrategy.Nested,
            _ => QuantifierStrategy.InMemory,
        };

        var inner = expr.Substring(open + 1, close - open - 1).Trim();
        var arrow = TemplateUtils.IndexOfTopLevel(inner, "=>");

        if (arrow < 0)
        {
            parser.AddError(false, $"'{expr}' needs a lambda, like {m.Groups["token"].Value}.{m.Groups["kind"].Value}{m.Groups["strategy"].Value}($l => $l.Something > 0)");
            return new ConditionQuantifier(kind, strategy, null, null);
        }

        var parameter = inner.Substring(0, arrow).Trim();
        var body = inner.Substring(arrow + 2).Trim();

        if (!Regex.IsMatch(parameter, @"^\$?\w+$"))
        {
            parser.AddError(false, $"'{parameter}' is not a valid parameter name in '{expr}'");
            return new ConditionQuantifier(kind, strategy, null, null);
        }

        //The body refers to the element through the parameter, which is a variable like the one @foreach
        //declares, so it is written with a $ whether or not the author typed one.
        var variable = parameter.StartsWith("$") ? parameter : "$" + parameter;

        var tokenString = m.Groups["token"].Value.Trim();
        var collectionString = strategy switch
        {
            QuantifierStrategy.Element => tokenString + ".Element",
            QuantifierStrategy.Nested => tokenString + ".Nested",
            _ => tokenString,
        };

        var collection = ValueProviderBase.TryParse(collectionString, variable, parser);

        if (collection != null)
        {
            collection.IsForeach = true;

            if (strategy == QuantifierStrategy.InMemory && collection is TokenValueProvider)
                parser.AddError(false, $"'{tokenString}' is a query token, so it needs {m.Groups["kind"].Value}Element or {m.Groups["kind"].Value}Nested instead of {m.Groups["kind"].Value}");

            if (strategy != QuantifierStrategy.InMemory && collection is not TokenValueProvider)
                parser.AddError(false, $"'{tokenString}' is not a query token, so it needs {m.Groups["kind"].Value} instead of {m.Groups["kind"].Value}{m.Groups["strategy"].Value}");
        }

        //The parameter is only in scope inside the body
        using (parser.NewVariableScope())
        {
            if (collection != null && !parser.Variables.ContainsKey(variable))
                parser.Variables.Add(variable, collection);

            var bodyCondition = TemplateUtils.ParseCondition(body, "", parser);

            return new ConditionQuantifier(kind, strategy, collection, bodyCondition);
        }
    }

    public override ConditionBase Clone() => new ConditionQuantifier(Kind, Strategy, Collection, Body?.Clone());

    public override bool Evaluate(TemplateParameters p)
    {
        if (Collection == null || Body == null)
            return false;

        var any = false;
        var all = true;

        Collection.Foreach(p, () =>
        {
            if (Body.Evaluate(p))
                any = true;
            else
                all = false;
        });

        return Kind == QuantifierKind.Any ? any : all;
    }

    public override void Declare(ScopedDictionary<string, ValueProviderBase> variables)
    {
        return;
    }

    public override void FillQueryTokens(List<QueryToken> tokens)
    {
        Collection?.FillQueryTokens(tokens, forForeach: true);
        Body?.FillQueryTokens(tokens);
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {
        Collection?.Synchronize(sc, remainingText, false);

        using (sc.NewScope())
        {
            Collection?.Declare(sc.Variables);

            Body?.Synchronize(sc, remainingText);
        }
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        var token = Collection?.ToString(variables, null) ?? "";

        //The strategy lives in the token while it is parsed and in the name while it is written
        if (Strategy != QuantifierStrategy.InMemory)
            token = token.TryBeforeLast('.') ?? token;

        sb.Append(token);
        sb.Append('.');
        sb.Append(Kind.ToString());
        if (Strategy != QuantifierStrategy.InMemory)
            sb.Append(Strategy.ToString());
        sb.Append('(');
        sb.Append(Collection?.Variable);
        sb.Append(" => ");

        var scoped = new ScopedDictionary<string, ValueProviderBase>(variables);
        if (Collection?.Variable != null)
            scoped.Add(Collection.Variable, Collection);

        Body?.ToStringInternal(sb, scoped);
        sb.Append(')');
    }

}

public class ConditionAnd : ConditionBase
{
    public ConditionAnd(ConditionBase leftNode, ConditionBase rightNode)
    {
        LeftNode = leftNode;
        RightNode = rightNode;
    }

    public ConditionBase LeftNode { get; private set; }
    public ConditionBase RightNode { get; private set; }

    public override ConditionBase Clone() => new ConditionAnd(LeftNode, RightNode);

    public override bool Evaluate(TemplateParameters p)
    {
        return this.LeftNode.Evaluate(p) && this.RightNode.Evaluate(p);
    }

    public override void Declare(ScopedDictionary<string, ValueProviderBase> variables)
    {
        return;
    }

    public override void FillQueryTokens(List<QueryToken> tokens)
    {
        this.LeftNode.FillQueryTokens(tokens);
        this.RightNode.FillQueryTokens(tokens);
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {
        this.LeftNode.Synchronize(sc, remainingText);
        this.RightNode.Synchronize(sc, remainingText);
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        this.LeftNode.ToStringInternal(sb, variables);
        sb.Append(" && ");
        this.RightNode.ToStringInternal(sb, variables);
    }

}

public class ConditionOr : ConditionBase
{
    public ConditionOr(ConditionBase leftNode, ConditionBase rightNode)
    {
        LeftNode = leftNode;
        RightNode = rightNode;
    }

    public ConditionBase LeftNode { get; private set; }
    public ConditionBase RightNode { get; private set; }

    public override ConditionBase Clone() => new ConditionOr(LeftNode, RightNode);

    public override bool Evaluate(TemplateParameters p)
    {
        return this.LeftNode.Evaluate(p) || this.RightNode.Evaluate(p);
    }

    public override void Declare(ScopedDictionary<string, ValueProviderBase> variables)
    {
        return;
    }
    public override void FillQueryTokens(List<QueryToken> tokens)
    {
        this.LeftNode.FillQueryTokens(tokens);
        this.RightNode.FillQueryTokens(tokens);
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {
        this.LeftNode.Synchronize(sc, remainingText);
        this.RightNode.Synchronize(sc, remainingText);
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        this.LeftNode.ToStringInternal(sb, variables);
        sb.Append(" || ");
        this.RightNode.ToStringInternal(sb, variables);
    }

}

public class ConditionCompare : ConditionBase
{
    public readonly ValueProviderBase? ValueProvider;
    private FilterOperation? Operation;
    private string? Value;

    public ConditionCompare(ValueProviderBase? valueProvider)
    {
        this.ValueProvider = valueProvider;
    }

    public ConditionCompare(ValueProviderBase? valueProvider, string operation, string value, Action<bool, string> addError)
    {
        this.ValueProvider = valueProvider;
        this.Operation = FilterValueConverter.ParseOperation(operation);
        this.Value = value;

        ValueProvider?.ValidateConditionValue(value, Operation, addError);
    }

    public ConditionCompare(ConditionCompare other)
    {
        this.ValueProvider = other.ValueProvider;
        this.Operation = other.Operation;
        this.Value = other.Value;
    }

    public override ConditionBase Clone() => new ConditionCompare(this);

    public override void FillQueryTokens(List<QueryToken> tokens)
    {
        this.ValueProvider!.FillQueryTokens(tokens, forForeach: false);
    }

    public override bool Evaluate(TemplateParameters p)
    {
        var obj = ValueProvider!.GetValue(p);

        if (Operation == null)
            return ToBool(obj);
        else
        {
            var type = this.ValueProvider.Type!;

            Expression token = Expression.Constant(obj, type);

            Expression value = Expression.Constant(FilterValueConverter.Parse(Value, type, Operation.Value.IsList()), type);

            Expression newBody = QueryUtils.GetCompareExpression(Operation.Value, token, value, inMemory: true);
            var lambda = Expression.Lambda<Func<bool>>(newBody).Compile();

            return lambda();
        }
    }


    protected static bool ToBool(object? obj)
    {
        if (obj == null)
            return false;

        if (obj is bool b)
            return b;

        if (obj is string str)
            return str != "";

        if (obj is ResultTable rt)
            return rt.Rows.Length > 0;

        return true;
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {
        this.ValueProvider!.Synchronize(sc, remainingText, false);

        if (Operation != null)
        {
        retry:
            string? val = Value;
            var qKey = sc.QueryDescription != null ? Signum.DynamicQuery.QueryUtils.GetKey(sc.QueryDescription.QueryName) : "";
            var tokStr = this.ValueProvider!.ToString() ?? "";
            FixTokenResult result = QueryTokenSynchronizer.FixValue(sc.TokenSync, qKey, tokStr, this.ValueProvider!.Type!, ref val, allowRemoveToken: false, isListOrPair: Operation.Value.IsListOrPair(), fixInstead: true, null);
            switch (result)
            {
                case FixTokenResult.Fix:
                case FixTokenResult.Nothing:
                    Value = val;
                    break;
                case FixTokenResult.SkipEntity:
                case FixTokenResult.RemoveToken:
                    throw new TemplateSyncException(result);
                case FixTokenResult.FixTokenInstead:
                    {
                        this.ValueProvider!.Synchronize(sc, remainingText, forceChange: false);
                        goto retry;
                    }
                case FixTokenResult.FixOperationInstead:
                    {
                        var newOperation = SafeConsole.AskMultiLine($"New filter operation for: {ValueProvider.ToString()} {Operation} {Value}?", EnumEntity.GetValues(typeof(FilterOperation)).Select(a => a.ToString()).ToArray());
                        if (newOperation != null)
                            Operation = Enum.Parse<FilterOperation>(newOperation);
                        goto retry;
                    }
            }

        }
    }

    public override void Declare(ScopedDictionary<string, ValueProviderBase> variables)
    {
        this.ValueProvider!.Declare(variables);
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        this.ValueProvider!.ToStringInternal(sb, variables);
        if (this.Operation != null)
            sb.Append(FilterValueConverter.ToStringOperation(Operation.Value) + Value);
    }

    public override void ToStringBrackets(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        sb.Append("[");
        this.ToStringInternal(sb, variables);
        sb.Append("]");
        if (this.ValueProvider!.Variable.HasText())
            sb.Append(" as " + this.ValueProvider.Variable);
    }
    Func<object?, bool>? elementPredicate;

    Func<object?, bool> GetElementPredicate()
    {
        if (elementPredicate != null)
            return elementPredicate;

        var elementType = ValueProviderBase.CollectionElementTypeOrNull(this.ValueProvider!.Type!)
            ?? throw new InvalidOperationException($"{this.ValueProvider.Type} is not a collection");

        var param = Expression.Parameter(typeof(object), "e");

        var token = Expression.Convert(param, elementType);
        var value = Expression.Constant(FilterValueConverter.Parse(Value, elementType, Operation!.Value.IsList()), elementType);

        var body = QueryUtils.GetCompareExpression(Operation.Value, token, value, inMemory: true);

        return elementPredicate = Expression.Lambda<Func<object?, bool>>(body, param).Compile();
    }

}
