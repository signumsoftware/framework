using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Signum.Entities;
using Signum.Entities.Isolation;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Windows.Controls;
using System.ServiceModel;
using System.Windows.Threading;

namespace Signum.Windows.Isolation
{
    public class IsolationClient
    {
        public static Func<Window, Lite<IsolationDN>> SelectIsolationInteractively;

        public static Func<Lite<IsolationDN>, ImageSource> GetIsolationIcon; 

        public static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => IsolationClient.Start(null)));
        }

        public static void Start(Func<Lite<IsolationDN>, ImageSource> getIsolationIcon)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Constructor.Manager.PreConstructors += Constructor_PreConstructors;

                WidgetPanel.GetWidgets += (e, c) => e is IdentifiableEntity && MixinDeclarations.IsDeclared(e.GetType(), typeof(IsolationMixin)) ?
                    new IsolationWidget() { Order = -1 } : null;

                List<Lite<IsolationDN>> isolations = null;

                Server.OnOperation += context =>
                {
                    var iso = IsolationDN.CurrentThreadVariable.Value;

                    if (iso != null)
                    {
                        var msg = new MessageHeader<string>(iso.KeyLong())
                            .GetUntypedHeader("CurrentIsolation", "http://www.signumsoftware.com/Isolation");
                        context.OutgoingMessageHeaders.Add(msg);
                    }
                };

                GetIsolationIcon = getIsolationIcon;

                SelectIsolationInteractively = owner =>
                {
                    if (isolations == null)
                        isolations = Server.RetrieveAllLite<IsolationDN>();

                    Lite<IsolationDN> result;
                    if (SelectorWindow.ShowDialog(isolations, out result,
                        elementIcon: getIsolationIcon,
                        elementText: iso => getIsolationIcon(iso) == null ? iso.ToString() : null,
                        title: IsolationMessage.SelectAnIsolation.NiceToString(),
                        message: IsolationMessage.SelectAnIsolation.NiceToString(),
                        owner: owner))
                        return result;

                    return null;
                };

                Async.OnShowInAnotherThread += Async_OnShowInAnotherThread;

                Navigator.Manager.TaskSearchWindow += Manager_TaskSearchWindow;
            }
        }

        static Action<Window> Async_OnShowInAnotherThread()
        {
            Lite<IsolationDN> current = IsolationDN.Current;

            return win =>
            {
                if (Application.Current.Dispatcher == Dispatcher.CurrentDispatcher)
                    throw new InvalidOperationException("Isolation can not be set in the main Thread");

                var entity = win.DataContext as IdentifiableEntity;

                if(entity != null)
                    current = current ?? entity.TryIsolation();

                IsolationDN.CurrentThreadVariable.Value = current;
            }; 
        }

        static void Manager_TaskSearchWindow(SearchWindow sw, object queryName)
        {
            if (IsolationDN.Default != null)
                return;

            var iso = IsolationDN.Current;
            
            if (iso == null)
                return;

            var tb = sw.Child<TextBox>(a => a.Name == "tbEntityType");

            tb.Before(new Image { Stretch = Stretch.None, SnapsToDevicePixels = true, Source = GetIsolationIcon(iso) }); 
        }


        static IDisposable Constructor_PreConstructors(ConstructorContext ctx)
        {
            if (MixinDeclarations.IsDeclared(ctx.Type, typeof(IsolationMixin)))
            {
                Lite<IsolationDN> isolation = GetIsolation(ctx.Element);

                if (isolation == null)
                {
                    ctx.CancelConstruction = true;
                    return null;
                }
                ctx.Args.Add(isolation);

                return IsolationDN.OverrideIfNecessary(isolation);
            }

            return null;
        }

        public static Lite<IsolationDN> GetIsolation(FrameworkElement element)
        {
            var result = IsolationDN.Current;
            if (result != null)
                return result;

            var entity = element == null ? null: element.DataContext as IdentifiableEntity;
            if (entity != null)
            {
                result = entity.TryIsolation();
                if (result != null)
                    return result;
            }

            return SelectIsolationInteractively(element == null ? null : Window.GetWindow(element));
        }
    }
}
