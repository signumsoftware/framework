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
using Signum.Utilities;

namespace Signum.Windows.Authorization
{
    /// <summary>
    /// Interaction logic for Test.xaml
    /// </summary>
    public partial class PermissionRules : Window
    {
        public Lite<RoleEntity> Role
        {
            get { return (Lite<RoleEntity>)GetValue(RoleProperty); }
            set { SetValue(RoleProperty, value); }
        }

        public static readonly DependencyProperty RoleProperty =
            DependencyProperty.Register("Role", typeof(Lite<RoleEntity>), typeof(PermissionRules), new UIPropertyMetadata(null));

        public PermissionRules()
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
            this.Title = AuthMessage._0RulesFor1.NiceToString().FormatWith(typeof(PermissionSymbol).NiceName(), Role);
            DataContext = Server.Return((IPermissionAuthServer s) => s.GetPermissionRules(Role));
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            Server.Execute((IPermissionAuthServer s) => s.SetPermissionRules((PermissionRulePack)DataContext)); 
            Load(); 
        }  

        private void btReload_Click(object sender, RoutedEventArgs e)
        {
            Load(); 
        }

    }
}
