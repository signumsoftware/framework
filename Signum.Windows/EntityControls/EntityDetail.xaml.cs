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
    public partial class EntityDetail : EntityBase
    {
        public static readonly DependencyProperty OrientationProperty =
          DependencyProperty.Register("Orientation", typeof(Orientation), typeof(EntityDetail), new UIPropertyMetadata(Orientation.Vertical));
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public static readonly DependencyProperty EntityControlProperty =
         DependencyProperty.Register("EntityControl", typeof(object), typeof(EntityDetail), new UIPropertyMetadata((d, e) => ((EntityDetail)d).OnEntityControlChanged(e)));

        private void OnEntityControlChanged(DependencyPropertyChangedEventArgs e)
        {
            //base.RemoveLogicalChild(e.OldValue);
            //base.AddLogicalChild(e.NewValue);
        }

        public object EntityControl
        {
            get { return (object)GetValue(EntityControlProperty); }
            set { SetValue(EntityControlProperty, value); }
        }

        static EntityDetail()
        {
            ViewProperty.OverrideMetadata(typeof(EntityDetail), new FrameworkPropertyMetadata(false));
            FindProperty.OverrideMetadata(typeof(EntityDetail), new FrameworkPropertyMetadata(false));
        }

        protected override void UpdateVisibility()
        {
            btCreate.Visibility = CanCreate() ? Visibility.Visible : Visibility.Collapsed;
            btFind.Visibility = CanFind() ? Visibility.Visible : Visibility.Collapsed;
            btView.Visibility = CanView() ? Visibility.Visible : Visibility.Collapsed;
            btRemove.Visibility = CanRemove() ? Visibility.Visible : Visibility.Collapsed;
        }

        public EntityDetail()
        {
            InitializeComponent();
        }

        protected override void OnEntityChanged(object oldValue, object newValue)
        {
            base.OnEntityChanged(oldValue, newValue);

            Lite lite = newValue as Lite;
            if (lite != null)
                contentPresenter.DataContext = Server.Retrieve(lite);
            else
                contentPresenter.DataContext = newValue;
        }

        private void contentPresenter_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }
    }
}
