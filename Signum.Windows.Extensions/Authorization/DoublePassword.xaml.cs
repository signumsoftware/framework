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
using Signum.Utilities;
using Signum.Services;

namespace Signum.Windows.Authorization
{
    /// <summary>
    /// Interaction logic for Password.xaml
    /// </summary>
    public partial class DoublePassword : UserControl
    {
        public static readonly DependencyProperty PasswordHashProperty =
            DependencyProperty.Register("PasswordHash", typeof(byte[]), typeof(DoublePassword), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (o, e) => ((DoublePassword)o).PasswordChanged(e)));
        public byte[] PasswordHash
        {
            get { return (byte[])GetValue(PasswordHashProperty); }
            set { SetValue(PasswordHashProperty, value); }
        }

        public static readonly DependencyProperty ErrorProperty =
            DependencyProperty.Register("Error", typeof(string), typeof(DoublePassword), new UIPropertyMetadata(null));
        public string Error
        {
            get { return (string)GetValue(ErrorProperty); }
            set { SetValue(ErrorProperty, value); }
        }

        public DoublePassword()
        {
            InitializeComponent();
        }

        private void password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (changing)
                return;

            if (!password.Password.HasText())
            {
                Error = "Password incompleto";
                return;
            }

            if (!password2.Password.HasText())
            {
                Error = "Password 2 incompleto";
                return;
            }

            if (password2.Password != password.Password)
            {
                Error = "Passwords distintos";
                return;
            }

            Error = null;

            byte[] hash = Security.EncodePassword(password.Password);

            PasswordHash = hash;

            CleanPasswords();
        }

        bool changing; 
        private void PasswordChanged(DependencyPropertyChangedEventArgs e)
        {
            CleanPasswords();
        }

        private void CleanPasswords()
        {
            try
            {
                changing = true;
                password.Password = "";
                password2.Password = "";
            }
            finally
            {
                changing = false;
            }
        }
    }
}
