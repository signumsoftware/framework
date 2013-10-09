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
using Signum.Entities.ControlPanel;
using Signum.Utilities;
using Signum.Windows.Basics;

namespace Signum.Windows.ControlPanels.Admin
{
    /// <summary>
    /// Interaction logic for ControlPanelEdit.xaml
    /// </summary>
    public partial class ControlPanelEdit : UserControl
    {
        public ControlPanelDN Panel
        {
            get { return (ControlPanelDN)DataContext; }
        }

        public ControlPanelEdit()
        {
            InitializeComponent();
        }

        private object EntityRepeater_Creating()
        {
            var imp = (Implementations)PropertyRoute.Construct((ControlPanelDN cp) => cp.Parts.First().Content).GetImplementations();

            var type = Navigator.SelectType(Window.GetWindow(this), imp.Types, t => Navigator.IsCreable(t, isSearchEntity: false));

            if (type == null)
                return null;

            var lastColumn = 0.To(Panel.NumberOfColumns).WithMin(c => Panel.Parts.Count(p => p.Column == c));

            return new PanelPartDN
            {
                Column = lastColumn,
                Row = (Panel.Parts.Where(a => a.Column == lastColumn).Max(a => (int?)a.Row + 1) ?? 0),
                Content = (IPartDN)Constructor.Construct(type, this),
                Title = null,
            };
        }

        IEnumerable<Lite<IdentifiableEntity>> EntityType_AutoCompleting(string text)
        {
            return TypeClient.ViewableServerTypes().Where(t => t.CleanName.Contains(text, StringComparison.InvariantCultureIgnoreCase)).Select(t => t.ToLite()).Take(5);
        }
    }
}
