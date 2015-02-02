using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities;
using System.Collections;
using System.Threading;
using Signum.Utilities;
using System.Collections.Concurrent;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Entities.Cache;
using Signum.Engine.Authorization;
using System.Drawing;
using Signum.Entities.Basics;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Data.SqlTypes;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.SchemaInfoTables;
using Signum.Engine.Basics;
using Signum.Engine.Linq;
using System.Linq.Expressions;
using System.IO;
using System.Data;

namespace Signum.Engine.Cache
{
    public static class CacheLogic
    {
        public static bool CheckOwner = false;

        public static bool WithSqlDependency { get; internal set; }

        public static bool DropStaleServices = true;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null)));
        }

        /// <summary>
        /// If you have invalidation problems look at exceptions in: select * from sys.transmission_queue 
        /// If there are exceptions like: 'Could not obtain information about Windows NT group/user'
        ///    Change login to a SqlServer authentication (i.e.: sa)
        ///    Change Server Authentication mode and enable SA: http://msdn.microsoft.com/en-us/library/ms188670.aspx
        ///    Change Database ownership to sa: ALTER AUTHORIZATION ON DATABASE::yourDatabase TO sa
        /// </summary>
        public static void Start(SchemaBuilder sb, bool? withSqlDependency = null)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                PermissionAuthLogic.RegisterTypes(typeof(CachePermission));

                sb.SwitchGlobalLazyManager(new CacheGlobalLazyManager());

                if (withSqlDependency == true && !Connector.Current.SupportsSqlDependency)
                    throw new InvalidOperationException("Sql Dependency is not supported by the current connection");

                WithSqlDependency = withSqlDependency ?? Connector.Current.SupportsSqlDependency;

                sb.Schema.BeforeDatabaseAccess += StartSqlDependencyAndEnableBrocker;
            }
        }


        public static TextWriter LogWriter;
        public static List<T> ToListWithInvalidation<T>(this IQueryable<T> simpleQuery, Type type, string exceptionContext, Action<SqlNotificationEventArgs> invalidation)
        {
            if (!WithSqlDependency)
                throw new InvalidOperationException("ToListWithInvalidation requires SqlDependency");

            ITranslateResult tr;
            using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
                tr = ((DbQueryProvider)simpleQuery.Provider).GetRawTranslateResult(simpleQuery.Expression);

            OnChangeEventHandler onChange = (object sender, SqlNotificationEventArgs args) =>
            {
                try
                {
                    if (args.Type != SqlNotificationType.Change)
                        throw new InvalidOperationException(
                            "Problems with SqlDependency (Type : {0} Source : {1} Info : {2}) on query: \r\n{3}"
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
            };

            SimpleReader reader = null;

            Expression<Func<IProjectionRow, T>> projectorExpression = (Expression<Func<IProjectionRow, T>>)tr.GetMainProjector();
            Func<IProjectionRow, T> projector = projectorExpression.Compile();

            List<T> list = new List<T>();

            CacheLogic.AssertSqlDependencyStarted();

            Table table = Schema.Current.Table(type);
            DatabaseName db = table.Name.Schema.Try(s => s.Database);

            SqlConnector subConnector = ((SqlConnector)Connector.Current).ForDatabase(db);

            if (CacheLogic.LogWriter != null)
                CacheLogic.LogWriter.WriteLine("Load ToListWithInvalidations {0} {1}".FormatWith(typeof(T).TypeName()), exceptionContext);

            using (new EntityCache())
                subConnector.ExecuteDataReaderDependency(tr.MainCommand, onChange, StartSqlDependencyAndEnableBrocker, fr =>
                    {
                        if (reader == null)
                            reader = new SimpleReader(fr, EntityCache.NewRetriever());

                        list.Add(projector(reader));
                    }, CommandType.Text);

            return list;
        }

        public static void ExecuteDataReaderOpionalDependency(this SqlConnector connector, SqlPreCommandSimple preCommand, OnChangeEventHandler change, Action<FieldReader> forEach)
        {
            if (WithSqlDependency)
            {
                connector.ExecuteDataReaderDependency(preCommand, change, StartSqlDependencyAndEnableBrocker, forEach, CommandType.Text);
            }
            else
            {
                using (var dr = preCommand.UnsafeExecuteDataReader())
                {
                    FieldReader reader = new FieldReader(dr);
                    while (dr.Read())
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
                SqlConnector connector = (SqlConnector)Connector.Current;

                if (DropStaleServices)
                {
                    //to avoid massive logs with SqlQueryNotificationStoredProcedure
                    //http://rusanu.com/2007/11/10/when-it-rains-it-pours/
                    var staleServices = (from s in Database.View<SysServiceQueues>()
                                         where s.activation_procedure != null && !Database.View<SysProcedures>().Any(p => "[dbo].[" + p.name + "]" == s.activation_procedure)
                                         select s.name).ToList();

                    foreach (var s in staleServices)
                    {
                        try
                        {
                            new SqlPreCommandSimple("DROP SERVICE [{0}]".FormatWith(s)).ExecuteNonQuery();
                            new SqlPreCommandSimple("DROP QUEUE [{0}]".FormatWith(s)).ExecuteNonQuery();
                        }
                        catch { }
                    }
                }

                foreach (var database in Schema.Current.DatabaseNames())
                {
                    SqlConnector sub = connector.ForDatabase(database);

                    try
                    {
                        try
                        {
                            SqlDependency.Start(sub.ConnectionString);
                        }
                        catch (InvalidOperationException ex)
                        {
                            string databaseName = database.TryToString() ?? Connector.Current.DatabaseName();

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
                        else throw e;
                    }
                }

                RegisterOnShutdown();

                started = true;
            }
        }

        static void EnableOrCreateBrocker(string databaseName)
        {
            try
            {
                using (Transaction tr = Transaction.None())
                {
                    Executor.ExecuteNonQuery("ALTER DATABASE {0} SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;".FormatWith(databaseName));

                    tr.Commit();
                }
            }
            catch (SqlException)
            {
                using (Transaction tr = Transaction.None())
                {
                    Executor.ExecuteNonQuery("ALTER DATABASE {0} SET NEW_BROKER WITH ROLLBACK IMMEDIATE;".FormatWith(databaseName));

                    tr.Commit();
                }
            }
        }

        static bool registered = false;
        static void RegisterOnShutdown()
        {
            if (registered)
                return;

            SafeConsole.SetConsoleCtrlHandler(ct =>
            {
                Shutdown();
                return true;
            }, true);

            AppDomain.CurrentDomain.DomainUnload += (o, a) => Shutdown();

            registered = true;
        }

        public static void Shutdown()
        {
            if (GloballyDisabled)
                return;

            var connector = ((SqlConnector)Connector.Current);
            foreach (var database in Schema.Current.DatabaseNames())
            {
                SqlConnector sub = connector.ForDatabase(database);

                SqlDependency.Stop(sub.ConnectionString);
            }

        }

        static SqlPreCommandSimple GetDependencyQuery(ITable table)
        {
            return new SqlPreCommandSimple("SELECT {0} FROM {1}".FormatWith(table.Columns.Keys.ToString(c => c.SqlEscape(), ", "), table.Name));
        }

        class CacheController<T> : CacheControllerBase<T>, ICacheLogicController
                where T : Entity
        {
            public CachedTable<T> cachedTable;
            public CachedTableBase CachedTable { get { return cachedTable; } }

            public CacheController(Schema schema)
            {
                var ee = schema.EntityEvents<T>();

                ee.CacheController = this;
                ee.Saving += ident =>
            {
                    if (ident.IsGraphModified)
            {
                        DisableAndInvalidate(withUpdates: !ident.IsNew);
            }
                };
                ee.PreUnsafeDelete += query => DisableAndInvalidate(withUpdates: false); ;
                ee.PreUnsafeUpdate += (update, entityQuery) => DisableAndInvalidate(withUpdates: true); ;
                ee.PreUnsafeInsert += (query, constructor, entityQuery) => { DisableAndInvalidate(withUpdates: constructor.Body.Type.IsInstantiationOf(typeof(MListElement<,>))); return constructor; };
                ee.PreUnsafeMListDelete += (mlistQuery, entityQuery) => DisableAndInvalidate(withUpdates: true);
                ee.PreBulkInsert += () => DisableAndInvalidate(withUpdates: false);
            }

            public void BuildCachedTable()
            {
                cachedTable = new CachedTable<T>(this, new Linq.AliasGenerator(), null, null);
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
                get { return !GloballyDisabled && !ExecutionMode.IsCacheDisabled && !IsDisabledInTransaction(typeof(T)); }
            }

            private void AssertEnabled()
            {
                if (!Enabled)
                    throw new InvalidOperationException("Cache for {0} is not enabled".FormatWith(typeof(T).TypeName()));
            }

            public event EventHandler<CacheEventArgs> Invalidated;

            public void OnChange(object sender, SqlNotificationEventArgs args)
            {
                NotifyInvalidateAllConnectedTypes(typeof(T));
            }

            static object syncLock = new object();

            public override void Load()
            {
                cachedTable.LoadAll();
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

            public override string GetToString(PrimaryKey id)
            {
                AssertEnabled();

                return cachedTable.GetToString(id);
            }

            public override string TryGetToString(PrimaryKey id)
            {
                AssertEnabled();

                return cachedTable.TryGetToString(id);
            }

            public override void Complete(T entity, IRetriever retriver)
            {
                AssertEnabled();

                cachedTable.Complete(entity, retriver);
            }

            public void NotifyDisabled()
            {
                if (Invalidated != null)
                    Invalidated(this, CacheEventArgs.Disabled);
            }

            public void NotifyInvalidated()
            {
                if (Invalidated != null)
                    Invalidated(this, CacheEventArgs.Invalidated);
            }
        }


        static Dictionary<Type, ICacheLogicController> controllers = new Dictionary<Type, ICacheLogicController>(); //CachePack

        static DirectedGraph<Type> inverseDependencies = new DirectedGraph<Type>();

        public static bool GloballyDisabled { get; set; }

        const string DisabledCachesKey = "disabledCaches";

        static HashSet<Type> DisabledTypesDuringTransaction()
        {
            var topUserData = Transaction.TopParentUserData();

            var hs = topUserData.TryGetC(DisabledCachesKey) as HashSet<Type>;
            if (hs == null)
            {
                topUserData[DisabledCachesKey] = hs = new HashSet<Type>();
            }

            return hs;
        }

        static bool IsDisabledInTransaction(Type type)
        {
            if (!Transaction.HasTransaction)
                return false;

            HashSet<Type> disabledTypes = Transaction.TopParentUserData().TryGetC(DisabledCachesKey) as HashSet<Type>;

            return disabledTypes != null && disabledTypes.Contains(type);
        }

        static void DisableTypeInTransaction(Type type)
        {
            DisabledTypesDuringTransaction().Add(type);

            controllers[type].NotifyDisabled();
        }

        static void DisableAllConnectedTypesInTransaction(Type type)
        {
            var connected = inverseDependencies.IndirectlyRelatedTo(type, true);

            var hs = DisabledTypesDuringTransaction();

            foreach (var stype in connected)
            {
                hs.Add(stype);
                controllers[stype].TryDo(t => t.NotifyDisabled());
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

        static GenericInvoker<Action<SchemaBuilder>> giCacheTable = new GenericInvoker<Action<SchemaBuilder>>(sb => CacheTable<Entity>(sb));
        public static void CacheTable<T>(SchemaBuilder sb) where T : Entity
        {
            AssertStarted(sb);

            EntityData data = EntityDataOverrides.TryGetS(typeof(T)) ?? EntityKindCache.GetEntityData(typeof(T));

            if (data == EntityData.Master)
            {
                var cc = new CacheController<T>(sb.Schema);
                controllers.AddOrThrow(typeof(T), cc, "{0} already registered");

                TryCacheSubTables(typeof(T), sb);

                cc.BuildCachedTable();
            }
            else //data == EntityData.Transactional
            {
                controllers.AddOrThrow(typeof(T), null, "{0} already registered");

                TryCacheSubTables(typeof(T), sb);
            }
        }

        static void TryCacheSubTables(Type type, SchemaBuilder sb)
        {
            List<Type> relatedTypes = sb.Schema.Table(type).DependentTables()
                .Where(a => !a.Value.IsEnum)
                .Select(t => t.Key.Type).ToList();

            inverseDependencies.Add(type);

            foreach (var rType in relatedTypes)
            {
                TryCacheTable(sb, rType);

                inverseDependencies.Add(rType, type);
            }
        }

        static ICacheLogicController GetController(Type type)
        {
            var controller = controllers.GetOrThrow(type, "{0} is not registered in CacheLogic");

            if (controller == null)
                throw new InvalidOperationException("{0} is just semi cached".FormatWith(type.TypeName()));

            return controller;
        }

        static void NotifyInvalidateAllConnectedTypes(Type type)
        {
            var connected = inverseDependencies.IndirectlyRelatedTo(type, includeInitialNode: true);

            foreach (var stype in connected)
            {
                var controller = controllers[stype];
                if (controller != null)
                    controller.NotifyInvalidated();
            }
        }

        public static List<CachedTableBase> Statistics()
        {
            return controllers.Values.NotNull().Select(a => a.CachedTable).OrderByDescending(a => a.Count).ToList();
        }

        public static CacheType GetCacheType(Type type)
        {
            if(!type.IsEntity())
                throw new ArgumentException("type should be an Entity");

            ICacheLogicController controller;
            if (!controllers.TryGetValue(type, out controller))
                return CacheType.None;

            if (controller == null)
                return CacheType.Semi;

            return CacheType.Cached;
        }

        public static CachedTableBase GetCachedTable(Type type)
        {
            return controllers.GetOrThrow(type).CachedTable;
        }

        public static void ForceReset()
        {
            foreach (var controller in controllers.Values.NotNull())
            {
                controller.ForceReset();
            }
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
                if (CacheLogic.GloballyDisabled)
                {
                    base.AttachInvalidations(sb, invalidateWith, invalidate);
                }
                else
                {
                    EventHandler<CacheEventArgs> onInvalidation = (sender, args) =>
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
                    };

                    foreach (var t in invalidateWith.Types)
                    {
                        CacheLogic.TryCacheTable(sb, t);

                        GetController(t).Invalidated += onInvalidation;
                    }
                }
            }

            public override void OnLoad(SchemaBuilder sb, InvalidateWith invalidateWith)
            {
                if (CacheLogic.GloballyDisabled)
                {
                    base.OnLoad(sb, invalidateWith);
                }
                else
                {
                    foreach (var t in invalidateWith.Types)
                        sb.Schema.CacheController(t).Load();
                }
            }
        }

        public static void LoadAllControllers()
        {
            foreach (var c in controllers.Values.NotNull())
            {
                c.Load();
            }
        }

      
    }

    internal interface ICacheLogicController : ICacheController
    {
        event EventHandler<CacheEventArgs> Invalidated;

        CachedTableBase CachedTable { get; }

        void NotifyDisabled();

        void NotifyInvalidated();

        void OnChange(object sender, SqlNotificationEventArgs args);

        void ForceReset();
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
}
