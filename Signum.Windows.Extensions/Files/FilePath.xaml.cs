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
using Signum.Entities.Files;
using Signum.Utilities.Reflection;
using Signum.Entities.Reflection;
using Signum.Windows.Files;
using System.Diagnostics;
using System.IO;
using Signum.Utilities;

namespace Signum.Windows.Files
{
    /// <summary>
    /// Interaction logic for FilePath.xaml
    /// </summary>
    public partial class FilePath : UserControl, IHaveToolBarElements
    {
        public FilePath()
        {
            InitializeComponent();

            lblFullFP.Content = ReflectionTools.GetPropertyInfo((FilePathDN f) => f.FullPhysicalPath).NiceName();
            lblFullWP.Content = ReflectionTools.GetPropertyInfo((FilePathDN f) => f.FullWebPath).NiceName();
        }

        private void hypFullPhysicalPath_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                MessageBox.Show(Window.GetWindow(this), "FilePath is empty", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            Process.Start(((FilePathDN)DataContext).FullPhysicalPath);
        }

        private void hypFullWebPath_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                MessageBox.Show(Window.GetWindow(this), "FilePath is empty", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            System.Diagnostics.Process.Start(((FilePathDN)DataContext).FullWebPath);
        }

        #region IHaveToolBarElements Members


        public List<FrameworkElement> GetToolBarElements(object dataContext, EntityButtonContext ctx)
        {
            ToolBarButton button = new ToolBarButton()
            {
                Content = FileMessage.Open.NiceToString(),
                Image = ExtensionsImageLoader.GetImageSortName("document_view.png"),
            };

            button.Click += new RoutedEventHandler(buttonOpen_Click);

            return new List<FrameworkElement>() { button };
        }

        private FilePathDN Fp
        {
            get { return (FilePathDN)DataContext; }
        }

        private byte[] file = null;

        void buttonOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFile(Fp, file);
        }

        public static void OpenFile(FilePathDN fp, byte[] file)
        {
            if (fp == null || fp.IsNew)
                return;

            if (file == null)
                file = FileLine.DefaultResolveBinaryFile(fp);

            string fullPath = String.Empty;
            int loop = 0;
            do
            {
                string fileName = loop == 0 ? fp.FileName :
                    "{0}({1}){2}".Formato(System.IO.Path.GetFileNameWithoutExtension(fp.FileName),
                    loop, System.IO.Path.GetExtension(fp.FileName));
                fullPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
                loop++;
            } while (File.Exists(fullPath));
            File.WriteAllBytes(fullPath, file);
            Process.Start(fullPath);
        }

        #endregion
    }
}
