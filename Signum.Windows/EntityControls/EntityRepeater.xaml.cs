using System;
using System.Collections.Generic;
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
using System.Collections;
using Signum.Entities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for Repeater.xaml
    /// </summary>
    public partial class EntityRepeater : EntityListBase
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(EntityRepeater), new UIPropertyMetadata(null));
        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public EntityRepeater()
        {
            this.InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
        }

        protected override void btCreate_Click(object sender, RoutedEventArgs e)
        {
            object value = OnCreate();

            if (value != null)
            {
                IList list = EnsureEntities();
                list.Add(value);
                SetEntityUserInteraction(value);
            }
        }

        protected override void btFind_Click(object sender, RoutedEventArgs e)
        {
            object value = OnFinding();

            if (value != null)
            {
                IList list = EnsureEntities();

                if (value is object[])
                {
                    foreach (var entity in (object[])value)
                    {
                        list.Add(entity);
                        SetEntityUserInteraction(entity);
                    }
                }
                else
                {
                    list.Add(value);
                    SetEntityUserInteraction(value);
                }
            }
        }

        protected override void btRemove_Click(object sender, RoutedEventArgs e)
        {
            object value = ((Grid)(((Button)sender).Parent)).DataContext;

            if (value != null)
            {
                if (OnRemoving(value))
                {
                    IList list = EnsureEntities();
                    list.Remove(value);
                    SetEntityUserInteraction(null);
                }
            }
        }

        protected override void UpdateVisibility()
        {
            btCreate.Visibility = CanCreate() ? Visibility.Visible : Visibility.Collapsed;
            btFind.Visibility = CanFind() ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override bool CanRemove()
        {
            return Remove && !Common.GetIsReadOnly(this);
        }

        //private void Grid_Initialized(object sender, EventArgs e)
        //{
        //    Grid grid = (Grid)sender;

        //    PropertyRoute tc = Common.GetTypeContext(grid);

        //    Common.SetTypeContext(grid, tc.Add("Item"));
        //}
    }

    [StyleTypedPropertyAttribute(Property = "ItemContainerStyle", StyleTargetType = typeof(ContentControl))]
    public class RepeaterItemsControl : ItemsControl
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ContentControl();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ContentControl;
        }
    }
}