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

            var columns = Fields.Values.SelectMany(c => c.Field.Columns()).ToDictionary(c => c.Name, errorSuffix);

            if (Mixins != null)
                columns.AddRange(Mixins.Values.SelectMany(m => m.Fields.Values).SelectMany(f => f.Field.Columns()).ToDictionary(c => c.Name, errorSuffix), errorSuffix);

            SetFullMListGetter();

            Columns = columns;

            inserterDisableIdentity = new ResetLazy<InsertCacheDisableIdentity>(() => InsertCacheDisableIdentity.InitializeInsertDisableIdentity(this));
            inserterIdentity = new ResetLazy<InsertCacheIdentity>(() => InsertCacheIdentity.InitializeInsertIdentity(this));
            updater = new ResetLazy<UpdateCache>(() => UpdateCache.InitializeUpdate(this));
            saveCollections = new ResetLazy<CollectionsCache>(() => CollectionsCache.InitializeCollections(this));
        }

        public Field GetField(MemberInfo member)
        {
            if (member is MethodInfo)
            {
                var mi = (MethodInfo)member;

                if (mi.IsGenericMethod && mi.GetGenericMethodDefinition().Name == "Mixin")
                {
                    if(Mixins == null)
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
            if (member is MethodInfo)
            {
                var mi = (MethodInfo)member;

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
                List<IColumn> attachedFields = fields.Where(f => f.FieldInfo.GetCustomAttribute<AttachToUniqueIndexesAttribute>() != null)
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

        public void SetFullMListGetter()
        {
            var root = PropertyRoute.Root(this.Type);

            foreach (var field in this.Fields.Values)
            {
                field.Field.SetFullMListGetter(root.Add(field.FieldInfo), field.Getter);
            }

            if (this.Mixins != null)
            {
                foreach (var kvp in this.Mixins)
                {
                    kvp.Value.SetFullMListGetter(root.Add(kvp.Key), kvp.Value.Getter);
                }
            }
        }

        /// <summary>
        /// Use this method also to change the Server
        /// </summary>
        public void ToDatabase(DatabaseName databaseName)
        {
            this.Name = this.Name.OnDatabase(databaseName);

            foreach (var item in TablesMList())
                item.ToDatabase(databaseName);
        }

        public void ToSchema(SchemaName schemaName)
        {
            this.Name = this.Name.OnSchema(schemaName);

            foreach (var item in TablesMList())
                item.ToSchema(schemaName);
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
        public Func<object, object> Getter { get; private set; }
        //public Action<object, object> Setter { get; private set; }

        public EntityField(Type type, FieldInfo fi)
        {
            FieldInfo = fi;
            Getter = ReflectionTools.CreateGetterUntyped(type, fi);
            //Setter = ReflectionTools.CreateSetterUntyped(type, fi);
        }

        public override string ToString()
        {
            return FieldInfo.FieldName();
        }
    }

    public abstract partial class Field
    {
        public Type FieldType { get; private set; }
        public UniqueIndex UniqueIndex { get; set; }

        public Field(Type fieldType)
        {
            FieldType = fieldType;
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
        internal abstract void SetFullMListGetter(PropertyRoute route, Func<Entity, object> getter); 
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
    }

    public partial interface IColumn
    {
        string Name { get; }
        bool Nullable { get; }
        SqlDbType SqlDbType { get; }
        Type Type { get; }
        string UserDefinedTypeName { get; }
        bool PrimaryKey { get; }
        bool IdentityBehaviour { get; }
        bool Identity { get; }
        string Default { get; set; }
        int? Size { get; }
        int? Scale { get; }
        Table ReferenceTable { get; }
        bool AvoidForeignKey { get; }
    }

    public static partial class ColumnExtensions
    {
        public static string GetSqlDbTypeString(this IColumn column)
        {
            return column.SqlDbType.ToString().ToUpper(CultureInfo.InvariantCulture) + SqlBuilder.GetSizeScale(column.Size, column.Scale);
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
        bool IColumn.Nullable { get { return false; } }
        public SqlDbType SqlDbType { get; set; }
        public string UserDefinedTypeName { get; set; }
        bool IColumn.PrimaryKey { get { return true; } }
        public bool Identity { get; set; }
        bool IColumn.IdentityBehaviour { get { return table.IdentityBehaviour; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        Table IColumn.ReferenceTable { get { return null; } }
        public Type Type { get; set; }
        public bool AvoidForeignKey { get { return false; } }
        public string Default { get; set; }

        Table table;
        public FieldPrimaryKey(Type fieldType, Table table)
            : base(fieldType)
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

            return Enumerable.Empty<Index>();
        }

        internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
        {
            return Enumerable.Empty<KeyValuePair<Table, RelationInfo>>();
        }

        internal override IEnumerable<TableMList> TablesMList()
        {
            return Enumerable.Empty<TableMList>();
        }

        internal override void SetFullMListGetter(PropertyRoute route, Func<Entity, object> getter)
        {
        }
    }

    public partial class FieldValue : Field, IColumn
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        public SqlDbType SqlDbType { get; set; }
        public string UserDefinedTypeName { get; set; }
        public bool PrimaryKey { get; set; }
        bool IColumn.Identity { get { return false; } }
        bool IColumn.IdentityBehaviour { get { return false; } }
        public int? Size { get; set; }
        public int? Scale { get; set; }
        Table IColumn.ReferenceTable { get { return null; } }
        public bool AvoidForeignKey { get { return false; } }
        public string Default { get; set; }

        public FieldValue(Type fieldType)
            : base(fieldType)
        {
        }

        public override string ToString()
        {
            return "{0} {1} ({2},{3},{4})".FormatWith(
                Name,
                SqlDbType,
                Nullable ? "Nullable" : "",
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

        internal override void SetFullMListGetter(PropertyRoute route, Func<Entity, object> getter)
        {
            
        }

        public virtual Type Type
        {
            get { return this.Nullable ? this.FieldType.Nullify() : this.FieldType; }
        }
    }

    public partial class FieldTicks : FieldValue
    {
        public new Type Type { get; set; }

        public FieldTicks(Type fieldType)
            : base(fieldType)
        {
        }
    }

    public partial class FieldEmbedded : Field, IFieldFinder
    {
        public partial class EmbeddedHasValueColumn : IColumn
        {
            public string Name { get; set; }
            public bool Nullable { get { return false; } } //even on neasted embeddeds
            public SqlDbType SqlDbType { get { return SqlDbType.Bit; } }
            string IColumn.UserDefinedTypeName { get { return null; } }
            bool IColumn.PrimaryKey { get { return false; } }
            bool IColumn.Identity { get { return false; } }
            bool IColumn.IdentityBehaviour { get { return false; } }
            int? IColumn.Size { get { return null; } }
            int? IColumn.Scale { get { return null; } }
            public Table ReferenceTable { get { return null; } }
            Type IColumn.Type { get { return typeof(bool); } }
            public bool AvoidForeignKey { get { return false; } }
            public string Default { get; set; }
        }

        public EmbeddedHasValueColumn HasValue { get; set; }

        public Dictionary<string, EntityField> EmbeddedFields { get; set; }

        public Func<EmbeddedEntity> Constructor { get; private set; }

        public FieldEmbedded(Type fieldType)
            : base(fieldType)
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

        internal override void SetFullMListGetter(PropertyRoute route, Func<Entity, object> getter)
        {
            foreach (var item in EmbeddedFields.Values)
            {
                item.Field.SetFullMListGetter(route.Add(item.FieldInfo), entity =>
                {
                    var embedded = getter(entity);


                    if (embedded == null)
                        return null;

                    return item.Getter(embedded);
                }); 
            }

        }
    }

    public partial class FieldMixin : Field, IFieldFinder
    {
        public Dictionary<string, EntityField> Fields { get; set; }

        public Table MainEntityTable;

        public FieldMixin(Type fieldType, Table mainEntityTable)
            : base(fieldType)
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

        internal override void SetFullMListGetter(PropertyRoute route, Func<Entity, object> getter)
        {
            foreach (var field in Fields.Values)
            {
                field.Field.SetFullMListGetter(route.Add(field.FieldInfo), entity => field.Getter(getter(entity)));
            }
        }

        internal MixinEntity Getter(Entity ident)
        {
            return ((Entity)ident).GetMixin(FieldType);
        }
    }

    public partial class FieldReference : Field, IColumn, IFieldReference
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
    
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        bool IColumn.IdentityBehaviour { get { return false; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get; set; }
        public SqlDbType SqlDbType { get { return ReferenceTable.PrimaryKey.SqlDbType; } }
        public string UserDefinedTypeName { get { return ReferenceTable.PrimaryKey.UserDefinedTypeName; } }
        public Type Type { get { return this.Nullable ? ReferenceTable.PrimaryKey.Type.Nullify() : ReferenceTable.PrimaryKey.Type; } }
        
        public bool AvoidForeignKey { get; set; }

        public bool IsLite { get; internal set; }
        public bool AvoidExpandOnRetrieving { get; set; }
        public string Default { get; set; }

        public FieldReference(Type fieldType) : base(fieldType) { }

        public override string ToString()
        {
            return "{0} -> {1} {3} ({2})".FormatWith(
                Name,
                ReferenceTable.Name,
                IsLite ? "Lite" : "",
                Nullable ? "Nullable" : "");
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
                 IsNullable = Nullable
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

        internal override void SetFullMListGetter(PropertyRoute route, Func<Entity, object> getter)
        {
        }
    }

    public partial class FieldEnum : FieldReference
    {
        public FieldEnum(Type fieldType) : base(fieldType) { }

        public override string ToString()
        {
            return "{0} -> {1} {4} ({2})".FormatWith(
                Name,
                "-",
                IsLite ? "Lite" : "",
                Nullable ? "Nullable" : "");
        }

        internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
        {
            if (ReferenceTable == null)
                yield break;
            yield return KVP.Create(ReferenceTable, new RelationInfo
            {
                IsLite = IsLite,
                IsCollection = false,
                IsNullable = Nullable,
                IsEnum = true,
            });
        }

        internal override IEnumerable<TableMList> TablesMList()
        {
            return Enumerable.Empty<TableMList>();
        }

        internal override void SetFullMListGetter(PropertyRoute route, Func<Entity, object> getter)
        {
        }
    }

    public partial class FieldImplementedBy : Field, IFieldReference
    {
        public bool IsLite { get; internal set; }
        public CombineStrategy SplitStrategy { get; internal set; }
        public bool AvoidExpandOnRetrieving { get; internal set; }

        public Dictionary<Type, ImplementationColumn> ImplementationColumns { get; set; }

        public FieldImplementedBy(Type fieldType) : base(fieldType) { }

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
                IsNullable = a.Value.Nullable
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

        internal override void SetFullMListGetter(PropertyRoute route, Func<Entity, object> getter)
        {
        }
    }

    public partial class FieldImplementedByAll : Field, IFieldReference
    {
        public bool IsLite { get; internal set; }

        public bool AvoidExpandOnRetrieving { get; internal set; }

        public ImplementationStringColumn Column { get; set; }
        public ImplementationColumn ColumnType { get; set; }

        public FieldImplementedByAll(Type fieldType) : base(fieldType) { }

        public override IEnumerable<IColumn> Columns()
        {
            return new IColumn[] { Column, ColumnType };
        }

        internal override IEnumerable<KeyValuePair<Table, RelationInfo>> GetTables()
        {
            yield return KVP.Create(ColumnType.ReferenceTable, new RelationInfo
            {
                 IsNullable = this.ColumnType.Nullable,
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

        internal override void SetFullMListGetter(PropertyRoute route, Func<Entity, object> getter)
        {
        }
    }

    public partial class ImplementationColumn : IColumn
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        bool IColumn.IdentityBehaviour { get { return false; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get; set; }
        public SqlDbType SqlDbType { get { return ReferenceTable.PrimaryKey.SqlDbType; } }
        public string UserDefinedTypeName { get { return ReferenceTable.PrimaryKey.UserDefinedTypeName; } }
        public Type Type { get { return this.Nullable ? ReferenceTable.PrimaryKey.Type.Nullify() : ReferenceTable.PrimaryKey.Type; } }
        public bool AvoidForeignKey { get; set; }
        public string Default { get; set; }
    }

    public partial class ImplementationStringColumn : IColumn
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        string IColumn.UserDefinedTypeName { get { return null; } }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        bool IColumn.IdentityBehaviour { get { return false; } }
        public int? Size { get; set; }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get { return null; } }
        public SqlDbType SqlDbType { get { return SqlDbType.NVarChar; } }
        public Type Type { get { return typeof(string); } }
        public bool AvoidForeignKey { get { return false; } }
        public string Default { get; set; }
    }

    public partial class FieldMList : Field, IFieldFinder
    {
        public TableMList TableMList { get; set; }

        public FieldMList(Type fieldType) : base(fieldType) { }

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

        internal override void SetFullMListGetter(PropertyRoute route, Func<Entity, object> getter)
        {
            TableMList.Route = route;
            TableMList.FullGetter = getter;
        }
    }

    public partial class TableMList : ITable, IFieldFinder, ITablePrivate
    {
        public class PrimaryKeyColumn : IColumn
        {
            public string Name { get; set; }
            bool IColumn.Nullable { get { return false; } }
            public SqlDbType SqlDbType { get; set; }
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

        public Type CollectionType { get; private set; }
        public Func<IList> Constructor { get; private set; }

        public Func<Entity, object> FullGetter { get; internal set; }
        public PropertyRoute Route { get; internal set; }

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
