using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;
using System.Windows;
using System.Linq.Expressions;
using Signum.Entities.Authorization;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Engine.Extensions.Properties;
using Signum.Entities.Disconnected;
using Signum.Utilities.Reflection;
using System.IO;
using System.IO.Compression;
using System.Data.SqlClient;

namespace Signum.Engine.Disconnected
{
    public static class DisconnectedLogic
    {
        public static bool OfflineMode = false;

        static Dictionary<Type, IDisconnectedStrategy> strategies = new Dictionary<Type, IDisconnectedStrategy>();

        public static ExportManager ExportManager = new ExportManager();
        public static LocalRestoreManager LocalRestoreManager = new LocalRestoreManager();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DisconnectedMachineDN>();
                sb.Include<DownloadStatisticsDN>();
                sb.Include<UploadStatisticsDN>();

                dqm[typeof(DisconnectedMachineDN)] = (from dm in Database.Query<DisconnectedMachineDN>()
                                                      select new
                                                      {
                                                          Entity = dm,
                                                          dm.MachineName,
                                                          dm.IsOffline,
                                                          dm.SeedMin,
                                                          dm.SeedMax,
                                                      }).ToDynamic();

                dqm[typeof(DownloadStatisticsDN)] = (from dm in Database.Query<DownloadStatisticsDN>()
                                                      select new
                                                      {
                                                          Entity = dm,
                                                          dm.CreationDate,
                                                          dm.Machine,
                                                          dm.State,
                                                          dm.Total,
                                                          dm.Exception,
                                                      }).ToDynamic();



                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += AssertDisconnectedStrategies;

                sb.Schema.EntityEventsGlobal.Saving += new SavingEventHandler<IdentifiableEntity>(EntityEventsGlobal_Saving);
            }
        }

        public static void UnsafeLock(Lite<DisconnectedMachineDN> machine)
        {
            foreach (var strategy in strategies.Values)
            {
                if (strategy.Upload == Upload.Subset)
                    miUnsafeLock.MakeGenericMethod(strategy.Type).Invoke(null, new object[] { machine, strategy });
            }
        }

        static readonly MethodInfo miUnsafeLock = typeof(DisconnectedLogic).GetMethod("UnsafeLock", BindingFlags.NonPublic | BindingFlags.Static);
        static int UnsafeLock<T>(Lite<DisconnectedMachineDN> machine, DisconnectedStrategy<T> strategy) where T : IdentifiableEntity, IDisconnectedEntity, new()
        {
            using (Schema.Current.GlobalMode())
                return Database.Query<T>().Where(strategy.UploadSubset).UnsafeUpdate(a => new T { DisconnectedMachine = machine, LastOnlineTicks = a.Ticks });
        }


        public static void UnsafeUnlock(Lite<DisconnectedMachineDN> machine)
        {
            foreach (var kvp in strategies)
            {
                if (kvp.Value.Upload == Upload.Subset)
                    miUnsafeUnlock.MakeGenericMethod(kvp.Key).Invoke(null, new[] { machine });
            }
        }

        static readonly MethodInfo miUnsafeUnlock = typeof(DisconnectedLogic).GetMethod("UnsafeUnlock", BindingFlags.NonPublic | BindingFlags.Static);
        static int UnsafeUnlock<T>(Lite<DisconnectedMachineDN> machine) where T : IdentifiableEntity, IDisconnectedEntity, new()
        {
            using (Schema.Current.GlobalMode())
                return Database.Query<T>().Where(a => a.DisconnectedMachine == machine).UnsafeUpdate(a => new T { DisconnectedMachine = null, LastOnlineTicks = null });
        }

        static void EntityEventsGlobal_Saving(IdentifiableEntity ident)
        {
            if (ident.Modified.Value)
            {
                strategies[ident.GetType()].Saving(ident);
            }
        }

        static void AssertDisconnectedStrategies()
        {
            var result = EnumerableExtensions.JoinStrict(
                strategies.Keys,
                Schema.Current.Tables.Keys.Where(a => !a.IsEnumProxy()),
                a => a,
                a => a,
                (a, b) => 0);

            var extra = result.Extra.OrderBy(a => a.Namespace).ThenBy(a => a.Name).ToString(t => "  DisconnectedLogic.Register<{0}>(Download.None, Upload.None);".Formato(t.Name), "\r\n");

            var lacking = result.Lacking.GroupBy(a => a.Namespace).OrderBy(gr => gr.Key).ToString(gr => "  //{0}\r\n".Formato(gr.Key) +
                gr.ToString(t => "  DisconnectedLogic.Register<{0}>(Download.None, Upload.None);".Formato(t.Name), "\r\n"), "\r\n\r\n");

            if (extra.HasText() || lacking.HasText())
                throw new InvalidOperationException("DisconnectedLogic's download strategies are not synchronized with the Schema.\r\n" +
                        (extra.HasText() ? ("Remove something like:\r\n" + extra + "\r\n\r\n") : null) +
                        (lacking.HasText() ? ("Add something like:\r\n" + lacking + "\r\n\r\n") : null));
        }

        public static void Register<T>(Download download, Upload upload) where T : IdentifiableEntity
        {
            Register(typeof(T), new DisconnectedStrategy<T>(download, null, upload, null));
        }

        public static void Register<T>(Expression<Func<T, bool>> downloadSubset, Upload upload) where T : IdentifiableEntity
        {
            Register(typeof(T), new DisconnectedStrategy<T>(Download.Subset, downloadSubset, upload, null));
        }

        public static void Register<T>(Download download, Expression<Func<T, bool>> uploadSubset) where T : IdentifiableEntity, IDisconnectedEntity
        {
            Register(typeof(T), new DisconnectedStrategy<T>(download, null, Upload.Subset, uploadSubset));
        }

        public static void Register<T>(Expression<Func<T, bool>> downloadSuperset, Expression<Func<T, bool>> uploadSubset) where T : IdentifiableEntity, IDisconnectedEntity
        {
            Register(typeof(T), new DisconnectedStrategy<T>(Download.Subset, downloadSuperset, Upload.Subset, uploadSubset));
        }

        public static void Register<T>(Expression<Func<T, bool>> subset) where T : IdentifiableEntity, IDisconnectedEntity
        {
            Register(typeof(T), new DisconnectedStrategy<T>(Download.Subset, subset, Upload.Subset, subset));
        }

        static void Register(Type type, IDisconnectedStrategy pair)
        {
            if (type.IsEnumProxy())
                throw new InvalidOperationException("EnumProxies can not be registered on DisconnectedLogic");

            strategies.AddOrThrow(type, pair, "{0} has already been registered");
        }

        public static DownloadStatisticsDN GetDownloadEstimation(Lite<DisconnectedMachineDN> machine)
        {
            return Database.Query<DownloadStatisticsDN>().Where(a => a.Total.HasValue).OrderBy(a => a.Machine == machine ? 0 : 1).ThenBy(a => a.Id).LastOrDefault();
        }

        public static UploadStatisticsDN GetUploadEstimation(Lite<DisconnectedMachineDN> machine)
        {
            return Database.Query<UploadStatisticsDN>().OrderBy(a => a.Machine == machine ? 0 : 1).ThenBy(a => a.Id).LastOrDefault();
        }

        public static Lite<DisconnectedMachineDN> GetDisconnectedMachine(string machineName)
        {
            return Database.Query<DisconnectedMachineDN>().Where(a => a.MachineName == machineName).Select(a => a.ToLite()).SingleOrDefault(); 
        }


        class EnumProxyDisconnectedStrategy : IDisconnectedStrategy
        {
            public Download Download { get { return Download.None; } }
            public Upload Upload { get { return Upload.None; } }


            public EnumProxyDisconnectedStrategy(Type type)
            {
                this.Type = type;
            }

            public Type Type{get;  private set;}

            public void Saving(IdentifiableEntity ident)
            {
                return;
            }
        }

        internal static IDisconnectedStrategy GetStrategy(Type type)
        {
            if(type.IsEnumProxy())
                return new EnumProxyDisconnectedStrategy(type);

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

        void Saving(IdentifiableEntity ident);
    }

    public class DisconnectedStrategy<T> : IDisconnectedStrategy where T : IdentifiableEntity
    {
        public DisconnectedStrategy(Download download, Expression<Func<T, bool>> downloadSubset, Upload upload, Expression<Func<T, bool>> uploadSubset)
        {
            if (download == Download.Subset && downloadSubset == null)
                throw new InvalidOperationException("In order to use Download.Subset, use an overload that takes a downloadSubset expression");

            this.Download = download;
            this.DownloadSubset = downloadSubset;

            if (upload == Upload.Subset)
            {
                if (uploadSubset == null)
                    throw new InvalidOperationException("In order to use Upload.Subset, use an overload that takes a uploadSubset expression");

                if (download == Download.None)
                    throw new InvalidOperationException("Upload.Subset is not compatible with Download.None, choose Upload.New instead");

                if (!typeof(IDisconnectedEntity).IsAssignableFrom(typeof(T)))
                    throw new InvalidOperationException("Upload.Subset requires that {0} implements {1}".Formato(typeof(T).Name, typeof(IDisconnectedEntity).Name));
            }
            this.Upload = upload;
            this.UploadSubset = uploadSubset;
        }

        public Download Download { get; private set; }
        public Expression<Func<T, bool>> DownloadSubset { get; private set; }

        public Upload Upload { get; private set; }
        public Expression<Func<T, bool>> UploadSubset { get; private set; }

        public void Saving(IdentifiableEntity ident)
        {
            if (DisconnectedLogic.OfflineMode)
            {
                switch (Upload)
                {
                    case Upload.None: throw new InvalidOperationException(Resources.NotAllowedToSave0WhileOffline.Formato(ident.GetType().NicePluralName()));
                    case Upload.New:
                        if (!ident.IsNew)
                            throw new InvalidOperationException(Resources.NotAllowedToModifyExisting0WhileOffline.Formato(ident.GetType().NicePluralName()));
                        break;
                    case Upload.Subset:

                        if (ident.IsNew)
                            return;

                        if (DownloadSubset == UploadSubset)
                            return;

                        if (((IDisconnectedEntity)ident).DisconnectedMachine != null)
                            throw new InvalidOperationException(Resources.The0WithId12IsLockedBy3.Formato(ident.GetType().NiceName(), ident.Id, ((IDisconnectedEntity)ident).DisconnectedMachine));
                        break;
                    default: break;
                }
            }
            else
            {
                switch (Upload)
                {
                    case Upload.None: break;
                    case Upload.New: break;
                    case Upload.Subset:
                        if (((IDisconnectedEntity)ident).DisconnectedMachine != null)
                            throw new InvalidOperationException(Resources.The0WithId12IsLockedBy3.Formato(ident.GetType().NiceName(), ident.Id, ((IDisconnectedEntity)ident).DisconnectedMachine));
                        break;
                    default: break;
                }
            }
        }

        public Type Type
        {
            get { return typeof(T); }
        }
    }
}
