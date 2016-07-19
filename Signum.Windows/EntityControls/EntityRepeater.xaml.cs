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

        internal static readonly DependencyProperty RemoveVisibilityProperty =
            DependencyProperty.Register("RemoveVisibility", typeof(Visibility), typeof(EntityRepeater), new UIPropertyMetadata(Visibility.Visible));
        internal Visibility RemoveVisibility
        {
            get { return (Visibility)GetValue(RemoveVisibilityProperty); }
            set { SetValue(RemoveVisibilityProperty, value); }
        }

        internal static readonly DependencyProperty ViewVisibilityProperty =
          DependencyProperty.Register("ViewVisibility", typeof(Visibility), typeof(EntityRepeater), new UIPropertyMetadata(Visibility.Visible));
        internal Visibility ViewVisibility
        {
            get { return (Visibility)GetValue(ViewVisibilityProperty); }
            set { SetValue(ViewVisibilityProperty, value); }
        }


        internal static readonly DependencyProperty MoveVisibilityProperty =
            DependencyProperty.Register("MoveVisibility", typeof(Visibility), typeof(EntityRepeater), new UIPropertyMetadata(Visibility.Collapsed));
        internal Visibility MoveVisibility
        {
            get { return (Visibility)GetValue(MoveVisibilityProperty); }
            set { SetValue(MoveVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ButtonsOrientationProperty =
            DependencyProperty.Register("ButtonsOrientation", typeof(Orientation), typeof(EntityRepeater), new PropertyMetadata(Orientation.Vertical));
        public Orientation ButtonsOrientation
        {
            get { return (Orientation)GetValue(ButtonsOrientationProperty); }
            set { SetValue(ButtonsOrientationProperty, value); }
        }

        public static readonly DependencyProperty ItemsPanelProperty =
            DependencyProperty.Register("ItemsPanel", typeof(ItemsPanelTemplate), typeof(EntityRepeater), new FrameworkPropertyMetadata(GetDefaultItemsPanelTemplate()));
        public ItemsPanelTemplate ItemsPanel
        {
            get { return (ItemsPanelTemplate)GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        static ItemsPanelTemplate GetDefaultItemsPanelTemplate()
        {
            ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)));
            template.Seal();
            return template;
        }

        public static readonly DependencyProperty ItemContainerStyleProperty =
           DependencyProperty.Register("ItemContainerStyle", typeof(Style), typeof(EntityRepeater), new PropertyMetadata(null));
        public Style ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(EntityRepeater), new UIPropertyMetadata(null));
        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        static EntityRepeater()
        {
            ViewProperty.OverrideMetadata(typeof(EntityRepeater), new FrameworkPropertyMetadata(false));
            ViewOnCreateProperty.OverrideMetadata(typeof(EntityRepeater), new FrameworkPropertyMetadata(false));
        }

        public EntityRepeater()
        {
            this.InitializeComponent();
            this.AddHandler(EntityRepeaterContentControl.RemoveClickEvent, new RoutedEventHandler(btRemove_Click));
            this.AddHandler(EntityRepeaterContentControl.MoveUpClickEvent, new RoutedEventHandler(btMoveUp_Click));
            this.AddHandler(EntityRepeaterContentControl.MoveDownClickEvent, new RoutedEventHandler(btMoveDown_Click));
            this.AddHandler(EntityRepeaterContentControl.ViewClickEvent, new RoutedEventHandler(btView_Click));
        }

        public override void OnLoad(object sender, RoutedEventArgs e)
        {
            if (EntityTemplate == null)
                EntityTemplate = Fluent.GetDataTemplate(() => new DataBorder { AutoChild = true });

            base.OnLoad(sender, e);
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

        protected override void btView_Click(object sender, RoutedEventArgs e)
        {
            object entity = ((EntityRepeaterContentControl)e.OriginalSource).DataContext;

            var result = OnViewingOrNavigating(entity, creating: false);

            if (result != null)
            {
                IList list = this.EnsureEntities();
                int index = list.IndexOf(entity);
                list.RemoveAt(index);
                list.Insert(index, result);
            }

            e.Handled = true;
        }


        protected override void btRemove_Click(object sender, RoutedEventArgs e)
        {
            object entity = ((EntityRepeaterContentControl)e.OriginalSource).DataContext;

            if (entity != null)
            {
                if (OnRemoving(entity))
                {
                    IList list = EnsureEntities();
                    list.Remove(entity);
                    SetEntityUserInteraction(null);
                }
            }

            e.Handled = true;
        }

        protected void btMoveUp_Click(object sender, RoutedEventArgs e)
        {
            object entity = ((EntityRepeaterContentControl)e.OriginalSource).DataContext;

            if (entity != null)
            {
                int index = Entities.IndexOf(entity);
                if (index > 0)
                {
                    Entities.RemoveAt(index);
                    Entities.Insert(index - 1, entity);
                    SetEntityUserInteraction(entity);
                }
            }

            e.Handled = true;
        }

        protected void btMoveDown_Click(object sender, RoutedEventArgs e)
        {
            object entity = ((EntityRepeaterContentControl)e.OriginalSource).DataContext;

            if (entity != null)
            {
                int index = Entities.IndexOf(entity);
                if (index < Entities.Count - 1)
                {
                    Entities.RemoveAt(index);
                    Entities.Insert(index + 1, entity);
                    SetEntityUserInteraction(entity);
                }
            }

            e.Handled = true;
        }

        protected override void UpdateVisibility()
        {
            btCreate.Visibility = CanCreate() ? Visibility.Visible : Visibility.Collapsed;
            btFind.Visibility = CanFind() ? Visibility.Visible : Visibility.Collapsed;
            
           
            RemoveVisibility = this.CanRemove().ToVisibility();
            MoveVisibility = this.CanMove().ToVisibility();
            ViewVisibility = this.View.ToVisibility();
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
        public static readonly RoutedEvent RemoveClickEvent = EventManager.RegisterRoutedEvent(
          "RemoveClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityRepeaterContentControl));
        public event RoutedEventHandler RemoveClick
        {
            add { AddHandler(RemoveClickEvent, value); }
            remove { RemoveHandler(RemoveClickEvent, value); }
        }

        public static readonly RoutedEvent MoveUpClickEvent = EventManager.RegisterRoutedEvent(
          "MoveUpClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityRepeaterContentControl));
        public event RoutedEventHandler MoveUpClick
        {
            add { AddHandler(MoveUpClickEvent, value); }
            remove { RemoveHandler(MoveUpClickEvent, value); }
        }

        public static readonly RoutedEvent MoveDownClickEvent = EventManager.RegisterRoutedEvent(
          "MoveDownClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityRepeaterContentControl));
        public event RoutedEventHandler MoveDownClick
        {
            add { AddHandler(MoveDownClickEvent, value); }
            remove { RemoveHandler(MoveDownClickEvent, value); }
        }

        public static readonly RoutedEvent ViewClickEvent = EventManager.RegisterRoutedEvent(
         "ViewClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityRepeaterContentControl));
        public event RoutedEventHandler ViewClick
        {
            add { AddHandler(ViewClickEvent, value); }
            remove { RemoveHandler(ViewClickEvent, value); }
        }

        static EntityRepeaterContentControl()
        {
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(EntityRepeaterContentControl), new FrameworkPropertyMetadata(typeof(EntityRepeaterContentControl)));
        }

        Button btRemove;
        Button btMoveUp;
        Button btMoveDown;
        Button btView;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (btRemove != null)
                btRemove.Click -= new RoutedEventHandler(btRemove_Click);
            if (btMoveUp != null)
                btMoveUp.Click -= new RoutedEventHandler(btMoveUp_Click);
            if (btMoveDown != null)
                btMoveDown.Click -= new RoutedEventHandler(btMoveDown_Click);
            if (btView != null)
                btView.Click -= new RoutedEventHandler(btView_Click);

            btRemove = (Button)base.GetTemplateChild("PART_RemoveButton");
            btMoveUp = (Button)base.GetTemplateChild("PART_MoveUp");
            btMoveDown = (Button)base.GetTemplateChild("PART_MoveDown");
            btView = (Button)base.GetTemplateChild("PART_View");

            if (btRemove != null)
                btRemove.Click += new RoutedEventHandler(btRemove_Click);
            if (btMoveUp != null)
                btMoveUp.Click += new RoutedEventHandler(btMoveUp_Click);
            if (btMoveDown != null)
                btMoveDown.Click += new RoutedEventHandler(btMoveDown_Click);
            if (btView != null)
                btView.Click += new RoutedEventHandler(btView_Click);
        }

        private void btRemove_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(RemoveClickEvent)); 
        }

        private void btView_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(ViewClickEvent));
        }

        void btMoveUp_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(MoveUpClickEvent));
        }

        void btMoveDown_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(MoveDownClickEvent)); 
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

        protected override string GetNameCore()
        {
            var parentRoute = Common.GetPropertyRoute(Owner);

            if (parentRoute == null)
                return null;

            return parentRoute.Add("Item").ToString();
        }
    }
}
