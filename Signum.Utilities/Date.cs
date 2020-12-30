using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

//Thanks to supersonicclay
//From https://github.com/supersonicclay/csharp-date/blob/master/CSharpDate/Date.cs
namespace Signum.Utilities
{

    [Serializable, TypeConverter(typeof(DateTypeConverter))]
    public struct Date : IComparable, IFormattable, ISerializable, IComparable<Date>, IEquatable<Date>
    {
        private DateTime _dt;

        public static readonly Date MaxValue = new Date(DateTime.MaxValue);
        public static readonly Date MinValue = new Date(DateTime.MinValue);

        public Date(int year, int month, int day)
        {
            this._dt = new DateTime(year, month, day);
        }

        public Date(DateTime dateTime)
        {
            this._dt = dateTime.AddTicks(-dateTime.Ticks % TimeSpan.TicksPerDay);
        }

        private Date(SerializationInfo info, StreamingContext context)
        {
            this._dt = DateTime.FromFileTime(info.GetInt64("ticks"));
        }

        public static TimeSpan operator -(Date d1, Date d2)
        {
            return d1._dt - d2._dt;
        }

        public static DateTime operator -(Date d, TimeSpan t)
        {
            return d._dt - t;
        }

        public static bool operator !=(Date d1, Date d2)
        {
            return d1._dt != d2._dt;
        }

        public static DateTime operator +(Date d, TimeSpan t)
        {
            return d._dt + t;
        }

        public static bool operator <(Date d1, Date d2)
        {
            return d1._dt < d2._dt;
        }

        public static bool operator <=(Date d1, Date d2)
        {
            return d1._dt <= d2._dt;
        }

        public static bool operator ==(Date d1, Date d2)
        {
            return d1._dt == d2._dt;
        }

        public static bool operator >(Date d1, Date d2)
        {
            return d1._dt > d2._dt;
        }

        public static bool operator >=(Date d1, Date d2)
        {
            return d1._dt >= d2._dt;
        }

        public static implicit operator DateTime(Date d)
        {
            return d._dt;
        }

        public static explicit operator Date(DateTime d)
        {
            return new Date(d);
        }

        public int Day
        {
            get
            {
                return this._dt.Day;
            }
        }

        public DayOfWeek DayOfWeek
        {
            get
            {
                return this._dt.DayOfWeek;
            }
        }

        public int DayOfYear
        {
            get
            {
                return this._dt.DayOfYear;
            }
        }

        public int Month
        {
            get
            {
                return this._dt.Month;
            }
        }

        public static Date Today
        {
            get
            {
                return new Date(DateTime.Today);
            }
        }

        public int Year
        {
            get
            {
                return this._dt.Year;
            }
        }

        public long Ticks
        {
            get
            {
                return this._dt.Ticks;
            }
        }

        public Date AddDays(int value)
        {
            return new Date(this._dt.AddDays(value));
        }

        public Date AddMonths(int months)
        {
            return new Date(this._dt.AddMonths(months));
        }

        public Date AddYears(int value)
        {
            return new Date(this._dt.AddYears(value));
        }

        public static int Compare(Date d1, Date d2)
        {
            return d1.CompareTo(d2);
        }

        public int CompareTo(Date value)
        {
            return this._dt.CompareTo(value._dt);
        }

        public int CompareTo(object? value)
        {
            return this._dt.CompareTo(value);
        }

        public static int DaysInMonth(int year, int month)
        {
            return DateTime.DaysInMonth(year, month);
        }

        public bool Equals(Date value)
        {
            return this._dt.Equals(value._dt);
        }

        public override bool Equals(object? value)
        {
            return value is Date && this._dt.Equals(((Date)value)._dt);
        }

        public override int GetHashCode()
        {
            return this._dt.GetHashCode();
        }

        public static bool Equals(Date d1, Date d2)
        {
            return d1._dt.Equals(d2._dt);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ticks", this._dt.Ticks);
        }

        public static bool IsLeapYear(int year)
        {
            return DateTime.IsLeapYear(year);
        }

        public static Date Parse(string s)
        {
            return new Date(DateTime.Parse(s));
        }

        public static Date Parse(string s, IFormatProvider provider)
        {
            return new Date(DateTime.Parse(s, provider));
        }

        public static Date Parse(string s, IFormatProvider provider, DateTimeStyles style)
        {
            return new Date(DateTime.Parse(s, provider, style));
        }

        public static Date ParseExact(string s, string format, IFormatProvider provider)
        {
            return new Date(DateTime.ParseExact(s, ConvertFormat(format), provider));
        }

        public static Date ParseExact(string s, string format, IFormatProvider provider, DateTimeStyles style)
        {
            return new Date(DateTime.ParseExact(s, ConvertFormat(format), provider, style));
        }

        public static Date ParseExact(string s, string[] formats, IFormatProvider provider, DateTimeStyles style)
        {
            return new Date(DateTime.ParseExact(s, formats, provider, style));
        }

        public DateTime Add(TimeSpan value)
        {
            return this + value;
        }

        public TimeSpan Subtract(Date value)
        {
            return this - value;
        }

        public DateTime Subtract(TimeSpan value)
        {
            return this - value;
        }

        public string ToLongString()
        {
            return this._dt.ToLongDateString();
        }

        public string ToShortString()
        {
            return this._dt.ToShortDateString();
        }

        public override string ToString()
        {
            return this.ToShortString();
        }

        public string ToString(IFormatProvider provider) => ToString(null, provider);
        public string ToString(string format) => ToString(format, null);
        public string ToString(string? format, IFormatProvider? provider)  => this._dt.ToString(ConvertFormat(format), provider);

        [return: NotNullIfNotNull("format")]
        private static string? ConvertFormat(string? format)
        {
            if (format == "O" || format == "o" || format == "s")
            {
                format = "yyyy-MM-dd";
            }

            if (format == null)
                return "d";

            return format;
        }

        public static bool TryParse(string s, out Date result)
        {
            DateTime d;
            bool success = DateTime.TryParse(s, out d);
            result = new Date(d);
            return success;
        }

        public static bool TryParse(string s, IFormatProvider provider, DateTimeStyles style, out Date result)
        {
            DateTime d;
            bool success = DateTime.TryParse(s, provider, style, out d);
            result = new Date(d);
            return success;
        }

        public static bool TryParseExact(string s, string format, IFormatProvider provider, DateTimeStyles style, out Date result)
        {
            if (format == "O" || format == "o" || format == "s")
            {
                format = "yyyy-MM-dd";
            }

            DateTime d;
            bool success = DateTime.TryParseExact(s, format, provider, style, out d);
            result = new Date(d);
            return success;
        }

        public static bool TryParseExact(string s, string[] formats, IFormatProvider provider, DateTimeStyles style, out Date result)
        {
            DateTime d;
            bool success = DateTime.TryParseExact(s, formats, provider, style, out d);
            result = new Date(d);
            return success;
        }
    }

    public class DateTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return string.IsNullOrEmpty((string)value) ? (Date?)null : (Date?)Date.ParseExact((string)value, "o", CultureInfo.InvariantCulture);
        }

        public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            var date = (Date?)value;
            return date == null ? null : date.Value.ToString("o");
        }
    }
}
