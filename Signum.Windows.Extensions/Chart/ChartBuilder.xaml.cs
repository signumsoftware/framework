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

        public List<ChartScriptDN> chartScripts = Server.RetrieveAll<ChartScriptDN>();

        public ObservableCollection<ChartScriptDN> ChartScripts
        {
            get { return new ObservableCollection<ChartScriptDN>(chartScripts); }
        }

        public static IValueConverter ChartTypeToImage = ConverterFactory.New((Lite<FileDN> ct) =>
        {
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

    //public class PropertyLabelConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        ChartType cr = (ChartType)value;
            
    //        ChartTokenName pi = ((string)parameter).ToEnum<ChartTokenName>();
            
    //        var label = ChartUtils.PropertyLabel(cr, pi);

    //        if (!label.HasValue)
    //            return "";

    //        return label.Value.NiceToString();
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class GroupByVisibleConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        ChartResultType cr = (ChartResultType)value;

    //        ChartTokenName pi = ((string)parameter).ToEnum<ChartTokenName>();

    //        return ChartUtils.CanGroupBy(cr, pi) ? Visibility.Visible : Visibility.Collapsed;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public class ChartTypeBackgroundConverter : IMultiValueConverter
    {
        Brush superLightBlue = (Brush)new BrushConverter().ConvertFromString("#dfefff");

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3 || !(values[0] is ChartScriptDN) || !(values[1] is IChartBase) || !(values[2] is bool) || ((bool)values[2]))
                return null;

            if (((ChartScriptDN)values[0]).IsCompatibleWith(((IChartBase)values[1]).Columns))
                return superLightBlue;

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
