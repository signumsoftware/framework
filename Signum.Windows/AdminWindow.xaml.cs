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
    [Serializable]
    public class FakeEntityDN : Entity
    {
        MList<IdentifiableEntity> elements;
        public MList<IdentifiableEntity> Elements
        {
            get { return elements; }
            set { Set(ref elements, value, () => Elements); }
        }
    }

	public partial class AdminWindow: AdminBase
	{
        public static readonly DependencyProperty MainControlProperty =
            DependencyProperty.Register("MainControl", typeof(Control), typeof(AdminWindow));
        public Control MainControl
        {
            get { return (Control)GetValue(MainControlProperty); }
            set { SetValue(MainControlProperty, value); }
        }

        FakeEntityDN fake = new FakeEntityDN();

        //public static readonly DependencyProperty ElementsProperty =
        //    DependencyProperty.Register("Elements", typeof(ObservableCollection<IdentifiableEntity>), typeof(AdminWindow), new UIPropertyMetadata(null));
        //public ObservableCollection<IdentifiableEntity> Elements
        //{
        //    get { return (ObservableCollection<IdentifiableEntity>)GetValue(ElementsProperty); }
        //    set { SetValue(ElementsProperty, value); }
        //}

        Type type; 

        public AdminWindow(Type adminType)
        {
            if (!typeof(IdentifiableEntity).IsAssignableFrom(adminType))
                throw new ArgumentException(Properties.Resources.TypeArgumentMustInherit0.Formato(typeof(IdentifiableEntity).Name));

            this.type = adminType;

            this.InitializeComponent();

            Common.SetTypeContext(WidgetPanel, PropertyRoute.Root(type));

            entityList.EntitiesType = typeof(MList<>).MakeGenericType(adminType);
            this.DataContext = fake; 

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
            return fake.Elements.ToList();
        }

        public override void SetEntities(List<IdentifiableEntity> value)
        {
            fake.Elements = new MList<IdentifiableEntity>(value);
        }

        public override void UpdateInterface()
        {
           
        }

        public override void RetrieveEntities()
        {
            fake.Elements = new MList<IdentifiableEntity>(Server.RetrieveAll(type));
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