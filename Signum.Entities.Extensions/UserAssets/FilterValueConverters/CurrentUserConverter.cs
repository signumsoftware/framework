using Signum.Entities.Authorization;

namespace Signum.Entities.UserAssets
{
    public class CurrentUserConverter : IFilterValueConverter
    {
        static string CurrentUserKey = "[CurrentUser]";

        public Result<string?>? TryToStringValue(object? value, Type type)
        {
            if (value is Lite<UserEntity> lu && lu.Is(UserEntity.Current))
            {
                return new Result<string?>.Success(CurrentUserKey);
            }

            return null;
        }

        public Result<object?>? TryParseValue(string? value, Type type)
        {
            if (value.HasText() && value.StartsWith(CurrentUserKey))
            {
                string after = value.Substring(CurrentUserKey.Length).Trim();

                string[] parts = after.SplitNoEmpty('.');

                return SimpleMemberEvaluator.EvaluateExpression(UserEntity.Current, parts);
            }

            return null;
        }

      
    }

    static class SimpleMemberEvaluator
    {
        

        internal static Result<object?> EvaluateExpression(object? result, string[] parts)
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
    }
}

