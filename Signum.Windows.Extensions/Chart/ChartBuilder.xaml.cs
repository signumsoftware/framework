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
using System.Globalization;
using Signum.Entities.Chart;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Services;
using System.ComponentModel;
using System.Windows.Threading;
using Signum.Windows;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection;
using System.IO;
using Signum.Entities.Files;
using System.Collections.ObjectModel;

namespace Signum.Windows.Chart
{
    /// <summary>
    /// Interaction logic for ChartBuilder.xaml
    /// </summary>
    public partial class ChartBuilder : UserControl
    {
        public static ChartTypeBackgroundConverter ChartTypeBackground = new ChartTypeBackgroundConverter();

        public List<ChartScriptDN> chartScripts = Server.RetrieveAll<ChartScriptDN>().OrderBy(a => a.Columns.Count).ThenByDescending(a => a.Columns.Count(c => c.IsOptional)).ThenBy(a => a.Name).ToList();

        public ObservableCollection<ChartScriptDN> ChartScripts
        {
            get { return new ObservableCollection<ChartScriptDN>(chartScripts); }
        }

        public static IValueConverter ChartTypeToImage = ConverterFactory.New((Lite<FileDN> ct) =>
        {
            if (ct == null)
                return null;

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(ct.Retrieve().BinaryFile);
            image.EndInit();

            return image;
        });

        public QueryDescription Description;
        public Type EntityType;
   
        public IChartBase Request
        {
            get { return (IChartBase)DataContext; }
        }

        public ChartBuilder()
        {
            InitializeComponent();
        }
    }


    public class ChartTypeBackgroundConverter : IMultiValueConverter
    {
        Brush superLightBlue = (Brush)new BrushConverter().ConvertFromString("#dfefff");

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 4 || !(values[0] is ChartScriptDN) || !(values[1] is IChartBase) || !(values[2] is bool) || ((bool)values[2]))
                return null;

            if (((ChartScriptDN)values[0]).IsCompatibleWith((IChartBase)values[1]))
                return superLightBlue;

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
