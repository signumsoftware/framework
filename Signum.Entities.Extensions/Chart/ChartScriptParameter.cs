using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using System.Globalization;

namespace Signum.Entities.Chart
{
    public class ChartScriptParameter
    {
        public ChartScriptParameter(string name, ChartParameterType type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; set; }
        public int? ColumnIndex { get; set; }
        public ChartParameterType Type { get; set; }
        public IChartParameterValueDefinition ValueDefinition { get; set; }
    }

    public interface IChartParameterValueDefinition
    {
       

    }


    [Serializable]
    public class ChartScriptParameterEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 50)]
        public string Name { get; set; }

        ChartParameterType type;
        public ChartParameterType Type
        {
            get { return type; }
            set
            {
                if (Set(ref type, value))
                {
                    ValueDefinition = null;
                }
            }
        }

        public int? ColumnIndex { get; set; }

        string valueDefinition;
        [StringLengthValidator(AllowNulls = true, Max = int.MaxValue)]
        public string ValueDefinition
        {
            get { return valueDefinition; }
            set
            {
                if (Set(ref valueDefinition, value))
                {
                    enumValues = null;
                    numberInterval = null;
                }
            }
        }



        public string ToCode()
        {
            return $@"new ChartScriptParameter(""{Name}"", ChartParameterType.{Type}) {{ {(ColumnIndex == null ? "" :  ("ColumnIndex = " + ColumnIndex + ", "))} ValueDefinition = {GetValueDefinitionCode()} }}";
        }

        private string GetValueDefinitionCode()
        {
            switch (Type)
            {
                case ChartParameterType.Enum: return EnumValueList.TryParse(valueDefinition, out enumValues) == null ? enumValues.ToCode() : "error";
                case ChartParameterType.Number: return NumberInterval.TryParse(valueDefinition, out numberInterval) == null ? numberInterval.ToCode() : "error" ;
                case ChartParameterType.String: return "null";
                default: throw new InvalidOperationException();
            }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(ValueDefinition) && ValueDefinition != null)
            {
                switch (Type)
                {
                    case ChartParameterType.Enum: return EnumValueList.TryParse(valueDefinition, out enumValues);
                    case ChartParameterType.Number: return NumberInterval.TryParse(valueDefinition, out numberInterval);
                    case ChartParameterType.String: return null;
                    default: throw new InvalidOperationException();
                }
            }

            return base.PropertyValidation(pi);
        }

        public string DefaultValue(QueryToken token)
        {
            switch (Type)
            {
                case ChartParameterType.Enum: return GetEnumValues().DefaultValue(token);
                case ChartParameterType.Number: return GetNumberInterval().DefaultValue.ToString(CultureInfo.InvariantCulture);
                case ChartParameterType.String: return ValueDefinition;
                default: throw new InvalidOperationException();
            }
        }


        public string Valdidate(string parameter, QueryToken token)
        {
            switch (Type)
            {
                case ChartParameterType.Enum: return GetEnumValues().Validate(parameter, token);
                case ChartParameterType.Number: return GetNumberInterval().Validate(parameter);
                case ChartParameterType.String: return null;
                default: throw new InvalidOperationException();
            }
        }

        [Ignore, NonSerialized]
        EnumValueList enumValues;
        public EnumValueList GetEnumValues()
        {
            if (Type != ChartParameterType.Enum)
                throw new InvalidOperationException("Type is not Enum");

            if (enumValues != null)
                return enumValues;

            lock (this)
            {
                if (enumValues != null)
                    return enumValues;

                string error = EnumValueList.TryParse(valueDefinition, out enumValues);
                if (error.HasText())
                    throw new FormatException(error);
            }

            return enumValues;
        }

        [Ignore, NonSerialized]
        NumberInterval numberInterval;
        public NumberInterval GetNumberInterval()
        {
            if (Type != ChartParameterType.Number)
                throw new InvalidOperationException("Type is not Number");

            if (numberInterval != null)
                return numberInterval;

            lock (this)
            {
                if (numberInterval != null)
                    return numberInterval;

                string error = NumberInterval.TryParse(valueDefinition, out numberInterval);
                if (error.HasText())
                    throw new FormatException(error);
            }

            return numberInterval;
        }


        internal ChartScriptParameterEmbedded Clone()
        {
            return new ChartScriptParameterEmbedded
            {
                Name = Name,
                Type = Type,
                ValueDefinition = ValueDefinition,
                ColumnIndex = ColumnIndex
            };
        }

        internal bool ShouldHaveColumnIndex()
        {
            return Type == ChartParameterType.Enum && GetEnumValues().Any(a => a.TypeFilter.HasValue);
        }

        public QueryToken GetToken(IChartBase chartBase)
        {
            if (this.ColumnIndex == null)
                return null;

            return chartBase.Columns[this.ColumnIndex.Value].Token?.Token;
        }
    }

    public class NumberInterval : IChartParameterValueDefinition
    {
        public decimal DefaultValue;
        public decimal? MinValue;
        public decimal? MaxValue;

        public static string TryParse(string valueDefinition, out NumberInterval interval)
        {
            interval = null;
            var m = Regex.Match(valueDefinition, @"^\s*(?<def>.+)\s*(\[(?<min>.+)?\s*,\s*(?<max>.+)?\s*\])?\s*$");

            if (!m.Success)
                return "Invalid number interval, [min?, max?]";

            interval = new NumberInterval();

            if (!ReflectionTools.TryParse(m.Groups["def"].Value, CultureInfo.InvariantCulture, out interval.DefaultValue))
                return "Invalid default value";

            if (!ReflectionTools.TryParse(m.Groups["min"].Value, CultureInfo.InvariantCulture, out interval.MinValue))
                return "Invalid min value";

            if (!ReflectionTools.TryParse(m.Groups["max"].Value, CultureInfo.InvariantCulture, out interval.MaxValue))
                return "Invalid max value";

            return null;
        }

        public override string ToString()
        {
            return "{0}[{1},{2}]".FormatWith(DefaultValue, MinValue, MaxValue);
        }

        public string Validate(string parameter)
        {
            if (!decimal.TryParse(parameter, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value))
                return "{0} is not a valid number".FormatWith(parameter);

            if (MinValue.HasValue && value < MinValue)
                return "{0} is lesser than the minimum {1}".FormatWith(value, MinValue);

            if (MaxValue.HasValue && MaxValue < value)
                return "{0} is grater than the maximum {1}".FormatWith(value, MinValue);

            return null;
        }

        internal string ToCode()
        {
            if (MinValue != null || MaxValue != null)
                return $@"new NumberInterval {{ DefaultValue = {ToDecimal(DefaultValue)}, MinValue = {ToDecimal(MinValue)}, MaxValue = {ToDecimal(MaxValue)} }}";
            else
                return $@"new NumberInterval {{ DefaultValue = {ToDecimal(DefaultValue)} }}";
        }

        private string ToDecimal(decimal? val)
        {
            if (val == null)
                return "null";

            return val.ToString() + "m";
        }
    }
    
    public class EnumValueList : List<EnumValue>, IChartParameterValueDefinition
    {
        public static string TryParse(string valueDefinition, out EnumValueList list)
        {
            list = new EnumValueList();
            foreach (var item in valueDefinition.SplitNoEmpty('|'))
            {
                string error = EnumValue.TryParse(item, out EnumValue val);
                if (error.HasText())
                    return error;

                list.Add(val);
            }

            if (list.Count == 0)
                return "No parameter values set";

            return null;
        }

        public static EnumValueList Parse(string valueDefinition)
        {
            var error = TryParse(valueDefinition, out var list);
            if (error == null)
                return list;

            throw new Exception(error);
        }

        internal string Validate(string parameter, QueryToken token)
        {
            if (token == null)
                return null; //?

            var enumValue = this.SingleOrDefault(a => a.Name == parameter);

            if (enumValue == null)
                return "{0} is not in the list".FormatWith(parameter);

            if (!enumValue.CompatibleWith(token))
                return "{0} is not compatible with {1}".FormatWith(parameter, token?.NiceName());

            return null;
        }

        internal string DefaultValue(QueryToken token)
        {
            return this.Where(a => a.CompatibleWith(token)).FirstEx(() => "No default parameter value for {0} found".FormatWith(token.NiceName())).Name;
        }

        internal string ToCode()
        {
            return $@"EnumValueList.Parse(""{this.ToString("|")}"")";
        }
    }

    public class EnumValue
    {
        public string Name;
        public ChartColumnType? TypeFilter;

        public override string ToString()
        {
            if (TypeFilter == null)
                return Name;

            return "{0} ({1})".FormatWith(Name, TypeFilter.Value.GetComposedCode());
        }

        public static string TryParse(string value, out EnumValue enumValue)
        {
            var m = Regex.Match(value, @"^\s*(?<name>[^\(]*)\s*(\((?<filter>.*?)\))?\s*$");

            if (!m.Success)
            {
                enumValue = null;
                return "Invalid EnumValue";
            }

            enumValue = new EnumValue()
            {
                Name = m.Groups["name"].Value.Trim()
            };

            if (string.IsNullOrEmpty(enumValue.Name))
                return "Parameter has no name";

            string composedCode = m.Groups["filter"].Value;
            if (!composedCode.HasText())
                return null;


            string error = ChartColumnTypeUtils.TryParseComposed(composedCode, out ChartColumnType filter);
            if (error.HasText())
                return enumValue.Name + ": " + error;

            enumValue.TypeFilter = filter;

            return null;
        }

        public bool CompatibleWith(QueryToken token)
        {
            return TypeFilter == null || token != null && ChartUtils.IsChartColumnType(token, TypeFilter.Value);
        }

        internal string ToCode()
        {
            if (this.TypeFilter.HasValue)
                return $@"new EnumValue(""{ this.Name }"", ChartColumnType.{this.TypeFilter.Value})";
            else
                return $@"new EnumValue(""{ this.Name }"")";
        }
    }

    public enum ChartParameterType
    {
        Enum,
        Number,
        String,
    }
}
