using Microsoft.Win32;
using Signum.Entities.Files;
using Signum.Entities.Mailing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Signum.Windows.Mailing
{
    /// <summary>
    /// Interaction logic for EmailAttachment.xaml
    /// </summary>
    public partial class EmailAttachment : UserControl
    {
        public EmailAttachment()
        {
            InitializeComponent();
        }

        private Signum.Entities.Files.IFile File_Creating()
        {
            return new FilePathEntity(EmailFileType.Attachment);
        }
    }
}
