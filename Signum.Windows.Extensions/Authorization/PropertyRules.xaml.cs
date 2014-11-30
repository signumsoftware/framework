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
using Signum.Entities.Basics;

namespace Signum.Windows.Authorization
{
    /// <summary>
    /// Interaction logic for Test.xaml
    /// </summary>
    public partial class PropertyRules : Window
    {
        public static readonly DependencyProperty TypeProperty =
         DependencyProperty.Register("Type", typeof(TypeEntity), typeof(PropertyRules), new UIPropertyMetadata(null));
        public TypeEntity Type
        {
            get { return (TypeEntity)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public static readonly DependencyProperty RoleProperty =
            DependencyProperty.Register("Role", typeof(Lite<RoleEntity>), typeof(PropertyRules), new UIPropertyMetadata(null));
        public Lite<RoleEntity> Role
        {
            get { return (Lite<RoleEntity>)GetValue(RoleProperty); }
            set { SetValue(RoleProperty, value); }
        }

        public PropertyRules()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Test_Loaded);
        }

        void Test_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = AuthMessage._0RulesFor1.NiceToString().FormatWith(typeof(PropertyRouteEntity).NiceName(), Role);
            Load();
        }

        private void Load()
        {
            DataContext = Server.Return((IPropertyAuthServer s)=>s.GetPropertyRules(Role, Type)); 
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            Server.Execute((IPropertyAuthServer s) => s.SetPropertyRules((PropertyRulePack)DataContext));
            Load();
        }

        private void btReload_Click(object sender, RoutedEventArgs e)
        {
            Load(); 
        }
    }
}
