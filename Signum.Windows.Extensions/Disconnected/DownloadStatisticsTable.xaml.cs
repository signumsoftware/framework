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
using Signum.Entities.Disconnected;

namespace Signum.Windows.Disconnected
{
    /// <summary>
    /// Interaction logic for DownloadStatisticsTable.xaml
    /// </summary>
    public partial class DownloadStatisticsTable : UserControl
    {
        public DownloadStatisticsTable()
        {
            InitializeComponent();
            Common.SetDelayedRoutes(this, true); 
        }
        
        public DownloadStatisticsTable(PropertyRoute route)
        {
            InitializeComponent();
            Common.SetTypeContext(this, route);
        }
    }
}
