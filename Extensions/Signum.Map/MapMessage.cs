using System.ComponentModel;

namespace Signum.Map;

public enum MapMessage
{
    Map,
    Namespace,
    TableSize,
    Columns,
    Rows,
    [Description("Press {0} to explore each table")]
    Press0ToExploreEachTable,
    [Description("Press {0} to explore states and operations")]
    Press0ToExploreStatesAndOperations,
    Filter,
    Color,
    State,
    StateColor,
    RowsHistory,
    TableSizeHistory,
    [Description("Focus on selection")]
    FocusOnSelection,
    Depth,
    [Description("Select a table to focus")]
    SelectATableToFocus,
}

[DescriptionOptions(DescriptionOptions.Members)]
public enum DefaultState
{
    Start,
    All,
    End,
}

[AutoInit]
public static class MapPermission
{
    public static PermissionSymbol ViewMap;
}
