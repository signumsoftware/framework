using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Signum.Chart;

public class ChartScriptColumn
{
    public ChartScriptColumn(Enum displayName, ChartColumnType columnType)
    {
        Name = displayName.ToString();
        GetDisplayName = displayName.NiceToString;
        ColumnType = columnType;
    }

    public string Name { get; set; }
    [JsonIgnore]
    public Func<string> GetDisplayName { get; set; }
    public string DisplayName => GetDisplayName();
    public bool IsOptional { get; set; }
    public ChartColumnType ColumnType { get; set; }
}


[Flags, InTypeScript(true), DescriptionOptions(DescriptionOptions.Members)]
public enum ChartColumnType
{
    Number = 1,
    DecimalNumber = 2,
    Date = 4,
    DateTime = 8,
    String = 16, //Guid
    Entity = 32,
    Enum = 64, // Boolean,
    RoundedNumber = 128,
    Time = 256,

    AnyGroupKey = RoundedNumber | Number | Date | String | Entity | Enum,
    [Description("Any number")]
    AnyNumber = Number | DecimalNumber | RoundedNumber,
    [Description("Any number, date or time")]
    AnyNumberDateTime = Number | DecimalNumber | RoundedNumber | Date | DateTime | Time,
    [Description("All types")]
    AllTypes = Number | DecimalNumber | RoundedNumber | Date | DateTime | Time | String | Entity | Enum,
}

