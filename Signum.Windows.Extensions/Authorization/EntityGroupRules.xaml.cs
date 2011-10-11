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
using System.Windows.Threading;

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
            listView.ItemsSource = result.Rules.Select(r => new EntityGroupRuleBuilderDN(r.Allowed){ Priority = r.Priority, Resource = r.Resource}).ToList();
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            var result = (EntityGroupRulePack)DataContext;

            result.Rules = ((List<EntityGroupRuleBuilderDN>)listView.ItemsSource).Select(m => new EntityGroupAllowedRule
            {
                Allowed = m.Allowed.TypeAllowed,
                Priority = m.Priority,
                Resource = m.Resource,
            }).ToMList();

            Server.Execute((IEntityGroupAuthServer s) => s.SetEntityGroupAllowedRules(result));
            Load();
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

    public class EntityGroupRuleBuilderDN
    {
        public EntityGroupRuleBuilderDN(TypeAllowed allowed)
        {
            this.Allowed = new TypeAllowedBuilderDN(allowed);
        }

        public TypeAllowedBuilderDN Allowed { get; private set; }

        public int Priority { get; set; }

        public Enum Resource { get; set; }
    }

    public class TypeAllowedBuilderDN : ModelEntity
    {
        public TypeAllowedBuilderDN( TypeAllowed typeAllowed)
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
}
