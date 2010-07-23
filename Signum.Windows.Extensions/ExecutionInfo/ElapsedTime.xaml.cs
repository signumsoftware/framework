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
using Signum.Utilities;

namespace Signum.Windows.ExecutionInfo
{
    /// <summary>
    /// Interaction logic for ElapsedTime.xaml
    /// </summary>
    public partial class ElapsedTime : UserControl
    {
        public ElapsedTime()
        {
            InitializeComponent();

            List<ElapsedNode> nodes = new List<ElapsedNode>();

            foreach (KeyValuePair<string, ElapsedTimeEntry> pair in Signum.Utilities.ElapsedTime.IdentifiedElapseds.OrderByDescending(p => p.Value.Average))
            {
                nodes.Add(new ElapsedNode
                {
                    Name = pair.Key.Split(' ')[0],
                    EntityName = pair.Key.Split(' ')[1],
                    ElapsedTimeEntry = pair.Value
                });
            }
            lvTiempos.ItemsSource = nodes;
        }
    }

    class ElapsedNode
    {
        public string Name { get; set; }
        public string EntityName { get; set; }
        public ElapsedTimeEntry ElapsedTimeEntry { get; set; }
    }
}
