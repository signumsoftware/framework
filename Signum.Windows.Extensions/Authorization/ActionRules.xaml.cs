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
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Services;

namespace Signum.Windows.Authorization
{
    /// <summary>
    /// Interaction logic for Test.xaml
    /// </summary>
    public partial class ActionRules : Window
    {
        public Lazy<RoleDN> Role
        {
            get { return (Lazy<RoleDN>)GetValue(RoleProperty); }
            set { SetValue(RoleProperty, value); }
        }

        public static readonly DependencyProperty RoleProperty =
            DependencyProperty.Register("Role", typeof(Lazy<RoleDN>), typeof(ActionRules), new UIPropertyMetadata(null));

        public ActionRules()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Test_Loaded);
        }

        void Test_Loaded(object sender, RoutedEventArgs e)
        {
            Load();
        }

        private void Load()
        {
            listView.ItemsSource = Server.Service<IActionAuthServer>().GetActionAllowedRules(Role);
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            Server.Service<IActionAuthServer>().SetActionAllowedRules((List<AllowedRule>)listView.ItemsSource, Role);
            Load(); 
        }  

        private void btReload_Click(object sender, RoutedEventArgs e)
        {
            Load(); 
        }

    }
}
