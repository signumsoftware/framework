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
using Microsoft.Win32;
using Signum.Windows;
using Bugs.Entities;

namespace Bugs.Windows.Controls
{
    /// <summary>
    /// Interaction logic for CustomerDN.xaml
    /// </summary>
    public partial class Bug : UserControl, IHaveQuickLinks
    {
        public Bug()
        {
            InitializeComponent();
        }
        
        private void FileLine_CustomizeFileDialog(FileDialog obj)
        {
            obj.Filter = "Bitmap Image|*.bmp|JPEG Image|*.jpg;*.jpeg|PNG Image|*.png|GIF Image|*.gif";
        }


        public List<QuickLink> QuickLinks()
        {
            return new List<QuickLink>
            {
                new QuickLink("Comments")
                {
                     Action = () => Navigator.Find(typeof(CommentDN))
                }
            };
        }

    }
}
