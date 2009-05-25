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

namespace Signum.Engine.Maps
{
    public class Schema
    {
        Dictionary<Type, Table> tables = new Dictionary<Type, Table>();
        public Dictionary<Type, Table> Tables
        {
            get { return tables; }
        }

        Dictionary<Type, int> idsForType;
        internal Dictionary<Type, int> IDsForType
        {
            get { return idsForType.ThrowIfNullC(Resources.TypeDNTableNotCached); }
            set { idsForType = value; }
        }

        Dictionary<int, Table> tablesForID;
        internal Dictionary<int, Table> TablesForID
        {
            get { return tablesForID.ThrowIfNullC(Resources.TypeDNTableNotCached); }
            set { tablesForID = value; }
        }

        #region Events
        public event EntityEventHandler Saving;
        public event EntityEventHandler Saved;

        public event TypeIdEventHandler Retrieving;
        public event EntityEventHandler Retrieved;

        public event TypeIdEventHandler Deleting;
        public event TypeIdEventHandler Deleted;

        internal void OnSaving(IdentifiableEntity entity)
        {
            if (Saving != null)
                Saving(this, entity);
        }

        internal void OnSaved(IdentifiableEntity entity)
        {
            if (Saved != null)
                Saved(this, entity);
        }

        internal void OnRetrieving(Type type, int id)
        {
            if (Retrieving != null)
                Retrieving(this, type, id);
        }

        internal void OnRetrieved(IdentifiableEntity entity)
        {
            if (Retrieved != null)
                Retrieved(this, entity);
        }

        internal void OnRetrieved(IEnumerable<IdentifiableEntity> collection)
        {
            if (Retrieved != null)
                foreach (var ei in collection)
                    Retrieved(this, ei);
        }

        internal void OnDeleting(Type type, int id)
        {
            if (Deleting != null)
                Deleting(this, type, id);
        }

        internal void OnDeleting(Type type, List<int> ids)
        {
            if (Deleting != null)
                foreach (var id in ids)
                    Deleting(this, type, id); 
        }

        internal void OnDeleted(Type type, int id)
        {
            if (Deleted != null)
                Deleted(this, type, id);
        }

        internal void OnDeleted(Type type, List<int> ids)
        {
            if (Deleted != null)
                foreach (var id in ids)
                    Deleted(this, type, id); 
        }

        public event Func<Replacements, SqlPreCommand> Synchronizing;
        public event Func<SqlPreCommand> Generating;
        public event InitEventHandler Initializing;

        internal SqlPreCommand SynchronizationScript()
        {
            if (Synchronizing == null)
                return null;

            Replacements replacements = new Replacements();
            SqlPreCommand command = Synchronizing
                .GetInvocationList()
                .Cast<Func<Replacements, SqlPreCommand>>()
                .Select(e =>
                {
                    try
                    {
                        return e(replacements);
                    }
                    catch (Exception ex)
                    {
                        return new SqlPreCommandSimple("--- Exception on {0}: {1}".Formato(e.Method, ex.Message));
                    }
                })
                .Combine(Spacing.Triple);


            return SqlPreCommand.Combine(Spacing.Double,
                new SqlPreCommandSimple("--- START OF SYNC SCRIPT GENERATED ON {0}".Formato(DateTime.Now)),
                command,
                new SqlPreCommandSimple("--- END OF SYNC SCRIPT")); 
        }

        internal SqlPreCommand GenerationScipt()
        {
            if (Generating == null)
                return null;

            return Generating
                .GetInvocationList()
                .Cast<Func<SqlPreCommand>>()
                .Select(e => e())
                .Combine(Spacing.Triple);
        }

        internal void Initialize()
        {
            if (Initializing != null)
                foreach (InitEventHandler init in Initializing.GetInvocationList())
                    init(this);
        }
        #endregion

        public Schema()
        {
            Generating += Administrator.RemoveAllScript;
            Generating += Administrator.CreateTablesScript;
            Generating += Administrator.InsertEnumValuesScript;
           
            Synchronizing += Administrator.SynchronizeSchemaScript;
            Synchronizing += Administrator.SynchronizeEnumsScript;
        }

        public static Schema Current
        {
            get { return ConnectionScope.Current.Schema; }
        }

        public Table Table<T>() where T : IdentifiableEntity
        {
            return Table(typeof(T));
        }

        public Table Table(Type type)
        {
            return Tables.GetOrThrow(type, Resources.Table0NotLoadedInSchema);
        }

        static Field FindField(IFieldFinder fieldFinder, IEnumerable<MemberInfo> members)
        {
            Field campo = fieldFinder.GetField(members.First(Resources.NoFieldWasGiven));

            if (members.Count() == 1)
                return campo;
            else
                return FindField((IFieldFinder)campo, members.Skip(1));
        }

        public Field FieldUntyped(Type type, MemberInfo[] fi)
        {
            return FindField(Table(type), fi);
        }

        public Type[] FindImplementations(Type lazyType, MemberInfo[] members)
        {
            if (!Tables.ContainsKey(lazyType))
                return null;

            Field field = FindField(Table(lazyType), members); 

            ImplementedByField ibaField = field as ImplementedByField;
            if (ibaField != null)
                return ibaField.ImplementationColumns.Keys.ToArray();

            return null;
        }

        /// <summary>
        /// Uses a lambda navigate in a strongly-typed way, you can acces field using the property and collections using Single().
        /// Nota: Haz el campo internal y añade [assembly:InternalsVisibleTo]
        /// </summary>
        public Field Field<T>(Expression<Func<T, object>> lambdaToField)
            where T : IdentifiableEntity
        {
            return FindField(Table(typeof(T)), Reflector.GetMemberList(lambdaToField));
        }

        public override string ToString()
        {
            return tables.Values.ToString(t=>t.Type.TypeName(),"\r\n\r\n"); 
        }
    }

    public delegate void EntityEventHandler(Schema sender, IdentifiableEntity ident);
    public delegate void TypeIdEventHandler(Schema sender, Type type, int id);
    public delegate void InitEventHandler(Schema sender); 

    public interface IFieldFinder
    {
        Field GetField(MemberInfo value); 
    }

    public interface ITable
    {
        string Name { get; }
        Dictionary<string, IColumn> Columns { get; }
        void GenerateColumns();
    }

    public partial class Table : IFieldFinder, ITable
    {
        public Type Type { get; private set; }

        public string Name { get; set; }
        public bool Identity {get; set;}
        public bool IsView { get; internal set; }

        public Dictionary<string, Field> Fields { get; set; }
        public Dictionary<string, IColumn> Columns { get; set; }

        public Func<object> Constructor { get; private set; }

        public Table(Type type)
        {
            this.Type = type;
            this.Constructor = ReflectionTools.CreateConstructorUntyped(type);
        }
                  
        public override string ToString()
        {
            return "[{0}] ({1})\r\n{2}".Formato(Name, Type.TypeName(), Fields.ToString(c=>"{0} : {1}".Formato(c.Key,c.Value),"\r\n").Indent(2));
        }

        public void GenerateColumns()
        {
            Columns = Fields.Values.SelectMany(c => c.Columns()).ToDictionary(c => c.Name);
            
        }
    
        public Field GetField(MemberInfo value)
        {
            return Fields.GetOrThrow(Reflector.FindFieldInfo(value).Name, Resources.Field0NotInTable1.Formato(value.Name, Name));
        }
    }

    public abstract partial class Field
    {
        public Field(Type type, FieldInfo fi, Type fieldType)
        {
            FieldInfo = fi;
            Getter = ReflectionTools.CreateGetterUntyped(type, fi);
            Setter = ReflectionTools.CreateSetterUntyped(type, fi);
            FieldType = fieldType;
        }

        public FieldInfo FieldInfo { get; private set; }
        public Func<object, object> Getter { get; private set; }
        public Action<object, object> Setter { get; private set; }

        public Type FieldType { get; private set; }

        public abstract IEnumerable<IColumn> Columns();
    }

    public partial interface IColumn
    {
        string Name { get; }
        bool Nullable{get;}
        Index Index{get;}
        SqlDbType SqlDbType { get; }
        bool PrimaryKey { get; }
        bool Identity { get; }
        int? Size { get; }
        int? Scale { get; }
        Table ReferenceTable { get; }
    }

    public interface IReferenceField
    {
        bool IsLazy { get; set; }
    }

    public partial class PrimaryKeyField : Field, IColumn
    {
        public string Name { get { return SqlBuilder.PrimaryKeyName; } }
        bool IColumn.Nullable { get { return false; } }
        Index IColumn.Index { get { return Index.None; } }
        SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
        bool IColumn.PrimaryKey { get { return true; } }
        public bool Identity { get; set; }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        Table IColumn.ReferenceTable { get { return null; } }

        public PrimaryKeyField(Type type, FieldInfo fi, Type fieldType) : base(type, fi, fieldType) { }

        public override string ToString()
        {
            return "{0} PrimaryKey".Formato(Name); 
        }

        public override IEnumerable<IColumn> Columns()
        {
            return new[] { this };
        }
    }

    public partial class ValueField : Field, IColumn
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        public Index Index { get; set; }
        public SqlDbType SqlDbType { get; set; }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        public int? Size { get; set; }
        public int? Scale { get; set; }
        Table IColumn.ReferenceTable { get { return null; } }

        public ValueField(Type type, FieldInfo fi, Type fieldType) : base(type, fi, fieldType) { }

        public override string ToString()
        {
            return "{0} {1} ({2},{3},{4}) {5}".Formato(
                Name, 
                SqlDbType, 
                Nullable? "Nullable": "", 
                Size, 
                Scale,
                Index == Index.None ? "": Index.ToString()); 
        }
        public override IEnumerable<IColumn> Columns()
        {
            return new[] { this };
        }
    }

    public partial class EmbeddedField : Field, IFieldFinder
    {
        public Dictionary<string, Field> EmbeddedFields{get;set;}

        public Func<EmbeddedEntity> Constructor { get; private set; } 

        public EmbeddedField(Type type, FieldInfo fi, Type fieldType) : base(type, fi, fieldType) 
        {
            Constructor = ReflectionTools.CreateConstructor<EmbeddedEntity>(fieldType); 
        }

        public override string ToString()
        {
            return "Embebed\r\n{0}".Formato(EmbeddedFields.ToString(c => "{0} : {1}".Formato(c.Key, c.Value), "\r\n").Indent(2));
        }

        public Field GetField(MemberInfo value)
        {
            return EmbeddedFields.GetOrThrow(Reflector.FindFieldInfo(value).Name, Resources.Field0NotInField0.Formato(value.Name, this.FieldInfo.Name));
        }

        public override IEnumerable<IColumn> Columns()
        {
            return EmbeddedFields.Values.SelectMany(c => c.Columns());
        }
    }

    public partial class ReferenceField : Field, IColumn,IReferenceField
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        public Index Index { get; set; }
        SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get; set; }

        public bool IsLazy { get; set; }

        public ReferenceField(Type type, FieldInfo fi, Type fieldType) : base(type, fi, fieldType) { }

        public override string ToString()
        {
            return "{0} -> {1} {4} ({2}) {3}".Formato(
                Name,
                ReferenceTable.Name,
                IsLazy ? "Lazy" : "",
                Nullable ? "Nullable" : "",
                Index == Index.None ? "" : Index.ToString());
        }

        public override IEnumerable<IColumn> Columns()
        {
            return new[] { this };
        }
    }

    public partial class EnumField : ReferenceField
    {
        public EnumField(Type type, FieldInfo fi, Type fieldType) : base(type, fi, fieldType) { }
    }

    public partial class ImplementedByField : Field, IReferenceField
    {
        public bool IsLazy { get; set; }

        public Dictionary<Type, ImplementationColumn> ImplementationColumns{get;set;}

        public ImplementedByField(Type type, FieldInfo fi, Type fieldType) : base(type, fi, fieldType) { }

        public override string ToString()
        {
            return "ImplementedBy\r\n{0}".Formato(ImplementationColumns.ToString(k => "{0} -> {1} ({2})".Formato(k.Value.Name, k.Value.ReferenceTable.Name, k.Key.Name), "\r\n").Indent(2));
        }

        public override IEnumerable<IColumn> Columns()
        {
            return ImplementationColumns.Values.Cast<IColumn>(); 
        }
    }

    public partial class ImplementedByAllField : Field, IReferenceField
    {
        public bool IsLazy { get; set; }
        public ImplementationColumn Column { get; set; }
        public ImplementationColumn ColumnTypes { get; set; }

        public ImplementedByAllField(Type type, FieldInfo fi, Type fieldType) : base(type, fi, fieldType) { }

        public override IEnumerable<IColumn> Columns()
        {
            return new[] { Column, ColumnTypes };
        }
    }

    public partial class ImplementationColumn : IColumn
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        public Index Index { get; set; }
        SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get; set; }
    }

    public partial class CollectionField : Field, IFieldFinder
    {
        public RelationalTable RelationalTable { get; set; }

        public CollectionField(Type type, FieldInfo fi, Type fieldType) : base(type, fi, fieldType) { }

        public override string ToString()
        {
            return "Coleccion\r\n{0}".Formato(RelationalTable.ToString().Indent(2));
        }

        static readonly string[] elementMethods = new[] { "First" , "FirstOrDefault" ,"Single" ,"SingleOrDefault" };
        public Field GetField(MemberInfo value)
        {
            if (value is MethodInfo && elementMethods.Contains(value.Name)  || value is PropertyInfo && value.Name == "Item" )
                return RelationalTable.Field;

            throw new ApplicationException(Resources.MemberInfo0NotSupportedByCollectionField.Formato(value)); 
        }

        public override IEnumerable<IColumn> Columns()
        {
            return new IColumn[0];
        }
    }

    public partial class RelationalTable: ITable
    {
        public class PrimaryKeyColumn : IColumn
        {
            string IColumn.Name { get { return SqlBuilder.PrimaryKeyName; } }
            bool IColumn.Nullable { get { return false; } }
            Index IColumn.Index { get { return Index.None; } }
            SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
            bool IColumn.PrimaryKey { get { return true; } }
            bool IColumn.Identity { get { return true; } }
            int? IColumn.Size { get { return null; } }
            int? IColumn.Scale { get { return null; } }
            Table IColumn.ReferenceTable { get { return null; } }
        }

        public partial class BackReferenceColumn : IColumn
        {
            public string Name { get; set; }
            bool IColumn.Nullable { get { return false; } }
            public Index Index { get; set; }
            SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
            bool IColumn.PrimaryKey { get { return false; } }
            bool IColumn.Identity { get { return false; } }
            int? IColumn.Size { get { return null; } }
            int? IColumn.Scale { get { return null; } }
            public Table ReferenceTable { get; set; }
        }

        public Dictionary<string, IColumn> Columns { get; set; }

        public string Name { get; set; }
        public PrimaryKeyColumn PrimaryKey { get; set; }
        public BackReferenceColumn BackReference { get; set; }
        public Field Field{get; set;}

        public Type CollectionType { get; private set; }
        public Func<IList> Constructor { get; private set; }

        public RelationalTable(Type collectionType)
        {
            this.CollectionType = collectionType;
            this.Constructor = ReflectionTools.CreateConstructor<IList>(collectionType); 
        }

        public override string ToString()
        {
            return "[{0}]\r\n  {1}\r\n  {2}".Formato(Name, BackReference.Name, Field.ToString());
        }

        public void GenerateColumns()
        {
            Columns = new IColumn[] { PrimaryKey, BackReference}.Union(Field.Columns()).ToDictionary(a => a.Name);
        }
    }

    public enum KindOfField
    {
        PrimaryKey,
        Value,
        Reference,
        Enum,
        Embedded,
        Collection,
        Lazy
    }

    [Flags]
    public enum Contexts
    {
        Normal = 1,
        Embedded = 2,
        Collection = 4,
        Lazy = 8,
        View = 16,
    }

   
}
