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
    Show,
    All,
    Selected,
    [Description("Selected and neighbors")]
    SelectedAndNeighbors,
    Help,
    [Description("Click a table to select it. Click again to deselect.")]
    HelpClick,
    [Description("Shift+Click to add or remove a table from the selection.")]
    HelpShiftClick,
    [Description("Ctrl+Click to open the table in a new tab.")]
    HelpCtrlClick,
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
