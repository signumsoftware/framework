using System;
using Signum.Utilities;

namespace Signum.Entities.UserAssets
{
    public class LiteFilterValueConverter : IFilterValueConverter
    {
        public Result<string?>? TryToStringValue(object? value, Type type)
        {
            if (!(value is Lite<Entity> lite))
            {
                return null;
            }

            return new Result<string?>.Success(lite.Key());
        }

        public Result<object?>? TryParseValue(string? value, Type type)
        {
            if (!value.HasText())
                return null;

            string? error = Lite.TryParseLite(value, out Lite<Entity>? lite);
            if (error == null)
                return new Result<object?>.Success(lite);
            else
                return new Result<object?>.Error(error);
        }
    }
}

