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
        public static Func<Window, Func<Lite<IsolationEntity>, string>, Lite<IsolationEntity>> SelectIsolationInteractively;

        public static Func<Lite<IsolationEntity>, ImageSource> GetIsolationIcon; 

        public static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => IsolationClient.Start(null)));
        }

        public static void Start(Func<Lite<IsolationEntity>, ImageSource> getIsolationIcon)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Constructor.Manager.PreConstructors += Constructor_PreConstructors;

                WidgetPanel.GetWidgets += (e, c) => e is Entity && MixinDeclarations.IsDeclared(e.GetType(), typeof(IsolationMixin)) ?
                    new IsolationWidget().Set(Common.OrderProperty, -1.0) : null;

                List<Lite<IsolationEntity>> isolations = null;

                Server.OnOperation += context =>
                {
                    var iso = IsolationEntity.CurrentThreadVariable.Value;

                    if (iso != null)
                    {
                        var msg = new MessageHeader<string>(iso.Item1?.Let(i=>i.KeyLong()))
                            .GetUntypedHeader("CurrentIsolation", "http://www.signumsoftware.com/Isolation");
                        context.OutgoingMessageHeaders.Add(msg);
                    }
                };

                GetIsolationIcon = getIsolationIcon;

                SelectIsolationInteractively = (owner, isValid) =>
                {
                    if (isolations == null)
                        isolations = Server.RetrieveAllLite<IsolationEntity>();


                    var isos = isValid == null ? isolations : isolations.Where(i => isValid(i) == null).ToList();

                    if (SelectorWindow.ShowDialog(isos, out Lite<IsolationEntity> result,
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
            if (IsolationEntity.Default != null)
                return win => { };

            Lite<IsolationEntity> prev = IsolationEntity.Current;

            return win =>
            {
                if (Application.Current.Dispatcher == Dispatcher.CurrentDispatcher)
                    throw new InvalidOperationException("Isolation can not be set in the main Thread");

                if (win.DataContext != null)
                {
                    SetIsolation(win.DataContext as Entity, prev);
                }
                else if (win is NormalWindow)
                {
                    ((NormalWindow)win).PreEntityLoaded += (w, args) =>
                    {
                        SetIsolation(args.Entity as Entity, prev);
                        return;
                    };
                }
                else if (prev != null)
                {
                    IsolationEntity.CurrentThreadVariable.Value = Tuple.Create(prev);
                }
            }; 
        }

        private static void SetIsolation(Entity entity, Lite<IsolationEntity> prev)
        {
            if (entity == null)
                IsolationEntity.CurrentThreadVariable.Value = Tuple.Create(prev);
            else
            {
                var cur = entity.TryIsolation();
                IsolationEntity.CurrentThreadVariable.Value = Tuple.Create(cur);
            }
        }

        static void Manager_TaskSearchWindow(SearchWindow sw, object queryName)
        {
            if (IsolationEntity.Default != null)
                return;

            var iso = IsolationEntity.Current;
            
            if (iso == null)
                return;

            var tb = sw.Child<TextBox>(a => a.Name == "tbEntityType");

            tb.Before(new Image { Stretch = Stretch.None, SnapsToDevicePixels = true, Source = GetIsolationIcon(iso) }); 
        }

        public static Dictionary<Type, Func<Lite<IsolationEntity>, string>> IsValid = new Dictionary<Type, Func<Lite<IsolationEntity>, string>>();

        public static void RegisterIsValid<T>(Func<Lite<IsolationEntity>, string> isValid) where T : Entity
        {
            IsValid[typeof(T)] = isValid;
        }

        static IDisposable Constructor_PreConstructors(ConstructorContext ctx)
        {
            if (MixinDeclarations.IsDeclared(ctx.Type, typeof(IsolationMixin)))
            {
                Lite<IsolationEntity> isolation = GetIsolation(ctx, IsValid.TryGetC(ctx.Type));

                if (isolation == null)
                {
                    ctx.CancelConstruction = true;
                    return null;
                }
                ctx.Args.Add(isolation);

                return IsolationEntity.Override(isolation);
            }

            return null;
        }

        public static Lite<IsolationEntity> GetIsolation(ConstructorContext ctx, Func<Lite<IsolationEntity>, string> isValid = null)
        {
            var element = ctx.Element;

            var result = IsolationEntity.Current;

            if (result == null)
            {
                var entity = element == null ? null : element.DataContext as Entity;
                if (entity != null)
                    result = entity.TryIsolation();
            }

            if (result == null)
            {
                var sc = element as SearchControl ?? (element as SearchWindow)?.SearchControl;
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
