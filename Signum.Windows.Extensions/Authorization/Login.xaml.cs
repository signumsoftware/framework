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
using System.Windows.Media.Imaging;

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

        public string CompanyName
        {
            get { return tbCompanyName.Text; }
            set { tbCompanyName.Text = value; }
        }

        public string ProductName
        {
            get { return tbProductName.Text; }
            set { tbProductName.Text = value; }
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

        private BitmapImage _minimizarFocus = null;
        public BitmapImage MinimizarFocus {
            get
            {
                if (_minimizarFocus == null)
                    _minimizarFocus = new BitmapImage(PackUriHelper.Reference("Images/bminimizar-on.png","Signum.Windows.Extensions"));
                return _minimizarFocus;
            }
        }

        private BitmapImage _minimizarNoFocus = null;
        public BitmapImage MinimizarNoFocus
        {
            get
            {
                if (_minimizarNoFocus == null)
                    _minimizarNoFocus = new BitmapImage(PackUriHelper.Reference("Images/bminimizar.png","Signum.Windows.Extensions"));
                return _minimizarNoFocus;
            }
        }

        private BitmapImage _cerrarFocus = null;
        public BitmapImage CerrarFocus
        {
            get
            {
                if (_cerrarFocus == null)
                    _cerrarFocus = new BitmapImage(PackUriHelper.Reference("Images/bcerrar-on.png", "Signum.Windows.Extensions"));
                return _cerrarFocus;
            }
        }

        private BitmapImage _cerrarNoFocus = null;
        public BitmapImage CerrarNoFocus
        {
            get
            {
                if (_cerrarNoFocus == null)
                    _cerrarNoFocus = new BitmapImage(PackUriHelper.Reference("Images/bcerrar.png", "Signum.Windows.Extensions"));
                return _cerrarNoFocus;
            }
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

        private void EntraMinimizar(object sender, System.Windows.Input.MouseEventArgs e)
        {
            iMinimizar.Source = MinimizarFocus;
        }
        private void SaleMinimizar(object sender, System.Windows.Input.MouseEventArgs e)
        {
            iMinimizar.Source = MinimizarNoFocus;
        }

        private void EntraCerrar(object sender, System.Windows.Input.MouseEventArgs e)
        {
            iCerrar.Source = CerrarFocus;
        }
        private void SaleCerrar(object sender, System.Windows.Input.MouseEventArgs e)
        {
            iCerrar.Source = CerrarNoFocus;
        }
	}
}