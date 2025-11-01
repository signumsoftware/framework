using Signum.Engine.Sync;
using Signum.Basics;
using Signum.Disconnected;
using Signum.Utilities.Reflection;

namespace Signum.Disconnected;

public static class DisconnectedLogic
{
    public static bool OfflineMode = false;

    public static string DatabaseFolder = @"C:\Databases";
    public static string BackupFolder = @"C:\Backups";
    public static string BackupNetworkFolder = @"C:\Backups";

    internal static Dictionary<Type, IDisconnectedStrategy> strategies = new Dictionary<Type, IDisconnectedStrategy>();

    public static ExportManager ExportManager = new ExportManager();
    public static ImportManager ImportManager = new ImportManager();
    public static LocalBackupManager LocalBackupManager = new LocalBackupManager();

    [AutoExpressionField]
    public static IQueryable<DisconnectedImportEntity> Imports(this DisconnectedMachineEntity m) => 
        As.Expression(() => Database.Query<DisconnectedImportEntity>().Where(di => di.Machine.Is(m)));

    [AutoExpressionField]
    public static IQueryable<DisconnectedImportEntity> Exports(this DisconnectedMachineEntity m) => 
        As.Expression(() => Database.Query<DisconnectedImportEntity>().Where(di => di.Machine.Is(m)));

    public static long ServerSeed;

    public static void Start(SchemaBuilder sb, long serverSeed = 1000000000)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        ServerSeed = serverSeed;

        sb.Include<DisconnectedMachineEntity>()
            .WithQuery(() => dm => new
            {
                Entity = dm,
                dm.MachineName,
                dm.State,
                dm.SeedMin,
                dm.SeedMax,
            });

        sb.Include<DisconnectedExportEntity>()
            .WithQuery(() => dm => new
            {
                Entity = dm,
                dm.CreationDate,
                dm.Machine,
                dm.State,
                dm.Total,
                dm.Exception,
            });

        sb.Include<DisconnectedImportEntity>()
            .WithQuery(() => dm => new
            {
                Entity = dm,
                dm.CreationDate,
                dm.Machine,
                dm.State,
                dm.Total,
                dm.Exception,
            });

        QueryLogic.Expressions.Register((DisconnectedMachineEntity dm) => dm.Imports(), DisconnectedMessage.Imports);
        QueryLogic.Expressions.Register((DisconnectedMachineEntity dm) => dm.Exports(), DisconnectedMessage.Exports);

        MachineGraph.Register();

        sb.Schema.SchemaCompleted += AssertDisconnectedStrategies;

        sb.Schema.Synchronizing += Schema_Synchronizing;
        sb.Schema.Generating += Schema_Generating;

        sb.Schema.EntityEventsGlobal.Saving += new SavingEventHandler<Entity>(EntityEventsGlobal_Saving);

        sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand?>(AuthCache_PreDeleteSqlSync);

        Validator.PropertyValidator((DisconnectedMachineEntity d) => d.SeedMin).StaticPropertyValidation += (dm, pi) => ValidateDisconnectedMachine(dm, pi, isMin: true);
        Validator.PropertyValidator((DisconnectedMachineEntity d) => d.SeedMax).StaticPropertyValidation += (dm, pi) => ValidateDisconnectedMachine(dm, pi, isMin: false);
    }

    private static SqlPreCommand? Schema_Generating()
    {
        if (DisconnectedLogic.OfflineMode)
            return null;

        return GetTablesToSeed()
            .Select(a => DisconnectedTools.SetNextIdSync(a, ServerSeed))
            .Combine(Spacing.Simple);
    }

    private static SqlPreCommand? Schema_Synchronizing(Replacements arg)
    {
        if (DisconnectedLogic.OfflineMode)
            return null;

        if (!arg.Interactive && arg.SchemaOnly) // Is ImportManager
            return null;

        return GetTablesToSeed()
            .Where(a => DisconnectedTools.GetNextId(a) < ServerSeed)
            .Select(a => DisconnectedTools.SetNextIdSync(a, ServerSeed))
            .Combine(Spacing.Simple);
    }

    static IEnumerable<ITable> GetTablesToSeed()
    {
        return Schema.Current.Tables.Values
            .Where(t => GetStrategy(t.Type).Upload != Upload.None)
            .SelectMany(t => t.TablesMList().Cast<ITable>().PreAnd(t))
            .Where(t => t.PrimaryKey.Identity);
    }

    static string? ValidateDisconnectedMachine(DisconnectedMachineEntity dm, PropertyInfo pi, bool isMin)
    {
        var conflicts = Database.Query<DisconnectedMachineEntity>()
            .Where(e => e.SeedInterval.Overlaps(dm.SeedInterval) && !e.Is(dm))
            .Select(e => new { e.SeedInterval, Machine = e.ToLite() })
            .ToList();

        conflicts = conflicts.Where(c => c.SeedInterval.Contains(isMin ? dm.SeedMin : dm.SeedMax) ||
            dm.SeedInterval.IsSubset(c.SeedInterval) || c.SeedInterval.IsSubset(dm.SeedInterval)).ToList();

        if (conflicts.Any())
            return DisconnectedMessage._0OverlapsWith1.NiceToString(pi.NiceName(), conflicts.CommaAnd(s => "{0} {1}".FormatWith(s.Machine, s.SeedInterval)));

        return null;
    }

    class MachineGraph : Graph<DisconnectedMachineEntity, DisconnectedMachineState>
    {
        public static void Register()
        {
            GetState = dm => dm.State;

            new Execute(DisconnectedMachineOperation.Save)
            {
                FromStates = { DisconnectedMachineState.Connected },
                ToStates = { DisconnectedMachineState.Connected },
                CanBeNew = true,
                CanBeModified = true,
                Execute = (dm, _) =>
                {

                }
            }.Register();

            new Execute(DisconnectedMachineOperation.UnsafeUnlock)
            {
                FromStates = { DisconnectedMachineState.Disconnected, DisconnectedMachineState.Faulted, DisconnectedMachineState.Fixed, DisconnectedMachineState.Connected }, //not fully disconnected
                ToStates = { DisconnectedMachineState.Connected },
                Execute = (dm, _) =>
                {
                    ImportManager.UnlockTables(dm.ToLite());
                    dm.State = DisconnectedMachineState.Connected;
                }
            }.Register();

            new Graph<DisconnectedImportEntity>.ConstructFrom<DisconnectedMachineEntity>(DisconnectedMachineOperation.FixImport)
            {
                CanConstruct = dm => dm.State.InState(DisconnectedMachineState.Faulted),
                Construct = (dm, _) =>
                {
                    return ImportManager.BeginImportDatabase(dm, null).RetrieveAndRemember();
                }
            }.Register();
        }
    }

    static SqlPreCommand? AuthCache_PreDeleteSqlSync(Entity arg)
    {
        TypeEntity type = (TypeEntity)arg;

        var ce = Administrator.UnsafeDeletePreCommandMList((DisconnectedExportEntity de) => de.Copies, Database.MListQuery((DisconnectedExportEntity de) => de.Copies).Where(mle => mle.Element.Type.Is(type)));
        var ci = Administrator.UnsafeDeletePreCommandMList((DisconnectedImportEntity di) => di.Copies, Database.MListQuery((DisconnectedImportEntity di) => di.Copies).Where(mle => mle.Element.Type.Is(type)));

        return SqlPreCommand.Combine(Spacing.Simple, ce, ci);
    } 

    static void EntityEventsGlobal_Saving(Entity ident)
    {
        if (ident.IsGraphModified)
        {
            strategies[ident.GetType()].Saving(ident);
        }
    }

    static void AssertDisconnectedStrategies()
    {
        Schema s = Schema.Current;

        var result = EnumerableExtensions.JoinStrict(
            strategies.Keys,
            s.Tables.Keys.Where(a => !a.IsEnumEntity()),
            a => a,
            a => a,
            (a, b) => 0);

        var extra = result.Extra.OrderBy(a => a.Namespace).ThenBy(a => a.Name).ToString(t => "  DisconnectedLogic.Register<{0}>(Download.None, Upload.None);".FormatWith(t.Name), "\n");

        var lacking = result.Missing.GroupBy(a => a.Namespace).OrderBy(gr => gr.Key).ToString(gr => "  //{0}\n".FormatWith(gr.Key) +
            gr.ToString(t => "  DisconnectedLogic.Register<{0}>(Download.None, Upload.None);".FormatWith(t.Name), "\n"), "\n\n");

        if (extra.HasText() || lacking.HasText())
            throw new InvalidOperationException("DisconnectedLogic's download strategies are not synchronized with the Schema.\n" +
                    (extra.HasText() ? ("Remove something like:\n" + extra + "\n\n") : null) +
                    (lacking.HasText() ? ("Add something like:\n" + lacking + "\n\n") : null));

        string errors = strategies.Where(kvp => kvp.Value.Upload == Upload.Subset && s.Table(kvp.Key).Ticks == null).ToString(a => a.Key.Name, "\n");
        if (errors.HasText())
            throw new InvalidOperationException("Ticks is mandatory for this Disconnected strategy. Tables: \n" + errors.Indent(4));

        foreach (var item in strategies.Where(kvp => kvp.Value.Upload != Upload.None).Select(a => a.Key))
        {
            giRegisterPreUnsafeInsert.GetInvoker(item)();
        }

        ExportManager.Initialize();
        ImportManager.Initialize();
    }


    static readonly GenericInvoker<Action> giRegisterPreUnsafeInsert = new(() => Register_PreUnsafeInsert<Entity>());
    static void Register_PreUnsafeInsert<T>() where T : Entity
    {
        Schema.Current.EntityEvents<T>().PreUnsafeInsert += (IQueryable query, LambdaExpression constructor, IQueryable<T> entityQuery) =>
        {
            if (constructor.Body.Type == typeof(T))
            {
                var newBody = Expression.Call(
                  miSetMixin.MakeGenericMethod(typeof(T), typeof(DisconnectedCreatedMixin), typeof(bool)),
                  constructor.Body,
                  Expression.Quote(disconnectedCreated),
                  Expression.Constant(DisconnectedLogic.OfflineMode));

                return Expression.Lambda(newBody, constructor.Parameters);
            }

            return constructor; //MListTable
        };
    }

    static readonly MethodInfo miSetMixin = ReflectionTools.GetMethodInfo((Entity a) => a.SetMixin((DisconnectedCreatedMixin m) => m.DisconnectedCreated, true)).GetGenericMethodDefinition();
    static Expression<Func<DisconnectedCreatedMixin, bool>> disconnectedCreated = (DisconnectedCreatedMixin m) => m.DisconnectedCreated;

    public static GenericInvoker<Func<Download, Upload, IDisconnectedStrategy>> giRegister = 
        new((down, up) => Register<TypeEntity>(down, up));

    public static DisconnectedStrategy<T> Register<T>(Download download, Upload upload) where T : Entity
    {
        return Register(new DisconnectedStrategy<T>(download, null, upload, null, new BasicImporter<T>()));
    }

    public static DisconnectedStrategy<T> Register<T>(Expression<Func<T, bool>> downloadSubset, Upload upload) where T : Entity
    {
        return Register(new DisconnectedStrategy<T>(Download.Subset, downloadSubset, upload, null, new BasicImporter<T>()));
    }

    public static DisconnectedStrategy<T> Register<T>(Download download, Expression<Func<T, bool>> uploadSubset) where T : Entity
    {
        return Register(new DisconnectedStrategy<T>(download, null, Upload.Subset, uploadSubset, new UpdateImporter<T>()));
    }

    public static DisconnectedStrategy<T> Register<T>(Expression<Func<T, bool>> downloadSuperset, Expression<Func<T, bool>> uploadSubset) where T : Entity
    {
        return Register(new DisconnectedStrategy<T>(Download.Subset, downloadSuperset, Upload.Subset, uploadSubset, new UpdateImporter<T>()));
    }

    public static DisconnectedStrategy<T> Register<T>(Expression<Func<T, bool>> subset) where T : Entity
    {
        return Register(new DisconnectedStrategy<T>(Download.Subset, subset, Upload.Subset, subset, new UpdateImporter<T>()));
    }


    static DisconnectedStrategy<T> Register<T>(DisconnectedStrategy<T> stragety) where T : Entity
    {
        if (typeof(T).IsEnumEntity())
            throw new InvalidOperationException("EnumEntities can not be registered on DisconnectedLogic");

        strategies.AddOrThrow(typeof(T), stragety, "{0} has already been registered");

        return stragety;
    }

    public static DisconnectedExportEntity? GetDownloadEstimation(Lite<DisconnectedMachineEntity> machine)
    {
        return Database.Query<DisconnectedExportEntity>().Where(a => a.Total.HasValue)
            .OrderBy(a => a.Machine.Is(machine) ? 0 : 1).ThenBy(a => a.Id).LastOrDefault();
    }

    public static DisconnectedImportEntity? GetUploadEstimation(Lite<DisconnectedMachineEntity> machine)
    {
        return Database.Query<DisconnectedImportEntity>().Where(a => a.Total.HasValue)
            .OrderBy(a => a.Machine.Is(machine) ? 0 : 1).ThenBy(a => a.Id).LastOrDefault();
    }

    public static Lite<DisconnectedMachineEntity>? GetDisconnectedMachine(string machineName)
    {
        return Database.Query<DisconnectedMachineEntity>().Where(a => a.MachineName == machineName).Select(a => a.ToLite()).SingleOrDefault();
    }


    class EnumEntityDisconnectedStrategy : IDisconnectedStrategy
    {
        public Download Download { get { return Download.None; } }
        public Upload Upload { get { return Upload.None; } }

        public EnumEntityDisconnectedStrategy(Type type)
        {
            this.Type = type;
        }

        public Type Type { get; private set; }

        public void Saving(Entity ident)
        {
            return;
        }

        public bool? DisableForeignKeys
        {
            get { return false; }
            set { throw new InvalidOperationException("Disable foreign keys not allowed for Enums"); }
        }

        public ICustomImporter? Importer
        {
            get { return null; }
            set { throw new InvalidOperationException("Disable foreign keys not allowed for Enums"); }
        }

        public ICustomExporter? Exporter
        {
            get { return null; }
            set { throw new InvalidOperationException("Disable foreign keys not allowed for Enums"); }
        }

        public void CreateDefaultImporter()
        {
            throw new NotImplementedException();
        }
    }

    internal static IDisconnectedStrategy GetStrategy(Type type)
    {
        if (type.IsEnumEntity())
            return new EnumEntityDisconnectedStrategy(type);

        return DisconnectedLogic.strategies[type];
    }

    public static Dictionary<Type, StrategyPair> GetStrategyPairs()
    {
        return strategies.Values.ToDictionary(a => a.Type, a => new StrategyPair { Download = a.Download, Upload = a.Upload });
    }
}

public interface IDisconnectedStrategy
{
    Download Download { get; }

    Upload Upload { get; }

    Type Type { get; }

    bool? DisableForeignKeys { get; set; }

    void Saving(Entity ident);

    ICustomImporter? Importer { get; set; }
    ICustomExporter? Exporter { get; set; }
}

public class DisconnectedStrategy<T> : IDisconnectedStrategy where T : Entity
{
    internal DisconnectedStrategy(Download download, Expression<Func<T, bool>>? downloadSubset, Upload upload, Expression<Func<T, bool>>? uploadSubset, BasicImporter<T> importer)
    {
        if (download == Download.Subset && downloadSubset == null)
            throw new InvalidOperationException("In order to use Download.Subset, use an overload that takes a downloadSubset expression");

        this.Download = download;
        this.DownloadSubset = downloadSubset;

        if (typeof(T) == typeof(DisconnectedExportEntity) && (download != Download.None || upload != Upload.None))
            throw new InvalidOperationException("{0} should have DownloadStrategy and UploadStratey = None".FormatWith(typeof(T).NiceName()));

        if (upload != Upload.None)
            MixinDeclarations.Register(typeof(T), typeof(DisconnectedCreatedMixin));

        if (upload == Upload.Subset)
        {
            if (uploadSubset == null)
                throw new InvalidOperationException("In order to use Upload.Subset, use an overload that takes a uploadSubset expression");

            if (download == Download.None)
                throw new InvalidOperationException("Upload.Subset is not compatible with Download.None, choose Upload.New instead");

            MixinDeclarations.Register(typeof(T), typeof(DisconnectedSubsetMixin));
        }

        this.Upload = upload;
        this.UploadSubset = uploadSubset;

        this.Importer = importer;

        this.Exporter = download == Download.Replace ? new DeleteAndCopyExporter<T>() : new BasicExporter<T>();
    }

    public Download Download { get; private set; }
    public Expression<Func<T, bool>>? DownloadSubset { get; private set; }

    public Upload Upload { get; private set; }
    public Expression<Func<T, bool>>? UploadSubset { get; private set; }

    public void Saving(Entity entity)
    {
        if (DisconnectedLogic.OfflineMode)
        {
            if (Upload == Upload.None)
                throw new ApplicationException(DisconnectedMessage.NotAllowedToSave0WhileOffline.NiceToString().FormatWith(entity.GetType().NicePluralName()));

            if (entity.Mixin<DisconnectedCreatedMixin>().DisconnectedCreated)
                return;

            if (entity.IsNew)
            {
                entity.Mixin<DisconnectedCreatedMixin>().DisconnectedCreated = true;
                return;
            }

            if (Upload == Upload.Subset)
            {
                var dm = entity.Mixin<DisconnectedSubsetMixin>();

                if (dm.DisconnectedMachine != null)
                {
                    if (!dm.DisconnectedMachine.Is(DisconnectedMachineEntity.Current))
                        throw new ApplicationException(DisconnectedMessage.NotAllowedToSave0WhileOffline.NiceToString().FormatWith(entity.GetType().NiceName(), entity.Id, entity.ToString(), dm.DisconnectedMachine));
                    else
                        return;
                }
            }
        }
        else
        {
            if (Upload == Upload.Subset)
            {
                var dm = entity.Mixin<DisconnectedSubsetMixin>();

                if (dm.DisconnectedMachine != null)
                    throw new ApplicationException(DisconnectedMessage.The0WithId12IsLockedBy3.NiceToString().FormatWith(entity.GetType().NiceName(), entity.Id, entity.ToString(), dm.DisconnectedMachine));
            }
        }
    }

    public Type Type
    {
        get { return typeof(T); }
    }

    public bool? DisableForeignKeys { get; set; }

    public ICustomImporter? Importer { get; set; }
    public ICustomExporter? Exporter { get; set; }
}
