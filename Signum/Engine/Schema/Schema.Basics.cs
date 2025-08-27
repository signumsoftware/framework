using System.Data;
using Signum.Utilities.Reflection;
using Signum.Engine.Linq;
using NpgsqlTypes;
using Signum.Utilities.DataStructures;
using Signum.Engine.Sync;

namespace Signum.Engine.Maps;

public interface IFieldFinder
{
    Field GetField(MemberInfo value);
    Field? TryGetField(MemberInfo value);
    IEnumerable<Field> FindFields(Func<Field, bool> predicate);
}

public interface ITable
{
    ObjectName Name { get; }

    IColumn PrimaryKey { get; }

    Dictionary<string, IColumn> Columns { get; }

    List<TableIndex>? AdditionalIndexes { get; set; }

    List<TableIndex> AllIndexes();

    void GenerateColumns();

    SystemVersionedInfo? SystemVersioned { get; }

    bool IdentityBehaviour { get; }

    FieldEmbedded.EmbeddedHasValueColumn? GetHasValueColumn(IColumn column);

    SqlPartitionScheme? PartitionScheme { get; } 

}

public class SystemVersionedInfo
{
    public ObjectName TableName;
    public string? StartColumnName;
    public string? EndColumnName;
    public string? PostgresSysPeriodColumnName;

    public SystemVersionedInfo(ObjectName tableName, string startColumnName, string endColumnName)
    {
        TableName = tableName;
        StartColumnName = startColumnName;
        EndColumnName = endColumnName;
    }

    public SystemVersionedInfo(ObjectName tableName, string postgreeSysPeriodColumnName)
    {
        TableName = tableName;
        PostgresSysPeriodColumnName = postgreeSysPeriodColumnName;
    }

    internal IEnumerable<IColumn> Columns()
    {
        if (PostgresSysPeriodColumnName != null)
            return new[]
            {
                    new PostgresPeriodColumn(this.PostgresSysPeriodColumnName!),
                };
        else
            return new[]
            {
                    new SqlServerPeriodColumn(this.StartColumnName!, SystemVersionColumnType.Start),
                    new SqlServerPeriodColumn(this.EndColumnName!, SystemVersionColumnType.End)
                };
    }


    internal IntervalExpression? IntervalExpression(Alias tableAlias)
    {
        return new IntervalExpression(typeof(NullableInterval<DateTime>),
            StartColumnName == null ? null : new ColumnExpression(typeof(DateTime?), tableAlias, StartColumnName).SetMetadata(ExpressionMetadata.UTC),
            EndColumnName == null ? null : new ColumnExpression(typeof(DateTime?), tableAlias, EndColumnName).SetMetadata(ExpressionMetadata.UTC),
            PostgresSysPeriodColumnName == null ? null : new ColumnExpression(typeof(NpgsqlRange<DateTime>), tableAlias, PostgresSysPeriodColumnName).SetMetadata(ExpressionMetadata.UTC)
        );
    }

    public enum SystemVersionColumnType
    {
        Start,
        End,
    }

    public class SqlServerPeriodColumn : IColumn
    {
        public SqlServerPeriodColumn(string name, SystemVersionColumnType systemVersionColumnType)
        {
            this.Name = name;
            this.SystemVersionColumnType = systemVersionColumnType;
        }

        public string Name { get; private set; }
        public SystemVersionColumnType SystemVersionColumnType { get; private set; }

        public IsNullable Nullable => IsNullable.No;
        
        public AbstractDbType DbType => new AbstractDbType(SqlDbType.DateTime2);
        public Type Type => typeof(DateTime);
        public string? UserDefinedTypeName => null;
        public bool PrimaryKey => false;
        public bool IdentityBehaviour => false;
        public bool Identity => false;
        public string? Default { get; set; }
        public string? Check { get; set; }
        ComputedColumn? IColumn.ComputedColumn => null;
        public int? Size => null;
        public byte? Precision => null;
        public byte? Scale => null;
        public string? Collation => null;
        public Table? ReferenceTable => null;
        public bool AvoidForeignKey => false;

        public DateTimeKind DateTimeKind => DateTimeKind.Utc;


        public override string ToString()
        {
            return Name;
        }

    }

    public class PostgresPeriodColumn : IColumn
    {
        public PostgresPeriodColumn(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

        public IsNullable Nullable => IsNullable.No;
        public AbstractDbType DbType => new AbstractDbType(NpgsqlDbType.Range | NpgsqlDbType.TimestampTz);
        public Type Type => typeof(NpgsqlTypes.NpgsqlRange<DateTime>);
        public string? UserDefinedTypeName => null;
        public bool PrimaryKey => false;
        public bool IdentityBehaviour => false;
        public bool Identity => false;
        public string? Default { get; set; }
        public string? Check { get; set; }
        ComputedColumn? IColumn.ComputedColumn => null;
        public int? Size => null;
        public byte? Precision => null;
        public byte? Scale => null;
        public string? Collation => null;
        public Table? ReferenceTable => null;
        public bool AvoidForeignKey => false;

        public DateTimeKind DateTimeKind => DateTimeKind.Utc;

        public override string ToString()
        {
            return Name;
        }
    }

}

interface ITablePrivate
{
    ColumnExpression GetPrimaryOrder(Alias alias);
}

public partial class Table : IFieldFinder, ITable, ITablePrivate
{
    public Type Type { get; private set; }
    public Schema Schema { get; private set; }

    public ObjectName Name { get; set; }

    public bool IdentityBehaviour { get; internal set; }
    public bool IsView { get; internal set; }
    public string CleanTypeName { get; set; }

    public SystemVersionedInfo? SystemVersioned { get; set; }

    public Dictionary<string, EntityField> Fields { get; set; }
    public Dictionary<Type, FieldMixin>? Mixins { get; set; }

    public Dictionary<string, IColumn> Columns { get; set; }


    public List<TableIndex>? AdditionalIndexes { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Table(Type type)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        this.Type = type;
    }

    public override string ToString()
    {
        return Name.ToString();
    }

    public void GenerateColumns()
    {
        var errorSuffix = "columns in table " + this.Name.Name;
        var columns = new Dictionary<string, IColumn>();

        void AddColumns(IEnumerable<IColumn> newColumns)
        {
            try
            {
                columns.AddRange(newColumns, c => c.Name, c => c, errorSuffix);
            }
            catch (RepeatedElementsException ex) when (StartParameters.IgnoredCodeErrors != null)
            {
                StartParameters.IgnoredCodeErrors.Add(ex);
            }
        }

        AddColumns(Fields.Values.SelectMany(c => c.Field.Columns()));

        if (Mixins != null)
            AddColumns(Mixins.Values.SelectMany(m => m.Fields.Values).SelectMany(f => f.Field.Columns()));

        if (this.SystemVersioned != null)
            AddColumns(this.SystemVersioned.Columns());

        Columns = columns;
        inserterDisableIdentity = new ResetLazy<InsertCacheDisableIdentity>(() => InsertCacheDisableIdentity.InitializeInsertDisableIdentity(this));
        inserterIdentity = new ResetLazy<InsertCacheIdentity>(() => InsertCacheIdentity.InitializeInsertIdentity(this));
        updater = new ResetLazy<UpdateCache>(() => UpdateCache.InitializeUpdate(this));
        saveCollections = new ResetLazy<CollectionsCache?>(() => CollectionsCache.InitializeCollections(this));
    }

    public Field GetField(MemberInfo member)
    {
        Type? mixinType = member as Type ?? GetMixinType(member);
        if (mixinType != null)
        {
            if (Mixins == null)
                throw new InvalidOperationException("{0} has not mixins".FormatWith(this.Type.Name));

            return Mixins.GetOrThrow(mixinType);
        }

        FieldInfo fi = member as FieldInfo ?? Reflector.FindFieldInfo(Type, (PropertyInfo)member);

        if (fi == null)
            throw new InvalidOperationException("Field {0} not found on {1}".FormatWith(member.Name, Type));

        EntityField field = Fields.GetOrThrow(fi.Name, "Field {0} not found on schema");

        return field.Field;
    }

    public Field? TryGetField(MemberInfo member)
    {
        Type? mixinType = member as Type ?? GetMixinType(member);
        if (mixinType != null)
        {
            return Mixins?.TryGetC(mixinType);
        }

        FieldInfo fi = member as FieldInfo ?? Reflector.TryFindFieldInfo(Type, (PropertyInfo)member)!;

        if (fi == null)
            return null;

        EntityField? field = Fields.TryGetC(fi.Name);

        if (field == null)
            return null;

        return field.Field;
    }


    internal static Type? GetMixinType(MemberInfo member)
    {
        if (member is MethodInfo mi)
        {
            if (mi.IsGenericMethod && mi.GetGenericMethodDefinition().Name == "Mixin")
            {
                return mi.GetGenericArguments().SingleEx();
            }
        }
        return null;
    }


    public IEnumerable<Field> FindFields(Func<Field, bool> predicate)
    {
        var fields =
            Fields.Values.Select(a => a.Field).SelectMany(f => predicate(f) ? new[] { f } :
            f is IFieldFinder ff ? ff.FindFields(predicate) :
            Enumerable.Empty<Field>()).ToList();

        if (Mixins != null)
        {
            foreach (var mixin in this.Mixins.Values)
            {
                fields.AddRange(mixin.FindFields(predicate));
            }
        }
        return fields;
    }

    List<TableIndex>? allIndexes;
    public List<TableIndex> AllIndexes() => allIndexes ??= GeneratAllIndexes();

    List<TableIndex> GeneratAllIndexes()
    {
        IEnumerable<EntityField> fields = Fields.Values.AsEnumerable();
        if (Mixins != null)
            fields = fields.Concat(Mixins.Values.SelectMany(m => m.Fields.Values));

        var result = fields.SelectMany(f => f.Field.GenerateIndexes(this)).ToList();

        if (AdditionalIndexes != null)
            result.AddRange(AdditionalIndexes);

        if (result.Where(a=>a.Unique).Any())
        {
            var s = Schema.Current.Settings;
            List<IColumn> attachedFields = fields.Where(f => s.FieldAttributes(PropertyRoute.Root(this.Type).Add(f.FieldInfo))!.OfType<AttachToUniqueIndexesAttribute>().Any())
               .SelectMany(f => TableIndex.GetColumnsFromFields(f.Field))
               .ToList();

            if (attachedFields.Any())
            {
                result = result.Select(ix =>
                {
                    if (!ix.Unique || ix.AvoidAttachToUniqueIndexes)
                        return ix;

                    return new TableIndex(ix.Table, ix.Columns.Union(attachedFields).ToArray())
                    {
                        Unique = true,
                        Where = ix.Where
                    };
                }).ToList();
            }
        }

        if (this.SystemVersioned != null)
        {
            result.Add(new TableIndex(this, this.SystemVersioned.Columns().PreAnd(this.PrimaryKey).ToArray()));
        }

        return result;
    }

    public IEnumerable<KeyValuePair<Table, RelationInfo>> DependentTables()
    {
        var result = Fields.Values.SelectMany(f => f.Field.GetTables()).ToList();

        if (Mixins != null)
            result.AddRange(Mixins.Values.SelectMany(fm => fm.GetTables()));

        return result;
    }

    public IEnumerable<TableMList> TablesMList()
    {
        return this.AllFields().SelectMany(f => f.Field.TablesMList());
    }

    public FieldPrimaryKey PrimaryKey { get; internal set; }
    public FieldTicks? Ticks { get; internal set; }
    public FieldPartitionId? PartitionId { get; internal set; }

    IColumn ITable.PrimaryKey => PrimaryKey;

    public SqlPartitionScheme? PartitionScheme { get; set; }

    public void SetPartitionSchem(SqlPartitionScheme scheme)
    {
        this.PartitionScheme = scheme;
        foreach (var item in TablesMList())
        {
            item.PartitionScheme = scheme;
        }
    }

    public IEnumerable<EntityField> AllFields()
    {
        return this.Fields.Values.Concat(
            this.Mixins == null ? Enumerable.Empty<EntityField>() :
            this.Mixins.Values.SelectMany(fm => fm.Fields.Values));
    }

    public FieldEmbedded.EmbeddedHasValueColumn? GetHasValueColumn(IColumn column)
    {
        return this.AllFields().Select(a => a.Field).OfType<FieldEmbedded>().Select(a => a.GetHasValueColumn(column)).NotNull().SingleOrDefaultEx();
    }
}

public class EntityField
{
    public Field Field { get; set; }
    public FieldInfo FieldInfo { get; private set; }

    Type type;
    Func<object, object?>? getter;
    public Func<object, object?> Getter => getter ?? (getter = ReflectionTools.CreateGetter<object, object?>(FieldInfo)!);

    public EntityField(Type type, FieldInfo fi, Field field)
    {
        this.FieldInfo = fi;
        this.type = type;
        this.Field = field;
    }

    public override string ToString()
    {
        return FieldInfo.FieldName();
    }
}

public abstract partial class Field
{
    public Type FieldType { get; private set; }
    public PropertyRoute Route { get; private set; }
    public TableIndex? Index { get; set; }
    public TableIndex? UniqueIndex { get; set; }

    public Field(PropertyRoute route, Type? fieldType = null)
    {
        this.Route = route;
        this.FieldType = fieldType ?? route.Type;
    }


    public abstract IEnumerable<IColumn> Columns();

    public virtual IEnumerable<TableIndex> GenerateIndexes(ITable table)
    {
        if (UniqueIndex != null && Index != null)
            throw new InvalidOperationException($"Having both UniqueIndex and Index attributes on '{this.Route}' is not allowed");

        if (UniqueIndex != null)
            return new[] { UniqueIndex };

        if (Index != null)
            return new[] { Index };

        return Enumerable.Empty<TableIndex>();
    }

    public virtual TableIndex? GenerateUniqueIndex(ITable table, UniqueIndexAttribute? attribute)
    {
        if (attribute == null)
            return null;

        var result = new TableIndex(table, TableIndex.GetColumnsFromFields(this))
        {
            Unique = true,
            AvoidAttachToUniqueIndexes = attribute.AvoidAttachToUniqueIndexes
        };

        if (attribute.AllowMultipleNulls)
            result.Where = IndexWhereExpressionVisitor.IsNull(this, false, Schema.Current.Settings.IsPostgres);

        return result;
    }

    public virtual TableIndex? GenerateIndex(ITable table, IndexAttribute? attribute)
    {
        if (attribute == null)
            return null;

        var result = new TableIndex(table, TableIndex.GetColumnsFromFields(this));

        return result;
    }

    internal abstract IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables();

    internal abstract IEnumerable<TableMList> TablesMList();
}

public static class FieldExtensions
{
    public static bool Implements(this Field field, Type type)
    {
        if (field is FieldReference)
            return ((FieldReference)field).FieldType == type;

        if (field is FieldImplementedByAll)
            return true;

        if (field is FieldImplementedBy)
            return ((FieldImplementedBy)field).ImplementationColumns.ContainsKey(type);

        return false;
    }

    public static void AssertImplements(this Field field, Type type)
    {
        if (!Implements(field, type))
            throw new InvalidOperationException("{0} does not implement {1}".FormatWith(field.ToString(), type.Name));
    }

    public static ObjectName GetName(this ITable table, bool useHistoryName)
    {
        return useHistoryName && table.SystemVersioned != null ? table.SystemVersioned.TableName : table.Name;
    }
}

public partial interface IColumn
{
    string Name { get; }
    IsNullable Nullable { get; }
    AbstractDbType DbType { get; }
    DateTimeKind DateTimeKind { get; }
    Type Type { get; }
    string? UserDefinedTypeName { get; }
    bool PrimaryKey { get; }
    bool IdentityBehaviour { get; }
    bool Identity { get; }
    string? Default { get; }
    ComputedColumn? ComputedColumn { get; }
    string? Check { get; }
    int? Size { get; }
    byte? Precision { get; }
    byte? Scale { get; }
    string? Collation { get; }
    Table? ReferenceTable { get; }
    bool AvoidForeignKey { get; }
}

public struct ComputedColumn
{
    public ComputedColumn(string expression, bool persisted)
    {
        Expression = expression;
        Persisted = persisted;
    }

    public string Expression { get; }
    public bool Persisted { get; }
}

public enum IsNullable
{
    No,
    Yes,
    //Nullable only because in a Embedded nullabled
    Forced
}

public static partial class ColumnExtensions
{
    public static bool ToBool(this IsNullable isNullable)
    {
        return isNullable != IsNullable.No;
    }

    //public static string GetSqlDbTypeString(this IColumn column)
    //{
    //    return column.SqlDbType.ToString().ToUpper(CultureInfo.InvariantCulture) + SqlBuilder.GetSizeScale(column.Size, column.Scale);
    //}

    public static GeneratedAlwaysType GetGeneratedAlwaysType(this IColumn column)
    {
        if (column is SystemVersionedInfo.SqlServerPeriodColumn svc)
            return svc.SystemVersionColumnType == SystemVersionedInfo.SystemVersionColumnType.Start ? GeneratedAlwaysType.AsRowStart : GeneratedAlwaysType.AsRowEnd;

        return GeneratedAlwaysType.None;
    }
}

public interface IFieldReference
{
    bool IsLite { get; }
    bool ClearEntityOnSaving { get; set; }
    bool AvoidExpandOnRetrieving { get; }
    Type FieldType { get; }
}

public partial class FieldPrimaryKey : Field, IColumn
{
    public string Name { get; set; }
    IsNullable IColumn.Nullable => IsNullable.No;
    public AbstractDbType DbType { get; set; }
    public string? UserDefinedTypeName { get; set; }
    bool IColumn.PrimaryKey => true;
    public bool Identity { get; set; }
    bool IColumn.IdentityBehaviour => table.IdentityBehaviour;
    public int? Size { get; set; }
    byte? IColumn.Precision => null;
    byte? IColumn.Scale => null;
    public string? Collation { get; set; }
    Table? IColumn.ReferenceTable => null;
    public Type Type { get; set; }
    public bool AvoidForeignKey => false;
    public string? Default { get; set; }
    public string? Check { get; set; }
    ComputedColumn? IColumn.ComputedColumn => null;

    public DateTimeKind DateTimeKind => DateTimeKind.Unspecified;

    Table table;
    public FieldPrimaryKey(PropertyRoute route, Table table, string name, Type type)
        : base(route)
    {
        this.table = table;
        this.Name = name;
        this.Type = type;
    }

    public override string ToString()
    {
        return "{0} PrimaryKey".FormatWith(Name);
    }

    public override IEnumerable<IColumn> Columns()
    {
        return new[] { this };
    }

    public override IEnumerable<TableIndex> GenerateIndexes(ITable table)
    {
        if (this.UniqueIndex != null)
            throw new InvalidOperationException("Changing IndexType is not allowed for FieldPrimaryKey");

        if (table.PartitionScheme == null)
            return new[] { new TableIndex(table, this) { PrimaryKey = true, Clustered = true } };
        else
            return new[] {
                new TableIndex(table, this) { PrimaryKey = true },
                new TableIndex(table, this) { Clustered = true, Partitioned = true },
            };

    }

    internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
    {
        return Enumerable.Empty<KeyValuePair<Table, RelationInfo>>();
    }

    internal override IEnumerable<TableMList> TablesMList()
    {
        return Enumerable.Empty<TableMList>();
    }
}

public partial class FieldValue : Field, IColumn
{
    public string Name { get; set; }
    public IsNullable Nullable { get; set; }
    public AbstractDbType DbType { get; set; }
    public string? UserDefinedTypeName { get; set; }
    public bool PrimaryKey { get; set; }
    bool IColumn.Identity => false;
    bool IColumn.IdentityBehaviour => false;
    public int? Size { get; set; }
    public string? Collation { get; set; }
    public byte? Precision { get; set; }
    public byte? Scale { get; set; }
    Table? IColumn.ReferenceTable => null;
    public bool AvoidForeignKey => false;
    public string? Default { get; set; }
    public string? Check { get; set; }
    ComputedColumn? IColumn.ComputedColumn => null;
    public DateTimeKind DateTimeKind { get; set; }

    public FieldValue(PropertyRoute route, Type? fieldType, string name)
        : base(route, fieldType)
    {
        this.Name = name;
    }

    public override string ToString()
    {
        return "{0} {1} ({2},{3},{4},{5})".FormatWith(
            Name,
            DbType,
            Nullable.ToBool() ? "Nullable" : "",
            Size,
            Precision,
            Scale);
    }

    public override IEnumerable<IColumn> Columns()
    {
        return new[] { this };
    }

    internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
    {
        return Enumerable.Empty<KeyValuePair<Table, RelationInfo>>();
    }

    internal override IEnumerable<TableMList> TablesMList()
    {
        return Enumerable.Empty<TableMList>();
    }

    public virtual Type Type => this.Nullable.ToBool() ? this.FieldType.Nullify() : this.FieldType;
}

public partial class FieldTicks : FieldValue
{
    public new Type Type { get; set; }

    public FieldTicks(PropertyRoute route, Type type, string name)
        : base(route, null, name)
    {
        this.Type = type;
    }
}

public partial class FieldPartitionId : FieldValue
{
    public new Type Type { get; set; }

    public FieldPartitionId(PropertyRoute route, Type type, string name)
        : base(route, type, name)
    {
        this.Type = type;
    }
}

public partial class FieldEmbedded : Field, IFieldFinder
{
    public partial class EmbeddedHasValueColumn : IColumn
    {
        public string Name { get; set; }
        public IsNullable Nullable => IsNullable.No;  //even on neasted embeddeds
        public AbstractDbType DbType => new AbstractDbType(SqlDbType.Bit, NpgsqlDbType.Boolean);
        string? IColumn.UserDefinedTypeName => null;
        bool IColumn.PrimaryKey => false;
        bool IColumn.Identity => false;
        bool IColumn.IdentityBehaviour => false;
        int? IColumn.Size => null;
        byte? IColumn.Precision => null;
        byte? IColumn.Scale => null;
        string? IColumn.Collation => null;
        public Table? ReferenceTable => null;
        Type IColumn.Type => typeof(bool);
        public bool AvoidForeignKey => false;
        public string? Default { get; set; }
        public string? Check { get; set; }
        ComputedColumn? IColumn.ComputedColumn => null;
        public DateTimeKind DateTimeKind => DateTimeKind.Unspecified;

        public EmbeddedHasValueColumn(string name)
        {
            Name = name;
        }
    }

    public EmbeddedHasValueColumn? HasValue { get; set; }

    public Dictionary<string, EntityField> EmbeddedFields { get; set; }
    public Dictionary<Type, FieldMixin>? Mixins { get; set; }

    public FieldEmbedded(PropertyRoute route, EmbeddedHasValueColumn? hasValue, Dictionary<string, EntityField> embeddedFields, Dictionary<Type, FieldMixin>? mixins)
        : base(route)
    {
        this.HasValue = hasValue;
        this.EmbeddedFields = embeddedFields;
        this.Mixins = mixins;
    }

    public override string ToString()
    {
        return "\n".Combine(
            "Embedded",
            EmbeddedFields.ToString(c => "{0} : {1}".FormatWith(c.Key, c.Value), "\n").Indent(2),
            Mixins == null ? null : Mixins.ToString(m => "Mixin {0} : {1}".FormatWith(m.Key.Name, m.Value.ToString()), "\n")
            );
    }

    public Field GetField(MemberInfo member)
    {
        Type? mixinType = member as Type ?? Table.GetMixinType(member);
        if (mixinType != null)
        {
            if (Mixins == null)
                throw new InvalidOperationException("{0} has not mixins".FormatWith(this.Route.Type.Name));

            return Mixins.GetOrThrow(mixinType);
        }

        FieldInfo fi = member as FieldInfo ?? Reflector.FindFieldInfo(FieldType, (PropertyInfo)member);

        if (fi == null)
            throw new InvalidOperationException("Field {0} not found on {1}".FormatWith(member.Name, FieldType));

        EntityField field = EmbeddedFields.GetOrThrow(fi.Name, "Field {0} not found on schema");

        return field.Field;
    }

    public Field? TryGetField(MemberInfo member)
    {
        Type? mixinType = member as Type ?? Table.GetMixinType(member);
        if (mixinType != null)
        {
            return Mixins?.TryGetC(mixinType);
        }

        FieldInfo fi = member as FieldInfo ?? Reflector.TryFindFieldInfo(FieldType, (PropertyInfo)member)!;

        if (fi == null)
            return null;

        EntityField? field = EmbeddedFields.TryGetC(fi.Name);

        if (field == null)
            return null;

        return field.Field;
    }

    public IEnumerable<Field> FindFields(Func<Field, bool> predicate)
    {
        if (predicate(this))
            return new[] { this };

        var fields = EmbeddedFields.Values.Select(a => a.Field).SelectMany(f => predicate(f) ? new[] { f } :
            f is IFieldFinder ff ? ff.FindFields(predicate) :
            Enumerable.Empty<Field>()).ToList();

        if (Mixins != null)
        {
            foreach (var mixin in this.Mixins.Values)
            {
                fields.AddRange(mixin.FindFields(predicate));
            }
        }
        return fields;
    }

    public override IEnumerable<IColumn> Columns()
    {
        var result = new List<IColumn>();

        if (HasValue != null)
            result.Add(HasValue);

        result.AddRange(EmbeddedFields.Values.SelectMany(c => c.Field.Columns()));

        if (Mixins != null)
            result.AddRange(Mixins.Values.SelectMany(c => c.Columns()));

        return result;
    }

    public override IEnumerable<TableIndex> GenerateIndexes(ITable table)
    {
        return this.EmbeddedFields.Values.SelectMany(f => f.Field.GenerateIndexes(table))
            .Concat((this.Mixins?.Values.SelectMany(a => a.Fields.Values).SelectMany(f => f.Field.GenerateIndexes(table))).EmptyIfNull());
    }

    internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
    {

        foreach (var f in EmbeddedFields.Values)
        {
            foreach (var kvp in f.Field.GetTables())
            {
                yield return kvp;
            }
        }

        if (Mixins != null)
        {
            foreach (var mi in Mixins.Values)
            {
                foreach (var f in mi.Fields.Values)
                {
                    foreach (var kvp in f.Field.GetTables())
                    {
                        yield return kvp;
                    }
                }
            }
        }
    }

    internal override IEnumerable<TableMList> TablesMList()
    {
        return EmbeddedFields.Values.SelectMany(e => e.Field.TablesMList());
    }

    internal FieldEmbedded.EmbeddedHasValueColumn? GetHasValueColumn(IColumn column)
    {
        var enbeddedHasValue = this.EmbeddedFields.Select(a => a.Value.Field).OfType<FieldEmbedded>().Select(f => f.GetHasValueColumn(column)).NotNull().SingleOrDefaultEx();
        if (enbeddedHasValue != null)
            return enbeddedHasValue;

        var mixinHasValue = this.Mixins?.Select(a => a.Value).Select(f => f.GetHasValueColumn(column)).NotNull().SingleOrDefaultEx();
        if (mixinHasValue != null)
            return mixinHasValue;

        return this.Columns().Contains(column) ? this.HasValue : null;
    }
}

public partial class FieldMixin : Field, IFieldFinder
{
    public Dictionary<string, EntityField> Fields { get; set; }

    public ITable MainEntityTable;

    public FieldMixin(PropertyRoute route, ITable mainEntityTable, Dictionary<string, EntityField> fields)
        : base(route)
    {
        this.MainEntityTable = mainEntityTable;
        this.Fields = fields;
    }

    public override string ToString()
    {
        return "Mixin\n{0}".FormatWith(Fields.ToString(c => "{0} : {1}".FormatWith(c.Key, c.Value), "\n").Indent(2));
    }

    public Field GetField(MemberInfo member)
    {
        FieldInfo fi = member as FieldInfo ?? Reflector.FindFieldInfo(FieldType, (PropertyInfo)member);

        if (fi == null)
            throw new InvalidOperationException("Field {0} not found on {1}".FormatWith(member.Name, FieldType));

        EntityField field = Fields.GetOrThrow(fi.Name, "Field {0} not found on schema");

        return field.Field;
    }

    public Field? TryGetField(MemberInfo value)
    {
        FieldInfo fi = value as FieldInfo ?? Reflector.TryFindFieldInfo(FieldType, (PropertyInfo)value)!;

        if (fi == null)
            return null;

        EntityField? field = Fields.TryGetC(fi.Name);

        if (field == null)
            return null;

        return field.Field;
    }

    public IEnumerable<Field> FindFields(Func<Field, bool> predicate)
    {
        if (predicate(this))
            return new[] { this };

        return Fields.Values.Select(a => a.Field).SelectMany(f => predicate(f) ? new[] { f } :
            f is IFieldFinder ff ? ff.FindFields(predicate) :
            Enumerable.Empty<Field>()).ToList();
    }

    public override IEnumerable<IColumn> Columns()
    {
        var result = new List<IColumn>();
        result.AddRange(Fields.Values.SelectMany(c => c.Field.Columns()));

        return result;
    }

    public override IEnumerable<TableIndex> GenerateIndexes(ITable table)
    {
        throw new InvalidOperationException();
    }

    internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
    {
        foreach (var f in Fields.Values)
        {
            foreach (var kvp in f.Field.GetTables())
            {
                yield return kvp;
            }
        }
    }


    internal override IEnumerable<TableMList> TablesMList()
    {
        return Fields.Values.SelectMany(e => e.Field.TablesMList());
    }

    internal MixinEntity Getter(Entity ident)
    {
        return ((Entity)ident).GetMixin(FieldType);
    }

    internal FieldEmbedded.EmbeddedHasValueColumn? GetHasValueColumn(IColumn column)
    {
        return Fields.Select(a => a.Value.Field).OfType<FieldEmbedded>().Select(f => f.GetHasValueColumn(column)).NotNull().SingleOrDefaultEx();
    }
}

public partial class FieldReference : Field, IColumn, IFieldReference
{
    public string Name { get; set; }
    public IsNullable Nullable { get; set; }

    public bool PrimaryKey { get; set; } //For View
    bool IColumn.Identity => false;
    bool IColumn.IdentityBehaviour => false;
    int? IColumn.Size => this.ReferenceTable.PrimaryKey.Size;
    byte? IColumn.Precision => null;
    byte? IColumn.Scale => null;
    public Table ReferenceTable { get; set; }
    Table? IColumn.ReferenceTable => ReferenceTable;
    public AbstractDbType DbType => ReferenceTable.PrimaryKey.DbType;
    public string? Collation => ReferenceTable.PrimaryKey.Collation;
    public string? UserDefinedTypeName => ReferenceTable.PrimaryKey.UserDefinedTypeName;
    public virtual Type Type => this.Nullable.ToBool() ? ReferenceTable.PrimaryKey.Type.Nullify() : ReferenceTable.PrimaryKey.Type;

    public bool AvoidForeignKey { get; set; }

    public bool IsLite { get; internal set; }
    public Type? CustomLiteModelType { get; internal set; }

    public bool AvoidExpandOnRetrieving { get; set; }
    public string? Default { get; set; }
    public string? Check { get; set; }
    ComputedColumn? IColumn.ComputedColumn => null;
    public DateTimeKind DateTimeKind => DateTimeKind.Unspecified;

    public FieldReference(PropertyRoute route, Type? fieldType, string name, Table referenceTable) : base(route, fieldType)
    {
        this.Name = name;
        this.ReferenceTable = referenceTable;
    }

    public override string ToString()
    {
        return "{0} -> {1} {3} ({2})".FormatWith(
            Name,
            ReferenceTable.Name,
            IsLite ? "Lite" : "",
            Nullable.ToBool() ? "Nullable" : "");
    }

    public override IEnumerable<IColumn> Columns()
    {
        return new[] { this };
    }

    internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
    {
        yield return KeyValuePair.Create(ReferenceTable, new RelationInfo
        {
            IsLite = IsLite,
            IsCollection = false,
            IsNullable = Nullable.ToBool(),
            PropertyRoute = this.Route
        });
    }

    public override IEnumerable<TableIndex> GenerateIndexes(ITable table)
    {
        if (UniqueIndex == null)
            return new[] { new TableIndex(table, (IColumn)this) };

        return base.GenerateIndexes(table);
    }

    bool clearEntityOnSaving;
    public bool ClearEntityOnSaving
    {
        get
        {
            this.AssertIsLite();
            return this.clearEntityOnSaving;
        }
        set
        {
            this.AssertIsLite();
            this.clearEntityOnSaving = value;
        }
    }


    internal override IEnumerable<TableMList> TablesMList()
    {
        return Enumerable.Empty<TableMList>();
    }
}

public partial class FieldEnum : FieldReference, IColumn
{
    public override Type Type
    {
        get
        {
            if (this.ReferenceTable != null)
                return base.Type;

            var ut = Enum.GetUnderlyingType(this.FieldType.UnNullify());

            return this.Nullable.ToBool() ? ut.Nullify() : ut;
        }
    }

    public FieldEnum(PropertyRoute route, string name, Table referenceTable) : base(route, null, name, referenceTable) { }

    public override string ToString()
    {
        return "{0} -> {1} {3} ({2})".FormatWith(
            Name,
            "-",
            IsLite ? "Lite" : "",
            Nullable.ToBool() ? "Nullable" : "");
    }

    internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
    {
        if (ReferenceTable == null)
            yield break;

        yield return KeyValuePair.Create(ReferenceTable, new RelationInfo
        {
            IsLite = IsLite,
            IsCollection = false,
            IsNullable = Nullable.ToBool(),
            IsEnum = true,
            PropertyRoute = this.Route
        });
    }

    internal override IEnumerable<TableMList> TablesMList()
    {
        return Enumerable.Empty<TableMList>();
    }
}

public partial class FieldImplementedBy : Field, IFieldReference
{
    public bool IsLite { get; internal set; }
    public CombineStrategy SplitStrategy { get; internal set; }
    public bool AvoidExpandOnRetrieving { get; internal set; }

    public Dictionary<Type, ImplementationColumn> ImplementationColumns { get; set; }

    public FieldImplementedBy(PropertyRoute route, Dictionary<Type, ImplementationColumn> implementations) : base(route, null)
    {
        this.ImplementationColumns = implementations;
    }

    public override string ToString()
    {
        return "ImplementedBy\n{0}".FormatWith(ImplementationColumns.ToString(k => "{0} -> {1} ({2})".FormatWith(k.Value.Name, k.Value.ReferenceTable.Name, k.Key.Name), "\n").Indent(2));
    }

    public override IEnumerable<IColumn> Columns()
    {
        return ImplementationColumns.Values.Cast<IColumn>();
    }

    internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
    {
        return ImplementationColumns.Select(a => KeyValuePair.Create(a.Value.ReferenceTable, new RelationInfo
        {
            IsLite = IsLite,
            IsCollection = false,
            IsNullable = a.Value.Nullable.ToBool(),
            PropertyRoute = this.Route
        }));
    }

    public override IEnumerable<TableIndex> GenerateIndexes(ITable table)
    {
        return this.Columns().Select(c => new TableIndex(table, c)).Concat(base.GenerateIndexes(table));
    }

    bool clearEntityOnSaving;
    public bool ClearEntityOnSaving
    {
        get
        {
            this.AssertIsLite();
            return this.clearEntityOnSaving;
        }
        set
        {
            this.AssertIsLite();
            this.clearEntityOnSaving = value;
        }
    }

    internal override IEnumerable<TableMList> TablesMList()
    {
        return Enumerable.Empty<TableMList>();
    }
}

public partial class FieldImplementedByAll : Field, IFieldReference
{
    public bool IsLite { get; internal set; }

    public bool AvoidExpandOnRetrieving { get; internal set; }

    public Dictionary<Type/*PrimaryKeyType*/, ImplementedByAllIdColumn> IdColumns { get; set; }

    public ImplementationColumn TypeColumn { get; set; }

    public Dictionary<Type, Type>? CustomLiteModelTypes;

    public FieldImplementedByAll(PropertyRoute route, IEnumerable<ImplementedByAllIdColumn> columnIds, ImplementationColumn columnType) : base(route)
    {
        this.IdColumns = columnIds.ToDictionaryEx(a => a.Type.UnNullify());
        this.TypeColumn = columnType;
    }

    public override IEnumerable<IColumn> Columns()
    {
        yield return TypeColumn;

        foreach (var item in IdColumns.Values)
        {
            yield return item;
        }
    }

    internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
    {
        yield return KeyValuePair.Create(TypeColumn.ReferenceTable, new RelationInfo
        {
            IsNullable = this.TypeColumn.Nullable.ToBool(),
            IsLite = this.IsLite,
            IsImplementedByAll = true,
            PropertyRoute = this.Route
        });
    }

    bool clearEntityOnSaving;
    public bool ClearEntityOnSaving
    {
        get
        {
            this.AssertIsLite();
            return this.clearEntityOnSaving;
        }
        set
        {
            this.AssertIsLite();
            this.clearEntityOnSaving = value;
        }
    }

    public override IEnumerable<TableIndex> GenerateIndexes(ITable table)
    {
        var baseIndexes = this.IdColumns.Values.Select(idCol => new TableIndex(table, this.TypeColumn, idCol)).ToList();

        if (UniqueIndex == null)
            return baseIndexes;

        return baseIndexes.Concat(base.GenerateIndexes(table));
    }

    internal override IEnumerable<TableMList> TablesMList()
    {
        return Enumerable.Empty<TableMList>();
    }
}

public partial class ImplementationColumn : IColumn
{
    public string Name { get; set; }
    public IsNullable Nullable { get; set; }
    bool IColumn.PrimaryKey => false;
    bool IColumn.Identity => false;
    bool IColumn.IdentityBehaviour => false;
    int? IColumn.Size => null;
    byte? IColumn.Precision => null;
    byte? IColumn.Scale => null;
    public Table ReferenceTable { get; private set; }
    Table? IColumn.ReferenceTable => ReferenceTable;
    public AbstractDbType DbType => ReferenceTable.PrimaryKey.DbType;
    public string? Collation => ReferenceTable.PrimaryKey.Collation;
    public string? UserDefinedTypeName => ReferenceTable.PrimaryKey.UserDefinedTypeName;
    public Type Type => this.Nullable.ToBool() ? ReferenceTable.PrimaryKey.Type.Nullify() : ReferenceTable.PrimaryKey.Type;
    public bool AvoidForeignKey { get; set; }
    public string? Default { get; set; }
    public string? Check { get; set; }
    ComputedColumn? IColumn.ComputedColumn => null;
    public Type? CustomLiteModelType { get; internal set; }
    public DateTimeKind DateTimeKind => DateTimeKind.Unspecified;

    public ImplementationColumn(string name, Table referenceTable)
    {
        Name = name;
        ReferenceTable = referenceTable;
    }

    public override string ToString()
    {
        return this.Name;
    }
}

public partial class ImplementedByAllIdColumn : IColumn
{
    public string Name { get; set; }
    public IsNullable Nullable { get; set; }
    string? IColumn.UserDefinedTypeName => null;
    bool IColumn.PrimaryKey => false;
    bool IColumn.Identity => false;
    bool IColumn.IdentityBehaviour => false;
    public int? Size { get; set; }
    byte? IColumn.Precision => null;
    byte? IColumn.Scale => null;
    public string? Collation { get; set; }
    public Table? ReferenceTable => null;
    public AbstractDbType DbType { get; private set; }
    public Type Type { get; private set;  }
    public bool AvoidForeignKey => false;
    public string? Default { get; set; }
    public string? Check { get; set; }
    ComputedColumn? IColumn.ComputedColumn => null;
    public DateTimeKind DateTimeKind => DateTimeKind.Unspecified;

    public ImplementedByAllIdColumn(string name, Type type, AbstractDbType dbType)
    {
        Name = name;
        this.DbType = dbType;
        this.Type = type;
    }

    public override string ToString()
    {
        return this.Name;
    }
}

public partial class FieldMList : Field, IFieldFinder
{
    public TableMList TableMList { get; set; }

    public FieldMList(PropertyRoute route, TableMList tableMList) : base(route)
    {
        this.TableMList = tableMList;
    }

    public override string ToString()
    {
        return "Coleccion\n{0}".FormatWith(TableMList.ToString().Indent(2));
    }

    public Field GetField(MemberInfo member)
    {
        if (member.Name == "Item")
            return TableMList.Field;

        throw new InvalidOperationException("{0} not supported by MList field".FormatWith(member.Name));
    }

    public Field? TryGetField(MemberInfo member)
    {
        if (member.Name == "Item")
            return TableMList.Field;

        return null;
    }

    public IEnumerable<Field> FindFields(Func<Field, bool> predicate)
    {
        if (predicate(this))
            return new[] { this };

        return TableMList.FindFields(predicate);

    }

    public override IEnumerable<IColumn> Columns()
    {
        return Array.Empty<IColumn>();
    }

    public override IEnumerable<TableIndex> GenerateIndexes(ITable table)
    {
        if (UniqueIndex != null)
            throw new InvalidOperationException("Changing IndexType is not allowed for FieldMList");

        return Enumerable.Empty<TableIndex>();
    }

    internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
    {
        foreach (var kvp in TableMList.GetTables())
        {
            kvp.Value.IsCollection = true;
            yield return kvp;
        }
    }


    internal override IEnumerable<TableMList> TablesMList()
    {
        return new[] { TableMList };
    }
}

public partial class TableMList : ITable, IFieldFinder, ITablePrivate
{
    public class PrimaryKeyColumn : IColumn
    {
        public string Name { get; set; }
        IsNullable IColumn.Nullable => IsNullable.No;
        public AbstractDbType DbType { get; set; }
        public string? Collation { get; set; }
        public string? UserDefinedTypeName { get; set; }
        bool IColumn.PrimaryKey => true;
        public bool Identity { get; set; }
        bool IColumn.IdentityBehaviour => true;
        int? IColumn.Size => null;
        byte? IColumn.Precision => null;
        byte? IColumn.Scale => null;
        Table? IColumn.ReferenceTable => null;
        public Type Type { get; set; }
        public bool AvoidForeignKey => false;
        public string? Default { get; set; }
        public string? Check { get; set; }
        ComputedColumn? IColumn.ComputedColumn => null;
        public DateTimeKind DateTimeKind => DateTimeKind.Unspecified;



        public PrimaryKeyColumn(Type type, string name)
        {
            Type = type;
            Name = name;
        }

    }

    public Dictionary<string, IColumn> Columns { get; set; }
    public List<TableIndex>? AdditionalIndexes { get; set; }

    public ObjectName Name { get; set; }
    public PrimaryKeyColumn PrimaryKey { get; set; }
    public FieldReference BackReference { get; set; }
    public FieldValue? Order { get; set; }
    public FieldPartitionId PartitionId { get; internal set; }
    public Field Field { get; set; }

    public SystemVersionedInfo? SystemVersioned { get; set; }
    public SqlPartitionScheme? PartitionScheme { get; set; }

    public Type CollectionType { get; private set; }

    public PropertyRoute PropertyRoute { get; internal set; }
    Func<Entity, IMListPrivate>? getter;
    public Func<Entity, IMListPrivate> Getter => getter ?? (getter = PropertyRoute.GetLambdaExpression<Entity, IMListPrivate>(true).Compile());

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    public TableMList(Type collectionType, ObjectName name, PrimaryKeyColumn primaryKey, FieldReference backReference)
    {
        this.Name = name;
        this.PrimaryKey = primaryKey;
        this.BackReference = backReference;
        this.CollectionType = collectionType;
        this.cache = new Lazy<IMListCache>(() => (IMListCache)giCreateCache.GetInvoker(this.Field!.FieldType)(this));
    }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public override string ToString()
    {
        return "[{0}]\n  {1}\n  {2}".FormatWith(Name, BackReference.Name, Field.ToString());
    }

    public void GenerateColumns()
    {
        var errorSuffix = "columns in table " + this.Name.Name;
        var columns = new Dictionary<string, IColumn>();

        void AddColumns(params IEnumerable<IColumn> newColumns)
        {
            try
            {
                columns.AddRange(newColumns, c => c.Name, c => c, errorSuffix);
            }
            catch (RepeatedElementsException ex) when (StartParameters.IgnoredCodeErrors != null)
            {
                StartParameters.IgnoredCodeErrors.Add(ex);
            }
        }

        AddColumns(PrimaryKey);
        AddColumns(BackReference);

        if (Order != null)
            AddColumns(Order);

        if (PartitionId != null)
            AddColumns(PartitionId);

        AddColumns(Field.Columns());

        if (this.SystemVersioned != null)
            AddColumns(this.SystemVersioned.Columns());

        Columns = columns;
    }

    List<TableIndex>? allIndexes;
    public List<TableIndex> AllIndexes() => allIndexes ??= GeneratAllIndexes();

    List<TableIndex> GeneratAllIndexes()
    {
        var result = new List<TableIndex>();

        if (PartitionScheme == null)
            result.Add(new TableIndex(this, this.PrimaryKey) { Clustered = true, PrimaryKey = true });
        else
        {
            result.Add(new TableIndex(this, this.PrimaryKey) { PrimaryKey = true });
            result.Add(new TableIndex(this, this.PrimaryKey) { Clustered = true, Partitioned = true });
        }

        result.AddRange(BackReference.GenerateIndexes(this));
        result.AddRange(Field.GenerateIndexes(this));

        if (AdditionalIndexes != null)
            result.AddRange(AdditionalIndexes);

        return result;
    }

    public Field GetField(MemberInfo member)
    {
        Field? result = TryGetField(member);

        if (result == null)
            throw new InvalidOperationException("'{0}' not found".FormatWith(member.Name));

        return result;
    }

    public Field? TryGetField(MemberInfo mi)
    {
        if (mi.Name == "Parent")
            return this.BackReference;

        if (mi.Name == "Element")
            return this.Field;

        return null;
    }

    public IEnumerable<Field> FindFields(Func<Field, bool> predicate)
    {
        if (predicate(this.BackReference))
            yield return this.BackReference;

        if (this.Order != null && predicate(this.Order))
            yield return this.Order;

        if (this.PartitionId != null && predicate(this.PartitionId))
            yield return this.PartitionId;

        if (predicate(this.Field))
            yield return this.Field;
        else if (this.Field is IFieldFinder ff)
        {
            foreach (var f in ff.FindFields(predicate))
            {
                yield return f;
            }
        }
    }

    public void ToDatabase(DatabaseName databaseName)
    {
        this.Name = this.Name.OnDatabase(databaseName);
    }

    public void ToSchema(SchemaName schemaName)
    {
        this.Name = this.Name.OnSchema(schemaName);
    }


    IColumn ITable.PrimaryKey => PrimaryKey;

    public bool IdentityBehaviour => true; //For now

    internal object?[] BulkInsertDataRow(Entity entity, object value, int order, PrimaryKey? rowId)
    {
        return this.cache.Value.BulkInsertDataRow(entity, value, order, rowId);
    }

    public IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
    {
        return this.Field.GetTables();
    }

    public FieldEmbedded.EmbeddedHasValueColumn? GetHasValueColumn(IColumn column)
    {
        if (this.Field is FieldEmbedded f)
            return f.GetHasValueColumn(column);

        return null;
    }
}

public struct AbstractDbType : IEquatable<AbstractDbType>
{
    SqlDbType? sqlServer;
    public SqlDbType SqlServer => sqlServer ?? throw new InvalidOperationException("No SqlDbType type defined");

    NpgsqlDbType? postgreSql;
    public NpgsqlDbType PostgreSql => postgreSql ?? throw new InvalidOperationException("No PostgresSql type defined");

    public bool HasPostgres => postgreSql.HasValue;
    public bool HasSqlServer => sqlServer.HasValue;

    public AbstractDbType(SqlDbType sqlDbType)
    {
        this.sqlServer = sqlDbType;
        this.postgreSql = null;
    }

    public AbstractDbType(NpgsqlDbType npgsqlDbType)
    {
        this.sqlServer = null;
        this.postgreSql = npgsqlDbType;
    }

    public AbstractDbType(SqlDbType sqlDbType, NpgsqlDbType npgsqlDbType)
    {
        this.sqlServer = sqlDbType;
        this.postgreSql = npgsqlDbType;
    }

    public override bool Equals(object? obj) => obj is AbstractDbType adt && Equals(adt);
    public bool Equals(AbstractDbType adt) =>
        Schema.Current.Settings.IsPostgres ?
        this.postgreSql == adt.postgreSql :
        this.sqlServer == adt.sqlServer;
    public override int GetHashCode() => this.postgreSql.GetHashCode() ^ this.sqlServer.GetHashCode();

    public bool IsDate()
    {
        if (sqlServer is SqlDbType s)
            switch (s)
            {
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime:
                    return true;
                default:
                    return false;
            }

        if (postgreSql is NpgsqlDbType p)
            switch (p)
            {
                case NpgsqlDbType.Date:
                case NpgsqlDbType.Timestamp:
                case NpgsqlDbType.TimestampTz:
                    return true;
                default:
                    return false;
            }

        throw new NotImplementedException();
    }

    public bool IsTime()
    {
        if (sqlServer is SqlDbType s)
            switch (s)
            {
                case SqlDbType.Time:
                    return true;
                default:
                    return false;
            }

        if (postgreSql is NpgsqlDbType p)
            switch (p)
            {
                case NpgsqlDbType.Time:
                case NpgsqlDbType.TimeTz:
                    return true;
                default:
                    return false;
            }

        throw new NotImplementedException();
    }

    public bool IsNumber()
    {
        if (sqlServer is SqlDbType s)
            switch (s)
            {
                case SqlDbType.BigInt:
                case SqlDbType.Float:
                case SqlDbType.Decimal:
                case SqlDbType.Int:
                case SqlDbType.Bit:
                case SqlDbType.Money:
                case SqlDbType.Real:
                case SqlDbType.TinyInt:
                case SqlDbType.SmallInt:
                case SqlDbType.SmallMoney:
                    return true;
                default:
                    return false;
            }

        if (postgreSql is NpgsqlDbType p)
            switch (p)
            {
                case NpgsqlDbType.Smallint:
                case NpgsqlDbType.Integer:
                case NpgsqlDbType.Bigint:
                case NpgsqlDbType.Numeric:
                case NpgsqlDbType.Money:
                case NpgsqlDbType.Real:
                case NpgsqlDbType.Double:
                    return true;
                default:
                    return false;
            }

        throw new NotImplementedException();
    }

    public bool IsString()
    {
        if (sqlServer is SqlDbType s)
            switch (s)
            {
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.VarChar:
                case SqlDbType.Char:
                case SqlDbType.NChar:
                    return true;
                default:
                    return false;
            }


        if (postgreSql is NpgsqlDbType p)
            switch (p)
            {
                case NpgsqlDbType.Char:
                case NpgsqlDbType.Varchar:
                case NpgsqlDbType.Text:
                    return true;
                default:
                    return false;
            }

        throw new NotImplementedException();
    }


    public override string ToString() => ToString(Schema.Current.Settings.IsPostgres);
    public string ToString(bool isPostgres)
    {
        if (!isPostgres)
            return sqlServer.ToString()!.ToUpperInvariant();

        var pg = postgreSql!.Value;
        if ((pg & NpgsqlDbType.Array) != 0)
            return (pg & ~NpgsqlDbType.Range).ToString() + "[]";

        if ((pg & NpgsqlDbType.Range) != 0)
            switch (pg & ~NpgsqlDbType.Range)
            {
                case NpgsqlDbType.Integer: return "int4range";
                case NpgsqlDbType.Bigint: return "int8range";
                case NpgsqlDbType.Numeric: return "numrange";
                case NpgsqlDbType.TimestampTz: return "tstzrange";
                case NpgsqlDbType.Date: return "daterange";
                default:
                    throw new UnexpectedValueException(pg);
            }

        if (pg == NpgsqlDbType.Double)
            return "double precision";

        return pg.ToString()!;
    }

    public bool IsBoolean()
    {
        if (sqlServer is SqlDbType s)
            switch (s)
            {
                case SqlDbType.Bit:
                    return true;
                default:
                    return false;
            }


        if (postgreSql is NpgsqlDbType p)
            switch (p)
            {
                case NpgsqlDbType.Boolean:
                    return true;
                default:
                    return false;
            }

        throw new NotImplementedException();
    }

    public bool IsGuid()
    {
        if (sqlServer is SqlDbType s)
            switch (s)
            {
                case SqlDbType.UniqueIdentifier:
                    return true;
                default:
                    return false;
            }


        if (postgreSql is NpgsqlDbType p)
            switch (p)
            {
                case NpgsqlDbType.Uuid:
                    return true;
                default:
                    return false;
            }

        throw new NotImplementedException();
    }

    internal bool IsDecimal()
    {
        if (sqlServer is SqlDbType s)
            switch (s)
            {
                case SqlDbType.Decimal:
                    return true;
                default:
                    return false;
            }

        if (postgreSql is NpgsqlDbType p)
            switch (p)
            {
                case NpgsqlDbType.Numeric:
                    return true;
                default:
                    return false;
            }

        throw new NotImplementedException();
    }
}



