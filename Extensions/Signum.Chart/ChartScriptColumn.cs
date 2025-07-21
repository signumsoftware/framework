using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Signum.Chart;

public class ChartScriptColumn
{
    public ChartScriptColumn(Enum displayName, ChartColumnType columnType)
    {
        GetDisplayName = displayName.NiceToString;
        ColumnType = columnType;
    }

    public ChartScriptColumn(Func<string> displayName, ChartColumnType columnType)
    {
        GetDisplayName = displayName;
        ColumnType = columnType;
    }

    [JsonIgnore]
    public Func<string> GetDisplayName { get; set; }
    public string DisplayName => GetDisplayName();
    public bool IsOptional { get; set; }
    public ChartColumnType ColumnType { get; set; }
}


[Flags, InTypeScript(true), DescriptionOptions(DescriptionOptions.Members)]
public enum ChartColumnType
{
    [Code("i")]
    Number = 1,
    [Code("r")]
    DecimalNumber = 2,
    [Code("d")]
    Date = 4,
    [Code("dt")]
    DateTime = 8,
    [Code("s")]
    String = 16, //Guid
    [Code("l")]
    Entity = 32,
    [Code("e")]
    Enum = 64, // Boolean,
    [Code("rg")]
    RoundedNumber = 128,
    [Code("t")]
    Time = 256,

    [Code("G")]
    AnyGroupKey = ChartColumnTypeUtils.GroupMargin | RoundedNumber | Number | Date | String | Entity | Enum,
    [Code("M"), Description("Any number")]
    AnyNumber = ChartColumnTypeUtils.GroupMargin | Number | DecimalNumber | RoundedNumber,
    [Code("P"), Description("Any number, date or time")]
    AnyNumberDateTime = ChartColumnTypeUtils.GroupMargin | Number | DecimalNumber | RoundedNumber | Date | DateTime | Time,
    [Code("A"), Description("All types")]
    AllTypes = ChartColumnTypeUtils.GroupMargin | Number | DecimalNumber | RoundedNumber | Date | DateTime | Time | String | Entity | Enum,
}


[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public sealed class CodeAttribute : Attribute
{
    string code;
    public CodeAttribute(string code)
    {
        this.code = code;
    }

    public string Code
    {
        get { return code; }
    }
}

public static class ChartColumnTypeUtils
{
    public const int GroupMargin = 0x10000000;

    static Dictionary<ChartColumnType, string> codes = EnumFieldCache.Get(typeof(ChartColumnType)).ToDictionary(
        a => (ChartColumnType)a.Key,
        a => a.Value.GetCustomAttribute<CodeAttribute>()!.Code);

    public static string GetCode(this ChartColumnType columnType)
    {
        return codes.GetOrThrow(columnType);
    }

    public static string GetComposedCode(this ChartColumnType columnType)
    {
        var result = columnType.GetCode();

        if (result.HasText())
            return result;

        return EnumExtensions.GetValues<ChartColumnType>()
            .Where(a => (int)a < GroupMargin && columnType.HasFlag(a))
            .ToString(GetCode, ",");
    }

    static Dictionary<string, ChartColumnType> fromCodes = EnumFieldCache.Get(typeof(ChartColumnType)).ToDictionary(
        a => a.Value.GetCustomAttribute<CodeAttribute>()!.Code,
        a => (ChartColumnType)a.Key);

    public static string? TryParse(string code, out ChartColumnType type)
    {
        if (fromCodes.TryGetValue(code, out type))
            return null;

        return "{0} is not a valid type code, use {1} instead".FormatWith(code, fromCodes.Keys.CommaOr());
    }

    public static string? TryParseComposed(string code, out ChartColumnType type)
    {
        type = default;
        foreach (var item in code.Split(','))
        {
            string? error = TryParse(item, out ChartColumnType temp);
            if (error.HasText())
                return error;

            type |= temp;
        }
        return null;
    }
}
