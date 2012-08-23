using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities;
using Signum.Entities.Operations;
using Signum.Entities;
using System.Windows;
using System.Linq.Expressions;
using Signum.Entities.Reflection;

namespace Signum.Windows.UIAutomation
{
    public class NormalWindowProxy<T>: WindowProxy, ILineContainer<T> where T: ModifiableEntity
    {
        public WindowProxy ParentWindow { get { return this;} }
        public PropertyRoute PreviousRoute { get; set; }

        AutomationElement ILineContainer.Element
        {
            get { return this.MainControl; }
        }

        public NormalWindowProxy(AutomationElement element)
            : base(element)
        {
            
        }

        public ButtonBarProxy ButtonBar
        {
            get { return new ButtonBarProxy(Element.ChildById("buttonBar")); }
        }

        public AutomationElement LeftExpander
        {
            get { return Element.ChildById("expander"); }
        }

        AutomationElement mainControl;
        public AutomationElement MainControl
        {
            get { return mainControl ?? (mainControl = Element.Child(a => a.Current.ClassName == "ScrollViewer").Child(a => a.Current.ControlType != ControlType.ScrollBar)); }
        }

        public string EntityId
        {
            get { return Element.ChildById("entityTitle").ChildById("entityId").Value(); }
        }

        public string EntityToStr
        {
            get { return Element.ChildById("entityTitle").ChildById("entityToStr").Value(); }
        }


        public void Ok()
        {
            ButtonBar.OkButton.ButtonInvoke();
        }

        public void Save()
        {
            ButtonBar.SaveButton.ButtonInvoke();

            this.WaitDataContextChangedAssertMessageBox(actionDescription: ()=> "Save " + EntityId);
        }

        public void Reload()
        {
            ButtonBar.ReloadButton.ButtonInvoke();

            this.WaitDataContextChangedAssertMessageBox(actionDescription: () => "Reload " + EntityId);
        }

        public void Reload(bool confirm)
        {
            ButtonBar.ReloadButton.ButtonInvoke();

            using (var mb = WaitCurrentMessageBox())
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

            ButtonBar.GetOperationButton(operationKey).ButtonInvoke();
            this.WaitDataContextChangedAssertMessageBox(actionDescription: () => "Executing {0} from {1}".Formato(OperationDN.UniqueKey(operationKey), EntityId));
        }

        public void WaitDataContextChangedAssertMessageBox(int? timeOut = null, Func<string> actionDescription = null)
        {
            Element.WaitDataContextChanged(this,timeOut, actionDescription);
        }


        public AutomationElement ConstructFrom(Enum operationKey, int? timeOut = null)
        {
            var time = timeOut ?? OperationTimeouts.ConstructFromTimeout;

            return GetWindowAfter(
                () => ButtonBar.GetOperationButton(operationKey).ButtonInvoke(),
                () => "Finding a window after {0} from {1} took more than {2} ms".Formato(OperationDN.UniqueKey(operationKey), EntityId, time));
        }

        public NormalWindowProxy<T> ConstructFrom<T>(Enum operationKey, int? timeOut = null) where T:IdentifiableEntity
        {
            AutomationElement element = ConstructFrom(operationKey, timeOut);

            return new NormalWindowProxy<T>(element);
        }


        public override void Dispose()
        {
            if (base.Close())
            {
                MessageBoxProxy confirmation = null;

                Element.Wait(() =>
                {
                    if (IsClosed)
                        return true;

                    confirmation = TryGetCurrentMessageBox();

                    if (confirmation != null)
                        return true;

                    return false;
                }, () => "Waiting for normal window to close or show confirmation dialog");


                if (confirmation != null)
                {
                    confirmation.NoButton.ButtonInvoke();
                }
            }

            OnDisposed(); 
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

        public AutomationElement SaveButton
        {
            get { return Element.ChildById("btSave"); }
        }

        public AutomationElement ReloadButton
        {
            get { return Element.ChildById("btReload"); }
        }

        public AutomationElement GetOperationButton(Enum operationKey)
        {
            return Element.Child(a => a.Current.ItemStatus == OperationDN.UniqueKey(operationKey));
        }
    }

    
}
