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
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Windows;
using Signum.Services;
using Signum.Utilities;

namespace Signum.Windows.Authorization
{
    /// <summary>
    /// Interaction logic for Usuario.xaml
    /// </summary>
    public partial class User : UserControl
    {
        public User()
        {
            InitializeComponent();
        }

        private void changePassword_Click(object sender, RoutedEventArgs e)
        {
            UserEntity user = (UserEntity)DataContext;
            var np = new NewPassword()
            {
                User = user,
                Owner = Window.GetWindow(this)
            };


            if (np.ShowDialog() == true)
            {
                if (!user.IsNew)
                {
                    MessageBox.Show(Window.GetWindow(this), AuthMessage.PasswordChanged.NiceToString(), MessageBoxImage.Information.ToString(), MessageBoxButton.OK, MessageBoxImage.Information);

                    this.RaiseEvent(new ChangeDataContextEventArgs(user.ToLite().Retrieve()));
                }
            }
        }
    }
}
