using System.Data;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Runtime.Serialization;
using System.Globalization;
using System.ComponentModel;
using Signum.DynamicQuery.Tokens;

namespace Signum.DynamicQuery;

public class ResultColumn
{
    Column column;
    public Column Column => column;

    int index;
    public int Index
    {
        get { return index; }
        internal set { index = value; }
    }

    IList values;
    public IList Values => values;

    public bool CompressUniqueValues { get; set; }

    public ResultColumn(Column column, IList values)
    {
        this.column = column;
        this.values = values;
    }

    public override string ToString() => "Col" + this.Index + ": " + this.Column.ToString();
}

public class ResultTable
{
    internal ResultColumn? entityColumn;
    public ColumnDescription? EntityColumn
    {
        get { return entityColumn == null ? null : ((ColumnToken)entityColumn.Column.Token).Column; }
    }

    public bool HasEntities
    {
        get { return entityColumn != null; }
    }

    ResultColumn[] columns;
    public ResultColumn[] Columns { get { return columns; } }

    [NonSerialized]
    ResultRow[] rows;
    public ResultRow[] Rows { get { return rows; } }

    public ResultColumn[] AllColumns() => entityColumn == null ? Columns : Columns.PreAnd(entityColumn).ToArray();

    public ResultTable(ResultColumn[] columns, int? totalElements, Pagination pagination)
    {
        this.entityColumn = columns.Where(c => c.Column is _EntityColumn).SingleOrDefaultEx();
        this.columns = columns.Where(c => !(c.Column is _EntityColumn) && c.Column.Token.IsAllowed() == null).ToArray();

        int rowCount = columns.Select(a => a.Values.Count).Distinct().SingleEx(() => "Count");
        for (int i = 0; i < Columns.Length; i++)
            Columns[i].Index = i;
        this.rows = 0.To(rowCount).Select(i => new ResultRow(i, this)).ToArray();

        this.totalElements = totalElements;
        this.pagination = pagination;
    }

    public DataTable ToDataTable(DataTableValueConverter? converter = null)
    {
        var defConverter = converter ?? new InvariantDataTableValueConverter();

        DataTable dt = new DataTable("Table");
        dt.Columns.AddRange(Columns.Select(c => new DataColumn(c.Column.Name, defConverter.ConvertType(c.Column))).ToArray());
        foreach (var row in Rows)
        {
            dt.Rows.Add(Columns.Select((c, i) => defConverter.ConvertValue(row[i], c.Column)).ToArray());
        }
        return dt;
    }


    int? totalElements;
    public int? TotalElements { get { return totalElements; } }

    Pagination pagination;
    public Pagination Pagination { get { return pagination; } }

    public int? TotalPages
    {
        get { return Pagination is Pagination.Paginate ? ((Pagination.Paginate)Pagination).TotalPages(TotalElements!.Value) : (int?)null; }
    }

    public int? StartElementIndex
    {
        get { return Pagination is Pagination.Paginate ? ((Pagination.Paginate)Pagination).StartElementIndex() : (int?)null; }
    }

    public int? EndElementIndex
    {
        get { return Pagination is Pagination.Paginate ? ((Pagination.Paginate)Pagination).EndElementIndex(Rows.Count()) : (int?)null; }
    }
}

public abstract class DataTableValueConverter
{
    public abstract Type ConvertType(Column column);
    public abstract object? ConvertValue(object? value, Column column);
}

public class NiceDataTableValueConverter : DataTableValueConverter
{
    public override Type ConvertType(Column column)
    {
        var type = column.Type;

        if (type.IsLite())
            return typeof(string);

        if (type.UnNullify().IsEnum)
            return typeof(string);

        if (type.UnNullify() == typeof(DateTime) && column.Format != "g")
            return typeof(string);

        return type.UnNullify();
    }

    public override object? ConvertValue(object? value, Column column)
    {
        if (value is Lite<Entity> lite)
            return lite.ToString();

        if (value is Enum @enum)
            return @enum.NiceToString();

        if (value is DateTime time && column.Token.Format != "g")
            return time.ToString(column.Token.Format);

        return value;
    }
}

public class UnambiguousNiceDataTableValueConverter : NiceDataTableValueConverter
{
    HashSet<Lite<Entity>> ambiguousObjects = new HashSet<Lite<Entity>>();

    public UnambiguousNiceDataTableValueConverter(ResultTable rt)
    {
        this.ambiguousObjects = rt.Columns
            .Where(a => typeof(Lite<Entity>).IsAssignableFrom(a.Column.Type))
            .SelectMany(c => c.Values.Cast<Lite<Entity>?>().NotNull().Distinct().GroupBy(v => base.ConvertValue(v, c.Column)).Where(g => g.Count() > 1).SelectMany(a => a))
            .ToHashSet();
    }

    public override object? ConvertValue(object? value, Column column)
    {
        if (value is Lite<Entity> lite && ambiguousObjects.Contains(lite))
            return lite.ToString() + "(" + lite.Key() + ")";

        return base.ConvertValue(value, column);
    }
}

public class InvariantDataTableValueConverter : NiceDataTableValueConverter
{
    public override Type ConvertType(Column column)
    {
        var type = column.Token.Type;

        if (type.IsLite())
            return typeof(string);

        if (type.UnNullify().IsEnum)
            return typeof(string);

        return type.UnNullify();
    }

    public override object? ConvertValue(object? value, Column column)
    {
        if (value is Lite<Entity> lite)
            return lite.KeyLong();

        if (value is Enum @enum)
            return @enum.ToString();

        return value;
    }
}

public class ResultRow : INotifyPropertyChanged
{
    public readonly int Index;
    public readonly ResultTable Table;

    bool isDirty;
    public bool IsDirty
    {
        get { return isDirty; }
        set
        {
            isDirty = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsDirty"));
        }
    }

    public object? this[int columnIndex]
    {
        get { return Table.Columns[columnIndex].Values[Index]; }
    }

    public object? this[ResultColumn column]
    {
        get { return column.Values[Index]; }
    }

    internal ResultRow(int index, ResultTable table)
    {
        this.Index = index;
        this.Table = table;
    }

    public Lite<Entity> Entity
    {
        get { return (Lite<Entity>)Table.entityColumn!.Values[Index]!; }
    }

    public Lite<Entity>? TryEntity
    {
        get { return Table.entityColumn == null ? null : (Lite<Entity>?)Table.entityColumn.Values[Index]; }
    }

    public T GetValue<T>(string columnName)
    {
        return (T)this[Table.Columns.Where(c => c.Column.Name == columnName).SingleEx(() => columnName)]!;
    }

    public T GetValue<T>(int columnIndex)
    {
        return (T)this[columnIndex]!;
    }

    public T GetValue<T>(ResultColumn column)
    {
        return (T)this[column]!;
    }

    public object?[] GetValues(ResultColumn[] columnArray)
    {
        var result = new object?[columnArray.Length];
        for (int i = 0; i < columnArray.Length; i++)
        {
            result[i] = this[columnArray[i]];
        }
        return result;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
