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
using Signum.Entities.Basics;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace Signum.Windows.Authorization
{
    /// <summary>
    /// Interaction logic for Test.xaml
    /// </summary>
    public partial class TypeRules : Window
    {
        public static Type RuleType = typeof(TypeRuleBuilder);
        public static Type GroupType = typeof(NamespaceNode);
        public static Type ConditionType = typeof(TypeConditionRuleBuilder);
        
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


        private void Load()
        {
            TypeRulePack trp = Server.Return((ITypeAuthServer s) => s.GetTypesRules(Role));

            DataContext = trp;

            treeView.ItemsSource = (from r in trp.Rules
                                    group r by r.Resource.Namespace into g
                                    orderby g.Key
                                    select new NamespaceNode
                                    {
                                        Name = g.Key,
                                        SubNodes = g.OrderBy(a => a.Resource.ClassName).Select(a=>new TypeRuleBuilder(a)).ToList()
                                    }).ToList();
        }

        public DataTemplateSelector MyDataTemplateSelector;


        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            TypeRulePack trp = (TypeRulePack)DataContext;

            trp.Rules = ((List<NamespaceNode>)treeView.ItemsSource).SelectMany(a => a.SubNodes).Select(a => a.ToRule()).ToMList();

            Server.Execute((ITypeAuthServer s) => s.SetTypesRules(trp));
            Load();
        }

        private void btReload_Click(object sender, RoutedEventArgs e)
        {
            Load(); 
        }

        private void properties_Click(object sender, RoutedEventArgs e)
        {
            TypeRuleBuilder rules = (TypeRuleBuilder)((Button)sender).DataContext;

            new PropertyRules
            {
                Owner = this,
                Type = rules.Resource,
                Role = Role
            }.Show(); 
        }

        private void operations_Click(object sender, RoutedEventArgs e)
        {
            TypeRuleBuilder rules = (TypeRuleBuilder)((Button)sender).DataContext;

            new OperationRules
            {
                Owner = this,
                Type = rules.Resource,
                Role = Role
            }.Show(); 
        }

        private void queries_Click(object sender, RoutedEventArgs e)
        {
            TypeRuleBuilder rules = (TypeRuleBuilder)((Button)sender).DataContext;

            new QueryRules
            {
                Owner = this,
                Type = rules.Resource,
                Role = Role
            }.Show(); 
        }

        private void treeView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            swTop.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private void addCondition_Click(object sender, RoutedEventArgs e)
        {
            TypeRuleBuilder rules = (TypeRuleBuilder)((Button)sender).DataContext;
            
            Enum value;
            if (SelectorWindow.ShowDialog<Enum>(
                rules.AvailableConditions.Except(rules.Conditions.Select(a => a.ConditionName)).ToArray(), 
                out value, 
                elementIcon: null,
                elementText: v => v.NiceToString(),
                title: "New condition", 
                message: "Select the condition for {0} to add specific authorization rules".Formato(rules.Resource.CleanName), 
                owner: this))
            {
                rules.Conditions.Add(new TypeConditionRuleBuilder(value, rules.Allowed.None ? TypeAllowed.Create : TypeAllowed.None)); 
            }
        }

        private void removeCondition_Click(object sender, RoutedEventArgs e)
        {
            var node = ((Button)sender).VisualParents().OfType<TreeViewItem>().FirstEx();
            var parentNode = node.VisualParents().Skip(1).OfType<TreeViewItem>().FirstEx();

            var rules = (TypeRuleBuilder)parentNode.DataContext;
            var condition = (TypeConditionRuleBuilder)node.DataContext;
            rules.Conditions.Remove(condition); 
        }

        private void btFind_Click(object sender, RoutedEventArgs e)
        {
            if (tbType.Text.IsEmpty())
                return;
            Find(this.treeView, tbType.Text);
        }

        private void btRemove_Click(object sender, RoutedEventArgs e)
        {
            CleanSelection(this.treeView);
        }

        private void CleanSelection(TreeView tree)
        {
            if (tree == null)
                return;
            tbType.IsReadOnly = false;
            tbType.Text = "";

            foreach (var item in tree.Items)
            {
                var i = item as NamespaceNode;
                if (i != null)
                {
                    i.Selected = false;
                    foreach (var subitem in i.SubNodes)
                    {
                            subitem.Selected = false;
                    }
                }
            }
        }

        private void Find(TreeView tree, string key)
        {
            if (tree == null)
                return;
            tbType.IsReadOnly = true;
            foreach (var item in tree.Items)
            {
                var i = item as NamespaceNode;
                if (i != null)
                { 
                    if(i.Name.ToUpper().Contains(key.Trim().ToUpper()))
                        i.Selected = true;

                    foreach (var subitem in i.SubNodes)
                    {
                        if (subitem.Resource.ClassName.ToUpper().Contains(key.Trim().ToUpper()))
                            subitem.Selected = true;
                    }
                }
            }
        }

        private void tbType_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CleanSelection(this.treeView);
                e.Handled = true;
            }
            else
                if (e.Key == Key.Enter)
                {
                    Find(this.treeView, tbType.Text);
                    e.Handled = true;
                }
        }
    }

    public class NamespaceNode : ModelEntity
    {
        public string Name { get; set; }

        bool selected;
        public bool Selected
        {
            get { return selected; }
            set { Set(ref selected, value, () => Selected); }
        }

        public List<TypeRuleBuilder> SubNodes { get; set; } //Will be TypeAccesRule or NamespaceNode
    }

    public class TypeRuleBuilder : ModelEntity
    {
        bool selected;
        public bool Selected
        {
            get { return selected; }
            set { Set(ref selected, value, () => Selected); }
        }


        [NotifyChildProperty]
        TypeAllowedBuilder allowed;
        public TypeAllowedBuilder Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value, () => Allowed); }
        }

        TypeAllowedAndConditions allowedBase;
        public TypeAllowedAndConditions AllowedBase
        {
            get { return allowedBase; }
        }

        [NotifyChildProperty, NotifyCollectionChanged]
        MList<TypeConditionRuleBuilder> conditions = new MList<TypeConditionRuleBuilder>();
        public MList<TypeConditionRuleBuilder> Conditions
        {
            get { return conditions; }
            set { Set(ref conditions, value, () => Conditions); }
        }

        ReadOnlyCollection<Enum> availableConditions;
        public ReadOnlyCollection<Enum> AvailableConditions
        {
            get { return availableConditions; }
            set { Set(ref availableConditions, value, () => AvailableConditions); }
        }

        public TypeDN Resource { get; set; }

        public AuthThumbnail? Properties { get; set; }
        public AuthThumbnail? Operations { get; set; }
        public AuthThumbnail? Queries { get; set; }

        public TypeRuleBuilder(TypeAllowedRule rule)
        {
            this.allowed = new TypeAllowedBuilder(rule.Allowed.Fallback);
            this.conditions = rule.Allowed.Conditions.Select(c => new TypeConditionRuleBuilder(c.ConditionName, c.Allowed)).ToMList();
            this.availableConditions = rule.AvailableConditions;
            this.allowedBase = rule.AllowedBase; 
            this.Resource = rule.Resource;

            this.Properties = rule.Properties;
            this.Operations = rule.Operations;
            this.Queries = rule.Queries;

            this.RebindEvents();
        }

        protected override void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Notify(() => Overriden);

            base.ChildPropertyChanged(sender, e);
        }

        protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            Notify(() => Overriden);
            Notify(() => CanAdd);

            base.ChildCollectionChanged(sender, args);
        }

        public TypeAllowedRule ToRule()
        {
            return new TypeAllowedRule
            {
                Resource = Resource,

                AllowedBase = AllowedBase,
                Allowed = new TypeAllowedAndConditions(Allowed.TypeAllowed, 
                    Conditions.Select(a=>new TypeConditionRule(a.ConditionName, a.Allowed.TypeAllowed)).ToReadOnly()),

                AvailableConditions = this.AvailableConditions,

                Properties = Properties,
                Operations = Operations,
                Queries = Queries,
            };
        }

    

        public bool Overriden
        {
            get 
            {
                if (!allowedBase.Fallback.Equals(Allowed.TypeAllowed))
                    return true;

                return !allowedBase.Conditions.SequenceEqual(Conditions.Select(a => 
                    new TypeConditionRule(a.ConditionName, a.Allowed.TypeAllowed)));
            }
        }

        public bool CanAdd
        {
            get { return availableConditions.Except(Conditions.Select(a => a.ConditionName)).Any(); }
        }
    }

    public class TypeConditionRuleBuilder : ModelEntity
    {
        [NotifyChildProperty]
        TypeAllowedBuilder allowed;
        public TypeAllowedBuilder Allowed
        {
            get { return allowed; }
            private set { Set(ref allowed, value, () => Allowed); }
        }

        public Enum ConditionName { get; private set; }

        public string ConditionNiceToString { get { return ConditionName.NiceToString(); } }

        public TypeConditionRuleBuilder(Enum conditionName, TypeAllowed allowed)
        {
            this.ConditionName = conditionName;
            this.Allowed = new TypeAllowedBuilder(allowed);
        }

        protected override void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Notify(() => Allowed);
        }
    }

    public class TypeAllowedBuilder : ModelEntity
    {
        public TypeAllowedBuilder(TypeAllowed typeAllowed)
        {
            this.typeAllowed = typeAllowed;
        }

        TypeAllowed typeAllowed;
        public TypeAllowed TypeAllowed { get { return typeAllowed; } }

        public bool Create
        {
            get { return typeAllowed.IsActive(TypeAllowedBasic.Create); }
            set { Set(TypeAllowedBasic.Create); }
        }

        public bool Modify
        {
            get { return typeAllowed.IsActive(TypeAllowedBasic.Modify); }
            set { Set(TypeAllowedBasic.Modify); }
        }

        public bool Read
        {
            get { return typeAllowed.IsActive(TypeAllowedBasic.Read); }
            set { Set(TypeAllowedBasic.Read); }
        }

        public bool None
        {
            get { return typeAllowed.IsActive(TypeAllowedBasic.None); }
            set { Set(TypeAllowedBasic.None); }
        }

        public int GetNum()
        {
            return (Create ? 1 : 0) + (Modify ? 1 : 0) + (Read ? 1 : 0) + (None ? 1 : 0);
        }

        private void Set(TypeAllowedBasic typeAllowedBasic)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                typeAllowed = TypeAllowedExtensions.Create(typeAllowedBasic, typeAllowedBasic);
            }
            else
            {
                int num = GetNum();
                if (!typeAllowed.IsActive(typeAllowedBasic) && num == 1)
                {
                    var db = typeAllowed.GetDB();
                    typeAllowed = TypeAllowedExtensions.Create(
                        db > typeAllowedBasic ? db : typeAllowedBasic,
                        db < typeAllowedBasic ? db : typeAllowedBasic);
                }
                else if (typeAllowed.IsActive(typeAllowedBasic) && num >= 2)
                {
                    var other = typeAllowed.GetDB() == typeAllowedBasic ? typeAllowed.GetDB() : typeAllowed.GetUI();
                    typeAllowed = TypeAllowedExtensions.Create(other, other);
                }
            }

            Notify(() => Create);
            Notify(() => Modify);
            Notify(() => Read);
            Notify(() => None);
            Notify(() => TypeAllowed);
        }
    }
    public static class TypeRuleConverter
    {
        public static readonly IValueConverter BoolToBlueBackground = ConverterFactory.New(
            (bool value) => value ? Brushes.Blue : Brushes.Transparent);

    }
}
