using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Windows.Automation;
using System.Linq.Expressions;
using Signum.Entities.Reflection;
using Signum.Engine;

namespace Signum.Windows.UIAutomation
{
    public class LineContainer<T> : ILineContainer<T> where T : ModifiableEntity
    {
        public PropertyRoute PreviousRoute { get; set; }
        public AutomationElement Element { get; set;  }
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

        public static ValueLineProxy ValueLine(this ILineContainer container, PropertyRoute route, TreeScope scope = TreeScope.Descendants)
        {
            var valueLine = container.Element.Element(scope, a => (a.Current.ClassName == "ValueLine" || a.Current.ClassName == "TextArea") && a.Current.Name == route.ToString());

            return new ValueLineProxy(valueLine, route);
        }

        public static EntityLineProxy EntityLine(this ILineContainer container, PropertyRoute route, TreeScope scope = TreeScope.Descendants)
        {
            var entityLine = container.Element.Element(scope, a => a.Current.ClassName == "EntityLine" && a.Current.Name == route.ToString());

            return new EntityLineProxy(entityLine, route);
        }

        public static EntityComboProxy EntityCombo(this ILineContainer container, PropertyRoute route, TreeScope scope = TreeScope.Descendants)
        {
            var entityCombo = container.Element.Element(scope, a => a.Current.ClassName == "EntityCombo" && a.Current.Name == route.ToString());

            return new EntityComboProxy(entityCombo, route);
        }

        public static EntityDetailProxy EntityDetail(this ILineContainer container, PropertyRoute route, TreeScope scope = TreeScope.Descendants)
        {
            var entityDetails = container.Element.Element(scope, a => a.Current.ClassName == "EntityDetail" && a.Current.Name == route.ToString());

            return new EntityDetailProxy(entityDetails, route);
        }

        public static EntityListProxy EntityList(this ILineContainer container, PropertyRoute route, TreeScope scope = TreeScope.Descendants)
        {
            var entityList = container.Element.Element(scope, a => a.Current.ClassName == "EntityList" && a.Current.Name == route.ToString());

            return new EntityListProxy(entityList, route);
        }

        public static EntityRepeaterProxy EntityRepeater(this ILineContainer container, PropertyRoute route, TreeScope scope = TreeScope.Descendants)
        {
            var entityRepeater = container.Element.Element(scope, a => a.Current.ClassName == "EntityRepeater" && a.Current.Name == route.ToString());

            return new EntityRepeaterProxy(entityRepeater, route);
        }
    }

    public static class LineContainerOfTExtensions
    {
        public static ILineContainer<T> ToLineContainer<T>(this AutomationElement element, PropertyRoute previousRoute = null) where T : ModifiableEntity
        {
            return new LineContainer<T> { Element = element, PreviousRoute = previousRoute };
        }

        public static ILineContainer<C> SubContainer<T, C>(this ILineContainer<T> container, Expression<Func<T, C>> property, TreeScope scope = TreeScope.Descendants) 
            where T : ModifiableEntity
            where C : ModifiableEntity
        {
            PropertyRoute route = property.Body.NodeType != ExpressionType.Convert ? container.GetRoute(property) :
                 container.GetRoute(Expression.Lambda<Func<T, IEntity>>(((UnaryExpression)property.Body).Operand, property.Parameters));

            string str = route.PropertyRouteType == PropertyRouteType.LiteEntity ? route.Parent.ToString() : route.ToString();

            var subContainer = container.Element.Element(scope, a => a.Current.Name == str);

            return new LineContainer<C> { Element = subContainer, PreviousRoute = typeof(C).IsEmbeddedEntity() ? route : null };
        }

        public static ValueLineProxy ValueLine<T>(this ILineContainer<T> container, Expression<Func<T, object>> property, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.ValueLine(route, scope);
        }

        public static V ValueLineValue<T, V>(this ILineContainer<T> container, Expression<Func<T, V>> property, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return (V)container.ValueLine(route, scope).Value;
        }

        public static void ValueLineValue<T, V>(this ILineContainer<T> container, Expression<Func<T, V>> property, V value, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            container.ValueLine(route, scope).Value = value;
        }

        public static EntityLineProxy EntityLine<T>(this ILineContainer<T> container, Expression<Func<T, object>> property, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityLine(route, scope);
        }

        public static V EntityLineValue<T, V>(this ILineContainer<T> container, Expression<Func<T, V>> property, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            var lite = container.EntityLine(route, scope).LiteValue;

            return lite is V ? (V)lite : (V)lite.Retrieve();
        }

        public static void EntityLineValue<T, V>(this ILineContainer<T> container, Expression<Func<T, V>> property, V value, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            container.EntityLine(route, scope).LiteValue = value as Lite<IEntity> ?? ((IEntity)value).ToLite();
        }


        public static EntityComboProxy EntityCombo<T>(this ILineContainer<T> container, Expression<Func<T, object>> property, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityCombo(route, scope);
        }

        public static V EntityComboValue<T, V>(this ILineContainer<T> container, Expression<Func<T, V>> property, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            var lite = container.EntityCombo(route, scope).LiteValue;

            return lite is V ? (V)lite : (V)lite.Retrieve();
        }

        public static void EntityComboValue<T, V>(this ILineContainer<T> container, Expression<Func<T, V>> property, V value, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            container.EntityCombo(route, scope).LiteValue = value as Lite<IEntity> ?? ((IEntity)value).ToLite();
        }


        public static EntityDetailProxy EntityDetail<T>(this ILineContainer<T> container, Expression<Func<T, object>> property, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityDetail(route, scope);
        }

        public static ILineContainer<S> EntityDetailControl<T, S>(this ILineContainer<T> container, Expression<Func<T, S>> property, TreeScope scope = TreeScope.Descendants)
            where T : ModifiableEntity
            where S : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityDetail(route, scope).GetDetailControl<S>();
        }

        public static EntityListProxy EntityList<T>(this ILineContainer<T> container, Expression<Func<T, object>> property, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityList(route, scope);
        }

        public static EntityRepeaterProxy EntityRepeater<T>(this ILineContainer<T> container, Expression<Func<T, object>> property, TreeScope scope = TreeScope.Descendants) where T : ModifiableEntity
        {
            PropertyRoute route = container.GetRoute(property);

            return container.EntityRepeater(route, scope);
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
