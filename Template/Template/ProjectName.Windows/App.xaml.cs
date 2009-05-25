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

        protected override void OnStartup(StartupEventArgs e)
        {
            Navigator.NavigationManager = new NavigationManager
            {
                Settings = new Dictionary<Type, EntitySettings>()
                {
                    {typeof(MyEntityDN), new EntitySettings(false){ View = ()=> new MyEntity()} },
                    {typeof(NoteDN), new EntitySettings(false){ View = ()=>new Note(), IsCreable = admin=>false}},
                },
                QuerySetting = ((IQueryServer)Server.ServerProxy).GetQueryNames().ToDictionary(a => a, a => new QuerySetting()),
                ServerTypes = Server.RetrieveAll<TypeDN>().ToDictionary(a=>a.ClassName)                 
            };

            NotesProvider.Manager = new NotesProviderManager
            {
                CreateNote = ei => ei.IsNew ? null : new NoteDN { Entity = ei.ToLazy() },
                RetrieveNotes = ei => ei is INoteDN || ei.IsNew ? null : Server$custommessage$.Current.RetrieveNotes(ei.ToLazy())
            };
        }
    }
}
