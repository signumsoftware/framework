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

        public static string DownloadBackupFile = "LocalDB.Download.bak";
        public static string UploadBackupFile = "LocalDB.Upload.bak";
        public static string DatabaseFile = "LocalDB.mdf";
        public static string DatabaseLogFile = "LocalDB_log.ldf";

        static Dictionary<Type, StrategyPair> strategies;

        public static DisconnectedExportDN LastExport;
     

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<DisconnectedMachineDN> { View = dm => new DisconnectedMachine() },
                    new EntitySettings<DisconnectedExportDN> { View = dm => new DisconnectedExport() },
                    new EntitySettings<DisconnectedImportDN> { View = dm => new DisconnectedImport() },
                });

                Server.Connecting += UpdateCache;
                UpdateCache();

                Navigator.Manager.IsReadOnly += (type, entity) => entity is IdentifiableEntity && !Editable((IdentifiableEntity)entity, type);

                Navigator.Manager.IsCreable += type =>
                {
                    if (Server.OfflineMode)
                        return strategies[type].Upload != Upload.None;
                    else
                        return true;
                }; 

                Lite<DisconnectedMachineDN> current = null; 

                DisconnectedMachineDN.CurrentVariable.ValueFactory = () =>
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

        private static bool Editable(IdentifiableEntity entity, Type type)
        {
            Upload upload = strategies[type].Upload;

            if (Server.OfflineMode)
            {
                if (upload == Upload.None)
                    return false;

                if (entity == null)
                    return true;

                if (entity.IsNew)
                    return true;

                if (upload == Upload.Subset)
                {
                    var dm =  entity.Mixin<DisconnectedMixin>();

                    if(dm.DisconnectedMachine != null)
                        return dm.DisconnectedMachine.Is(DisconnectedMachineDN.Current);
                }

                return DisconnectedExportRanges.InModifiableRange(type, entity.Id);
            }
            else
            {
                if (upload == Upload.Subset && entity != null)
                    return entity.Mixin<DisconnectedMixin>().DisconnectedMachine == null;

                return true;
            }
        }

        static void UpdateCache()
        {
            strategies = Server.Return((IDisconnectedServer ds) => ds.GetStrategyPairs());
        }
    }
}
