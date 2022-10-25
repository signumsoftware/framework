
namespace Signum.Entities.UserAssets;

public class LiteFilterValueConverter : IFilterValueConverter
{


    public Result<string?>? TryGetExpression(object? value, Type targetType)
    {
        if (!targetType.IsLite())
            return null;

        if (!(value is Lite<Entity> lite))
            return null;

        return new Result<string?>.Success(lite.Key());
    }

    public Result<object?>? TryParseExpression(string? expression, Type targetType)
    {
        if (!expression.HasText())
            return null;

        if (!targetType.IsLite())
            return null;

        string? error = Lite.TryParseLite(expression, out Lite<Entity>? lite);
        if (error == null)
            return new Result<object?>.Success(lite);
        else
            return new Result<object?>.Error(error);
    }

    public Result<Type>? IsValidExpression(string? expression, Type targetType, Type? currentEntityType)
    {
        if (!expression.HasText())
            return null;

        if (!targetType.IsLite())
            return null;

        string? error = Lite.TryParseLite(expression, out Lite<Entity>? lite);
        if (error == null)
            return new Result<Type>.Success(Lite.Generate(lite!.EntityType));
        else
            return new Result<Type>.Error(error);
    }
}

