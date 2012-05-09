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

namespace Signum.Windows.Disconnected
{
    public static class DisconnectedClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<DisconnectedMachineDN>(EntityType.Admin) { View = dm => new DisconnectedMachine() },
                    new EntitySettings<DownloadStatisticsDN>(EntityType.ServerOnly) { View = dm => new DownloadStatistics() },
                    new EmbeddedEntitySettings<DownloadStatisticsTableDN>{ View = (dm, r) => new DownloadStatisticsTable() },
                });
            }
        }

        public static void DownloadDatabase(Window owner)
        {
            if (MessageBox.Show("Downloading a database will take a while and close your session. Are you sure?", "Confirm database download", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var machines = Server.Return((IDisconnectedServer ds) => ds.CurrentMachines());

                if (machines.IsEmpty())
                {
                    MessageBox.Show(owner,
                        "You need to create a {0} for the user {1}".Formato(typeof(DisconnectedMachineDN).NiceName(), UserDN.Current),
                        "No {0} found for the current user".Formato(typeof(DisconnectedMachineDN).NiceName()),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                var machine = machines.Only();

                if (machine == null)
                {
                    if (!SelectorWindow.ShowDialog(machines, out machine))
                        return;
                }

                new DownloadDatabase { machine = machine }.ShowDialog();
            }
        }



        public static Func<IDisconnectedTransferServer> GetTransferServer;

        public static string BackupFileName = "LocalDB.bak";
        public static string DatabaseFileName = "LocalDB.mdf";
    }
}
