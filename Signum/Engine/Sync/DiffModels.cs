using NpgsqlTypes;
using Signum.Engine.Maps;
using System.Data;

namespace Signum.Engine.Sync;


#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class DiffPeriod
{
    public string StartColumnName;
    public string EndColumnName;

    internal bool PeriodEquals(SystemVersionedInfo systemVersioned)
    {
        return systemVersioned.StartColumnName == StartColumnName &&
            systemVersioned.EndColumnName == EndColumnName;
    }
}

public class DiffPostgresVersioningTrigger
{
    public int? tgrelid;
    public string tgname;
    public string proname;
    public byte[] tgargs;
    public int tgfoid;
}

public class DiffSchema
{
    public SchemaName Name;
    public string? Owner;
}

public class DiffTable
{
    public ObjectName Name;

    public ObjectName? PrimaryKeyName;

    public Dictionary<string, DiffColumn> Columns;

    public List<DiffIndex> SimpleIndices
    {
        get { return Indices.Values.ToList(); }
        set { Indices.AddRange(value, a => a.IndexName, a => a); }
    }

    public List<DiffIndex> ViewIndices
    {
        get { return Indices.Values.ToList(); }
        set { Indices.AddRange(value, a => a.IndexName, a => a); }
    }

    public DiffIndex? FullTextIndex
    {
        get { return Indices.Values.SingleOrDefault(a => a.FullTextIndex != null); }
        set
        {
            if (value != null)
                Indices[FullTextTableIndex.SqlServerOptions.FULL_TEXT] = value;
            else
                Indices.Remove(FullTextTableIndex.SqlServerOptions.FULL_TEXT);
        }
    }

    public DiffPostgresVersioningTrigger? VersionningTrigger;
    public string? Owner;
    public SysTableTemporalType TemporalType;
    public ObjectName? TemporalTableName;
    public ObjectName? InferredTemporalTableName;
    public DiffPeriod? Period;

    public Dictionary<string, DiffIndex> Indices = new Dictionary<string, DiffIndex>();

    public List<DiffStats> Stats = new List<DiffStats>();

    public List<DiffForeignKey> MultiForeignKeys = new List<DiffForeignKey>();
    public List<DiffCheckConstraint> CheckConstraints = new List<DiffCheckConstraint>();

    public void ForeignKeysToColumns()
    {
        foreach (var fk in MultiForeignKeys.Where(a => a.Columns.Count == 1).ToList())
        {
            this.Columns[fk.Columns.SingleEx().Parent].ForeignKey = fk;
            MultiForeignKeys.Remove(fk);
        }

        foreach (var cc in CheckConstraints.Where(a => a.ColumnName != null).ToList())
        {
            this.Columns[cc.ColumnName!].CheckConstraint = cc;
            CheckConstraints.Remove(cc);
        }
    }

    public DiffColumn AddColumnBefore(string columnName, DiffColumn newColumn)
    {
        var cols = Columns.Values.ToList();

        cols.Insert(cols.FindIndex(c => c.Name == columnName), newColumn);

        Columns = cols.ToDictionary(a => a.Name);

        return newColumn;
    }

    public override string ToString()
    {
        return Name.ToString();
    }

    internal void FixSqlColumnLengthSqlServer()
    {
        foreach (var c in Columns.Values.Where(c => c.Length != -1))
        {
            var sqlDbType = c.DbType.SqlServer;
            if (sqlDbType == SqlDbType.NChar || sqlDbType == SqlDbType.NText || sqlDbType == SqlDbType.NVarChar)
                c.Length /= 2;
        }
    }
}



public record class FullTextCatallogName(string Name, DatabaseName? Database);

public class DiffPartitionFunction
{
    public DatabaseName? DatabaseName;
    public string FunctionName;
    public int Fanout;
    public object[] Values;
}

public class DiffPartitionScheme
{
    public DatabaseName? DatabaseName;
    public string SchemeName;
    public string FunctionName;
    public string[] FileGroups;
}

public enum SysTableTemporalType
{
    None = 0,
    HistoryTable = 1,
    SystemVersionTemporalTable = 2
}

public class DiffStats
{
    public string StatsName;

    public List<string> Columns;
}

public class DiffIndexColumn
{
    public string ColumnName;
    public bool IsDescending;
    public bool IsIncluded;
    public DiffIndexColumnType Type;
}

public enum DiffIndexColumnType
{
    Key,
    Included,
    Partition,
}

public class DiffIndex
{
    public bool IsUnique;
    public bool IsPrimary;
    public FullTextIndex? FullTextIndex;
    public string IndexName;
    public string? ViewName;
    public string? FilterDefinition;
    public string? DataSpaceName;

    public DiffIndexType? Type;

    public List<DiffIndexColumn> Columns;

    public override string ToString()
    {
        return "{0} ({1})".FormatWith(IndexName, Columns.ToString(", "));
    }

    internal bool IndexEquals(DiffTable dif, Maps.TableIndex mix, bool isPostgress)
    {
        if (this.ViewName != mix.ViewName)
            return false;

        if (this.ColumnsChanged(dif, mix) && Type != DiffIndexType.Heap)
            return false;

        if (this.IsPrimary != mix.PrimaryKey)
            return false;

        if (this.DataSpaceName != null && this.DataSpaceName != (mix.PartitionSchemeName ?? "PRIMARY"))
            return false;

        if (this.Type != GetIndexType(mix, isPostgress))
            return false;

        return true;
    }

    private static DiffIndexType? GetIndexType(TableIndex mix, bool isPostgress)
    {
        if (mix.Unique && mix.ViewName != null)
            return null;

        if (mix is FullTextTableIndex && !isPostgress)
            return DiffIndexType.FullTextIndex;

        if (mix.Clustered && !isPostgress)
            return DiffIndexType.Clustered;

        return DiffIndexType.NonClustered;
    }

    bool ColumnsChanged(DiffTable dif, TableIndex mix)
    {
        if (mix is FullTextTableIndex fti && dif.Name.IsPostgres)
        {
            if (fti.Postgres.TsVectorColumnName == this.Columns.Only()?.ColumnName)
                return false;

            return true;
        }
        else
        {
            bool sameCols = IdenticalColumns(dif, mix.Columns, this.Columns.Where(a => a.Type == DiffIndexColumnType.Key).ToList());
            bool sameIncCols = IdenticalColumns(dif, mix.IncludeColumns, this.Columns.Where(a => a.Type == DiffIndexColumnType.Included).ToList());

            if (sameCols && sameIncCols)
                return false;

            return true;
        }
    }

    private static bool IdenticalColumns(DiffTable dif, IColumn[]? modColumns, List<DiffIndexColumn> diffColumns)
    {
        if ((modColumns?.Length ?? 0) != diffColumns.Count)
            return false;

        if (diffColumns.Count == 0)
            return true;

        var difColumns = diffColumns.Select(cn => dif.Columns.Values.SingleOrDefault(dc => dc.Name == cn.ColumnName)).ToList(); //Ny old name

        var perfect = difColumns.ZipOrDefault(modColumns!, (dc, mc) => dc != null && mc != null && dc.ColumnEquals(mc, ignorePrimaryKey: true, ignoreIdentity: true, ignoreGenerateAlways: true)).All(a => a);
        return perfect;
    }

    public bool IsControlledIndex(bool isPostgres)
    {
        return
            IndexName.StartsWith(isPostgres ? "ix_" : "IX_") ||
            IndexName.StartsWith(isPostgres ? "uix_" : "UIX_") ||
            IndexName.StartsWith(isPostgres ? "cix_" : "CIX_");
    }
}

public class FullTextIndex
{
    public string CatallogName;
    public string StopList;
    public string PropertiesList;
}

public enum DiffIndexType
{
    Heap = 0,
    Clustered = 1,
    NonClustered = 2,
    Xml = 3,
    Spatial = 4,
    ClusteredColumnstore = 5,
    NonClusteredColumnstore = 6,
    NonClusteredHash = 7,


    FullTextIndex = 100,
}

public enum GeneratedAlwaysType
{
    None = 0,
    AsRowStart = 1,
    AsRowEnd = 2
}

public class DiffDefaultConstraint
{
    public string? Name;
    public string Definition;
}

public class DiffComputedColumn
{
    public bool Persisted;
    public string Definition;
}

public class DiffColumn
{
    public string Name;
    public AbstractDbType DbType;
    public string? UserTypeName;
    public bool Nullable;
    public string? Collation;
    public int Length;
    public int Precision;
    public int Scale;
    public bool Identity;
    public bool PrimaryKey;

    public DiffForeignKey? ForeignKey;

    public DiffComputedColumn? ComputedColumn;
    public DiffDefaultConstraint? DefaultConstraint;

    public DiffCheckConstraint? CheckConstraint;

    public GeneratedAlwaysType GeneratedAlwaysType;


    public bool ColumnEquals(IColumn other, bool ignorePrimaryKey, bool ignoreIdentity, bool ignoreGenerateAlways)
    {
        var result = DbType.Equals(other.DbType)
            && Collation == other.Collation
            && StringComparer.InvariantCultureIgnoreCase.Equals(UserTypeName, other.UserDefinedTypeName)
            && Nullable == (other.Nullable.ToBool())
            && SizeEquals(other)
            && PrecisionEquals(other)
            && ScaleEquals(other)
            && (ignoreIdentity || Identity == other.Identity)
            && (ignorePrimaryKey || PrimaryKey == other.PrimaryKey)
            && (ignoreGenerateAlways || GeneratedAlwaysType == other.GetGeneratedAlwaysType())
            && ComputedEquals(other);

        if (!result)
            return false;

        return result;
    }

    public bool ScaleEquals(IColumn other)
    {
        return (other.Scale == null || other.Scale.Value == Scale);
    }

    public bool SizeEquals(IColumn other)
    {
        return (other.DbType.IsDecimal() || other.Size == null || other.Size.Value == Precision || other.Size.Value == Length || other.Size.Value == int.MaxValue && Length == -1);
    }

    public bool PrecisionEquals(IColumn other)
    {
        return (!other.DbType.IsDecimal() || other.Precision == null || other.Precision == 0 || other.Precision.Value == Precision);
    }

    public bool DefaultEquals(IColumn other)
    {
        if (other.Default == null && this.DefaultConstraint == null)
            return true;

        var result = CleanParenthesis(this.DefaultConstraint?.Definition) == CleanParenthesis(other.Default);

        return result;
    }

    public bool ComputedEquals(IColumn other)
    {
        if (other.ComputedColumn == null && this.ComputedColumn == null)
            return true;

        if (other.ComputedColumn?.Persisted != this.ComputedColumn?.Persisted)
            return false;

        if (CleanParenthesis(this.ComputedColumn?.Definition) != CleanParenthesis(other.ComputedColumn?.Expression))
            return false;

        return true;
    }

    public bool CheckEquals(IColumn other)
    {
        if (other.Check == null && this.CheckConstraint == null)
            return true;

        var result = this.CheckConstraint?.Definition == other.Check;

        return result;
    }

    private string? CleanParenthesis(string? p)
    {
        if (p == null)
            return null;

        return p.Replace("(", "").Replace(")", "").Replace("'", "").ToLower();
    }

    public DiffColumn Clone()
    {
        return new DiffColumn
        {
            Name = Name,
            ForeignKey = ForeignKey,
            DefaultConstraint = DefaultConstraint?.Let(dc => new DiffDefaultConstraint { Name = dc.Name, Definition = dc.Definition }),
            Identity = Identity,
            Length = Length,
            PrimaryKey = PrimaryKey,
            Nullable = Nullable,
            Precision = Precision,
            Scale = Scale,
            DbType = DbType,
            UserTypeName = UserTypeName,
        };
    }

    public override string ToString()
    {
        return this.Name;
    }

    internal bool CompatibleTypes(IColumn tabCol)
    {
        if (Schema.Current.Settings.IsPostgres)
            return CompatibleTypes_Postgres(this.DbType.PostgreSql, tabCol.DbType.PostgreSql);
        else
            return CompatibleTypes_SqlServer(this.DbType.SqlServer, tabCol.DbType.SqlServer);
    }

    private bool CompatibleTypes_Postgres(NpgsqlDbType fromType, NpgsqlDbType toType)
    {
        return true;
    }

    private bool CompatibleTypes_SqlServer(SqlDbType fromType, SqlDbType toType)
    {
        //https://docs.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql
        switch (fromType)
        {
            //BLACKLIST!!
            case SqlDbType.Binary:
            case SqlDbType.VarBinary:
                switch (toType)
                {
                    case SqlDbType.Float:
                    case SqlDbType.Real:
                    case SqlDbType.NText:
                    case SqlDbType.Text:
                        return false;
                    default:
                        return true;
                }

            case SqlDbType.Char:
            case SqlDbType.VarChar:
                return true;

            case SqlDbType.NChar:
            case SqlDbType.NVarChar:
                return toType != SqlDbType.Image;

            case SqlDbType.DateTime:
            case SqlDbType.SmallDateTime:
                switch (toType)
                {
                    case SqlDbType.UniqueIdentifier:
                    case SqlDbType.Image:
                    case SqlDbType.NText:
                    case SqlDbType.Text:
                    case SqlDbType.Xml:
                    case SqlDbType.Udt:
                        return false;
                    default:
                        return true;
                }

            case SqlDbType.Date:
                if (toType == SqlDbType.Time)
                    return false;
                goto case SqlDbType.DateTime2;

            case SqlDbType.Time:
                if (toType == SqlDbType.Date)
                    return false;
                goto case SqlDbType.DateTime2;

            case SqlDbType.DateTimeOffset:
            case SqlDbType.DateTime2:
                switch (toType)
                {
                    case SqlDbType.Decimal:
                    case SqlDbType.Float:
                    case SqlDbType.Real:
                    case SqlDbType.BigInt:
                    case SqlDbType.Int:
                    case SqlDbType.SmallInt:
                    case SqlDbType.TinyInt:
                    case SqlDbType.Money:
                    case SqlDbType.SmallMoney:
                    case SqlDbType.Bit:
                    case SqlDbType.UniqueIdentifier:
                    case SqlDbType.Image:
                    case SqlDbType.NText:
                    case SqlDbType.Text:
                    case SqlDbType.Xml:
                    case SqlDbType.Udt:
                        return false;
                    default:
                        return true;
                }

            case SqlDbType.Decimal:
            case SqlDbType.Float:
            case SqlDbType.Real:
            case SqlDbType.BigInt:
            case SqlDbType.Int:
            case SqlDbType.SmallInt:
            case SqlDbType.TinyInt:
            case SqlDbType.Money:
            case SqlDbType.SmallMoney:
            case SqlDbType.Bit:
                switch (toType)
                {
                    case SqlDbType.Date:
                    case SqlDbType.Time:
                    case SqlDbType.DateTimeOffset:
                    case SqlDbType.DateTime2:
                    case SqlDbType.UniqueIdentifier:
                    case SqlDbType.Image:
                    case SqlDbType.NText:
                    case SqlDbType.Text:
                    case SqlDbType.Xml:
                    case SqlDbType.Udt:
                        return false;
                    default:
                        return true;
                }

            case SqlDbType.Timestamp:
                switch (toType)
                {
                    case SqlDbType.NChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.Date:
                    case SqlDbType.Time:
                    case SqlDbType.DateTimeOffset:
                    case SqlDbType.DateTime2:
                    case SqlDbType.UniqueIdentifier:
                    case SqlDbType.Image:
                    case SqlDbType.NText:
                    case SqlDbType.Text:
                    case SqlDbType.Xml:
                    case SqlDbType.Udt:
                        return false;
                    default:
                        return true;
                }
            case SqlDbType.Variant:
                switch (toType)
                {
                    case SqlDbType.Timestamp:
                    case SqlDbType.Image:
                    case SqlDbType.NText:
                    case SqlDbType.Text:
                    case SqlDbType.Xml:
                    case SqlDbType.Udt:
                        return false;
                    default:
                        return true;
                }

            //WHITELIST!!
            case SqlDbType.UniqueIdentifier:
                switch (toType)
                {
                    case SqlDbType.Binary:
                    case SqlDbType.VarBinary:
                    case SqlDbType.Char:
                    case SqlDbType.VarChar:
                    case SqlDbType.NChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.UniqueIdentifier:
                    case SqlDbType.Variant:
                        return true;
                    default:
                        return false;
                }
            case SqlDbType.Image:
                switch (toType)
                {
                    case SqlDbType.Binary:
                    case SqlDbType.Image:
                    case SqlDbType.VarBinary:
                    case SqlDbType.Timestamp:
                        return true;
                    default:
                        return false;
                }
            case SqlDbType.NText:
            case SqlDbType.Text:
                switch (toType)
                {
                    case SqlDbType.Char:
                    case SqlDbType.VarChar:
                    case SqlDbType.NChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.NText:
                    case SqlDbType.Text:
                    case SqlDbType.Xml:
                        return true;
                    default:
                        return false;
                }
            case SqlDbType.Xml:
            case SqlDbType.Udt:
                switch (toType)
                {
                    case SqlDbType.Binary:
                    case SqlDbType.VarBinary:
                    case SqlDbType.Char:
                    case SqlDbType.VarChar:
                    case SqlDbType.NChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.Xml:
                    case SqlDbType.Udt:
                        return true;
                    default:
                        return false;
                }
            default:
                throw new NotImplementedException("Unexpected SqlDbType");
        }
    }

    internal bool SizeEquals(DiffColumn hisCol)
    {
        return Precision == hisCol.Precision ||
                Scale == hisCol.Scale ||
                Length == hisCol.Length;
    }
}

public class DiffCheckConstraint
{
    public string Name;
    public string Definition;
    public string? ColumnName;

    public override string ToString() => Name;
}

public class DiffForeignKey
{
    public ObjectName Name;
    public ObjectName TargetTable;
    public bool IsDisabled;
    public bool IsNotTrusted;
    public List<DiffForeignKeyColumn> Columns;

    public override string ToString() => Name.ToString();
}

public class DiffForeignKeyColumn
{
    public string Parent;
    public string Referenced;

    public override string ToString() => $"{Parent} -> {Referenced}";
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
