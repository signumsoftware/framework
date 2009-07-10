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
    public partial class EntityList : EntityBase
    {
        public static readonly DependencyProperty EntitiesProperty =
            DependencyProperty.Register("Entities", typeof(IList), typeof(EntityList), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((EntityList)d).EntitiesChanged(e)));
        public IList Entities
        {
            get { return (IList)GetValue(EntitiesProperty); }
            set { SetValue(EntitiesProperty, value); }
        }

        public static readonly DependencyProperty EntitiesTypeProperty =
          DependencyProperty.Register("EntitiesType", typeof(Type), typeof(EntityList), new UIPropertyMetadata(null));
        public Type EntitiesType
        {
            get { return (Type)GetValue(EntitiesTypeProperty); }
            set { SetValue(EntitiesTypeProperty, value); }
        }

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register("SelectionMode", typeof(SelectionMode), typeof(EntityList), new UIPropertyMetadata(SelectionMode.Single));
        public SelectionMode SelectionMode
        {
            get { return (SelectionMode)GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        public static readonly DependencyProperty MoveProperty =
            DependencyProperty.Register("Move", typeof(bool), typeof(EntityBase), new FrameworkPropertyMetadata(false, (d, e) => ((EntityList)d).UpdateVisibility()));
        public bool Move
        {
            get { return (bool)GetValue(MoveProperty); }
            set { SetValue(MoveProperty, value); }
        }

        protected override void UpdateVisibility()
        {
            btCreate.Visibility = CanCreate() ? Visibility.Visible : Visibility.Collapsed;
            btFind.Visibility = CanFind() ? Visibility.Visible : Visibility.Collapsed;
            btView.Visibility = CanView() ? Visibility.Visible : Visibility.Collapsed;
            btRemove.Visibility = CanRemove() ? Visibility.Visible : Visibility.Collapsed;
            btUp.Visibility = Move ? (CanMoveUp() ? Visibility.Visible : Visibility.Hidden) : Visibility.Collapsed;
            btDown.Visibility = Move ? (CanMoveDown() ? Visibility.Visible : Visibility.Hidden) : Visibility.Collapsed;
        }

        private bool CanMoveUp()
        {
            return Entity != null && Entities.IndexOf(Entity) > 0;
        }

        private bool CanMoveDown()
        {
            return Entity != null && Entities.IndexOf(Entity) < Entities.Count - 1;
        }
     
        protected override bool CanFind()
        {
            return Find && !Common.GetIsReadOnly(this);
        }

        protected override bool CanCreate()
        {
            return Create && !Common.GetIsReadOnly(this);
        }

        public IList EnsureEntities()
        {
            if (Entities == null)
                Entities = (IList)Activator.CreateInstance(EntitiesType);
            return Entities;
        }

        public void EntitiesChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && CleanType != null)
            {
                EntitySettings es = Navigator.Manager.Settings.TryGetC(CleanType);
                if (es!= null && es.CollectionViewOperations != null)
                {
                    var colView = CollectionViewSource.GetDefaultView(e.NewValue);
                    es.CollectionViewOperations(CleanLazy, colView);
                }
            }
        }
    
        public EntityList()
        {
            InitializeComponent();
        }

        private void btCreate_Click(object sender, RoutedEventArgs e)
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

        private void btFind_Click(object sender, RoutedEventArgs e)
        {
            IList list = EnsureEntities();

            object value = OnFinding(true);
            if (value == null)
                return;

            if (Move)
            {
                int index = InsertIndex();
                if (value is object[])
                    ((object[])value).ForEach((a, i) => list.Insert(index + i, a));
                else
                    list.Insert(index, value);

                SetEntityUserInteraction(list[index]);
            }
            else
            {
                if (value is object[])
                    ((object[])value).ForEach(a => list.Add(a));
                else
                    list.Add(value);

                SetEntityUserInteraction(list[list.Count - 1]); 
            }
        }

        private void btView_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnViewing(this.listBox.SelectedItem);

            if (entity != null)
                this.EnsureEntities()[this.listBox.SelectedIndex] = entity;
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

        private void btRemove_Click(object sender, RoutedEventArgs e)
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
    }
}
