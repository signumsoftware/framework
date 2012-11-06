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
using System.Windows.Automation.Peers;
using Signum.Utilities;
using System.Linq;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for Repeater.xaml
    /// </summary>
    public partial class EntityRepeater : EntityListBase
    {
        public static readonly DependencyProperty VerticalScrollBarVisiblityProperty =
           DependencyProperty.Register("VerticalScrollBarVisiblity", typeof(ScrollBarVisibility), typeof(EntityRepeater), new UIPropertyMetadata(ScrollBarVisibility.Disabled));
        public ScrollBarVisibility VerticalScrollBarVisiblity
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisiblityProperty); }
            set { SetValue(VerticalScrollBarVisiblityProperty, value); }
        }

        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.Register("HorizontalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(EntityRepeater), new UIPropertyMetadata(ScrollBarVisibility.Disabled));
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

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

            var remove = this.CanRemove().ToVisibility();
            var buttons = this.itemsControl.Children<Button>().Where(a => a.Name == "btRemove").ToList();
            foreach (var bt in buttons)
                bt.Visibility = remove;
        }

        protected override bool CanRemove()
        {
            return Remove && !Common.GetIsReadOnly(this);
        }

        private void btRemove_Loaded(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Visibility = this.CanRemove().ToVisibility();
        }
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

    public class EntityRepeaterLineBorder: Border
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new EntityRepeaterLineBorderAutomationPeer(this);
        }
    }

    class EntityRepeaterLineBorderAutomationPeer : FrameworkElementAutomationPeer
    {
        public EntityRepeaterLineBorderAutomationPeer(EntityRepeaterLineBorder owner)
            : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        protected override string GetClassNameCore()
        {
            return base.Owner.GetType().Name;
        }

        protected override string GetItemStatusCore()
        {
            return Common.GetTypeContext(Owner).TryToString();
        }
    }
}