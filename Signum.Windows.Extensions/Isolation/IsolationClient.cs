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
                Constructor.Manager.PostConstructors += Manager_PostConstructors;

                WidgetPanel.GetWidgets += (e, c) => e is IdentifiableEntity && MixinDeclarations.IsDeclared(e.GetType(), typeof(IsolationMixin)) ? new IsolationWidget() : null;

                List<Lite<IsolationDN>> isolations = null;

                GetIsolationIcon = getIsolationIcon;

                SelectIsolationInteractively = owner =>
                {
                    if(isolations == null)
                        isolations = Server.RetrieveAllLite<IsolationDN>(); 
                    
                    Lite<IsolationDN> result;
                    if (SelectorWindow.ShowDialog(isolations, out result,
                        elementIcon: getIsolationIcon,
                        elementText: iso=> getIsolationIcon(iso) == null ? iso.ToString() : null,
                        title: IsolationMessage.SelectAnIsolation.NiceToString(),
                        message: IsolationMessage.SelectAnIsolation.NiceToString(), 
                        owner: owner))
                        return result;

                    return null;
                };

                Navigator.Manager.GetArgs += () =>
                {
                    var win = Navigator.Manager.GetCurrentWindow();
                    
                    var ident = win.DataContext as IdentifiableEntity;
                    if (ident != null)
                    {
                        var iso = ident.TryIsolation();

                        if (iso != null)
                            return iso;
                    }

                    var sw = win as SearchWindow;

                    if (sw != null)
                    {
                        return sw.SearchControl.Args.TryGetArgC<Lite<IsolationDN>>();
                    }

                    return null;
                };

                Navigator.Manager.TaskSearchWindow += Manager_TaskSearchWindow;
            }
        }

        static void Manager_TaskSearchWindow(SearchWindow sw, object queryName)
        {
            if(IsolationDN.Default!=null)
                return;

            var iso = sw.Args.TryGetArgC<Lite<IsolationDN>>();
            
            if (iso == null)
                return;

            var tb = sw.Child<TextBox>(a => a.Name == "tbEntityType");

            tb.Before(new Image { Stretch = Stretch.None, SnapsToDevicePixels = true, Source = GetIsolationIcon(iso) }); 
        }

        static bool Manager_PostConstructors(Type type, FrameworkElement element, List<object> args, object result)
        {
            var iden = result as IdentifiableEntity;

            if (iden != null && MixinDeclarations.IsDeclared(type, typeof(IsolationMixin)) && iden.Isolation() == null)
            {
                iden.SetIsolation(args.TryGetArgC<Lite<IsolationDN>>());
            }

            return true;
        }

        static bool Constructor_PreConstructors(Type type, FrameworkElement element, List<object> args)
        {
            if (MixinDeclarations.IsDeclared(type, typeof(IsolationMixin)))
            {
                Lite<IsolationDN> isolation = GetIsolation(element);

                if (isolation == null)
                    return false;

                args.Add(isolation);
            }

            return true;
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
