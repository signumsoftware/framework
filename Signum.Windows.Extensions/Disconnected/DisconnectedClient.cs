using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System.Windows;
using Signum.Services;
using System.Reflection;
using System.Collections;
using Signum.Windows;
using System.Windows.Controls;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Windows.Operations;
using Signum.Entities.Disconnected;
using Signum.Windows.Authorization;

namespace Signum.Windows.Disconnected
{
    public static class DisconnectedClient
    {
        public static Func<IDisconnectedTransferServer> GetTransferServer;

        public static Lite<DisconnectedMachineDN> CurrentDisconnectedMachine
        {
            get
            {
                if (GetCurrentDisconnectedMachine == null)
                    throw new InvalidOperationException("DisconnectedClient.GetCurrentDisconnectedMachine is not set");

                return GetCurrentDisconnectedMachine();
            }
        }

        public static Func<Lite<DisconnectedMachineDN>> GetCurrentDisconnectedMachine; 

        public static string BackupFile = "LocalDB.bak";
        public static string DatabaseFile = "LocalDB.mdf";
        public static string DatabaseLogFile = "LocalDB_log.ldf";

        static Dictionary<Type, StrategyPair> strategies; 

        public static bool OfflineMode { get; set; }
     
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<DisconnectedMachineDN>(EntityType.Admin) { View = dm => new DisconnectedMachine() },
                    new EntitySettings<DownloadStatisticsDN>(EntityType.ServerOnly) { View = dm => new DownloadStatistics() },
                });

                Server.Connecting += UpdateCache;
                UpdateCache();

                Navigator.Manager.Initializing += () =>
                {
                    foreach (EntitySettings es in Navigator.Manager.EntitySettings.Values)
                    {
                        if (typeof(IdentifiableEntity).IsAssignableFrom(es.StaticType))
                            miAttachTypeEvent.GetInvoker(es.StaticType)(es);
                    }
                };

                Lite<DisconnectedMachineDN> current = null; 

                GetCurrentDisconnectedMachine = () =>
                {
                    if (current != null)
                        return current;

                    current = Server.Return((IDisconnectedServer s) => s.GetDisconnectedMachine(Environment.MachineName));

                    if (current == null)
                        throw new ApplicationException("No {0} found for '{1}'".Formato(typeof(DisconnectedMachineDN).NiceName(), Environment.MachineName));

                    return current;
                };
            }
        }

        static GenericInvoker<Action<EntitySettings>> miAttachTypeEvent = new GenericInvoker<Action<EntitySettings>>(es => AttachTypeEvent<TypeDN>((EntitySettings<TypeDN>)es));
        private static void AttachTypeEvent<T>(EntitySettings<T> settings) where T : IdentifiableEntity
        {
            settings.IsCreable += admin =>
            {
                if (OfflineMode)
                    return strategies[typeof(T)].Upload != Upload.None;
                else 
                    return true;
            };

            settings.IsReadOnly += (entity, admin) =>
            {
                var upload = strategies[typeof(T)].Upload;

                if (OfflineMode)
                {
                    if (upload == Upload.None)
                        return true;

                    if (entity == null || entity.IsNew)
                        return false;

                    if (upload == Upload.New)
                        return !entity.IsNew;

                    //Upload.Subset
                    return !entity.IsNew && !((IDisconnectedEntity)entity).DisconnectedMachine.Is(CurrentDisconnectedMachine);
                }
                else
                {
                    if (upload == Upload.New && entity != null)
                        return ((IDisconnectedEntity)entity).DisconnectedMachine != null;

                    return true;
                }
            }; 
        }

        static void UpdateCache()
        {
            if (OfflineMode)
            {
                strategies = Server.Return((IDisconnectedServer ds) => ds.GetStrategyPairs());
            }
        }

        public static void DownloadDatabase(Window owner)
        {
            if (MessageBox.Show("Downloading a database will take a while and close your session. Are you sure?", "Confirm database download", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                new DownloadDatabase { machine = DisconnectedClient.CurrentDisconnectedMachine }.ShowDialog();
            }
        }

    }
}
