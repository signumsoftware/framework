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

            InitializeComponent();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Fix so App.xaml InitializeComponent gets generated
        }

        void UnhandledAsyncException(Exception e)
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
            Navigator.Start(new NavigationManager());
            Constructor.Start(new ConstructorManager());

            Navigator.AddSettings(new List<EntitySettings>
            {
                new EntitySettings<MyEntityDN>(EntityType.Default) { View = e => new MyEntity() }
            }); 

            Navigator.Initialize();
        }
    }
}
