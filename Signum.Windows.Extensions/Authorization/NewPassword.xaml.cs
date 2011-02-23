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

using Signum.Services;
using Signum.Windows;

using Signum.Entities.Authorization;

namespace Signum.Windows.Authorization
{
    /// <summary>
    /// Interaction logic for NewPassword.xaml
    /// </summary>
    public partial class NewPassword : Window
    {
      
        public NewPassword()
        {
            InitializeComponent();
        }
        public string Password
        {
            get { return pb1.Password; }
        }

        private void bntOk_Click(object sender, RoutedEventArgs e)
        {
            
         
            if (pb1.Password != pb2.Password)
            {
                MessageBox.Show(this, "Password do not match", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Server.Execute((ILoginServer a) => a.ChagePassword(UserDN.Current.UserName, UserDN.Current.PasswordHash, Security.EncodePassword(Password)));

            MessageBox.Show(this, "Password changed", "Notice", MessageBoxButton.OK);

            DialogResult = false;

        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
