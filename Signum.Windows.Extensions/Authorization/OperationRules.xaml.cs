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
using Signum.Entities.Basics;

namespace Signum.Windows.Authorization
{
    /// <summary>
    /// Interaction logic for Test.xaml
    /// </summary>
    public partial class OperationRules : Window
    {
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(TypeDN), typeof(OperationRules), new UIPropertyMetadata(null));
        public TypeDN Type
        {
            get { return (TypeDN)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public Lite<RoleDN> Role
        {
            get { return (Lite<RoleDN>)GetValue(RoleProperty); }
            set { SetValue(RoleProperty, value); }
        }

        public static readonly DependencyProperty RoleProperty =
            DependencyProperty.Register("Role", typeof(Lite<RoleDN>), typeof(OperationRules), new UIPropertyMetadata(null));

        public OperationRules()
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
            DataContext = Server.Return((IOperationAuthServer s) => s.GetOperationRules(Role, Type));
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            Server.Execute((IOperationAuthServer s) => s.SetOperationRules((OperationRulePack)DataContext));
            Load();
        }

        private void btReload_Click(object sender, RoutedEventArgs e)
        {
            Load();
        }
    }
}
