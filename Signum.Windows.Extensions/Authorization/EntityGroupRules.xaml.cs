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
    public partial class EntityGroupRules : Window
    {
        public Lite<RoleDN> Role
        {
            get { return (Lite<RoleDN>)GetValue(RoleProperty); }
            set { SetValue(RoleProperty, value); }
        }

        public static readonly DependencyProperty RoleProperty =
            DependencyProperty.Register("Role", typeof(Lite<RoleDN>), typeof(EntityGroupRules), new UIPropertyMetadata(null));

        public EntityGroupRules()
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
            var result = Server.Return((IEntityGroupAuthServer s)=>s.GetEntityGroupAllowedRules(Role));
            DataContext = result;
            listView.ItemsSource = result.Rules.Select(r => new MutableEntityGroupRule(r)).ToList();
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            var result = (EntityGroupRulePack)DataContext;

            result.Rules = ((List<MutableEntityGroupRule>)listView.ItemsSource).Select(m => m.ToRule()).ToMList();

            Server.Execute((IEntityGroupAuthServer s) => s.SetEntityGroupAllowedRules(result));
            Load();
        }

        class AllowedRuleProxy
        {
            public AllowedRule<EntityGroupDN, EntityGroupAllowedDN> Rule { get; set; }
         
        }

        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            Close(); 
        }     

        private void btReload_Click(object sender, RoutedEventArgs e)
        {
            Load(); 
        }

    }

    public class MutableEntityGroupRule : EmbeddedEntity
    {
        TypeAllowed inGroupBase;
        TypeAllowed outGroupBase;

        TypeAllowed inGroup;
        public TypeAllowed InGroup
        {
            get { return inGroup; }
            set
            {
                if (Set(ref inGroup, value, () => InGroup))
                {
                    Notify(() => InOverriden);
                }
            }
        }

        TypeAllowed outGroup;
        public TypeAllowed OutGroup
        {
            get { return outGroup; }
            set
            {
                if (Set(ref outGroup, value, () => OutGroup))
                {
                    Notify(() => OutOverriden);
                }
            }
        }

        public bool InOverriden
        {
            get { return !inGroupBase.Equals(inGroup); }
        }

        public bool OutOverriden
        {
            get { return !outGroupBase.Equals(outGroup); }
        }

        EntityGroupDN resource;
        public EntityGroupDN Resource
        {
            get { return resource; }
            set { Set(ref resource, value, () => Resource); }
        }

        public MutableEntityGroupRule(EntityGroupAllowedRule rule)
        {
            this.inGroupBase = rule.AllowedBase.InGroup;
            this.outGroupBase = rule.AllowedBase.OutGroup;

            this.inGroup = rule.Allowed.InGroup;
            this.outGroup = rule.Allowed.OutGroup;

            this.resource = rule.Resource; 
        }

        public EntityGroupAllowedRule ToRule()
        {
            return new EntityGroupAllowedRule
            {
                Resource = Resource,
                AllowedBase = new EntityGroupAllowedDN(inGroupBase, outGroupBase),
                Allowed = new EntityGroupAllowedDN(inGroup, outGroup)
            };
        }
    }
}
