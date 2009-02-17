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
using System.IO;
using Signum.Utilities;
using System.Globalization;
using Signum.Windows;

namespace Bugs.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

    //    private System.Collections.IEnumerable AutoCompleteTextBox_AutoCompleting(string arg)
    //    {
    //        if (string.IsNullOrEmpty(arg))
    //            return null;

    //        string dir = System.IO.Path.GetDirectoryName(arg);

    //        if (string.IsNullOrEmpty(dir))
    //            return null;

    //        string file = System.IO.Path.GetFileName(arg);
    //        DirectoryInfo di = new DirectoryInfo(dir);

    //        var directories = di.GetDirectories(file + "*").Select(a => a.FullName);
    //        var files = di.GetFiles(file + "*.*").Select(a => a.FullName);

    //        return directories.Concat(files).ToArray();
    //    }

    //    private void AutoCompleteTextBox_SelectedItemChanged(object sender, RoutedEventArgs e)
    //    {
    //        tbPath.Text = (string)tbPath.SelectedItem;
    //        tbPath.Focus();
    //    }
    }

    public static class MyConverters
    {
        public static readonly IValueConverter ColorBrushConverter = ConverterFactory.New(
               (Color color) => new SolidColorBrush(color));

        public static readonly IValueConverter DarkColor = ConverterFactory.New(
               (Color c) => Color.FromArgb(c.A, (byte)(c.R / 2), (byte)(c.G / 2), (byte)(c.B / 2)));

    }


    public class BushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
