
namespace Signum.UserAssets;

public class CurrentEntityConverter : IFilterValueConverter
{
    public static string CurrentEntityKey = "[CurrentEntity]";

    static readonly AsyncThreadVariable<Entity?> currentEntityVariable = Statics.ThreadVariable<Entity?>("currentFilterValueEntity");

    public static IDisposable SetCurrentEntity(Entity? currentEntity)
    {
        if (currentEntity == null)
            throw new InvalidOperationException("currentEntity is null");

        var old = currentEntityVariable.Value;

        currentEntityVariable.Value = currentEntity;

        return new Disposable(() => currentEntityVariable.Value = old);
    }

    public Result<string?>? TryGetExpression(object? value, Type targetType)
    {
        if (value is Lite<Entity> lite && lite.Is(currentEntityVariable.Value))
        {
            return new Result<string?>.Success(CurrentEntityKey);
        }

        return null;
    }

    public Result<object?>? TryParseExpression(string? expression, Type targetType)
    {
        if (expression.HasText() && expression.StartsWith(CurrentEntityKey))
        {
            string after = expression.Substring(CurrentEntityKey.Length).Trim();

            string[] parts = after.SplitNoEmpty('.' );

            return SimpleMemberEvaluator.EvaluateExpression(currentEntityVariable.Value, parts);
        }

        return null;
    }

    public Result<Type>? IsValidExpression(string? expression, Type targetType, Type? currentEntityType)
    {
        if (expression.HasText() && expression.StartsWith(CurrentEntityKey))
        {
            currentEntityType = currentEntityType ?? typeof(Entity);

            string after = expression.Substring(CurrentEntityKey.Length).Trim();

            string[] parts = after.SplitNoEmpty('.');

            return SimpleMemberEvaluator.CheckExpression(currentEntityType, parts);
        }

        return null;
    }
}

