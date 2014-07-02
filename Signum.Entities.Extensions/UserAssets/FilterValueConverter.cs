using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using System.Globalization;
using Signum.Utilities;
using Signum.Entities;
using System.Text.RegularExpressions;
using Signum.Entities.Reflection;
using Signum.Services;
using Signum.Entities.Authorization;
using System.Collections;
using System.Reflection;

namespace Signum.Entities.UserAssets
{
    public static class FilterValueConverter
    {
        public const string Continue = "__Continue__";

        public static Dictionary<FilterType, List<IFilterValueConverter>> SpecificFilters = new Dictionary<FilterType, List<IFilterValueConverter>>()
        {
            {FilterType.DateTime, new List<IFilterValueConverter>{ new SmartDateTimeFilterValueConverter()} },
            {FilterType.Lite, new List<IFilterValueConverter>{ new CurrentUserConverter(), new CurrentEntityConverter(), new LiteFilterValueConverter() } },
        };

        public static string ToString(object value, Type type)
        {
            if (value is IList)
                return ((IList)value).Cast<object>().ToString(o => ToStringElement(o, type), "|");

            return ToStringElement(value, type);
        }

        static string ToStringElement(object value, Type type)
        {
            string result;
            string error = TryToString(value, type, out result);
            if (error == null)
                return result;
            throw new InvalidOperationException(error);
        }

        static string TryToString(object value, Type type, out string result)
        {
            FilterType filterType = QueryUtils.GetFilterType(type);

            var list = SpecificFilters.TryGetC(filterType);

            if (list != null)
            {
                foreach (var fvc in list)
                {
                    string error = fvc.TryToString(value, type, out result);
                    if(error != Continue)
                        return error;
                }
            }

            if (value == null)
                result = null;
            else if (value is IFormattable)
                result = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            else
                result = value.ToString();

            return null;
        }

        public static object Parse(string stringValue, Type type, bool isList)
        {
            object result;
            string error = TryParse(stringValue, type, out result, isList);
            if (error.HasText())
                throw new FormatException(error);

            return result;
        }

        public static string TryParse(string stringValue, Type type, out object result, bool isList)
        {
            if (isList && stringValue != null && stringValue.Contains('|'))
            {
                IList list = (IList)Activator.CreateInstance(typeof(MList<>).MakeGenericType(type));
                result = list;
                foreach (var item in stringValue.Split('|'))
                {
                    object element;
                    string error = TryParseInternal(item.Trim(), type, out element);
                    if (error.HasText())
                        return error;

                    list.Add(element);
                }
                return null;
            }
            else
            {
                return TryParseInternal(stringValue, type, out result);
            }
        }

        private static string TryParseInternal(string stringValue, Type type, out object result)
        {
            FilterType filterType = QueryUtils.GetFilterType(type);

            List<IFilterValueConverter> filters = SpecificFilters.TryGetC(filterType);

            if (filters != null)
            {
                foreach (var fvc in filters)
                {
                    string error = fvc.TryParse(stringValue, type, out result);
                    if (error != Continue)
                        return error;
                }
            }

            if (ReflectionTools.TryParse(stringValue, type, CultureInfo.InvariantCulture, out result))
                return null;
            else
            {
                return "Invalid format";
            }
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

            throw new InvalidOperationException("Unexpected Filter {0}".Formato(operationString));
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

            throw new InvalidOperationException("Unexpected Filter {0}".Formato(operation));
        }
    }

    public interface IFilterValueConverter
    {
        string TryToString(object value, Type type, out string result);
        string TryParse(string value, Type type, out object result);
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

            public static string TryParse(string str, out SmartDateTimeSpan result)
            {
                if (string.IsNullOrEmpty(str))
                {
                    result = null;
                    return FilterValueConverter.Continue;
                }

                Match match = regex.Match(str);
                if (!match.Success)
                {
                    result = null;
                    return "Invalid Format: yyyy/mm/dd hh:mm:ss";
                }

                result = new SmartDateTimeSpan();

                return
                    Assert(match, "year", "yyyy", 0, int.MaxValue, out result.Year) ??
                    Assert(match, "month", "mm", 1, 12, out result.Month) ??
                    Assert(match, "day", "dd", 1, 31,  out result.Day) ??
                    Assert(match, "hour", "hh", 0, 23, out result.Hour) ??
                    Assert(match, "minute", "mm", 0, 59, out result.Minute) ??
                    Assert(match, "second", "ss", 0, 59, out result.Second);
            }

            static string Assert(Match m, string groupName, string defaultValue, int minValue, int maxValue, out string result)
            {
                result = m.Groups[groupName].Value;
                if (string.IsNullOrEmpty(result))
                    return "{0} has no value".Formato(groupName);

                if (defaultValue == result)
                    return null;

                if (partRegex.IsMatch(result))
                {
                    if (result.StartsWith("+") || result.StartsWith("-"))
                        return null;

                    int val = int.Parse(result);
                    if (minValue <= val && val <= maxValue)
                        return null;

                    return "{0} must be between {1} and {2}".Formato(groupName, minValue, maxValue);
                }

                if(groupName == "day" && string.Equals(result, "max", StringComparison.InvariantCultureIgnoreCase))
                    return null;

                string options = new[] { defaultValue, "const", "+inc", "-dec", groupName == "day" ? "max" : null }.NotNull().Comma(" or ");

                return "'{0}' is not a valid {1}. Try {2} instead".Formato(result, groupName, options);
            }

            public DateTime ToDateTime()
            {
                DateTime now = TimeZoneManager.Now;

                int year = Mix(now.Year, Year, "yyyy");
                int month = Mix(now.Month, Month, "mm");
                int day = Day.ToLower() == "max" ? DateTime.DaysInMonth(year, month) : Mix(now.Day, Day, "dd");
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

            static string Diference(int diference, string pattern)
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
                return "{0}/{1}/{2} {3}:{4}:{5}".Formato(Year, Month, Day, Hour, Minute, Second);
            }
        }

        public string TryToString(object value, Type type, out string result)
        {
            if (value == null)
            {
                result = null;
                return FilterValueConverter.Continue;
            }

            DateTime dateTime = (DateTime)value;

            SmartDateTimeSpan ss = SmartDateTimeSpan.Substract(dateTime, TimeZoneManager.Now);
            result = ss.ToString();
            return null;
        }

        public string TryParse(string value, Type type, out object result)
        {
            SmartDateTimeSpan ss;
            string error = SmartDateTimeSpan.TryParse(value, out ss);

            if (error != null)
            {
                DateTime dtResult;
                if (DateTime.TryParse(value, out dtResult)) 
                {
                    result = dtResult;
                    return null; //do not block 
                }

                result = null;
                return error;
            }

            result = ss.ToDateTime();
            return null;
        }
    }

    public class LiteFilterValueConverter : IFilterValueConverter
    {
        public string TryToString(object value, Type type, out string result)
        {
            if (!(value is Lite<IdentifiableEntity>))
            {
                result = null;
                return FilterValueConverter.Continue;
            }

            result = ((Lite<IdentifiableEntity>)value).Key();
            return null;
        }

        public string TryParse(string value, Type type, out object result)
        {
            if (string.IsNullOrEmpty(value))
            {
                result = null;
                return FilterValueConverter.Continue;
            }

            Lite<IdentifiableEntity> lResult;
            string error = Lite.TryParseLite( value, out lResult);

            if (error == null)
            {
                result = lResult;
                return null;
            }
            else
            {
                result = null;
                return error;
            }
        }
    }

    public class CurrentEntityConverter : IFilterValueConverter
    {
        public static string CurrentEntityKey = "[CurrentEntity]";

        static readonly ThreadVariable<IdentifiableEntity> currentEntityVariable = Statics.ThreadVariable<IdentifiableEntity>("currentFilterValueEntity");

        public static IDisposable SetCurrentEntity(IdentifiableEntity currentEntity)
        {
            if (currentEntity == null)
                throw new InvalidOperationException("currentEntity is null");

            var old = currentEntityVariable.Value;

            currentEntityVariable.Value = currentEntity;

            return new Disposable(() => currentEntityVariable.Value = old);
        }

        public string TryToString(object value, Type type, out string result)
        {
            var lite = value as Lite<IdentifiableEntity>;

            if (lite != null && lite.RefersTo(currentEntityVariable.Value))
            {
                result = CurrentEntityKey;
                return null;
            }

            result = null;
            return FilterValueConverter.Continue;
        }

        public string TryParse(string value, Type type, out object result)
        {
            if (value.HasText() && value.StartsWith(CurrentEntityKey))
            {
                string after = value.Substring(CurrentEntityKey.Length).Trim();

                string[] parts = after.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                result = currentEntityVariable.Value;

                if (result == null)
                    return null;

                foreach (var part in parts)
                {
                    var prop = result.GetType().GetProperty(part, BindingFlags.Instance | BindingFlags.Public);

                    if (prop == null)
                        return "Property {0} not found on {1}".Formato(part, type.FullName);

                    result = prop.GetValue(result, null);

                    if (result == null)
                        return null;
                }

                if (result is IdentifiableEntity)
                    result = ((IdentifiableEntity)result).ToLite();

                return null;
            }

            result = null;
            return FilterValueConverter.Continue;
        }
    }

    public class CurrentUserConverter : IFilterValueConverter
    {
        static string CurrentUserKey = "[CurrentUser]";

        public string TryToString(object value, Type type, out string result)
        {
            var lu = value as Lite<UserDN>;

            if (lu != null  && lu.EntityType == typeof(UserDN) && lu.IdOrNull == UserDN.Current.Id)
            {
                result = CurrentUserKey;
                return null; 
            }

            result = null;
            return FilterValueConverter.Continue;
            
        }

        public string TryParse(string value, Type type, out object result)
        {
            if (value == CurrentUserKey)
            {
                result = UserDN.Current.ToLite();
                return null;
            }

            result = null;
            return FilterValueConverter.Continue;
        }
    }
}

