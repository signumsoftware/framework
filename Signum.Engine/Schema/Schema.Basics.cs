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

        public TimeZoneMode TimeZoneMode { get; set; }

        public SchemaSettings Settings { get; private set; }

        Dictionary<Type, Table> tables = new Dictionary<Type, Table>();
        public Dictionary<Type, Table> Tables
        {
            get { return tables; }
        }
        
        const string errorType = "TypeDN table not cached. Remember to call Schema.Current.Initialize";

        Dictionary<Type, int> idsForType;
        internal Dictionary<Type, int> IDsForType
        {
            get { return idsForType.ThrowIfNullC(errorType); }
            set { idsForType = value; }
        }

        Dictionary<int, Table> tablesForID;
        internal Dictionary<int, Table> TablesForID
        {
            get { return tablesForID.ThrowIfNullC(errorType); }
            set { tablesForID = value; }
        }

        Dictionary<Type, TypeDN> typeToDN;
        internal Dictionary<Type, TypeDN> TypeToDN
        {
            get { return typeToDN.ThrowIfNullC(errorType); }
            set { typeToDN = value; }
        }

        Dictionary<TypeDN, Type> dnToType;
        internal Dictionary<TypeDN, Type> DnToType
        {
            get { return dnToType.ThrowIfNullC(errorType); }
            set { dnToType = value; }
        }

        #region Events

        public event Func<Type, string> IsAllowedCallback;

        public string IsAllowed(Type type)
        {
            if (IsAllowedCallback != null)
                foreach (Func<Type, string> f in IsAllowedCallback.GetInvocationList())
                {
                    string result = f(type);

                    if (result != null)
                        return result;
                }

            return null;
        }

        public void AssertAllowed(Type type)
        {
            string error = IsAllowed(type);

            if (error != null)
                throw new UnauthorizedAccessException(Resources.UnauthorizedAccessTo0Because1.Formato(type.NiceName(), error));
        }

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

        internal void OnPreSaving(IdentifiableEntity entity, bool isRoot, ref bool graphModified)
        {
            AssertAllowed(entity.GetType()); 

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnPreSaving(entity, isRoot, ref graphModified);

            entityEventsGlobal.OnPreSaving(entity, isRoot, ref graphModified); 
        }

        internal void OnSaving(IdentifiableEntity entity, bool isRoot)
        {
            AssertAllowed(entity.GetType());

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnSaving(entity, isRoot);

            entityEventsGlobal.OnSaving(entity, isRoot);
        }

        internal void OnRetrieving(Type type, int id, bool isRoot)
        {
            AssertAllowed(type); 

            IEntityEvents ee = entityEvents.TryGetC(type);

            if (ee != null)
                ee.OnRetrieving(type, id, isRoot);

            entityEventsGlobal.OnRetrieving(type, id, isRoot); 
        }

        internal void OnRetrieved(IdentifiableEntity entity, bool isRoot)
        {
            AssertAllowed(entity.GetType()); 

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnRetrieved(entity, isRoot);

            entityEventsGlobal.OnRetrieved(entity, isRoot); 
        }

        internal void OnDeleting(Type type, List<int> ids)
        {
            AssertAllowed(type); 

            IEntityEvents ee = entityEvents.TryGetC(type);

            foreach (var id in ids)
            {
                if (ee != null)
                    ee.OnDeleting(type, id);

                entityEventsGlobal.OnDeleting(type, id); 
            }
        }

        internal IQueryable<T> OnFilterQuery<T>(IQueryable<T> query)
            where T: IdentifiableEntity
        {
            AssertAllowed(typeof(T)); 

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
                        return new SqlPreCommandSimple("Exception on {0}".Formato(e.Method, ex.Message));
                    }
                })
                .Combine(Spacing.Triple);

            if (command == null)
                return null; 

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

            public override string ToString()
            {
                return "{0} -> {1}.{2}".Formato(Level, Handler.Method.DeclaringType.TypeName(), Handler.Method.MethodName());
            }
        }

        List<InitPair> initializing = new List<InitPair>();
        public void Initializing(InitLevel level, InitEventHandler handler)
        {
            initializing.Insert(initializing.FindIndex(p => p.Level > level).NotFound(initializing.Count),
                new InitPair { Level = level, Handler = handler });
        }

        InitLevel? initLevel;
        
        public void Initialize()
        {
            Initialize(InitLevel.Level4BackgroundProcesses);
        }

        public void Initialize(InitLevel topLevel)
        {
            for (InitLevel current = initLevel ?? InitLevel.Level0SyncEntities; current <= topLevel; current++)
            {
                InitializeJust(current); 
            }

            initLevel = topLevel + 1; 
        }

        void InitializeJust(InitLevel currentLevel)
        {
            var handlers = initializing.Where(pair => currentLevel == pair.Level).ToList();

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
                    Debug.WriteLine("{0} ms initializing {1}".Formato(pair.Handler.Method.DeclaringType.TypeName(), sw.Elapsed.TotalMilliseconds));
                }
            }
        }
        #endregion

        static Schema()
        {
            PropertyRoute.SetFindImplementationsCallback(pr => Schema.Current.FindImplementations(pr));
        }

        internal Schema(SchemaSettings settings)
        {
            this.Settings = settings;

            Generating += Administrator.RemoveAllScript;
            Generating += Administrator.ShrinkDataBase;
            Generating += Administrator.CreateTablesScript;
            Generating += Administrator.InsertEnumValuesScript;
            Generating += TypeLogic.Schema_Generating;


            Synchronizing += Administrator.SynchronizeSchemaScript;
            Synchronizing += Administrator.SynchronizeEnumsScript;
            Synchronizing += TypeLogic.Schema_Synchronizing;

            Initializing(InitLevel.Level0SyncEntities, TypeLogic.Schema_Initializing);
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
            return Tables.GetOrThrow(type, "Table {0} not loaded in schema");
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

        public Implementations FindImplementations(PropertyRoute route)
        {
            Type type = route.IdentifiableType; 

            if (!Tables.ContainsKey(type))
                return null;

            Field field = FindField(Table(type), route.Properties, false);

            FieldImplementedBy ibField = field as FieldImplementedBy;
            if (ibField != null)
                return new ImplementedByAttribute(ibField.ImplementationColumns.Keys.ToArray());

            FieldImplementedByAll ibaField = field as FieldImplementedByAll;
            if (ibaField != null)
                return new ImplementedByAllAttribute();

            return null;
        }

        /// <summary>
        /// Uses a lambda navigate in a strongly-typed way, you can acces field using the property and collections using Single().
        /// Nota: Haz el campo internal y añade [assembly:InternalsVisibleTo]
        /// </summary>
        public Field Field<T>(Expression<Func<T, object>> lambdaToField)
            where T : IdentifiableEntity
        {
            return FindField(Table(typeof(T)), Reflector.GetMemberList(lambdaToField), true);
        }

        public override string ToString()
        {
            return tables.Values.ToString(t=>t.Type.TypeName(),"\r\n\r\n"); 
        }

        internal Dictionary<string, ITable> GetDatabaseTables()
        {
            return Schema.Current.Tables.Values.SelectMany(t => 
                t.Fields.Values.Select(a => a.Field).OfType<FieldMList>().Select(f => (ITable)f.RelationalTable).PreAnd(t))
                .ToDictionary(a => a.Name);
        }
    }

    internal interface IEntityEvents
    {
        void OnPreSaving(IdentifiableEntity entity, bool isRoot, ref bool graphModified);
        void OnSaving(IdentifiableEntity entity, bool isRoot);
        void OnRetrieving(Type type, int id, bool isRoot);
        void OnRetrieved(IdentifiableEntity entity, bool isRoot);
        void OnDeleting(Type type, int id);
    }

    public class EntityEvents<T> : IEntityEvents
        where T : IdentifiableEntity
    {
        public event PreSavingEntityEventHandler<T> PreSaving;
        public event EntityEventHandler<T> Saving;

        public event RetrivingEntityEventHandler Retrieving;
        public event EntityEventHandler<T> Retrieved;

        public event DeleteEntityEventHandler Deleting;

        public event FilterQueryEventHandler<T> FilterQuery; 

        public IQueryable<T> OnFilterQuery(IQueryable<T> query)
        {
            if(FilterQuery != null)
                foreach (FilterQueryEventHandler<T> filter in FilterQuery.GetInvocationList())
                    query = filter(query); 

            return query; 
        }

        void IEntityEvents.OnPreSaving(IdentifiableEntity entity, bool isRoot, ref bool graphModified)
        {
            if (PreSaving != null)
                PreSaving((T)entity, isRoot, ref graphModified);
        }

        void IEntityEvents.OnSaving(IdentifiableEntity entity, bool isRoot)
        {
            if (Saving != null)
                Saving((T)entity, isRoot);
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
    }

    public delegate void PreSavingEntityEventHandler<T>(T ident, bool isRoot, ref bool graphModified) where T : IdentifiableEntity;
    public delegate void EntityEventHandler<T>(T ident, bool isRoot) where T : IdentifiableEntity;
    public delegate void SavedEntityEventHandler<T>(T ident, SavedEventArgs args) where T : IdentifiableEntity;
    public delegate void RetrivingEntityEventHandler(Type type, int id, bool isRoot);
    public delegate void DeleteEntityEventHandler(Type type, int id);
    public delegate IQueryable<T> FilterQueryEventHandler<T>(IQueryable<T> query);

    public delegate void InitEventHandler(Schema sender);

    public class SavedEventArgs
    {
        public bool IsRoot { get; set; }
        public bool WasNew { get; set; }
        public bool WasModified { get; set; }
    }

    public interface IFieldFinder
    {
        Field GetField(MemberInfo value, bool throws); 
    }

    public class UniqueIndex
    {
        public ITable Table { get; private set; }
        public IColumn[] Columns { get; private set; }
        public string Where { get; set; }

        public UniqueIndex(ITable table, params Field[] fields)
        {
            if (table == null)
                throw new ArgumentNullException("table"); 

            if (fields == null || fields.Empty())
                throw new InvalidOperationException("No fields");

            if (fields.OfType<FieldEmbedded>().Any())
                throw new InvalidOperationException("Embedded fields not supported for indexes");

            this.Table = table; 
            this.Columns = fields.SelectMany(f => f.Columns()).ToArray();
        }


        public UniqueIndex(ITable table, params IColumn[] columns)
        {
            if (table == null)
                throw new ArgumentNullException("table"); 

            if (columns == null || columns.Empty())
                throw new ArgumentNullException("columns");

            this.Table = table; 
            this.Columns = columns;
        }

        public string IndexName
        {
            get { return "IX_{0}_{1}".Formato(Table.Name, ColumnSignature()); }
        }

        public string ViewName
        {
            get
            {
                if (string.IsNullOrEmpty(Where) || ConnectionScope.Current.DBMS != DBMS.SqlServer2005)
                    return null;

                return "VIX_{0}_{1}".Formato(Table.Name, ColumnSignature()); 
            }
        }

        string ColumnSignature()
        {
            string columns = Columns.ToString(c => c.Name, "_");
            if (string.IsNullOrEmpty(Where))
                return columns;

            return columns + "_" + Encode(Where);
        }

        static readonly string letters = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        static string Encode(string str)
        {
            int hash = GetHashCode32(str);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 32; i+=5)
            {
                sb.Append(letters[hash & 31]);
                hash >>= 5; 
            }

            return sb.ToString(); 
        }

        static int GetHashCode32(string value)
        {
            int num = 0x15051505;
            int num2 = num;
            for (int i = 0; i < value.Length; i++)
            {
                if ((i & 0x1) == 0)
                    num = (((num << 5) + num) + (num >> 0x1b)) ^ value[i];
                else
                    num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ value[i];
            }
           
            return (num + (num2 * 0x5d588b65));
        }


        public UniqueIndex WhereNotNull(params IColumn[] notNullColumns)
        {
            if (notNullColumns == null || notNullColumns.Empty())
            {
                Where = null;
                return this;
            }

            this.Where = notNullColumns.ToString(c => c.Name.SqlScape() + " IS NOT NULL", " AND ");
            return this; 
        }

        public UniqueIndex WhereNotNull(params Field[] notNullFields)
        {
            if (notNullFields == null || notNullFields.Empty())
            {
                Where = null;
                return this;
            }

            if(notNullFields.OfType<FieldEmbedded>().Any())
                throw new InvalidOperationException("Embedded fields not supported for indexes");

            this.WhereNotNull(notNullFields.Where(a => !IsComplexIB(a)).SelectMany(a => a.Columns()).ToArray());

            if (notNullFields.Any(IsComplexIB))
                this.Where += " AND " + notNullFields.Where(IsComplexIB).ToString(f => "({0})".Formato(f.Columns().ToString(c => c.Name.SqlScape() + " IS NOT NULL", " OR ")), " AND ");

            return this;
        }

        static bool IsComplexIB(Field field)
        {
            return field is FieldImplementedBy && ((FieldImplementedBy)field).ImplementationColumns.Count > 1;
        }

        public override string ToString()
        {
            return IndexName;
        }

        static readonly IColumn[] Empty = new IColumn[0]; 
    }

    public interface ITable
    {
        string Name { get; }
        Dictionary<string, IColumn> Columns { get; }

        List<UniqueIndex> MultiIndexes { get; set; }

        List<UniqueIndex> GeneratUniqueIndexes();

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

        public List<UniqueIndex> MultiIndexes { get; set; }

        public Func<object> Constructor { get; private set; }

        public Table(Type type)
        {
            this.Type = type;
            this.Constructor = ReflectionTools.CreateConstructor<object>(type);
        }
                  
        public override string ToString()
        {
            return "[{0}] ({1})\r\n{2}".Formato(Name, Type.TypeName(), Fields.ToString(c=>"{0} : {1}".Formato(c.Key,c.Value),"\r\n").Indent(2));
        }

        public void GenerateColumns()
        {
            Columns = Fields.Values.SelectMany(c => c.Field.Columns()).ToDictionary(c => c.Name);

            int i = 0;
            foreach (var col in Columns.Values)
            {
                col.Position = i++;
            }

            CompleteRetrieve(); 
        }

        public Field GetField(MemberInfo value, bool throws)
        {
            FieldInfo fi = Reflector.FindFieldInfo(Type, value, throws);

            if (fi == null && !throws)
                return null;

            EntityField field = Fields.TryGetC(fi.Name);

            if (field == null)
                if (throws)
                    throw new InvalidOperationException("Field {0} not in type {1}".Formato(value.Name, Type.TypeName()));
                else
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
            throw new NotImplementedException();
        }
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
        bool Nullable{get;}
        int Position { get; set; }
        SqlDbType SqlDbType { get; }
        bool PrimaryKey { get; }
        bool Identity { get; }
        int? Size { get; }
        int? Scale { get; }
        Table ReferenceTable { get; }
    }

    public interface IFieldReference
    {
        bool IsLite { get; }
        Type FieldType { get; }
    }

    public partial class FieldPrimaryKey : Field, IColumn
    {
        public string Name { get { return SqlBuilder.PrimaryKeyName; } }
        bool IColumn.Nullable { get { return false; } }
        SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
        bool IColumn.PrimaryKey { get { return true; } }
        bool IColumn.Identity { get { return table.Identity; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        Table IColumn.ReferenceTable { get { return null; } }
        public int Position { get; set; }

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

        public override IEnumerable<UniqueIndex> GeneratUniqueIndexes(ITable table)
        {
            if (IndexType != Maps.IndexType.None)
                throw new InvalidOperationException("Changing IndexType is not allowed for FieldPrimaryKey"); 

            return Enumerable.Empty<UniqueIndex>(); 
        }
    }

    public partial class FieldValue : Field, IColumn
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        public SqlDbType SqlDbType { get; set; }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        public int? Size { get; set; }
        public int? Scale { get; set; }
        Table IColumn.ReferenceTable { get { return null; } }
        public int Position { get; set; }

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
    }

    public partial class FieldEmbedded : Field, IFieldFinder
    {
        public partial class EmbeddedHasValueColumn : IColumn
        {
            public string Name { get; set; }
            public bool Nullable { get { return false; } } //even on neasted embeddeds
            public SqlDbType SqlDbType { get { return SqlDbType.Bit; } }
            bool IColumn.PrimaryKey { get { return false; } }
            bool IColumn.Identity { get { return false; } }
            int? IColumn.Size { get { return null; } }
            int? IColumn.Scale { get { return null; } }
            public Table ReferenceTable { get { return null; } }
            public int Position { get; set; }
        }

        public EmbeddedHasValueColumn HasValue { get; set; }

        public Dictionary<string, EntityField> EmbeddedFields { get; set; }

        public Func<EmbeddedEntity> Constructor { get; private set; } 

        public FieldEmbedded(Type fieldType) : base(fieldType) 
        {
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
                    throw new InvalidOperationException("Field {0} not in type {1}".Formato(value.Name, FieldType.TypeName()));
                else
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
    }

    public partial class FieldReference : Field, IColumn, IFieldReference
    {
        public string Name { get; set; }
        public bool Nullable { get; set; }
        SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get; set; }
        public int Position { get; set; }

        public bool IsLite { get; set; }

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
        SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
        bool IColumn.PrimaryKey { get { return false; } }
        bool IColumn.Identity { get { return false; } }
        int? IColumn.Size { get { return null; } }
        int? IColumn.Scale { get { return null; } }
        public Table ReferenceTable { get; set; }
        public int Position { get; set; }
    }

    public partial class FieldMList : Field, IFieldFinder
    {
        public RelationalTable RelationalTable { get; set; }

        public FieldMList(Type fieldType) : base(fieldType) { }

        public override string ToString()
        {
            return "Coleccion\r\n{0}".Formato(RelationalTable.ToString().Indent(2));
        }
        public Field GetField(MemberInfo value, bool throws)
        {
            if (value is PropertyInfo && value.Name == "Item" )
                return RelationalTable.Field;

            if (throws)
                throw new InvalidOperationException("MemberInfo {0} not supported by MList field".Formato(value));

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
    }

    public partial class RelationalTable: ITable
    {
        public class PrimaryKeyColumn : IColumn
        {
            string IColumn.Name { get { return SqlBuilder.PrimaryKeyName; } }
            bool IColumn.Nullable { get { return false; } }
            SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
            bool IColumn.PrimaryKey { get { return true; } }
            bool IColumn.Identity { get { return true; } }
            int? IColumn.Size { get { return null; } }
            int? IColumn.Scale { get { return null; } }
            Table IColumn.ReferenceTable { get { return null; } }
            public int Position { get; set; }
        }

        public partial class BackReferenceColumn : Field, IColumn
        {
            public BackReferenceColumn(Type fieldType)
                : base(fieldType)
            {
            }

            public string Name { get; set; }
            bool IColumn.Nullable { get { return false; } }
            SqlDbType IColumn.SqlDbType { get { return SqlBuilder.PrimaryKeyType; } }
            bool IColumn.PrimaryKey { get { return false; } }
            bool IColumn.Identity { get { return false; } }
            int? IColumn.Size { get { return null; } }
            int? IColumn.Scale { get { return null; } }
            public Table ReferenceTable { get; set; }
            public int Position { get; set; }

            public override IEnumerable<IColumn> Columns()
            {
                yield return this; 
            }

            public override IEnumerable<UniqueIndex> GeneratUniqueIndexes(ITable table)
            {
                throw new NotImplementedException();
            }

            internal override Expression GetExpression(ProjectionToken token, string tableAlias, QueryBinder binder)
            {
                throw new NotImplementedException();
            }

            internal override Expression GenerateValue(ParameterExpression reader, ParameterExpression retriever)
            {
                throw new NotImplementedException();
            }
        }

        public Dictionary<string, IColumn> Columns { get; set; }
        public List<UniqueIndex> MultiIndexes { get; set; }

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
            Columns = new IColumn[] { PrimaryKey, BackReference}.Concat(Field.Columns()).ToDictionary(a => a.Name);

            int i = 0;
            foreach (var col in Columns.Values)
            {
                col.Position = i++;
            }

            CompleteRetrieve();
        }

        public List<UniqueIndex> GeneratUniqueIndexes()
        {
            var result = Field.GeneratUniqueIndexes(this).ToList();

            if (MultiIndexes != null)
                result.AddRange(MultiIndexes);

            return result; 
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
