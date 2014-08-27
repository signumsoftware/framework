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


        internal void OnSaved(IdentifiableEntity entity, SavedEventArgs args)
        {
            AssertAllowed(entity.GetType());

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnSaved(entity, args);

            entityEventsGlobal.OnSaved(entity, args);
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

        internal Expression<Func<T, bool>> OnFilterQuery<T>()
            where T : IdentifiableEntity
        {
            AssertAllowed(typeof(T));

            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));
            if (ee == null)
                return null;

            return ee.OnFilterQuery();
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

            using (CultureInfoUtils.ChangeBothCultures(ForceCultureInfo))
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

            using (CultureInfoUtils.ChangeBothCultures(ForceCultureInfo))
            using (ExecutionMode.Global())
            {
                return Generating
                    .GetInvocationList()
                    .Cast<Func<SqlPreCommand>>()
                    .Select(e => e())
                    .Combine(Spacing.Triple);
            }
        }


        public Action Initializing;

        public void Initialize()
        {
            if (Initializing == null)
                return;

            using (ExecutionMode.Global())
                foreach (Action item in Initializing.GetInvocationList())
                    item();

            Initializing = null;
        }

        #endregion

        static Schema()
        {
            PropertyRoute.SetFindImplementationsCallback(pr => Schema.Current.FindImplementations(pr));
            ModifiableEntity.SetIsRetrievingFunc(() => EntityCache.HasRetriever);
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
            Synchronizing += SchemaSynchronizer.SynchronizeSystemDefaultConstraints;
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

                foreach (var subTable in table.TablesMList().Cast<ITable>())
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

  

    public interface ICacheController
    {
        bool Enabled { get; }
        void Load();

        IEnumerable<int> GetAllIds();

        void Complete(IdentifiableEntity entity, IRetriever retriver);

        string GetToString(int id);
        string TryGetToString(int id);
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
        public abstract string TryGetToString(int id);
    }

    


}
