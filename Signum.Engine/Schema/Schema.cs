using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities;
using System.Globalization;
using System.Diagnostics;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.DataStructures;
using System.Threading;
using System.Linq.Expressions;
using Signum.Entities.Reflection;
using System.Collections;
using Signum.Utilities.Reflection;
using Signum.Engine.Linq;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Basics;
using Signum.Engine.Basics;

namespace Signum.Engine.Maps
{
    public class Schema : IImplementationsFinder
    {
        public CultureInfo ForceCultureInfo { get; set; }

        public TimeZoneMode TimeZoneMode { get; set; }

        Version version;
        public Version Version
        {
            get
            {
                if (version == null)
                    throw new InvalidOperationException("Schema.Version is not set");

                return version;
            }
            set { this.version = value; }
        }

        string applicationName; 
        public string ApplicationName
        {
            get { return applicationName ?? (applicationName = AppDomain.CurrentDomain.FriendlyName); }
            set { applicationName = value; }
        }

        public SchemaSettings Settings { get; private set; }

        public SchemaAssets Assets { get; private set; }

        Dictionary<Type, Table> tables = new Dictionary<Type, Table>();
        public Dictionary<Type, Table> Tables
        {
            get { return tables; }
        }

        const string errorType = "TypeDN table not cached. Remember to call Schema.Current.Initialize";


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

        internal Dictionary<string, Type> NameToType = new Dictionary<string, Type>();
        internal Dictionary<Type, string> TypeToName = new Dictionary<Type, string>();

        internal ResetLazy<TypeCaches> typeCachesLazy;

        public void AssertAllowed(Type type)
        {
            string error = IsAllowed(type);

            if (error != null)
                throw new UnauthorizedAccessException(EngineMessage.UnauthorizedAccessTo0Because1.NiceToString().Formato(type.NiceName(), error));
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

        internal void OnRetrieved(IdentifiableEntity entity)
        {
            AssertAllowed(entity.GetType());

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnRetrieved(entity);

            entityEventsGlobal.OnRetrieved(entity);
        }

        internal void OnPreUnsafeDelete<T>(IQueryable<T> entityQuery) where T : IdentifiableEntity
        {
            AssertAllowed(typeof(T));

            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));

            if (ee != null)
                ee.OnPreUnsafeDelete(entityQuery);
        }

        internal void OnPreUnsafeMListDelete<T>(IQueryable mlistQuery, IQueryable<T> entityQuery) where T : IdentifiableEntity
        {
            AssertAllowed(typeof(T));

            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));

            if (ee != null)
                ee.OnPreUnsafeMListDelete(mlistQuery, entityQuery);
        }


        internal void OnPreUnsafeUpdate(IUpdateable update)
        {
            var type = update.EntityType;
            if (type.IsInstantiationOf(typeof(MListElement<,>)))
                type = type.GetGenericArguments().First();

            AssertAllowed(type);

            var ee = entityEvents.TryGetC(type);

            if (ee != null)
                ee.OnPreUnsafeUpdate(update);
        }

        internal void OnPreUnsafeInsert(Type type, IQueryable query, LambdaExpression constructor, IQueryable entityQuery)
        {
            AssertAllowed(type);

            var ee = entityEvents.TryGetC(type);

            if (ee != null)
                ee.OnPreUnsafeInsert(query, constructor, entityQuery);
        }

        public ICacheController CacheController(Type type)
        {
            IEntityEvents ee = entityEvents.TryGetC(type);

            if (ee == null)
                return null;

            return ee.CacheController;
        }

        internal CacheControllerBase<T> CacheController<T>() where T : IdentifiableEntity
        {
            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));

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
        internal SqlPreCommand SynchronizationScript(string databaseName, bool interactive = true)
        {
            if (Synchronizing == null)
                return null;

            using (Sync.ChangeBothCultures(ForceCultureInfo))
            using (ExecutionMode.Global())
            {
                Replacements replacements = new Replacements() { Interactive = interactive };
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
                            return new SqlPreCommandSimple("Exception on {0}.{1}: {2}".Formato(e.Method.DeclaringType.Name, e.Method.Name, ex.Message));
                        }
                    })
                    .Combine(Spacing.Triple);

                if (command == null)
                    return null;

                var replacementsComment = replacements.Interactive ? null : replacements.Select(r =>
                    SqlPreCommandConcat.Combine(Spacing.Double, new SqlPreCommandSimple("-- Replacements on {0}".Formato(r.Key)),
                        r.Value.Select(a => new SqlPreCommandSimple("--   {0} -> {1}".Formato(a.Key, a.Value))).Combine(Spacing.Simple)));

                return SqlPreCommand.Combine(Spacing.Double,
                    new SqlPreCommandSimple(SynchronizerMessage.StartOfSyncScriptGeneratedOn0.NiceToString().Formato(DateTime.Now)),

                    new SqlPreCommandSimple("use {0}".Formato(databaseName)),
                    command,
                    new SqlPreCommandSimple(SynchronizerMessage.EndOfSyncScript.NiceToString()));
            }
        }

        public event Func<SqlPreCommand> Generating;
        internal SqlPreCommand GenerationScipt()
        {
            if (Generating == null)
                return null;

            using (Sync.ChangeBothCultures(ForceCultureInfo))
            using (ExecutionMode.Global())
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
            Dictionary<InitLevel, Action> dict = new Dictionary<InitLevel, Action>();

            InitLevel? initLevel;

            public Action this[InitLevel level]
            {
                get { return dict.TryGetC(level); }
                set
                {
                    int current = dict.TryGetC(level).Try(d => d.GetInvocationList().Length) ?? 0;
                    int @new = value.Try(d => d.GetInvocationList().Length) ?? 0;

                    if (Math.Abs(current - @new) > 1)
                        throw new InvalidOperationException("add or remove just one event handler each time");

                    dict[level] = value;
                }
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
                using (HeavyProfiler.Log("InitializeJuts", () => currentLevel.ToString()))
                {
                    Action h = dict.TryGetC(currentLevel);
                    if (h == null)
                        return;

                    var handlers = h.GetInvocationList().Cast<Action>();

                    foreach (Action handler in handlers)
                    {
                        using (HeavyProfiler.Log("InitAction", () => "{0}.{1}".Formato(handler.Method.DeclaringType.TypeName(), handler.Method.MethodName())))
                        {
                            handler();
                        }
                    }
                }
            }

            public override string ToString()
            {
                return dict.OrderBy(a => a.Key)
                    .ToString(a => "{0} -> \r\n{1}".Formato(
                        a.Key,
                        a.Value.GetInvocationList().Select(h => h.Method).ToString(mi => "\t{0}.{1}".Formato(
                            mi.DeclaringType.TypeName(),
                            mi.MethodName()),
                        "\r\n")
                    ), "\r\n\r\n");
            }
        }

        public InitEventDictionary Initializing = new InitEventDictionary();

        public void Initialize()
        {
            using (ExecutionMode.Global())
                Initializing.InitializeUntil(InitLevel.Level4BackgroundProcesses);
        }

        public void InitializeUntil(InitLevel level)
        {
            using (ExecutionMode.Global())
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
            this.Assets = new SchemaAssets();

            Generating += SchemaGenerator.CreateSchemasScript;
            Generating += SchemaGenerator.CreateTablesScript;
            Generating += SchemaGenerator.InsertEnumValuesScript;
            Generating += TypeLogic.Schema_Generating;
            Generating += Assets.Schema_Generating;

            Synchronizing += SchemaSynchronizer.SnapshotIsolation;
            Synchronizing += SchemaSynchronizer.SynchronizeSchemasScript;
            Synchronizing += SchemaSynchronizer.SynchronizeTablesScript;
            Synchronizing += TypeLogic.Schema_Synchronizing;
            Synchronizing += Assets.Schema_Synchronizing;
        }

        public static Schema Current
        {
            get { return Connector.Current.Schema; }
        }

        public Table Table<T>() where T : IdentifiableEntity
        {
            return Table(typeof(T));
        }

        public Table Table(Type type)
        {
            return Tables.GetOrThrow(type, "Table {0} not loaded in schema");
        }

        internal static Field FindField(IFieldFinder fieldFinder, MemberInfo[] members)
        {
            IFieldFinder current = fieldFinder;
            Field result = null;
            foreach (var mi in members)
            {
                if (current == null)
                    throw new InvalidOperationException("{0} does not implement {1}".Formato(result, typeof(IFieldFinder).Name));

                result = current.GetField(mi);

                current = result as IFieldFinder;
            }

            return result;
        }

        internal static Field TryFindField(IFieldFinder fieldFinder, MemberInfo[] members)
        {
            IFieldFinder current = fieldFinder;
            Field result = null;
            foreach (var mi in members)
            {
                if (current == null)
                    return null;

                result = current.TryGetField(mi);

                if (result == null)
                    return null;

                current = result as IFieldFinder;
            }

            return result;
        }

        public Dictionary<PropertyRoute, Implementations> FindAllImplementations(Type root)
        {
            try
            {
                if (!Tables.ContainsKey(root))
                    return null;

                var table = Table(root);

                return PropertyRoute.GenerateRoutes(root)
                    .Select(r => r.Type.IsMList() ? r.Add("Item") : r)
                    .Where(r => r.Type.CleanType().IsIIdentifiable())
                    .ToDictionary(r => r, r => FindImplementations(r));
            }
            catch (Exception e)
            {
                e.Data["rootType"] = root.TypeName();
                throw;
            }
        }

        public Implementations FindImplementations(PropertyRoute route)
        {
            if (route.PropertyRouteType == PropertyRouteType.LiteEntity)
                route = route.Parent;

            Type type = route.RootType;

            if (!Tables.ContainsKey(type))
                return Implementations.By(route.Type.CleanType());

            Field field = TryFindField(Table(type), route.Members);
            //if (field == null)
            //    return Implementations.ByAll;

            FieldReference refField = field as FieldReference;
            if (refField != null)
                return Implementations.By(refField.FieldType.CleanType());

            FieldImplementedBy ibField = field as FieldImplementedBy;
            if (ibField != null)
                return Implementations.By(ibField.ImplementationColumns.Keys.ToArray());

            FieldImplementedByAll ibaField = field as FieldImplementedByAll;
            if (ibaField != null)
                return Implementations.ByAll;

            Implementations? implementations = CalculateExpressionImplementations(route);

            if (implementations != null)
                return implementations.Value;

            var ss = Schema.Current.Settings;
            if (route.Follow(r => r.Parent)
                .TakeWhile(t => t.PropertyRouteType != PropertyRouteType.Root)
                .SelectMany(r => ss.FieldAttributes(r))
                .Any(a => a is IgnoreAttribute))
            {
                var atts = ss.FieldAttributes(route);

                return Implementations.TryFromAttributes(route.Type.CleanType(), atts, route) ?? Implementations.By();
            }

            throw new InvalidOperationException("Impossible to determine implementations for {0}".Formato(route, typeof(IIdentifiable).Name));
        }

        private Implementations? CalculateExpressionImplementations(PropertyRoute route)
        {
            if (route.PropertyRouteType != PropertyRouteType.FieldOrProperty)
                return null;

            var lambda = ExpressionCleaner.GetFieldExpansion(route.Parent.Type, route.PropertyInfo);
            if (lambda == null)
                return null;

            Expression e = MetadataVisitor.JustVisit(lambda, new MetaExpression(route.Parent.Type, new CleanMeta(route.Parent.TryGetImplementations(), new[] { route.Parent })));

            MetaExpression me = e as MetaExpression;
            if (me == null)
                return null;

            return me.Meta.Implementations;
        }

        /// <summary>
        /// Uses a lambda navigate in a strongly-typed way, you can acces field using the property and collections using Single().
        /// </summary>
        public Field Field<T, V>(Expression<Func<T, V>> lambdaToField)
            where T : IdentifiableEntity
        {
            return FindField(Table(typeof(T)), Reflector.GetMemberList(lambdaToField));
        }

        public override string ToString()
        {
            return "Schema ( tables: {0} )".Formato(tables.Count);
        }

        public IEnumerable<ITable> GetDatabaseTables()
        {
            foreach (var table in Schema.Current.Tables.Values)
            {
                yield return table;

                foreach (var subTable in table.RelationalTables().Cast<ITable>())
                    yield return subTable;
            }
        }

        public List<DatabaseName> DatabaseNames()
        {
            return GetDatabaseTables().Select(a => a.Name.Schema.Try(s => s.Database)).Distinct().ToList();
        }

        public DirectedEdgedGraph<Table, RelationInfo> ToDirectedGraph()
        {
            return DirectedEdgedGraph<Table, RelationInfo>.Generate(Tables.Values, t => t.DependentTables());
        }

        public Type GetType(int id)
        {
            return typeCachesLazy.Value.IdToType[id];
        }

      
    }

    public class RelationInfo
    {
        public bool IsLite { get; set; }
        public bool IsNullable { get; set; }
        public bool IsCollection { get; set; }
        public bool IsEnum { get; set; }
    }

    internal interface IEntityEvents
    {
        void OnPreSaving(IdentifiableEntity entity, ref bool graphModified);
        void OnSaving(IdentifiableEntity entity);
        void OnRetrieved(IdentifiableEntity entity);

        void OnPreUnsafeUpdate(IUpdateable update);
        void OnPreUnsafeInsert(IQueryable query, LambdaExpression constructor, IQueryable entityQuery);

        ICacheController CacheController { get; }

        bool HasQueryFilter { get; }
    }

    public interface ICacheController
    {
        bool Enabled { get; }
        void Load();

        IEnumerable<int> GetAllIds();

        void Complete(IdentifiableEntity entity, IRetriever retriver);

        string GetToString(int id);
    }

    public class InvalidateEventArgs : EventArgs { }
    public class InvaludateEventArgs : EventArgs { }

    public abstract class CacheControllerBase<T> : ICacheController
        where T : IdentifiableEntity
    {
        public abstract bool Enabled { get; }
        public abstract void Load();

        public abstract IEnumerable<int> GetAllIds();

        void ICacheController.Complete(IdentifiableEntity entity, IRetriever retriver)
        {
            Complete((T)entity, retriver);
        }

        public abstract void Complete(T entity, IRetriever retriver);

        public abstract string GetToString(int id);
    }

    public class EntityEvents<T> : IEntityEvents
        where T : IdentifiableEntity
    {
        public event PreSavingEventHandler<T> PreSaving;
        public event SavingEventHandler<T> Saving;

        public event RetrievedEventHandler<T> Retrieved;

        public CacheControllerBase<T> CacheController { get; set; }

        public event FilterQueryEventHandler<T> FilterQuery;

        public event DeleteHandler<T> PreUnsafeDelete;
        public event DeleteMlistHandler<T> PreUnsafeMListDelete;

        public event UpdateHandler<T> PreUnsafeUpdate;

        public event InsertHandler<T> PreUnsafeInsert;

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

        internal void OnPreUnsafeDelete(IQueryable<T> entityQuery)
        {
            if (PreUnsafeDelete != null)
                foreach (DeleteHandler<T> action in PreUnsafeDelete.GetInvocationList().Reverse())
                    action(entityQuery);
        }

        internal void OnPreUnsafeMListDelete(IQueryable mlistQuery, IQueryable<T> entityQuery)
        {
            if (PreUnsafeMListDelete != null)
                foreach (DeleteMlistHandler<T> action in PreUnsafeMListDelete.GetInvocationList().Reverse())
                    action(mlistQuery, entityQuery);
        }

        void IEntityEvents.OnPreUnsafeUpdate(IUpdateable update)
        {
            if (PreUnsafeUpdate != null)
            {
                var query = update.EntityQuery<T>();
                foreach (UpdateHandler<T> action in PreUnsafeUpdate.GetInvocationList().Reverse())
                    action(update, query);
            }
        }

        void IEntityEvents.OnPreUnsafeInsert(IQueryable query, LambdaExpression constructor, IQueryable entityQuery)
        {
            if (PreUnsafeInsert != null)
                foreach (InsertHandler<T> action in PreUnsafeInsert.GetInvocationList().Reverse())
                    action(query, constructor, (IQueryable<T>)entityQuery);

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

        void IEntityEvents.OnRetrieved(IdentifiableEntity entity)
        {
            if (Retrieved != null)
                Retrieved((T)entity);
        }

        ICacheController IEntityEvents.CacheController
        {
            get { return CacheController; }
        }
    }

    public delegate void PreSavingEventHandler<T>(T ident, ref bool graphModified) where T : IdentifiableEntity;
    public delegate void RetrievedEventHandler<T>(T ident) where T : IdentifiableEntity;
    public delegate void SavingEventHandler<T>(T ident) where T : IdentifiableEntity;
    public delegate void SavedEventHandler<T>(T ident, SavedEventArgs args) where T : IdentifiableEntity;
    public delegate IQueryable<T> FilterQueryEventHandler<T>(IQueryable<T> query);

    public delegate void DeleteHandler<T>(IQueryable<T> entityQuery);
    public delegate void DeleteMlistHandler<T>(IQueryable mlistQuery, IQueryable<T> entityQuery);
    public delegate void UpdateHandler<T>(IUpdateable update, IQueryable<T> entityQuery);
    public delegate void InsertHandler<T>(IQueryable query, LambdaExpression constructor, IQueryable<T> entityQuery);

    public class SavedEventArgs
    {
        public bool IsRoot { get; set; }
        public bool WasNew { get; set; }
        public bool WasModified { get; set; }
    }


    public static class TableExtensions
    {
        internal static string UnScapeSql(this string name)
        {
            return name.Trim('[', ']');
        }
    }

    public class ServerName : IEquatable<ServerName>
    {
        public string Name { get; private set; }

        /// <summary>
        /// Linked Servers: http://msdn.microsoft.com/en-us/library/ms188279.aspx
        /// Not fully supported jet
        /// </summary>
        /// <param name="name"></param>
        public ServerName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            this.Name = name;
        }

        public override string ToString()
        {
            return Name.SqlEscape();
        }

        public bool Equals(ServerName other)
        {
            return other.Name == Name;
        }

        public override bool Equals(object obj)
        {
            var db = obj as ServerName;
            return db != null && Equals(db);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        internal static ServerName Parse(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return new ServerName(name.UnScapeSql());
        }
    }

    public class DatabaseName : IEquatable<DatabaseName>
    {
        public string Name { get; private set; }

        public ServerName Server { get; private set; }

        public DatabaseName(ServerName server, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            this.Name = name;
            this.Server = server;
        }

        public override string ToString()
        {
            var result = Name.SqlEscape();

            if (Server == null)
                return result;

            return Server.ToString() + "." + result;
        }

        public bool Equals(DatabaseName other)
        {
            return other.Name == Name &&
                object.Equals(Server, other.Server);
        }

        public override bool Equals(object obj)
        {
            var db = obj as DatabaseName;
            return db != null && Equals(db);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ (Server == null ? 0 : Server.GetHashCode());
        }

        internal static DatabaseName Parse(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return new DatabaseName(ServerName.Parse(name.TryBeforeLast('.')), (name.TryAfterLast('.') ?? name).UnScapeSql());
        }
    }

    public class SchemaName : IEquatable<SchemaName>
    {
        public string Name { get; private set; }

        readonly DatabaseName database;

        public DatabaseName Database
        {
            get
            {
                if (database == null || ObjectName.CurrentOptions.AvoidDatabaseName)
                    return null;

                return database;
            }
        }

        public static readonly SchemaName Default = new SchemaName(null, "dbo");

        public bool IsDefault()
        {
            return Name == "dbo" && Database == null;
        }

        public SchemaName(DatabaseName database, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            this.Name = name;
            this.database = database;
        }

        public override string ToString()
        {
            var result = Name.SqlEscape();

            if (Database == null)
                return result;

            return Database.ToString() + "." + result;
        }

        public bool Equals(SchemaName other)
        {
            return other.Name == Name &&
                object.Equals(Database, other.Database);
        }

        public override bool Equals(object obj)
        {
            var sc = obj as SchemaName;
            return sc != null && Equals(sc);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ (Database == null ? 0 : Database.GetHashCode());
        }

        internal static SchemaName Parse(string name)
        {
            if (string.IsNullOrEmpty(name))
                return SchemaName.Default;

            return new SchemaName(DatabaseName.Parse(name.TryBeforeLast('.')), (name.TryAfterLast('.') ?? name).UnScapeSql());
        }

    }

    public class ObjectName : IEquatable<ObjectName>
    {
        public string Name { get; private set; }

        public SchemaName Schema { get; private set; }

        public ObjectName(SchemaName schema, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (schema == null)
                throw new ArgumentNullException("schema");

            this.Name = name;
            this.Schema = schema;
        }

        public override string ToString()
        {
            if (Schema == null || Schema.IsDefault())
                return Name.SqlEscape();

            return Schema.ToString() + "." + Name.SqlEscape();
        }

        public string ToStringDbo()
        {
            if (Schema == null)
                return Name.SqlEscape();

            return Schema.ToString() + "." + Name.SqlEscape();
        }

        public bool Equals(ObjectName other)
        {
            return other.Name == Name &&
                object.Equals(Schema, other.Schema);
        }

        public override bool Equals(object obj)
        {
            var sc = obj as ObjectName;
            return sc != null && Equals(sc);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ (Schema == null ? 0 : Schema.GetHashCode());
        }

        internal static ObjectName Parse(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            return new ObjectName(SchemaName.Parse(name.TryBeforeLast('.')), (name.TryAfterLast('.') ?? name).UnScapeSql());
        }

        public ObjectName OnDatabase(DatabaseName databaseName)
        {
            return new ObjectName(new SchemaName(databaseName, Schema.Name), Name);
        }

        public ObjectName OnSchema(SchemaName schemaName)
        {
            return new ObjectName(schemaName, Name);
        }

        static readonly ThreadVariable<ObjectNameOptions> optionsVariable = Statics.ThreadVariable<ObjectNameOptions>("objectNameOptions");
        public static IDisposable OverrideOptions(ObjectNameOptions options)
        {
            var old = optionsVariable.Value;
            optionsVariable.Value = options;
            return new Disposable(() => optionsVariable.Value = old);
        }

        public static ObjectNameOptions CurrentOptions
        {
            get { return optionsVariable.Value; }
        }
    }

    public struct ObjectNameOptions
    {
        public bool IncludeDboSchema;
        public DatabaseName OverrideDatabaseNameOnSystemQueries;
        public bool AvoidDatabaseName;
    }
}
