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
using Signum.Engine.Properties;
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
        string Name { get; }
        Dictionary<string, IColumn> Columns { get; }

        List<UniqueIndex> MultiIndexes { get; set; }

        List<UniqueIndex> GeneratUniqueIndexes();

        void GenerateColumns();
    }

    interface ITablePrivate
    {
        ColumnExpression GetIdExpression(Alias alias);
    }
      

    public partial class Table : IFieldFinder, ITable, ITablePrivate
    {
        public Type Type { get; private set; }

        public string Name { get; set; }
        bool identity = true;
        public bool Identity
        {
            get { return identity; }
            set
            {
                if (identity != value)
                {
                    identity = value;
                    if (inserter != null && inserter.IsValueCreated) // not too fast
                        inserter.Reset();
                }
            }
        }
        public bool IsView { get; internal set; }
        public string CleanTypeName { get; set; }

        public Dictionary<string, EntityField> Fields { get; set; }
        public Dictionary<string, IColumn> Columns { get; set; }

        public List<UniqueIndex> MultiIndexes { get; set; }

        public Table(Type type)
        {
            this.Type = type;
        }

        public override string ToString()
        {
            return Name;
        }

        public void GenerateColumns()
        {
            Columns = Fields.Values.SelectMany(c => c.Field.Columns()).ToDictionary(c => c.Name);

            inserter = new ResetLazy<InsertCache>(InitializeInsert);
            updater = new ResetLazy<UpdateCache>(InitializeUpdate);
            saveCollections = new ResetLazy<CollectionsCache>(InitializeCollections);
        }

        public Field GetField(MemberInfo member)
        {
            FieldInfo fi = member as FieldInfo ?? Reflector.FindFieldInfo(Type, (PropertyInfo)member);

            if (fi == null)
                throw new InvalidOperationException("Field {0} not found on {1}".Formato(member.Name, Type));

            EntityField field = Fields.GetOrThrow(fi.Name, "Field {0} not found on schema");

            return field.Field;
        }

        public Field TryGetField(MemberInfo member)
        {
            FieldInfo fi = member as FieldInfo ??  Reflector.TryFindFieldInfo(Type, (PropertyInfo)member);

            if (fi == null)
                return null;

            EntityField field = Fields.TryGetC(fi.Name);

            if (field == null)
                return null;

            return field.Field;
        }

        public List<UniqueIndex> GeneratUniqueIndexes()
        {
            var result = Fields.SelectMany(f => f.Value.Field.GeneratUniqueIndexes(this)).ToList();

            if (MultiIndexes != null)
                result.AddRange(MultiIndexes);

            return result;
        }

        internal void InsertMany(List<IdentifiableEntity> list, DirectedGraph<IdentifiableEntity> backEdges)
        {
            var ic = inserter.Value;

            if (!Connector.Current.AllowsMultipleQueries)
            {
                foreach (var item in list)
                    ic.Insert(item, backEdges);
            }
            else
            {
                foreach (var ls in list.Split_1_2_4_8_16())
                {
                    switch (ls.Count)
                    {
                        case 1: ic.Insert(ls[0], backEdges); break;
                        case 2: ic.Insert2(ls, backEdges); break;
                        case 4: ic.Insert4(ls, backEdges); break;
                        case 8: ic.Insert8(ls, backEdges); break;
                        case 16: ic.Insert16(ls, backEdges); break;
                    }
                }
            }
        }

        internal void UpdateMany(List<IdentifiableEntity> list, DirectedGraph<IdentifiableEntity> backEdges)
        {
            var uc = updater.Value;

            if (!Connector.Current.AllowsMultipleQueries)
            {
                foreach (var item in list)
                    uc.Update(item, backEdges);
            }
            else
            {
                foreach (var ls in list.Split_1_2_4_8_16())
                {
                    switch (ls.Count)
                    {
                        case 1: uc.Update(ls[0], backEdges); break;
                        case 2: uc.Update2(ls, backEdges); break;
                        case 4: uc.Update4(ls, backEdges); break;
                        case 8: uc.Update8(ls, backEdges); break;
                        case 16: uc.Update16(ls, backEdges); break;
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<Table, bool>> DependentTables()
        {
            return Fields.Values.SelectMany(f => f.Field.GetTables());
        }

        public IEnumerable<RelationalTable> RelationalTables()
        {
            return Fields.Values.Select(a => a.Field).OfType<FieldMList>().Select(f => f.RelationalTable);
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
        public IndexType IndexType { get; set; }

        public Field(Type fieldType)
        {
            FieldType = fieldType;
        }

        public abstract IEnumerable<IColumn> Columns();

        public virtual IEnumerable<UniqueIndex> GeneratUniqueIndexes(ITable table)
        {
            switch (IndexType)
            {
                case IndexType.None: return Enumerable.Empty<UniqueIndex>();
                case IndexType.Unique: return new[] { new UniqueIndex(table, this) };
                case IndexType.UniqueMultipleNulls: return new[] { new UniqueIndex(table, this).WhereNotNull(this) };
            }
            throw new InvalidOperationException("IndexType {0} not expected".Formato(IndexType));
        }

        internal abstract IEnumerable<KeyValuePair<Table, bool>> GetTables(); 
    }

    public enum IndexType
    {
        None,
        Unique,
        UniqueMultipleNulls
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
                throw new InvalidOperationException("{0} does not implement {1}".Formato(field.ToString(), type.Name));
        }
    }

    public partial interface IColumn
    {
        string Name { get; }
        bool Nullable { get; }
        SqlDbType SqlDbType { get; }
        string UdtTypeName { get; }
        bool PrimaryKey { get; }
        bool Identity { get; }
        int? Size { get; }
        int? Scale { get; }
        Table ReferenceTable { get; }
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
        Type FieldType { get; }
    }

    public partial class FieldPrimaryKey : Field, IColumn
    {
        public string Name { get { return SqlBuilder.PrimaryKeyName; } }
        bool IColumn.Nullable { get { return false; } }
        SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
        string IColumn.UdtTypeName { get { return null; } }
        bool IColumn.PrimaryKey { get { return true; } }
        bool IColumn.Identity { get { return table.Identity; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        Table IColumn.ReferenceTable { get { return null; } }

        Table table;
        public FieldPrimaryKey(Type fieldType, Table table)
            : base(fieldType)
        {
            this.table = table;
        }

        public override string ToString()
        {
            return "{0} PrimaryKey".Formato(Name);
        }

        public override IEnumerable<IColumn> Columns()
        {
            return new[] { this };
        }

        public override IEnumerable<UniqueIndex> GeneratUniqueIndexes(ITable table)
        {
            if (IndexType != Maps.IndexType.None)
                throw new InvalidOperationException("Changing IndexType is not allowed for FieldPrimaryKey");

            return Enumerable.Empty<UniqueIndex>();
        }

        internal override IEnumerable<KeyValuePair<Table, bool>> GetTables()
        {
            return Enumerable.Empty<KeyValuePair<Table, bool>>();
        }
    }

    public partial class FieldValue : Field, IColumn
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        public SqlDbType SqlDbType { get; set; }
        public string UdtTypeName { get; set; }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        public int? Size { get; set; }
        public int? Scale { get; set; }
        Table IColumn.ReferenceTable { get { return null; } }

        public FieldValue(Type fieldType)
            : base(fieldType)
        {
        }

        public override string ToString()
        {
            return "{0} {1} ({2},{3},{4}) {5}".Formato(
                Name,
                SqlDbType,
                Nullable ? "Nullable" : "",
                Size,
                Scale,
                IndexType.DefaultToNull().ToString());
        }

        public override IEnumerable<IColumn> Columns()
        {
            return new[] { this };
        }

        internal override IEnumerable<KeyValuePair<Table, bool>> GetTables()
        {
            return Enumerable.Empty<KeyValuePair<Table, bool>>();
        }
    }

    public partial class FieldEmbedded : Field, IFieldFinder
    {
        public partial class EmbeddedHasValueColumn : IColumn
        {
            public string Name { get; set; }
            public bool Nullable { get { return false; } } //even on neasted embeddeds
            public SqlDbType SqlDbType { get { return SqlDbType.Bit; } }
            string IColumn.UdtTypeName { get { return null; } }
            bool IColumn.PrimaryKey { get { return false; } }
            bool IColumn.Identity { get { return false; } }
            int? IColumn.Size { get { return null; } }
            int? IColumn.Scale { get { return null; } }
            public Table ReferenceTable { get { return null; } }
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
            return "Embebed\r\n{0}".Formato(EmbeddedFields.ToString(c => "{0} : {1}".Formato(c.Key, c.Value), "\r\n").Indent(2));
        }

        public Field GetField(MemberInfo member)
        {
            FieldInfo fi = member as FieldInfo ?? Reflector.FindFieldInfo(FieldType, (PropertyInfo)member);

            if (fi == null)
                throw new InvalidOperationException("Field {0} not found on {1}".Formato(member.Name, FieldType));

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

        public override IEnumerable<UniqueIndex> GeneratUniqueIndexes(ITable table)
        {
            return this.EmbeddedFields.Values.SelectMany(f => f.Field.GeneratUniqueIndexes(table));
        }

        internal override IEnumerable<KeyValuePair<Table, bool>> GetTables()
        {
            return EmbeddedFields.Values.SelectMany(f => f.Field.GetTables());
        }
    }

    public partial class FieldReference : Field, IColumn, IFieldReference
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        public SqlDbType SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
        public string UdtTypeName { get { return null; } }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get; set; }

        public bool IsLite { get; internal set; }

        public FieldReference(Type fieldType) : base(fieldType) { }

        public override string ToString()
        {
            return "{0} -> {1} {4} ({2}) {3}".Formato(
                Name,
                ReferenceTable.Name,
                IsLite ? "Lite" : "",
                Nullable ? "Nullable" : "",
                IndexType.DefaultToNull().ToString());
        }

        public override IEnumerable<IColumn> Columns()
        {
            return new[] { this };
        }

        internal override IEnumerable<KeyValuePair<Table, bool>> GetTables()
        {
            yield return KVP.Create(ReferenceTable, IsLite); 
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
    }

    public partial class FieldEnum : FieldReference
    {
        public FieldEnum(Type fieldType) : base(fieldType) { }

        public override string ToString()
        {
            return "{0} -> {1} {4} ({2}) {3}".Formato(
                Name,
                "-",
                IsLite ? "Lite" : "",
                Nullable ? "Nullable" : "",
                IndexType.DefaultToNull().ToString());
        }

        internal override IEnumerable<KeyValuePair<Table, bool>> GetTables()
        {
            if (ReferenceTable == null)
                yield break; 
            yield return KVP.Create(ReferenceTable, IsLite);
        }
    }

    public partial class FieldImplementedBy : Field, IFieldReference
    {
        public bool IsLite { get; internal set; }

        public Dictionary<Type, ImplementationColumn> ImplementationColumns { get; set; }

        public FieldImplementedBy(Type fieldType) : base(fieldType) { }

        public override string ToString()
        {
            return "ImplementedBy\r\n{0}".Formato(ImplementationColumns.ToString(k => "{0} -> {1} ({2})".Formato(k.Value.Name, k.Value.ReferenceTable.Name, k.Key.Name), "\r\n").Indent(2));
        }

        public override IEnumerable<IColumn> Columns()
        {
            return ImplementationColumns.Values.Cast<IColumn>();
        }

        internal override IEnumerable<KeyValuePair<Table, bool>> GetTables()
        {
            return ImplementationColumns.Select(a => KVP.Create(a.Value.ReferenceTable, IsLite)).ToList();
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
    }

    public partial class FieldImplementedByAll : Field, IFieldReference
    {
        public bool IsLite { get; internal set; }
        public ImplementationColumn Column { get; set; }
        public ImplementationColumn ColumnTypes { get; set; }

        public FieldImplementedByAll(Type fieldType) : base(fieldType) { }

        public override IEnumerable<IColumn> Columns()
        {
            return new[] { Column, ColumnTypes };
        }

        internal override IEnumerable<KeyValuePair<Table, bool>> GetTables()
        {
            return Enumerable.Empty<KeyValuePair<Table, bool>>();
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
    }

    public partial class ImplementationColumn : IColumn
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
        string IColumn.UdtTypeName { get { return null; } }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get; set; }
    }

    public partial class FieldMList : Field, IFieldFinder
    {
        public RelationalTable RelationalTable { get; set; }

        public FieldMList(Type fieldType) : base(fieldType) { }

        public override string ToString()
        {
            return "Coleccion\r\n{0}".Formato(RelationalTable.ToString().Indent(2));
        }

        public Field GetField(MemberInfo member)
        {
            if (member.Name == "Item")
                return RelationalTable.Field;

            throw new InvalidOperationException("{0} not supported by MList field".Formato(member.Name));
        }

        public Field TryGetField(MemberInfo member)
        {
            if (member.Name == "Item")
                return RelationalTable.Field;

            return null;
        }

        public override IEnumerable<IColumn> Columns()
        {
            return new IColumn[0];
        }

        public override IEnumerable<UniqueIndex> GeneratUniqueIndexes(ITable table)
        {
            if (IndexType != Maps.IndexType.None)
                throw new InvalidOperationException("Changing IndexType is not allowed for FieldMList");

            return Enumerable.Empty<UniqueIndex>();
        }

        internal override IEnumerable<KeyValuePair<Table, bool>> GetTables()
        {
            return RelationalTable.Field.GetTables();
        }
    }

    public partial class RelationalTable : ITable, IFieldFinder, ITablePrivate
    {
        public class PrimaryKeyColumn : IColumn
        {
            string IColumn.Name { get { return SqlBuilder.PrimaryKeyName; } }
            bool IColumn.Nullable { get { return false; } }
            SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
            string IColumn.UdtTypeName { get { return null; } }
            bool IColumn.PrimaryKey { get { return true; } }
            bool IColumn.Identity { get { return true; } }
            int? IColumn.Size { get { return null; } }
            int? IColumn.Scale { get { return null; } }
            Table IColumn.ReferenceTable { get { return null; } }
        }

        public Dictionary<string, IColumn> Columns { get; set; }
        public List<UniqueIndex> MultiIndexes { get; set; }

        public string Name { get; set; }
        public PrimaryKeyColumn PrimaryKey { get; set; }
        public FieldReference BackReference { get; set; }
        public Field Field { get; set; }

        public Type CollectionType { get; private set; }
        public Func<IList> Constructor { get; private set; }

        public RelationalTable(Type collectionType)
        {
            this.CollectionType = collectionType;
        }

        public override string ToString()
        {
            return "[{0}]\r\n  {1}\r\n  {2}".Formato(Name, BackReference.Name, Field.ToString());
        }

        public void GenerateColumns()
        {
            Columns = new IColumn[] { PrimaryKey, BackReference }.Concat(Field.Columns()).ToDictionary(a => a.Name);
        }

        public List<UniqueIndex> GeneratUniqueIndexes()
        {
            var result = Field.GeneratUniqueIndexes(this).ToList();

            if (MultiIndexes != null)
                result.AddRange(MultiIndexes);

            return result;
        }

        public Field GetField(MemberInfo member)
        {
            Field result = TryGetField(member); 

            if(result  == null)
                throw new InvalidOperationException("'{0}' not found".Formato(member.Name));

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
    }

    public enum KindOfField
    {
        PrimaryKey,
        Value,
        Reference,
        Enum,
        Embedded,
        MList,
    }

    [Flags]
    public enum Contexts
    {
        Normal = 1,
        Embedded = 2,
        MList = 4,
        View = 8,
    }

    public enum InitLevel
    {
        Level0SyncEntities,
        Level1SimpleEntities,
        Level2NormalEntities,
        Level3MainEntities,
        Level4BackgroundProcesses,
    }
}
