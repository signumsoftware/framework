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
using System.Windows.Markup;
using System.ComponentModel;

namespace Signum.Windows.Authorization
{
    /// <summary>
    /// Interaction logic for Test.xaml
    /// </summary>
    public partial class TypeRules : Window
    {
        public Lite<RoleDN> Role
        {
            get { return (Lite<RoleDN>)GetValue(RoleProperty); }
            set { SetValue(RoleProperty, value); }
        }

        public bool Properties { get; set; }
        public bool Operations { get; set; }
        public bool Queries { get; set; }

        public static readonly DependencyProperty RoleProperty =
            DependencyProperty.Register("Role", typeof(Lite<RoleDN>), typeof(TypeRules  ), new UIPropertyMetadata(null));
        public TypeRules()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Test_Loaded);
        }

        ScrollViewer swTree; 

        void Test_Loaded(object sender, RoutedEventArgs e)
        {
            swTree = treeView.Child<ScrollViewer>(WhereFlags.VisualTree);
            grid.Bind(Grid.WidthProperty, swTree.Content, "ActualWidth");
            Load();
        }

        List<TypeAccessRule> rules;

        private void Load()
        {
            rules = Server.Return((ITypeAuthServer s) => s.GetTypesAccessRules(Role));

            Dictionary<string, List<TypeAccessRule>> nodes = rules.GroupToDictionary(a => TrimLastToken(a.Type.FullClassName));

            List<TypeAccessRule> empty = new List<TypeAccessRule>();

            List<Node<string>> root = TreeHelper.ToTreeC(nodes.Keys, TrimLastToken);

            Func<Node<string>, NamespaceNode> toNamespaceNode = null;

            toNamespaceNode = node => new NamespaceNode
            {
                Name = LastToken(node.Value),
                SubNodes = node.Children.Select(toNamespaceNode).OrderBy(ta => ta.Name).Cast<object>().Concat(
                          (nodes.TryGetC(node.Value) ?? empty).OrderBy(ta => ta.Type.FriendlyName).Cast<object>()).ToList()
            };

            treeView.ItemsSource = root.Select(toNamespaceNode).ToArray();
        }

        public DataTemplateSelector MyDataTemplateSelector;

  
        public string TrimLastToken(string typeOrNamespace)
        {
            int index = typeOrNamespace.LastIndexOf('.');
            if (index == -1)
                return null;
            return typeOrNamespace.Substring(0, index);
        }

        public string LastToken(string nameSpace)
        {
            if (nameSpace == null)
                return null;

            int index = nameSpace.LastIndexOf('.');
            if (index == -1)
                return nameSpace;

            return nameSpace.Substring(index + 1);
        }


        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            Server.Execute((ITypeAuthServer s) => s.SetTypesAccessRules(rules, Role)); 
            Load(); 
        }

        private void btReload_Click(object sender, RoutedEventArgs e)
        {
            Load(); 
        }

        private void properties_Click(object sender, RoutedEventArgs e)
        {
            TypeAccessRule rules = (TypeAccessRule)((Button)sender).DataContext;

            new PropertyRules
            {
                Owner = this,
                Type = rules.Type,
                Role = Role
            }.Show(); 
        }

        private void operations_Click(object sender, RoutedEventArgs e)
        {
            TypeAccessRule rules = (TypeAccessRule)((Button)sender).DataContext;

            new OperationRules
            {
                Owner = this,
                Type = rules.Type,
                Role = Role
            }.Show(); 
        }

        private void queries_Click(object sender, RoutedEventArgs e)
        {
            TypeAccessRule rules = (TypeAccessRule)((Button)sender).DataContext;

            new QueryRules
            {
                Owner = this,
                Type = rules.Type,
                Role = Role
            }.Show(); 
        }

        private void treeView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            swTop.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

    }

    public class NamespaceNode
    {
        public string Name { get; set; }
        public List<object> SubNodes { get; set; } //Will be TypeAccesRule or NamespaceNode

        public bool Expanded { get { return SubNodes.OfType<NamespaceNode>().Any(); } }
    }

}
