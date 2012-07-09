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
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
           DependencyProperty.Register("VerticalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(EntityRepeater), new UIPropertyMetadata(ScrollBarVisibility.Disabled));
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.Register("HorizontalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(EntityRepeater), new UIPropertyMetadata(ScrollBarVisibility.Disabled));
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        public static readonly DependencyProperty RemoveVisibilityProperty =
            DependencyProperty.Register("RemoveVisibility", typeof(Visibility), typeof(EntityRepeater), new UIPropertyMetadata(Visibility.Visible));
        public Visibility RemoveVisibility
        {
            get { return (Visibility)GetValue(RemoveVisibilityProperty); }
            set { SetValue(RemoveVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ItemsPanelProperty =
            DependencyProperty.Register("ItemsPanel", typeof(ItemsPanelTemplate), typeof(EntityRepeater), new UIPropertyMetadata(null));
        public ItemsPanelTemplate ItemsPanel
        {
            get { return (ItemsPanelTemplate)GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
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
            this.AddHandler(EntityRepeaterContentControl.RemoveClickedEvent,  new RoutedEventHandler(btRemove_Click));
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
            object value = ((EntityRepeaterContentControl)e.OriginalSource).DataContext;

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
           
            RemoveVisibility = this.CanRemove().ToVisibility();

        }

        protected override bool CanRemove()
        {
            return Remove && !Common.GetIsReadOnly(this);
        }
    }


    [StyleTypedPropertyAttribute(Property = "ItemContainerStyle", StyleTargetType = typeof(EntityRepeaterContentControl))]
    public class EntityRepeaterItemsControl : ItemsControl
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new EntityRepeaterContentControl();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is EntityRepeaterContentControl;
        }
    }

    [TemplatePart(Name = "PART_RemoveButton", Type = typeof(Button))]
    public class EntityRepeaterContentControl: ContentControl
    {
        public static readonly RoutedEvent RemoveClickedEvent = EventManager.RegisterRoutedEvent(
          "RemoveClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityRepeaterContentControl));
        public event RoutedEventHandler RemoveClicked
        {
            add { AddHandler(RemoveClickedEvent, value); }
            remove { RemoveHandler(RemoveClickedEvent, value); }
        }

        static EntityRepeaterContentControl()
        {
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(EntityRepeaterContentControl), new FrameworkPropertyMetadata(typeof(EntityRepeaterContentControl)));
        }

        Button btRemove;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (this.btRemove != null)
            {
                btRemove.Click -= new RoutedEventHandler(btRemove_Click);
            }

            btRemove = (Button)base.GetTemplateChild("PART_RemoveButton");

            if (this.btRemove != null)
            {
                btRemove.Click += new RoutedEventHandler(btRemove_Click);
            }
        }

        private void btRemove_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(RemoveClickedEvent)); 
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new EntityRepeaterLineBorderAutomationPeer(this);
        }
    }

    class EntityRepeaterLineBorderAutomationPeer : FrameworkElementAutomationPeer
    {
        public EntityRepeaterLineBorderAutomationPeer(EntityRepeaterContentControl owner)
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