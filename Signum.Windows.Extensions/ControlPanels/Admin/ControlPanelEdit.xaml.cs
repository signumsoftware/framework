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
using Signum.Entities.Exceptions;
using Signum.Entities.ControlPanel;

namespace Signum.Windows.ControlPanels
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
            var win = Window.GetWindow(this);

            var imp = (ImplementedByAttribute)PropertyRoute.Construct<ControlPanelDN>(cp => cp.Parts.First().Content).GetImplementations();

            var type = Navigator.SelectType(win, imp.ImplementedTypes);

            if (type == null)
                return null;

            var lastColumn = Panel.NumberOfColumns - 1;

            return new PanelPart
            {
                Column = lastColumn,
                Row = (Panel.Parts.Where(a => a.Column == lastColumn).Max(a => (int?)a.Row) ?? 0) + 1,
                Content = (IIdentifiable)Constructor.Construct(type, win),
                Title = null,
            };
        }
    }
}
