using System;
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
            if (value == CurrentUserKey)
            {
                return new Result<object?>.Success(UserEntity.Current?.ToLite());
            }

            return null;
        }
    }
}

