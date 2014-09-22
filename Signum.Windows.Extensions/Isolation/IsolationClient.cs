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
using Signum.Services;

namespace Signum.Windows.Isolation
{
    public class IsolationClient
    {
        public static Func<Window, Func<Lite<IsolationDN>, string>, Lite<IsolationDN>> SelectIsolationInteractively;

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
                    new IsolationWidget().Set(Common.OrderProperty, -1.0) : null;

                List<Lite<IsolationDN>> isolations = null;

                Server.OnOperation += context =>
                {
                    var iso = IsolationDN.CurrentThreadVariable.Value;

                    if (iso != null)
                    {
                        var msg = new MessageHeader<string>(iso.Item1.Try(i=>i.KeyLong()))
                            .GetUntypedHeader("CurrentIsolation", "http://www.signumsoftware.com/Isolation");
                        context.OutgoingMessageHeaders.Add(msg);
                    }
                };

                GetIsolationIcon = getIsolationIcon;

                SelectIsolationInteractively = (owner, isValid) =>
                {
                    if (isolations == null)
                        isolations = Server.RetrieveAllLite<IsolationDN>();


                    var isos = isValid == null ? isolations : isolations.Where(i => isValid(i) == null).ToList();

                    Lite<IsolationDN> result;
                    if (SelectorWindow.ShowDialog(isos, out result,
                        elementIcon: getIsolationIcon,
                        elementText: iso => getIsolationIcon(iso) == null ? iso.ToString() : null,
                        title: IsolationMessage.SelectAnIsolation.NiceToString(),
                        message: IsolationMessage.SelectAnIsolation.NiceToString(),
                        owner: owner))
                        return result;

                    return null;
                };

                Async.OnShowInAnotherThread += Async_OnShowInAnotherThread;

                Finder.Manager.TaskSearchWindow += Manager_TaskSearchWindow;
            }
        }

        static Action<Window> Async_OnShowInAnotherThread()
        {
            if (IsolationDN.Default != null)
                return win => { };

            Lite<IsolationDN> prev = IsolationDN.Current;

            return win =>
            {
                if (Application.Current.Dispatcher == Dispatcher.CurrentDispatcher)
                    throw new InvalidOperationException("Isolation can not be set in the main Thread");

                if (win.DataContext != null)
                {
                    SetIsolation(win.DataContext as IdentifiableEntity, prev);
                }
                else if (win is NormalWindow)
                {
                    ((NormalWindow)win).PreEntityLoaded += (w, args) =>
                    {
                        SetIsolation(args.Entity as IdentifiableEntity, prev);
                        return;
                    };
                }
                else if (prev != null)
                {
                    IsolationDN.CurrentThreadVariable.Value = Tuple.Create(prev);
                }
            }; 
        }

        private static void SetIsolation(IdentifiableEntity entity, Lite<IsolationDN> prev)
        {
            if (entity == null)
                IsolationDN.CurrentThreadVariable.Value = Tuple.Create(prev);
            else
            {
                var cur = entity.TryIsolation();
                IsolationDN.CurrentThreadVariable.Value = Tuple.Create(cur);
            }
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

        public static Dictionary<Type, Func<Lite<IsolationDN>, string>> IsValid = new Dictionary<Type, Func<Lite<IsolationDN>, string>>();

        public static void RegisterIsValid<T>(Func<Lite<IsolationDN>, string> isValid) where T : IdentifiableEntity
        {
            IsValid[typeof(T)] = isValid;
        }

        static IDisposable Constructor_PreConstructors(ConstructorContext ctx)
        {
            if (MixinDeclarations.IsDeclared(ctx.Type, typeof(IsolationMixin)))
            {
                Lite<IsolationDN> isolation = GetIsolation(ctx, IsValid.TryGetC(ctx.Type));

                if (isolation == null)
                {
                    ctx.CancelConstruction = true;
                    return null;
                }
                ctx.Args.Add(isolation);

                return IsolationDN.Override(isolation);
            }

            return null;
        }

        public static Lite<IsolationDN> GetIsolation(ConstructorContext ctx, Func<Lite<IsolationDN>, string> isValid = null)
        {
            var element = ctx.Element;

            var result = IsolationDN.Current;

            if (result == null)
            {
                var entity = element == null ? null : element.DataContext as IdentifiableEntity;
                if (entity != null)
                    result = entity.TryIsolation();
            }

            if (result == null)
            {
                var sc = element as SearchControl ?? (element as SearchWindow).Try(s => s.SearchControl);
                if (sc != null && ctx.OperationInfo != null && (
                    ctx.OperationInfo.OperationType == OperationType.ConstructorFrom ||
                    ctx.OperationInfo.OperationType == OperationType.ConstructorFromMany))
                    result = Server.Return((IIsolationServer a) => a.GetOnlyIsolation(sc.SelectedItems.ToList()));
            }

            if (result != null)
            {
                var error = isValid == null ? null : isValid(result);
                if (error != null)
                    throw new ApplicationException(error);

                return result;
            }

            return SelectIsolationInteractively(element == null ? null : Window.GetWindow(element), isValid);
        }
    }
}
