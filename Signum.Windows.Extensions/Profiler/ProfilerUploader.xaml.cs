using Signum.Services;
using Signum.Utilities;
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

namespace Signum.Windows.Profiler
{
    /// <summary>
    /// Interaction logic for ProfilerUploader.xaml
    /// </summary>
    public partial class ProfilerUploader : Window
    {
        static ProfilerUploader Singletone;

        public static void OpenProfilerUploader()
        {
            if (Singletone != null)
                Singletone.Activate();
            else
                new ProfilerUploader().Show();
        }

        public ProfilerUploader()
        {
            InitializeComponent();
            this.Closed += ProfilerUploader_Closed;
            Singletone = this;
            UpdateInterface();
        }

        void ProfilerUploader_Closed(object sender, EventArgs e)
        {
            Singletone = null;
        }

        private void start_Click_1(object sender, RoutedEventArgs e)
        {
            HeavyProfiler.Clean();
            HeavyProfiler.Enabled = true;

            UpdateInterface();
        }


        private void stop_Click_1(object sender, RoutedEventArgs e)
        {
            HeavyProfiler.Enabled = false;

            var list = HeavyProfiler.Entries.ToList();

            foreach (var item in list)
                item.CleanStackTrace();

            Server.Execute((IProfilerServer ps) => ps.PushProfilerEntries(list));

            UpdateInterface();
        }


        private void UpdateInterface()
        {
            start.IsEnabled = !HeavyProfiler.Enabled;
            stop.IsEnabled = HeavyProfiler.Enabled;
        }
    }
}
