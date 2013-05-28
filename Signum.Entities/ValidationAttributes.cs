using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Globalization;

namespace Signum.Entities
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public abstract class ValidatorAttribute : Attribute
    {
        public Func<ModifiableEntity, bool> IsApplicable; 
        public Func<string> ErrorMessage { get; set; }

        public string UnlocalizableErrorMessage
        {
            get { return ErrorMessage == null ? null : ErrorMessage(); }
            set { ErrorMessage = () => value; }
        }

        public int Order { get; set; }

        //Descriptive information that continues the sentence: The property should {HelpMessage}
        //Used for documentation purposes only
        public abstract string HelpMessage { get; }

        public string Error(ModifiableEntity entity, PropertyInfo property, object value)
        {
            if (IsApplicable != null && !IsApplicable(entity))
                return null;

            string defaultError = OverrideError(value);

            if (defaultError == null)
                return null;

            string error = ErrorMessage == null ? defaultError : ErrorMessage();
            if (error != null)
                error = error.Formato(property.NiceName());

            return error; 
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
            if (obj == null)
                return ValidationMessage._0IsNotSet.NiceToString();

            return null;
        }

        public override string HelpMessage
        {
            get { return ValidationMessage.BeNotNull.NiceToString(); }
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
            string val = (string)value;

            if (string.IsNullOrEmpty(val))
                return allowNulls ? null : ValidationMessage._0IsNotSet.NiceToString();

            if (min == max && min != -1 && val.Length != min)
                return ValidationMessage.TheLenghtOf0HasToBeEqualTo0.NiceToString().Formato(min);

            if (min != -1 && val.Length < min)
                return ValidationMessage.TheLengthOf0HasToBeGreaterOrEqualTo0.NiceToString().Formato(min);

            if (max != -1 && val.Length > max)
                return ValidationMessage.TheLengthOf0HasToBeLesserOrEqualTo0.NiceToString().Formato(max);

            return null;
        }

        public override string HelpMessage
        {
            get
            {
                string result =
                    min != -1 && max != -1 ? ValidationMessage.HaveBetween0And1Characters.NiceToString().Formato(min, max) :
                    min != -1 ? ValidationMessage.HaveMinimum0Characters.NiceToString().Formato(min) :
                    max != -1 ? ValidationMessage.HaveMaximun0Characters.NiceToString().Formato(max) : null;

                if (allowNulls)
                    result = result.Add(" ", ValidationMessage.OrBeNull.NiceToString());

                return result;
            }
        }
    }


    public class RegexValidatorAttribute : ValidatorAttribute
    {
        Regex regex;
        public RegexValidatorAttribute(Regex regex)
        {
            this.regex = regex;
        }

        public RegexValidatorAttribute(string regexExpresion)
        {
            this.regex = new Regex(regexExpresion);
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
            if (string.IsNullOrEmpty(str))
                return null;

            if (regex.IsMatch(str))
                return null;

            if (formatName == null)
                return ValidationMessage._0HasNoCorrectFormat.NiceToString();
            else
                return ValidationMessage._0DoesNotHaveAValid0Format.NiceToString().Formato(formatName);
        }

        public override string HelpMessage
        {
            get
            {
                return ValidationMessage.HaveValid0Format.NiceToString().Formato(formatName);
            }
        }
    }

    public class EMailValidatorAttribute : RegexValidatorAttribute
    {
        public static readonly Regex EmailRegex = new Regex(
                          @"^(([^<>()[\]\\.,;:\s@\""]+"
                        + @"(\.[^<>()[\]\\.,;:\s@\""]+)*)|(\"".+\""))@"
                        + @"((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}"
                        + @"\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+"
                        + @"[a-zA-Z]{2,}))$", RegexOptions.IgnoreCase);

        public EMailValidatorAttribute()
            : base(EmailRegex)
        {
            this.FormatName = "e-Mail";
        }
    }

    public class TelephoneValidatorAttribute : RegexValidatorAttribute
    {
        public static readonly Regex TelephoneRegex = new Regex(@"^((\+|00)\d\d)? *(\([ 0-9]+\))? *[0-9][ \-\.0-9]+$");

        public TelephoneValidatorAttribute()
            : base(TelephoneRegex)
        {
            this.FormatName = ValidationMessage.Telephone.NiceToString();
        }
    }

    public class URLValidatorAttribute : RegexValidatorAttribute
    {
        public static readonly Regex URLRegex = new Regex(
              "^(https?://)"
            + "?(([0-9a-z_!~*'().&=+$%-]+: )?[0-9a-z_!~*'().&=+$%-]+@)?" //user@ 
            + @"(([0-9]{1,3}\.){3}[0-9]{1,3}" // IP- 199.194.52.184 
            + "|" // allows either IP or domain 
            + @"([0-9a-z_!~*'()-]+\.)*" // tertiary domain(s)- www. 
            + @"([0-9a-z][0-9a-z-]{0,61})?[0-9a-z]" // second level domain 
            + @"(\.[a-z]{2,6})?)" // first level domain- .com or .museum 
            + "(:[0-9]{1,4})?" // port number- :80 
            + "((/?)|" // a slash isn't required if there is no file name 
            + "(/[0-9a-z_!~*'().;?:@&=+$,%#-]+)+/?)$", RegexOptions.IgnoreCase);

        public URLValidatorAttribute()
            : base(URLRegex)
        {
            this.FormatName = "URL";
        }
    }

    public class FileNameValidatorAttribute : RegexValidatorAttribute
    {
        public static readonly Regex FileNameRegex = new Regex(@"^(?!^(PRN|AUX|CLOCK\$|NUL|CON|COM\d|LPT\d|\..*)(\..+)?$)[^\x00-\x1f\\?*:\"";|/]+$");
        public FileNameValidatorAttribute()
            : base(FileNameRegex)
        {
            this.FormatName = ValidationMessage.FileName.NiceToString();
        }
    }

    public class DecimalsValidatorAttribute : ValidatorAttribute
    {
        public int DecimalPlaces { get; set; }

        public DecimalsValidatorAttribute()
        {
            DecimalPlaces = 2;
        }

        public DecimalsValidatorAttribute(int decimalPlaces)
        {
            this.DecimalPlaces = decimalPlaces;
        }

        protected override string OverrideError(object value)
        {
            if (value == null)
                return null;

            if (value is decimal && Math.Round((decimal)value, DecimalPlaces) != (decimal)value)
            {
                return ValidationMessage._0HasMoreThan0DecimalPlaces.NiceToString().Formato(DecimalPlaces);
            }

            return null;
        }

        public override string HelpMessage
        {
            get { return ValidationMessage.Have0Decimals.NiceToString().Formato(DecimalPlaces); }
        }
    }


    public class NumberIsValidatorAttribute : ValidatorAttribute
    {
        public ComparisonType ComparisonType;
        public IComparable number;

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

        public NumberIsValidatorAttribute(ComparisonType comparison, byte number)
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

        protected override string OverrideError(object value)
        {
            if (value == null)
                return null;

            IComparable val = (IComparable)value;

            if (number.GetType() != value.GetType())
                number = (IComparable)Convert.ChangeType(number, value.GetType()); // asi se hace solo una vez 

            bool ok = (ComparisonType == ComparisonType.EqualTo && val.CompareTo(number) == 0) ||
                      (ComparisonType == ComparisonType.DistinctTo && val.CompareTo(number) != 0) ||
                      (ComparisonType == ComparisonType.GreaterThan && val.CompareTo(number) > 0) ||
                      (ComparisonType == ComparisonType.GreaterThanOrEqual && val.CompareTo(number) >= 0) ||
                      (ComparisonType == ComparisonType.LessThan && val.CompareTo(number) < 0) ||
                      (ComparisonType == ComparisonType.LessThanOrEqual && val.CompareTo(number) <= 0);

            if (ok)
                return null;

            return ValidationMessage._0HasToBe0Than1.NiceToString().Formato(ComparisonType.NiceToString(), number.ToString());
        }

        public override string HelpMessage
        {
            get { return ValidationMessage.Be.NiceToString() + ComparisonType.NiceToString() + " " + number.ToString(); }
        }
    }

    //Not using C intervals to please user!
    public class NumberBetweenValidatorAttribute : ValidatorAttribute
    {
        IComparable min;
        IComparable max;

        public NumberBetweenValidatorAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public NumberBetweenValidatorAttribute(double min, double max)
        {
            this.min = min;
            this.max = max;
        }

        public NumberBetweenValidatorAttribute(byte min, byte max)
        {
            this.min = min;
            this.max = max;
        }

        public NumberBetweenValidatorAttribute(short min, short max)
        {
            this.min = min;
            this.max = max;
        }

        public NumberBetweenValidatorAttribute(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public NumberBetweenValidatorAttribute(long min, long max)
        {
            this.min = min;
            this.max = max;
        }

        protected override string OverrideError(object value)
        {
            if (value == null)
                return null;

            IComparable val = (IComparable)value;

            if (min.GetType() != value.GetType())
            {
                min = (IComparable)Convert.ChangeType(min, val.GetType()); // asi se hace solo una vez 
                max = (IComparable)Convert.ChangeType(max, val.GetType());
            }

            if (min.CompareTo(val) <= 0 &&
                val.CompareTo(max) <= 0)
                return null;

            return ValidationMessage._0HasToBeBetween0And1.NiceToString().Formato(min, max);
        }

        public override string HelpMessage
        {
            get { return ValidationMessage.BeBetween0And1.NiceToString().Formato(min, max); }
        }
    }

    public class NoRepeatValidatorAttribute : ValidatorAttribute
    {
        protected override string OverrideError(object value)
        {
            IList list = (IList)value;
            if (list == null || list.Count <= 1)
                return null;
            string ex = list.Cast<object>().GroupCount().Where(kvp => kvp.Value > 1).ToString(e => "{0} x {1}".Formato(e.Key, e.Value), ", ");
            if (ex.HasText())
                return ValidationMessage._0HasSomeRepeatedElements0.NiceToString().Formato(ex);
            return null;
        }

        public override string HelpMessage
        {
            get { return ValidationMessage.HaveNoRepeatedElements.NiceToString(); }
        }

        public static string ByKey<T, K>(IEnumerable<T> collection, Func<T, K> keySelector)
        {
            var errors = collection.GroupBy(keySelector)
                .Select(gr => new { gr.Key, Count = gr.Count() })
                .Where(a => a.Count > 1)
                .ToString(e => "{0} x {1}".Formato(e.Key, e.Count), ", ");

            return errors;
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

            int val = list == null? 0: list.Count;

            if ((ComparisonType == ComparisonType.EqualTo && val.CompareTo(number) == 0) ||
                (ComparisonType == ComparisonType.DistinctTo && val.CompareTo(number) != 0) ||
                (ComparisonType == ComparisonType.GreaterThan && val.CompareTo(number) > 0) ||
                (ComparisonType == ComparisonType.GreaterThanOrEqual && val.CompareTo(number) >= 0) ||
                (ComparisonType == ComparisonType.LessThan && val.CompareTo(number) < 0) ||
                (ComparisonType == ComparisonType.LessThanOrEqual && val.CompareTo(number) <= 0))
                return null;

            return ValidationMessage.TheNumberOfElementsOf0HasToBe01.NiceToString().Formato(ComparisonType.NiceToString(), number.ToString());
        }

        public override string HelpMessage
        {
            get { return ValidationMessage.HaveANumberOfElements01.NiceToString().Formato(ComparisonType.NiceToString(), number.ToString()); }
        }
    }

    public class DaysPrecissionValidatorAttribute : DateTimePrecissionValidatorAttribute
    {
        public DaysPrecissionValidatorAttribute()
            : base(DateTimePrecision.Days)
        { }
    }

    public class SecondsPrecissionValidatorAttribute : DateTimePrecissionValidatorAttribute
    {
        public SecondsPrecissionValidatorAttribute()
            : base(DateTimePrecision.Seconds)
        { }
    }

    public class MinutesPrecissionValidatorAttribute : DateTimePrecissionValidatorAttribute
    {
        public MinutesPrecissionValidatorAttribute()
            : base(DateTimePrecision.Minutes)
        { }

    }

    public class DateTimePrecissionValidatorAttribute : ValidatorAttribute
    {
        public DateTimePrecision Precision { get; private set; }

        public DateTimePrecissionValidatorAttribute(DateTimePrecision precision)
        {
            this.Precision = precision;
        }

        protected override string OverrideError(object value)
        {
            if (value == null)
                return null;

            var prec = ((DateTime)value).GetPrecision();
            if (prec > Precision)
                return "{{0}} has a precission of {0} instead of {1}".Formato(prec, Precision);

            return null;
        }

        public string FormatString
        {
            get
            {
                var dtfi = CultureInfo.CurrentCulture.DateTimeFormat;
                switch (Precision)
                {
                    case DateTimePrecision.Days: return "d";
                    case DateTimePrecision.Hours: return dtfi.ShortDatePattern + " " + "HH";
                    case DateTimePrecision.Minutes: return "g";
                    case DateTimePrecision.Seconds: return "G";
                    case DateTimePrecision.Milliseconds: return dtfi.ShortDatePattern + " " + dtfi.LongTimePattern + ".fff";
                    default: return "";
                }
            }
        }

        public override string HelpMessage
        {
            get
            {
                return ValidationMessage.HaveAPrecisionOf.NiceToString() + " " + Precision.NiceToString().ToLower();
            }
        }
    }

    public class TimeSpanPrecissionValidatorAttribute : ValidatorAttribute
    {
        public DateTimePrecision Precision { get; private set; }

        public TimeSpanPrecissionValidatorAttribute(DateTimePrecision precision)
        {
            this.Precision = precision;
        }

        protected override string OverrideError(object value)
        {
            if (value == null)
                return null;

            var prec = ((TimeSpan)value).GetPrecision();
            if (prec > Precision)
                return "{{0}} has a precission of {0} instead of {1}".Formato(prec, Precision);

            if(((TimeSpan)value).Days != 0)
                return "{{0}} has days".Formato(prec, Precision);

            return null;
        }

        public string FormatString
        {
            get
            {
                var dtfi = CultureInfo.CurrentCulture.DateTimeFormat;
                switch (Precision)
                {
                    case DateTimePrecision.Hours: return "HH";
                    case DateTimePrecision.Minutes: return dtfi.ShortTimePattern;
                    case DateTimePrecision.Seconds: return "c";
                    case DateTimePrecision.Milliseconds: return dtfi.LongTimePattern + ".fff";
                    default: return "";
                }
            }
        }

        public override string HelpMessage
        {
            get
            {
                return ValidationMessage.HaveAPrecisionOf.NiceToString() + " " + Precision.NiceToString().ToLower();
            }
        }
    }

    public class StringCaseValidatorAttribute : ValidatorAttribute
    {
        private Case textCase;
        public Case TextCase
        {
            get { return this.textCase; }
            set { this.textCase = value; }
        }

        public StringCaseValidatorAttribute(Case textCase)
        {
            this.textCase = textCase;
        }

        protected override string OverrideError(object value)
        {
            if (string.IsNullOrEmpty((string)value)) return null;

            string str = (string)value;

            if ((this.textCase == Case.Uppercase) && (str != str.ToUpper()))
                return ValidationMessage._0HasToBeUppercase.NiceToString();

            if ((this.textCase == Case.Lowercase) && (str != str.ToLower()))
                return ValidationMessage._0HasToBeLowercase.NiceToString();

            return null;
        }

        public override string HelpMessage
        {
            get { return ValidationMessage.Be.NiceToString() + textCase.NiceToString(); }
        }
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum Case
    {
        Uppercase,
        Lowercase
    }
    
    [DescriptionOptions(DescriptionOptions.Members)]
    public enum ComparisonType
    {
        EqualTo,
        DistinctTo,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
    }

    public class StateValidator<E, S> : IEnumerable
        where E : ModifiableEntity
        where S : struct
    {
        Func<E, S> getState;
        string[] propertyNames;
        PropertyInfo[] properties;
        Func<E, object>[] getters;

        Dictionary<S, bool?[]> dictionary = new Dictionary<S, bool?[]>();

        public StateValidator(Func<E, S> getState, params Expression<Func<E, object>>[] properties)
        {
            this.getState = getState;
            this.properties = properties.Select(p => ReflectionTools.GetPropertyInfo(p)).ToArray();
            this.propertyNames = properties.Select(pi => pi.Name).ToArray();
            this.getters = properties.Select(p => p.Compile()).ToArray();
        }

        public void Add(S state, params bool?[] necessary)
        {
            if (necessary != null && necessary.Length != propertyNames.Length)
                throw new ArgumentException("The StateValidator {0} for state {1} has {2} values instead of {3}"
                    .Formato(GetType().TypeName(), state, necessary.Length, propertyNames.Length));

            dictionary.Add(state, necessary);
        }

        public string Validate(E entity, PropertyInfo pi)
        {
            return Validate(entity, pi, true);
        }

        public bool? IsAllowed(S state, PropertyInfo pi)
        {
            int index = propertyNames.IndexOf(pi.Name);
            if (index == -1)
                return null;

            return Necessary(state, index);
        }

      

        public string Validate(E entity, PropertyInfo pi, bool showState)
        {
            int index = propertyNames.IndexOf(pi.Name);
            if (index == -1)
                return null;

            S state = getState(entity);

            return GetMessage(entity, state, showState, index);
        }

        private string GetMessage(E entity, S state, bool showState, int index)
        {
            bool? necessary = Necessary(state, index);

            if (necessary == null)
                return null;

            object val = getters[index](entity);
            if (val is IList && ((IList)val).Count == 0 || val is string && ((string)val).Length == 0) //both are indistinguible after retrieving
                val = null;

            if (val != null && !necessary.Value)
                return showState ? ValidationMessage._0IsNotAllowedOnState1.NiceToString().Formato(properties[index].NiceName(), state) :
                                   ValidationMessage._0IsNotAllowed.NiceToString().Formato(properties[index].NiceName());

            if (val == null && necessary.Value)
                return showState ? ValidationMessage._0IsNecessaryOnState1.NiceToString().Formato(properties[index].NiceName(), state) :
                                   ValidationMessage._0IsNecessary.NiceToString().Formato(properties[index].NiceName());

            return null;
        }

        public bool? Necessary(S state, PropertyInfo pi)
        {
            int index = propertyNames.IndexOf(pi.Name);
            if (index == -1)
                throw new ArgumentException("The property is not registered");

            return Necessary(state, index);
        }

        bool? Necessary(S state, int index)
        {
            return dictionary.GetOrThrow(state, "State {0} not registered in StateValidator")[index];
        }

        public IEnumerator GetEnumerator() //just to use object initializer
        {
            throw new NotImplementedException();
        }

        public string PreviewErrors(E entity, S targetState, bool showState)
        {
            string result = propertyNames.Select((pn, i) => GetMessage(entity, targetState, showState, i)).NotNull().ToString("\r\n");

            return string.IsNullOrEmpty(result) ? null : result;
        }
    }


    public enum ValidationMessage
    {
        [Description("{{0}} does not have a valid {0} format")]
        _0DoesNotHaveAValid0Format,
        [Description("{0} has an invalid format")]
        _0HasAnInvalidFormat,
        [Description("{{0}} has more than {0} decimal places")]
        _0HasMoreThan0DecimalPlaces,
        [Description("{0} has no correct format")]
        _0HasNoCorrectFormat,
        [Description("{{0}} has some repeated elements: {0}")]
        _0HasSomeRepeatedElements0,
        [Description("{{0}} has to be {0} {1}")]
        _0HasToBe0Than1,
        [Description("{{0}} Has to be between {0} and {1}")]
        _0HasToBeBetween0And1,
        [Description("{0} has to be lowercase")]
        _0HasToBeLowercase,
        [Description("{0} has to be uppercase")]
        _0HasToBeUppercase,
        [Description("{0} is necessary")]
        _0IsNecessary,
        [Description("{0} is necessary on state {1}")]
        _0IsNecessaryOnState1,
        [Description("{0} is not allowed")]
        _0IsNotAllowed,
        [Description("{0} is not allowed on state {1}")]
        _0IsNotAllowedOnState1,
        [Description("{0} is not set")]
        _0IsNotSet,
        [Description("be ")]
        Be,
        [Description("be between {0} and {1}")]
        BeBetween0And1,
        [Description("be not null")]
        BeNotNull,
        [Description("file name")]
        FileName,
        [Description("have {0} decimals")]
        Have0Decimals,
        [Description("have a number of elements {0} {1}")]
        HaveANumberOfElements01,
        [Description("have a precision of ")]
        HaveAPrecisionOf,
        [Description("have between {0} and {1} characters")]
        HaveBetween0And1Characters,
        [Description("have maximun {0} characters")]
        HaveMaximun0Characters,
        [Description("have minimum {0} characters")]
        HaveMinimum0Characters,
        [Description("have no repeated elements")]
        HaveNoRepeatedElements,
        [Description("have a valid {0} format")]
        HaveValid0Format,
        InvalidDateFormat,
        InvalidFormat,
        [Description("Not possible to assign {0}")]
        NotPossibleToaAssign0,
        [Description("or be null")]
        OrBeNull,
        Telephone,
        [Description("The lenght of {{0}} has to be equal to {0}")]
        TheLenghtOf0HasToBeEqualTo0,
        [Description("The length of {{0}} has to be greater than or equal to {0}")]
        TheLengthOf0HasToBeGreaterOrEqualTo0,
        [Description("The length of {{0}} has to be less than or equal to {0}")]
        TheLengthOf0HasToBeLesserOrEqualTo0,
        [Description("The number of {0} is being multiplied by {1}")]
        TheNumberOf0IsBeingMultipliedBy1,
        [Description("The number of elements of {{0}} has to be {0} {1}")]
        TheNumberOfElementsOf0HasToBe01,
        [Description("Type {0} not allowed")]
        Type0NotAllowed
    }


}