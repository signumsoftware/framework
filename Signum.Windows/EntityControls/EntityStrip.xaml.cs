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
using System.Threading;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for Repeater.xaml
    /// </summary>
    public partial class EntityStrip : EntityListBase
    {
        public event Func<string, IEnumerable<Lite<Entity>>> Autocompleting;

        public static readonly DependencyProperty AutocompleteProperty =
            DependencyProperty.Register("Autocomplete", typeof(bool), typeof(EntityStrip), new FrameworkPropertyMetadata(true));
        public bool Autocomplete
        {
            get { return (bool)GetValue(AutocompleteProperty); }
            set { SetValue(AutocompleteProperty, value); }
        }

        int autocompleteElements = 5;
        public int AutocompleteElements
        {
            get { return autocompleteElements; }
            set { autocompleteElements = value; }
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(EntityStrip), new PropertyMetadata(Orientation.Horizontal, (s,a)=>
            {
                ((EntityStrip)s).ItemsPanel = ((Orientation)a.NewValue) == Orientation.Horizontal ? WrapPanel() : StackPanel();
            }));
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public static readonly DependencyProperty ItemsPanelProperty =
            DependencyProperty.Register("ItemsPanel", typeof(ItemsPanelTemplate), typeof(EntityStrip), new FrameworkPropertyMetadata(WrapPanel()));
        public ItemsPanelTemplate ItemsPanel
        {
            get { return (ItemsPanelTemplate)GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }


        internal static readonly DependencyProperty RemoveVisibilityProperty =
            DependencyProperty.Register("RemoveVisibility", typeof(Visibility), typeof(EntityStrip), new UIPropertyMetadata(Visibility.Visible));
        internal Visibility RemoveVisibility
        {
            get { return (Visibility)GetValue(RemoveVisibilityProperty); }
            set { SetValue(RemoveVisibilityProperty, value); }
        }

        internal static readonly DependencyProperty ViewVisibilityProperty =
          DependencyProperty.Register("ViewVisibility", typeof(Visibility), typeof(EntityStrip), new UIPropertyMetadata(Visibility.Visible));
        internal Visibility ViewVisibility
        {
            get { return (Visibility)GetValue(ViewVisibilityProperty); }
            set { SetValue(ViewVisibilityProperty, value); }
        }

        internal static readonly DependencyProperty MoveVisibilityProperty =
            DependencyProperty.Register("MoveVisibility", typeof(Visibility), typeof(EntityStrip), new UIPropertyMetadata(Visibility.Collapsed));
        internal Visibility MoveVisibility
        {
            get { return (Visibility)GetValue(MoveVisibilityProperty); }
            set { SetValue(MoveVisibilityProperty, value); }
        }


        static ItemsPanelTemplate WrapPanel()
        {
            ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)));
            template.Seal();
            return template;
        }

        static ItemsPanelTemplate StackPanel()
        {
            ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)));
            template.Seal();
            return template;
        }

        public static readonly DependencyProperty ItemContainerStyleProperty =
           DependencyProperty.Register("ItemContainerStyle", typeof(Style), typeof(EntityStrip), new PropertyMetadata(null));
        public Style ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        static EntityStrip()
        {
            ViewProperty.OverrideMetadata(typeof(EntityStrip), new FrameworkPropertyMetadata(false));
        }

        public EntityStrip()
        {
            this.InitializeComponent();
            this.AddHandler(EntityStripContentControl.RemoveClickEvent, new RoutedEventHandler(btRemove_Click));
            this.AddHandler(EntityStripContentControl.MoveUpClickEvent, new RoutedEventHandler(btMoveUp_Click));
            this.AddHandler(EntityStripContentControl.MoveDownClickEvent, new RoutedEventHandler(btMoveDown_Click));
            this.AddHandler(EntityStripContentControl.ViewClickEvent, new RoutedEventHandler(btView_Click));
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
            object entity = ((EntityStripContentControl)e.OriginalSource).DataContext;

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
            object entity = ((EntityStripContentControl)e.OriginalSource).DataContext;

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
            object entity = ((EntityStripContentControl)e.OriginalSource).DataContext;

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
            object entity = ((EntityStripContentControl)e.OriginalSource).DataContext;

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
            
            autocompleteTextBox.Visibility = CanAutocomplete().ToVisibility();
            RemoveVisibility = this.CanRemove().ToVisibility();
            MoveVisibility = this.CanMove().ToVisibility();
            ViewVisibility = this.View.ToVisibility();
        }

        protected override bool CanRemove()
        {
            return Remove && !Common.GetIsReadOnly(this);
        }

        protected bool CanAutocomplete()
        {
            return Autocomplete && !Common.GetIsReadOnly(this);
        }

        private IEnumerable autocompleteTextBox_Autocompleting(string arg, CancellationToken ct)
        {
            IEnumerable value;
            if (Autocompleting != null)
                value = Autocompleting(arg);
            else
                value = Server.FindLiteLike(safeImplementations.Value, arg, AutocompleteElements);  

            return value;
        }

        private void autocompleteTextBox_Closed(object sender, CloseEventArgs e)
        {
            if (e.IsCommit)
            {
                if (CanAutocomplete())
                { 
                    object value = Server.Convert(autocompleteTextBox.SelectedItem, Type);

                    if (value != null)
                    {
                        IList list = EnsureEntities();
                        list.Add(value);
                        SetEntityUserInteraction(value);
                    }
                }

                autocompleteTextBox.Text = "";
                autocompleteTextBox.SelectEnd();
            }
            else
            { 
            }
        }

        private void autocompleteTextBox_SelectedItemChanged(object sender, RoutedEventArgs e)
        {
        }
    }


    [StyleTypedPropertyAttribute(Property = "ItemContainerStyle", StyleTargetType = typeof(EntityStripContentControl))]
    public class EntityStripItemsControl : ItemsControl
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new EntityStripContentControl();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is EntityStripContentBase;
        }
    }

    public class EntityStripContentBase : ContentControl
    {

    }

    [TemplatePart(Name = "PART_RemoveButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_MoveUp", Type = typeof(Button))]
    [TemplatePart(Name = "PART_MoveDown", Type = typeof(Button))]
    [TemplatePart(Name = "PART_View", Type = typeof(Button))]
    public class EntityStripContentControl : EntityStripContentBase
    {
        public static readonly RoutedEvent RemoveClickEvent = EventManager.RegisterRoutedEvent(
          "RemoveClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityStripContentControl));
        public event RoutedEventHandler RemoveClick
        {
            add { AddHandler(RemoveClickEvent, value); }
            remove { RemoveHandler(RemoveClickEvent, value); }
        }

        public static readonly RoutedEvent MoveUpClickEvent = EventManager.RegisterRoutedEvent(
          "MoveUpClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityStripContentControl));
        public event RoutedEventHandler MoveUpClick
        {
            add { AddHandler(MoveUpClickEvent, value); }
            remove { RemoveHandler(MoveUpClickEvent, value); }
        }

        public static readonly RoutedEvent MoveDownClickEvent = EventManager.RegisterRoutedEvent(
          "MoveDownClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityStripContentControl));
        public event RoutedEventHandler MoveDownClick
        {
            add { AddHandler(MoveDownClickEvent, value); }
            remove { RemoveHandler(MoveDownClickEvent, value); }
        }

        public static readonly RoutedEvent ViewClickEvent = EventManager.RegisterRoutedEvent(
         "ViewClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityStripContentControl));
        public event RoutedEventHandler ViewClick
        {
            add { AddHandler(ViewClickEvent, value); }
            remove { RemoveHandler(ViewClickEvent, value); }
        }

        static EntityStripContentControl()
        {
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(EntityStripContentControl), new FrameworkPropertyMetadata(typeof(EntityStripContentControl)));
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
            return new EntityStripLineBorderAutomationPeer(this);
        }
    }

    class EntityStripLineBorderAutomationPeer : FrameworkElementAutomationPeer
    {
        public EntityStripLineBorderAutomationPeer(EntityStripContentControl owner)
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

    public class BindingProxy : Freezable
    {
        #region Overrides of Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        #endregion

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata());
        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }
    }
}
