using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{
    public class BaseLineProxy
    {
        public AutomationElement Element { get; private set; }

        public BaseLineProxy(AutomationElement element)
        {
            this.Element = element;
        }

        public string PropertyRoute
        {
            get { return Element.Current.ItemStatus;  }
        }

        public string Label
        {
            get { return Element.ChildById("label").Current.Name; }
        }
    }

    public class ValueLineProxy : BaseLineProxy
    {
        AutomationElement valueControl;
        public AutomationElement ValueControl
        {
            get { return valueControl ?? (valueControl = Element.Child(a => a.Current.ControlType != ControlType.Text)); }
        }

        public ValueLineProxy(AutomationElement element): base(element)
        {
        }

        public string Value
        {
            get
            {
                switch (ValueControl.Current.ClassName)
                {
                    case "TextBox":
                        return ValueControl.Value();
                    case "ComboBox":
                        return ValueControl.ComboGetSelectedItem().Current.Name;
                    default:
                        throw new NotImplementedException("Unexpected Value Control of type {0}".Formato(ValueControl.Current.ClassName));
                }
            }

            set
            {
                switch (ValueControl.Current.ClassName)
                {
                    case "TextBox":
                        ValueControl.Value(value);
                        break;
                    case "ComboBox":
                        ValueControl.ComboSelectItem(a => a.Current.Name == value);
                        break;
                    default:
                        throw new NotImplementedException("Unexpected Value Control of type {0}".Formato(ValueControl.Current.ClassName));
                }
            }
        }

        public string Unit
        {
            get { return Element.ChildById("unit").Current.Name; }
        }
    }

    public class EntityBaseProxy : BaseLineProxy
    {   
        public AutomationElement CreateButton
        {
            get { return Element.ChildById("btCreate"); }
        }

        public AutomationElement FindButton
        {
            get { return Element.ChildById("btFind"); }
        }

        public AutomationElement ViewButton
        {
            get { return Element.ChildById("btView"); }
        }

        public AutomationElement RemoveButton
        {
            get { return Element.ChildById("btRemove"); }
        }

        public EntityBaseProxy(AutomationElement element)
            : base(element)
        {
        }

        public NormalWindowProxy Create(int? timeOut = null)
        {
            return new NormalWindowProxy(CreateBasic(timeOut));
        }

        public AutomationElement CreateBasic(int? timeOut = null)
        {
            var win = Element.GetModalWindowAfter(
                () => CreateButton.ButtonInvoke(),
                () => "Create a new entity on {0}".Formato(Element.Current.ItemStatus), timeOut);
            return win;
        }

        public SearchWindowProxy Find(int? timeOut = null)
        {
            var win = Element.GetModalWindowAfter(
                () => FindButton.ButtonInvoke(),
                () => "Search entity on {0}".Formato(Element.Current.ItemStatus), timeOut);

            return new SearchWindowProxy(win);
        }


        public NormalWindowProxy View(int? timeOut = null)
        {
            var win = Element.GetModalWindowAfter(
                () => ViewButton.ButtonInvoke(),
                () => "View entity on {0}".Formato(Element.Current.ItemStatus), timeOut);

            return new NormalWindowProxy(win);
        }

        public void Remove()
        {
            RemoveButton.ButtonInvoke();
        }
     
    }

    public class EntityLineProxy : EntityBaseProxy
    {
        AutomationElement autoCompleteControl;
        public AutomationElement AutoCompleteControl
        {
            get { return autoCompleteControl ?? (autoCompleteControl = Element.Child(a => a.Current.ClassName == "AutoCompleteTextBox")); }
        }

        public EntityLineProxy(AutomationElement element)
            : base(element)
        {
        }

        public void AutoComplete(string text, int? timeOut = null)
        {
            Element.ButtonInvoke();

            AutoCompleteControl.Value(text);

            var lb = AutoCompleteControl.WaitChildById("lstBox", timeOut ?? 2000);

            var li = lb.Child(a => a.Current.ControlType == ControlType.ListItem);

            li.Pattern<SelectionItemPattern>().Select();
        }
    }
}
