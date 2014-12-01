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
using System.Diagnostics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Reflection;
using System.ComponentModel;
using Signum.Entities.Reflection;
using Signum.Entities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class LightEntityLine : UserControl
    {
        public static readonly DependencyProperty EntityProperty =
            DependencyProperty.Register("Entity", typeof(object), typeof(LightEntityLine),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((LightEntityLine)d).UpdateVisibility()));
        public object Entity
        {
            get { return (object)GetValue(EntityProperty); }
            set { SetValue(EntityProperty, value); }
        }

        void UpdateVisibility()
        {
            if (Entity != null)
            {
                btNavigate.Visibility = Visibility.Visible;

                Type cleanType = Entity is Lite<IdentifiableEntity> ? ((Lite<IdentifiableEntity>)Entity).EntityType : Entity.GetType();

                btNavigate.IsEnabled = Navigator.IsNavigable(cleanType, isSearch: false);
            }
            else
            {
                btNavigate.Visibility = Visibility.Collapsed;
            }
        }

        public LightEntityLine()
        {
            InitializeComponent();
            UpdateVisibility();
        }

        private void btView_Click(object sender, RoutedEventArgs e)
        {
            Navigator.NavigateUntyped(this.Entity);
        }
    }
}
