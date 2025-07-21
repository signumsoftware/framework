using DocumentFormat.OpenXml;
using Signum.DynamicQuery.Tokens;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Signum.Chart;

[JsonConverter(typeof(ChartScriptParameterGroupJsonConverter))]
public class ChartScriptParameterGroup : IEnumerable<ChartScriptParameter>
{
    public Func<string>? DisplayName { get; }

    public ChartScriptParameterGroup(Enum displayName)
    {
        DisplayName = displayName.NiceToString;
    }
    public ChartScriptParameterGroup(Func<string>? dispalyName = null)
    {
        DisplayName = dispalyName;
    }

    public void Add(ChartScriptParameter p)
    {
        Parameters.Add(p);
    }

    public IEnumerator<ChartScriptParameter> GetEnumerator() => Parameters.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Parameters.GetEnumerator();

    public List<ChartScriptParameter> Parameters = new List<ChartScriptParameter>();

    class ChartScriptParameterGroupJsonConverter : JsonConverter<ChartScriptParameterGroup>
    {
        public override ChartScriptParameterGroup? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, ChartScriptParameterGroup value, JsonSerializerOptions options)
        {
            var group = value!;
            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteStringValue(group.DisplayName?.Invoke());
            writer.WritePropertyName("parameters");
            JsonSerializer.Serialize(writer, group.Parameters, options);
            writer.WriteEndObject();
        }
    }
}



public class ChartScriptParameter
{
    public ChartScriptParameter(Enum prefix, Enum suffix, ChartParameterType type) :
        this(prefix.ToString() + suffix.ToString(), () => prefix.NiceToString() + " " + suffix.NiceToString(), type)
    {
    }

    public ChartScriptParameter(Enum displayName, ChartParameterType type): 
        this(displayName.ToString(), displayName.NiceToString, type)
    {
    }

    public ChartScriptParameter(string name, Func<string> displayName, ChartParameterType type)
    {
        Name = name;
        GetDisplayName = displayName;
        Type = type;
    }

    public string Name { get; set; }
    [JsonIgnore]
    public Func<string> GetDisplayName { get; set; }

    public string DisplayName => GetDisplayName();

    public int? ColumnIndex { get; set; }
    public ChartParameterType Type { get; set; }

    [JsonIgnore]
    public IChartParameterValueDefinition ValueDefinition { get; set; }

    [JsonPropertyName("valueDefinition")]
    public object ValueDefinitionObj => ValueDefinition; //object required for polymorphisim

    public QueryToken? GetToken(IChartBase chartBase)
    {
        if (ColumnIndex == null)
            return null;

        return chartBase.Columns[ColumnIndex.Value].Token?.Token;
    }

    public string? Validate(string? value, QueryToken? token)
    {
        return ValueDefinition?.Validate(value, token);
    }

    internal string DefaultValue(QueryToken? token)
    {
        return ValueDefinition.DefaultValue(token);
    }
}

public interface IChartParameterValueDefinition
{
    string DefaultValue(QueryToken? token);
    string? Validate(string? parameter, QueryToken? token);
}

public class NumberInterval : IChartParameterValueDefinition
{
    public decimal? DefaultValue;
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
        if (!parameter.HasText() && DefaultValue == null)
            return null;

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
        return DefaultValue?.ToString(CultureInfo.InvariantCulture) ?? "";
    }
}

[InTypeScript(true)]
public enum SpecialParameterType
{
    ColorCategory,
    ColorInterpolate,
}

public class SpecialParameter : IChartParameterValueDefinition
{
    public SpecialParameterType SpecialParameterType { get; }

    public SpecialParameter(SpecialParameterType specialParameterType)
    {
        SpecialParameterType = specialParameterType;
    }

    public string DefaultValue(QueryToken? token)
    {
        return "";
    }

    public string? Validate(string? parameter, QueryToken? token)
    {
        return null;
    }
}

public class Scala : IChartParameterValueDefinition
{
    public Dictionary<string, ChartColumnType?> StandardScalas;
    public bool Custom;

    public Scala(bool bands = false, bool zeroMax = true, bool minMax = true, bool minZeroMax = false, bool log = true, bool sqrt = true, bool custom = true)
    {
        StandardScalas = new Dictionary<string, ChartColumnType?>();
        if (bands) 
            StandardScalas.Add("Bands", ChartColumnType.AnyGroupKey);
        if(zeroMax)
            StandardScalas.Add("ZeroMax", ChartColumnType.AnyNumber);
        if (minMax)
            StandardScalas.Add("MinMax", null);
        if (minZeroMax)
            StandardScalas.Add("MinZeroMax", ChartColumnType.AnyNumber);
        if (log)
            StandardScalas.Add("Log", ChartColumnType.AnyNumber);
        if (sqrt)
            StandardScalas.Add("Sqrt", ChartColumnType.AnyNumber);

        this.Custom = custom;
    }


    public string DefaultValue(QueryToken? token)
    {
        return StandardScalas.First(a =>  a.Value == null || token == null || ChartUtils.IsChartColumnType(token, a.Value.Value)).Key;
    }

    public string? Validate(string? parameter, QueryToken? token)
    {
        if (parameter == null)
            return null;

        if (StandardScalas.TryGetValue(parameter, out ChartColumnType? type))
        {
            if (type == null || token == null || ChartUtils.IsChartColumnType(token, type.Value))
                return null;

            return "{0} is not compatible with {1}".FormatWith(parameter, token?.NiceName());
        }

        if(Custom && parameter.Contains("..."))
        {
            var minValue = parameter.Before("...");
            var maxValue = parameter.After("...");

            if (!decimal.TryParse(minValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return "{0} is not a valid number".FormatWith(minValue);

            if (!decimal.TryParse(maxValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return "{0} is not a valid number".FormatWith(maxValue);

            return null;
        }

        if (Custom)
            return "{0} is not in the list and is not a custom scala (min...max)".FormatWith(parameter);
        else
            return "{0} is not in the list".FormatWith(parameter);
    }
}

public class EnumValueList : List<string>, IChartParameterValueDefinition
{
    public static string? TryParse(string valueDefinition, out EnumValueList list)
    {
        list = [.. valueDefinition.SplitNoEmpty('|')];

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

        var enumValue = this.SingleOrDefault(a => a == parameter);

        if (enumValue == null)
            return "{0} is not in the list".FormatWith(parameter);

        return null;
    }

    public string DefaultValue(QueryToken? token)
    {
        return this.FirstEx(() => "No default parameter value for {0} found".FormatWith(token?.NiceName()));
    }
}

[InTypeScript(true)]
public enum ChartParameterType
{
    Enum,
    Number,
    String,
    Special,
    Scala,
}

public class StringValue : IChartParameterValueDefinition
{
    public string DefaultValue;

    public StringValue(string defaultValue)
    {
        DefaultValue = defaultValue;
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
