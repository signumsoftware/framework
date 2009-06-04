using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Utilities;
using System.Collections;
using System.Linq.Expressions;
using Signum.Entities.Properties;

namespace Signum.Entities
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public class DoNotValidateAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public abstract class ValidatorAttribute : Attribute
    {
        public bool DisableOnCorrupt { get; set; }


        public string Error(object value)
        {
            if (DisableOnCorrupt && !Corruption.Strict)
                return null;

            return OverrideError(value); 
        }

        /// <summary>
        /// When overriden, validates the value against this validator rule
        /// </summary>
        /// <param name="value"></param>
        /// <returns>returns an string with the error message, using {0} if you want the property name to be inserted</returns>
        protected abstract string OverrideError(object value);
    }

    public class NotNullValidatorAttribute : ValidatorAttribute
    {
        protected override string OverrideError(object obj)
        {
            return obj != null ? null : Resources.Property0HasNoValue;
        }
    }

    public class StringLengthValidatorAttribute : ValidatorAttribute
    {
        int min = -1;
        int max = -1;
        bool allowNulls = false;

        public bool AllowNulls
        {
            get { return allowNulls; }
            set { allowNulls = value; }
        }

        public int Min
        {
            get { return min; }
            set { min = value; }
        }

        public int Max
        {
            get { return max; }
            set { max = value; }
        }

        protected override string OverrideError(object value)
        {
            if (value == null)
                return  allowNulls? null: Resources.Property0HasNoValue;

            string val = (string)value;

            if (min != -1 && val.Length < min)
                return Resources.TheLengthOf0HasToBeGreaterOrEqualTo0.Formato(min);

            if (max != -1 && val.Length > max)
                return Resources.TheLengthOf0HasToBeLesserOrEqualTo0.Formato(max);

            return null; 
        }
    }

    public class RegexValidatorAttribute : ValidatorAttribute
    {
        Regex regex;         
        public RegexValidatorAttribute(string regex)
        {
            this.regex = new Regex(regex);
        }

        string formatName;        
        public string FormatName
        {
            get { return formatName; }
            set { formatName = value; }
        }

        protected override string OverrideError(object value)
        {
            string str = (string)value;
            if (str == null)
                return null;

            if (regex.IsMatch(str))
                return null;

            if (formatName == null)
                return Resources._0HasNoCorrectFormat;
            else
                return Resources._0DoesNotHaveAValid0Format.Formato(formatName);
        }
    }

    public class EmailValidatorAttribute : RegexValidatorAttribute
    {
        const string EmailRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                          @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                          @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";

        public EmailValidatorAttribute()
            : base(EmailRegex)
        {
            this.FormatName = "e-Mail";
        }
    }

    public class NumberIsValidatorAttribute : ValidatorAttribute
    {
        public ComparisonType ComparisonType;
        public object number;

        public NumberIsValidatorAttribute(ComparisonType comparison, float number)
        {
            this.ComparisonType = comparison;
            this.number = number; 
        }

        public NumberIsValidatorAttribute(ComparisonType comparison, double number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        public NumberIsValidatorAttribute(ComparisonType comparison, short number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        public NumberIsValidatorAttribute(ComparisonType comparison, int number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        public NumberIsValidatorAttribute(ComparisonType comparison, long number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        public NumberIsValidatorAttribute(ComparisonType comparison, byte number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        protected override string OverrideError(object value)
        {
            IComparable val = (IComparable)value;

            if (value == null)
                return null;

            if (number.GetType() != value.GetType())
                number = Convert.ChangeType(number, value.GetType()); // asi se hace solo una vez 

            bool ok = (ComparisonType == ComparisonType.EqualTo && val.CompareTo(number) == 0) ||
                      (ComparisonType == ComparisonType.DistinctTo && val.CompareTo(number) != 0) ||
                      (ComparisonType == ComparisonType.GreaterThan && val.CompareTo(number) > 0) ||
                      (ComparisonType == ComparisonType.GreaterThanOrEqual && val.CompareTo(number) >= 0) ||
                      (ComparisonType == ComparisonType.LessThan && val.CompareTo(number) < 0) ||
                      (ComparisonType == ComparisonType.LessThanOrEqual && val.CompareTo(number) <= 0);

            if (ok)
                return null;

            return Resources._0HasToBe0Than1.Formato(ComparisonType.NiceToString(), number.ToString()); 
        }
    }

    public class CountIsValidatorAttribute : ValidatorAttribute
    {
        public ComparisonType ComparisonType;
        public int number;

        public CountIsValidatorAttribute(ComparisonType comparison, int number)
        {
            this.ComparisonType = comparison;
            this.number = number;
        }

        protected override string OverrideError(object value)
        {
            IList list = (IList)value;
            if (list == null)
                return null;

            int val = list.Count;

            bool ok = (ComparisonType == ComparisonType.EqualTo && val.CompareTo(number) == 0) ||
                      (ComparisonType == ComparisonType.DistinctTo && val.CompareTo(number) != 0) ||
                      (ComparisonType == ComparisonType.GreaterThan && val.CompareTo(number) > 0) ||
                      (ComparisonType == ComparisonType.GreaterThanOrEqual && val.CompareTo(number) >= 0) ||
                      (ComparisonType == ComparisonType.LessThan && val.CompareTo(number) < 0) ||
                      (ComparisonType == ComparisonType.LessThanOrEqual && val.CompareTo(number) <= 0);

            if (ok)
                return null;

            return Resources.TheNumberOfElementsOf0HasToBe0Than1.Formato(ComparisonType.NiceToString(), number.ToString());
        }
    }

    public class DateOnlyValidatorAttribute : ValidatorAttribute
    {
        protected override string OverrideError(object value)
        {
            DateTime? dt = (DateTime?)value;
            if (dt.HasValue && dt.Value != dt.Value.Date)
                return Resources._0HasHoursMinutesAndSeconds;
            
            return null;
        }
    }

    public class StringCaseAttribute : ValidatorAttribute
    {
        private Case textCase;
        public Case TextCase
        {
            get { return this.textCase; }
            set { this.textCase = value; }
        }

        public StringCaseAttribute(Case textCase)
        {
            this.textCase = textCase;
        }

        protected override string OverrideError(object value)
        {
            if (value != null)
            {
                string str = (string)value;
                
                if ((this.textCase == Case.Uppercase) && (str != str.ToUpper()))
                    return Resources._0HasToBeUppercase;
                
                if ((this.textCase == Case.Lowercase) && (str != str.ToLower()))
                    return Resources._0HasToBeLowercase;
            }
            return null;            
        }
      
    }

    public enum Case
    {
        Uppercase,
        Lowercase
    }
 
    public enum ComparisonType
    {
        EqualTo,
        DistinctTo,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
    }

    internal static class ComparisonTypeExtensions
    {
        public static string NiceToString(this ComparisonType comp)
        {
            switch (comp)
            {
                case ComparisonType.EqualTo: return Resources.EqualTo;
                case ComparisonType.DistinctTo: return Resources.DistinctTo;
                case ComparisonType.GreaterThan: return Resources.GreaterThan;
                case ComparisonType.GreaterThanOrEqual: return Resources.GreaterThanOrEqual;
                case ComparisonType.LessThan: return Resources.LessThan;
                case ComparisonType.LessThanOrEqual: return Resources.LessThanOrEqual;
            }
            throw new NotImplementedException(); 
        }
    }

}