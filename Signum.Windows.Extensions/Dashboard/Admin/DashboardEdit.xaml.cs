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
using Signum.Utilities;
using Signum.Windows.Basics;

namespace Signum.Windows.Dashboard.Admin
{
    /// <summary>
    /// Interaction logic for DashboardEdit.xaml
    /// </summary>
    public partial class DashboardEdit : UserControl
    {
        public DashboardDN Panel
        {
            get { return (DashboardDN)DataContext; }
        }

        public DashboardEdit()
        {
            InitializeComponent();
        }

        private object EntityRepeater_Creating()
        {
            var imp = (Implementations)PropertyRoute.Construct((DashboardDN cp) => cp.Parts.First().Content).GetImplementations();

            var type = Navigator.SelectType(Window.GetWindow(this), imp.Types, t => Navigator.IsCreable(t, isSearch: false));

            if (type == null)
                return null;

            return new PanelPartDN
            {
                Row = Panel.Parts.IsEmpty() ? 0 : Panel.Parts.Max(a => Math.Max(a.Row, 0)) + 1,
                Columns = 12,
                StartColumn = 0,
                Content = (IPartDN)new ConstructorContext(this).ConstructUntyped(type),
                Title = null,
            };
        }

        IEnumerable<Lite<IdentifiableEntity>> EntityType_AutoCompleting(string text)
        {
            return TypeClient.ViewableServerTypes().Where(t => t.CleanName.Contains(text, StringComparison.InvariantCultureIgnoreCase)).Select(t => t.ToLite()).Take(5);
        }
    }
}
