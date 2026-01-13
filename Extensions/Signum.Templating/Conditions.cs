using Signum.DynamicQuery.Tokens;
using Signum.Engine.Sync;
using Signum.UserAssets;
using Signum.UserAssets.QueryTokens;
using Signum.Utilities.DataStructures;
using System.Collections;

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

    public virtual IEnumerable<object> GetFilteredRows(TemplateParameters p)
    {
        var filter = this.GetResultFilter(p);

        var filtered = p.QueryContext!.CurrentRows.Where(filter).ToList();

        return filtered;
    }

    public abstract Func<ResultRow, bool> GetResultFilter(TemplateParameters p);
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

    public override Func<ResultRow, bool> GetResultFilter(TemplateParameters p)
    {
        var left = LeftNode.GetResultFilter(p);
        var right = RightNode.GetResultFilter(p);

        return rr => left(rr) && right(rr);
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

    public override Func<ResultRow, bool> GetResultFilter(TemplateParameters p)
    {
        var left = LeftNode.GetResultFilter(p);
        var right = RightNode.GetResultFilter(p);

        return rr => left(rr) || right(rr);
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

        if (obj is bool)
            return ((bool)obj);

        if (obj is string)
            return ((string)obj) != "";

        return true;
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {
        this.ValueProvider!.Synchronize(sc, remainingText, false);

        if (Operation != null)
        {
        retry:
            string? val = Value;
            FixTokenResult result = QueryTokenSynchronizer.FixValue(sc.Replacements, this.ValueProvider!.Type!, ref val, allowRemoveToken: false, isList: Operation.Value.IsList(), fixInstead: true, null);
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
    public override IEnumerable<object> GetFilteredRows(TemplateParameters p)
    {
        if (this.ValueProvider is TokenValueProvider)
        {
            return base.GetFilteredRows(p);
        }
        else
        {
            var collection = (IEnumerable)this.ValueProvider!.GetValue(p)!;

            return collection.Cast<object>();
        }
    }

    public override Func<ResultRow, bool> GetResultFilter(TemplateParameters p)
    {
        var tvp = (TokenValueProvider)ValueProvider!;

        if (Operation == null)
        {
            var column = p.QueryContext!.ResultColumns[tvp.ParsedToken.QueryToken!];

            return r => ToBool(r[column]);
        }
        else
        {
            var type = this.ValueProvider!.Type!;

            object? val = FilterValueConverter.Parse(Value, type, Operation.Value.IsList());

            Expression value = Expression.Constant(val, type);

            ResultColumn col = p.QueryContext!.ResultColumns[tvp.ParsedToken.QueryToken!];

            var expression = Signum.Utilities.ExpressionTrees.Linq.Expr((ResultRow rr) => rr[col]);

            Expression newBody = QueryUtils.GetCompareExpression(Operation.Value, Expression.Convert(expression.Body, type), value, inMemory: true);
            var lambda = Expression.Lambda<Func<ResultRow, bool>>(newBody, expression.Parameters).Compile();

            return lambda;
        }
    }
}
