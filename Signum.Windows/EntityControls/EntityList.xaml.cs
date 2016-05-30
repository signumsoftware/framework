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
using System.Collections;
using System.ComponentModel;
using Signum.Entities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class EntityList : EntityListBase
    {
        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register("SelectionMode", typeof(SelectionMode), typeof(EntityList), new UIPropertyMetadata(SelectionMode.Single));
        public SelectionMode SelectionMode
        {
            get { return (SelectionMode)GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        public IList SelectedEntities
        {
            get { return (IList)listBox.SelectedItems; }
        }

        protected override void UpdateVisibility()
        {
            btCreate.Visibility = CanCreate().ToVisibility();
            btFind.Visibility = CanFind().ToVisibility();
            btView.Visibility = CanViewOrNavigate().ToVisibility();
            btRemove.Visibility = CanRemove().ToVisibility();
            btUp.Visibility = this.CanMove() ? (CanMoveUp() ? Visibility.Visible : Visibility.Hidden) : Visibility.Collapsed;
            btDown.Visibility = this.CanMove() ? (CanMoveDown() ? Visibility.Visible : Visibility.Hidden) : Visibility.Collapsed;
        }

        static EntityList()
        {
            Common.LabelPropertySelector.SetDefinition(typeof(EntityList), null);
            Common.ValuePropertySelector.SetDefinition(typeof(EntityList), EntitiesProperty);
            Common.TypePropertySelector.SetDefinition(typeof(EntityList), EntitiesTypeProperty);
        }

        private bool CanMoveUp()
        {
            return Entity != null && Entities.IndexOf(Entity) > 0;
        }

        private bool CanMoveDown()
        {
            return Entity != null && Entities.IndexOf(Entity) < Entities.Count - 1;
        }
     
        public EntityList()
        {
            InitializeComponent();
        }

        protected override void btCreate_Click(object sender, RoutedEventArgs e)
        {
            object value = OnCreate();

            if (value != null)
            {
                IList list = EnsureEntities();

                if (Move)
                {
                    int index = InsertIndex();
                    list.Insert(index, value);
                }
                else
                {
                    list.Add(value);
                }

                SetEntityUserInteraction(value); 
            }
        }

        private int InsertIndex()
        {
            object entity = Entity;
            return Entity != null ? Entities.IndexOf(entity) + 1 : Entities.Count;
        }

        protected override void btFind_Click(object sender, RoutedEventArgs e)
        {
            object value = OnFinding();
            if (value == null)
                return;

            IList list = EnsureEntities();

            int index = Move ? InsertIndex() : list.Count;
            if (value is IEnumerable)
                foreach (var item in (IEnumerable)value)
                    list.Insert(index++, item);
            else
                list.Insert(index, value);

            if (index > 0)
                SetEntityUserInteraction(list[index - 1]);
        }

        protected override void btView_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnViewingOrNavigating(this.listBox.SelectedItem, false);

            if (entity != null)
            {
                IList list = this.EnsureEntities();
                int index = this.listBox.SelectedIndex;
                list.RemoveAt(index);
                list.Insert(index, entity);
                listBox.SelectedIndex = index;
            }
        }

        private void btDown_Click(object sender, RoutedEventArgs e)
        {
            if (!CanMoveDown())
                return;

            object entity = Entity;
            int index = Entities.IndexOf(entity);
            Entities.RemoveAt(index);
            Entities.Insert(index + 1, entity);
            listBox.SelectedIndex = index + 1;
            listBox.ScrollIntoView(entity);
        }

        private void btUp_Click(object sender, RoutedEventArgs e)
        {
            if(!CanMoveUp())
                return;

            object entity = Entity;
            int index = Entities.IndexOf(entity);
            Entities.RemoveAt(index);
            Entities.Insert(index - 1, entity);
            listBox.SelectedIndex = index - 1;
            listBox.ScrollIntoView(entity);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                RemoveElements(); 
                e.Handled = true; 
            }
        }

        protected override void btRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveElements();
        }

        private void RemoveElements()
        {
            ArrayList list = new ArrayList(listBox.SelectedItems);
            foreach (var item in list)
            {
                if (OnRemoving(item))
                    EnsureEntities().Remove(item);
            }
        }

        private void listBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btView_Click(sender, null);
        }

        protected override void OnEntityChanged(object oldValue, object newValue)
        {
            listBox.SelectedItem = newValue;

            base.OnEntityChanged(oldValue, newValue);
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var newEntity = e.AddedItems.Cast<object>().SingleOrDefault();

            if (this.Entity != newEntity)
                this.Entity = newEntity;
        }
    }
}
