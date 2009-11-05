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

        Type type; 
        ObservableCollection<IdentifiableEntity> list; 

        public AdminWindow(Type adminType)
        {
            if (!typeof(IdentifiableEntity).IsAssignableFrom(adminType))
                throw new ArgumentException(Properties.Resources.TypeArgumentMustInherit0.Formato(typeof(IdentifiableEntity).Name)); 

            this.type = adminType;

            Common.SetTypeContext(this, new TypeContext(typeof(List<>).MakeGenericType(adminType)));

            this.InitializeComponent();

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
            return list.ToList();
        }

        public override void SetEntities(List<IdentifiableEntity> value)
        {
            list = new ObservableCollection<IdentifiableEntity>(value);
        }

        public override void UpdateInterface()
        {
            this.DataContext = list;
        }

        public override void RetrieveEntities()
        {
            list = new ObservableCollection<IdentifiableEntity>(Server.RetrieveAll(type));
        }

        public override List<IdentifiableEntity> SaveEntities(List<IdentifiableEntity> value)
        {
            return Server.SaveList(value);
        }

        private void widgetPanel_ExpandedCollapsed(object sender, RoutedEventArgs e)
        {
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }
	}
}