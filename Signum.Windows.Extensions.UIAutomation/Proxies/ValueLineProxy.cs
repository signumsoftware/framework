using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Entities.Reflection;
using Signum.Engine;

namespace Signum.Windows.UIAutomation
{
    public abstract class BaseLineProxy
    {
        public AutomationElement Element { get; private set; }

        public PropertyRoute PropertyRoute { get; private set; }

        public BaseLineProxy(AutomationElement element, PropertyRoute route)
        {
            this.Element = element;
            this.PropertyRoute = route;
        }

        public string PropertyRouteString
        {
            get { return Element.Current.ItemStatus; }
        }

        public string Label
        {
            get { return Element.ChildById("label").Current.Name; }
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(GetType().Name, PropertyRoute == null ? PropertyRouteString.DefaultText("Unknown") : PropertyRoute.ToString());
        }
    }

    public class ValueLineProxy : BaseLineProxy
    {
        AutomationElement valueControl;
        public AutomationElement ValueControl
        {
            get { return valueControl ?? (valueControl = Element.Child(a => a.Current.ControlType != ControlType.Text)); }
        }

        public ValueLineProxy(AutomationElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public string StringValue
        {
            get
            {
                switch (ValueControl.Current.ClassName)
                {
                    case "TextBox":
                        return ValueControl.Value();
                    case "ComboBox":
                        return ValueControl.ComboGetSelectedItem().Current.Name;
                    case "TimePicker":
                        return ValueControl.ChildById("textBox").Value();
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
                    case "TimePicker":
                        ValueControl.ChildById("textBox").Value(value);
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

        public object Value
        {
            get { return ReflectionTools.Parse(StringValue, PropertyRoute.Type); }
            set
            {
                StringValue = value == null ? null :
                    value is IFormattable ? ((IFormattable)value).ToString(Reflector.FormatString(PropertyRoute), null) :
                    value.ToString();
            } 
        }
    }

    public class TextAreaProxy: BaseLineProxy
    {
        public TextAreaProxy(AutomationElement element, PropertyRoute route)
            : base(element, route)
        {
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

        public PropertyRoute Route { get; private set; } 

        public EntityBaseProxy(AutomationElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public Lite LiteValue
        {
            get { return string.IsNullOrEmpty(Element.Current.HelpText) ? null : TypeLogic.ParseLite(Route == null ? typeof(IdentifiableEntity) : Reflector.ExtractLite(Route.Type), Element.Current.HelpText); }
            set
            {
                if (!FastSelect(value))
                {
                    FindLite(value);
                }
            }
        }

        public void FindLite(Lite value)
        {
            if (Element.TryChildById("btFind") == null)
                throw new InvalidOperationException("The {0} {1} has no find button to complete the search for {2}".Formato(GetType().Name, this, value.KeyLong())); 

            var win = FindBasic();

            if(win.Current.ClassName == "SelectorWindow")
            {
                using(var selector = new SelectorWindowProxy(win))
                {
                    win = Element.GetModalWindowAfter(() => selector.SelectType(value.RuntimeType),
                        () => "Open SearchWindow on {0} after type selector took more than {1} ms".Formato(this, SearchWindowTimeout), SearchWindowTimeout);
                }
            }

            using (var sw = new SearchWindowProxy(win))
            {
                sw.AddFilterString("Entity.Id", FilterOperation.EqualTo, value.Id.ToString());
                sw.SelectElementAt(0);
                sw.Ok();
            }
        }

        protected virtual bool FastSelect(Lite value)
        {
            return false;
        }

        public static int NormalWindowTimeout = 3 * 1000;
        public static int SearchWindowTimeout = 3 * 1000; 

        public NormalWindowProxy<T> Create<T>(int? timeOut = null) where T: ModifiableEntity
        {
            return new NormalWindowProxy<T>(CreateBasic(timeOut ?? NormalWindowTimeout));
        }

        public AutomationElement CreateBasic(int? timeOut = null)
        {
            var win = Element.GetModalWindowAfter(
                () => CreateButton.ButtonInvoke(),
                () => "Create a new entity on {0}".Formato(this), timeOut);
            return win;
        }

        public SearchWindowProxy Find(int? timeOut = null)
        {
            var win = FindBasic(timeOut);

            return new SearchWindowProxy(win);
        }

        public AutomationElement FindBasic(int? timeOut = null)
        {
            var win = Element.GetModalWindowAfter(
                () => FindButton.ButtonInvoke(),
                () => "Search entity on {0}".Formato(this), timeOut);
            return win;
        }

        public NormalWindowProxy<T> View<T>(int? timeOut = null) where T: ModifiableEntity
        {
            var win = Element.GetModalWindowAfter(
                () => ViewButton.ButtonInvoke(),
                () => "View entity on {0}".Formato(this), timeOut);

            return new NormalWindowProxy<T>(win);
        }

        public void Remove()
        {
            RemoveButton.ButtonInvoke();
        }

        protected void SelectByString(List<AutomationElement> list, string toString)
        {
            var filtered = list.Where(a => a.Current.Name == toString).ToList();

            if (filtered.Count == 1)
            {
                filtered.SingleEx().Pattern<SelectionItemPattern>().Select();
            }
            else
            {
                filtered = list.Where(a => a.Current.Name.Contains(toString, StringComparison.InvariantCultureIgnoreCase)).ToList();

                if (filtered.Count == 0)
                    throw new InvalidOperationException("No element found on {0} with ToString '{1}'. Found: \r\n{2}".Formato(this, toString, list.ToString(a => a.Current.Name, "\r\n")));

                if (filtered.Count > 1)
                    throw new InvalidOperationException("Ambiguous elements found on {0} with ToString '{1}'. Found: \r\n{2}".Formato(this, toString, filtered.ToString(a => a.Current.Name, "\r\n")));

                filtered.Single().Pattern<SelectionItemPattern>().Select();
            }
        }
    }

    public class EntityLineProxy : EntityBaseProxy
    {
        AutomationElement autoCompleteControl;
        public AutomationElement AutoCompleteControl
        {
            get { return autoCompleteControl ?? (autoCompleteControl = Element.Child(a => a.Current.ClassName == "AutoCompleteTextBox")); }
        }

        public EntityLineProxy(AutomationElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public static int AutoCompleteTimeout = 2 * 1000;

        public void AutoComplete(string toString, int? timeOut = null)
        {
            Element.ButtonInvoke();

            AutoCompleteControl.Value(toString);

            timeOut = timeOut ?? AutoCompleteTimeout; 

            var lb = AutoCompleteControl.WaitChildById("lstBox", timeOut);

            var list = lb.Children(a => a.Current.ControlType == ControlType.ListItem);

            SelectByString(list, toString);
        }

        protected override bool FastSelect(Lite value)
        {
            if (!value.ToString().HasText())
                return false;

            Element.ButtonInvoke();

            AutoCompleteControl.Value(value.ToString());

            var lb = AutoCompleteControl.WaitChildById("lstBox", AutoCompleteTimeout);

            var list = lb.Children(a => a.Current.ControlType == ControlType.ListItem);

            if (list.Count != 1)
                return false;

            list.Single().Pattern<SelectionItemPattern>().Select();

            return true;
        }
    }

    public class EntityComboProxy : EntityBaseProxy
    {
        AutomationElement comboBox;
        public AutomationElement ComboBox
        {
            get { return comboBox ?? (comboBox = Element.Child(a => a.Current.ControlType == ControlType.ComboBox)); }
        }

        public EntityComboProxy(AutomationElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public void SelectLite(Lite lite)
        {
            ComboBox.ComboSelectItem(a => a.Current.ItemStatus == lite.Key());
        }

        public void SelectToString(string toString)
        {
            ComboBox.Pattern<ExpandCollapsePattern>().Expand();

            var list = ComboBox.ChildrenAll();

            SelectByString(list, toString);
        }

        protected override bool FastSelect(Lite lite)
        {
            ComboBox.Pattern<ExpandCollapsePattern>().Expand();

            var item = ComboBox.TryChild(a => a.Current.ItemStatus == lite.Key());

            if (item == null)
                return false;

            item.Pattern<SelectionItemPattern>().Select();

            return true;
        }
    }

    public class EntityDetailsProxy : EntityBaseProxy
    {
        public EntityDetailsProxy(AutomationElement element, PropertyRoute route)
            : base(element, route)
        {
        }
    }

    public class EntityListProxy : EntityBaseProxy
    {
        AutomationElement listBox;
        public AutomationElement ListBox
        {
            get { return listBox ?? (listBox = Element.Child(a => a.Current.ControlType == ControlType.List)); }
        }

        public EntityListProxy(AutomationElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public void SelectElementAt(int index)
        {
            var list = ListBox.ChildrenAll();

            if (list.Count <= index)
                throw new InvalidOperationException("Index {0} not found on {1} with only {2} items".Formato(index, this, list.Count));

            list[index].Pattern<SelectionItemPattern>().Select();
        }

        public void SelectElementToString(string toString)
        {
            var list = ListBox.ChildrenAll();

            SelectByString(list, toString);
        }
    }

    public class EntityRepeaterProxy : EntityBaseProxy
    {
        public EntityRepeaterProxy(AutomationElement element, PropertyRoute route)
            : base(element, route)
        {
        }
    }
}
