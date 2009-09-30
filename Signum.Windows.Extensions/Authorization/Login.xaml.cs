using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Signum.Services;
using Signum.Utilities;
using System.Threading;
using Signum.Windows;
using System.Windows.Threading;

namespace Signum.Windows.Authorization
{
	public partial class Login
	{
		public Login()
		{
			this.InitializeComponent();
		}

        public string UserName
        {
            get { return tbUserName.Text; }
            set { tbUserName.Text = value; }
        }

        public string Password
        {
            get { return tbPassword.Password; }
            set { tbPassword.Password = value; }
        }

        public string Error
        {
            get { return txtError.Text; }
            set { txtError.Text = value; }
        }

        public void FocusUserName()
        {
            tbUserName.SelectAll();
            tbUserName.Focus();
        }

        public void FocusPassword()
        {
            tbPassword.SelectAll();
            tbPassword.Focus();
        }

        public event EventHandler LoginClicked; 

        private void btLogin_Click(object sender, RoutedEventArgs e)
        {
            if (LoginClicked == null)
                throw new ApplicationException("LoginClicked not attached"); 

            LoginClicked(this, null); 
        }

        private void btSalir_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
                this.DragMove();

        }

        private void Cerrar(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Minimizar(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
	}
}