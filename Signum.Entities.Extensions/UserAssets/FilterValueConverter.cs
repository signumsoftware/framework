using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using System.Globalization;
using Signum.Utilities;
using System.Text.RegularExpressions;
using Signum.Entities.Authorization;
using System.Collections;
using System.Reflection;
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

    public class SmartDateTimeFilterValueConverter : IFilterValueConverter
    {
        public class SmartDateTimeSpan
        {
            const string part = @"^((\+\d+)|(-\d+)|(\d+))$";
            static Regex partRegex = new Regex(part);
            static Regex regex = new Regex(@"^(?<year>.+)/(?<month>.+)/(?<day>.+) (?<hour>.+):(?<minute>.+):(?<second>.+)$", RegexOptions.IgnoreCase);

            public string Year;
            public string Month;
            public string Day;
            public string Hour;
            public string Minute;
            public string Second;

            public static Result<SmartDateTimeSpan>? TryParse(string? str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return null;
                }

                Match match = regex.Match(str);
                if (!match.Success)
                    return new Result<SmartDateTimeSpan>.Error("Invalid Format: yyyy/mm/dd hh:mm:ss");

                var span = new SmartDateTimeSpan();

                string? error =
                    Assert(match, "year", "yyyy", 0, int.MaxValue, out span.Year) ??
                    Assert(match, "month", "mm", 1, 12, out span.Month) ??
                    Assert(match, "day", "dd", 1, 31,  out span.Day) ??
                    Assert(match, "hour", "hh", 0, 23, out span.Hour) ??
                    Assert(match, "minute", "mm", 0, 59, out span.Minute) ??
                    Assert(match, "second", "ss", 0, 59, out span.Second);

                if (error.HasText())
                    return new Result<SmartDateTimeSpan>.Error(error);

                return new Result<SmartDateTimeSpan>.Success(span);
            }

            static string? Assert(Match m, string groupName, string defaultValue, int minValue, int maxValue, out string result)
            {
                result = m.Groups[groupName].Value;
                if (string.IsNullOrEmpty(result))
                    return "{0} has no value".FormatWith(groupName);

                if (defaultValue == result)
                    return null;

                if (partRegex.IsMatch(result))
                {
                    if (result.StartsWith("+") || result.StartsWith("-"))
                        return null;

                    int val = int.Parse(result);
                    if (minValue <= val && val <= maxValue)
                        return null;

                    return "{0} must be between {1} and {2}".FormatWith(groupName, minValue, maxValue);
                }

                if(groupName == "day" && string.Equals(result, "max", StringComparison.InvariantCultureIgnoreCase))
                    return null;

                string options = new[] { defaultValue, "const", "+inc", "-dec", groupName == "day" ? "max" : null }.NotNull().Comma(" or ");

                return "'{0}' is not a valid {1}. Try {2} instead".FormatWith(result, groupName, options);
            }

            public DateTime ToDateTime()
            {
                DateTime now = TimeZoneManager.Now;

                int year = Mix(now.Year, Year, "yyyy");
                int month = Mix(now.Month, Month, "mm");
                int day;
                if (Day.ToLower() == "max")
                {
                    year += MonthDivMod(ref month);
                    day = DateTime.DaysInMonth(year, month);
                }
                else
                {
                    day = Mix(now.Day, Day, "dd");
                }
                int hour = Mix(now.Hour, Hour, "hh");
                int minute = Mix(now.Minute, Minute, "mm");
                int second = Mix(now.Second, Second, "ss");

                minute += second.DivMod(60, out second);
                hour += minute.DivMod(60, out minute);
                day += hour.DivMod(24, out hour);

                DateDivMod(ref year, ref month, ref day);

                return new DateTime(year, month, day, hour, minute, second);
            }

            private static void DateDivMod(ref int year, ref int month, ref int day)
            {
                year += MonthDivMod(ref month); // We need right month for DaysInMonth

                int daysInMonth;
                while (day > (daysInMonth = DateTime.DaysInMonth(year, month)))
                {
                    day -= daysInMonth;

                    month++;
                    year += MonthDivMod(ref month);
                }

                while (day <= 0)
                {
                    month--;
                    year += MonthDivMod(ref month);

                    day += DateTime.DaysInMonth(year, month);
                }
            }

            private static int MonthDivMod(ref int month)
            {
                int year = 0;

                while (12 < month)
                {
                    year++;
                    month -= 12;
                }

                while (month <= 0)
                {
                    year--;
                    month += 12;
                }

                return year;
            }

            static int Mix(int current, string rule, string pattern)
            {
                if (string.Equals(rule, pattern, StringComparison.InvariantCultureIgnoreCase))
                    return current;

                if (rule.StartsWith("+"))
                    return current + int.Parse(rule.Substring(1));
                if (rule.StartsWith("-"))
                    return current - int.Parse(rule.Substring(1));

                return int.Parse(rule);
            }

            public static SmartDateTimeSpan Substract(DateTime date, DateTime now)
            {
                var ss = new SmartDateTimeSpan
                {
                    Year = Diference(now.Year - date.Year, "yyyy") ?? date.Year.ToString("0000"),
                    Month = Diference(now.Month - date.Month, "mm") ?? date.Month.ToString("00"),
                    Day = date.Day == DateTime.DaysInMonth(date.Year, date.Month) ? "max" : (Diference(now.Day - date.Day, "dd") ?? date.Day.ToString("00")),
                };

                if (date == date.Date)
                {
                    ss.Hour = ss.Minute = ss.Second = "00";
                }
                else
                {
                    ss.Hour = Diference(now.Hour - date.Hour, "hh") ?? date.Hour.ToString("00");
                    ss.Minute = Diference(now.Minute - date.Minute, "mm") ?? date.Minute.ToString("00");
                    ss.Second = Diference(now.Second - date.Second, "ss") ?? date.Second.ToString("00");
                }

                return ss;
            }

            public static SmartDateTimeSpan Simple(DateTime date)
            {
                return new SmartDateTimeSpan
                {
                    Year = date.Year.ToString("0000"),
                    Month = date.Month.ToString("00"),
                    Day = date.Day.ToString("00"),
                    Hour = date.Hour.ToString("00"),
                    Minute = date.Minute.ToString("00"),
                    Second = date.Second.ToString("00")
                };
            }

            static string? Diference(int diference, string pattern)
            {
                if (diference == 0)
                    return pattern;
                if (diference == +1)
                    return "-1";
                if (diference == -1)
                    return "+1";
                return null;
            }

            public override string ToString()
            {
                return "{0}/{1}/{2} {3}:{4}:{5}".FormatWith(Year, Month, Day, Hour, Minute, Second);
            }


        }

        public Result<string?>? TryToStringValue(object? value, Type type)
        {
            if (value == null)
                return null;

            DateTime dateTime = (DateTime)value;

            SmartDateTimeSpan ss = SmartDateTimeSpan.Substract(dateTime, TimeZoneManager.Now);

            return new Result<string?>.Success(ss.ToString());
        }

        public Result<object?>? TryParseValue(string? value, Type type)
        {
            var res = SmartDateTimeSpan.TryParse(value);
            if (res == null)
                return null;

            if (res is Result<SmartDateTimeSpan>.Error e)
                return new Result<object?>.Error(e.ErrorText);

            return new Result<object?>.Success(((Result<SmartDateTimeSpan>.Success)res).Value.ToDateTime());
        }
    }

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

    public class CurrentUserConverter : IFilterValueConverter
    {
        static string CurrentUserKey = "[CurrentUser]";

        public Result<string?>? TryToStringValue(object? value, Type type)
        {
            if (value is Lite<UserEntity> lu && lu.EntityType == typeof(UserEntity) && lu.IdOrNull == UserEntity.Current.Id)
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

