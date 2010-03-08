using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Services;
using Signum.Windows;
using Signum.Windows.Basics;
using $custommessage$.Entities;
using $custommessage$.Windows.Controls;

namespace $custommessage$.Windows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
            : base()
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.InvariantCulture.IetfLanguageTag)));

            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            Async.ExceptionHandler = UnhandledAsyncException;

            //InitializeComponent();
        }

        void UnhandledAsyncException(Exception e, Window win)
        {
            Program.HandleException("Error in async call", e);
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Program.HandleException("Unexpected error", e.Exception);
            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs args)
        {
            Navigator.Start(new NavigationManager
            {
                Settings = new Dictionary<Type, EntitySettings>()
                {
                    {typeof(MyEntityDN), new EntitySettings(EntityType.Default){ View = e => new MyEntity() } },
                },            
            });

            Constructor.Start(new ConstructorManager());

            Note.Start();
        }
    }
}
