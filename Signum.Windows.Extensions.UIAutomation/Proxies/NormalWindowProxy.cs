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
        }

        public void Reload()
        {
            ButtonBar.ReloadButton.ButtonInvoke();
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

        public static int ExecuteTimeout = 3 * 1000;

        public void Execute(Enum operationKey, int? timeOut = null)
        {
            var time = timeOut ?? ExecuteTimeout;

            ButtonBar.GetOperationButton(operationKey).ButtonInvoke();
            if (!Element.Pattern<WindowPattern>().WaitForInputIdle(time))
                throw new TimeoutException("Reloading entity after {0} took more than {1} ms".Formato(OperationDN.UniqueKey(operationKey), time));
        }

        public static int ConstructFromTimeout = 2 * 1000;

        public AutomationElement ConstructFrom(Enum operationKey, int? timeOut = null)
        {
            var time = timeOut ?? ConstructFromTimeout;

            return Element.GetWindowAfter(
                () => ButtonBar.GetOperationButton(operationKey).ButtonInvoke(),
                () => "Executing {0} from {1} took more than {2} ms".Formato(OperationDN.UniqueKey(operationKey), EntityId, time), time);
        }

        public override void Dispose()
        {
            if (base.Close())
            {
                MessageBoxProxy confirmation = null;

                Element.Wait(() =>
                {
                    if (Element.Current.IsOffscreen)
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
        }
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
