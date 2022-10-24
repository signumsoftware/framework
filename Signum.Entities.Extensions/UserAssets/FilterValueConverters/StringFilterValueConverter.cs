using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq.Expressions;

namespace Signum.Entities.UserAssets;

public class StringFilterValueConverter : IFilterValueConverter
{
   

    public Result<string?>? TryGetExpression(object? value, Type targetType)
    {
        if (value==null|| targetType != typeof(string))
            return null;


        return new Result<string?>.Success(value.ToString());
    }

    public Result<object?>? TryParseExpression(string? expression, Type targetType)
    {

        if (expression.IsNullOrEmpty()||targetType!= typeof (string))
            return null;

        return new Result<object?>.Success(expression);

    }

    public Result<Type>? IsValidExpression(string? expression, Type targetType, Type? currentEntityType)
    {


        if (expression.IsNullOrEmpty() || targetType != typeof(string))
            return null;

        return  new Result<Type>.Success(typeof(string)); 

    }
}

