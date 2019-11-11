using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using System.Globalization;
using System.Collections;
using Newtonsoft.Json;

namespace Signum.Entities.Chart
{
    [JsonConverter(typeof(ChartScriptParameterGroupJsonConverter))]
    public class ChartScriptParameterGroup : IEnumerable<ChartScriptParameter>
    {
        public string Name { get; }

        public ChartScriptParameterGroup(string name)
        {
            this.Name = name;
        }

        public void Add(ChartScriptParameter p)
        {
            this.Parameters.Add(p);
        }

        public IEnumerator<ChartScriptParameter> GetEnumerator() => Parameters.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Parameters.GetEnumerator();

        public List<ChartScriptParameter> Parameters = new List<ChartScriptParameter>();

        class ChartScriptParameterGroupJsonConverter : JsonConverter
        {
            public override bool CanWrite => true;
            public override bool CanRead => false;

            public override bool CanConvert(Type objectType) => typeof(ChartScriptParameterGroup) == objectType;

            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                var group = (ChartScriptParameterGroup)value!;
                writer.WriteStartObject();
                writer.WritePropertyName("name");
                writer.WriteValue(group.Name);
                writer.WritePropertyName("parameters");
                serializer.Serialize(writer, group.Parameters);
                writer.WriteEndObject();
            }
        }
    }
    
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

        public QueryToken? GetToken(IChartBase chartBase)
        {
            if (this.ColumnIndex == null)
                return null;

            return chartBase.Columns[this.ColumnIndex.Value].Token?.Token;
        }

        public string? Validate(string? value, QueryToken? token)
        {
            return ValueDefinition?.Validate(value, token);
        }

        internal string DefaultValue(QueryToken? token)
        {
            return this.ValueDefinition.DefaultValue(token);
        }
    }

    public interface IChartParameterValueDefinition
    {
        string DefaultValue(QueryToken? token);
        string? Validate(string? parameter, QueryToken? token);
    }

    public class NumberInterval : IChartParameterValueDefinition
    {
        public decimal DefaultValue;
        public decimal? MinValue;
        public decimal? MaxValue;

        public static string? TryParse(string valueDefinition, out NumberInterval? interval)
        {
            interval = null;
            var m = Regex.Match(valueDefinition, @"^\s*(?<def>.+)\s*(\[(?<min>.+)?\s*,\s*(?<max>.+)?\s*\])?\s*$");

            if (!m.Success)
                return "Invalid number interval, [min?, max?]";

            interval = new NumberInterval();

            if (!ReflectionTools.TryParse(m.Groups["def"].Value, CultureInfo.InvariantCulture, out interval!.DefaultValue))
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

        public string? Validate(string? parameter, QueryToken? token)
        {
            if (!decimal.TryParse(parameter, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value))
                return "{0} is not a valid number".FormatWith(parameter);

            if (MinValue.HasValue && value < MinValue)
                return "{0} is lesser than the minimum {1}".FormatWith(value, MinValue);

            if (MaxValue.HasValue && MaxValue < value)
                return "{0} is grater than the maximum {1}".FormatWith(value, MinValue);

            return null;
        }

        string IChartParameterValueDefinition.DefaultValue(QueryToken? token)
        {
            return DefaultValue.ToString(CultureInfo.InvariantCulture);
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
        public static string? TryParse(string valueDefinition, out EnumValueList list)
        {
            list = new EnumValueList();
            foreach (var item in valueDefinition.SplitNoEmpty('|'))
            {
                string? error = EnumValue.TryParse(item, out EnumValue? val);
                if (error.HasText())
                    return error;

                list.Add(val!);
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

        public string? Validate(string? parameter, QueryToken? token)
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

        public string DefaultValue(QueryToken? token)
        {
            return this.Where(a => a.CompatibleWith(token)).FirstEx(() => "No default parameter value for {0} found".FormatWith(token?.NiceName())).Name;
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

        public static string? TryParse(string value, out EnumValue? enumValue)
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

            if (string.IsNullOrEmpty(enumValue!.Name))
                return "Parameter has no name";

            string composedCode = m.Groups["filter"].Value;
            if (!composedCode.HasText())
                return null;


            string? error = ChartColumnTypeUtils.TryParseComposed(composedCode, out ChartColumnType filter);
            if (error.HasText())
                return enumValue.Name + ": " + error;

            enumValue.TypeFilter = filter;

            return null;
        }

        public bool CompatibleWith(QueryToken? token)
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

    [InTypeScript(true)]
    public enum ChartParameterType
    {
        Enum,
        Number,
        String,
    }

    public class StringValue : IChartParameterValueDefinition
    {
        public string DefaultValue;

        public StringValue(string defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public string? Validate(string? parameter, QueryToken? token)
        {
            return null;
        }

        string IChartParameterValueDefinition.DefaultValue(QueryToken? token)
        {
            return DefaultValue;
        }
    }
}
