using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Entities.Authorization;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Entities.Disconnected;
using Signum.Utilities.Reflection;
using System.IO;
using System.IO.Compression;
using System.Data.SqlClient;
using Signum.Engine.Operations;
using Signum.Entities.Basics;

namespace Signum.Engine.Disconnected
{
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

        static Expression<Func<DisconnectedMachineDN, IQueryable<DisconnectedImportDN>>> ImportsExpression =
                m => Database.Query<DisconnectedImportDN>().Where(di => di.Machine.RefersTo(m));
        public static IQueryable<DisconnectedImportDN> Imports(this DisconnectedMachineDN m)
        {
            return ImportsExpression.Evaluate(m);
        }

        static Expression<Func<DisconnectedMachineDN, IQueryable<DisconnectedImportDN>>> ExportsExpression =
               m => Database.Query<DisconnectedImportDN>().Where(di => di.Machine.RefersTo(m));
        public static IQueryable<DisconnectedImportDN> Exports(this DisconnectedMachineDN m)
        {
            return ExportsExpression.Evaluate(m);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DisconnectedMachineDN>();
                sb.Include<DisconnectedExportDN>();
                sb.Include<DisconnectedImportDN>();

                dqm.RegisterQuery(typeof(DisconnectedMachineDN), () =>
                    from dm in Database.Query<DisconnectedMachineDN>()
                    select new
                    {
                        Entity = dm,
                        dm.MachineName,
                        dm.State,
                        dm.SeedMin,
                        dm.SeedMax,
                    });

                dqm.RegisterQuery(typeof(DisconnectedExportDN), () =>
                    from dm in Database.Query<DisconnectedExportDN>()
                    select new
                    {
                        Entity = dm,
                        dm.CreationDate,
                        dm.Machine,
                        dm.State,
                        dm.Total,
                        dm.Exception,
                    });

                dqm.RegisterQuery(typeof(DisconnectedImportDN), () =>
                    from dm in Database.Query<DisconnectedImportDN>()
                    select new
                    {
                        Entity = dm,
                        dm.CreationDate,
                        dm.Machine,
                        dm.State,
                        dm.Total,
                        dm.Exception,
                    });

                dqm.RegisterExpression((DisconnectedMachineDN dm) => dm.Imports(), ()=>DisconnectedMessage.Imports.NiceToString());
                dqm.RegisterExpression((DisconnectedMachineDN dm) => dm.Exports(), ()=>DisconnectedMessage.Exports.NiceToString());


                MachineGraph.Register();


                sb.Schema.Initializing += AssertDisconnectedStrategies;

                sb.Schema.EntityEventsGlobal.Saving += new SavingEventHandler<IdentifiableEntity>(EntityEventsGlobal_Saving);

                sb.Schema.Table<TypeDN>().PreDeleteSqlSync += new Func<IdentifiableEntity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);
            }
        }

        class MachineGraph : Graph<DisconnectedMachineDN, DisconnectedMachineState>
        {
            public static void Register()
            {
                GetState = dm => dm.State;

                new Execute(DisconnectedMachineOperation.Save)
                {
                    FromStates = { DisconnectedMachineState.Connected },
                    ToState = DisconnectedMachineState.Connected,
                    AllowsNew = true,
                    Lite = false,
                    Execute = (dm, _) => { }
                }.Register();

                new Execute(DisconnectedMachineOperation.UnsafeUnlock)
                {
                    FromStates = { DisconnectedMachineState.Disconnected, DisconnectedMachineState.Faulted, DisconnectedMachineState.Fixed, DisconnectedMachineState.Connected }, //not fully disconnected
                    ToState = DisconnectedMachineState.Connected,
                    Execute = (dm, _) =>
                    {
                        ImportManager.UnlockTables(dm.ToLite());
                        dm.State = DisconnectedMachineState.Connected;
                    }
                }.Register();

                new Graph<DisconnectedImportDN>.ConstructFrom<DisconnectedMachineDN>(DisconnectedMachineOperation.FixImport)
                {
                    CanConstruct = dm => dm.State.InState(DisconnectedMachineState.Faulted),
                    Construct = (dm, _) =>
                    {
                        return ImportManager.BeginImportDatabase(dm, null).Retrieve();
                    }
                }.Register();
            }
        }

        static SqlPreCommand AuthCache_PreDeleteSqlSync(IdentifiableEntity arg)
        {
            TypeDN type = (TypeDN)arg;

            var ce = Administrator.UnsafeDeletePreCommand(Database.MListQuery((DisconnectedExportDN de) => de.Copies).Where(mle => mle.Element.Type.RefersTo(type)));
            var ci = Administrator.UnsafeDeletePreCommand(Database.MListQuery((DisconnectedImportDN di) => di.Copies).Where(mle => mle.Element.Type.RefersTo(type)));

            return SqlPreCommand.Combine(Spacing.Simple, ce, ci);
        }



        static void EntityEventsGlobal_Saving(IdentifiableEntity ident)
        {
            if (ident.IsGraphModified)
            {
                strategies[ident.GetType()].Saving(ident);
            }
        }

        static void AssertDisconnectedStrategies()
        {
            var result = EnumerableExtensions.JoinStrict(
                strategies.Keys,
                Schema.Current.Tables.Keys.Where(a => !a.IsEnumEntity()),
                a => a,
                a => a,
                (a, b) => 0);

            var extra = result.Extra.OrderBy(a => a.Namespace).ThenBy(a => a.Name).ToString(t => "  DisconnectedLogic.Register<{0}>(Download.None, Upload.None);".Formato(t.Name), "\r\n");

            var lacking = result.Missing.GroupBy(a => a.Namespace).OrderBy(gr => gr.Key).ToString(gr => "  //{0}\r\n".Formato(gr.Key) +
                gr.ToString(t => "  DisconnectedLogic.Register<{0}>(Download.None, Upload.None);".Formato(t.Name), "\r\n"), "\r\n\r\n");

            if (extra.HasText() || lacking.HasText())
                throw new InvalidOperationException("DisconnectedLogic's download strategies are not synchronized with the Schema.\r\n" +
                        (extra.HasText() ? ("Remove something like:\r\n" + extra + "\r\n\r\n") : null) +
                        (lacking.HasText() ? ("Add something like:\r\n" + lacking + "\r\n\r\n") : null));

            ExportManager.Initialize();
            ImportManager.Initialize();
        }

        public static DisconnectedStrategy<T> Register<T>(Download download, Upload upload) where T : IdentifiableEntity
        {
            return Register(new DisconnectedStrategy<T>(download, null, upload, null, new BasicImporter<T>()));
        }

        public static DisconnectedStrategy<T> Register<T>(Expression<Func<T, bool>> downloadSubset, Upload upload) where T : IdentifiableEntity
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

        static DisconnectedStrategy<T> Register<T>(DisconnectedStrategy<T> stragety) where T : IdentifiableEntity
        {
            if (typeof(T).IsEnumEntity())
                throw new InvalidOperationException("EnumProxies can not be registered on DisconnectedLogic");

            strategies.AddOrThrow(typeof(T), stragety, "{0} has already been registered");

            return stragety;
        }

        public static DisconnectedExportDN GetDownloadEstimation(Lite<DisconnectedMachineDN> machine)
        {
            return Database.Query<DisconnectedExportDN>().Where(a => a.Total.HasValue).OrderBy(a => a.Machine == machine ? 0 : 1).ThenBy(a => a.Id).LastOrDefault();
        }

        public static DisconnectedImportDN GetUploadEstimation(Lite<DisconnectedMachineDN> machine)
        {
            return Database.Query<DisconnectedImportDN>().Where(a => a.Total.HasValue).OrderBy(a => a.Machine == machine ? 0 : 1).ThenBy(a => a.Id).LastOrDefault();
        }

        public static Lite<DisconnectedMachineDN> GetDisconnectedMachine(string machineName)
        {
            return Database.Query<DisconnectedMachineDN>().Where(a => a.MachineName == machineName).Select(a => a.ToLite()).SingleOrDefault();
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

            public void Saving(IdentifiableEntity ident)
            {
                return;
            }

            public bool? DisableForeignKeys
            {
                get { return false; }
                set { throw new InvalidOperationException("Disable foreign keys not allowed for Enums"); }
            }

            public ICustomImporter Importer
            {
                get { return null; }
                set { throw new InvalidOperationException("Disable foreign keys not allowed for Enums"); }
            }

            public ICustomExporter Exporter
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

        void Saving(IdentifiableEntity ident);

        ICustomImporter Importer { get; set; }
        ICustomExporter Exporter { get; set; }
    }

    public class DisconnectedStrategy<T> : IDisconnectedStrategy where T : IdentifiableEntity
    {
        internal DisconnectedStrategy(Download download, Expression<Func<T, bool>> downloadSubset, Upload upload, Expression<Func<T, bool>> uploadSubset, BasicImporter<T> importer)
        {
            if (download == Download.Subset && downloadSubset == null)
                throw new InvalidOperationException("In order to use Download.Subset, use an overload that takes a downloadSubset expression");

            this.Download = download;
            this.DownloadSubset = downloadSubset;

            if (typeof(T) == typeof(DisconnectedExportDN) && (download != Download.None || upload != Upload.None))
                throw new InvalidOperationException("{0} should have DownloadStrategy and UploadStratey = None".Formato(typeof(T).NiceName()));

            if (upload == Upload.Subset)
            {
                if (uploadSubset == null)
                    throw new InvalidOperationException("In order to use Upload.Subset, use an overload that takes a uploadSubset expression");

                if (download == Download.None)
                    throw new InvalidOperationException("Upload.Subset is not compatible with Download.None, choose Upload.New instead");

                MixinDeclarations.Register(typeof(T), typeof(DisconnectedMixin));
            }

            this.Upload = upload;
            this.UploadSubset = uploadSubset;

            this.Importer = importer;

            this.Exporter = download == Entities.Disconnected.Download.Replace ? new DeleteAndCopyExporter<T>() : new BasicExporter<T>();
        }

        public Download Download { get; private set; }
        public Expression<Func<T, bool>> DownloadSubset { get; private set; }

        public Upload Upload { get; private set; }
        public Expression<Func<T, bool>> UploadSubset { get; private set; }

        public void Saving(IdentifiableEntity ident)
        {
            if (DisconnectedLogic.OfflineMode)
            {
                if (Upload == Upload.None)
                    throw new ApplicationException(DisconnectedMessage.NotAllowedToSave0WhileOffline.NiceToString().Formato(ident.GetType().NicePluralName()));

                if (ident.IsNew)
                    return;

                if (Upload == Upload.Subset)
                {
                    var dm = ident.Mixin<DisconnectedMixin>();

                    if (dm.DisconnectedMachine != null)
                    {
                        if (!dm.DisconnectedMachine.Is(DisconnectedMachineDN.Current))
                            throw new ApplicationException(DisconnectedMessage.The0WithId12IsLockedBy3.NiceToString().Formato(ident.GetType().NiceName(), ident.Id, ident.ToString(), dm.DisconnectedMachine));
                        else
                            return;
                    }
                }

                if (!DisconnectedExportRanges.InModifiableRange(ident.GetType(), ident.Id))
                    throw new ApplicationException(AuthMessage.NotAllowedToSaveThis0WhileOffline.NiceToString().Formato(ident.GetType().NiceName()));

            }
            else
            {
                if (Upload == Upload.Subset)
                {
                    var dm = ident.Mixin<DisconnectedMixin>();

                    if (dm.DisconnectedMachine != null)
                        throw new ApplicationException(DisconnectedMessage.The0WithId12IsLockedBy3.NiceToString().Formato(ident.GetType().NiceName(), ident.Id, ident.ToString(), dm.DisconnectedMachine));
                }
            }
        }

        public Type Type
        {
            get { return typeof(T); }
        }

        public bool? DisableForeignKeys { get; set; }

        public ICustomImporter Importer { get; set; }
        public ICustomExporter Exporter { get; set; }
    }
}
