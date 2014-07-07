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
using Signum.Entities.UserQueries;
using Signum.Services;

namespace Signum.Windows.UserAssets
{
    public partial class ImportUserAssetsConfirmation : Window
    {   
        public ImportUserAssetsConfirmation()
        {
            InitializeComponent();
        }

        private void OkCancelBar_OkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void OkCancelBar_CancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
