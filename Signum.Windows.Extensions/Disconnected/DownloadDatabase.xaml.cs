using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Signum.Entities.Disconnected;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Authorization;
using System.Windows.Threading;
using System.IO;
using System.IO.Compression;

namespace Signum.Windows.Disconnected
{
    /// <summary>
    /// Interaction logic for DownloadDatabase.xaml
    /// </summary>
    public partial class DownloadDatabase : Window
    {
        public DownloadDatabase()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(DownloadDatabase_Loaded);
        }

        DownloadStatisticsDN estimation;

        Lite<DownloadStatisticsDN> currentLite;

        internal Lite<DisconnectedMachineDN> machine;

        IDisconnectedTransferServer transferServer = DisconnectedClient.GetTransferServer();

        DispatcherTimer timer = new DispatcherTimer();

        void DownloadDatabase_Loaded(object sender, RoutedEventArgs e)
        {
            estimation = Server.Return((IDisconnectedServer ds) => ds.GetDownloadEstimation(machine));

            if (estimation != null)
            {
                pbDownloading.Minimum = 0;
                pbDownloading.Maximum = 1;
            }


            currentLite = transferServer.BeginExportDatabase(machine);

            timer.Tick += new EventHandler(timer_Tick);

            timer.Interval = TimeSpan.FromSeconds(1);
         
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            var current = currentLite.RetrieveAndForget();

            ctrlStats.DataContext = null;
            ctrlStats.DataContext = current;

            if (current.State != DownloadStatisticsState.InProgress)
            {
                timer.Stop();

                if (current.State == DownloadStatisticsState.Error)
                {
                    this.Background = Brushes.Pink;

                    if (MessageBox.Show(Window.GetWindow(this), "There have been an error. View Details?", "Error generating database", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        Navigator.View(current.Exception);

                    Close();
                }
                else
                {
                    StartDownloading();
                }
            }
            else
            {
                if (estimation == null)
                    pbGenerating.IsIndeterminate = true;
                else
                    pbGenerating.Value = current.Ratio(estimation);
            }
        }

        private void StartDownloading()
        {
            var file = transferServer.EndExportDatabase(new DownloadDatabaseRequests { DownloadStatistics = currentLite });

            pbDownloading.Minimum = 0;
            pbDownloading.Maximum = file.Length;

            using (var ps = new ProgresssStream(new DeflateStream(file.Stream, CompressionMode.Decompress)))
            {
                ps.ProgressChanged += (s, args) => pbDownloading.Value = ps.Position;

                using (FileStream fs = File.OpenWrite(DisconnectedClient.BackupFileName))
                    ps.CopyTo(fs);
            }

            MessageBox.Show(Window.GetWindow(this), "You have sucesfully downloaded a local database. \r\nThe application will turn off.\r\nChoose LocalDB ", "Download complete", MessageBoxButton.OK);

            Environment.Exit(0);
        }
    }
}
