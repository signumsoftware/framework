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
using Signum.Entities.Reflection;

namespace Signum.Windows.Disconnected
{
    public static class DisconnectedClient
    {
        public static Func<IDisconnectedTransferServer> GetTransferServer;

        public static string DownloadBackupFile = "LocalDB.Download.bak";
        public static string UploadBackupFile = "LocalDB.Upload.bak";
        public static string DatabaseFile = "LocalDB.mdf";
        public static string DatabaseLogFile = "LocalDB_log.ldf";

        static Dictionary<Type, StrategyPair> strategies;

        public static DisconnectedExportEntity LastExport;
     

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<DisconnectedMachineEntity> { View = dm => new DisconnectedMachine() },
                    new EntitySettings<DisconnectedExportEntity> { View = dm => new DisconnectedExport() },
                    new EntitySettings<DisconnectedImportEntity> { View = dm => new DisconnectedImport() },
                });

                Server.Connecting += UpdateCache;
                UpdateCache();

                Navigator.Manager.IsReadOnly += (type, entity) => entity is Entity && !Editable((Entity)entity, type);
                
                Navigator.Manager.IsCreable += type =>
                {
                    if (!type.IsEntity())
                        return true;

                    if (!Server.OfflineMode)
                        return true;

                    return strategies[type].Upload != Upload.None;
                }; 

                Lite<DisconnectedMachineEntity> current = null; 

                DisconnectedMachineEntity.CurrentVariable.ValueFactory = () =>
                {
                    if (current != null)
                        return current;

                    current = Server.Return((IDisconnectedServer s) => s.GetDisconnectedMachine(Environment.MachineName));

                    if (current == null)
                        throw new ApplicationException("No {0} found for '{1}'".FormatWith(typeof(DisconnectedMachineEntity).NiceName(), Environment.MachineName));

                    return current;
                };
            }
        }

        private static bool Editable(Entity entity, Type type)
        {
            Upload upload = strategies[type].Upload;

            if (Server.OfflineMode)
            {
                if (upload == Upload.None)
                    return false;

                if (entity == null)
                    return true;

                if (entity.IsNew || entity.Mixin<DisconnectedCreatedMixin>().DisconnectedCreated)
                    return true;

                if (upload == Upload.Subset)
                {
                    var dm =  entity.Mixin<DisconnectedSubsetMixin>();

                    return dm.DisconnectedMachine.Is(DisconnectedMachineEntity.Current);
                }

                return false;
            }
            else
            {
                if (upload == Upload.Subset && entity != null)
                    return entity.Mixin<DisconnectedSubsetMixin>().DisconnectedMachine == null;

                return true;
            }
        }

        static void UpdateCache()
        {
            strategies = Server.Return((IDisconnectedServer ds) => ds.GetStrategyPairs());
        }
    }
}
