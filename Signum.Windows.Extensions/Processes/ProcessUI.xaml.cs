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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Windows;
using Signum.Entities;
using Signum.Entities.Processes;
using System.Windows.Threading;

namespace Signum.Windows.Processes
{
    /// <summary>
    /// Interaction logic for ProcessUI.xaml
    /// </summary>
    public partial class ProcessUI : UserControl
    {
        DispatcherTimer timer;

        public ProcessUI()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(.5);
            timer.IsEnabled = false;
            timer.Tick += new EventHandler(timer_Tick);
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(Process_DataContextChanged);
            this.Unloaded += Process_Unloaded;
        }

        void Process_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.IsEnabled = false;
            timer = null; 
        }

        void timer_Tick(object sender, EventArgs e)
        {
            ProcessDN pe = DataContext as ProcessDN;
            if (pe != null && (pe.State == ProcessState.Queued || pe.State == ProcessState.Executing || pe.State == ProcessState.Suspending))
            {
                ProcessDN npe = pe.ToLite().RetrieveAndForget();
                RaiseEvent(new ChangeDataContextEventArgs(npe));
            }
        }

        void Process_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ProcessDN pe = e.NewValue as ProcessDN;
            timer.IsEnabled = pe != null && (pe.State == ProcessState.Queued || pe.State == ProcessState.Executing || pe.State == ProcessState.Suspending); 
        }
    }
}
