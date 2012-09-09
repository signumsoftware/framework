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
using Signum.Entities.Chart;
using Signum.Entities.Files;
using System.IO;
using System.ComponentModel;
using AurelienRibon.Ui.SyntaxHighlightBox;

namespace Signum.Windows.Chart
{
    /// <summary>
    /// Interaction logic for ChartScript.xaml
    /// </summary>
    public partial class ChartScript : UserControl
    {
        public static IValueConverter ImageConverter = ConverterFactory.New((Lite<FileDN> file) =>
        {
            if (file == null)
                return null;

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(file.Retrieve().BinaryFile);
            image.EndInit();

            return image;
        }); 

        public ChartScript()
        {
            InitializeComponent();
            box.CurrentHighlighter = HighlighterManager.Instance.Highlighters["JavaScript"];
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ChartScript_DataContextChanged);
        }

        void ChartScript_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var oldEntity = e.OldValue as INotifyPropertyChanged;
            if(oldEntity != null)
                oldEntity.PropertyChanged -= oldEntity_PropertyChanged;

            
            var newEntity = e.NewValue as INotifyPropertyChanged;
            if(newEntity != null)
                newEntity.PropertyChanged += oldEntity_PropertyChanged;
            
        }

        void oldEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var cs = sender as ChartScriptDN;

            if (e.PropertyName == "Script")
                RequestWindow.SetResults(cs.Script); //If saved, different clone
        }

        public ChartRequestWindow RequestWindow { get; set; }
    }
}
