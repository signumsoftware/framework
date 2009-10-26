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
    public partial class EntityCombo : EntityBase
    {
        public static readonly Lazy[] EmptyList = new Lazy[0];

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

                object newEntity = CleanLazy ? Entity : Server.Convert(Entity, Reflector.GenerateLazy(CleanType ?? Type));

                if (!isLoaded || newEntity != null && !combo.Items.Contains(newEntity))
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

                SetEntityUserInteraction(Server.Convert(combo.SelectedItem, Type));
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

            LoadNow();

            isLoaded = true;
        }

        public void LoadNow()
        {
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

                var selectedItem = !CleanLazy ? Server.Convert(Entity, Reflector.GenerateLazy(Type)) : Entity;

                if (selectedItem != null && !combo.Items.Contains(selectedItem))
                    combo.Items.Add(selectedItem);

                combo.SelectedItem = selectedItem;
            }
            finally
            {
                changing = false;
            }
        }

  /*      private void combo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down) {
                ((ComboBox)sender).IsDropDownOpen = true;
            }
        }*/
    }

    public enum LoadDataTrigger
    {
        OnLoad,
        OnExpand,
    }  
}
