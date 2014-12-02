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
using Signum.Utilities.ExpressionTrees;

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
            return "{0} {1}".FormatWith(GetType().Name, PropertyRoute == null ? PropertyRouteString.DefaultText("Unknown") : PropertyRoute.ToString());
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
                    case "CheckBox":
                        return ValueControl.GetCheckState().TryToString();
                    default:
                        throw new NotImplementedException("Unexpected Value Control of type {0}".FormatWith(ValueControl.Current.ClassName));
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
                        throw new NotImplementedException("Unexpected Value Control of type {0}".FormatWith(ValueControl.Current.ClassName));
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

        public Lite<IEntity> LiteValue
        {
            get
            {
                return NormalWindowExtensions.ParseLiteHash(Element.Current.ItemStatus);
            }
            set
            {
                AssertCompatibleLite(value);

                if (!FastSelect(value))
                {
                    FindLite(value);
                }
            }
        }

        public void FindLite(Lite<IEntity> value)
        {
            AssertCompatibleLite(value);

            if (Element.TryChildById("btFind") == null)
                throw new InvalidOperationException("The {0} {1} has no find button to complete the search for {2}".FormatWith(GetType().Name, this, value.KeyLong())); 

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

        private void AssertCompatibleLite(Lite<IEntity> value)
        {
            var imps = GetImplementations();
            
            if (!imps.IsByAll && !imps.Types.Contains(value.EntityType))
                throw new InvalidOperationException("Lite of type {0} can not be set on {1} {2} {3}".FormatWith(value.EntityType.TypeName(), GetType().TypeName(), PropertyRoute, imps));
        }

        public virtual Implementations GetImplementations()
        {
            return PropertyRoute.GetImplementations();
        }

        protected virtual bool FastSelect(Lite<IEntity> value)
        {
            return false;
        }

        public static int NormalWindowTimeout = 3 * 1000;
        public static int SearchWindowTimeout = 3 * 1000; 

        public NormalWindowProxy<T> Create<T>(int? timeOut = null) where T: ModifiableEntity
        {
            var win = CreateCapture(timeOut); 
            return new NormalWindowProxy<T>(win) { PreviousRoute = GetElementRoute() };
        }

        public AutomationElement CreateCapture(int? timeOut = null)
        {
            if (CreateButton.Current.IsOffscreen)
                throw new InvalidOperationException("CreateButton is not visible on {0}".FormatWith(this));

            var win = Element.CaptureWindow(
                () => CreateButton.ButtonInvoke(),
                () => "Create a new entity on {0}".FormatWith(this), timeOut ?? NormalWindowTimeout);
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

        public void FindSelectId(PrimaryKey id, int? timeOut = null)
        {
            var win = FindCapture(timeOut);
            using (var searchWindow = new SearchWindowProxy(win))
            {
                searchWindow.AddFilterString("Id", FilterOperation.EqualTo, id.ToString());
                searchWindow.Search();
                searchWindow.SelectElementAt(0);
                searchWindow.Ok();
            }
        }

        public void FindSelectFilter(string token, FilterOperation operation, string value, int? timeOut = null)
        {
            var win = FindCapture(timeOut);
            using (var searchWindow = new SearchWindowProxy(win))
            {
                searchWindow.AddFilterString(token, operation, value);
                searchWindow.Search();
                searchWindow.SelectElementAt(0);
                searchWindow.Ok();
            }
        }

        public AutomationElement FindCapture(int? timeOut = null)
        {
            if (FindButton.Current.IsOffscreen)
                throw new InvalidOperationException("FindButton is not visible on {0}".FormatWith(this));

            var win = Element.CaptureWindow(
                () => FindButton.ButtonInvoke(),
                () => "Search entity on {0}".FormatWith(this), timeOut ?? SearchWindowTimeout);
            return win;
        }

        public NormalWindowProxy<T> View<T>(int? timeOut = null) where T: ModifiableEntity
        {
            var win = ViewCapture(timeOut);

            return new NormalWindowProxy<T>(win) { PreviousRoute = GetElementRoute() };
        }

        public AutomationElement ViewCapture(int? timeOut = null)
        {
            if (ViewButton.Current.IsOffscreen)
                throw new InvalidOperationException("ViewButton is not visible on {0}".FormatWith(this));

            var win = Element.CaptureWindow(
                () => ViewButton.ButtonInvoke(),
                () => "View entity on {0}".FormatWith(this), timeOut ?? NormalWindowTimeout);
            return win;
        }

        protected virtual PropertyRoute GetElementRoute()
        {
            if (this.PropertyRoute == null)
                return null;

            var result = this.PropertyRoute;
            if (result.Type.IsIRootEntity() || result.Type.IsLite())
                return null;

            return result; 
        }

        public void Remove()
        {
            if (RemoveButton.Current.IsOffscreen)
                throw new InvalidOperationException("RemoveButton is not visible on {0}".FormatWith(this));

            RemoveButton.ButtonInvoke();
        }
    }

    public class EntityLineProxy : EntityBaseProxy
    {
        AutomationElement autoCompleteControl;
        public AutomationElement AutoCompleteControl
        {
            get { return autoCompleteControl ?? (autoCompleteControl = Element.Child(a => a.Current.ClassName == "AutocompleteTextBox")); }
        }

        public EntityLineProxy(AutomationElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public static int AutoCompleteTimeout = 2 * 1000;

        public void Autocomplete(string toString, int? timeOut = null)
        {
            Element.ButtonInvoke();

            AutoCompleteControl.Value(toString);

            timeOut = timeOut ?? AutoCompleteTimeout; 

            var lb = AutoCompleteControl.WaitChildById("lstBox", timeOut);

            lb.SelectListItemByName(toString, () => this.ToString());
        }

        protected override bool FastSelect(Lite<IEntity> value)
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

        public void SelectLite(Lite<IEntity> lite)
        {
            ComboBox.ComboSelectItem(a => a.Current.ItemStatus == lite.Key());
        }

        public void SelectToString(string toString)
        {
            ComboBox.Pattern<ExpandCollapsePattern>().Expand();

            ComboBox.SelectListItemByName(toString, ()=>this.ToString());
        }

        protected override bool FastSelect(Lite<IEntity> lite)
        {
            ComboBox.Pattern<ExpandCollapsePattern>().Expand();

            var item = ComboBox.TryChild(a => a.Current.ItemStatus == lite.Key());

            if (item == null)
                return false;

            item.Pattern<SelectionItemPattern>().Select();

            return true;
        }

        public void WaitHasItems(int? timeOut = null)
        {
            ComboBox.WaitComboBoxHasItems(() => "EntityCombo {0} has items".FormatWith(this), timeOut);
        }

        public void SelectFirstElement()
        {
            ComboBox.Pattern<ExpandCollapsePattern>().Expand();

            var item = ComboBox.Child(a => a.Current.ControlType == ControlType.ListItem);

            item.Pattern<SelectionItemPattern>().Select();
        }
    }

    public class EntityDetailProxy : EntityBaseProxy
    {
        static Condition BorderCondition = ConditionBuilder.ToCondition(a => a.Current.ClassName == "AutomationBorder"); 

        public AutomationElement GetDetailControl()
        {
            return Element.ChildByCondition(BorderCondition).ChildByCondition(Condition.TrueCondition);
        }

        public ILineContainer<T> GetDetailControl<T>() where T : ModifiableEntity
        {
            return GetDetailControl().ToLineContainer<T>(GetElementRoute());
        }

        public AutomationElement TryDetailControl()
        {
            return Element.ChildByCondition(BorderCondition).TryChildByCondition(Condition.TrueCondition);
        }

        public ILineContainer<T> TryDetailControl<T>() where T : ModifiableEntity
        {
            var detail = TryDetailControl();
            if (detail == null)
                return null;

            return detail.ToLineContainer<T>(GetElementRoute());
        }

        public AutomationElement GetOrCreateDetailControl()
        {
            var result = TryDetailControl();

            if (result != null)
                return result;
              
            CreateButton.ButtonInvoke();

            return GetDetailControl();
        }

        public ILineContainer<T> GetOrCreateDetailControl<T>() where T : ModifiableEntity
        {
            var result = TryDetailControl<T>();

            if (result != null)
                return result;

            CreateButton.ButtonInvoke();

            return GetDetailControl<T>();
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
                throw new InvalidOperationException("Index {0} not found on {1} with only {2} items".FormatWith(index, this, list.Count));

            list[index].Pattern<SelectionItemPattern>().Select();
        }

        public void SelectElementToString(string toString)
        {
            ListBox.SelectListItemByName(toString, () => this.ToString());
        }

        protected override PropertyRoute GetElementRoute()
        {
            if (this.PropertyRoute == null)
                return null;

            var result = this.PropertyRoute.Add("Item");
            if (result.Type.IsIRootEntity() || result.Type.IsLite())
                return null;

            return result;
        }

        public override Implementations GetImplementations()
        {
            return PropertyRoute.Add("Item").GetImplementations();
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
                .Select(ae => new RepeaterLineProxy<T>(ae, GetElementRoute())).ToList();
        }

        protected override PropertyRoute GetElementRoute()
        {
            if (this.PropertyRoute == null)
                return null;

            var result = this.PropertyRoute.Add("Item");
            if (result.Type.IsIRootEntity() || result.Type.IsLite())
                return null;

            return result;
        }

        public override Implementations GetImplementations()
        {
            return PropertyRoute.Add("Item").GetImplementations();
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
