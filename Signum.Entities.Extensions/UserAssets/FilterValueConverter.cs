using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using System.Globalization;
using Signum.Utilities;
using System.Collections;
using System.Collections.ObjectModel;

namespace Signum.Entities.UserAssets
{
    public static class FilterValueConverter
    {
        public static Dictionary<FilterType, List<IFilterValueConverter>> SpecificConverters = new Dictionary<FilterType, List<IFilterValueConverter>>()
        {
            { FilterType.DateTime, new List<IFilterValueConverter>{ new SmartDateTimeFilterValueConverter()} },
            { FilterType.Lite, new List<IFilterValueConverter>{ new CurrentUserConverter(), new CurrentEntityConverter(), new LiteFilterValueConverter() } },
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
            FilterType filterType = QueryUtils.GetFilterType(type);

            var converters = SpecificConverters.TryGetC(filterType);

            if (converters != null)
            {
                foreach (var fvc in converters)
                {
                    var r = fvc.TryToStringValue(value, type);
                    if (r != null)
                        return r;
                }
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

        public static Result<object?> TryParse(string? stringValue, Type type, bool isList)
        {
            if (isList && stringValue != null && stringValue.Contains('|'))
            {
                IList list = (IList)Activator.CreateInstance(typeof(ObservableCollection<>).MakeGenericType(type))!;
                foreach (var item in stringValue.Split('|'))
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
                return TryParseInternal(stringValue, type);
            }
        }

        private static Result<object?> TryParseInternal(string? stringValue, Type type)
        {
            FilterType filterType = QueryUtils.GetFilterType(type);

            List<IFilterValueConverter>? converters = SpecificConverters.TryGetC(filterType);

            if (converters != null)
            {
                foreach (var fvc in converters)
                {
                    var res = fvc.TryParseValue(stringValue, type);
                    if (res != null)
                        return res;
                }
            }

            if (ReflectionTools.TryParse(stringValue, type, CultureInfo.InvariantCulture, out var result))
                return new Result<object?>.Success(result);
            else
                return new Result<object?>.Error("Invalid format");
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
        Result<string?>? TryToStringValue(object? value, Type type);
        Result<object?>? TryParseValue(string? value, Type type);
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
}

