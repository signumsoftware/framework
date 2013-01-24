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
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Services; 

namespace Signum.Windows.Basics
{
    /// <summary>
    /// Interaction logic for Usuario.xaml
    /// </summary>
    public partial class DateSpan : UserControl
    {
        public DateSpan(PropertyRoute tc)
        {
            Common.SetPropertyRoute(this, tc); 
            InitializeComponent();
        }

        public DateSpan()
        {
            Common.SetDelayedRoutes(this, true);
            InitializeComponent();
        }

        Lite<RoleDN> Lite
        {
            get { return ((RoleDN)DataContext).ToLite(); }
        }
   }
}
