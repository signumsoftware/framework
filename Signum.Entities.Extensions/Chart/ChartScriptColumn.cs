
namespace Signum.Entities.Chart;

public class ChartScriptColumn
{
    public ChartScriptColumn(string displayName, ChartColumnType columnType)
    {
        this.DisplayName = displayName;
        this.ColumnType = columnType;
    }

    public string DisplayName { get; set; }
    public bool IsOptional { get; set; }
    public ChartColumnType ColumnType { get; set; }
}

   
[Flags, InTypeScript(true), DescriptionOptions(DescriptionOptions.Members)]
public enum ChartColumnType
{
    [Code("i")]
    Integer = 1,
    [Code("r")]
    Real = 2,
    [Code("d")]
    DateOnly = 4,
    [Code("dt")]
    DateTime = 8,
    [Code("s")]
    String = 16, //Guid
    [Code("l")]
    Lite = 32,
    [Code("e")]
    Enum = 64, // Boolean,
    [Code("rg")]
    RealGroupable = 128,
    [Code("t")]
    Time = 256,

    [Code("G")]
    Groupable = ChartColumnTypeUtils.GroupMargin | RealGroupable | Integer | DateOnly | String | Lite | Enum,
    [Code("M")]
    Magnitude = ChartColumnTypeUtils.GroupMargin | Integer | Real | RealGroupable,
    [Code("P")]
    Positionable = ChartColumnTypeUtils.GroupMargin | Integer | Real | RealGroupable | DateOnly | DateTime | Time,
    [Code("A")]
    Any = ChartColumnTypeUtils.GroupMargin | Integer | Real | RealGroupable | DateOnly | DateTime | Time | String | Lite | Enum,
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
            .Where(a => (int)a < ChartColumnTypeUtils.GroupMargin && columnType.HasFlag(a))
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
        type = default(ChartColumnType);
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
