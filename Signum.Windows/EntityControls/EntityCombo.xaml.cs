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
        static readonly string NullValueString = " - ";

        public static readonly DependencyProperty NullValueProperty =
            DependencyProperty.Register("NullValue", typeof(bool), typeof(EntityCombo), new PropertyMetadata(true));
        public bool NullValue
        {
            get { return (bool)GetValue(NullValueProperty); }
            set { SetValue(NullValueProperty, value); }
        }

        public static readonly DependencyProperty LoadDataTriggerProperty =
            DependencyProperty.Register("LoadDataTrigger", typeof(LoadDataTrigger), typeof(EntityCombo), new UIPropertyMetadata(LoadDataTrigger.OnExpand));
        public LoadDataTrigger LoadDataTrigger
        {
            get { return (LoadDataTrigger)GetValue(LoadDataTriggerProperty); }
            set { SetValue(LoadDataTriggerProperty, value); }
        }

        public static readonly DependencyProperty SortElementsProperty =
            DependencyProperty.Register("SortElements", typeof(bool), typeof(EntityCombo), new UIPropertyMetadata(true));
        public bool SortElements
        {
            get { return (bool)GetValue(SortElementsProperty); }
            set { SetValue(SortElementsProperty, value); }
        }

        public event Func<IEnumerable<Lite<IEntity>>> LoadData;

        static EntityCombo()
        {
            RemoveProperty.OverrideMetadata(typeof(EntityCombo), new FrameworkPropertyMetadata(false));
            FindProperty.OverrideMetadata(typeof(EntityCombo), new FrameworkPropertyMetadata(false));
            NavigateProperty.OverrideMetadata(typeof(EntityCombo), new FrameworkPropertyMetadata(false));
        }

        protected override void UpdateVisibility()
        {
            btCreate.Visibility = CanCreate() ? Visibility.Visible : Visibility.Collapsed;
            btFind.Visibility = CanFind() ? Visibility.Visible : Visibility.Collapsed;
            btView.Visibility = CanViewOrNavigate() ? Visibility.Visible : Visibility.Collapsed;
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

                object newEntity = CleanLite ? Entity : Server.Convert(Entity, Lite.Generate(CleanType ?? Type));

                if (!isLoaded || newEntity != null && !combo.Items.Contains(newEntity))
                    combo.Items.Add(newEntity);

                combo.SelectedItem = NullValue && newEntity == null ? NullValueString : newEntity;
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

                var selItem = NullValue && combo.SelectedItem == (object)NullValueString ? null : combo.SelectedItem;

                SetEntityUserInteraction(Server.Convert(selItem, Type));
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

            if (LoadDataTrigger == LoadDataTrigger.OnLoad)
                OnLoadData(sender, e);
            else
            {
                if (NullValue && Entity == null) //Beasue entity changed won't be fired
                {
                    combo.Items.Add(NullValueString);
                    combo.SelectedItem = NullValueString;
                }

                combo.DropDownOpened += new EventHandler(OnLoadData);
            }
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
            IEnumerable<Lite<IEntity>> data;
            if (LoadData != null)
            {
                data = LoadData();
                if (data == null)
                    return;
            }
            else
            {
                data = Server.FindAllLite(safeImplementations.Value);
            }

            if (SortElements)
                data = data.OrderBy(a => a.ToString()).ToList();

            try
            {
                changing = true;
                combo.Items.Clear();
                if(NullValue)
                    combo.Items.Add(NullValueString); 
                foreach (Lite<IEntity> lite in data)
                {
                    combo.Items.Add(lite);
                }

                var selectedItem = !CleanLite ? Server.Convert(Entity, Lite.Generate(Type)) : Entity;

                if (selectedItem != null && !combo.Items.Contains(selectedItem))
                    combo.Items.Add(selectedItem);

                combo.SelectedItem = NullValue && selectedItem == null ? NullValueString : selectedItem;

                if (!NullValue && selectedItem == null)
                {
                    combo.SelectedIndex = -1;
                    combo.SelectedValue = -1;
                    combo.Text = "";
                }
            }
            finally
            {
                changing = false;
            }
        }

        private void combo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                ((ComboBox)sender).IsDropDownOpen = true;
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
