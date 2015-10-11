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
        public static readonly DependencyProperty EntityControlProperty =
            DependencyProperty.Register("EntityControl", typeof(object), typeof(EntityDetail));
        public object EntityControl
        {
            get { return (object)GetValue(EntityControlProperty); }
            set { SetValue(EntityControlProperty, value); }
        }


        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(EntityDetail), new UIPropertyMetadata(null));
        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }


        static EntityDetail()
        {
            ViewProperty.OverrideMetadata(typeof(EntityDetail), new FrameworkPropertyMetadata(false));
            NavigateProperty.OverrideMetadata(typeof(EntityDetail), new FrameworkPropertyMetadata(false));
        }

        protected override void UpdateVisibility()
        {
            btCreate.Visibility = CanCreate() ? Visibility.Visible : Visibility.Collapsed;
            btFind.Visibility = CanFind() ? Visibility.Visible : Visibility.Collapsed;
            btView.Visibility = CanViewOrNavigate() ? Visibility.Visible : Visibility.Collapsed;
            btRemove.Visibility = CanRemove() ? Visibility.Visible : Visibility.Collapsed;
        }

        public EntityDetail()
        {
            InitializeComponent();
            this.AddHandler(Common.ChangeDataContextEvent, new ChangeDataContextHandler(ChangeEntity));
        }


        void ChangeEntity(object sender, ChangeDataContextEventArgs e)
        {
            if (e.Refresh)
            {
                var lite = (Entity as IEntity)?.ToLite();

                if (lite != null)
                {
                    this.Entity = null;
                    this.Entity = lite.Retrieve();
                }
            }
            else
            {
                this.Entity = null;
                this.Entity = e.NewDataContext;
            }
          
            e.Handled = true;
        }

        public override void OnLoad(object sender, RoutedEventArgs e)
        {
            base.OnLoad(sender, e);

            contentPresenter.SetBinding(ContentControl.DataContextProperty, new Binding
            {
                Path = new PropertyPath(EntityProperty),
                Source = this,
                Converter = CleanLite ? Converters.Retrieve : null
            });

            if (EntityControl == null)
                EntityControl = new DataBorder { AutoChild = true };
        }
    }
}
