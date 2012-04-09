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
    public class NormalWindowProxy<T>: WindowProxy where T: ModifiableEntity
    {
        public PropertyRoute PreviousRoute { get; set; }

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

        public ValueLineProxy ValueLine(Expression<Func<T, object>> property)
        {
            PropertyRoute route = GetRoute(property);

            var valueLine = MainControl.Descendant(a => a.Current.ClassName == "ValueLine" && a.Current.ItemStatus == route.ToString());

            return new ValueLineProxy(valueLine, route);
        }

        public V ValueLineValue<V>(Expression<Func<T, V>> property)
        {
            PropertyRoute route = GetRoute(property);

            var valueLine = MainControl.Descendant(a => a.Current.ClassName == "ValueLine" && a.Current.ItemStatus == route.ToString());

            return (V)new ValueLineProxy(valueLine, route).Value;
        }

        public void ValueLineValue<V>(Expression<Func<T, V>> property, V value)
        {
            PropertyRoute route = GetRoute(property);

            var valueLine = MainControl.Descendant(a => a.Current.ClassName == "ValueLine" && a.Current.ItemStatus == route.ToString());

            new ValueLineProxy(valueLine, route).Value = value;
        }

        public EntityLineProxy EntityLine(Expression<Func<T, object>> property)
        {
            PropertyRoute route = GetRoute(property);

            var entityLine = MainControl.Descendant(a => a.Current.ClassName == "EntityLine" && a.Current.ItemStatus == route.ToString());

            return new EntityLineProxy(entityLine, route);
        }

        public EntityComboProxy EntityCombo(Expression<Func<T, object>> property)
        {
            PropertyRoute route = GetRoute(property);

            var entityCombo = MainControl.Descendant(a => a.Current.ClassName == "EntityCombo" && a.Current.ItemStatus == route.ToString());

            return new EntityComboProxy(entityCombo, route);
        }

        public EntityDetailsProxy EntityDetails(Expression<Func<T, object>> property)
        {
            PropertyRoute route = GetRoute(property);

            var entityDetails = MainControl.Descendant(a => a.Current.ClassName == "EntityDetails" && a.Current.ItemStatus == route.ToString());

            return new EntityDetailsProxy(entityDetails, route);
        }

        public EntityListProxy EntityList(Expression<Func<T, object>> property)
        {
            PropertyRoute route = GetRoute(property);

            var entityList = MainControl.Descendant(a => a.Current.ClassName == "EntityList" && a.Current.ItemStatus == route.ToString());

            return new EntityListProxy(entityList, route);
        }

        public EntityRepeaterProxy EntityRepeater(Expression<Func<T, object>> property)
        {
            PropertyRoute route = GetRoute(property);

            var entityRepeater = MainControl.Descendant(a => a.Current.ClassName == "EntityRepeater" && a.Current.ItemStatus == route.ToString());

            return new EntityRepeaterProxy(entityRepeater, route);
        }

        private PropertyRoute GetRoute<S>(Expression<Func<T, S>> property)
        {
            PropertyRoute result = PreviousRoute ?? PropertyRoute.Root(typeof(T));

            foreach (var mi in Reflector.GetMemberList(property))
            {
                result = result.Add(mi);
            }
            return result;
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
