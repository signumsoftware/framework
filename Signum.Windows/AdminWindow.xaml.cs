using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Signum.Entities;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Signum.Utilities.DataStructures;
using Signum.Utilities;

namespace Signum.Windows
{
	public partial class AdminWindow: AdminBase
	{
        public static readonly DependencyProperty MainControlProperty =
            DependencyProperty.Register("MainControl", typeof(Control), typeof(AdminWindow));
        public Control MainControl
        {
            get { return (Control)GetValue(MainControlProperty); }
            set { SetValue(MainControlProperty, value); }
        }

        public static readonly DependencyProperty ElementsProperty =
            DependencyProperty.Register("Elements", typeof(ObservableCollection<IdentifiableEntity>), typeof(AdminWindow), new UIPropertyMetadata(null));
        public ObservableCollection<IdentifiableEntity> Elements
        {
            get { return (ObservableCollection<IdentifiableEntity>)GetValue(ElementsProperty); }
            set { SetValue(ElementsProperty, value); }
        }

        Type type; 

        public AdminWindow(Type adminType)
        {
            if (!typeof(IdentifiableEntity).IsAssignableFrom(adminType))
                throw new ArgumentException(Properties.Resources.TypeArgumentMustInherit0.Formato(typeof(IdentifiableEntity).Name));

            this.type = adminType;

            this.InitializeComponent();

            Common.SetTypeContext(WidgetPanel, TypeContext.Root(type));

            entityList.EntitiesType = typeof(MList<>).MakeGenericType(adminType);

            this.Loaded += new RoutedEventHandler(AdminWindow_Loaded);
        }
        
        public AdminWindow()
		{
			this.InitializeComponent();

            this.Loaded += new RoutedEventHandler(AdminWindow_Loaded);
		}

        void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            entityList.Create = Navigator.IsCreable(type, true);

            Retrieve();
        } 

        public override Button GetSaveButton()
        {
            return this.btSave;
        }

        public override List<IdentifiableEntity> GetEntities()
        {
            return Elements.ToList();
        }

        public override void SetEntities(List<IdentifiableEntity> value)
        {
            Elements = new ObservableCollection<IdentifiableEntity>(value);
        }

        public override void UpdateInterface()
        {
            this.DataContext = Elements;
        }

        public override void RetrieveEntities()
        {
            Elements = new ObservableCollection<IdentifiableEntity>(Server.RetrieveAll(type));
        }

        public override List<IdentifiableEntity> SaveEntities(List<IdentifiableEntity> value)
        {
            return Server.SaveList(value);
        }

        private void WidgetPanel_ExpandedCollapsed(object sender, RoutedEventArgs e)
        {
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }
	}
}