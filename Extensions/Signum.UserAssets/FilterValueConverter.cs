using Signum.Utilities.Reflection;
using System.Globalization;
using System.Collections;
using System.Collections.ObjectModel;

namespace Signum.UserAssets;

public static class FilterValueConverter
{
    public static List<IFilterValueConverter> SpecificConverters = new List<IFilterValueConverter>()
    {
        new CurrentEntityConverter(), 
        new CurrentUserConverter(), 
        new SmartDateTimeFilterValueConverter(),
        new LiteFilterValueConverter(),
    };

    public static string? ToString(object? value, Type type)
    {
        if (value is IList)
            return ((IList)value).Cast<object>().ToString(o => ToStringElement(o, type), "|");

        return ToStringElement(value, type);
    }

    static string? ToStringElement(object? value, Type type)
    {
        var result = TryToStringElement(value, type);

        if (result is Result<string?>.Success s)
            return s.Value;

        throw new InvalidOperationException(((Result<string?>.Error)result).ErrorText);
    }

    static Result<string?> TryToStringElement(object? value, Type type)
    {
        foreach (var fvc in SpecificConverters)
        {
            var r = fvc.TryGetExpression(value, type);
            if (r != null)
                return r;
        }

        string? result =
            value == null ? null :
            value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) :
            value.ToString();

        return new Result<string?>.Success(result);
    }

    public static object? Parse(string? stringValue, Type type, bool isList)
    {
        var result = TryParse(stringValue, type, isList);
        if (result is Result<object?>.Error e)
            throw new FormatException(e.ErrorText);

        return ((Result<object?>.Success)result).Value;
    }

    public static Result<object?> TryParse(string? expression, Type type, bool isList)
    {
        if (isList)
        {
            IList list = (IList)Activator.CreateInstance(typeof(ObservableCollection<>).MakeGenericType(type))!;
            foreach (var item in (expression ?? "").SplitNoEmpty('|'))
            {
                var result = TryParseInternal(item.Trim(), type);
                if (result is Result<object?>.Error e)
                    return new Result<object?>.Error(e.ErrorText);

                list.Add(((Result<object?>.Success)result).Value);
            }
            return new Result<object?>.Success(list);
        }
        else
        {
            return TryParseInternal(expression, type);
        }
    }

    private static Result<object?> TryParseInternal(string? expression, Type targetType)
    {
        foreach (var fvc in SpecificConverters)
        {
            var res = fvc.TryParseExpression(expression, targetType);
            if (res != null)
            {
                if (res is Result<object?>.Success s)
                {
                    try
                    {
                        var v = ReflectionTools.ChangeType(s.Value, targetType);
                        return new Result<object?>.Success(v);
                    }
                    catch (Exception e)
                    {
                        return new Result<object?>.Error(e.Message);
                    }
                }
                else
                    return res;

            }
        }

        if (ReflectionTools.TryParse(expression, targetType, CultureInfo.InvariantCulture, out var result))
            return new Result<object?>.Success(result);
        else
            return new Result<object?>.Error($"Impossible to parse expression '${expression}' to ${targetType}");
    }

    public static Result<Type> IsValidExpression(string? expression, Type targetType, bool isList, Type? currentEntityType)
    {
        if (isList && expression != null && expression.Contains('|'))
        {
            List<Type> list = new List<Type>();
            foreach (var item in expression.Split('|'))
            {
                var result = IsValidExpression(item.Trim(), targetType, currentEntityType);
                if (result is Result<Type>.Error e)
                    return new Result<Type>.Error(e.ErrorText);

                list.Add(((Result<Type>.Success)result).Value);
            }
            return new Result<Type>.Success(list.Distinct().SingleEx());
        }
        else
        {
            return IsValidExpression(expression, targetType, currentEntityType);
        }
    }

    private static Result<Type> IsValidExpression(string? expression, Type targetType, Type? currentEntityType)
    {   
        foreach (var fvc in SpecificConverters)
        {
            var res = fvc.IsValidExpression(expression, targetType, currentEntityType);
            if (res != null)
            {
                if (res is Result<Type>.Success s)
                {
                    var v = ReflectionTools.CanChangeType(s.Value, targetType);
                    if (v)
                        return new Result<Type>.Success(s.Value);
                    else
                        return new Result<Type>.Error($"Impossible to convert from ${s.Value} to ${targetType}");

                }
                else
                    return res;

            }
        }

        if (ReflectionTools.TryParse(expression, targetType, CultureInfo.InvariantCulture, out var result))
            return new Result<Type>.Success(result?.GetType() ?? targetType);
        else
            return new Result<Type>.Error($"Impossible to parse expression '{expression}' to {targetType.TypeName()}");
    }

    public static FilterOperation ParseOperation(string operationString)
    {
        switch (operationString)
        {
            case "=":
            case "==": return FilterOperation.EqualTo;
            case "<=": return FilterOperation.LessThanOrEqual;
            case ">=": return FilterOperation.GreaterThanOrEqual;
            case "<": return FilterOperation.LessThan;
            case ">": return FilterOperation.GreaterThan;
            case "^=": return FilterOperation.StartsWith;
            case "$=": return FilterOperation.EndsWith;
            case "*=": return FilterOperation.Contains;
            case "%=": return FilterOperation.Like;

            case "!=": return FilterOperation.DistinctTo;
            case "!^=": return FilterOperation.NotStartsWith;
            case "!$=": return FilterOperation.NotEndsWith;
            case "!*=": return FilterOperation.NotContains;
            case "!%=": return FilterOperation.NotLike;
        }

        throw new InvalidOperationException("Unexpected Filter {0}".FormatWith(operationString));
    }

    public const string OperationRegex = @"==?|<=|>=|<|>|\^=|\$=|%=|\*=|\!=|\!\^=|\!\$=|\!%=|\!\*=";

    public static string ToStringOperation(FilterOperation operation)
    {
        switch (operation)
        {
            case FilterOperation.EqualTo: return "=";
            case FilterOperation.DistinctTo: return "!=";
            case FilterOperation.GreaterThan: return ">";
            case FilterOperation.GreaterThanOrEqual: return ">=";
            case FilterOperation.LessThan: return "<";
            case FilterOperation.LessThanOrEqual: return "<=";
            case FilterOperation.Contains: return "*=";
            case FilterOperation.StartsWith: return "^=";
            case FilterOperation.EndsWith: return "$=";
            case FilterOperation.Like: return "%=";
            case FilterOperation.NotContains: return "!*=";
            case FilterOperation.NotStartsWith: return "!^=";
            case FilterOperation.NotEndsWith: return "!$=";
            case FilterOperation.NotLike: return "!%=";
        }

        throw new InvalidOperationException("Unexpected Filter {0}".FormatWith(operation));
    }
}

public interface IFilterValueConverter
{
    Result<string?>? TryGetExpression(object? value, Type targetType);
    Result<object?>? TryParseExpression(string? expression, Type targetType);
    Result<Type>? IsValidExpression(string? expression, Type targetType, Type? currentEntityType);
}

public abstract class Result<T>
{
    public class Error : Result<T>
    {
        public string ErrorText { get; }
        public Error(string errorText)
        {
            this.ErrorText = errorText;
        }
    }

    public class Success : Result<T>
    {
        public T Value { get; }
        public Success(T value)
        {
            this.Value = value;
        }
    }
}

