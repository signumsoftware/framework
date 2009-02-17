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
using Bugs.Entities;

namespace Bugs.Windows.Controls
{
    /// <summary>
    /// Interaction logic for CustomerDN.xaml
    /// </summary>
    public partial class Project : UserControl, IHaveQuickLinks
    {
        public Project()
        {
            InitializeComponent();
        }

        public List<QuickLink> QuickLinks()
        {
            return new List<QuickLink>
            {
                new QuickLink("Related Bugs")
                {
                     Action = ()=> Navigator.Find(typeof(BugDN), "Project", DataContext)
                }
            };
        }
    }
}
