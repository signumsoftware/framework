using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Threading;
using Signum.Windows;
using Bugs.Entities;
using Bugs.Windows.Controls;
using Signum.Services;
using System.Threading;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Windows.Basics;
using System.Windows.Controls;
using Signum.Windows.Extensions;
using Signum.Entities.Extensions;

namespace Bugs.Windows
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
                    {typeof(BugDN), new EntitySettings(false){ View = ()=> new Bug()} },
                    {typeof(ProjectDN), new EntitySettings(false){ View = ()=> new Project()} },
                    {typeof(DeveloperDN), new EntitySettings(false){ View = ()=> new Developer()} },
                    {typeof(CustomerDN), new EntitySettings(true){ View = ()=> new Customer()} },
                    {typeof(NoteDN), new EntitySettings(false){ View = ()=>new Note(), IsCreable = admin=>false}},
                    {typeof(ExcelReportDN), new EntitySettings(false){ View = ()=>new ExcelReport()}},
                },
                QuerySetting = ((IQueryServer)Server.ServerProxy).GetQueryNames().ToDictionary(a => a, a => new QuerySetting()),
                ServerTypes = Server.RetrieveAll<TypeDN>().ToDictionary(a=>a.ClassName)                 
            };

            NotesProvider.Manager = new NotesProviderManager
            {
                CreateNote = ei => ei.IsNew ? null : new NoteDN { Entity = ei.ToLazy() },
                RetrieveNotes = ei => ei is INoteDN || ei.IsNew ? null : ServerBugs.Current.RetrieveNotes(ei.ToLazy())
            };

            SearchControl.GetCustomMenuItems += queryName =>
                queryName != typeof(ExcelReportDN) ?
                    new MenuItem[] { new ExcelReportMenuItem(), new ExcelReportPivotTableMenuItem() } :
                    new MenuItem[] { new ExcelReportMenuItem() }; 
        }
    }
}
