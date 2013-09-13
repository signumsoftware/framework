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
            Element.WaitDataContextSet(() => "DataContextSet for {0}".Formato(typeof(T).Name));
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
            get { return Element.ChildById("entityTitle").ChildById("tbEntityId").Value(); }
        }

        public string EntityToStr
        {
            get { return Element.ChildById("entityTitle").ChildById("tbEntityToStr").Value(); }
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
                        throw new InvalidOperationException("A window was open after pressing Ok on {0}. Consider using OkCapture".Formato(entityId));
                    }

                    return IsClosed;
                },
                actionDescription: () => "Waiting to close window after OK {0}".Formato(entityId));
        }

        public AutomationElement OkCapture()
        {
            var entityId = EntityId;
            return Element.CaptureWindow(
                action: () => ButtonBar.OkButton.ButtonInvoke(),
                actionDescription: () => "Waiting to capture window after OK {0}".Formato(entityId));
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

        public void Execute(Enum operationKey, int? timeOut = null)
        {
            var time = timeOut ?? OperationTimeouts.ExecuteTimeout;
            var entityId = EntityId;
            var button = ButtonBar.GetButton(operationKey);

            Element.WaitDataContextChangedAfter(
                action: () => button.ButtonInvoke(),
                actionDescription: () => "Executing {0} from {1}".Formato(OperationDN.UniqueKey(operationKey), entityId));
        }

        public AutomationElement ExecuteCapture(Enum operationKey, int? timeOut = null)
        {
            var time = timeOut ?? OperationTimeouts.ExecuteTimeout;
            var entityId = EntityId;
            var button = ButtonBar.GetButton(operationKey);

            return Element.CaptureWindow(
                action: () => button.ButtonInvoke(),
                actionDescription: () => "Executing {0} from {1} and waiting to capture window".Formato(OperationDN.UniqueKey(operationKey), entityId));
        }

        public void ExecuteCaptureDialog(Enum operationKey, Action<AutomationElement> dialogAction, int? timeOut = null)
        {
            var time = timeOut ?? OperationTimeouts.ExecuteTimeout;
            var entityId = EntityId;
            var button = ButtonBar.GetButton(operationKey);

            Element.WaitDataContextChangedAfter(
                action: () =>
                {
                    var dialog = Element.CaptureWindow(action: () => button.ButtonInvoke(),
                        actionDescription: () => "Executing {0} from {1} and waiting to capture window".Formato(OperationDN.UniqueKey(operationKey), entityId));

                    dialogAction(dialog);
                },
                actionDescription: () => "Executing {0} from {1}".Formato(OperationDN.UniqueKey(operationKey), entityId));
        }

        public AutomationElement ConstructFromCapture(Enum operationKey, int? timeOut = null)
        {
            var time = timeOut ?? OperationTimeouts.ConstructFromTimeout;
            var entityId = EntityId;

            var button = GetConstructorButton(operationKey);

            return Element.CaptureWindow(
                () => button.ButtonInvoke(),
                () => "Finding a window after {0} from {1} took more than {2} ms".Formato(OperationDN.UniqueKey(operationKey), entityId, time));
        }

        private AutomationElement GetConstructorButton(Enum operationKey)
        {
            var result = ButtonBar.TryGetButton(operationKey);
            if (result != null)
                return result;
            
            result = (from sc in MainControl.Descendants(a => a.Current.ClassName == "SearchControl").Select(sc => new SearchControlProxy(sc))
                      select sc.GetOperationButton(operationKey)).NotNull().FirstOrDefault();
            if (result != null)
                return result;

            throw new ElementNotFoundException("Button for operation {0} not found on the ButtonBar or any visible SearchControl".Formato(OperationDN.UniqueKey(operationKey)));
        }

        public NormalWindowProxy<R> ConstructFrom<R>(Enum operationKey, int? timeOut = null) where R : IdentifiableEntity
        {
            AutomationElement element = ConstructFromCapture(operationKey, timeOut);

            return new NormalWindowProxy<R>(element);
        }


        public override void Dispose()
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

        public static Lite<IIdentifiable> ParseLiteHash(string itemStatus)
        {
            if (string.IsNullOrEmpty(itemStatus))
                return null;

            return Signum.Entities.Lite.Parse(itemStatus.Split(new[] { " Hash:" }, StringSplitOptions.None)[0]);
        }

        public static Lite<T> Lite<T>(this NormalWindowProxy<T> entity) where T : IdentifiableEntity
        {
            return (Lite<T>)NormalWindowExtensions.ParseLiteHash(entity.Element.Current.ItemStatus);
        }
    }


    public class OperationTimeouts
    {
        public static int ExecuteTimeout = 3 * 1000;
        public static int ConstructFromTimeout = 2 * 1000;
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

        public AutomationElement GetButton(Enum operationKey)
        {
            var button = TryGetButton(operationKey);
            if (button == null)
                throw new ElementNotFoundException("No button for operation {0} found".Formato(OperationDN.UniqueKey(operationKey)));

            return button;
        }

        public AutomationElement TryGetButton(Enum operationKey)
        {
            var directButton = Element.TryChild(a => a.Current.ControlType == ControlType.Button && a.Current.Name == OperationDN.UniqueKey(operationKey));

            if (directButton != null)
                return directButton;

            foreach (var groupButton in Element.Children(a => a.Current.ControlType == ControlType.Button && a.Current.ItemStatus == "Group"))
            {
                string groupName = groupButton.Current.Name;

                var window = Element.CaptureChildWindow(
                    () => groupButton.ButtonInvoke(),
                    actionDescription: () => "Waiting for ContextMenu after click on {0}".Formato(groupName));

                var menuItem = window
                    .Child(a => a.Current.ControlType == ControlType.Menu)
                    .TryChild(a => a.Current.ControlType == ControlType.MenuItem && a.Current.Name == OperationDN.UniqueKey(operationKey));

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
                actionDescription: () => "Waiting to capture window after QuickLink {0} on LeftPanel".Formato(name));
        }

        public static SearchWindowProxy QuickLinkExplore(this LeftPanelProxy left, object queryName)
        {
            return left.QuickLinkCapture(QueryUtils.GetQueryUniqueKey(queryName)).ToSearchWindow(); 
        }

        public static NormalWindowProxy<T> QuickLinkNavigate<T>(this LeftPanelProxy left) where T : IdentifiableEntity
        {
            return left.QuickLinkCapture(QueryUtils.GetQueryUniqueKey(typeof(T).FullName)).ToNormalWindow<T>();
        }
    }
}
