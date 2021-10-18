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

                return SimpleMemberEvaluator.EvaluateExpression(currentEntityVariable.Value, parts);
            }

            return null;
        }
    }
}

