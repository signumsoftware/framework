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
using System.Windows.Shapes;
using Signum.Windows;
using Signum.Utilities;
using Bugs.Windows.Controls;
using Bugs.Entities; 

namespace Bugs.Windows
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        public Main()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Main_Loaded);
            this.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(GenericMenuItem_Click));
        }

        void Main_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = this.ActualHeight;
        }

        private void GenericMenuItem_Click(object sender, RoutedEventArgs e)
        {
            object o = ((MenuItem)e.OriginalSource).Tag;

            if (o == null)
                return;

            if (o is FindOptions)
                Navigator.Find(((FindOptions)o).Do(fo => { fo.Buttons = SearchButtons.Close; }));
            else if (o is AdminOptions)
                Navigator.Admin(((AdminOptions)o));
            else
                throw new ApplicationException("El tipo {0} no está soportado".Formato(o.GetType().Name));
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
    
            new Window1().Show(); 
        }
    }

}
