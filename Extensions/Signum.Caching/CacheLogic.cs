using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Drawing;
using System.Xml.Linq;
using Signum.Engine.Linq;
using System.IO;
using System.Data;
using System.Runtime.InteropServices;
using Signum.Engine.Sync;
using Microsoft.Data.SqlClient;
using Signum.Engine.Sync.SqlServer;
using Signum.API;
using Signum.Authorization;
using Signum.Authorization.Rules;
using DocumentFormat.OpenXml.Office2013.Drawing.Chart;

namespace Signum.Cache;

public interface IServerBroadcast
{
    bool Running { get; }
    void StartIfNecessary();
    void Send(string methodName, string argument);
    event Action<string, string>? Receive;
}

public static class CacheLogic
{
    public static IServerBroadcast? ServerBroadcast { get; private set; }

    public static bool WithSqlDependency { get; internal set; }

    public static bool DropStaleServices = true;

    public static FluentInclude<T> WithCache<T>(this FluentInclude<T> fi)
      where T : Entity
    {
        CacheLogic.TryCacheTable(fi.SchemaBuilder, typeof(T));
        return fi;
    }

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null!, null, null)));
    }

    /// <summary>
    /// If you have invalidation problems look at exceptions in: select * from sys.transmission_queue
    /// If there are exceptions like: 'Could not obtain information about Windows NT group/user'
    ///    Change login to a SqlServer authentication (i.e.: sa)
    ///    Change Server Authentication mode and enable SA: http://msdn.microsoft.com/en-us/library/ms188670.aspx
    ///    Change Database ownership to sa: ALTER AUTHORIZATION ON DATABASE::yourDatabase TO sa
    /// </summary>
    public static void Start(SchemaBuilder sb, bool? withSqlDependency = null, IServerBroadcast? serverBroadcast = null)
    {
        if (sb.WebServerBuilder != null)
            CacheServer.Start(sb.WebServerBuilder);
        
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            PermissionLogic.RegisterTypes(typeof(CachePermission));

            sb.SwitchGlobalLazyManager(new CacheGlobalLazyManager());

            if (withSqlDependency == true && !Connector.Current.SupportsSqlDependency)
                throw new InvalidOperationException("Sql Dependency is not supported by the current connection");

            WithSqlDependency = withSqlDependency ?? Connector.Current.SupportsSqlDependency;

            if (serverBroadcast != null && WithSqlDependency)
                throw new InvalidOperationException("cacheInvalidator is only necessary if SqlDependency is not enabled");

            ServerBroadcast = serverBroadcast;
            if(ServerBroadcast != null)
            {
                ServerBroadcast!.Receive += ServerBroadcast_Receive;
                sb.Schema.BeforeDatabaseAccess += () => ServerBroadcast!.StartIfNecessary();
            }

            sb.Schema.SchemaCompleted += () => Schema_SchemaCompleted(sb);
            sb.Schema.BeforeDatabaseAccess += StartSqlDependencyAndEnableBrocker;
            sb.Schema.OnInvalidateCache += () => CacheLogic.ForceReset();

            GlobalLazy.OnResetAll += systemLog => CacheLogic.ForceReset(systemLog);
        }
    }

    static void Schema_SchemaCompleted(SchemaBuilder sb)
    {
        foreach (var kvp in VirtualMList.RegisteredVirtualMLists)
        {
            var type = kvp.Key;

            if (controllers.TryGetCN(type) != null)
            {
                foreach (var vml in kvp.Value)
                {
                    var rType = vml.Value.BackReferenceRoute.RootType;

                    EntityData data = EntityDataOverrides.TryGetS(rType) ?? EntityKindCache.GetEntityData(rType);

                    if (data == EntityData.Transactional)
                        throw new InvalidOperationException($"Type {rType.Name} should be {nameof(EntityData)}.{nameof(EntityData.Master)} because is in a virtual MList of {type.Name} (Master and cached)");

                    TryCacheTable(sb, rType);

                    dependencies.Add(type, rType);
                    inverseDependencies.Add(rType, type);
                }
            }
        }

        foreach (var cont in controllers.Values.NotNull())
        {
            cont.BuildCachedTable();
        }

        foreach (var cont in controllers.Values.NotNull())
        {
            cont.CachedTable.SchemaCompleted();
        }

        var missingInMemory = (from t in TypeConditionLogic.Types
                               where controllers.TryGetCN(t) != null || semiControllers.TryGetC(t).EmptyIfNull().Any(a => a.Type.IsEntity())
                               from tc in TypeConditionLogic.ConditionsFor(t)
                               where !TypeConditionLogic.HasInMemoryCondition(t, tc)
                               select new { t, tc }).ToList();

        if (missingInMemory.Any())
            throw new InvalidOperationException("The following types are cached, but do not have in-memory TypeConditions defined: " +
                missingInMemory.GroupBy(a => a.t, a => a.tc).ToString(gr => $" * {gr.Key.TypeName()} ({EntityKindCache.GetEntityData(gr.Key)}): {gr.ToString(a => a.Key, ", ")}", "\n")
                );
    }

    static void ServerBroadcast_Receive(string methodName, string argument)
    {
        BroadcastReceivers.TryGetC(methodName)?.Invoke(argument);
    }

    public static Dictionary<string /*methodName*/, Action<string /*argument*/>> BroadcastReceivers = new Dictionary<string, Action<string>>
    {
        { Method_InvalidateTable, ServerBroadcast_InvalidateTable },
        { Method_InvalidateAllTable, ServerBroadcast_InvalidateAllTables },
    };

    static void ServerBroadcast_InvalidateAllTables(string args)
    {
        CacheLogic.InvalidateAll(systemLog: args == "nodb" ? false : true);
    }

    static void ServerBroadcast_InvalidateTable(string cleanName)
    {
        Type type = TypeLogic.GetType(cleanName);

        var c = controllers.GetOrThrow(type);

        if (c != null)
        {
            c.CachedTable.ResetAll(forceReset: false);
            c.NotifyInvalidated();
        }
        else
        {
            var list = CacheLogic.semiControllers.GetOrThrow(type);
            foreach (var sc in list)
            {
                sc.ResetAll(forceReset: false);
                sc.controller.NotifyInvalidated();
            }
        }
    }

    public static TextWriter? LogWriter;
    public static List<T> ToListWithInvalidation<T>(this IQueryable<T> simpleQuery, Type type, string exceptionContext, Action<SqlNotificationEventArgs> invalidation)
    {
        if (!WithSqlDependency)
            throw new InvalidOperationException("ToListWithInvalidation requires SqlDependency");

        ITranslateResult tr;
        using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
            tr = ((DbQueryProvider)simpleQuery.Provider).GetRawTranslateResult(simpleQuery.Expression);

        void onChange(object sender, SqlNotificationEventArgs args)
        {
            try
            {
                if (args.Type != SqlNotificationType.Change)
                    throw new InvalidOperationException(
                        "Problems with SqlDependency (Type : {0} Source : {1} Info : {2}) on query: \n{3}"
                        .FormatWith(args.Type, args.Source, args.Info, tr.MainCommand.PlainSql()));

                if (args.Info == SqlNotificationInfo.PreviousFire)
                    throw new InvalidOperationException("The same transaction that loaded the data is invalidating it!") { Data = { { "query", tr.MainCommand.PlainSql() } } };

                if (CacheLogic.LogWriter != null)
                    CacheLogic.LogWriter.WriteLine("Change ToListWithInvalidations {0} {1}".FormatWith(typeof(T).TypeName()), exceptionContext);

                invalidation(args);
            }
            catch (Exception e)
            {
                e.LogException(c => c.ControllerName = exceptionContext);
            }
        }

        SimpleReader? reader = null;

        Expression<Func<IProjectionRow, T>> projectorExpression = (Expression<Func<IProjectionRow, T>>)tr.GetMainProjector();
        Func<IProjectionRow, T> projector = projectorExpression.Compile();

        List<T> list = new List<T>();

        CacheLogic.AssertSqlDependencyStarted();

        Table table = Schema.Current.Table(type);
        DatabaseName? db = table.Name.Schema?.Database;

        SqlServerConnector subConnector = (SqlServerConnector)Connector.Current.ForDatabase(db);

        if (CacheLogic.LogWriter != null)
            CacheLogic.LogWriter.WriteLine("Load ToListWithInvalidations {0} {1}".FormatWith(typeof(T).TypeName()), exceptionContext);

        using (new EntityCache())
        using (var r = EntityCache.NewRetriever())
        {
            subConnector.ExecuteDataReaderDependency(tr.MainCommand, onChange, StartSqlDependencyAndEnableBrocker, fr =>
            {
                if (reader == null)
                    reader = new SimpleReader(fr, r);

                list.Add(projector(reader));
            }, CommandType.Text);

            r.CompleteAll();
        }

        return list;
    }

    public static void ExecuteDataReaderOptionalDependency(this Connector connector, SqlPreCommandSimple preCommand, OnChangeEventHandler change, Action<FieldReader> forEach)
    {
        if (WithSqlDependency)
        {
            ((SqlServerConnector)connector).ExecuteDataReaderDependency(preCommand, change, StartSqlDependencyAndEnableBrocker, forEach, CommandType.Text);
        }
        else
        {
            using (var p = preCommand.UnsafeExecuteDataReader())
            {
                FieldReader reader = new FieldReader(p.Reader);
                while (p.Reader.Read())
                    forEach(reader);
            }
        }
    }

    class SimpleReader : IProjectionRow
    {
        FieldReader reader;
        IRetriever retriever;

        public SimpleReader(FieldReader reader, IRetriever retriever)
        {
            this.reader = reader;
            this.retriever = retriever;
        }

        public FieldReader Reader
        {
            get { return reader; }
        }

        public IRetriever Retriever
        {
            get { return retriever; }
        }

        public IEnumerable<S> Lookup<K, S>(LookupToken token, K key)
        {
            throw new InvalidOperationException("Subqueries can not be used on simple queries");
        }

        public MList<S> LookupRequest<K, S>(LookupToken token, K key, MList<S> field)
            where K : notnull
        {
            throw new InvalidOperationException("Subqueries can not be used on simple queries");
        }
    }

    static bool started = false;
    internal static void AssertSqlDependencyStarted()
    {
        if (GloballyDisabled)
            return;

        if (!started)
            throw new InvalidOperationException("call Schema.Current.OnBeforeDatabaseAccess() or Schema.Current.Initialize()  before any database access");
    }


    readonly static object startKeyLock = new object();
    public static void StartSqlDependencyAndEnableBrocker()
    {
        if (!WithSqlDependency)
        {
            started = true;
            return;
        }

        lock (startKeyLock)
        {
            SqlServerConnector connector = (SqlServerConnector)Connector.Current;
            bool isPostgree = false;

            if (DropStaleServices)
            {
                //to avoid massive logs with SqlQueryNotificationStoredProcedure
                //http://rusanu.com/2007/11/10/when-it-rains-it-pours/
                var staleServices = (from s in Database.View<SysServiceQueues>()
                                     where s.activation_procedure != null && !Database.View<SysProcedures>().Any(p => "[" + p.Schema().name + "].[" + p.name + "]" == s.activation_procedure)
                                     select new ObjectName(new SchemaName(null, s.Schema().name, isPostgree), s.name, isPostgree)).ToList();

                foreach (var s in staleServices)
                {
                    TryDropService(s.Name);
                    TryDropQueue(s);
                }

                var oldProcedures = (from p in Database.View<SysProcedures>()
                                     where p.name.Contains("SqlQueryNotificationStoredProcedure-") && !Database.View<SysServiceQueues>().Any(s => "[" + p.Schema().name + "].[" + p.name + "]" == s.activation_procedure)
                                     select new ObjectName(new SchemaName(null, p.Schema().name, isPostgree), p.name, isPostgree)).ToList();

                foreach (var item in oldProcedures)
                {
                    try
                    {
                        Executor.ExecuteNonQuery(new SqlPreCommandSimple($"DROP PROCEDURE {item}"));

                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number != 15151)
                            throw;
                    }
                }

            }

            foreach (var database in Schema.Current.DatabaseNames())
            {
                SqlServerConnector sub = (SqlServerConnector)connector.ForDatabase(database);

                try
                {
                    try
                    {
                        //InvalidOperationException throw?...just continue the catch will probably fix it!
                        SqlDependency.Start(sub.ConnectionString);
                    }
                    catch (InvalidOperationException ex)
                    {
                        string databaseName = database?.Name ?? Connector.Current.DatabaseName();

                        if (ex.Message.Contains("SQL Server Service Broker"))
                        {
                            EnableOrCreateBrocker(databaseName);

                            SqlDependency.Start(sub.ConnectionString);
                        }
                    }
                }
                catch (SqlException e)
                {
                    if (e.Number == 2797)
                    {
                        string currentUser = (string)Executor.ExecuteDataTable("SELECT CURRENT_USER").Rows[0][0];

                        Executor.ExecuteNonQuery("ALTER USER [{0}]  WITH DEFAULT_SCHEMA = dbo;".FormatWith(currentUser));

                        SqlDependency.Start(sub.ConnectionString);
                    }
                    else throw;
                }
            }

            RegisterOnShutdown();

            started = true;
        }
    }

    private static void TryDropService(string s)
    {
        try
        {
            using (var con = (SqlConnection)Connector.Current.CreateConnection())
            {
                con.Open();
                new SqlCommand("DROP SERVICE [{0}]".FormatWith(s), con).ExecuteNonQuery();
            }
        }
        catch (SqlException ex)
        {
            if (ex.Number != 15151)
                throw;
        }
    }

    private static void TryDropQueue(ObjectName s)
    {
        try
        {
            using (var con = (SqlConnection)Connector.Current.CreateConnection())
            {
                con.Open();
                new SqlCommand("DROP QUEUE {0}".FormatWith(s), con).ExecuteNonQuery();
            }
        }
        catch (SqlException ex)
        {
            if (ex.Number != 15151)
                throw;
        }
    }

    static void EnableOrCreateBrocker(string databaseName)
    {
        try
        {
            using (var tr = Transaction.None())
            {
                Executor.ExecuteNonQuery("ALTER DATABASE [{0}] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;".FormatWith(databaseName));

                tr.Commit();
            }
        }
        catch (SqlException)
        {
            using (var tr = Transaction.None())
            {
                Executor.ExecuteNonQuery("ALTER DATABASE [{0}] SET NEW_BROKER WITH ROLLBACK IMMEDIATE;".FormatWith(databaseName));

                tr.Commit();
            }
        }
    }

    static bool registered = false;
    static void RegisterOnShutdown()
    {
        if (registered)
            return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SafeConsole.SetConsoleCtrlHandler(ct =>
            {
                Shutdown();
                return true;
            }, true);
        }

        AppDomain.CurrentDomain.DomainUnload += (o, a) => Shutdown();

        registered = true;
    }

    public static void Shutdown()
    {
        if (GloballyDisabled || !WithSqlDependency)
            return;

        var connector = ((SqlServerConnector)Connector.Current);
        foreach (var database in Schema.Current.DatabaseNames())
        {
            SqlServerConnector sub = (SqlServerConnector)connector.ForDatabase(database);

            SqlDependency.Stop(sub.ConnectionString);
        }

    }

    class CacheController<T> : CacheControllerBase<T>, ICacheLogicController
            where T : Entity
    {
        public CachedTable<T> cachedTable = null!;
        public CachedTableBase CachedTable { get { return cachedTable; } }

        public CacheController(Schema schema)
        {
            var ee = schema.EntityEvents<T>();

            ee.CacheController = this;
            ee.Saving += ident =>
            {
                if (ident.IsGraphModified)
                    DisableAndInvalidate(withUpdates: true); //Even if new, loading the cache afterwars will Timeout
            };
            ee.PreUnsafeDelete += query => { DisableAndInvalidate(withUpdates: false); return null; };
            ee.PreUnsafeUpdate += (update, entityQuery) => { DisableAndInvalidate(withUpdates: true); return null; };
            ee.PreUnsafeInsert += (query, constructor, entityQuery) => { DisableAndInvalidate(withUpdates: constructor.Body.Type.IsInstantiationOf(typeof(MListElement<,>))); return constructor; };
            ee.PreUnsafeMListDelete += (mlistQuery, entityQuery) => { DisableAndInvalidate(withUpdates: true); return null; };
            ee.PreBulkInsert += inMListTable => DisableAndInvalidate(withUpdates: inMListTable);
        }

        public void BuildCachedTable()
        {
            cachedTable = new CachedTable<T>(this, new AliasGenerator(), null, null);
        }

        private void DisableAndInvalidate(bool withUpdates)
        {
            if (!withUpdates)
            {
                DisableTypeInTransaction(typeof(T));
            }
            else
            {
                DisableAllConnectedTypesInTransaction(typeof(T));
            }

            Transaction.PostRealCommit -= Transaction_PostRealCommit;
            Transaction.PostRealCommit += Transaction_PostRealCommit;
        }

        void Transaction_PostRealCommit(Dictionary<string, object> obj)
        {
            cachedTable.ResetAll(forceReset: false);
            NotifyInvalidateAllConnectedTypes(typeof(T));
        }

        public override bool Enabled
        {
            get { return !GloballyDisabled && !ExecutionMode.IsCacheDisabled && !IsDisabledInTransaction(typeof(T)) && SystemTime.Current == null; }
        }

        private void AssertEnabled()
        {
            if (!Enabled)
                throw new InvalidOperationException("Cache for {0} is not enabled".FormatWith(typeof(T).TypeName()));
        }

        public event EventHandler<CacheEventArgs>? Invalidated;

        public void OnChange(object sender, SqlNotificationEventArgs args)
        {
            NotifyInvalidateAllConnectedTypes(typeof(T));
        }

        public override void Load()
        {
            LoadAllConnectedTypes(typeof(T));
        }

        public void ForceReset()
        {
            cachedTable.ResetAll(forceReset: true);
        }

        public override IEnumerable<PrimaryKey> GetAllIds()
        {
            AssertEnabled();

            return cachedTable.GetAllIds();
        }

        public override object GetLiteModel(PrimaryKey id, Type modelType, IRetriever retriever)
        {
            AssertEnabled();

            return cachedTable.GetLiteModel(id, modelType, retriever);
        }

        public override object? TryGetLiteModel(PrimaryKey? id, Type modelType, IRetriever retriever)
        {
            AssertEnabled();

            return cachedTable.TryGetLiteModel(id!.Value, modelType, retriever)!;
        }

        public override bool Exists(PrimaryKey id)
        {
            AssertEnabled();

            return cachedTable.Exists(id)!;
        }

        public override void Complete(T entity, IRetriever retriver)
        {
            AssertEnabled();

            cachedTable.Complete(entity, retriver);
        }

        public void NotifyDisabled()
        {
            Invalidated?.Invoke(this, CacheEventArgs.Disabled);
        }

        public void NotifyInvalidated()
        {
            Invalidated?.Invoke(this, CacheEventArgs.Invalidated);
        }

        public override List<T> RequestByBackReference<R>(IRetriever retriever, Expression<Func<T, Lite<R>?>> backReference, Lite<R> lite)
        {
            var dic = this.cachedTable.GetBackReferenceDictionary(backReference);

            var ids = dic.TryGetC(lite.Id).EmptyIfNull();

            return ids.Select(id => retriever.Complete<T>(id, e =>
            {
                this.Complete(e, retriever);
            })!).ToList();
        }

        public Type Type
        {
            get { return typeof(T); }
        }
    }

    public static IEnumerable<Type> SemiControllers { get { return controllers.Where(a=>a.Value == null).Select(a=>a.Key); } }

    internal static Dictionary<Type, List<CachedTableBase>> semiControllers = new Dictionary<Type, List<CachedTableBase>>();
    static Dictionary<Type, ICacheLogicController?> controllers = new Dictionary<Type, ICacheLogicController?>(); //CachePack

    static DirectedGraph<Type> inverseDependencies = new DirectedGraph<Type>();
    static DirectedGraph<Type> dependencies = new DirectedGraph<Type>();

    public static bool GloballyDisabled { get; set; }

    const string DisabledCachesKey = "disabledCaches";

    static HashSet<Type> DisabledTypesDuringTransaction()
    {
        var topUserData = Transaction.TopParentUserData();

        if (topUserData.TryGetC(DisabledCachesKey) is not HashSet<Type> hs)
        {
            topUserData[DisabledCachesKey] = hs = new HashSet<Type>();
        }

        return hs;
    }

    static bool IsDisabledInTransaction(Type type)
    {
        if (!Transaction.HasTransaction)
            return false;

        return Transaction.TopParentUserData().TryGetC(DisabledCachesKey) is HashSet<Type> disabledTypes && disabledTypes.Contains(type);
    }

    internal static void DisableTypeInTransaction(Type type)
    {
        DisabledTypesDuringTransaction().Add(type);
        
        controllers[type]!.NotifyDisabled();
    }

    internal static void DisableAllConnectedTypesInTransaction(Type type)
    {
        var connected = inverseDependencies.IndirectlyRelatedTo(type, true);

        var hs = DisabledTypesDuringTransaction();

        foreach (var stype in connected)
        {
            hs.Add(stype);
            controllers[stype]?.Do(t => t.NotifyDisabled());
        }
    }


    public static Dictionary<Type, EntityData> EntityDataOverrides = new Dictionary<Type, EntityData>();

    public static void OverrideEntityData<T>(EntityData data)
    {
        EntityDataOverrides.AddOrThrow(typeof(T), data, "{0} is already overriden");
    }

    static void TryCacheTable(SchemaBuilder sb, Type type)
    {
        if (!controllers.ContainsKey(type))
            giCacheTable.GetInvoker(type)(sb);
    }

    static GenericInvoker<Action<SchemaBuilder>> giCacheTable = new(sb => CacheTable<Entity>(sb));
    public static void CacheTable<T>(SchemaBuilder sb) where T : Entity
    {
        AssertStarted(sb);

        EntityData data = EntityDataOverrides.TryGetS(typeof(T)) ?? EntityKindCache.GetEntityData(typeof(T));

        if (data == EntityData.Master)
        {
            var cc = new CacheController<T>(sb.Schema);
            controllers.AddOrThrow(typeof(T), cc, "{0} already registered");

            TryCacheSubTables(typeof(T), sb);
        }
        else //data == EntityData.Transactional
        {
            controllers.AddOrThrow(typeof(T), null, "{0} already registered");

            TryCacheSubTables(typeof(T), sb);
        }
    }

    static void TryCacheSubTables(Type type, SchemaBuilder sb)
    {
        HashSet<Type> relatedTypes = sb.Schema.Table(type)
            .DependentTables()
            .Where(a => !a.Value.IsEnum)
            .Select(t => t.Key.Type)
            .ToHashSet();

        dependencies.Add(type);
        inverseDependencies.Add(type);

        foreach (var rType in relatedTypes)
        {
            TryCacheTable(sb, rType);

            dependencies.Add(type, rType);
            inverseDependencies.Add(rType, type);
        }
    }

    static ICacheLogicController GetController(Type type)
    {
        var controller = controllers.GetOrThrow(type, "{0} is not registered in CacheLogic");

        if (controller == null)
        {
            var attr = Schema.Current.Settings.TypeAttribute<EntityKindAttribute>(type);
            throw new InvalidOperationException("{0} is just semi cached".FormatWith(type.TypeName()) +
                (attr?.EntityData == EntityData.Transactional ? " (consider changing it to EntityData.Master)" : ""));
        }

        return controller;
    }


    internal static void LoadAllConnectedTypes(Type type)
    {
        var connected = dependencies.IndirectlyRelatedTo(type, includeInitialNode: true);

        foreach (var stype in connected)
        {
            var controller = controllers[stype];
            if (controller != null)
            {
                if (controller.CachedTable == null)
                    throw new InvalidOperationException($@"CacheTable for {stype.Name} is null.
This may be because SchemaCompleted is not yet called and you are accesing some ResetLazy in the Start method.
Remember that the Start could be called with an empty database!");

                controller.CachedTable.LoadAll();
            }
        }
    }

    public const string Method_InvalidateTable = "InvalidateTable";
    public const string Method_InvalidateAllTable = "InvalidateAllTables";

    internal static void NotifyInvalidateAllConnectedTypes(Type type)
    {
        var connected = inverseDependencies.IndirectlyRelatedTo(type, includeInitialNode: true);

        foreach (var stype in connected)
        {
            var controller = controllers[stype];
            if (controller != null)
                controller.NotifyInvalidated();

            ServerBroadcast?.Send(Method_InvalidateTable, TypeLogic.GetCleanName(stype));
        }
    }

    public static List<CachedTableBase> Statistics()
    {
        return controllers.Values.NotNull().Select(a => a.CachedTable).OrderByDescending(a => a.Count).ToList();
    }

    public static CacheType GetCacheType(Type type)
    {
        if (!type.IsEntity())
            throw new ArgumentException("type should be an Entity");

        if (!controllers.TryGetValue(type, out ICacheLogicController? controller))
            return CacheType.None;

        if (controller == null)
            return CacheType.Semi;

        return CacheType.Cached;
    }

    public static CachedTableBase GetCachedTable(Type type)
    {
        return controllers.GetOrThrow(type)!.CachedTable;
    }

    public static void ForceReset(bool systemLog = true)
    {
        foreach (var controller in controllers.Values.NotNull())
        {
            controller.ForceReset();
        }

        if (systemLog)
            SystemEventLogLogic.Log("CacheLogic.ForceReset");
    }

    public static XDocument SchemaGraph(Func<Type, bool> cacheHint)
    {
        var dgml = Schema.Current.ToDirectedGraph().ToDGML(t =>
            new[]
        {
            new XAttribute("Label", t.Name),
            new XAttribute("Background", GetColor(t.Type, cacheHint).ToHtml())
        }, info => new[]
        {
            info.IsLite ? new XAttribute("StrokeDashArray",  "2 3") : null,
        }.NotNull().ToArray());

        return dgml;
    }

    static Color GetColor(Type type, Func<Type, bool> cacheHint)
    {
        if (type.IsEnumEntityOrSymbol())
            return Color.Red;

        switch (CacheLogic.GetCacheType(type))
        {
            case CacheType.Cached: return Color.Purple;
            case CacheType.Semi: return Color.Pink;
        }

        if (typeof(Symbol).IsAssignableFrom(type))
            return Color.Orange;

        if (cacheHint != null && cacheHint(type))
            return Color.Yellow;

        return Color.Green;
    }

    public class CacheGlobalLazyManager : GlobalLazyManager
    {
        public override void AttachInvalidations(SchemaBuilder sb, InvalidateWith invalidateWith, EventHandler invalidate)
        {
            if (CacheLogic.GloballyDisabled || invalidateWith.UseBaseImplementation)
            {
                base.AttachInvalidations(sb, invalidateWith, invalidate);
            }
            else
            {
                void onInvalidation(object? sender, CacheEventArgs args)
                {
                    if (args == CacheEventArgs.Invalidated)
                    {
                        invalidate(sender, args);
                    }
                    else if (args == CacheEventArgs.Disabled)
                    {
                        if (Transaction.InTestTransaction)
                        {
                            invalidate(sender, args);
                            Transaction.Rolledback += dic => invalidate(sender, args);
                        }

                        Transaction.PostRealCommit += dic => invalidate(sender, args);
                    }
                }

                foreach (var t in invalidateWith.Types)
                {
                    CacheLogic.TryCacheTable(sb, t);

                    GetController(t).Invalidated += onInvalidation;
                }
            }
        }

        public override void OnLoad(SchemaBuilder sb, InvalidateWith invalidateWith)
        {
            if (CacheLogic.GloballyDisabled || invalidateWith.UseBaseImplementation)
            {
                base.OnLoad(sb, invalidateWith);
            }
            else
            {
                CacheLogic.ServerBroadcast?.StartIfNecessary(); //Typicaly NOOP
                foreach (var t in invalidateWith.Types)
                    sb.Schema.CacheController(t)!.Load();
            }
        }
    }


    internal static AsyncThreadVariable<Dictionary<Type, bool>?> assumeMassiveChangesAsInvalidations = Statics.ThreadVariable<Dictionary<Type, bool>?>("assumeMassiveChangesAsInvalidations");


    public static bool? IsMassiveChangesAsInvalidations(Type type)
    {
        return assumeMassiveChangesAsInvalidations.Value?.TryGetS(type);
    }

    public static IDisposable AssumeMassiveChangesAsInvalidations<T>(bool assumeInvalidations) where T : Entity
    {
        var dic = assumeMassiveChangesAsInvalidations.Value;

        if (dic == null)
            dic = assumeMassiveChangesAsInvalidations.Value = new Dictionary<Type, bool>();

        dic.Add(typeof(T), assumeInvalidations);

        return new Disposable(() =>
        {
            dic.Remove(typeof(T));

            if (dic.IsEmpty())
                assumeMassiveChangesAsInvalidations.Value = null;
        });
    }

    public static void InvalidateAll(bool systemLog)
    {
        CacheLogic.ForceReset(systemLog);
        GlobalLazy.ResetAll(systemLog);
        Schema.Current.InvalidateMetadata();
        GC.Collect(2);
    }
}

internal interface ICacheLogicController : ICacheController
{
    Type Type { get; }

    event EventHandler<CacheEventArgs> Invalidated;

    CachedTableBase CachedTable { get; }

    void NotifyDisabled();

    void NotifyInvalidated();

    void OnChange(object sender, SqlNotificationEventArgs args);

    void ForceReset();

    void BuildCachedTable();
}

public class CacheEventArgs : EventArgs
{
    private CacheEventArgs() { }

    public static readonly CacheEventArgs Invalidated = new CacheEventArgs();
    public static readonly CacheEventArgs Disabled = new CacheEventArgs();
}

public enum CacheType
{
    Cached,
    Semi,
    None
}


