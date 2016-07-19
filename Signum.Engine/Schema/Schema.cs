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

        public ViewBuilder ViewBuilder { get; set; }

        Dictionary<Type, Table> tables = new Dictionary<Type, Table>();
        public Dictionary<Type, Table> Tables
        {
            get { return tables; }
        }

        const string errorType = "TypeEntity table not cached. Remember to call Schema.Current.Initialize";


        #region Events

        public event Func<Type, bool, string> IsAllowedCallback;

        public string IsAllowed(Type type, bool inUserInterface)
        {
            foreach (var f in IsAllowedCallback.GetInvocationListTyped())
            {
                string result = f(type, inUserInterface);

                if (result != null)
                    return result;
            }

            return null;
        }

        internal Dictionary<string, Type> NameToType = new Dictionary<string, Type>();
        internal Dictionary<Type, string> TypeToName = new Dictionary<Type, string>();

        internal ResetLazy<TypeCaches> typeCachesLazy;

        public void AssertAllowed(Type type, bool inUserInterface)
        {
            string error = IsAllowed(type, inUserInterface);

            if (error != null)
                throw new UnauthorizedAccessException(EngineMessage.UnauthorizedAccessTo0Because1.NiceToString().FormatWith(type.NiceName(), error));
        }

        readonly IEntityEvents entityEventsGlobal = new EntityEvents<Entity>();
        public EntityEvents<Entity> EntityEventsGlobal
        {
            get { return (EntityEvents<Entity>)entityEventsGlobal; }
        }

        Dictionary<Type, IEntityEvents> entityEvents = new Dictionary<Type, IEntityEvents>();
        public EntityEvents<T> EntityEvents<T>()
            where T : Entity
        {
            return (EntityEvents<T>)entityEvents.GetOrCreate(typeof(T), () => new EntityEvents<T>());
        }

        internal void OnPreSaving(Entity entity, ref bool graphModified)
        {
            AssertAllowed(entity.GetType(), inUserInterface: false);

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnPreSaving(entity, ref graphModified);

            entityEventsGlobal.OnPreSaving(entity, ref graphModified);
        }

        internal void OnSaving(Entity entity)
        {
            AssertAllowed(entity.GetType(), inUserInterface: false);

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnSaving(entity);

            entityEventsGlobal.OnSaving(entity);
        }


        internal void OnSaved(Entity entity, SavedEventArgs args)
        {
            AssertAllowed(entity.GetType(), inUserInterface: false);

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnSaved(entity, args);

            entityEventsGlobal.OnSaved(entity, args);
        }

        internal void OnRetrieved(Entity entity)
        {
            AssertAllowed(entity.GetType(), inUserInterface: false);

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnRetrieved(entity);

            entityEventsGlobal.OnRetrieved(entity);
        }

        internal void OnPreUnsafeDelete<T>(IQueryable<T> entityQuery) where T : Entity
        {
            AssertAllowed(typeof(T), inUserInterface: false);

            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));

            if (ee != null)
                ee.OnPreUnsafeDelete(entityQuery);
        }

        internal void OnPreUnsafeMListDelete<T>(IQueryable mlistQuery, IQueryable<T> entityQuery) where T : Entity
        {
            AssertAllowed(typeof(T), inUserInterface: false);

            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));

            if (ee != null)
                ee.OnPreUnsafeMListDelete(mlistQuery, entityQuery);
        }


        internal void OnPreUnsafeUpdate(IUpdateable update)
        {
            var type = update.EntityType;
            if (type.IsInstantiationOf(typeof(MListElement<,>)))
                type = type.GetGenericArguments().First();

            AssertAllowed(type, inUserInterface: false);

            var ee = entityEvents.TryGetC(type);

            if (ee != null)
                ee.OnPreUnsafeUpdate(update);
        }

        internal LambdaExpression OnPreUnsafeInsert(Type type, IQueryable query, LambdaExpression constructor, IQueryable entityQuery)
        {
            AssertAllowed(type, inUserInterface: false);

            var ee = entityEvents.TryGetC(type);

            if (ee == null)
                return constructor;
         
            return ee.OnPreUnsafeInsert(query, constructor, entityQuery);
        }

        internal void OnPreBulkInsert(Type type, bool inMListTable)
        {
            AssertAllowed(type, inUserInterface: false);

            var ee = entityEvents.TryGetC(type);

            if (ee != null)
                ee.OnPreBulkInsert(inMListTable);
        }

        public ICacheController CacheController(Type type)
        {
            IEntityEvents ee = entityEvents.TryGetC(type);

            if (ee == null)
                return null;

            return ee.CacheController;
        }

        internal CacheControllerBase<T> CacheController<T>() where T : Entity
        {
            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));

            if (ee == null)
                return null;

            return ee.CacheController;
        }

        internal FilterQueryResult<T> OnFilterQuery<T>()
            where T : Entity
        {
            AssertAllowed(typeof(T), inUserInterface: false);

            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));
            if (ee == null)
                return null;

            FilterQueryResult<T> result = null;
            foreach (var item in ee.OnFilterQuery())
                result = CombineFilterResult(result, item);

            return result;
        }

        FilterQueryResult<T> CombineFilterResult<T>(FilterQueryResult<T> result, FilterQueryResult<T> expression) 
            where T: Entity
        {
            if (result == null)
                return expression;

            if (expression == null)
                return result;

            if (result.InMemoryFunction == null || expression.InMemoryFunction == null)
                return new FilterQueryResult<T>(a => result.InDatabaseExpresson.Evaluate(a) && expression.InDatabaseExpresson.Evaluate(a), null);

            return new FilterQueryResult<T>(
                a => result.InDatabaseExpresson.Evaluate(a) && expression.InDatabaseExpresson.Evaluate(a),
                a => result.InMemoryFunction(a) && expression.InMemoryFunction(a));
        }

        public Func<T, bool> GetInMemoryFilter<T>(bool userInterface)
            where T: Entity
        {
            using (userInterface ? ExecutionMode.UserInterface() : null)
            {
                EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));
                if (ee == null)
                    return a => true;

                Func<T, bool> result = null;
                foreach (var item in ee.OnFilterQuery().NotNull())
                {
                    if(item.InMemoryFunction == null)
                        throw new InvalidOperationException("FilterQueryResult with InDatabaseExpresson '{0}' has no equivalent InMemoryFunction"
                        .FormatWith(item.InDatabaseExpresson.ToString()));

                    result = CombineFunc(result, item.InMemoryFunction);
                }

                if (result == null)
                    return a => true;

                return result;
            }
        }

        private Func<T, bool> CombineFunc<T>(Func<T, bool> result, Func<T, bool> func) where T : Entity
        {
            if (result == null)
                return func;

            if (func == null)
                return result;

            return a => result(a) && func(a);
        }

        public Expression<Func<T, bool>> GetInDatabaseFilter<T>()
           where T : Entity
        {
            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));
            if (ee == null)
                return null;

            Expression<Func<T, bool>> result = null;
            foreach (var item in ee.OnFilterQuery().NotNull())
            {
                result = CombineExpr(result, item.InDatabaseExpresson);
            }

            if (result == null)
                return null;

            return result;
        }

        private Expression<Func<T, bool>> CombineExpr<T>(Expression<Func<T, bool>> result, Expression<Func<T, bool>> func) where T : Entity
        {
            if (result == null)
                return func;

            if (func == null)
                return result;

            return a => result.Evaluate(a) && func.Evaluate(a);
        }

        public event Func<Replacements, SqlPreCommand> Synchronizing;
        public SqlPreCommand SynchronizationScript(bool interactive = true, bool schemaOnly = false, string replaceDatabaseName = null)
        {
            OnBeforeDatabaseAccess();

            if (Synchronizing == null)
                return null;

            using (CultureInfoUtils.ChangeBothCultures(ForceCultureInfo))
            using (ExecutionMode.Global())
            {
                Replacements replacements = new Replacements() { Interactive = interactive, ReplaceDatabaseName = replaceDatabaseName, SchemaOnly = schemaOnly  };
                SqlPreCommand command = Synchronizing
                    .GetInvocationListTyped()
                    .Select(e =>
                    {
                        try
                        {
                            return e(replacements);
                        }
                        catch (Exception ex)
                        {
                            return new SqlPreCommandSimple(" -- Exception on {0}.{1}: {2}".FormatWith(e.Method.DeclaringType.Name, e.Method.Name, ex.Message));
                        }
                    })
                    .Combine(Spacing.Triple);

                return command;
            }
        }

        public event Func<SqlPreCommand> Generating;
        internal SqlPreCommand GenerationScipt()
        {
            OnBeforeDatabaseAccess();

            if (Generating == null)
                return null;

            using (CultureInfoUtils.ChangeBothCultures(ForceCultureInfo))
            using (ExecutionMode.Global())
            {
                return Generating
                    .GetInvocationListTyped()
                    .Select(e => e())
                    .Combine(Spacing.Triple);
            }
        }



        public event Action SchemaCompleted;

        public void OnSchemaCompleted()
        {
            if (SchemaCompleted == null)
                return;

            using (ExecutionMode.Global())
                foreach (var item in SchemaCompleted.GetInvocationListTyped())
                    item();

            SchemaCompleted = null;
        }

        public void WhenIncluded<T>(Action action) where T: Entity
        {
            SchemaCompleted += () =>
            {
                if (this.Tables.ContainsKey(typeof(T)))
                    action();
            };
        }

        public event Action BeforeDatabaseAccess;

        public void OnBeforeDatabaseAccess()
        {
            if (SchemaCompleted != null)
                throw new InvalidOperationException("OnSchemaCompleted has to be call at the end of the Start method");

            if (BeforeDatabaseAccess == null)
                return;

            using (ExecutionMode.Global())
                foreach (var item in BeforeDatabaseAccess.GetInvocationListTyped())
                    item();

            BeforeDatabaseAccess = null;
        }

        public event Action Initializing;

        public void Initialize()
        {
            OnBeforeDatabaseAccess();

            if (Initializing == null)
                return;

            using (ExecutionMode.Global())
                foreach (var item in Initializing.GetInvocationListTyped())
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
            this.ViewBuilder = new Maps.ViewBuilder(this);

            Generating += SchemaGenerator.SnapshotIsolation;
            Generating += SchemaGenerator.CreateSchemasScript;
            Generating += SchemaGenerator.CreateTablesScript;
            Generating += SchemaGenerator.InsertEnumValuesScript;
            Generating += TypeLogic.Schema_Generating;
            Generating += Assets.Schema_Generating;

            Synchronizing += SchemaSynchronizer.SnapshotIsolation;
            Synchronizing += SchemaSynchronizer.SynchronizeTablesScript;
            Synchronizing += TypeLogic.Schema_Synchronizing;
            Synchronizing += Assets.Schema_Synchronizing;
        }

        public static Schema Current
        {
            get { return Connector.Current.Schema; }
        }

        public Table Table<T>() where T : Entity
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
                    throw new InvalidOperationException("{0} does not implement {1}".FormatWith(result, typeof(IFieldFinder).Name));

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
                    .Where(r => r.Type.CleanType().IsIEntity())
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
                .Any(r => ss.FieldAttribute<IgnoreAttribute>(r) != null))
            {
                var ib = ss.FieldAttribute<ImplementedByAttribute>(route);
                var iba = ss.FieldAttribute<ImplementedByAllAttribute>(route);

                return Implementations.TryFromAttributes(route.Type.CleanType(), route, ib, iba) ?? Implementations.By();
            }

            throw new InvalidOperationException("Impossible to determine implementations for {0}".FormatWith(route, typeof(IEntity).Name));
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
            where T : Entity
        {
            return FindField(Table(typeof(T)), Reflector.GetMemberList(lambdaToField));
        }

        public Field Field(PropertyRoute route)
        {
            return FindField(Table(route.RootType), route.Members);
        }

        public Field TryField(PropertyRoute route)
        {
            return TryFindField(Table(route.RootType), route.Members);
        }

        public override string ToString()
        {
            return "Schema ( tables: {0} )".FormatWith(tables.Count);
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
            return GetDatabaseTables().Select(a => a.Name.Schema?.Database).Distinct().ToList();
        }

        public DirectedEdgedGraph<Table, RelationInfo> ToDirectedGraph()
        {
            return DirectedEdgedGraph<Table, RelationInfo>.Generate(Tables.Values, t => t.DependentTables());
        }

        public Type GetType(PrimaryKey id)
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
        public bool IsImplementedByAll { get; set; }
    }

  

    public interface ICacheController
    {
        bool Enabled { get; }
        void Load();

        IEnumerable<PrimaryKey> GetAllIds();

        void Complete(Entity entity, IRetriever retriver);

        string GetToString(PrimaryKey id);
        string TryGetToString(PrimaryKey id);
    }

    public class InvalidateEventArgs : EventArgs { }
    public class InvaludateEventArgs : EventArgs { }

    public abstract class CacheControllerBase<T> : ICacheController
        where T : Entity
    {
        public abstract bool Enabled { get; }
        public abstract void Load();

        public abstract IEnumerable<PrimaryKey> GetAllIds();

        void ICacheController.Complete(Entity entity, IRetriever retriver)
        {
            Complete((T)entity, retriver);
        }

        public abstract void Complete(T entity, IRetriever retriver);

        public abstract string GetToString(PrimaryKey id);
        public abstract string TryGetToString(PrimaryKey id);
    }

    


}
