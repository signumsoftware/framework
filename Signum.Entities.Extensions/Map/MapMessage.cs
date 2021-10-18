using Signum.Entities.Authorization;
using Signum.Utilities;
using System.ComponentModel;

namespace Signum.Entities.Map
{
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
}
