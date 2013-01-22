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
using Signum.Utilities;
using System.Threading.Tasks;
using Signum.Windows;

namespace Signum.Windows
{
    public partial class ProgressWindow : Window
    {
        public static readonly DependencyProperty MessageProperty =
          DependencyProperty.Register("Message", typeof(string), typeof(ProgressWindow), new UIPropertyMetadata(""));
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public ProgressWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Completed)
            {
                e.Cancel = true;
            }
        }

        public bool Completed { get; set; }

        public static void Wait(string title, string message, Action backgroundThread)
        {
            ProgressWindow rd = new ProgressWindow
            {
                Title = title,
                Message = message,
            };

            Async.Do(
                backgroundThread: backgroundThread,
                endAction: null,
                finallyAction: () =>
                {
                    rd.Completed = true;
                    rd.Close();
                });

            rd.ShowDialog();
        }
    }
}
