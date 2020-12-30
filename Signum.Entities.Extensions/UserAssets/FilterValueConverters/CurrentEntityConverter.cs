using System;
using Signum.Utilities;
using System.Reflection;

namespace Signum.Entities.UserAssets
{
    public class CurrentEntityConverter : IFilterValueConverter
    {
        public static string CurrentEntityKey = "[CurrentEntity]";

        static readonly ThreadVariable<Entity?> currentEntityVariable = Statics.ThreadVariable<Entity?>("currentFilterValueEntity");

        public static IDisposable SetCurrentEntity(Entity? currentEntity)
        {
            if (currentEntity == null)
                throw new InvalidOperationException("currentEntity is null");

            var old = currentEntityVariable.Value;

            currentEntityVariable.Value = currentEntity;

            return new Disposable(() => currentEntityVariable.Value = old);
        }

        public Result<string?>? TryToStringValue(object? value, Type type)
        {
            if (value is Lite<Entity> lite && lite.Is(currentEntityVariable.Value))
            {
                return new Result<string?>.Success(CurrentEntityKey);
            }

            return null;
        }

        public Result<object?>? TryParseValue(string? value, Type type)
        {
            if (value.HasText() && value.StartsWith(CurrentEntityKey))
            {
                string after = value.Substring(CurrentEntityKey.Length).Trim();

                string[] parts = after.SplitNoEmpty('.' );

                object? result = currentEntityVariable.Value;

                if (result == null)
                    return new Result<object?>.Success(null);

                foreach (var part in parts)
                {
                    var prop = result.GetType().GetProperty(part, BindingFlags.Instance | BindingFlags.Public);

                    if (prop == null)
                        return new Result<object?>.Error("Property {0} not found on {1}".FormatWith(part, type.FullName));

                    result = prop.GetValue(result, null);

                    if (result == null)
                        return new Result<object?>.Success(null);
                }

                if (result is Entity e)
                    result = e.ToLite();

                return new Result<object?>.Success(result);
            }

            return null;
        }
    }
}

