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
using Signum.Entities;
using System.Collections;
using Signum.Entities.Reflection;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class EntityCombo : EntityBase
    {
        public static readonly DependencyProperty LoadDataTriggerProperty =
            DependencyProperty.Register("LoadDataTrigger", typeof(LoadDataTrigger), typeof(EntityCombo), new UIPropertyMetadata(LoadDataTrigger.OnExpand));
        public LoadDataTrigger LoadDataTrigger
        {
            get { return (LoadDataTrigger)GetValue(LoadDataTriggerProperty); }
            set { SetValue(LoadDataTriggerProperty, value); }
        }

        public event Func<IEnumerable<Lazy>> LoadData;

        static EntityCombo()
        {
            RemoveProperty.OverrideMetadata(typeof(EntityCombo), new FrameworkPropertyMetadata(false));
            FindProperty.OverrideMetadata(typeof(EntityCombo), new FrameworkPropertyMetadata(false));
        }

        protected override void UpdateVisibility()
        {
            btCreate.Visibility = CanCreate() ? Visibility.Visible : Visibility.Collapsed;
            btFind.Visibility = CanFind() ? Visibility.Visible : Visibility.Collapsed;
            btView.Visibility = CanView() ? Visibility.Visible : Visibility.Collapsed;
            btRemove.Visibility = CanRemove() ? Visibility.Visible : Visibility.Collapsed;
        }

        bool changing = false;
        protected override void OnEntityChanged(object oldValue, object newValue)
        {
            base.OnEntityChanged(oldValue, newValue);

            if (changing) return;
            try
            {
                changing = true;

                object newEntity = CleanLazy ? Entity : Server.Convert(Entity, Reflector.GenerateLazy(CleanType ?? EntityType));

                if (!isLoaded)
                    combo.Items.Add(newEntity);

                combo.SelectedItem = newEntity;
            }
            finally
            {
                changing = false;
            }
        }

        private void combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changing) return;
            try
            {
                changing = true;

                SetEntityUserInteraction(Server.Convert(combo.SelectedItem, EntityType));
            }
            finally
            {
                changing = false;
            }
        }
     
        public EntityCombo()
        {
            InitializeComponent();
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(EntityCombo_DataContextChanged);
        }

        void EntityCombo_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (isLoaded)
            {
                isLoaded = false;
                OnLoadData(this, null);
            }
        }

        public override void OnLoad(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            base.OnLoad(sender, e);

            combo.IsEnabled = !Common.GetIsReadOnly(this);

            if (LoadDataTrigger == LoadDataTrigger.OnLoad)
                OnLoadData(sender, e);
            else
                combo.DropDownOpened += new EventHandler(OnLoadData);
        }

        bool isLoaded = false;
        void OnLoadData(object sender, EventArgs e)
        {
            if (isLoaded || DesignerProperties.GetIsInDesignMode(this)) return;

            IEnumerable data;
            if (LoadData != null)
                data = LoadData();
            else 
                data = Server.RetriveAllLazy(CleanType, safeImplementations);

            try
            {
                changing = true;
                combo.Items.Clear();
                foreach (object o in data)
                {
                    combo.Items.Add(o);
                }

                combo.SelectedItem = !CleanLazy ? Server.Convert(Entity, Reflector.GenerateLazy(EntityType)) : Entity;
            }
            finally
            {
                changing = false;
            }

            isLoaded = true;
        }

        private void btCreate_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnCreate();

            if (entity != null)
                SetEntityUserInteraction(entity);
        }

        private void btFind_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnFinding(false);

            if (entity != null)
                SetEntityUserInteraction(entity);
        }

        private void btView_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnViewing(Entity);

            if (entity != null)
                SetEntityUserInteraction(entity);
        }

        private void btRemove_Click(object sender, RoutedEventArgs e)
        {
            if (OnRemoving(Entity))
                SetEntityUserInteraction(null);
        }
    }

    public enum LoadDataTrigger
    {
        OnLoad,
        OnExpand,
    }  
}
