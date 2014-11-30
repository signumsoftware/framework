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
using Signum.Entities;
using Signum.Utilities;

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

        public UserEntity User { get; set; }

        private void bntOk_Click(object sender, RoutedEventArgs e)
        {
            if (pb1.Password != pb2.Password)
            {
                MessageBox.Show(this, AuthMessage.PasswordsDoNotMatch.NiceToString(), MessageBoxImage.Error.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (User.IsNew)
            {
                User.PasswordHash = Security.EncodePassword(Password);
            }
            else
            {
                Server.Execute((ILoginServer s) => s.ChagePassword(User.ToLite(), User.PasswordHash, Security.EncodePassword(Password)));
            }
            DialogResult = false;
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
