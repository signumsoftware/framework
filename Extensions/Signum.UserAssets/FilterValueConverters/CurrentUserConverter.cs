using Signum.Authorization;
using Signum.Basics;

namespace Signum.UserAssets;

public class CurrentUserConverter : IFilterValueConverter
{
    static string CurrentUserKey = "[CurrentUser]";

    public static Func<UserEntity> GetCurrentUserEntity = ()=> throw new NotImplementedException("CurrentUserConverter.GetCurrentUserEntity")!;

    public Result<string?>? TryGetExpression(object? value, Type targetType)
    {
        if (value is Lite<UserEntity> lu && lu.Is(UserEntity.Current))
        {
            return new Result<string?>.Success(CurrentUserKey);
        }

        return null;
    }

    public Result<object?>? TryParseExpression(string? expression, Type targetType)
    {
        if (expression.HasText() && expression.StartsWith(CurrentUserKey))
        {
            string after = expression.Substring(CurrentUserKey.Length).Trim();

            string[] parts = after.SplitNoEmpty('.');

            if (parts.Length == 0)
                return new Result<object?>.Success(UserEntity.Current);

            return SimpleMemberEvaluator.EvaluateExpression(GetCurrentUserEntity(), parts);
        }

        return null;
    }

    public Result<Type>? IsValidExpression(string? expression, Type targetType, Type? currentEntityType)
    {
        if (expression.HasText() && expression.StartsWith(CurrentUserKey))
        {
            string after = expression.Substring(CurrentUserKey.Length).Trim();

            string[] parts = after.SplitNoEmpty('.');

            if (parts.Length == 0)
                return new Result<Type>.Success(typeof(Lite<UserEntity>));

            return SimpleMemberEvaluator.CheckExpression(typeof(UserEntity), parts);
        }

        return null;
    }
}

public static class SimpleMemberEvaluator
{
    public static Result<object?> EvaluateExpression(object? result, string[] parts)
    {
        if (result == null)
            return new Result<object?>.Success(null);

        foreach (var part in parts)
        {
            if (part.StartsWith("[") && part.EndsWith("]"))
            {
                var mixinName = part.Between("[", "]");

                var mixin = ((ModifiableEntity)result).TryMixin(mixinName);

                if (mixin == null)
                    return new Result<object?>.Error("Mixin {0} not found on {1}".FormatWith(mixinName, result.GetType().FullName));

                result = mixin;

                if (result == null)
                    return new Result<object?>.Success(null);
            }
            else if (part.StartsWith("(") && part.EndsWith(")"))
            {
                var typeName = part.Between("(", ")");

                var asType = TypeEntity.TryGetType(typeName);

                if (asType == null)
                    return new Result<object?>.Error("Type {0} not found on {1}".FormatWith(typeName, result.GetType().FullName));

                if (!asType.IsAssignableFrom(result.GetType()))
                    return new Result<object?>.Error("Type {0} is not assignable from {1}".FormatWith(typeName, result.GetType().FullName));

                if (result == null)
                    return new Result<object?>.Success(null);
            }
            else
            {
                if (result.GetType().GetProperty(part, BindingFlags.Instance | BindingFlags.Public) is { } prop)
                    result = prop.GetValue(result, null);
                else if (result.GetType().GetMethod(part, BindingFlags.Instance | BindingFlags.Public) is { } method)
                    result = method.Invoke(result, null);
                else
                    return new Result<object?>.Error("Property or Method {0} not found on {1}".FormatWith(part, result.GetType().FullName));

                if (result == null)
                    return new Result<object?>.Success(null);
            }
        }

        if (result is Entity e)
            result = e.ToLite();

        return new Result<object?>.Success(result);
    }

    internal static Result<Type> CheckExpression(Type type, string[] parts)
    {
        var currentType = type;

        foreach (var part in parts)
        {
            if (part.StartsWith("[") && part.EndsWith("]"))
            {
                var mixinName = part.Between("[", "]");

                var mixin = MixinDeclarations.GetMixinDeclarations(currentType).SingleOrDefault(a => a.Name == mixinName);

                if (mixin == null)
                    return new Result<Type>.Error("Mixin {0} not found on {1}".FormatWith(mixinName, currentType));

                currentType = mixin;
            }
            else if (part.StartsWith("(") && part.EndsWith(")"))
            {
                var typeName = part.Between("(", ")");

                var asType = TypeEntity.TryGetType(typeName);

                if (asType == null)
                    return new Result<Type>.Error("Type {0} not found on {1}".FormatWith(typeName, currentType));

                currentType = asType;
            }
            else
            {
                if (currentType.GetProperty(part, BindingFlags.Instance | BindingFlags.Public) is { } prop)
                    currentType = prop.PropertyType;
                else if (currentType.GetMethod(part, BindingFlags.Instance | BindingFlags.Public) is { } method)
                    currentType = method.ReturnType;
                else
                    return new Result<Type>.Error("Property or Method {0} not found on {1}".FormatWith(part, type.FullName));
            }
        }

        if (currentType.IsEntity())
            currentType = Lite.Generate(currentType);

        return new Result<Type>.Success(currentType);
    }
}

