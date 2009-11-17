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

namespace Signum.Engine.Maps
{
    public class Schema
    {
        bool silentMode = false;
        public bool SilentMode
        {
            get { return silentMode; }
            set { this.silentMode = value; }
        }

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

        readonly IEntityEvents entityEventsGlobal = new EntityEvents<IdentifiableEntity>(); 
        public EntityEvents<IdentifiableEntity> EntityEventsGlobal
        {
            get { return (EntityEvents<IdentifiableEntity>)entityEventsGlobal; }
        }

        Dictionary<Type, IEntityEvents> entityEvents = new Dictionary<Type,IEntityEvents>();
        public EntityEvents<T> EntityEvents<T>()
            where T : IdentifiableEntity
        {
            return (EntityEvents<T>)entityEvents.GetOrCreate(typeof(T), () => new EntityEvents<T>());
        }

        internal void OnSaving(IdentifiableEntity entity, bool isRoot, ref bool graphModified)
        {
            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnSaving(entity, isRoot, ref graphModified);

            entityEventsGlobal.OnSaving(entity, isRoot, ref graphModified); 
        }

        internal void OnSaved(IdentifiableEntity entity, bool isRoot)
        {
            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnSaved(entity, isRoot);

            entityEventsGlobal.OnSaved(entity, isRoot); 
        }

        internal void OnRetrieving(Type type, int id, bool isRoot)
        {
            IEntityEvents ee = entityEvents.TryGetC(type);

            if (ee != null)
                ee.OnRetrieving(type, id, isRoot);

            entityEventsGlobal.OnRetrieving(type, id, isRoot); 
        }

        internal void OnRetrieved(IdentifiableEntity entity, bool isRoot)
        {
            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnRetrieved(entity, isRoot);

            entityEventsGlobal.OnRetrieved(entity, isRoot); 
        }

        internal void OnDeleting(Type type, List<int> ids)
        {
            IEntityEvents ee = entityEvents.TryGetC(type);

            foreach (var id in ids)
            {
                if (ee != null)
                    ee.OnDeleting(type, id);

                entityEventsGlobal.OnDeleting(type, id); 
            }
        }

        internal void OnDeleted(Type type, List<int> ids)
        {
            IEntityEvents ee = entityEvents.TryGetC(type);

            foreach (var id in ids)
            {
                if (ee != null)
                    ee.OnDeleted(type, id);

                entityEventsGlobal.OnDeleted(type, id);
            }
        }

        internal IQueryable<T> OnFilterQuery<T>(IQueryable<T> query)
            where T: IdentifiableEntity
        {
            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));
            if (ee == null)
                return query;

            return ee.OnFilterQuery(query);
        }

        public event Func<Replacements, SqlPreCommand> Synchronizing;
        internal SqlPreCommand SynchronizationScript(string schemaName)
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
                        return new SqlPreCommandSimple(Resources.ExceptionOn01.Formato(e.Method, ex.Message));
                    }
                })
                .Combine(Spacing.Triple);


            return SqlPreCommand.Combine(Spacing.Double,
                new SqlPreCommandSimple(Resources.StartOfSyncScriptGeneratedOn0.Formato(DateTime.Now)),
                new SqlPreCommandSimple("use {0}".Formato(schemaName)),
                command,
                new SqlPreCommandSimple(Resources.EndOfSyncScript)); 
        }

        public event Func<SqlPreCommand> Generating;
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
        
        class InitPair
        {
            public InitLevel Level;
            public InitEventHandler Handler; 
        }

        List<InitPair> initializing = new List<InitPair>();
        public void Initializing(InitLevel level, InitEventHandler handler)
        {
            initializing.Add(new InitPair { Level = level, Handler = handler }); 
        }

        InitLevel? initLevel;
        
        public void Initialize()
        {
            Initialize(InitLevel.Level4BackgroundProcesses);
        }

        public void Initialize(InitLevel topLevel)
        {
            if (initLevel.HasValue && initLevel.Value > initLevel)
                return;

            var handlers = initializing.Where(pair => (initLevel == null || initLevel < pair.Level) && pair.Level <= topLevel).ToList();

            if (SilentMode)
            {
                foreach (InitPair pair in handlers)
                {
                    pair.Handler(this);
                }
            }
            else
            {
                Stopwatch sw = new Stopwatch();
                foreach (InitPair pair in handlers)
                {
                    sw.Reset();
                    sw.Start();
                    pair.Handler(this);
                    sw.Stop();
                    Debug.WriteLine("{1:0.00} ms initializing {0} ".Formato(pair.Handler.Method.DeclaringType.TypeName(), sw.Elapsed.TotalMilliseconds));
                }
            }

            initLevel = topLevel;
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

        static Field FindField(IFieldFinder fieldFinder, MemberInfo[] members, bool throws)
        {
            IFieldFinder current = fieldFinder; 
            Field result = null;
            foreach (var mi in members)
            {
                if (current == null)
                    return null; 

                result = current.GetField(mi, throws);

                if (result == null && !throws)
                    return null; 

                
                current = result as IFieldFinder; 
            }

            return result; 
        }

        public Type[] FindImplementations(Type liteType, MemberInfo[] members)
        {
            if (!Tables.ContainsKey(liteType))
                return null;
            
            Field field = FindField(Table(liteType), members, false); 

            FieldImplementedBy ibaField = field as FieldImplementedBy;
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
            return FindField(Table(typeof(T)), Reflector.GetMemberList(lambdaToField, true), true);
        }

        public override string ToString()
        {
            return tables.Values.ToString(t=>t.Type.TypeName(),"\r\n\r\n"); 
        }
    }

    internal interface IEntityEvents
    {
        void OnSaving(IdentifiableEntity entity, bool isRoot, ref bool graphModified);
        void OnSaved(IdentifiableEntity entity, bool isRoot);
        void OnRetrieving(Type type, int id, bool isRoot);
        void OnRetrieved(IdentifiableEntity entity, bool isRoot);
        void OnDeleting(Type type, int id);
        void OnDeleted(Type type, int id);
    }

    public class EntityEvents<T> : IEntityEvents
        where T : IdentifiableEntity
    {
        public event SavingEntityEventHandler<T> Saving;
        public event EntityEventHandler<T> Saved;

        public event TypeIdRootEventHandler Retrieving;
        public event EntityEventHandler<T> Retrieved;

        public event TypeIdEventHandler Deleting;
        public event TypeIdEventHandler Deleted;

        public event FilterQueryEventHandler<T> FilterQuery; 

        public IQueryable<T> OnFilterQuery(IQueryable<T> query)
        {
            if(FilterQuery != null)
                foreach (FilterQueryEventHandler<T> filter in FilterQuery.GetInvocationList())
                    query = filter(query); 

            return query; 
        }

        void IEntityEvents.OnSaving(IdentifiableEntity entity, bool isRoot, ref bool graphModified)
        {
            if (Saving != null)
                Saving((T)entity, isRoot, ref graphModified);
        }

        void IEntityEvents.OnSaved(IdentifiableEntity entity, bool isRoot)
        {
            if (Saved != null)
                Saved((T)entity, isRoot);
        }

        void IEntityEvents.OnRetrieving(Type type, int id, bool isRoot)
        {
            if (Retrieving != null)
                Retrieving(type, id, isRoot);
        }

        void IEntityEvents.OnRetrieved(IdentifiableEntity entity, bool isRoot)
        {
            if (Retrieved != null)
                Retrieved((T)entity, isRoot);
        }

        void IEntityEvents.OnDeleting(Type type, int id)
        {
            if (Deleting != null)
                Deleting(type, id);
        }

        void IEntityEvents.OnDeleted(Type type, int id)
        {
            if (Deleted != null)
                Deleted(type, id);
        }
    }

    public delegate void SavingEntityEventHandler<T>(T ident, bool isRoot, ref bool graphModified) where T : IdentifiableEntity;
    public delegate void EntityEventHandler<T>(T ident, bool isRoot) where T : IdentifiableEntity;
    public delegate void TypeIdRootEventHandler(Type type, int id, bool isRoot);
    public delegate void TypeIdEventHandler(Type type, int id);
    public delegate IQueryable<T> FilterQueryEventHandler<T>(IQueryable<T> query);

    public delegate void InitEventHandler(Schema sender); 

    public interface IFieldFinder
    {
        Field GetField(MemberInfo value, bool throws); 
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

        public Dictionary<string, EntityField> Fields { get; set; }
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
            Columns = Fields.Values.SelectMany(c => c.Field.Columns()).ToDictionary(c => c.Name);
            
        }

        public Field GetField(MemberInfo value, bool throws)
        {
            FieldInfo fi = Reflector.FindFieldInfo(Type, value, throws);

            if (fi == null && !throws)
                return null;

            EntityField field = Fields.TryGetC(fi.Name);

            if (field == null)
                if (throws)
                    throw new ApplicationException(Resources.Field0NotInType1.Formato(value.Name, Type.TypeName()));
                else
                    return null;

            return field.Field;
        }
    }

    public class EntityField
    {
        public Field Field { get; set; }
        public FieldInfo FieldInfo { get; private set; }
        public Func<object, object> Getter { get; private set; }
        public Action<object, object> Setter { get; private set; }

        public EntityField(Type type, FieldInfo fi)
        {
            FieldInfo = fi;
            Getter = ReflectionTools.CreateGetterUntyped(type, fi);
            Setter = ReflectionTools.CreateSetterUntyped(type, fi);
        }
    }

    public abstract partial class Field
    {
        public Type FieldType { get; private set; }

        public Field(Type fieldType)
        {
            FieldType = fieldType;
        }
        
        public abstract IEnumerable<IColumn> Columns();
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
                throw new ApplicationException("{0} does not implement {1}".Formato(field.ToString(), type.Name)); 
        }
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

    public interface IFieldReference
    {
        bool IsLite { get; set; }
    }

    public partial class FieldPrimaryKey : Field, IColumn
    {
        public string Name { get { return SqlBuilder.PrimaryKeyName; } }
        bool IColumn.Nullable { get { return false; } }
        Index IColumn.Index { get { return Index.None; } }
        SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
        bool IColumn.PrimaryKey { get { return true; } }
        bool IColumn.Identity { get { return table.Identity; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        Table IColumn.ReferenceTable { get { return null; } }

        Table table; 
        public FieldPrimaryKey(Type fieldType, Table table) : base(fieldType) 
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
    }

    public partial class FieldValue : Field, IColumn
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

        public FieldValue(Type fieldType) : base(fieldType) { }

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

    public partial class FieldEmbedded : Field, IFieldFinder
    {
        public Dictionary<string, EntityField> EmbeddedFields { get; set; }

        public Func<EmbeddedEntity> Constructor { get; private set; } 

        public FieldEmbedded(Type fieldType) : base(fieldType) 
        {
            Constructor = ReflectionTools.CreateConstructor<EmbeddedEntity>(fieldType); 
        }

        public override string ToString()
        {
            return "Embebed\r\n{0}".Formato(EmbeddedFields.ToString(c => "{0} : {1}".Formato(c.Key, c.Value), "\r\n").Indent(2));
        }

        public Field GetField(MemberInfo value, bool throws)
        {
            FieldInfo fi = Reflector.FindFieldInfo(FieldType, value, throws);

            if (fi == null && !throws)
                return null;

            EntityField field = EmbeddedFields.TryGetC(fi.Name);

            if (field == null)
                if (throws)
                    throw new ApplicationException(Resources.Field0NotInType1.Formato(value.Name, FieldType.TypeName()));
                else
                    return null;

            return field.Field;
        }

        public override IEnumerable<IColumn> Columns()
        {
            return EmbeddedFields.Values.SelectMany(c => c.Field.Columns());
        }
    }

    public partial class FieldReference : Field, IColumn, IFieldReference
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

        public bool IsLite { get; set; }

        public FieldReference(Type fieldType) : base(fieldType) { }

        public override string ToString()
        {
            return "{0} -> {1} {4} ({2}) {3}".Formato(
                Name,
                ReferenceTable.Name,
                IsLite ? "Lite" : "",
                Nullable ? "Nullable" : "",
                Index == Index.None ? "" : Index.ToString());
        }

        public override IEnumerable<IColumn> Columns()
        {
            return new[] { this };
        }
    }

    public partial class FieldEnum : FieldReference
    {
        public FieldEnum(Type fieldType) : base(fieldType) { }
    }

    public partial class FieldImplementedBy : Field, IFieldReference
    {
        public bool IsLite { get; set; }

        public Dictionary<Type, ImplementationColumn> ImplementationColumns{get;set;}

        public FieldImplementedBy(Type fieldType) : base(fieldType) { }

        public override string ToString()
        {
            return "ImplementedBy\r\n{0}".Formato(ImplementationColumns.ToString(k => "{0} -> {1} ({2})".Formato(k.Value.Name, k.Value.ReferenceTable.Name, k.Key.Name), "\r\n").Indent(2));
        }

        public override IEnumerable<IColumn> Columns()
        {
            return ImplementationColumns.Values.Cast<IColumn>(); 
        }
    }

    public partial class FieldImplementedByAll : Field, IFieldReference
    {
        public bool IsLite { get; set; }
        public ImplementationColumn Column { get; set; }
        public ImplementationColumn ColumnTypes { get; set; }

        public FieldImplementedByAll(Type fieldType) : base(fieldType) { }

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

    public partial class FieldMList : Field, IFieldFinder
    {
        public RelationalTable RelationalTable { get; set; }

        public FieldMList(Type fieldType) : base(fieldType) { }

        public override string ToString()
        {
            return "Coleccion\r\n{0}".Formato(RelationalTable.ToString().Indent(2));
        }

        static readonly string[] elementMethods = new[] { "First" , "FirstOrDefault" ,"Single" ,"SingleOrDefault" };
        public Field GetField(MemberInfo value, bool throws)
        {
            if (value is MethodInfo && elementMethods.Contains(value.Name)  || value is PropertyInfo && value.Name == "Item" )
                return RelationalTable.Field;

            if (throws)
                throw new ApplicationException(Resources.MemberInfo0NotSupportedByCollectionField.Formato(value));

            return null;
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
