using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Windows.Automation;
using System.Linq.Expressions;
using Signum.Entities.Reflection;

namespace Signum.Windows.UIAutomation
{
    class LineContainer<T> : ILineContainer<T> where T : ModifiableEntity
    {
        public PropertyRoute PreviousRoute { get; set; }
        public AutomationElement Element { get; set;  }
        public WindowProxy ParentWindow { get; set; }
    }

    public interface ILineContainer<T> : ILineContainer where T : ModifiableEntity
    {
    }

    public interface ILineContainer
    {
        PropertyRoute PreviousRoute { get; }
        AutomationElement Element { get; }
    }

    public static class LineContainerExtensions
    {
        //public static bool IsVisible(this ILineContainer container, PropertyRoute route)
        //{
        //    return container.Element.Descendant(a => a.Current.ItemStatus == route.ToString()) != null;
        //}

        public static ValueLineProxy ValueLine(this ILineContainer container, PropertyRoute route)
        {
            var valueLine = container.Element.Descendant(a =>( a.Current.ClassName == "ValueLine"  ||  a.Current.ClassName == "TextArea") && a.Current.ItemStatus == route.ToString());

            return new ValueLineProxy(valueLine, route);
        }

        public static EntityLineProxy EntityLine(this ILineContainer container, PropertyRoute route)
        {
            var entityLine = container.Element.Descendant(a => a.Current.ClassName == "EntityLine" && a.Current.ItemStatus == route.ToString());

            return new EntityLineProxy(entityLine, route);
        }

        public static EntityComboProxy EntityCombo(this ILineContainer container, PropertyRoute route)
        {
            var entityCombo = container.Element.Descendant(a => a.Current.ClassName == "EntityCombo" && a.Current.ItemStatus == route.ToString());

            return new EntityComboProxy(entityCombo, route);
        }

        public static EntityDetailProxy EntityDetail(this ILineContainer container, PropertyRoute route)
        {
            var entityDetails = container.Element.Descendant(a => a.Current.ClassName == "EntityDetail" && a.Current.ItemStatus == route.ToString());

            return new EntityDetailProxy(entityDetails, route);
        }

        public static EntityListProxy EntityList(this ILineContainer container, PropertyRoute route)
        {
            var entityList = container.Element.Descendant(a => a.Current.ClassName == "EntityList" && a.Current.ItemStatus == route.ToString());

            return new EntityListProxy(entityList, route);
        }

        public static EntityRepeaterProxy EntityRepeater(this ILineContainer container, PropertyRoute route)
        {
            var entityRepeater = container.Element.Descendant(a => a.Current.ClassName == "EntityRepeater" && a.Current.ItemStatus == route.ToString());

            return new EntityRepeaterProxy(entityRepeater, route);
        }
    }

    public static class LineContainerOfTExtensions
    {
        public static ILineContainer<T> ToLineContainer<T>(this AutomationElement element, PropertyRoute previousRoute = null) where T : ModifiableEntity
        {
            return new LineContainer<T> { Element = element, PreviousRoute = previousRoute };
        }

        public static ILineContainer<C> SubContainer<T, C>(this ILineContainer<T> container, Expression<Func<T, C>> property)
            where T : ModifiableEntity
            where C : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            var subContainer = container.Element.Descendant(a => a.Current.ItemStatus == route.ToString());

            return new LineContainer<C> { Element = subContainer, PreviousRoute = typeof(C).IsEmbeddedEntity() ? route : null };
        }

        //public static bool ValueLine<T>(this ILineContainer<T> container, Expression<Func<T, object>> property) where T : ModifiableEntity
        //{
        //    return container.Element.Descendant(a => a.Current.ItemStatus == route.ToString()) != null;
        //}

        public static ValueLineProxy ValueLine<T>(this ILineContainer<T> container, Expression<Func<T, object>> property) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.ValueLine(route);
        }

        public static V ValueLineValue<T, V>(this ILineContainer<T> container, Expression<Func<T, V>> property) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return (V)container.ValueLine(route).Value;
        }

        public static void ValueLineValue<T, V>(this ILineContainer<T> container, Expression<Func<T, V>> property, V value) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            container.ValueLine(route).Value = value;
        }

        public static EntityLineProxy EntityLine<T>(this ILineContainer<T> container, Expression<Func<T, object>> property) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityLine(route);
        }

        public static EntityComboProxy EntityCombo<T>(this ILineContainer<T> container, Expression<Func<T, object>> property) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityCombo(route);
        }

        public static EntityDetailProxy EntityDetail<T>(this ILineContainer<T> container, Expression<Func<T, object>> property) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityDetail(route);
        }

        public static ILineContainer<S> EntityDetailControl<T, S>(this ILineContainer<T> container, Expression<Func<T, S>> property)
            where T : ModifiableEntity
            where S : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityDetail(route).GetDetailControl().ToLineContainer<S>();
        }

        public static EntityListProxy EntityList<T>(this ILineContainer<T> container, Expression<Func<T, object>> property) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityList(route);
        }

        public static EntityRepeaterProxy EntityRepeater<T>(this ILineContainer<T> container, Expression<Func<T, object>> property) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityRepeater(route);
        }

        public static PropertyRoute GetRoute<T, S>(this ILineContainer<T> container, Expression<Func<T, S>> property) where T : ModifiableEntity
        {
            PropertyRoute result = container.PreviousRoute ?? PropertyRoute.Root(typeof(T));

            foreach (var mi in Reflector.GetMemberList(property))
            {
                result = result.Add(mi);
            }
            return result;
        }
    }
}
