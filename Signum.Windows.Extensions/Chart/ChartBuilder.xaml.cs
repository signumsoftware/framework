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

        public ObservableCollection<ChartScriptEntity> chartScripts = ChartUtils.PackInGroups(Server.Return((IChartServer cs) => cs.GetChartScripts()), 4).SelectMany(a => a).ToObservableCollection();

        public ObservableCollection<ChartScriptEntity> ChartScripts
        {
            get { return chartScripts; }
        }

        public static IValueConverter ChartTypeToImage = ConverterFactory.New((Lite<FileEntity> ct) =>
        {
            if (ct == null)
                return null;

            return new MemoryStream(ct.Retrieve().BinaryFile).Using(ImageLoader.ThreadSafeImage);
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
        Brush superLightBlue = ((Brush)new BrushConverter().ConvertFromString("#dfefff")).Do(b => b.Freeze());

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 4 || !(values[0] is ChartScriptEntity) || !(values[1] is IChartBase) || !(values[2] is bool) || ((bool)values[2]))
                return null;

            if (((ChartScriptEntity)values[0]).IsCompatibleWith((IChartBase)values[1]))
                return superLightBlue;

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
