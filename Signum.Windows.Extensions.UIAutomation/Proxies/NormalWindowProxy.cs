using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities;
using Signum.Entities;
using System.Windows;
using System.Linq.Expressions;
using Signum.Entities.Reflection;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Isolation;
using System.Diagnostics;

namespace Signum.Windows.UIAutomation
{
    public class NormalWindowProxy<T> : WindowProxy, ILineContainer<T> where T : ModifiableEntity
    {
        public WindowProxy ParentWindow { get { return this; } }
        public PropertyRoute PreviousRoute { get; set; }

        AutomationElement ILineContainer.Element
        {
            get { return this.MainControl; }
        }

        public NormalWindowProxy(AutomationElement element)
            : base(element.AssertClassName("NormalWindow"))
        {
            Element.WaitDataContextSet(() => "DataContextSet for {0}".FormatWith(typeof(T).Name));
        }

        public ButtonBarProxy ButtonBar
        {
            get { return new ButtonBarProxy(Element.ChildById("buttonBar")); }
        }

        public LeftPanelProxy LeftExpander
        {
            get { return new LeftPanelProxy(Element.ChildById("widgetPanel").ChildById("expander")); }
        }

        AutomationElement mainControl;
        public AutomationElement MainControl
        {
            get { return mainControl ?? (mainControl = Element.Child(a => a.Current.ClassName == "ScrollViewer").Child(a => a.Current.ControlType != ControlType.ScrollBar)); }
        }

        public string EntityId
        {
            get { return Element.WaitChildById("entityTitle").WaitChildById("tbEntityId").Value(); }
        }

        public string EntityToStr
        {
            get { return Element.WaitChildById("entityTitle").WaitChildById("tbEntityToStr").Value(); }
        }

        public void Ok()
        {
            var entityId = EntityId;
            ButtonBar.OkButton.ButtonInvoke();
            Element.Wait(
                () => 
                {
                    var childWindows = Element.TryChild(a => a.Current.ControlType == ControlType.Window);

                    if (childWindows != null)
                    {
                        MessageBoxProxy.ThrowIfError(childWindows);
                        throw new InvalidOperationException("A window ({0})was open after pressing Ok on {1}. Consider using OkCapture".FormatWith(WaitExtensions.NiceToString(childWindows), entityId));
                    }

                    return IsClosed;
                },
                actionDescription: () => "Waiting to close window after OK {0}".FormatWith(entityId));
        }

        public AutomationElement OkCapture()
        {
            var entityId = EntityId;
            return Element.CaptureWindow(
                action: () => ButtonBar.OkButton.ButtonInvoke(),
                actionDescription: () => "Waiting to capture window after OK {0}".FormatWith(entityId));
        }

        public void Reload()
        {
            var entityId = EntityId;
            Element.WaitDataContextChangedAfter(
                action: () => ButtonBar.ReloadButton.ButtonInvoke(),
                actionDescription: () => "Reload " + entityId);
        }

        public void Reload(bool confirm)
        {
            ButtonBar.ReloadButton.ButtonInvoke();

            using (var mb = Element.WaitMessageBoxChild())
            {
                if (confirm)
                    mb.OkButton.ButtonInvoke();
                else
                    mb.CancelButton.ButtonInvoke();
            }
        }

        public override void Dispose()
        {
            try
            {
                if (base.Close())
                {
                    MessageBoxProxy confirmation = null;

                    Element.Wait(() =>
                    {
                        try
                        {
                            if (IsClosed)
                                return true;

                            confirmation = Element.TryMessageBoxChild();

                            if (confirmation != null)
                                return true;

                            base.Close();

                            return false;
                        }
                        catch (ElementNotAvailableException)
                        {
                            return true;
                        }
                    }, () => "Waiting for normal window to close or show confirmation dialog");


                    if (confirmation != null && !confirmation.IsError)
                    {
                        confirmation.OkButton.ButtonInvoke();
                    }
                }

                OnDisposed();
            }
            catch
            {
                if (CurrentException == null)
                    throw;
            }
        }

        public void CloseLooseChanges()
        {
            Element.CaptureChildWindow(
                () => Close(), 
                ()=>"Waiting for loose changes dialog");
        }
    }

    public static class NormalWindowExtensions
    {
        public static NormalWindowProxy<T> ToNormalWindow<T>(this AutomationElement element) where T : ModifiableEntity
        {
            return new NormalWindowProxy<T>(element);
        }

        public static Lite<IEntity> ParseLiteHash(string itemStatus)
        {
            if (string.IsNullOrEmpty(itemStatus))
                return null;

            return Signum.Entities.Lite.Parse(itemStatus.Split(new[] { " Hash:" }, StringSplitOptions.None)[0]);
        }

        public static Lite<T> Lite<T>(this NormalWindowProxy<T> entity) where T : Entity
        {
            return (Lite<T>)NormalWindowExtensions.ParseLiteHash(entity.Element.Current.ItemStatus);
        }
    }

    public static class NormalWindowOperationExpressions
    {
        public static void Execute<T>(this NormalWindowProxy<T> window, ExecuteSymbol<T> symbol, int? timeOut = null)
              where T : Entity
        {
            var entityId = window.EntityId;
            var button = window.GetOperationButton(symbol.Symbol);

            window.Element.WaitDataContextChangedAfter(
                action: () => button.ButtonInvoke(),
                timeOut : timeOut ?? OperationTimeouts.ExecuteTimeout,
                actionDescription: () => "Executing {0} from {1}".FormatWith(symbol.Symbol, entityId));
        }

        public static AutomationElement ExecuteCapture<T>(this NormalWindowProxy<T> window, ExecuteSymbol<T> symbol, int? timeOut = null)
            where T : Entity
        {
            return window.OperationCapture(symbol.Symbol, timeOut); 
        }

        public static NormalWindowProxy<T> ConstructFrom<F, FB, T>(this NormalWindowProxy<F> window, ConstructSymbol<T>.From<FB> symbol, int? timeOut = null)
            where T : Entity
            where FB : class, IEntity
            where F : Entity, FB
        {
            AutomationElement element = window.OperationCapture(symbol.Symbol, timeOut);

            return new NormalWindowProxy<T>(element);
        }


        public static bool IsOperationEnabled<T>(this NormalWindowProxy<T> window, OperationSymbol operationSymbol)
             where T : Entity
        {
            return window.ButtonBar.GetButton(operationSymbol).Current.IsEnabled;
        }

        public static void OperationDialog<T>(this NormalWindowProxy<T> window, OperationSymbol operationSymbol, Action<AutomationElement> dialogAction, int? timeOut = null)
            where T : Entity
        {
            var time = timeOut ?? OperationTimeouts.ExecuteTimeout;
            var entityId = window.EntityId;
            var button = window.ButtonBar.GetButton(operationSymbol);

            window.Element.WaitDataContextChangedAfter(
                action: () =>
                {
                    var dialog = window.Element.CaptureWindow(action: () => button.ButtonInvoke(),
                        actionDescription: () => "Executing {0} from {1} and waiting to capture window".FormatWith(operationSymbol.Key, entityId));

                    dialogAction(dialog);
                },
                actionDescription: () => "Executing {0} from {1}".FormatWith(operationSymbol.Key, entityId));
        }

        public static AutomationElement OperationCapture<T>(this NormalWindowProxy<T> window, OperationSymbol operationSymbol, int? timeOut = null)
            where T : Entity
        {
            var time = timeOut ?? OperationTimeouts.ConstructFromTimeout;
            var entityId = window.EntityId;

            var button = window.GetOperationButton(operationSymbol);

            return window.Element.CaptureWindow(
                () => button.ButtonInvoke(),
                actionDescription : () => "Finding a window after {0} from {1} took more than {2} ms".FormatWith(operationSymbol.Key, entityId, time));
        }

        public static AutomationElement GetOperationButton<T>(this NormalWindowProxy<T> window, OperationSymbol operationSymbol)
             where T : Entity
        {
            var result = window.ButtonBar.TryGetButton(operationSymbol);
            if (result != null)
                return result;

            result = (from sc in window.MainControl.Descendants(a => a.Current.ClassName == "SearchControl").Select(sc => new SearchControlProxy(sc))
                      select sc.GetOperationButton(operationSymbol)).NotNull().FirstOrDefault();
            if (result != null)
                return result;

            throw new ElementNotFoundException("Button for operation {0} not found on the ButtonBar or any visible SearchControl".FormatWith(operationSymbol.Key));
        }
    }

    public class OperationTimeouts
    {
        public static int ExecuteTimeout = 10 * 1000;
        public static int ConstructFromTimeout = 10 * 1000;
    }


    public class ButtonBarProxy
    {
        public AutomationElement Element { get; private set; }

        public ButtonBarProxy(AutomationElement element)
        {
            Element = element;
        }

        public AutomationElement OkButton
        {
            get { return Element.ChildById("btOk"); }
        }

        public AutomationElement ReloadButton
        {
            get { return Element.ChildById("btReload"); }
        }

        public AutomationElement GetButton(OperationSymbol operationSymbol)
        {
            var button = TryGetButton(operationSymbol);
            if (button == null)
                throw new ElementNotFoundException("No button for operation {0} found".FormatWith(operationSymbol.Key));

            return button;
        }

        public AutomationElement TryGetButton(OperationSymbol operationSymbol)
        {
            var directButton = Element.TryChild(a => a.Current.ControlType == ControlType.Button && a.Current.Name == operationSymbol.Key);

            if (directButton != null)
                return directButton;

            foreach (var groupButton in Element.Children(a => a.Current.ControlType == ControlType.Button && a.Current.ItemStatus == "Group"))
            {
                string groupName = groupButton.Current.Name;

                AutomationElement window;
                int count = 0;
            retry:
                try
                {
                  
                    count++;
                    window = Element.CaptureChildWindow(
                        () => groupButton.ButtonInvoke(),
                        actionDescription: () => "Waiting for ContextMenu after click on {0}".FormatWith(groupName));
                }
                catch
                {
                    if (count < 2)
                        goto retry;

                    throw;
                }

                var menuItem = window
                    .Child(a => a.Current.ControlType == ControlType.Menu)
                    .TryChild(a => a.Current.ControlType == ControlType.MenuItem && a.Current.Name == operationSymbol.Key);

                if (menuItem != null)
                    return menuItem;
            }

            return null;
        }
    }


    public class LeftPanelProxy
    {
        public AutomationElement Element {get;set;}

        public LeftPanelProxy(AutomationElement element)
        {
            this.Element = element;
        }

        public AutomationElement LeftExpanderButton
        {
            get { return Element.ChildById("HeaderSite"); }
        }
    }

    public static class QuickLinkExtensions
    {
        public static AutomationElement Button(this LeftPanelProxy left, string name)
        {
            return left.Element.Descendant(el => el.Current.ControlType == ControlType.Button && el.Current.Name == name);
        }

        public static AutomationElement QuickLinks(this LeftPanelProxy left)
        {
            return left.Element.Child(c => c.Current.ClassName == "LinksWidget").ChildById("expQuickLinks").Child(c => c.Current.ControlType == ControlType.Pane);
        }

        public static AutomationElement QuickLinkCapture(this LeftPanelProxy left, string name)
        {
            var button = left.QuickLinks().Child(c => c.Current.Name == name).Child(c => c.Current.ControlType == ControlType.Button);

            return button.ButtonInvokeCapture(
                actionDescription: () => "Waiting to capture window after QuickLink {0} on LeftPanel".FormatWith(name));
        }

        public static SearchWindowProxy QuickLinkExplore(this LeftPanelProxy left, object queryName)
        {
            return left.QuickLinkCapture(QueryUtils.GetQueryUniqueKey(queryName)).ToSearchWindow(); 
        }

        public static NormalWindowProxy<T> QuickLinkNavigate<T>(this LeftPanelProxy left) where T : Entity
        {
            return left.QuickLinkCapture(QueryUtils.GetQueryUniqueKey(typeof(T).FullName)).ToNormalWindow<T>();
        }
    }

    public static class IsolationExtensions
    {
        public static Lite<IsolationEntity> GetIsolation<T>(this NormalWindowProxy<T> window)
             where T: Entity
        {
            throw new NotImplementedException();
        }
    }
}
