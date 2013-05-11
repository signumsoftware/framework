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
using Signum.Utilities;

namespace Signum.Windows.Basics
{
    /// <summary>
    /// Interaction logic for Exception.xaml
    /// </summary>
    public partial class TypeUI : UserControl
    {
        public TypeUI()
        {
            InitializeComponent();
        }
    }

    public static class TypeClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<TypeDN>() { View = e => new TypeUI()});
            }
        }

        public static IEnumerable<TypeDN> ViewableServerTypes()
        {
            return from t in Navigator.Manager.EntitySettings.Keys
                   where Navigator.IsViewable(t)
                   select Server.ServerTypes.TryGetC(t) into tdn
                   where tdn != null
                   select tdn;
        }

        public static IEnumerable<Lite<TypeDN>> ViewableServerTypes(string text)
        {
            return from t in Navigator.Manager.EntitySettings.Keys
                   where Navigator.IsViewable(t) && t.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase) || t.NiceName().Contains(text, StringComparison.InvariantCultureIgnoreCase)
                   select Server.ServerTypes.TryGetC(t) into tdn
                   where tdn != null
                   select tdn.ToLite();
        }
    }
}
