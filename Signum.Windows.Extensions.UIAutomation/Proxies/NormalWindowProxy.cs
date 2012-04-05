using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities;
using Signum.Entities.Operations;

namespace Signum.Windows.UIAutomation
{
    public class NormalWindowProxy : WindowProxy
    {
        public NormalWindowProxy(AutomationElement owner)
            : base(owner)
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

        public void Execute(Enum operationKey, int? timeOut = null)
        {
            var time = timeOut ?? 3000;

            ButtonBar.GetOperationButton(operationKey).ButtonInvoke();
            if (!Element.Pattern<WindowPattern>().WaitForInputIdle(time))
                throw new TimeoutException("Reloading entity after {0} took more than {1} ms".Formato(OperationDN.UniqueKey(operationKey), time));
        }

        public AutomationElement ConstructFrom(Enum operationKey, int? timeOut = null)
        {
            return Element.GetWindowAfter(
                () => ButtonBar.GetOperationButton(operationKey).ButtonInvoke(),
                () => "Executing {0} from {1}".Formato(OperationDN.UniqueKey(operationKey), EntityId), timeOut ?? 2000);
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
