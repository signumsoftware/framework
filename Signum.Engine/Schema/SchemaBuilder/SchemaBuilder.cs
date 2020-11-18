using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Linq;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Utilities.DataStructures;
using System.Threading;

namespace Signum.Engine.Maps
{
    public class SchemaBuilder
    {
        Schema schema;
        public SchemaSettings Settings
        {
            get { return schema.Settings; }
        }

        public SchemaBuilder(bool isDefault)
        {
            schema = new Schema(new SchemaSettings());

            if (isDefault)
            {
                if (TypeEntity.AlreadySet)
                    throw new InvalidOperationException("Only one default SchemaBuilder per application allowed");

                TypeEntity.SetTypeNameCallbacks(
                    t => schema.TypeToName.GetOrThrow(t, "Type {0} not found in the schema"),
                    cleanName => schema.NameToType.TryGetC(cleanName));

                FromEnumMethodExpander.miQuery = ReflectionTools.GetMethodInfo(() => Database.Query<Entity>()).GetGenericMethodDefinition();
            }

            Settings.AssertNotIncluded = MixinDeclarations.AssertNotIncluded = t =>
            {
                if (schema.Tables.ContainsKey(t))
                    throw new InvalidOperationException("{0} is already included in the Schema".FormatWith(t.TypeName()));
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


        public UniqueTableIndex AddUniqueIndex<T>(Expression<Func<T, object?>> fields, Expression<Func<T, bool>>? where = null, Expression<Func<T, object?>>? includeFields = null) where T : Entity
        {
            var table = Schema.Table<T>();

            IColumn[] columns = IndexKeyColumns.Split(table, fields);

            var index = AddUniqueIndex(table, columns);

            if (where != null)
                index.Where = IndexWhereExpressionVisitor.GetIndexWhere(where, table);

            if (includeFields != null)
            {
                index.IncludeColumns = IndexKeyColumns.Split(table, includeFields);
                if (table.SystemVersioned != null)
                {
                    index.IncludeColumns = index.IncludeColumns.Concat(table.SystemVersioned.Columns()).ToArray();
                }
            }

            return index;
        }

        public TableIndex AddIndex<T>(Expression<Func<T, object?>> fields, 
            Expression<Func<T, bool>>? where = null, 
            Expression<Func<T, object>>? includeFields = null) where T : Entity
        {
            var table = Schema.Table<T>();

            IColumn[] columns = IndexKeyColumns.Split(table, fields);

            var index = new TableIndex(table, columns);

            if (where != null)
                index.Where = IndexWhereExpressionVisitor.GetIndexWhere(where, table);

            if (includeFields != null)
            {
                index.IncludeColumns = IndexKeyColumns.Split(table, includeFields);
                if (table.SystemVersioned != null)
                {
                    index.IncludeColumns = index.IncludeColumns.Concat(table.SystemVersioned.Columns()).ToArray();
                }
            }

            AddIndex(index);

            return index;
        }

        public UniqueTableIndex AddUniqueIndexMList<T, V>(Expression<Func<T, MList<V>>> toMList,
            Expression<Func<MListElement<T, V>, object>> fields,
            Expression<Func<MListElement<T, V>, bool>>? where = null,
            Expression<Func<MListElement<T, V>, object>>? includeFields = null)
            where T : Entity
        {
            TableMList table = ((FieldMList)Schema.FindField(Schema.Table(typeof(T)), Reflector.GetMemberList(toMList))).TableMList;

            IColumn[] columns = IndexKeyColumns.Split(table, fields);

            var index = AddUniqueIndex(table, columns);

            if (where != null)
                index.Where = IndexWhereExpressionVisitor.GetIndexWhere(where, table);

            if (includeFields != null)
            {
                index.IncludeColumns = IndexKeyColumns.Split(table, includeFields);
                if (table.SystemVersioned != null)
                {
                    index.IncludeColumns = index.IncludeColumns.Concat(table.SystemVersioned.Columns()).ToArray();
                }
            }




            return index;
        }

        public TableIndex AddIndexMList<T, V>(Expression<Func<T, MList<V>>> toMList,
            Expression<Func<MListElement<T, V>, object>> fields,
            Expression<Func<MListElement<T, V>, bool>>? where = null,
            Expression<Func<MListElement<T, V>, object>>? includeFields = null)
            where T : Entity
        {
            TableMList table = ((FieldMList)Schema.FindField(Schema.Table(typeof(T)), Reflector.GetMemberList(toMList))).TableMList;

            IColumn[] columns = IndexKeyColumns.Split(table, fields);

            var index = AddIndex(table, columns);

            if (where != null)
                index.Where = IndexWhereExpressionVisitor.GetIndexWhere(where, table);

            if (includeFields != null)
            {
                index.IncludeColumns = IndexKeyColumns.Split(table, includeFields);
                if (table.SystemVersioned != null)
                {
                    index.IncludeColumns = index.IncludeColumns.Concat(table.SystemVersioned.Columns()).ToArray();
                }
            }

            return index;
        }


        public UniqueTableIndex AddUniqueIndex(ITable table, Field[] fields)
        {
            var index = new UniqueTableIndex(table, TableIndex.GetColumnsFromFields(fields));
            AddIndex(index);
            return index;
        }

        public UniqueTableIndex AddUniqueIndex(ITable table, IColumn[] columns)
        {
            var index = new UniqueTableIndex(table, columns);
            AddIndex(index);
            return index;
        }

        public TableIndex AddIndex(ITable table, Field[] fields)
        {
            var index = new TableIndex(table, TableIndex.GetColumnsFromFields(fields));
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

            if (table.MultiColumnIndexes == null)
                table.MultiColumnIndexes = new List<TableIndex>();

            table.MultiColumnIndexes.Add(index);
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

                foreach (var t in type.Follow(a => a.BaseType))
                    if (!t.IsSerializable)
                        throw new InvalidOperationException(ErrorIncluding(route) + $"Type {t.TypeName()} is not marked as serializable");

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
                table.Fields = GenerateFields(PropertyRoute.Root(type), table, NameSequence.Void, forceNull: false, inMList: false);
                tr.Switch("GenerateMixins");
                table.Mixins = GenerateMixins(PropertyRoute.Root(type), table, NameSequence.Void);
                tr.Switch("GenerateTemporal");
                table.SystemVersioned = ToSystemVersionedInfo(Settings.TypeAttribute<SystemVersionedAttribute>(type), table.Name);
                tr.Switch("GenerateColumns");
                table.GenerateColumns();
            }
        }

        public SystemVersionedInfo? ToSystemVersionedInfo(SystemVersionedAttribute? att, ObjectName tableName)
        {
            if (att == null)
                return null;

            var isPostgres = this.schema.Settings.IsPostgres;

            var tn = att.TemporalTableName != null ? ObjectName.Parse(att.TemporalTableName, isPostgres) :
                    new ObjectName(tableName.Schema, tableName.Name + "_History", isPostgres);

            if (isPostgres)
                return new SystemVersionedInfo(tn, att.PostgreeSysPeriodColumname);

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
        public bool NotDefined(MethodBase? methodBase)
        {
            this.Tracer.Switch(methodBase!.DeclaringType!.Name);

            var methods = methodBase.DeclaringType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
             .Where(m => !m.HasAttribute<MethodExpanderAttribute>())
             .Select(m => m.GetCustomAttribute<ExpressionFieldAttribute>()?.Name)
             .NotNull()
             .ToHashSet();

            var fields = methodBase.DeclaringType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => f.Name.EndsWith("Expression") && f.FieldType.IsInstantiationOf(typeof(Expression<>)));

            foreach (var f in fields)
            {
                if (!methods.Contains(f.Name))
                    throw new InvalidOperationException($"No Method found for expression '{f.Name}' in '{methodBase.DeclaringType.Name}'");
            }

            return LoadedModules.Add((type: methodBase.DeclaringType, method: methodBase.Name));
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

                    TicksColumnAttribute? t = type.GetCustomAttribute<TicksColumnAttribute>();
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
                }

                foreach (FieldInfo fi in Reflector.InstanceFieldsInOrder(type))
                {
                    PropertyRoute route = root.Add(fi);

                    if (Settings.FieldAttribute<IgnoreAttribute>(route) == null)
                    {
                        if (Reflector.TryFindPropertyInfo(fi) == null && !fi.IsPublic && !fi.HasAttribute<FieldWithoutPropertyAttribute>())
                            throw new InvalidOperationException("Field '{0}' of type '{1}' has no property".FormatWith(fi.Name, type.Name));

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

        static readonly FieldInfo fiToStr = ReflectionTools.GetFieldInfo((Entity o) => o.toStr);
        static readonly FieldInfo fiTicks = ReflectionTools.GetFieldInfo((Entity o) => o.ticks);
        static readonly FieldInfo fiId = ReflectionTools.GetFieldInfo((Entity o) => o.id);

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
                    name = NameSequence.Void.Add(vc.Name);
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

            PrimaryKey.PrimaryKeyType.SetDefinition(table.Type, attr.Type);

            DbTypePair pair = Settings.GetSqlDbType(attr, attr.Type);

            return table.PrimaryKey = new FieldPrimaryKey(route, table, attr.Name, attr.Type)
            {
                DbType = pair.DbType,
                Collation = Settings.GetCollate(attr),
                UserDefinedTypeName = pair.UserDefinedTypeName,
                Default = attr.GetDefault(Settings.IsPostgres),
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

            return table.Ticks = new FieldTicks(route, type, ticksName)
            {
                DbType = pair.DbType,
                Collation = Settings.GetCollate(ticksAttr),
                UserDefinedTypeName = pair.UserDefinedTypeName,
                Nullable = IsNullable.No,
                Size = Settings.GetSqlSize(ticksAttr, null, pair.DbType),
                Scale = Settings.GetSqlScale(ticksAttr, null, pair.DbType),
                Default = ticksAttr?.GetDefault(Settings.IsPostgres),
            };
        }

        protected virtual FieldValue GenerateFieldValue(ITable table, PropertyRoute route, NameSequence name, bool forceNull)
        {
            var att = Settings.FieldAttribute<DbTypeAttribute>(route);

            DbTypePair pair = Settings.IsPostgres && route.Type.IsArray && route.Type != typeof(byte[]) ?
               Settings.GetSqlDbType(att, route.Type.ElementType()!) :
               Settings.GetSqlDbType(att, route.Type);

            return new FieldValue(route, null, name.ToString())
            {
                DbType = pair.DbType,
                Collation = Settings.GetCollate(att),
                UserDefinedTypeName = pair.UserDefinedTypeName,
                Nullable = Settings.GetIsNullable(route, forceNull),
                Size = Settings.GetSqlSize(att, route, pair.DbType),
                Scale = Settings.GetSqlScale(att, route, pair.DbType),
                Default = att?.GetDefault(Settings.IsPostgres),
            }.Do(f => f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route)));
        }

        protected virtual FieldEnum GenerateFieldEnum(ITable table, PropertyRoute route, NameSequence name, bool forceNull)
        {
            var att = Settings.FieldAttribute<DbTypeAttribute>(route);

            Type cleanEnum = route.Type.UnNullify();

            var referenceTable = Include(EnumEntity.Generate(cleanEnum), route);

            return new FieldEnum(route, name.ToString(), referenceTable)
            {
                Nullable = Settings.GetIsNullable(route, forceNull),
                IsLite = false,
                AvoidForeignKey = Settings.FieldAttribute<AvoidForeignKeyAttribute>(route) != null,
                Default = att?.GetDefault(Settings.IsPostgres),
            }.Do(f => f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route)));
        }

        protected virtual FieldReference GenerateFieldReference(ITable table, PropertyRoute route, NameSequence name, bool forceNull)
        {
            var referenceTable = Include(Lite.Extract(route.Type) ?? route.Type, route);

            var nullable = Settings.GetIsNullable(route, forceNull);

            return new FieldReference(route, null, name.ToString(), referenceTable)
            {
                Nullable = nullable,
                IsLite = route.Type.IsLite(),
                AvoidForeignKey = Settings.FieldAttribute<AvoidForeignKeyAttribute>(route) != null,
                AvoidExpandOnRetrieving = Settings.FieldAttribute<AvoidExpandQueryAttribute>(route) != null,
                Default = Settings.FieldAttribute<DbTypeAttribute>(route)?.GetDefault(Settings.IsPostgres)
            }.Do(f => f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route)));
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

            var implementations = types.ToDictionary<Type, Type, ImplementationColumn>(t => t, t =>
            {
                var rt = Include(t, route);

                string impName = name.Add(TypeLogic.GetCleanName(t)).ToString();
                return new ImplementationColumn(impName, referenceTable: rt)
                {
                    Nullable = nullable,
                    AvoidForeignKey = avoidForeignKey,
                };
            });

            return new FieldImplementedBy(route, implementations)
            {
                SplitStrategy = strategy,
                IsLite = route.Type.IsLite(),
                AvoidExpandOnRetrieving = Settings.FieldAttribute<AvoidExpandQueryAttribute>(route) != null
            }.Do(f => f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route)));
        }

        protected virtual FieldImplementedByAll GenerateFieldImplementedByAll(PropertyRoute route, ITable table, NameSequence preName, bool forceNull)
        {
            var nullable = Settings.GetIsNullable(route, forceNull);

            var column = new ImplementationStringColumn(preName.ToString())
            {
                Nullable = nullable,
                Size = Settings.DefaultImplementedBySize,
            };

            var columnType = new ImplementationColumn(preName.Add("Type").ToString(), Include(typeof(TypeEntity), route))
            {
                Nullable = nullable,
                AvoidForeignKey = Settings.FieldAttribute<AvoidForeignKeyAttribute>(route) != null,
            };

            return new FieldImplementedByAll(route, column, columnType)
            {
                IsLite = route.Type.IsLite(),
                AvoidExpandOnRetrieving = Settings.FieldAttribute<AvoidExpandQueryAttribute>(route) != null
            }.Do(f => f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route)));
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

                order = new FieldValue(route: null!, fieldType:  typeof(int), orderAttr.Name ?? "Order")
                {
                    DbType = pair.DbType,
                    Collation = Settings.GetCollate(orderAttr),
                    UserDefinedTypeName = pair.UserDefinedTypeName,
                    Nullable = IsNullable.No,
                    Size = Settings.GetSqlSize(orderAttr, null, pair.DbType),
                    Scale = Settings.GetSqlScale(orderAttr, null, pair.DbType),
                };
            }

            var keyAttr = Settings.FieldAttribute<PrimaryKeyAttribute>(route) ?? Settings.DefaultPrimaryKeyAttribute;
            TableMList.PrimaryKeyColumn primaryKey;
            {
                var pair = Settings.GetSqlDbType(keyAttr, keyAttr.Type);

                primaryKey = new TableMList.PrimaryKeyColumn(keyAttr.Type, keyAttr.Name)
                {
                    DbType = pair.DbType,
                    Collation = Settings.GetCollate(orderAttr),
                    UserDefinedTypeName = pair.UserDefinedTypeName,
                    Default = keyAttr.GetDefault(Settings.IsPostgres),
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

            mlistTable.Field = GenerateField(mlistTable, route.Add("Item"), NameSequence.Void, forceNull: false, inMList: true);
            
            var sysAttribute = Settings.FieldAttribute<SystemVersionedAttribute>(route) ??
                (Settings.TypeAttribute<SystemVersionedAttribute>(table.Type) != null ? new SystemVersionedAttribute() : null);

            mlistTable.SystemVersioned = ToSystemVersionedInfo(sysAttribute, mlistTable.Name);

            mlistTable.GenerateColumns();

            return new FieldMList(route, mlistTable)
            {
                TableMList = mlistTable,
            };
        }

        protected virtual FieldEmbedded GenerateFieldEmbedded(ITable table, PropertyRoute route, NameSequence name, bool forceNull, bool inMList)
        {
            var nullable = Settings.GetIsNullable(route, false);

            var hasValue = nullable.ToBool() ? new FieldEmbedded.EmbeddedHasValueColumn(name.Add("HasValue").ToString()) : null;

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

            SchemaName sn = tn != null ? GetSchemaName(tn) : SchemaName.Default(isPostgres);

            string name =  tn?.Name ?? EnumEntity.Extract(type)?.Name ?? Reflector.CleanTypeName(type);

            return new ObjectName(sn, name, isPostgres);
        }

        private SchemaName GetSchemaName(TableNameAttribute tn)
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

            SchemaName sn = tn != null ? GetSchemaName(tn) : SchemaName.Default(isPostgres);

            return new ObjectName(sn, tn?.Name ?? (table.Name.Name + name.ToString()), isPostgres);
        }

        public virtual string GenerateMListFieldName(PropertyRoute route, KindOfField kindOfField)
        {
            Type type = Lite.Extract(route.Type) ?? route.Type;

            switch (kindOfField)
            {
                case KindOfField.Value:
                case KindOfField.Embedded:
                    return type.Name;
                case KindOfField.Enum:
                case KindOfField.Reference:
                    return (EnumEntity.Extract(type)?.Name ?? Reflector.CleanTypeName(type)) + "ID";
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
                case KindOfField.MList:  //se usa solo para el nombre de la tabla
                    return name;
                case KindOfField.Reference:
                case KindOfField.Enum:
                    return name + "ID";
                default:
                    throw new InvalidOperationException("No name for {0} defined".FormatWith(route.FieldInfo!.Name));
            }
        }

        public virtual string GenerateBackReferenceName(Type type, BackReferenceColumnNameAttribute? attribute)
        {
            return attribute?.Name ?? "ParentID";
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
            LazyThreadSafetyMode mode = LazyThreadSafetyMode.ExecutionAndPublication) where T : class
        {
            var result = Signum.Engine.GlobalLazy.WithoutInvalidations(() =>
            {
                GlobalLazyManager.OnLoad(this, invalidateWith);

                return func();
            });

            GlobalLazyManager.AttachInvalidations(this, invalidateWith, (sender, args) =>
            {
                result.Reset();
                onInvalidated?.Invoke();
            });

            return result;
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


        static readonly GenericInvoker<Action<Schema, Action>> giAttachInvalidationsDependant = new GenericInvoker<Action<Schema, Action>>((s, a) => AttachInvalidationsDependant<Entity>(s, a));
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

        static readonly GenericInvoker<Action<Schema, Action>> giAttachInvalidations = new GenericInvoker<Action<Schema, Action>>((s, a) => AttachInvalidations<Entity>(s, a));
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

            table.Fields = GenerateFields(PropertyRoute.Root(type), table, NameSequence.Void, forceNull: false, inMList: false);

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
            }.Do(f => f.UniqueIndex = f.GenerateUniqueIndex(table, Settings.FieldAttribute<UniqueIndexAttribute>(route)));
        }
    }

}
