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
using Signum.Windows.Extensions;

namespace Signum.Windows.Authorization
{
    public partial class Login
    {
        public Login()
        {
            this.InitializeComponent();
            //estas instanciacniones están aquí porque en este punto todavía no se ha registrado el schema de Uri
            //para "pack", ya que el login se hace un poco antes de lanzarse el registro de la aplicación. Una manera de evitarlo
            //sería poner esta línea:
            //UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", -1);
            //en primer lugar en el Main de Program.cs de la solución.
            _minimizarFocus = ExtensionsImageLoader.GetImageSortName("bminimizar-on.png");
            _minimizarNoFocus = ExtensionsImageLoader.GetImageSortName("bminimizar.png");
            _cerrarFocus = ExtensionsImageLoader.GetImageSortName("bcerrar-on.png");
            _cerrarNoFocus = ExtensionsImageLoader.GetImageSortName("bcerrar.png");
            SettingNewPassword = false;
            ReLogin = false;
        }

        private bool reLogin;
        public bool ReLogin
        {
            get { return settingNewPassword; }
            set
            {
                reLogin = value;
                this.tbUserName.IsReadOnly = value;
               
            }
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


        public string NewPassword
        {
            get { return tbNewPassword1.Password; }
            set { tbNewPassword1.Password = value; }
        }
        public string NewPassword2
        {
            get { return tbNewPassword2.Password; }
            set { tbNewPassword2.Password = value; }
        }
        private GridLength rowHeight ;
        private bool settingNewPassword;
        public bool SettingNewPassword
        {
            get { return settingNewPassword; }
            set
            {
                settingNewPassword = value;
                if (settingNewPassword)
                {
                    this.LayoutRoot.RowDefinitions[6].Height = rowHeight;
                    this.LayoutRoot.RowDefinitions[7].Height = rowHeight;
                }
                else
                {
                    rowHeight = this.LayoutRoot.RowDefinitions[6].Height;
                    this.LayoutRoot.RowDefinitions[6].Height = new GridLength(0);
                    this.LayoutRoot.RowDefinitions[7].Height = new GridLength(0);

                }
            }
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

        public void FocusNewPassword1()
        {
            tbNewPassword1.SelectAll();
            tbNewPassword1.Focus();
        }

        private BitmapFrame _minimizarFocus;

        private BitmapFrame _minimizarNoFocus;

        private BitmapFrame _cerrarFocus;

        private BitmapFrame _cerrarNoFocus;

        public  event EventHandler LoginClicked;

        private void btLogin_Click(object sender, RoutedEventArgs e)
        {
            if (LoginClicked == null)
                throw new InvalidOperationException("LoginClicked not attached");

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
            iMinimizar.Source = _minimizarFocus;
        }
        private void SaleMinimizar(object sender, System.Windows.Input.MouseEventArgs e)
        {
            iMinimizar.Source = _minimizarNoFocus;
        }

        private void EntraCerrar(object sender, System.Windows.Input.MouseEventArgs e)
        {
            iCerrar.Source = _cerrarFocus;
        }
        private void SaleCerrar(object sender, System.Windows.Input.MouseEventArgs e)
        {
            iCerrar.Source = _cerrarNoFocus;
        }
    }
}