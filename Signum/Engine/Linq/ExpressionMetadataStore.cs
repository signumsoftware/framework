using System.Runtime.CompilerServices;

namespace Signum.Engine.Linq;

public static class ExpressionMetadataStore
{
    [ThreadStatic]
    public static ConditionalWeakTable<Expression, ExpressionMetadata>? Metadata;

    public static IDisposable Scope()
    {
        var old = Metadata;
        Metadata = new ConditionalWeakTable<Expression, ExpressionMetadata>();
        return new Disposable(() => Metadata = old);
    }

    public static ExpressionMetadata? GetMetadata(this Expression ex)
    {
        if (Metadata!.TryGetValue(ex, out var md))
            return md;

        return null;
    }

    public static T SetMetadata<T>(this T ex, ExpressionMetadata md)
        where T : Expression
    {
        Metadata!.Add(ex, md);

        return ex;
    }

    public static T CopyMetadata<T>(this T ex, Expression? from)
        where T : Expression
    {
        var md = from?.GetMetadata();
        if (md == null)
            return ex;
        return ex.SetMetadata(md);
    }

    public static void ShareMetadata(Expression left, Expression right)
    {
        (left as ConstantExpression)?.CopyMetadataIfNeeded(right);
        (right as ConstantExpression)?.CopyMetadataIfNeeded(left);
    }

    public static T CopyMetadataIfNeeded<T>(this T ex, Expression? from)
        where T : Expression
    {
        var md = from?.GetMetadata();
        if (md == null)
            return ex;

        var already = ex.GetMetadata();
        if (already == null)
            return ex.SetMetadata(md);

        if (already.Equals(md))
            return ex;

        throw new Exception("ExpressionMetadata discrepancies");
    }

    public static Expression NullifyWithMetadata(this Expression e)
    {
        var result = e.Nullify();
        if (result == e)
            return e;

        return result.CopyMetadata(e);
    }

    public static Expression UnNullifyWithMetadata(this Expression e)
    {
        var result = e.UnNullify();
        if (result == e)
            return e;

        return result.CopyMetadata(e);
    }
}

public record ExpressionMetadata(DateTimeKind DateTimeKind)
{
    public static readonly ExpressionMetadata UTC = new ExpressionMetadata(DateTimeKind.Utc);
    public static readonly ExpressionMetadata Local = new ExpressionMetadata(DateTimeKind.Local);

}

