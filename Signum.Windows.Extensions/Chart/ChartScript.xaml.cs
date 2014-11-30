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
using Signum.Windows.SyntaxHighlight;
namespace Signum.Windows.Chart
{
    /// <summary>
    /// Interaction logic for ChartScript.xaml
    /// </summary>
    public partial class ChartScript : UserControl
    {
        public static IValueConverter ImageConverter = ConverterFactory.New((Lite<FileEntity> file) =>
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
            var d3Highlighter = HighlighterLibrary.Javascript();

            d3Highlighter.Rules.Insert(2, new WordRule("getColor getLabel getKey getClickKeys attr enterData append data d3 scale domain")
            {
                Formatter = new RuleFormatter("#5D4978")
            });

            box.CurrentHighlighter = d3Highlighter;
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
            var cs = sender as ChartScriptEntity;

            if (RequestWindow != null && e.PropertyName == "Script")
            {
                RequestWindow.Dispatcher.Invoke(() =>
                {
                    if (!RequestWindow.IsLoaded)
                        RequestWindow = null;
                    else
                        RequestWindow.SetResults(cs.Script); //If saved, different clone
                });
            }
        }

        public ChartRequestWindow RequestWindow { get; set; }
    }
}
