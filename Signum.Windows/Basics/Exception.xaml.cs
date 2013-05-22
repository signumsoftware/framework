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
using Signum.Windows;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.Basics;

namespace Signum.Windows.Basics
{
    /// <summary>
    /// Interaction logic for Exception.xaml
    /// </summary>
    public partial class ExceptionCtrl : UserControl
    {
        public ExceptionCtrl()
        {
            InitializeComponent();
        }
    }

    public static class ExceptionClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<ExceptionDN>() { View = e => new ExceptionCtrl(), Icon = ImageLoader.GetImageSortName("exception.png") });
            }
        }
    }
}
