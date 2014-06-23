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
using Signum.Entities.Dashboard;
using System.Diagnostics;
using Signum.Utilities;

namespace Signum.Windows.Dashboard
{
    /// <summary>
    /// Interaction logic for UserQueryPart.xaml
    /// </summary>
    public partial class LinkListPartView : UserControl
    {
        public LinkListPartView()
        {
            InitializeComponent();
        }

        private void Hyperlink_Click_1(object sender, RoutedEventArgs e)
        {
            string url = ((Hyperlink)sender).NavigateUri.ToString();
            if (System.Environment.OSVersion.NiceWindowsVersion() == "Windows 8")
                Process.Start(new ProcessStartInfo("explorer.exe", url)); // Problems with chrome
            else
                Process.Start(new ProcessStartInfo(url));
            e.Handled = true;
        }
    }
}
