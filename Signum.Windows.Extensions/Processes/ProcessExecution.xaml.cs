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
    /// Interaction logic for ProcessExecution.xaml
    /// </summary>
    public partial class ProcessExecution : UserControl
    {
        DispatcherTimer timer;

        public ProcessExecution()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.IsEnabled = false;
            timer.Tick += new EventHandler(timer_Tick);
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ProcessExecution_DataContextChanged);
            this.Unloaded += ProcessExecution_Unloaded;
        }

        void ProcessExecution_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.IsEnabled = false;
            timer = null; 
        }

        void timer_Tick(object sender, EventArgs e)
        {
            ProcessExecutionDN pe = DataContext as ProcessExecutionDN;
            if (pe != null && (pe.State == ProcessState.Queued || pe.State == ProcessState.Executing || pe.State == ProcessState.Suspending))
            {
                ProcessExecutionDN npe = pe.ToLite().RetrieveAndForget();
                RaiseEvent(new ChangeDataContextEventArgs(npe));
            }
        }

        void ProcessExecution_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ProcessExecutionDN pe = e.NewValue as ProcessExecutionDN;
            timer.IsEnabled = pe != null && (pe.State == ProcessState.Queued || pe.State == ProcessState.Executing || pe.State == ProcessState.Suspending); 
        }
    }
}
