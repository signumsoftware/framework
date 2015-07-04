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
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Signum.Entities.Basics;

namespace Signum.Windows.Disconnected
{
    /// <summary>
    /// Interaction logic for DownloadProgress.xaml
    /// </summary>
    public partial class UploadProgress : Window
    {
        public UploadProgress()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(DownloadDatabase_Loaded);
            this.Closing += new System.ComponentModel.CancelEventHandler(UploadProgress_Closing);

            var a = DisconnectedMachineEntity.Current;
        }

        void UploadProgress_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Completed)
            {
                MessageBox.Show(this, "Operation can not be cancelled");

                e.Cancel = true;
            }
        }

        bool Completed = false; 

        DisconnectedImportEntity estimation;

        Lite<DisconnectedImportEntity> currentLite;

        IDisconnectedTransferServer transferServer = DisconnectedClient.GetTransferServer();

        DispatcherTimer timer = new DispatcherTimer();

        FileInfo fi = new FileInfo(DisconnectedClient.UploadBackupFile);

        void DownloadDatabase_Loaded(object sender, RoutedEventArgs e)
        {
            estimation = Server.Return((IDisconnectedServer ds) => ds.GetUploadEstimation(DisconnectedMachineEntity.Current));

            UploadDatabase().ContinueWith(cl =>
            {
                pbUploading.Dispatcher.BeginInvoke(() =>
                {
                    currentLite = cl.Result;

                    pbImporting.Minimum = 0;
                    pbImporting.Maximum = 1;

                    timer.Tick += new EventHandler(timer_Tick);

                    timer.Interval = TimeSpan.FromSeconds(1);

                    timer.Start();
                });
            });
        }

        private Task<Lite<DisconnectedImportEntity>> UploadDatabase()
        {
            pbUploading.Minimum = 0;
            pbUploading.Maximum = fi.Length;

            return Task.Factory.StartNew(() =>
            {
                using (FileStream fs = fi.OpenRead())
                using (ProgressStream ps = new ProgressStream(fs))
                {
                    ps.ProgressChanged += (s, args) => pbUploading.Dispatcher.BeginInvoke(() => pbUploading.Value = ps.Position);

                    UploadDatabaseResult result = transferServer.UploadDatabase(new UploadDatabaseRequest
                    {
                        FileName = fi.Name,
                        Length = fi.Length,
                        Stream = ps,
                        Machine = DisconnectedMachineEntity.Current,
                        User = UserHolder.Current.ToLite(),
                    });

                    return result.UploadStatistics;
                }
            });
        }

        void timer_Tick(object sender, EventArgs e)
        {
            var current = currentLite.RetrieveAndForget();

            ctrlStats.DataContext = null;
            ctrlStats.DataContext = current;

            if (current.State != DisconnectedImportState.InProgress)
            {
                timer.Stop();

                if (current.State == DisconnectedImportState.Error)
                {
                    expander.IsExpanded = true;
                    pbImporting.IsIndeterminate = false;

                    if (MessageBox.Show(Window.GetWindow(this), "There has been an error. View Details?", "Error importing database", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        Navigator.View(current.Exception);

                    Completed = true;
                    DialogResult = false;
                }
                else
                {
                    MessageBox.Show(Window.GetWindow(this), "You have successfully uploaded your local database, you can continue working.", "Upload complete", MessageBoxButton.OK);
                    Completed = true;
                    pbImporting.Value = 1; 
                    DialogResult = true;
                    
                }
            }
            else
            {
                if (estimation == null)
                    pbImporting.IsIndeterminate = true;
                else
                    pbImporting.Value = current.Ratio(estimation);
            }
        }
    }
}
