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
    public class Schema : IImplementationsFinder
    {
        bool silentMode = false;
        public bool SilentMode
        {
            get { return silentMode; }
            set { this.silentMode = value; }
        }

        public CultureInfo ForceCultureInfo { get; set; }

        public TimeZoneMode TimeZoneMode { get; set; }

        public SchemaSettings Settings { get; private set; }

        Dictionary<Type, Table> tables = new Dictionary<Type, Table>();
        public Dictionary<Type, Table> Tables
        {
            get { return tables; }
        }

        const string errorType = "TypeDN table not cached. Remember to call Schema.Current.Initialize";

        Dictionary<Type, int> typeToId;
        internal Dictionary<Type, int> TypeToId
        {
            get { return typeToId.ThrowIfNullC(errorType); }
            set { typeToId = value; }
        }

        Dictionary<int, Type> idToType;
        internal Dictionary<int, Type> IdToType
        {
            get { return idToType.ThrowIfNullC(errorType); }
            set { idToType = value; }
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

        Dictionary<string, Type> nameToType = new Dictionary<string,Type>();
        internal Dictionary<string, Type> NameToType
        {
            get { return nameToType; }
            set { nameToType = value; }
        }

        Dictionary<Type, string> typeToName = new Dictionary<Type,string>();
        internal Dictionary<Type, string> TypeToName
        {
            get { return typeToName; }
            set { typeToName = value; }
        }

        internal  Type GetType(int id)
        {
            return this.idToType[id]; 
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

        Dictionary<Type, IEntityEvents> entityEvents = new Dictionary<Type, IEntityEvents>();
        public EntityEvents<T> EntityEvents<T>()
            where T : IdentifiableEntity
        {
            return (EntityEvents<T>)entityEvents.GetOrCreate(typeof(T), () => new EntityEvents<T>());
        }

        internal void OnPreSaving(IdentifiableEntity entity, ref bool graphModified)
        {
            AssertAllowed(entity.GetType());

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnPreSaving(entity, ref graphModified);

            entityEventsGlobal.OnPreSaving(entity, ref graphModified);
        }

        internal void OnSaving(IdentifiableEntity entity)
        {
            AssertAllowed(entity.GetType());

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnSaving(entity);

            entityEventsGlobal.OnSaving(entity);
        }

        internal void OnRetrieved(IdentifiableEntity entity, bool fromCache)
        {
            AssertAllowed(entity.GetType());

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnRetrieved(entity, fromCache);

            entityEventsGlobal.OnRetrieved(entity, fromCache);
        }

        internal ICacheController CacheController(Type type)
        {
            IEntityEvents ee = entityEvents.TryGetC(type);

            if (ee == null)
                return null;

            return ee.CacheController;
        }

        internal CacheController<T> CacheController<T>() where T:IdentifiableEntity
        {
            EntityEvents<T> ee =  (EntityEvents<T>)entityEvents.TryGetC(typeof(T));

            if (ee == null)
                return null;

            return ee.CacheController;
        }

        internal IQueryable<T> OnFilterQuery<T>(IQueryable<T> query)
            where T : IdentifiableEntity
        {
            AssertAllowed(typeof(T));

            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));
            if (ee == null)
                return query;

            return ee.OnFilterQuery(query);
        }

        internal bool HasQueryFilter(Type type)
        {
            IEntityEvents ee = entityEvents.TryGetC(type);
            if (ee == null)
                return false;

            return ee.HasQueryFilter;
        }

        public event Func<Replacements, SqlPreCommand> Synchronizing;
        internal SqlPreCommand SynchronizationScript(string schemaName)
        {
            if (Synchronizing == null)
                return null;

            using (Sync.ChangeBothCultures(ForceCultureInfo))
            {
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
        }

        public event Func<SqlPreCommand> Generating;
        internal SqlPreCommand GenerationScipt()
        {
            if (Generating == null)
                return null;

            using (Sync.ChangeBothCultures(ForceCultureInfo))
            {
                return Generating
                    .GetInvocationList()
                    .Cast<Func<SqlPreCommand>>()
                    .Select(e => e())
                    .Combine(Spacing.Triple);
            }
        }

        public class InitEventDictionary
        {
            Dictionary<InitLevel, InitEventHandler> dict = new Dictionary<InitLevel, InitEventHandler>();
            
            Dictionary<MethodInfo, long> times = new Dictionary<MethodInfo, long>(); 

            InitLevel? initLevel;

            public InitEventHandler this[InitLevel level]
            {
                get { return dict.TryGetC(level);}
                set {
                    int current = dict.TryGetC(level).TryCS(d => d.GetInvocationList().Length) ?? 0;
                    int @new = value.TryCS(d => d.GetInvocationList().Length) ?? 0;

                    if (Math.Abs(current - @new) > 1)
                        throw new InvalidOperationException("add or remove just one event handler each time"); 
                    
                    dict[level] = value; }
            }

            public void InitializeUntil(InitLevel topLevel)
            {
                for (InitLevel current = initLevel + 1 ?? InitLevel.Level0SyncEntities; current <= topLevel; current++)
                {
                    InitializeJust(current);
                    initLevel = current;
                }
            }

            void InitializeJust(InitLevel currentLevel)
            {
                InitEventHandler h = dict.TryGetC(currentLevel);
                if (h == null)
                    return;

                var handlers = h.GetInvocationList().Cast<InitEventHandler>();

                foreach (InitEventHandler handler in handlers)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    handler();
                    sw.Stop();
                    times.Add(handler.Method, sw.ElapsedMilliseconds);
                }
            }

            public override string ToString()
            {
                return dict.OrderBy(a => a.Key).ToString(a => "{0} -> \r\n{1}".Formato(a.Key,
                    a.Value.GetInvocationList().Select(h => h.Method).ToString(mi =>
                        "\t{0}.{1}: {2}".Formato(mi.DeclaringType.TypeName(), mi.MethodName(), times.TryGetS(mi).TryToString("0 ms") ?? "Not Initialized"), "\r\n")), "\r\n\r\n");
            }
        }

        public InitEventDictionary Initializing = new InitEventDictionary();

        public void Initialize()
        {
            using (GlobalMode())
                Initializing.InitializeUntil(InitLevel.Level4BackgroundProcesses);
        }

        public void InitializeUntil(InitLevel level)
        {
            using (GlobalMode())
                Initializing.InitializeUntil(level);
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

            Initializing[InitLevel.Level0SyncEntities] += TypeLogic.Schema_Initializing;
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

        internal static Field FindField(IFieldFinder fieldFinder, MemberInfo[] members, bool throws)
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
            Type type = route.RootType;

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
            return tables.Values.ToString(t => t.Type.TypeName(), "\r\n\r\n");
        }

        internal Dictionary<string, ITable> GetDatabaseTables()
        {
            return Schema.Current.Tables.Values.SelectMany(t =>
                t.Fields.Values.Select(a => a.Field).OfType<FieldMList>().Select(f => (ITable)f.RelationalTable).PreAnd(t))
                .ToDictionary(a => a.Name);
        }

        public DirectedEdgedGraph<Table, bool> ToDirectedGraph()
        {
            return DirectedEdgedGraph<Table, bool>.Generate(Tables.Values, t => t.Fields.Values.SelectMany(f => f.Field.GetTables()));
        }

        ThreadLocal<bool> inGlobalMode = new ThreadLocal<bool>(() => false);
        public bool InGlobalMode
        {
            get { return inGlobalMode.Value; }
        }

        internal IDisposable GlobalMode()
        {
            inGlobalMode.Value = true;
            return new Disposable(() => inGlobalMode.Value = false);
        }
    }

    internal interface IEntityEvents
    {
        void OnPreSaving(IdentifiableEntity entity, ref bool graphModified);
        void OnSaving(IdentifiableEntity entity);
        void OnRetrieved(IdentifiableEntity entity, bool fromCache);


        ICacheController CacheController { get; }

        bool HasQueryFilter { get; }
    }

    public interface ICacheController
    {
        bool Enabled { get; }
        IdentifiableEntity GetEntity(int id);
        IList GetAllEntities();
        IList GetEntitiesList(List<int> ids);
        IdentifiableEntity GetOrRequest(int id);
        bool Load();
    }

    public abstract class CacheController<T> : ICacheController where T : IdentifiableEntity
    {
        public abstract bool Enabled { get; }
        public abstract bool Load();
        public abstract T GetOrRequest(int id); 
        public abstract T GetEntity(int id);
        public abstract List<T> GetAllEntities();
        public abstract List<T> GetEntitiesList(List<int> list);

        IdentifiableEntity ICacheController.GetEntity(int id)
        {
            return GetEntity(id);
        }

        IdentifiableEntity ICacheController.GetOrRequest(int id)
        {
            return GetOrRequest(id);
        }

        IList ICacheController.GetAllEntities()
        {
            return GetAllEntities();
        }

        IList ICacheController.GetEntitiesList(List<int> ids)
        {
            return GetEntitiesList(ids);
        }
    }

    public class EntityEvents<T> : IEntityEvents
        where T : IdentifiableEntity
    {
        public event PreSavingEventHandler<T> PreSaving;
        public event SavingEventHandler<T> Saving;

        public event RetrievedEventHandler<T> Retrieved;

        public CacheController<T> CacheController { get; set; }

        public event Func<bool> HasCache;

        public event FilterQueryEventHandler<T> FilterQuery;

        public event PreUnsafeDeleteHandler<T> PreUnsafeDelete;

        internal IQueryable<T> OnFilterQuery(IQueryable<T> query)
        {
            if (FilterQuery != null)
                foreach (FilterQueryEventHandler<T> filter in FilterQuery.GetInvocationList())
                    query = filter(query);

            return query;
        }

        public bool HasQueryFilter
        {
            get { return FilterQuery != null; }
        }

        internal void OnPreUnsafeDelete(IQueryable<T> query)
        {
            if (PreUnsafeDelete != null)
                foreach (PreUnsafeDeleteHandler<T> action in PreUnsafeDelete.GetInvocationList().Reverse())
                    action(query);
        }

        void IEntityEvents.OnPreSaving(IdentifiableEntity entity, ref bool graphModified)
        {
            if (PreSaving != null)
                PreSaving((T)entity, ref graphModified);
        }

        void IEntityEvents.OnSaving(IdentifiableEntity entity)
        {
            if (Saving != null)
                Saving((T)entity);

        }

        void IEntityEvents.OnRetrieved(IdentifiableEntity entity, bool fromCache)
        {
            if (Retrieved != null)
                Retrieved((T)entity, fromCache);
        }

        ICacheController IEntityEvents.CacheController
        {
            get { return CacheController; }
        }
    }

    public delegate void PreSavingEventHandler<T>(T ident, ref bool graphModified) where T : IdentifiableEntity;
    public delegate void RetrievedEventHandler<T>(T ident, bool fromCache) where T : IdentifiableEntity;    
    public delegate void SavingEventHandler<T>(T ident) where T : IdentifiableEntity;
    public delegate void SavedEventHandler<T>(T ident, SavedEventArgs args) where T : IdentifiableEntity;
    public delegate IQueryable<T> FilterQueryEventHandler<T>(IQueryable<T> query);

    public delegate void PreUnsafeDeleteHandler<T>(IQueryable<T> query);

    public delegate void InitEventHandler();
    public delegate void SyncEventHandler();
    public delegate void GenSchemaEventHandler(); 

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

            if (fields == null || fields.IsEmpty())
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

            if (columns == null || columns.IsEmpty())
                throw new ArgumentNullException("columns");

            this.Table = table;
            this.Columns = columns;
        }

        public string IndexName
        {
            get { return "IX_{0}_{1}".Formato(Table.Name, ColumnSignature()).Left(ConnectionScope.Current.MaxNameLength, false); }
        }

        public string ViewName
        {
            get
            {
                if (string.IsNullOrEmpty(Where) || ConnectionScope.Current.DBMS != DBMS.SqlServer2005)
                    return null;

                return "VIX_{0}_{1}".Formato(Table.Name, ColumnSignature()).Left(ConnectionScope.Current.MaxNameLength, false);
            }
        }

        string ColumnSignature()
        {
            string columns = Columns.ToString(c => c.Name, "_");
            if (string.IsNullOrEmpty(Where))
                return columns;

            return columns + "__" + StringHashEncoder.Codify(Where);
        }

        public UniqueIndex WhereNotNull(params IColumn[] notNullColumns)
        {
            if (notNullColumns == null || notNullColumns.IsEmpty())
            {
                Where = null;
                return this;
            }

            this.Where = notNullColumns.ToString(c => {
                string res = c.Name.SqlScape() + " IS NOT NULL";
                if (!IsString(c.SqlDbType))
                    return res;

                return res + " AND " + c.Name.SqlScape() + " <> ''";

            }, " AND ");
            return this;
        }

        private bool IsString(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.VarChar:
                    return true;
            }

            return false;
        }

        public UniqueIndex WhereNotNull(params Field[] notNullFields)
        {
            if (notNullFields == null || notNullFields.IsEmpty())
            {
                Where = null;
                return this;
            }

            if (notNullFields.OfType<FieldEmbedded>().Any())
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
                        inserter.ResetPublicationOnly();
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
            return "[{0}] ({1})\r\n{2}".Formato(Name, Type.TypeName(), Fields.ToString(c => "{0} : {1}".Formato(c.Key, c.Value), "\r\n").Indent(2));
        }

        public void GenerateColumns()
        {
            Columns = Fields.Values.SelectMany(c => c.Field.Columns()).ToDictionary(c => c.Name);

            inserter = new Lazy<InsertCache>(InitializeInsert, LazyThreadSafetyMode.PublicationOnly);
            updater = new Lazy<UpdateCache>(InitializeUpdate, LazyThreadSafetyMode.None);
            saveCollections = new Lazy<Action<IdentifiableEntity,Forbidden,bool>>(InitializeCollections, LazyThreadSafetyMode.None);
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

        internal void InsertMany(List<IdentifiableEntity> list, DirectedGraph<IdentifiableEntity> graph)
        {
            var ic = inserter.Value;

            foreach (var ls in list.Split_1_2_4_8_16())
            {
                switch (ls.Count)
                {
                    case 1: ic.Insert(ls[0], graph); break;
                    case 2: ic.Insert2(ls, graph); break;
                    case 4: ic.Insert4(ls, graph); break;
                    case 8: ic.Insert8(ls, graph); break;
                    case 16: ic.Insert16(ls, graph); break;
                }
            }
        }

        internal void UpdateMany(List<IdentifiableEntity> list, DirectedGraph<IdentifiableEntity> graph)
        {
            var uc = updater.Value;

            foreach (var ls in list.Split_1_2_4_8_16())
            {
                switch (ls.Count)
                {
                    case 1: uc.Update(ls[0], graph); break;
                    case 2: uc.Update2(ls, graph); break;
                    case 4: uc.Update4(ls, graph); break;
                    case 8: uc.Update8(ls, graph); break;
                    case 16: uc.Update16(ls, graph); break;
                }
            }
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
    }

    public partial class FieldEnum : FieldReference
    {
        public FieldEnum(Type fieldType) : base(fieldType) { }
    }

    public partial class FieldImplementedBy : Field, IFieldReference
    {
        public bool IsLite { get; set; }

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

        internal override IEnumerable<KeyValuePair<Table, bool>> GetTables()
        {
            return Enumerable.Empty<KeyValuePair<Table, bool>>();
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
            if (value is PropertyInfo && value.Name == "Item")
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

        internal override IEnumerable<KeyValuePair<Table, bool>> GetTables()
        {
            return RelationalTable.Field.GetTables();
        }
    }

    public partial class RelationalTable : ITable, IFieldFinder
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

        public Field GetField(MemberInfo value, bool throws)
        {
            if (value.Name == "Parent")
                return this.BackReference;

            if (value.Name == "Element")
                return this.Field;

            if (throws)
                throw new InvalidOperationException("'{0}' not found".Formato(value.Name));

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
