using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Map;

public class MapColorProvider
{
    public static Func<MapColorProvider[]> GetColorProviders;

    public string Name;
    public string NiceName;
    public Action<TableInfo> AddExtra;
    public decimal Order { get; set; }
}

public class TableInfo
{
    public string typeName;
    public string niceName;
    public string tableName;
    public EntityKind entityKind;
    public EntityData entityData;
    public EntityBaseType entityBaseType;
    public string @namespace;
    public int columns;
    public int? rows;
    public int? total_size_kb;
    public int? rows_history;
    public int? total_size_kb_history;
    public Dictionary<string, object?> extra = new Dictionary<string, object?>();

    public List<MListTableInfo> mlistTables;
}

public enum EntityBaseType
{
    EnumEntity,
    Symbol,
    SemiSymbol,
    Entity,
    MList,
    Part,
}

public class MListTableInfo
{
    public string niceName;
    public string tableName;
    public int columns;
    public int? rows;
    public int? total_size_kb;
    public int? history_rows;
    public int? history_total_size_kb;

    public Dictionary<string, object?> extra = new Dictionary<string, object?>();
}
