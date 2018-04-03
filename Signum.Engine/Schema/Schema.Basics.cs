using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using System.IO;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine;
using System.Linq.Expressions;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System.Collections;
using Signum.Utilities.DataStructures;
using System.Diagnostics;
using Signum.Engine.Linq;
using System.Data.SqlClient;
using Signum.Services;
using System.Globalization;
using System.Threading;

namespace Signum.Engine.Maps
{
    public interface IFieldFinder
    {
        Field GetField(MemberInfo value);
        Field TryGetField(MemberInfo value);
    }

    public interface ITable
    {
        ObjectName Name { get; }

        IColumn PrimaryKey { get; }

        Dictionary<string, IColumn> Columns { get; }

        List<Index> MultiColumnIndexes { get; set; }

        List<Index> GeneratAllIndexes();

        void GenerateColumns();

        SystemVersionedInfo SystemVersioned { get; }
    }

    public class SystemVersionedInfo
    {
        public ObjectName TableName;
        public string StartColumnName;
        public string EndColumnName;

        internal IEnumerable<IColumn> Columns()
        {
            return new[]
            {
                new Column(this.StartColumnName, ColumnType.Start),
                new Column(this.EndColumnName, ColumnType.End)
            };
        }

        public enum ColumnType
        {
            Start, 
            End,
        }

        public class Column : IColumn
        {
            public Column(string name, ColumnType systemVersionColumnType)
            {
                this.Name = name;
                this.SystemVersionColumnType = systemVersionColumnType;
            }

            public string Name { get; private set; }
            public ColumnType SystemVersionColumnType { get; private set; }

            public IsNullable Nullable => IsNullable.No;
            public SqlDbType SqlDbType => SqlDbType.DateTime2;
            public Type Type => typeof(DateTime);
            public string UserDefinedTypeName => null;
            public bool PrimaryKey => false;
            public bool IdentityBehaviour => false;
            public bool Identity => false;
            public string Default { get; set; }
            public int? Size => null;
            public int? Scale => null;
            public string Collation => null;
            public Table ReferenceTable => null;
            public bool AvoidForeignKey => false;
        }

    }

    interface ITablePrivate
    {
        ColumnExpression GetPrimaryOrder(Alias alias);
    }
      
    public partial class Table : IFieldFinder, ITable, ITablePrivate
    {
        public Type Type { get; private set; }

        public ObjectName Name { get; set; }

        public bool IdentityBehaviour { get; set; }
        public bool IsView { get; internal set; }
        public string CleanTypeName { get; set; }

        public SystemVersionedInfo SystemVersioned { get; set; }

        public Dictionary<string, EntityField> Fields { get; set; }
        public Dictionary<Type, FieldMixin> Mixins { get; set; }
        
        public Dictionary<string, IColumn> Columns { get; set; }
        

        public List<Index> MultiColumnIndexes { get; set; }

        public Table(Type type)
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

            var columns = Fields.Values.SelectMany(c => c.Field.Columns()).ToDictionaryEx(c => c.Name, errorSuffix);

            if (Mixins != null)
                columns.AddRange(Mixins.Values.SelectMany(m => m.Fields.Values).SelectMany(f => f.Field.Columns()).ToDictionaryEx(c => c.Name, errorSuffix), errorSuffix);

            if (this.SystemVersioned != null)
                columns.AddRange(this.SystemVersioned.Columns().ToDictionaryEx(a => a.Name), errorSuffix);

            Columns = columns;

            inserterDisableIdentity = new ResetLazy<InsertCacheDisableIdentity>(() => InsertCacheDisableIdentity.InitializeInsertDisableIdentity(this));
            inserterIdentity = new ResetLazy<InsertCacheIdentity>(() => InsertCacheIdentity.InitializeInsertIdentity(this));
            updater = new ResetLazy<UpdateCache>(() => UpdateCache.InitializeUpdate(this));
            saveCollections = new ResetLazy<CollectionsCache>(() => CollectionsCache.InitializeCollections(this));
        }

        public Field GetField(MemberInfo member)
        {
            if (member is MethodInfo mi)
            {
                if (mi.IsGenericMethod && mi.GetGenericMethodDefinition().Name == "Mixin")
                {
                    if (Mixins == null)
                        throw new InvalidOperationException("{0} has not mixins".FormatWith(this.Type.Name));

                    return Mixins.GetOrThrow(mi.GetGenericArguments().Single());
                }
            }

            FieldInfo fi = member as FieldInfo ?? Reflector.FindFieldInfo(Type, (PropertyInfo)member);

            if (fi == null)
                throw new InvalidOperationException("Field {0} not found on {1}".FormatWith(member.Name, Type));

            EntityField field = Fields.GetOrThrow(fi.Name, "Field {0} not found on schema");

            return field.Field;
        }

        public Field TryGetField(MemberInfo member)
        {
            if (member is MethodInfo mi)
            {
                if (mi.IsGenericMethod && mi.GetGenericMethodDefinition().Name == "Mixin")
                {
                    return Mixins?.TryGetC(mi.GetGenericArguments().Single());
                }

                return null;
            }

            if (member is Type)
            {
                return Mixins?.TryGetC((Type)member);
            }

            FieldInfo fi = member as FieldInfo ??  Reflector.TryFindFieldInfo(Type, (PropertyInfo)member);

            if (fi == null)
                return null;

            EntityField field = Fields.TryGetC(fi.Name);

            if (field == null)
                return null;

            return field.Field;
        }

        public List<Index> GeneratAllIndexes()
        {
            IEnumerable<EntityField> fields = Fields.Values.AsEnumerable();
            if (Mixins != null)
                fields = fields.Concat(Mixins.Values.SelectMany(m => m.Fields.Values));

            var result = fields.SelectMany(f => f.Field.GenerateIndexes(this)).ToList();

            if (MultiColumnIndexes != null)
                result.AddRange(MultiColumnIndexes);

            if (result.OfType<UniqueIndex>().Any())
            {
                var s = Schema.Current.Settings;
                List<IColumn> attachedFields = fields.Where(f => s.FieldAttributes(PropertyRoute.Root(this.Type).Add(f.FieldInfo)).OfType<AttachToUniqueIndexesAttribute>().Any())
                   .SelectMany(f => Index.GetColumnsFromFields(f.Field))
                   .ToList();

                if (attachedFields.Any())
                {
                    result = result.Select(ix =>
                    {
                        var ui = ix as UniqueIndex;
                        if (ui == null || ui.AvoidAttachToUniqueIndexes)
                            return ix;

                        return new UniqueIndex(ui.Table, ui.Columns.Concat(attachedFields).ToArray())
                        {
                            Where = ui.Where
                        };
                    }).ToList();
                }
            }

            if(this.SystemVersioned != null)
            {
                result.Add(new Index(this, this.SystemVersioned.Columns().PreAnd(this.PrimaryKey).ToArray()));
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
        
        public FieldTicks Ticks { get; internal set; }
        public FieldPrimaryKey PrimaryKey { get; internal set; }

        IColumn ITable.PrimaryKey
        {
            get { return PrimaryKey; }
        }

        internal IEnumerable<EntityField> AllFields()
        {
            return this.Fields.Values.Concat(
                this.Mixins == null ? Enumerable.Empty<EntityField>() :
                this.Mixins.Values.SelectMany(fm => fm.Fields.Values));
        }
    }

    public class EntityField
    {
        public Field Field { get; set; }
        public FieldInfo FieldInfo { get; private set; }

        Type type;
        Func<object, object> getter;
        public Func<object, object> Getter => getter ?? (getter = ReflectionTools.CreateGetterUntyped(type, FieldInfo));

        public EntityField(Type type, FieldInfo fi)
        {
            FieldInfo = fi;
            this.type = type;
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
        public UniqueIndex UniqueIndex { get; set; }

        public Field(PropertyRoute route, Type fieldType = null)
        {
            this.Route = route;
            this.FieldType = fieldType ?? route.Type;
        }


        public abstract IEnumerable<IColumn> Columns();

        public virtual IEnumerable<Index> GenerateIndexes(ITable table)
        {
            if (UniqueIndex == null)
                return Enumerable.Empty<Index>();

            return new[] { UniqueIndex };
        }

        public virtual UniqueIndex GenerateUniqueIndex(ITable table, UniqueIndexAttribute attribute)
        {
            if (attribute == null)
                return null;

            var result = new UniqueIndex(table, Index.GetColumnsFromFields(this)) 
            { 
                AvoidAttachToUniqueIndexes = attribute.AvoidAttachToUniqueIndexes 
            }; 

            if(attribute.AllowMultipleNulls)
                result.Where = IndexWhereExpressionVisitor.IsNull(this, false);

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
        SqlDbType SqlDbType { get; }
        Type Type { get; }
        string UserDefinedTypeName { get; }
        bool PrimaryKey { get; }
        bool IdentityBehaviour { get; }
        bool Identity { get; }
        string Default { get; }
        int? Size { get; }
        int? Scale { get; }
        string Collation { get; }
        Table ReferenceTable { get; }
        bool AvoidForeignKey { get; }
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

        public static string GetSqlDbTypeString(this IColumn column)
        {
            return column.SqlDbType.ToString().ToUpper(CultureInfo.InvariantCulture) + SqlBuilder.GetSizeScale(column.Size, column.Scale);
        }

        public static GeneratedAlwaysType GetGeneratedAlwaysType(this IColumn column)
        {
            if (column is SystemVersionedInfo.Column svc)
                return svc.SystemVersionColumnType == SystemVersionedInfo.ColumnType.Start ? GeneratedAlwaysType.AsRowStart : GeneratedAlwaysType.AsRowEnd;

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
        IsNullable IColumn.Nullable { get { return IsNullable.No; } }
        public SqlDbType SqlDbType { get; set; }
        public string UserDefinedTypeName { get; set; }
        bool IColumn.PrimaryKey { get { return true; } }
        public bool Identity { get; set; }
        bool IColumn.IdentityBehaviour { get { return table.IdentityBehaviour; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public string Collation { get; set; }
        Table IColumn.ReferenceTable { get { return null; } }
        public Type Type { get; set; }
        public bool AvoidForeignKey { get { return false; } }
        public string Default { get; set; }

        Table table;
        public FieldPrimaryKey(PropertyRoute route, Table table)
            : base(route)
        {
            this.table = table;
        }

        public override string ToString()
        {
            return "{0} PrimaryKey".FormatWith(Name);
        }

        public override IEnumerable<IColumn> Columns()
        {
            return new[] { this };
        }

        public override IEnumerable<Index> GenerateIndexes(ITable table)
        {
            if (this.UniqueIndex != null)
                throw new InvalidOperationException("Changing IndexType is not allowed for FieldPrimaryKey");

            return new[] { new PrimaryClusteredIndex(table) };
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
        public SqlDbType SqlDbType { get; set; }
        public string UserDefinedTypeName { get; set; }
        public bool PrimaryKey { get; set; }
        bool IColumn.Identity { get { return false; } }
        bool IColumn.IdentityBehaviour { get { return false; } }
        public int? Size { get; set; }
        public string Collation { get; set; }
        public int? Scale { get; set; }
        Table IColumn.ReferenceTable { get { return null; } }
        public bool AvoidForeignKey { get { return false; } }
        public string Default { get; set; }

        public FieldValue(PropertyRoute route, Type fieldType = null)
            : base(route, fieldType)
        {
        }

        public override string ToString()
        {
            return "{0} {1} ({2},{3},{4})".FormatWith(
                Name,
                SqlDbType,
                Nullable.ToBool() ? "Nullable" : "",
                Size,
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

        public virtual Type Type
        {
            get { return this.Nullable.ToBool() ? this.FieldType.Nullify() : this.FieldType; }
        }
    }

    public partial class FieldTicks : FieldValue
    {
        public new Type Type { get; set; }

        public FieldTicks(PropertyRoute route)
            : base(route)
        {
        }
    }

    public partial class FieldEmbedded : Field, IFieldFinder
    {
        public partial class EmbeddedHasValueColumn : IColumn
        {
            public string Name { get; set; }
            public IsNullable Nullable { get { return IsNullable.No; } } //even on neasted embeddeds
            public SqlDbType SqlDbType { get { return SqlDbType.Bit; } }
            string IColumn.UserDefinedTypeName { get { return null; } }
            bool IColumn.PrimaryKey { get { return false; } }
            bool IColumn.Identity { get { return false; } }
            bool IColumn.IdentityBehaviour { get { return false; } }
            int? IColumn.Size { get { return null; } }
            int? IColumn.Scale { get { return null; } }
            string IColumn.Collation { get { return null; } }
            public Table ReferenceTable { get { return null; } }
            Type IColumn.Type { get { return typeof(bool); } }
            public bool AvoidForeignKey { get { return false; } }
            public string Default { get; set; }

        }

        public EmbeddedHasValueColumn HasValue { get; set; }

        public Dictionary<string, EntityField> EmbeddedFields { get; set; }

        public Func<EmbeddedEntity> Constructor { get; private set; }

        public FieldEmbedded(PropertyRoute route)
            : base(route)
        {
        }

        public override string ToString()
        {
            return "Embebed\r\n{0}".FormatWith(EmbeddedFields.ToString(c => "{0} : {1}".FormatWith(c.Key, c.Value), "\r\n").Indent(2));
        }

        public Field GetField(MemberInfo member)
        {
            FieldInfo fi = member as FieldInfo ?? Reflector.FindFieldInfo(FieldType, (PropertyInfo)member);

            if (fi == null)
                throw new InvalidOperationException("Field {0} not found on {1}".FormatWith(member.Name, FieldType));

            EntityField field = EmbeddedFields.GetOrThrow(fi.Name, "Field {0} not found on schema");

            return field.Field;
        }

        public Field TryGetField(MemberInfo value)
        {
            FieldInfo fi = value as FieldInfo ?? Reflector.TryFindFieldInfo(FieldType, (PropertyInfo)value);

            if (fi == null)
                return null;

            EntityField field = EmbeddedFields.TryGetC(fi.Name);

            if (field == null)
                return null;

            return field.Field;
        }
     
        public override IEnumerable<IColumn> Columns()
        {
            var result = new List<IColumn>();

            if (HasValue != null)
                result.Add(HasValue);

            result.AddRange(EmbeddedFields.Values.SelectMany(c => c.Field.Columns()));

            return result;
        }

        public override IEnumerable<Index> GenerateIndexes(ITable table)
        {
            return this.EmbeddedFields.Values.SelectMany(f => f.Field.GenerateIndexes(table));
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
        }

        internal override IEnumerable<TableMList> TablesMList()
        {
            return EmbeddedFields.Values.SelectMany(e => e.Field.TablesMList()); 
        }
    }

    public partial class FieldMixin : Field, IFieldFinder
    {
        public Dictionary<string, EntityField> Fields { get; set; }

        public Table MainEntityTable;

        public FieldMixin(PropertyRoute route, Table mainEntityTable)
            : base(route)
        {
            this.MainEntityTable = mainEntityTable;
        }

        public override string ToString()
        {
            return "Mixin\r\n{0}".FormatWith(Fields.ToString(c => "{0} : {1}".FormatWith(c.Key, c.Value), "\r\n").Indent(2));
        }

        public Field GetField(MemberInfo member)
        {
            FieldInfo fi = member as FieldInfo ?? Reflector.FindFieldInfo(FieldType, (PropertyInfo)member);

            if (fi == null)
                throw new InvalidOperationException("Field {0} not found on {1}".FormatWith(member.Name, FieldType));

            EntityField field = Fields.GetOrThrow(fi.Name, "Field {0} not found on schema");

            return field.Field;
        }

        public Field TryGetField(MemberInfo value)
        {
            FieldInfo fi = value as FieldInfo ?? Reflector.TryFindFieldInfo(FieldType, (PropertyInfo)value);

            if (fi == null)
                return null;

            EntityField field = Fields.TryGetC(fi.Name);

            if (field == null)
                return null;

            return field.Field;
        }
     
        public override IEnumerable<IColumn> Columns()
        {
            var result = new List<IColumn>();
            result.AddRange(Fields.Values.SelectMany(c => c.Field.Columns()));

            return result;
        }

        public override IEnumerable<Index> GenerateIndexes(ITable table)
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
    }

    public partial class FieldReference : Field, IColumn, IFieldReference
    {
        public string Name { get; set; }
        public IsNullable Nullable { get; set; }
    
        public bool PrimaryKey { get; set; } //For View
        bool IColumn.Identity { get { return false; } }
        bool IColumn.IdentityBehaviour { get { return false; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get; set; }
        public SqlDbType SqlDbType { get { return ReferenceTable.PrimaryKey.SqlDbType; } }
        public string Collation { get { return ReferenceTable.PrimaryKey.Collation; } }
        public string UserDefinedTypeName { get { return ReferenceTable.PrimaryKey.UserDefinedTypeName; } }
        public virtual Type Type { get { return this.Nullable.ToBool() ? ReferenceTable.PrimaryKey.Type.Nullify() : ReferenceTable.PrimaryKey.Type; } }
        
        public bool AvoidForeignKey { get; set; }

        public bool IsLite { get; internal set; }
        public bool AvoidExpandOnRetrieving { get; set; }
        public string Default { get; set; }

        public FieldReference(PropertyRoute route, Type fieldType = null) : base(route, fieldType) { }

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
            yield return KVP.Create(ReferenceTable, new RelationInfo
            {
                 IsLite = IsLite,
                 IsCollection = false,
                 IsNullable = Nullable.ToBool()
            }); 
        }

        public override IEnumerable<Index> GenerateIndexes(ITable table)
        {
            if (UniqueIndex == null)
                return new[] { new Index(table, (IColumn)this) };

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

        public FieldEnum(PropertyRoute route) : base(route) { }
        
        public override string ToString()
        {
            return "{0} -> {1} {4} ({2})".FormatWith(
                Name,
                "-",
                IsLite ? "Lite" : "",
                Nullable.ToBool() ? "Nullable" : "");
        }

        internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
        {
            if (ReferenceTable == null)
                yield break;
            yield return KVP.Create(ReferenceTable, new RelationInfo
            {
                IsLite = IsLite,
                IsCollection = false,
                IsNullable = Nullable.ToBool(),
                IsEnum = true,
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

        public FieldImplementedBy(PropertyRoute route) : base(route) { }

        public override string ToString()
        {
            return "ImplementedBy\r\n{0}".FormatWith(ImplementationColumns.ToString(k => "{0} -> {1} ({2})".FormatWith(k.Value.Name, k.Value.ReferenceTable.Name, k.Key.Name), "\r\n").Indent(2));
        }

        public override IEnumerable<IColumn> Columns()
        {
            return ImplementationColumns.Values.Cast<IColumn>();
        }

        internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
        {
            return ImplementationColumns.Select(a => KVP.Create(a.Value.ReferenceTable, new RelationInfo
            {
                IsLite = IsLite,
                IsCollection = false,
                IsNullable = a.Value.Nullable.ToBool()
            }));
        }

        public override IEnumerable<Index> GenerateIndexes(ITable table)
        {
            return this.Columns().Select(c => new Index(table, c)).Concat(base.GenerateIndexes(table));
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

        public ImplementationStringColumn Column { get; set; }
        public ImplementationColumn ColumnType { get; set; }

        public FieldImplementedByAll(PropertyRoute route) : base(route) { }

        public override IEnumerable<IColumn> Columns()
        {
            return new IColumn[] { Column, ColumnType };
        }

        internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
        {
            yield return KVP.Create(ColumnType.ReferenceTable, new RelationInfo
            {
                 IsNullable = this.ColumnType.Nullable.ToBool(),
                IsLite = true,
                 IsImplementedByAll = true,
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

        public override IEnumerable<Index> GenerateIndexes(ITable table)
        {
            if (UniqueIndex == null)
                return new[] { new Index(table, (IColumn)this.Column, (IColumn)this.ColumnType) };

            return base.GenerateIndexes(table);
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
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        bool IColumn.IdentityBehaviour { get { return false; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get; set; }
        public SqlDbType SqlDbType { get { return ReferenceTable.PrimaryKey.SqlDbType; } }
        public string Collation { get { return ReferenceTable.PrimaryKey.Collation; } }
        public string UserDefinedTypeName { get { return ReferenceTable.PrimaryKey.UserDefinedTypeName; } }
        public Type Type { get { return this.Nullable.ToBool() ? ReferenceTable.PrimaryKey.Type.Nullify() : ReferenceTable.PrimaryKey.Type; } }
        public bool AvoidForeignKey { get; set; }
        public string Default { get; set; }
    }

    public partial class ImplementationStringColumn : IColumn
    {
        public string Name { get; set; }
        public IsNullable Nullable { get; set; }
        string IColumn.UserDefinedTypeName { get { return null; } }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        bool IColumn.IdentityBehaviour { get { return false; } }
        public int? Size { get; set; }
        int? IColumn.Scale { get { return null; } }
        public string Collation { get; set; }
        public Table ReferenceTable { get { return null; } }
        public SqlDbType SqlDbType { get { return SqlDbType.NVarChar; } }
        public Type Type { get { return typeof(string); } }
        public bool AvoidForeignKey { get { return false; } }
        public string Default { get; set; }
    }

    public partial class FieldMList : Field, IFieldFinder
    {
        public TableMList TableMList { get; set; }

        public FieldMList(PropertyRoute route) : base(route) { }

        public override string ToString()
        {
            return "Coleccion\r\n{0}".FormatWith(TableMList.ToString().Indent(2));
        }

        public Field GetField(MemberInfo member)
        {
            if (member.Name == "Item")
                return TableMList.Field;

            throw new InvalidOperationException("{0} not supported by MList field".FormatWith(member.Name));
        }

        public Field TryGetField(MemberInfo member)
        {
            if (member.Name == "Item")
                return TableMList.Field;

            return null;
        }

        public override IEnumerable<IColumn> Columns()
        {
            return new IColumn[0];
        }

        public override IEnumerable<Index> GenerateIndexes(ITable table)
        {
            if (UniqueIndex != null)
                throw new InvalidOperationException("Changing IndexType is not allowed for FieldMList");

            return Enumerable.Empty<Index>();
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
            IsNullable IColumn.Nullable { get { return IsNullable.No; } }
            public SqlDbType SqlDbType { get; set; }
            public string Collation { get; set; }
            public string UserDefinedTypeName { get; set; }
            bool IColumn.PrimaryKey { get { return true; } }
            public bool Identity { get; set; }
            bool IColumn.IdentityBehaviour { get { return true; } }
            int? IColumn.Size { get { return null; } }
            int? IColumn.Scale { get { return null; } }
            Table IColumn.ReferenceTable { get { return null; } }
            public Type Type { get; set; }
            public bool AvoidForeignKey { get { return false; } }
            public string Default { get; set; }
        }

        public Dictionary<string, IColumn> Columns { get; set; }
        public List<Index> MultiColumnIndexes { get; set; }

        public ObjectName Name { get; set; }
        public PrimaryKeyColumn PrimaryKey { get; set; }
        public FieldReference BackReference { get; set; }
        public FieldValue Order { get; set; }
        public Field Field { get; set; }

        public SystemVersionedInfo SystemVersioned { get; set; }

        public Type CollectionType { get; private set; }
        public Func<IList> Constructor { get; private set; }

        public PropertyRoute PropertyRoute { get; internal set; }
        Func<Entity, IMListPrivate> getter;
        public Func<Entity, IMListPrivate> Getter => getter ?? (getter = PropertyRoute.GetLambdaExpression<Entity, IMListPrivate>(true).Compile());

        public TableMList(Type collectionType)
        {
            this.CollectionType = collectionType;
            this.cache = new Lazy<IMListCache>(() => (IMListCache)giCreateCache.GetInvoker(this.Field.FieldType)(this));
        }

        public override string ToString()
        {
            return "[{0}]\r\n  {1}\r\n  {2}".FormatWith(Name, BackReference.Name, Field.ToString());
        }

        public void GenerateColumns()
        {
            List<IColumn> cols = new List<IColumn> { PrimaryKey, BackReference }; 

            if(Order != null)
                cols.Add(Order); 

            cols.AddRange(Field.Columns());

            if (this.SystemVersioned != null)
                cols.AddRange(this.SystemVersioned.Columns());

            Columns = cols.ToDictionary(a => a.Name);
        }

        public List<Index> GeneratAllIndexes()
        {
            var result = BackReference.GenerateIndexes(this).ToList();

            result.AddRange(Field.GenerateIndexes(this));

            if (MultiColumnIndexes != null)
                result.AddRange(MultiColumnIndexes);

            return result;
        }

        public Field GetField(MemberInfo member)
        {
            Field result = TryGetField(member); 

            if(result  == null)
                throw new InvalidOperationException("'{0}' not found".FormatWith(member.Name));

            return result;
        }

        public Field TryGetField(MemberInfo mi)
        {
            if (mi.Name == "Parent")
                return this.BackReference;

            if (mi.Name == "Element")
                return this.Field;

            return null;
        }

        public void ToDatabase(DatabaseName databaseName)
        {
            this.Name = this.Name.OnDatabase(databaseName);
        }

        public void ToSchema(SchemaName schemaName)
        {
            this.Name = this.Name.OnSchema(schemaName);
        }


        IColumn ITable.PrimaryKey
        {
            get { return PrimaryKey; }
        }

        internal object[] BulkInsertDataRow(Entity entity, object value, int order)
        {
            return this.cache.Value.BulkInsertDataRow(entity, value, order);
        }

        public IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
        {
            return this.Field.GetTables();
        }
    }
}
