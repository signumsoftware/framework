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
            get { return Element.Current.Name; }
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
                    case "NumericTextBox":
                        return ValueControl.Value();
                    case "ComboBox":
                        return ValueControl.ComboGetSelectedItem().Current.Name;
                    case "TimePicker":
                        return ValueControl.ChildById("textBox").Value();
                    case "DateTimePicker":
                        return ValueControl.ChildById("PART_EditableTextBox").Value();
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
                    case "NumericTextBox":
                        ValueControl.Value(value);
                        break;
                    case "ComboBox":
                        ValueControl.ComboSelectItem(a => a.Current.Name == value);
                        break;
                    case "TimePicker":
                        ValueControl.ChildById("textBox").Value(value);
                        break;
                    case "DateTimePicker":
                        ValueControl.ChildById("PART_EditableTextBox").Value(value);
                        break;
                    case "CheckBox":
                        ValueControl.SetCheck(value == "True");
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
            get { return GetValue(PropertyRoute.Type); }
            set { SetValue(value, PropertyRoute.Type); }
        }

        public object GetValue(Type type)
        {
            return ReflectionTools.Parse(StringValue, type);
        }

        public void SetValue(object value, Type type)
        {
            StringValue = value == null ? null :
                    value is IFormattable ? ((IFormattable)value).ToString(ValueControl.Current.ItemStatus ?? Reflector.FormatString(type), null) :
                    value.ToString();
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

        public Lite<IIdentifiable> LiteValue
        {
            get
            {
                return NormalWindowExtensions.ParseLiteHash(Element.Current.ItemStatus);
            }
            set
            {
                if (!FastSelect(value))
                {
                    FindLite(value);
                }
            }
        }

        public void FindLite(Lite<IIdentifiable> value)
        {
            if (Element.TryChildById("btFind") == null)
                throw new InvalidOperationException("The {0} {1} has no find button to complete the search for {2}".Formato(GetType().Name, this, value.KeyLong())); 

            var win = FindCapture();

            if(win.Current.ClassName == "SelectorWindow")
            {
                using (var selector = new SelectorWindowProxy(win))
                {
                    win = selector.SelectCapture(value.EntityType.FullName);
                }
            }

            using (var sw = new SearchWindowProxy(win))
            {
                sw.AddFilterString("Entity.Id", FilterOperation.EqualTo, value.Id.ToString());
                sw.Search();
                sw.SelectElementAt(0);
                sw.Ok();
            }
        }

        protected virtual bool FastSelect(Lite<IIdentifiable> value)
        {
            return false;
        }

        public static int NormalWindowTimeout = 3 * 1000;
        public static int SearchWindowTimeout = 3 * 1000; 

        public NormalWindowProxy<T> Create<T>(int? timeOut = null) where T: ModifiableEntity
        {
            return new NormalWindowProxy<T>(CreateCapture(timeOut ?? NormalWindowTimeout));
        }

        public AutomationElement CreateCapture(int? timeOut = null)
        {
            if (CreateButton.Current.IsOffscreen)
                throw new InvalidOperationException("CreateButton is not visible on {0}".Formato(this));

            var win = Element.CaptureWindow(
                () => CreateButton.ButtonInvoke(),
                () => "Create a new entity on {0}".Formato(this), timeOut);
            return win;
        }

        public SearchWindowProxy Find(int? timeOut = null)
        {
            var win = FindCapture(timeOut);

            return new SearchWindowProxy(win);
        }

        public void FindSelectRow(int index = 0, int? timeOut = null)
        {
            var win = FindCapture(timeOut);

            SearchWindowProxy.Select(win, index);
        }

        public void FindAutoByFilterId(int id, int? timeOut = null)
        {
            var win = FindCapture(timeOut);
            var searchWindow = new SearchWindowProxy(win);

           
            searchWindow.AddFilterString("Id", FilterOperation.EqualTo, id.ToString());
            searchWindow.Search();
            searchWindow.SelectElementAt(0);
            searchWindow.Ok();
        }

        public AutomationElement FindCapture(int? timeOut = null)
        {
            if (FindButton.Current.IsOffscreen)
                throw new InvalidOperationException("FindButton is not visible on {0}".Formato(this));

            var win = Element.CaptureWindow(
                () => FindButton.ButtonInvoke(),
                () => "Search entity on {0}".Formato(this), timeOut);
            return win;
        }

        public NormalWindowProxy<T> View<T>(int? timeOut = null) where T: ModifiableEntity
        {
            if (ViewButton.Current.IsOffscreen)
                throw new InvalidOperationException("ViewButton is not visible on {0}".Formato(this));

            var win = Element.CaptureWindow(
                () => ViewButton.ButtonInvoke(),
                () => "View entity on {0}".Formato(this), timeOut);

            return new NormalWindowProxy<T>(win);
        }

        public void Remove()
        {
            if (RemoveButton.Current.IsOffscreen)
                throw new InvalidOperationException("RemoveButton is not visible on {0}".Formato(this));

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

            list.SelectByName(toString, ()=>this.ToString());
        }

        protected override bool FastSelect(Lite<IIdentifiable> value)
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

        public void SelectLite(Lite<IIdentifiable> lite)
        {
            ComboBox.ComboSelectItem(a => a.Current.ItemStatus == lite.Key());
        }

        public void SelectToString(string toString)
        {
            ComboBox.Pattern<ExpandCollapsePattern>().Expand();

            var list = ComboBox.ChildrenAll();

            list.SelectByName(toString, ()=>this.ToString());
        }

        protected override bool FastSelect(Lite<IIdentifiable> lite)
        {
            ComboBox.Pattern<ExpandCollapsePattern>().Expand();

            var item = ComboBox.TryChild(a => a.Current.ItemStatus == lite.Key());

            if (item == null)
                return false;

            item.Pattern<SelectionItemPattern>().Select();

            return true;
        }

        public List<AutomationElement> WaitChargedList()
        {
            List<AutomationElement> list = null;

            ComboBox.Wait(() =>
            {
                ComboBox.Pattern<ExpandCollapsePattern>().Expand();
                list = ComboBox.Children(c => c.Current.ControlType == ControlType.ListItem);
                return list.Any();
            }, () => "ComboBox not charged");

            return list;
        }
    }

    public class EntityDetailProxy : EntityBaseProxy
    {
        public static Condition DetailCondition = ConditionBuilder.ToCondition(a => a.Current.ControlType != ControlType.Text && a.Current.ControlType != ControlType.Button); 

        public AutomationElement GetDetailControl()
        {
            return Element.ChildByCondition(DetailCondition);
        }

        public AutomationElement TryDetailControl()
        {
            return Element.TryChildByCondition(DetailCondition);
        }

        public AutomationElement GetOrCreateDetailControl()
        {
            var result = TryDetailControl();

            if (result == null)
                CreateButton.ButtonInvoke();

            return GetDetailControl();
        }

        public EntityDetailProxy(AutomationElement element, PropertyRoute route)
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

            list.SelectByName(toString, () => this.ToString());
        }
    }

    public class EntityRepeaterProxy : EntityBaseProxy
    {
        public EntityRepeaterProxy(AutomationElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public RepeaterLineProxy<T> CreateLineContainer<T>() where T : ModifiableEntity
        {
            CreateButton.ButtonInvoke();
            return GetRepeaterElements<T>().Last();
        }

        public void CreateLineContainer<T>(Action<ILineContainer<T>> action) where T : ModifiableEntity
        {
            CreateButton.ButtonInvoke();
            var lineContainer = GetRepeaterElements<T>().Last();
            action(lineContainer);
        }

        public List<RepeaterLineProxy<T>> GetRepeaterElements<T>() where T : ModifiableEntity
        {
            return Element.Descendants(a => a.Current.ClassName == "EntityRepeaterContentControl")
                .Select(ae => new RepeaterLineProxy<T>(ae, this.PropertyRoute.Add("Item"))).ToList();
        }
    }

    public class RepeaterLineProxy<T> : LineContainer<T> where T : ModifiableEntity
    {
        public RepeaterLineProxy(AutomationElement element, PropertyRoute previousRoute)
        {
            this.Element = element;
            this.PreviousRoute = previousRoute;
        }

        public AutomationElement RemoveButton
        {
            get { return Element.ChildById("btnRemove"); }
        }

        public void Remove()
        {
            RemoveButton.ButtonInvoke();
        }
    }
}
