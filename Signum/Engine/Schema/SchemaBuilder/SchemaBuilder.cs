using Signum.API;
using Signum.DynamicQuery;
using Signum.Engine.Linq;
using Signum.Engine.Sync;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Collections.Immutable;
using System.IO;
using System.Security.AccessControl;

namespace Signum.Engine.Maps;

public class SchemaBuilder
{
    Schema schema;
    public SchemaSettings Settings
    {
        get { return schema.Settings; }
    }

    public bool IsPostgres
    {
        get { return schema.Settings.IsPostgres; }
    }

    public WebServerBuilder? WebServerBuilder { get; init; }

    public SchemaBuilder()
    {
        schema = new Schema(new SchemaSettings());

        schema.Settings.AssertNotIncluded = MixinDeclarations.AssertNotIncluded = t =>
        {
            if (schema.Tables.ContainsKey(t))
                throw new InvalidOperationException("{0} is already included in the Schema".FormatWith(t.TypeName()));
        };

        schema.SchemaCompleted += () =>
        {
            schema.Assets.CreateInitialAssets(this);
        };
    }


    protected SchemaBuilder(Schema schema)
    {
        this.schema = schema;
    }

    public SchemaBuilder(SchemaSettings settings)
    {
        schema = new Schema(settings);
    }

    public Schema Schema
    {
        get { return schema; }
    }


    public void AddUniqueIndex<T>(Expression<Func<T, object?>> fields, Expression<Func<T, bool>>? where = null, Expression<Func<T, object?>>? includeFields = null, bool onlyOneNull_SqlServerOnly = false) where T : Entity
    {
        var table = Schema.Table<T>();

        var pairs = IndexKeyColumns.Split(table, fields)!;

        var includedColumns = includeFields == null ? null : IndexKeyColumns.Split(table, includeFields).SelectMany(a => a.columns).ToArray();
        var globalWhere = where == null ? null : IndexWhereExpressionVisitor.GetIndexWhere(where, table);

        if (onlyOneNull_SqlServerOnly)
        {
            if (Settings.IsPostgres)
                throw new InvalidOperationException("onlyOneNull_SqlServerOnly is not supported in Postgres");

            var index = AddUniqueIndex(table, pairs.SelectMany(a => a.columns).ToArray());
            index.IncludeColumns = includedColumns;
            index.Where = globalWhere;
        }
        else
        {
            AddMultiUniqueIndex(table, pairs, includedColumns, globalWhere);
        }
    }

    private void AddMultiUniqueIndex(ITable table, (Field? field, IColumn[] columns)[] columnBlocks, IColumn[]? includedColumns, string? globalWhere)
    {
        var isPostgres = Settings.IsPostgres;
        void Recursive(int i, IColumn[] prevColumns, string prevWhere)
        {
            if (columnBlocks.Length == i)
            {
                var index = AddUniqueIndex(table, prevColumns);
                index.IncludeColumns = includedColumns;
                index.Where = " AND ".Combine(prevWhere, globalWhere);
            }
            else
            {
                var c = columnBlocks[i];
                if (c.field is FieldImplementedBy fib)
                {
                    foreach (var imp in fib.ImplementationColumns)
                    {
                        var filter = imp.Value.Nullable == IsNullable.No ? null :
                            $"{imp.Value.Name.SqlEscape(isPostgres)} IS NOT NULL";

                        Recursive(i + 1, [.. prevColumns, imp.Value], " AND ".Combine(prevWhere, filter));
                    }
                }

                else if (c.field is FieldImplementedByAll fiba)
                {
                    foreach (var imp in fiba.IdColumns)
                    {
                        var filter = imp.Value.Nullable == IsNullable.No ? null :
                            $"({fiba.TypeColumn.Name.SqlEscape(isPostgres)} IS NOT NULL AND {imp.Value.Name.SqlEscape(isPostgres)} IS NOT NULL)";

                        Recursive(i + 1, [.. prevColumns, fiba.TypeColumn, imp.Value], " AND ".Combine(prevWhere, filter));
                    }
                }
                else
                {
                    var filter = c.columns.Where(a => a.Nullable != IsNullable.No).ToString(a =>
                    a.Type == typeof(string) ? $"{a.Name.SqlEscape(isPostgres)} IS NOT NULL AND {a.Name.SqlEscape(isPostgres)} <> ''" :
                    $"{a.Name.SqlEscape(isPostgres)} IS NOT NULL", " AND ");
                    Recursive(i + 1, [.. prevColumns, .. c.columns], " AND ".Combine(prevWhere, filter));
                }
            }
        }

        Recursive(0, [], "");
    }

    public TableIndex AddIndex<T>(Expression<Func<T, object?>> fields,
        Expression<Func<T, bool>>? where = null,
        Expression<Func<T, object>>? includeFields = null) where T : Entity
    {
        var table = Schema.Table<T>();

        IColumn[] columns = IndexKeyColumns.Split(table, fields).SelectMany(a=>a.columns).ToArray();

        var index = new TableIndex(table, columns);

        if (where != null)
            index.Where = IndexWhereExpressionVisitor.GetIndexWhere(where, table);

        if (includeFields != null)
        {
            index.IncludeColumns = IndexKeyColumns.Split(table, includeFields).SelectMany(a => a.columns).ToArray();
        }

        AddIndex(index);

        return index;
    }

    public FullTextTableIndex AddFullTextIndex<T>(Expression<Func<T, object?>> fields, Action<FullTextTableIndex>? customize = null) where T : Entity
    {
        var table = Schema.Table<T>();

        IColumn[] columns = IndexKeyColumns.Split(table, fields).SelectMany(a => a.columns).ToArray();

        return AddFullTextIndex(table, columns, customize);
    }

    public void AddUniqueIndexMList<T, V>(Expression<Func<T, MList<V>>> toMList,
        Expression<Func<MListElement<T, V>, object>> fields,
        Expression<Func<MListElement<T, V>, bool>>? where = null,
        Expression<Func<MListElement<T, V>, object>>? includeFields = null)
        where T : Entity
    {
        TableMList table = ((FieldMList)Schema.FindField(Schema.Table(typeof(T)), Reflector.GetMemberList(toMList))).TableMList;

        var columns = IndexKeyColumns.Split(table, fields);

        var includedColumns = includeFields == null ? null : IndexKeyColumns.Split(table, includeFields).SelectMany(a => a.columns).ToArray();

        var globalWhere = where == null ? null : IndexWhereExpressionVisitor.GetIndexWhere(where, table);

        AddMultiUniqueIndex(table, columns, includedColumns, globalWhere);
    }

    public TableIndex AddIndexMList<T, V>(Expression<Func<T, MList<V>>> toMList,
        Expression<Func<MListElement<T, V>, object>> fields,
        Expression<Func<MListElement<T, V>, bool>>? where = null,
        Expression<Func<MListElement<T, V>, object>>? includeFields = null)
        where T : Entity
    {
        TableMList table = ((FieldMList)Schema.FindField(Schema.Table(typeof(T)), Reflector.GetMemberList(toMList))).TableMList;

        IColumn[] columns = IndexKeyColumns.Split(table, fields).SelectMany(a => a.columns).ToArray();

        var index = AddIndex(table, columns);

        if (where != null)
            index.Where = IndexWhereExpressionVisitor.GetIndexWhere(where, table);

        if (includeFields != null)
        {
            index.IncludeColumns = IndexKeyColumns.Split(table, includeFields).SelectMany(a => a.columns).ToArray();
        }

        return index;
    }

    public FullTextTableIndex AddFullTextIndexMList<T, V>(Expression<Func<T, MList<V>>> toMList,
        Expression<Func<MListElement<T, V>, object>> fields, Action<FullTextTableIndex>? customize = null)
        where T : Entity
    {
        TableMList table = ((FieldMList)Schema.FindField(Schema.Table(typeof(T)), Reflector.GetMemberList(toMList))).TableMList;

        IColumn[] columns = IndexKeyColumns.Split(table, fields).SelectMany(a => a.columns).ToArray();

        return AddFullTextIndex(table, columns, customize);
    }

    public FullTextTableIndex AddFullTextIndex(ITable table, Field[] fields, Action<FullTextTableIndex>? customize)
    {
        return AddFullTextIndex(table, fields.SelectMany(f => TableIndex.GetColumnsFromField(f)).ToArray(), customize);
    }

    public FullTextTableIndex AddFullTextIndex(ITable table, IColumn[] columns, Action<FullTextTableIndex>? customize)
    {
        var index = new FullTextTableIndex(table, columns);
        customize?.Invoke(index);
        AddIndex(index);

        foreach (var col in index.GenerateColumns())
        {
            table.Columns.Add(col.Name, col);
        }

        return index;
    }

    public void AddUniqueIndex(ITable table, Field[] fields)
    {
        var dic = fields.ToDictionary(f => f, f => TableIndex.GetColumnsFromField(f));





        var index = new TableIndex(table, dic.SelectMany(f => f.Value).ToArray()) { Unique = true };
        AddIndex(index);

    }

    public TableIndex AddUniqueIndex(ITable table, IColumn[] columns)
    {
        var index = new TableIndex(table, columns) { Unique = true };
        AddIndex(index);
        return index;
    }

    public TableIndex AddIndex(ITable table, Field[] fields)
    {
        var index = new TableIndex(table, fields.SelectMany(f => TableIndex.GetColumnsFromField(f)).ToArray());
        AddIndex(index);
        return index;
    }

    public TableIndex AddIndex(ITable table, IColumn[] columns)
    {
        var index = new TableIndex(table, columns);
        AddIndex(index);
        return index;
    }

    public void AddIndex(TableIndex index)
    {
        ITable table = index.Table;

        if (table.AdditionalIndexes == null)
            table.AdditionalIndexes = new List<TableIndex>();

        table.AdditionalIndexes.Add(index);
    }

    public FluentInclude<T> Include<T>() where T : Entity
    {
        var table = Include(typeof(T), null);
        return new FluentInclude<T>(table, this);
    }

    public virtual Table Include(Type type)
    {
        return Include(type, null);
    }

    internal protected virtual Table Include(Type type, PropertyRoute? route)
    {
        if (schema.Tables.TryGetValue(type, out var result))
            return result;

        if (this.Schema.IsCompleted) //Below for nop includes of Views referencing lites or entities
            throw new InvalidOperationException("Schema already completed");

        using (HeavyProfiler.LogNoStackTrace("Include", () => type.TypeName()))
        {
            if (type.IsAbstract)
                throw new InvalidOperationException(ErrorIncluding(route) + $"Impossible to include in the Schema the type {type} because is abstract");

            if (!Reflector.IsEntity(type))
                throw new InvalidOperationException(ErrorIncluding(route) + $"Impossible to include in the Schema the type {type} because is not and Entity");

            string cleanName = schema.Settings.desambiguatedNames?.TryGetC(type) ?? Reflector.CleanTypeName(EnumEntity.Extract(type) ?? type);

            if (schema.NameToType.ContainsKey(cleanName))
                throw new InvalidOperationException(ErrorIncluding(route) + @$"Two types have the same cleanName '{cleanName}', desambiguate using Schema.Current.Settings.Desambiguate method:
{schema.NameToType[cleanName].FullName}
{type.FullName}");

            try
            {
                result = new Table(type);

                schema.Tables.Add(type, result);
                schema.NameToType[cleanName] = type;
                schema.TypeToName[type] = cleanName;

                Complete(result);

                return result;
            }
            catch (Exception) //Avoid half-cooked tables
            {
                schema.Tables.Remove(type);
                schema.NameToType.Remove(cleanName);
                schema.TypeToName.Remove(type);
                throw;
            }
        }
    }

    private static string? ErrorIncluding(PropertyRoute? route)
    {
        return route?.Let(r => $"Error including {r}: ");
    }

    void Complete(Table table)
    {
        using (HeavyProfiler.LogNoStackTrace("Complete", () => table.Type.Name))
        using (var tr = HeavyProfiler.LogNoStackTrace("GetPrimaryKeyAttribute", () => table.Type.Name))
        {
            Type type = table.Type;

            table.IdentityBehaviour = GetPrimaryKeyAttribute(type).IdentityBehaviour;
            tr.Switch("GenerateTableName");
            table.Name = GenerateTableName(type, Settings.TypeAttribute<TableNameAttribute>(type));
            tr.Switch("GenerateCleanTypeName");
            table.CleanTypeName = GenerateCleanTypeName(type);
            tr.Switch("GenerateFields");
            table.Fields = GenerateFields(PropertyRoute.Root(type), table, NameSequence.GetVoid(IsPostgres), forceNull: false, inMList: false);
            tr.Switch("GenerateMixins");
            table.Mixins = GenerateMixins(PropertyRoute.Root(type), table, NameSequence.GetVoid(IsPostgres));
            tr.Switch("GenerateTemporal");
            table.SystemVersioned = ToSystemVersionedInfo(Settings.TypeAttribute<SystemVersionedAttribute>(type), table.Name);
            tr.Switch("PartitionScheme");
            table.PartitionScheme = ToPartitionScheme(Settings.TypeAttribute<PartitionColumnAttribute>(type));
            table.GenerateColumns();
        }
    }

    private SqlPartitionScheme? ToPartitionScheme(PartitionColumnAttribute? att)
    {
        if (att == null)
            return null;

        if (att.SchemeName == null)
            return schema.PartitionSchemes.SingleEx(); 
        
        return schema.PartitionSchemes.SingleEx(a => a.Name == att.SchemeName);
    }

    public SystemVersionedInfo? ToSystemVersionedInfo(SystemVersionedAttribute? att, ObjectName tableName)
    {
        if (att == null)
            return null;

        var isPostgres = this.schema.Settings.IsPostgres;

        var tn = att.TemporalTableName != null ? ObjectName.Parse(FixNameLength(att.TemporalTableName), isPostgres) :
                new ObjectName(tableName.Schema, FixNameLength(tableName.Name + "_" + Idiomatic("History")), isPostgres);

        if (isPostgres)
            return new SystemVersionedInfo(tn, att.PostgresSysPeriodColumname);

        return new SystemVersionedInfo(tn, att.StartDateColumnName, att.EndDateColumnName);
    }

    private Dictionary<Type, FieldMixin>? GenerateMixins(PropertyRoute propertyRoute, ITable table, NameSequence nameSequence)
    {
        Dictionary<Type, FieldMixin>? mixins = null;
        foreach (var t in MixinDeclarations.GetMixinDeclarations(propertyRoute.Type))
        {
            if (mixins == null)
                mixins = new Dictionary<Type, FieldMixin>();

            mixins.Add(t, this.GenerateFieldMixin(propertyRoute.Add(t), nameSequence, table));
        }

        return mixins;
    }

    public HeavyProfiler.Tracer? Tracer { get; set; }


    public HashSet<(Type type, string method)> LoadedModules = new HashSet<(Type type, string method)>();
    public bool AlreadyDefined(MethodBase? methodBase)
    {
        this.Tracer.Switch(methodBase!.DeclaringType!.Name);

        return !LoadedModules.Add((type: methodBase.DeclaringType, method: methodBase.Name));
    }

    public void AssertDefined(MethodBase methodBase)
    {
        var tulpe = (methodBase.DeclaringType!, methodBase.Name);

        if (!LoadedModules.Contains(tulpe))
            throw new ApplicationException("Call {0} first".FormatWith(tulpe));
    }

    #region Field Generator


    protected Dictionary<string, EntityField> GenerateFields(PropertyRoute root, ITable table, NameSequence preName, bool forceNull, bool inMList)
    {
        using (HeavyProfiler.LogNoStackTrace("SB.GenerateFields", () => root.ToString()))
        {
            Dictionary<string, EntityField> result = new Dictionary<string, EntityField>();
            var type = root.Type;

            if (type.IsEntity())
            {
                {
                    PropertyRoute route = root.Add(fiId);

                    Field field = GenerateField(table, route, preName, forceNull, inMList);

                    result.Add(fiId.Name, new EntityField(type, fiId, field));
                }

                TicksColumnAttribute? t = Settings.TypeAttribute<TicksColumnAttribute>(type);
                if (t == null || t.HasTicks)
                {
                    PropertyRoute route = root.Add(fiTicks);

                    Field field = GenerateField(table, route, preName, forceNull, inMList);

                    result.Add(fiTicks.Name, new EntityField(type, fiTicks, field));
                }

                Expression? exp = ExpressionCleaner.GetFieldExpansion(type, EntityExpression.ToStringMethod);
                if (exp == null)
                {
                    PropertyRoute route = root.Add(fiToStr);

                    Field field = GenerateField(table, route, preName, forceNull, inMList);

                    if (result.ContainsKey(fiToStr.Name))
                        throw new InvalidOperationException("Duplicated field with name {0} on {1}, shadowing not supported".FormatWith(fiToStr.Name, type.TypeName()));

                    result.Add(fiToStr.Name, new EntityField(type, fiToStr, field));
                }

                PartitionColumnAttribute? a = Settings.TypeAttribute<PartitionColumnAttribute>(type);
                if (a != null)
                {
                    PropertyRoute route = root.Add(fiPartitionId);

                    Field field = GenerateField(table, route, preName, forceNull, inMList);

                    result.Add(fiPartitionId.Name, new EntityField(type, fiPartitionId, field));

                }
            }

            foreach (FieldInfo fi in Reflector.InstanceFieldsInOrder(type))
            {
                PropertyRoute route = root.Add(fi);

                if (Settings.FieldAttribute<IgnoreAttribute>(route) == null)
                {
                    if (route.PropertyInfo == null && !fi.IsPublic && !fi.HasAttribute<FieldWithoutPropertyAttribute>())
                        throw new InvalidOperationException("Field '{0}' of type '{1}' has no property".FormatWith(fi.Name, type.Name));

                    if (route.PropertyInfo != null && !fi.IsPublic)
                    {
                        if (route.PropertyInfo.GetMethod == null)
                            throw new InvalidOperationException($"Property '{route.PropertyInfo.Name}' in {route.PropertyInfo.DeclaringType!.TypeName()} has no 'get' method");

                        if (route.PropertyInfo.SetMethod == null)
                            throw new InvalidOperationException($"Property '{route.PropertyInfo.Name}' in {route.PropertyInfo.DeclaringType!.TypeName()} has no 'set' method, use 'private set;' instead");
                    }


                    Field field = GenerateField(table, route, preName, forceNull, inMList);

                    if (result.ContainsKey(fi.Name))
                        throw new InvalidOperationException("Duplicated field with name '{0}' on '{1}', shadowing not supported".FormatWith(fi.Name, type.TypeName()));


                    var ef = new EntityField(type, fi, field);

                    if (field is FieldMList fml)
                        fml.TableMList.PropertyRoute = route;

                    result.Add(fi.Name, ef);
                }
            }

            return result;
        }
    }

    static readonly FieldInfo fiToStr = ReflectionTools.GetFieldInfo((Entity o) => o.ToStr);
    static readonly FieldInfo fiTicks = ReflectionTools.GetFieldInfo((Entity o) => o.ticks);
    static readonly FieldInfo fiId = ReflectionTools.GetFieldInfo((Entity o) => o.id);
    static readonly FieldInfo fiPartitionId = ReflectionTools.GetFieldInfo((Entity o) => o.partitionId);

    protected virtual Field GenerateField(ITable table, PropertyRoute route, NameSequence preName, bool forceNull, bool inMList)
    {
        using (HeavyProfiler.LogNoStackTrace("GenerateField", () => route.ToString()))
        {
            KindOfField kof = GetKindOfField(route);

            if (kof == KindOfField.MList && inMList)
                throw new InvalidOperationException("Field {0} of type {1} can not be nested in another MList".FormatWith(route, route.Type.TypeName(), kof));

            //field name generation
            NameSequence name;
            ColumnNameAttribute? vc = Settings.FieldAttribute<ColumnNameAttribute>(route);
            if (vc != null && vc.Name.HasText())
                name = NameSequence.GetVoid(IsPostgres).Add(vc.Name);
            else if (route.PropertyRouteType != PropertyRouteType.MListItems)
                name = preName.Add(GenerateFieldName(route, kof));
            else if (kof == KindOfField.Enum || kof == KindOfField.Reference)
                name = preName.Add(GenerateMListFieldName(route, kof));
            else
                name = preName;

            switch (kof)
            {
                case KindOfField.PrimaryKey:
                    return GenerateFieldPrimaryKey((Table)table, route, name);
                case KindOfField.Ticks:
                    return GenerateFieldTicks((Table)table, route, name);
                case KindOfField.PartitionId:
                    return GenerateFieldPartition((Table)table, route, name);
                case KindOfField.ToStr:
                    return GenerateFieldToString((Table)table, route, name);
                case KindOfField.Value:
                    return GenerateFieldValue(table, route, name, forceNull);
                case KindOfField.Reference:
                    {
                        Implementations at = Settings.GetImplementations(route);
                        if (at.IsByAll)
                            return GenerateFieldImplementedByAll(route, table, name, forceNull);
                        else if (at.Types.Only() == route.Type.CleanType())
                            return GenerateFieldReference(table, route, name, forceNull);
                        else
                            return GenerateFieldImplementedBy(table, route, name, forceNull, at.Types);
                    }
                case KindOfField.Enum:
                    return GenerateFieldEnum(table, route, name, forceNull);
                case KindOfField.Embedded:
                    return GenerateFieldEmbedded(table, route, name, forceNull, inMList);
                case KindOfField.MList:
                    return GenerateFieldMList((Table)table, route, name);
                default:
                    throw new NotSupportedException(EngineMessage.NoWayOfMappingType0Found.NiceToString().FormatWith(route.Type));
            }
        }
    }

    public enum KindOfField
    {
        PrimaryKey,
        Ticks,
        PartitionId,
        ToStr,
        Value,
        Reference,
        Enum,
        Embedded,
        MList,
    }

    protected virtual KindOfField GetKindOfField(PropertyRoute route)
    {
        if (route.FieldInfo != null && ReflectionTools.FieldEquals(route.FieldInfo, fiId))
            return KindOfField.PrimaryKey;

        if (route.FieldInfo != null && ReflectionTools.FieldEquals(route.FieldInfo, fiTicks))
            return KindOfField.Ticks;

        if (route.FieldInfo != null && ReflectionTools.FieldEquals(route.FieldInfo, fiPartitionId))
            return KindOfField.PartitionId;

        if (route.FieldInfo != null && ReflectionTools.FieldEquals(route.FieldInfo, fiToStr))
            return KindOfField.ToStr;

        if (Settings.TryGetSqlDbType(Settings.FieldAttribute<DbTypeAttribute>(route), route.Type) != null)
            return KindOfField.Value;

        if (route.Type.UnNullify().IsEnum)
            return KindOfField.Enum;

        if (Reflector.IsIEntity(Lite.Extract(route.Type) ?? route.Type))
            return KindOfField.Reference;

        if (Reflector.IsEmbeddedEntity(route.Type))
            return KindOfField.Embedded;

        if (Reflector.IsMList(route.Type))
            return KindOfField.MList;

        if (Settings.IsPostgres && route.Type.IsArray)
        {
            if (Settings.TryGetSqlDbType(Settings.FieldAttribute<DbTypeAttribute>(route), route.Type.ElementType()!) != null)
                return KindOfField.Value;
        }

        throw new InvalidOperationException($"Field {route} of type {route.Type.Name} has no database representation");
    }

    protected virtual Field GenerateFieldPrimaryKey(Table table, PropertyRoute route, NameSequence name)
    {
        var attr = GetPrimaryKeyAttribute(table.Type);

        PrimaryKey.PrimaryKeyType.Add(table.Type, attr.Type);

        DbTypePair pair = Settings.GetSqlDbType(attr, attr.Type);

        return table.PrimaryKey = new FieldPrimaryKey(route, table, FixNameLength(attr.Name ?? Idiomatic("ID")), attr.Type)
        {
            DbType = pair.DbType,
            Collation = Settings.GetCollation(attr),
            UserDefinedTypeName = pair.UserDefinedTypeName,
            Default = attr.GetDefault(Settings.IsPostgres),
            Check = attr.GetCheck(Settings.IsPostgres),
            Identity = attr.Identity,
            Size = attr.HasSize ? attr.Size : (int?)null,
        };
    }

    private PrimaryKeyAttribute GetPrimaryKeyAttribute(Type type)
    {
        var attr = Settings.TypeAttribute<PrimaryKeyAttribute>(type);

        if (attr != null)
            return attr;

        if (type.IsEnumEntity())
            return new PrimaryKeyAttribute(Enum.GetUnderlyingType(type.GetGenericArguments().Single())) { Identity = false, IdentityBehaviour = false };

        return Settings.DefaultPrimaryKeyAttribute;
    }

    protected virtual FieldValue GenerateFieldTicks(Table table, PropertyRoute route, NameSequence name)
    {
        var ticksAttr = Settings.TypeAttribute<TicksColumnAttribute>(table.Type);

        if (ticksAttr != null && !ticksAttr.HasTicks)
            throw new InvalidOperationException("HastTicks is false");

        Type type = ticksAttr?.Type ?? route.Type;

        DbTypePair pair = Settings.GetSqlDbType(ticksAttr, type);

        string ticksName = ticksAttr?.Name ?? name.ToString();

        return table.Ticks = new FieldTicks(route, type, FixNameLength(ticksName))
        {
            DbType = pair.DbType,
            Collation = Settings.GetCollation(ticksAttr),
            UserDefinedTypeName = pair.UserDefinedTypeName,
            Nullable = IsNullable.No,
            Size = Settings.GetSqlSize(ticksAttr, null, pair.DbType),
            Precision = Settings.GetSqlPrecision(ticksAttr, null, pair.DbType),
            Scale = Settings.GetSqlScale(ticksAttr, null, pair.DbType),
            Default = ticksAttr?.GetDefault(Settings.IsPostgres),
            Check = ticksAttr?.GetCheck(Settings.IsPostgres),
        };
    }

    protected virtual FieldValue GenerateFieldPartition(Table table, PropertyRoute route, NameSequence name)
    {
        var partitionAttr = Settings.TypeAttribute<PartitionColumnAttribute>(table.Type);

        if (partitionAttr == null)
            throw new InvalidOperationException("PartitionColumnAttribute is null");

        Type type = partitionAttr?.Type ?? route.Type;

        DbTypePair pair = Settings.GetSqlDbType(partitionAttr, type);

        string partitionName = partitionAttr?.Name ?? name.ToString();

        return table.PartitionId = new FieldPartitionId(route, type, FixNameLength(partitionName))
        {
            DbType = pair.DbType,
            Collation = Settings.GetCollation(partitionAttr),
            UserDefinedTypeName = pair.UserDefinedTypeName,
            Nullable = IsNullable.No,
            Size = Settings.GetSqlSize(partitionAttr, null, pair.DbType),
            Precision = Settings.GetSqlPrecision(partitionAttr, null, pair.DbType),
            Scale = Settings.GetSqlScale(partitionAttr, null, pair.DbType),
            Default = partitionAttr?.GetDefault(Settings.IsPostgres),
            Check = partitionAttr?.GetCheck(Settings.IsPostgres),
        };
    }

    protected virtual FieldValue GenerateFieldToString(Table table, PropertyRoute route, NameSequence name)
    {
        var toStrAttribute = Settings.TypeAttribute<ToStringColumnAttribute>(table.Type);

        Type type = toStrAttribute?.Type ?? route.Type;

        DbTypePair pair = Settings.GetSqlDbType(toStrAttribute, type);

        string toStrName =
            toStrAttribute?.Name == null ? name.ToString() :
            toStrAttribute.AvoidIdiomatic ? toStrAttribute.Name :
            Idiomatic(toStrAttribute.Name);

        return new FieldValue(route, type, FixNameLength(toStrName))
        {
            DbType = pair.DbType,
            Collation = Settings.GetCollation(toStrAttribute),
            UserDefinedTypeName = pair.UserDefinedTypeName,
            Nullable = toStrAttribute?.Nullable == false ? IsNullable.No : IsNullable.Yes,
            Size = Settings.GetSqlSize(toStrAttribute, null, pair.DbType),
            Precision = Settings.GetSqlPrecision(toStrAttribute, null, pair.DbType),
            Scale = Settings.GetSqlScale(toStrAttribute, null, pair.DbType),
            Default = toStrAttribute?.GetDefault(Settings.IsPostgres),
            Check = toStrAttribute?.GetCheck(Settings.IsPostgres),
        };
    }

    protected virtual FieldValue GenerateFieldValue(ITable table, PropertyRoute route, NameSequence name, bool forceNull)
    {
        var att = Settings.FieldAttribute<DbTypeAttribute>(route);

        DbTypePair pair = Settings.IsPostgres && route.Type.IsArray && route.Type != typeof(byte[]) ?
           Settings.GetSqlDbType(att, route.Type.ElementType()!) :
           Settings.GetSqlDbType(att, route.Type);

        return new FieldValue(route, null, FixNameLength(name.ToString()))
        {
            DbType = pair.DbType,
            Collation = Settings.GetCollation(att),
            UserDefinedTypeName = pair.UserDefinedTypeName,
            Nullable = Settings.GetIsNullable(route, forceNull),
            Size = Settings.GetSqlSize(att, route, pair.DbType),
            Precision = Settings.GetSqlPrecision(att, route, pair.DbType),
            Scale = Settings.GetSqlScale(att, route, pair.DbType),
            Default = att?.GetDefault(Settings.IsPostgres),
            Check = att?.GetCheck(Settings.IsPostgres),
            DateTimeKind = att?.DateTimeKind ??
            (route.Type.UnNullify() != typeof(DateTime) ? DateTimeKind.Unspecified :
             this.Schema.TimeZoneMode == TimeZoneMode.Utc ? DateTimeKind.Utc : DateTimeKind.Local),
        }.Do(f =>
        {
            f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route));
            f.Index = f.GenerateIndex(table, Settings.FieldAttribute<IndexAttribute>(route));
        });
    }

    protected virtual FieldEnum GenerateFieldEnum(ITable table, PropertyRoute route, NameSequence name, bool forceNull)
    {
        var att = Settings.FieldAttribute<DbTypeAttribute>(route);

        Type cleanEnum = route.Type.UnNullify();

        var referenceTable = Include(EnumEntity.Generate(cleanEnum), route);

        return new FieldEnum(route, FixNameLength(name.ToString()), referenceTable)
        {
            Nullable = Settings.GetIsNullable(route, forceNull),
            IsLite = false,
            AvoidForeignKey = Settings.FieldAttribute<AvoidForeignKeyAttribute>(route) != null,
            Default = att?.GetDefault(Settings.IsPostgres),
            Check = att?.GetCheck(Settings.IsPostgres),
        }.Do(f =>
        {
            f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route));
            f.Index = f.GenerateIndex(table, Settings.FieldAttribute<IndexAttribute>(route));
        });
    }

    protected virtual FieldReference GenerateFieldReference(ITable table, PropertyRoute route, NameSequence name, bool forceNull)
    {
        var entityType = Lite.Extract(route.Type) ?? route.Type;


        var referenceTable = Include(entityType, route);

        var nullable = Settings.GetIsNullable(route, forceNull);

        var isLite = route.Type.IsLite();

        var attr = Settings.FieldAttribute<DbTypeAttribute>(route);

        return new FieldReference(route, null, FixNameLength(name.ToString()), referenceTable)
        {
            Nullable = nullable,
            IsLite = isLite,
            CustomLiteModelType = !isLite ? null : Settings.FieldAttributes(route)?.OfType<LiteModelAttribute>().SingleOrDefaultEx()?.LiteModelType,
            AvoidForeignKey = Settings.FieldAttribute<AvoidForeignKeyAttribute>(route) != null,
            AvoidExpandOnRetrieving = Settings.FieldAttribute<AvoidExpandQueryAttribute>(route) != null,
            Default = attr?.GetDefault(Settings.IsPostgres),
            Check = attr?.GetCheck(Settings.IsPostgres)
        }.Do(f =>
        {
            f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route));
            f.Index = f.GenerateIndex(table, Settings.FieldAttribute<IndexAttribute>(route));
        });
    }

    protected virtual FieldImplementedBy GenerateFieldImplementedBy(ITable table, PropertyRoute route, NameSequence name, bool forceNull, IEnumerable<Type> types)
    {
        Type cleanType = Lite.Extract(route.Type) ?? route.Type;
        string errors = types.Where(t => !cleanType.IsAssignableFrom(t)).ToString(t => t.TypeName(), ", ");
        if (errors.Length != 0)
            throw new InvalidOperationException("Type {0} do not implement {1}".FormatWith(errors, cleanType));

        var nullable = Settings.GetIsNullable(route, forceNull);

        if (types.Count() > 1 && nullable == IsNullable.No)
            nullable = IsNullable.Forced;

        CombineStrategy strategy = Settings.FieldAttribute<CombineStrategyAttribute>(route)?.Strategy ?? CombineStrategy.Case;

        bool avoidForeignKey = Settings.FieldAttribute<AvoidForeignKeyAttribute>(route) != null;

        var isLite = route.Type.IsLite();

        var implementations = types.ToDictionary(t => t, t =>
        {
            var rt = Include(t, route);

            string impName = FixNameLength(name.Add(Idiomatic(TypeLogic.GetCleanName(t))).ToString());
            return new ImplementationColumn(impName, referenceTable: rt)
            {
                CustomLiteModelType = !isLite ? null : Settings.FieldAttributes(route)?.OfType<LiteModelAttribute>().SingleOrDefaultEx(a => a.ForEntityType == t)?.LiteModelType,
                Nullable = nullable,
                AvoidForeignKey = avoidForeignKey,
            };
        });

        return new FieldImplementedBy(route, implementations)
        {
            IsLite = isLite,
            SplitStrategy = strategy,
            AvoidExpandOnRetrieving = Settings.FieldAttribute<AvoidExpandQueryAttribute>(route) != null
        }.Do(f =>
        {
            foreach (var impColumn in f.ImplementationColumns.Values)
            {
                impColumn.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route), impColumn);
            }
            f.Index = f.GenerateIndex(table, Settings.FieldAttribute<IndexAttribute>(route));
        });
    }

    int? maxNameLength;
    string FixNameLength(string name) => StringHashEncoder.ChopHash(name, (maxNameLength ??= Connector.Current.MaxNameLength), this.IsPostgres);

    public string Idiomatic(string name) => IsPostgres ? name.PascalToSnake() : name;

    protected virtual FieldImplementedByAll GenerateFieldImplementedByAll(PropertyRoute route, ITable table, NameSequence preName, bool forceNull)
    {
        var nullable = Settings.GetIsNullable(route, forceNull);

        var primaryKeyTypes = Settings.ImplementedByAllPrimaryKeyTypes;

        if(primaryKeyTypes.Count() > 1 && nullable == IsNullable.No)
            nullable = IsNullable.Forced;

        var columns = Settings.ImplementedByAllPrimaryKeyTypes.Select(t => new ImplementedByAllIdColumn(FixNameLength(preName.Add(Idiomatic(t.Name)).ToString()), nullable.ToBool() ? t.Nullify() : t, Settings.DefaultSqlType(t))
        {
            Nullable = nullable,
            Size = t == typeof(string) ? Settings.ImplementedByAllStringSize : null,
        });

        var columnType = new ImplementationColumn(FixNameLength(preName.Add(Idiomatic("Type")).ToString()), Include(typeof(TypeEntity), route))
        {
            Nullable = nullable,
            AvoidForeignKey = Settings.FieldAttribute<AvoidForeignKeyAttribute>(route) != null,
        };

        return new FieldImplementedByAll(route, columns, columnType)
        {
            IsLite = route.Type.IsLite(),
            AvoidExpandOnRetrieving = Settings.FieldAttribute<AvoidExpandQueryAttribute>(route) != null
        }.Do(f =>
        {
            f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route));
            f.Index = f.GenerateIndex(table, Settings.FieldAttribute<IndexAttribute>(route));
        });
    }

    protected virtual FieldMList GenerateFieldMList(Table table, PropertyRoute route, NameSequence name)
    {
        Type elementType = route.Type.ElementType()!;

        if (table.Ticks == null)
            throw new InvalidOperationException("Type '{0}' has field '{1}' but does not Ticks. MList requires concurrency control.".FormatWith(route.Parent!.Type.TypeName(), route.FieldInfo!.FieldName()));

        var orderAttr = Settings.FieldAttribute<PreserveOrderAttribute>(route);

        FieldValue? order = null;
        if (orderAttr != null)
        {
            var pair = Settings.GetSqlDbTypePair(typeof(int));

            order = new FieldValue(route: null!, fieldType: typeof(int), FixNameLength(orderAttr.Name ?? Idiomatic("Order")))
            {
                DbType = pair.DbType,
                Collation = Settings.GetCollation(orderAttr),
                UserDefinedTypeName = pair.UserDefinedTypeName,
                Nullable = IsNullable.No,
                Size = Settings.GetSqlSize(orderAttr, null, pair.DbType),
                Precision = Settings.GetSqlPrecision(orderAttr, null, pair.DbType),
                Scale = Settings.GetSqlScale(orderAttr, null, pair.DbType),
            };
        }

        var keyAttr = Settings.FieldAttribute<PrimaryKeyAttribute>(route) ?? Settings.DefaultPrimaryKeyAttribute;
        TableMList.PrimaryKeyColumn primaryKey;
        {
            PrimaryKey.MListPrimaryKeyType.Add(route, keyAttr.Type);

            var pair = Settings.GetSqlDbType(keyAttr, keyAttr.Type);

            primaryKey = new TableMList.PrimaryKeyColumn(keyAttr.Type, FixNameLength(keyAttr.Name ?? Idiomatic("ID")))
            {
                DbType = pair.DbType,
                Collation = Settings.GetCollation(orderAttr),
                UserDefinedTypeName = pair.UserDefinedTypeName,
                Default = keyAttr.GetDefault(Settings.IsPostgres),
                Check = keyAttr.GetCheck(Settings.IsPostgres),
                Identity = keyAttr.Identity,
            };
        }

        var tableName = GenerateTableNameCollection(table, name, Settings.FieldAttribute<TableNameAttribute>(route));

        var backReference = new FieldReference(route: null!, fieldType: table.Type,
                name: GenerateBackReferenceName(table.Type, Settings.FieldAttribute<BackReferenceColumnNameAttribute>(route)),
                referenceTable: table
            )
        {
            AvoidForeignKey = Settings.FieldAttribute<AvoidForeignKeyAttribute>(route) != null,
        };

        TableMList mlistTable = new TableMList(route.Type, tableName, primaryKey, backReference)
        {
            Order = order,
        };

        mlistTable.Field = GenerateField(mlistTable, route.Add("Item"), NameSequence.GetVoid(IsPostgres), forceNull: false, inMList: true);

        var sysAttribute = Settings.FieldAttribute<SystemVersionedAttribute>(route) ??
            (Settings.TypeAttribute<SystemVersionedAttribute>(table.Type) != null ? new SystemVersionedAttribute() : null);
        mlistTable.SystemVersioned = ToSystemVersionedInfo(sysAttribute, mlistTable.Name);

        var partitionAttr = Settings.FieldAttribute<PartitionColumnAttribute>(route) ??
            (Settings.TypeAttribute<PartitionColumnAttribute>(table.Type) != null ? new PartitionColumnAttribute() : null);
        mlistTable.PartitionScheme = ToPartitionScheme(partitionAttr);
        if(mlistTable.PartitionScheme != null)
        {
            Type type = partitionAttr?.Type ?? typeof(int);

            DbTypePair pair = Settings.GetSqlDbType(partitionAttr, type);

            string columnName = partitionAttr?.Name ?? Idiomatic("PartitionId");

            mlistTable.PartitionId = new FieldPartitionId(null!, type, columnName)
            {
                DbType = pair.DbType,
                Collation = Settings.GetCollation(partitionAttr),
                UserDefinedTypeName = pair.UserDefinedTypeName,
                Nullable = IsNullable.No,
                Size = Settings.GetSqlSize(partitionAttr, null, pair.DbType),
                Precision = Settings.GetSqlPrecision(partitionAttr, null, pair.DbType),
                Scale = Settings.GetSqlScale(partitionAttr, null, pair.DbType),
                Default = partitionAttr?.GetDefault(Settings.IsPostgres),
                Check = partitionAttr?.GetCheck(Settings.IsPostgres),
            };
        }

        mlistTable.GenerateColumns();

        return new FieldMList(route, mlistTable)
        {
            TableMList = mlistTable,
        };
    }

    protected virtual FieldEmbedded GenerateFieldEmbedded(ITable table, PropertyRoute route, NameSequence name, bool forceNull, bool inMList)
    {
        var nullable = Settings.GetIsNullable(route, false);

        var hasValue = nullable.ToBool() ? new FieldEmbedded.EmbeddedHasValueColumn(FixNameLength(name.Add(Idiomatic("HasValue")).ToString())) : null;

        var embeddedFields = GenerateFields(route, table, name, forceNull: nullable.ToBool() || forceNull, inMList: inMList);
        var mixins = GenerateMixins(route, table, name);
        return new FieldEmbedded(route, hasValue, embeddedFields, mixins);
    }

    protected virtual FieldMixin GenerateFieldMixin(PropertyRoute route, NameSequence name, ITable table)
    {
        return new FieldMixin(route, table, GenerateFields(route, table, name, forceNull: false, inMList: false));
    }
    #endregion

    #region Names

    public virtual string GenerateCleanTypeName(Type type)
    {
        type = CleanType(type);

        var ctn = type.GetCustomAttribute<CleanTypeNameAttribute>();
        if (ctn != null)
            return ctn.Name;

        return Reflector.CleanTypeName(type);
    }

    protected static Type CleanType(Type type)
    {
        type = Lite.Extract(type) ?? type;
        type = EnumEntity.Extract(type) ?? type;
        return type;
    }

    public virtual ObjectName GenerateTableName(Type type, TableNameAttribute? tn)
    {
        var isPostgres = Schema.Settings.IsPostgres;

        SchemaName sn = tn != null ? ToSchemaName(tn) : GetSchemaName(type);

        string name = tn?.Name ?? 
            (isPostgres ? EnumEntity.Extract(type)?.Name.PascalToSnake() : EnumEntity.Extract(type)?.Name) ?? 
            (isPostgres ? Reflector.CleanTypeName(type).PascalToSnake() : Reflector.CleanTypeName(type));

        return new ObjectName(sn, name, isPostgres);
    }

    public virtual SchemaName GetSchemaName(Type type)
    {
        type = EnumEntity.Extract(type) ?? type;

        var isPostgres = this.Schema.Settings.IsPostgres;
        var assenbly = AssemblySchemaNameAttribute.OverridenAssembly.TryGetC(type) ?? type.Assembly!;
        var attributes = assenbly.GetCustomAttributes<AssemblySchemaNameAttribute>();

        var att = 
            attributes?.FirstOrDefault(a => a.ForNamespace == type.Namespace) ??
            attributes?.FirstOrDefault(a => a.ForNamespace == null);

        if (att != null)
            return new SchemaName(GetDatabase(type), att.AvoidIdiomatic ? att.SchemaName : Idiomatic(att.SchemaName), isPostgres);


        return SchemaName.Default(isPostgres);
    }

    public virtual DatabaseName? GetDatabase(Type type)
    {
        return null;
    }

    private SchemaName ToSchemaName(TableNameAttribute tn)
    {
        var isPostgres = Schema.Settings.IsPostgres;

        ServerName? server = tn.ServerName == null ? null : new ServerName(tn.ServerName, isPostgres);
        DatabaseName? dataBase = tn.DatabaseName == null && server == null ? null : new DatabaseName(server, tn.DatabaseName!, isPostgres);
        SchemaName schema = tn.SchemaName == null && dataBase == null ? (tn.Name.StartsWith("#") && isPostgres ? null! : SchemaName.Default(isPostgres)) : new SchemaName(dataBase, tn.SchemaName!, isPostgres);
        return schema;
    }

    public virtual ObjectName GenerateTableNameCollection(Table table, NameSequence name, TableNameAttribute? tn)
    {
        var isPostgres = Schema.Settings.IsPostgres;

        SchemaName sn = tn != null ? ToSchemaName(tn) : table.Name.Schema;

        return new ObjectName(sn, FixNameLength(tn?.Name ?? (table.Name.Name + "_" + name.ToString())), isPostgres);
    }

    public virtual string GenerateMListFieldName(PropertyRoute route, KindOfField kindOfField)
    {
        Type type = Lite.Extract(route.Type) ?? route.Type;

        switch (kindOfField)
        {
            case KindOfField.Value:
            case KindOfField.Embedded:
                return Idiomatic(type.Name);
            case KindOfField.Enum:
            case KindOfField.Reference:
                return Idiomatic((EnumEntity.Extract(type)?.Name ?? Reflector.CleanTypeName(type)) + "ID");
            default:
                throw new InvalidOperationException("No field name for type {0} defined".FormatWith(type));
        }
    }

    public virtual string GenerateFieldName(PropertyRoute route, KindOfField kindOfField)
    {
        string name = route.PropertyInfo != null ? (route.PropertyInfo.Name.TryAfterLast('.') ?? route.PropertyInfo.Name)
            : route.FieldInfo!.Name;

        switch (kindOfField)
        {
            case KindOfField.PrimaryKey:
            case KindOfField.Ticks:
            case KindOfField.Value:
            case KindOfField.Embedded:
            case KindOfField.MList:  //only used for table name
            case KindOfField.PartitionId:
            case KindOfField.ToStr:
                return Idiomatic(name);
            case KindOfField.Reference:
            case KindOfField.Enum:
                return Idiomatic(name + "ID");
            default:
                throw new InvalidOperationException("No name for {0} defined".FormatWith(route.FieldInfo!.Name));
        }
    }

    public virtual string GenerateBackReferenceName(Type type, BackReferenceColumnNameAttribute? attribute)
    {
        return attribute?.Name ?? Idiomatic("ParentID");
    }
    #endregion

    GlobalLazyManager GlobalLazyManager = new GlobalLazyManager();

    public void SwitchGlobalLazyManager(GlobalLazyManager manager)
    {
        GlobalLazyManager.AsserNotUsed();
        GlobalLazyManager = manager;
    }

    public ResetLazy<T> GlobalLazy<T>(Func<T> func, InvalidateWith invalidateWith,
        Action? onInvalidated = null,
        LazyThreadSafetyMode mode = LazyThreadSafetyMode.ExecutionAndPublication)
    {
        var result = Signum.Engine.GlobalLazy.WithoutInvalidations(() =>
        {
            GlobalLazyManager.OnLoad(this, invalidateWith);

            return func();
        }, mode);

        GlobalLazyManager.AttachInvalidations(this, invalidateWith, (sender, args) =>
        {
            result.Reset();
            onInvalidated?.Invoke();
        });

        return result;
    }


    public void WithPartition<T>(Func<T, int> calculatePartitionId)
        where T : Entity
    {
        if (Schema.Tables.ContainsKey(typeof(T)))
            throw new InvalidOperationException($"Type {typeof(T).Name} already included");

        Schema.Settings.TypeAttributes<T>().Add(new PartitionColumnAttribute());
        Schema.EntityEvents<T>().PreSaving += (e, ctx) => e.PartitionId = calculatePartitionId(e);
    }
}

public class GlobalLazyManager
{
    bool isUsed = false;

    public void AsserNotUsed()
    {
        if (isUsed)
            throw new InvalidOperationException("GlobalLazyManager has already been used");
    }

    public virtual void AttachInvalidations(SchemaBuilder sb, InvalidateWith invalidateWith, EventHandler invalidate)
    {
        isUsed = true;

        Action onInvalidation = () =>
        {
            if (Transaction.InTestTransaction)
            {
                invalidate(this, EventArgs.Empty);
                Transaction.Rolledback += dic => invalidate(this, EventArgs.Empty);
            }

            Transaction.PostRealCommit += dic => invalidate(this, EventArgs.Empty);
        };

        Schema schema = sb.Schema;

        foreach (var type in invalidateWith.Types)
        {
            giAttachInvalidations.GetInvoker(type)(schema, onInvalidation);
        }

        var dependants = DirectedGraph<Table>.Generate(invalidateWith.Types.Select(t => schema.Table(t)), t => t.DependentTables().Select(kvp => kvp.Key)).Select(t => t.Type).ToHashSet();
        dependants.ExceptWith(invalidateWith.Types);

        foreach (var type in dependants)
        {
            giAttachInvalidationsDependant.GetInvoker(type)(schema, onInvalidation);
        }
    }


    static readonly GenericInvoker<Action<Schema, Action>> giAttachInvalidationsDependant = new((s, a) => AttachInvalidationsDependant<Entity>(s, a));
    static void AttachInvalidationsDependant<T>(Schema s, Action action) where T : Entity
    {
        var ee = s.EntityEvents<T>();

        ee.Saving += e =>
        {
            if (!e.IsNew && e.IsGraphModified)
                action();
        };
        ee.PreUnsafeUpdate += (u, q) => { action(); return null; };
    }

    static readonly GenericInvoker<Action<Schema, Action>> giAttachInvalidations = new((s, a) => AttachInvalidations<Entity>(s, a));
    static void AttachInvalidations<T>(Schema s, Action action) where T : Entity
    {
        var ee = s.EntityEvents<T>();

        ee.Saving += e =>
        {
            if (e.IsGraphModified)
                action();
        };
        ee.PreUnsafeUpdate += (u, eq) => { action(); return null; };
        ee.PreUnsafeDelete += (q) => { action(); return null; };
        ee.PreUnsafeInsert += (query, constructor, entityQuery) => { action(); return constructor; };
        ee.PreBulkInsert += (isMList) => { action(); };
    }

    public virtual void OnLoad(SchemaBuilder sb, InvalidateWith invalidateWith)
    {
    }
}


public class ViewBuilder : SchemaBuilder
{
    public ViewBuilder(Schema schema)
        : base(schema)
    {
    }

    public override Table Include(Type type)
    {
        return Schema.Table(type);
    }

    public Table NewView(Type type)
    {
        Table table = new Table(type)
        {
            Name = GenerateTableName(type, Settings.TypeAttribute<TableNameAttribute>(type)),
            IsView = true
        };

        table.Fields = GenerateFields(PropertyRoute.Root(type), table, NameSequence.GetVoid(IsPostgres), forceNull: false, inMList: false);

        table.GenerateColumns();

        return table;
    }

    public Table NewFullTextResultTable(ITable forTable)
    {
        var type = typeof(FullTextResultTable);
        Table table = new Table(type)
        {
            Name = GenerateTableName(type, null),
            IsView = true
        };

        var fiKey = ReflectionTools.GetFieldInfo((FullTextResultTable ft) => ft.Key);
        var fiRank = ReflectionTools.GetFieldInfo((FullTextResultTable ft) => ft.Rank);

        table.Fields = new Dictionary<string, EntityField>
        {
            { fiKey.Name, new EntityField(type, fiKey, new FieldPrimaryKey(PropertyRoute.Root(type).Add(fiKey), table, "KEY", forTable.PrimaryKey.Type)) },
            { fiRank.Name, new EntityField(type, fiRank, new FieldValue(PropertyRoute.Root(type).Add(fiRank), typeof(int), "RANK")) }
        };

        table.GenerateColumns();

        return table;
    }


    public override ObjectName GenerateTableName(Type type, TableNameAttribute? tn)
    {
        var name = base.GenerateTableName(type, tn);

        return Administrator.ReplaceViewName(name);
    }


    protected override FieldReference GenerateFieldReference(ITable table, PropertyRoute route, NameSequence name, bool forceNull)
    {
        var result = base.GenerateFieldReference(table, route, name, forceNull);

        if (Settings.FieldAttribute<ViewPrimaryKeyAttribute>(route) != null)
            result.PrimaryKey = true;

        return result;
    }

    protected override FieldValue GenerateFieldValue(ITable table, PropertyRoute route, NameSequence name, bool forceNull)
    {
        var result = base.GenerateFieldValue(table, route, name, forceNull);

        if (Settings.FieldAttribute<ViewPrimaryKeyAttribute>(route) != null)
            result.PrimaryKey = true;

        return result;
    }

    protected override FieldEnum GenerateFieldEnum(ITable table, PropertyRoute route, NameSequence name, bool forceNull)
    {
        var att = Settings.FieldAttribute<DbTypeAttribute>(route);

        Type cleanEnum = route.Type.UnNullify();

        //var referenceTable = Include(EnumEntity.Generate(cleanEnum), route);

        return new FieldEnum(route, name.ToString(), null! /*referenceTable*/)
        {
            Nullable = Settings.GetIsNullable(route, forceNull),
            IsLite = false,
            AvoidForeignKey = Settings.FieldAttribute<AvoidForeignKeyAttribute>(route) != null,
            Default = att?.GetDefault(Settings.IsPostgres),
            Check = att?.GetCheck(Settings.IsPostgres),
        }.Do(f => 
        { 
            f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route));
            f.Index = f.GenerateIndex(table, Settings.FieldAttribute<IndexAttribute>(route));
        });
    }
}

