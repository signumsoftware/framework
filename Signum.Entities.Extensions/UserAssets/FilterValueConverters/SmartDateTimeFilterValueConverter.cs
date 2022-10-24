using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Signum.Entities.UserAssets;

public class SmartDateTimeFilterValueConverter : IFilterValueConverter
{
    public class SmartDateTimeSpan
    {
        static Regex partRegex = new Regex(@"^((\+\d+)|(-\d+)|(\d+))$");
        static Regex dayComplexRegex = new Regex(@"^(?<text>sun|mon|tue|wed|thu|fri|sat|max)(?<inc>[+-]\d+)?$", RegexOptions.IgnoreCase);

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
                return null;

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
                if (result.Contains("+") || result.Contains("-"))
                    return null;

                int val = int.Parse(result);
                if (minValue <= val && val <= maxValue)
                    return null;

                return "{0} must be between {1} and {2}".FormatWith(groupName, minValue, maxValue);
            }

            if (groupName == "day" && dayComplexRegex.IsMatch(result))
                return null;

            string options = new[] { defaultValue, "const", "+inc", "-dec", groupName == "day" ? "(max|sun|mon|tue|wed|fri|sat|)(+inc|-dec)?" : null }.NotNull().Comma(" or ");

            return "'{0}' is not a valid {1}. Try {2} instead".FormatWith(result, groupName, options);
        }

        public DateTime ToDateTime()
        {
            DateTime now = Clock.Now;

            int year = Mix(now.Year, Year, "yyyy");
            int month = Mix(now.Month, Month, "mm");
            int day;

            var m = dayComplexRegex.Match(Day);
            if (m.Success)
            {
                var text = m.Groups["text"].Value.ToLower();
                var inc = m.Groups["inc"].Value?.ToLower();
                if (text == "max")
                {
                    year += MonthDivMod(ref month);
                    day = DateTime.DaysInMonth(year, month);
                }
                else
                {
                    var dayOfWeek =
                        text == "sun" ? DayOfWeek.Sunday :
                        text == "mon" ? DayOfWeek.Monday :
                        text == "tue" ? DayOfWeek.Tuesday :
                        text == "wed" ? DayOfWeek.Wednesday :
                        text == "thu" ? DayOfWeek.Thursday :
                        text == "fri" ? DayOfWeek.Friday :
                        text == "sat" ? DayOfWeek.Saturday :
                        throw new InvalidOperationException("Unexpected text: " + text);

                    year += MonthDivMod(ref month);

                    var date = new DateTime(year, month, now.Day).WeekStart().AddDays(((int)dayOfWeek - (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek));
                    if(inc.HasText())
                    {
                        date = date.AddDays(int.Parse(inc));
                    }

                    year = date.Year;
                    month = date.Month;
                    day = date.Day;
                }
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

    static bool IsDate(Type targetType)
    {
        var uType = targetType.UnNullify();

        return uType == typeof(DateTime) || uType == typeof(DateOnly);
    }

    public Result<string?>? TryGetExpression(object? value, Type targetType)
    {
        if (value == null)
            return null;

        if (!IsDate(targetType))
            return null;

        DateTime dateTime = 
            value is string s ? DateTime.ParseExact(s, targetType == typeof(DateTime) ? "o" : "yyyy-MM-dd", CultureInfo.InvariantCulture) :
            value is DateOnly d ? d.ToDateTime(): 
            value is DateTime dt ? dt : throw new UnexpectedValueException(value);

        SmartDateTimeSpan ss = SmartDateTimeSpan.Substract(dateTime, Clock.Now);

        return new Result<string?>.Success(ss.ToString());
    }

    public Result<object?>? TryParseExpression(string? expression, Type targetType)
    {
        if (!IsDate(targetType))
            return null;

        var res = SmartDateTimeSpan.TryParse(expression);
        if (res == null)
            return null;

        if (res is Result<SmartDateTimeSpan>.Error e)
            return new Result<object?>.Error(e.ErrorText);

        if (res is Result<SmartDateTimeSpan>.Success s)
            return new Result<object?>.Success(targetType.UnNullify() == typeof(DateOnly) ? (object)s.Value.ToDateTime().ToDateOnly() : (object)s.Value.ToDateTime());

        throw new UnexpectedValueException(res);
    }

    public Result<Type>? IsValidExpression(string? expression, Type targetType, Type? currentEntityType)
    {
        if (!IsDate(targetType))
            return null;

        var res = SmartDateTimeSpan.TryParse(expression);
        if (res == null)
            return null;

        if (res is Result<SmartDateTimeSpan>.Error e)
            return new Result<Type>.Error(e.ErrorText);

        if (res is Result<SmartDateTimeSpan>.Success)
            return new Result<Type>.Success(
                targetType.UnNullify() == typeof(DateOnly) ? typeof(DateOnly) :
                targetType.UnNullify() == typeof(DateTime) ? typeof(DateTime) :
                throw new UnexpectedValueException(targetType.UnNullify()));

        throw new UnexpectedValueException(res);
    }
}

